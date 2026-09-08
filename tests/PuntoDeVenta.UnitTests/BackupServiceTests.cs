using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class BackupServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly AppDbContext _context;
    private readonly BackupService _backupService;
    private readonly List<string> _tempFilesCreated = new();

    public BackupServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"puntodeventa_test_{Guid.NewGuid():N}.db");
        _tempFilesCreated.Add(_tempDbPath);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_tempDbPath};")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Agregar datos de prueba
        _context.Categorias.Add(new Categoria { Nombre = "Calzado Deportivo" });
        _context.SaveChanges();

        _backupService = new BackupService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        SqliteConnection.ClearAllPools();

        foreach (var file in _tempFilesCreated)
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
                var wal = $"{file}-wal";
                var shm = $"{file}-shm";
                if (File.Exists(wal)) File.Delete(wal);
                if (File.Exists(shm)) File.Delete(shm);
            }
            catch
            {
                // Ignorar si el sistema aún no liberó el bloqueo
            }
        }
    }

    [Fact]
    public async Task CrearBackupAsync_ConRutaPersonalizada_DebeCrearArchivoSqliteValido()
    {
        // Arrange
        var backupPath = Path.Combine(Path.GetTempPath(), $"backup_custom_{Guid.NewGuid():N}.db");
        _tempFilesCreated.Add(backupPath);

        // Act
        var result = await _backupService.CrearBackupAsync(backupPath, esAutomaticoCierre: false);

        // Assert
        result.Should().NotBeNull();
        result.RutaCompleta.Should().Be(Path.GetFullPath(backupPath));
        File.Exists(backupPath).Should().BeTrue();
        result.TamañoBytes.Should().BeGreaterThan(0);

        // Validar que el archivo de respaldo es un SQLite íntegro y contiene la categoría
        using var conn = new SqliteConnection($"Data Source={backupPath};");
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";
        var integrity = (string?)await cmd.ExecuteScalarAsync();
        integrity.Should().Be("ok");

        using var cmdCount = conn.CreateCommand();
        cmdCount.CommandText = "SELECT COUNT(*) FROM Categorias;";
        var count = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
        count.Should().Be(1);
    }

    [Fact]
    public async Task CrearBackupAsync_SinRuta_DebeGuardarEnCarpetaBackupsPredeterminada()
    {
        // Act
        var result = await _backupService.CrearBackupAsync(rutaDestino: null, esAutomaticoCierre: true);

        // Assert
        result.Should().NotBeNull();
        result.EsAutomaticoCierre.Should().BeTrue();
        result.NombreArchivo.Should().Contain("cierre_caja");
        File.Exists(result.RutaCompleta).Should().BeTrue();

        _tempFilesCreated.Add(result.RutaCompleta);

        // Verificar que aparezca en el historial
        var historial = await _backupService.ObtenerHistorialBackupsAsync();
        historial.Should().Contain(b => b.NombreArchivo == result.NombreArchivo);
    }

    [Fact]
    public async Task RestaurarBackupAsync_ConArchivoValido_DebeRestaurarCorrectamente()
    {
        // Arrange: Crear un backup que tenga 2 categorías
        _context.Categorias.Add(new Categoria { Nombre = "Indumentaria Femenina" });
        await _context.SaveChangesAsync();

        var backupPath = Path.Combine(Path.GetTempPath(), $"backup_restore_{Guid.NewGuid():N}.db");
        _tempFilesCreated.Add(backupPath);
        await _backupService.CrearBackupAsync(backupPath);

        // Modificar la base actual borrando todo
        _context.Categorias.RemoveRange(_context.Categorias);
        await _context.SaveChangesAsync();
        _context.Categorias.Count().Should().Be(0);

        // Act: Restaurar desde el backup
        var restaurado = await _backupService.RestaurarBackupAsync(backupPath);

        // Assert
        restaurado.Should().BeTrue();

        using var connVerif = new SqliteConnection($"Data Source={_tempDbPath};");
        await connVerif.OpenAsync();
        using var cmd = connVerif.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Categorias;";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        count.Should().Be(2);
    }
}
