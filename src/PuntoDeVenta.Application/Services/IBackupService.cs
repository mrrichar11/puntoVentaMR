using PuntoDeVenta.Application.DTOs.Backup;

namespace PuntoDeVenta.Application.Services;

public interface IBackupService
{
    /// <summary>
    /// Crea una copia de seguridad segura de la base de datos SQLite actual mediante VACUUM INTO.
    /// Si rutaDestino es nula, guarda en la carpeta predeterminada ./Backups/ con nombre con marca temporal.
    /// </summary>
    Task<BackupInfoDto> CrearBackupAsync(string? rutaDestino = null, bool esAutomaticoCierre = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la lista de respaldos existentes en la carpeta de Backups ordenada del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<BackupInfoDto>> ObtenerHistorialBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura la base de datos a partir de un archivo .db seleccionado.
    /// Genera automáticamente una copia de resguardo preventiva de la base actual antes de reemplazar.
    /// </summary>
    Task<bool> RestaurarBackupAsync(string rutaArchivoBackup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna la ruta absoluta de la carpeta predeterminada donde se guardan los respaldos.
    /// </summary>
    string ObtenerCarpetaBackupsPredeterminada();
}
