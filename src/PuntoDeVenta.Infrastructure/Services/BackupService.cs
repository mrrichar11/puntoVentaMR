using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Backup;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Infrastructure.Data;

namespace PuntoDeVenta.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;
    private const int MaxBackupsRetenidos = 30;

    public BackupService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string ObtenerCarpetaBackupsPredeterminada()
    {
        var carpetaBase = AppDomain.CurrentDomain.BaseDirectory;
        var carpetaBackups = Path.Combine(carpetaBase, "Backups");
        if (!Directory.Exists(carpetaBackups))
        {
            Directory.CreateDirectory(carpetaBackups);
        }
        return carpetaBackups;
    }

    public async Task<BackupInfoDto> CrearBackupAsync(string? rutaDestino = null, bool esAutomaticoCierre = false, CancellationToken cancellationToken = default)
    {
        var carpetaBackups = ObtenerCarpetaBackupsPredeterminada();

        if (string.IsNullOrWhiteSpace(rutaDestino))
        {
            var tipoPrefijo = esAutomaticoCierre ? "cierre_caja" : "manual";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var nombreArchivo = $"backup_puntodeventa_{tipoPrefijo}_{timestamp}.db";
            rutaDestino = Path.Combine(carpetaBackups, nombreArchivo);
        }

        // Asegurar que el directorio contenedor exista
        var dirDestino = Path.GetDirectoryName(rutaDestino);
        if (!string.IsNullOrEmpty(dirDestino) && !Directory.Exists(dirDestino))
        {
            Directory.CreateDirectory(dirDestino);
        }

        // Si el archivo ya existía, SQLite VACUUM INTO arroja error; eliminar previo
        if (File.Exists(rutaDestino))
        {
            File.Delete(rutaDestino);
        }

        // Formatear ruta para SQLite con barras normales y caracteres seguros
        var rutaSqlite = rutaDestino.Replace('\\', '/').Replace("'", "''");

#pragma warning disable EF1002
        // Ejecutar snapshot atómico nativo de SQLite (VACUUM INTO no admite parámetros @p0)
        await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{rutaSqlite}';", cancellationToken);
#pragma warning restore EF1002

        var infoArchivo = new FileInfo(rutaDestino);

        var dto = new BackupInfoDto
        {
            NombreArchivo = Path.GetFileName(rutaDestino),
            RutaCompleta = Path.GetFullPath(rutaDestino),
            TamañoBytes = infoArchivo.Exists ? infoArchivo.Length : 0,
            FechaCreacion = infoArchivo.Exists ? infoArchivo.CreationTime : DateTime.Now,
            EsAutomaticoCierre = esAutomaticoCierre
        };

        // Si fue automático, aplicar política de retención para no saturar disco
        if (esAutomaticoCierre)
        {
            RotarBackupsAntiguos(carpetaBackups, MaxBackupsRetenidos);
        }

        return dto;
    }

    public Task<IReadOnlyList<BackupInfoDto>> ObtenerHistorialBackupsAsync(CancellationToken cancellationToken = default)
    {
        var carpetaBackups = ObtenerCarpetaBackupsPredeterminada();
        var lista = new List<BackupInfoDto>();

        if (Directory.Exists(carpetaBackups))
        {
            var archivos = new DirectoryInfo(carpetaBackups)
                .GetFiles("*.db")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            foreach (var archivo in archivos)
            {
                lista.Add(new BackupInfoDto
                {
                    NombreArchivo = archivo.Name,
                    RutaCompleta = archivo.FullName,
                    TamañoBytes = archivo.Length,
                    FechaCreacion = archivo.CreationTime,
                    EsAutomaticoCierre = archivo.Name.Contains("cierre_caja", StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        return Task.FromResult<IReadOnlyList<BackupInfoDto>>(lista);
    }

    public async Task<bool> RestaurarBackupAsync(string rutaArchivoBackup, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaArchivoBackup);

        if (!File.Exists(rutaArchivoBackup))
        {
            throw new FileNotFoundException("El archivo de respaldo seleccionado no existe.", rutaArchivoBackup);
        }

        // 1. Validar integridad de la base a restaurar
        var connStringBackup = $"Data Source={rutaArchivoBackup};";
        using (var connectionValidacion = new SqliteConnection(connStringBackup))
        {
            await connectionValidacion.OpenAsync(cancellationToken);
            using var cmd = connectionValidacion.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var resultado = (string?)await cmd.ExecuteScalarAsync(cancellationToken);
            if (!string.Equals(resultado, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"El archivo de respaldo está dañado o no es válido (Resultado: {resultado}).");
            }
        }

        // 2. Obtener la ruta de la base de datos en uso actual
        var connActual = _context.Database.GetDbConnection();
        var connStringBuilder = new SqliteConnectionStringBuilder(connActual.ConnectionString);
        var rutaDbActual = connStringBuilder.DataSource;

        if (!Path.IsPathRooted(rutaDbActual))
        {
            rutaDbActual = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rutaDbActual);
        }

        // 3. Crear copia de resguardo preventiva de la base actual antes de pisarla
        if (File.Exists(rutaDbActual))
        {
            var carpetaBackups = ObtenerCarpetaBackupsPredeterminada();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var rutaResguardo = Path.Combine(carpetaBackups, $"pre_restore_backup_{timestamp}.db");
            var rutaSqliteResguardo = rutaResguardo.Replace('\\', '/').Replace("'", "''");
#pragma warning disable EF1002
            await _context.Database.ExecuteSqlRawAsync($"VACUUM INTO '{rutaSqliteResguardo}';", cancellationToken);
#pragma warning restore EF1002
        }

        // 4. Liberar conexiones activas de SQLite para poder sobreescribir el archivo
        SqliteConnection.ClearAllPools();

        // 5. Sobreescribir archivo de base de datos
        File.Copy(rutaArchivoBackup, rutaDbActual, overwrite: true);

        // Limpiar archivos WAL y SHM viejos para forzar recreación limpia
        var walFile = $"{rutaDbActual}-wal";
        var shmFile = $"{rutaDbActual}-shm";
        if (File.Exists(walFile)) File.Delete(walFile);
        if (File.Exists(shmFile)) File.Delete(shmFile);

        return true;
    }

    private static void RotarBackupsAntiguos(string carpetaBackups, int maxRetenidos)
    {
        try
        {
            var dir = new DirectoryInfo(carpetaBackups);
            if (!dir.Exists) return;

            var archivosAutomaticos = dir.GetFiles("backup_puntodeventa_cierre_caja_*.db")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (archivosAutomaticos.Count > maxRetenidos)
            {
                var paraEliminar = archivosAutomaticos.Skip(maxRetenidos);
                foreach (var archivo in paraEliminar)
                {
                    try
                    {
                        archivo.Delete();
                    }
                    catch
                    {
                        // Continuar si algún archivo está temporalmente en uso
                    }
                }
            }
        }
        catch
        {
            // Ignorar errores en limpieza de rotación
        }
    }
}
