namespace PuntoDeVenta.Application.DTOs.Backup;

public class BackupInfoDto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaCompleta { get; set; } = string.Empty;
    public long TamañoBytes { get; set; }
    public string TamañoFormateado => FormatearTamaño(TamañoBytes);
    public DateTime FechaCreacion { get; set; }
    public bool EsAutomaticoCierre { get; set; }

    private static string FormatearTamaño(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
