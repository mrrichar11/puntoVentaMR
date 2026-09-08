namespace PuntoDeVenta.Application.DTOs.Sistema;

public class ActualizacionDto
{
    public string VersionActual { get; set; } = string.Empty;
    public string VersionNueva { get; set; } = string.Empty;
    public bool HayActualizacion { get; set; }
    public string TituloRelease { get; set; } = string.Empty;
    public string NotasVersion { get; set; } = string.Empty;
    public string UrlDescarga { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime FechaPublicacion { get; set; }
    public bool EsObligatoria { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    // Propiedades de conveniencia
    public bool HayActualizacionDisponible => HayActualizacion;
    public string NuevaVersion => VersionNueva;
    public string Titulo => TituloRelease;
    public string NotasCambios => NotasVersion;
}

public class ProgresoDescargaDto
{
    public double Porcentaje { get; set; }
    public long BytesRecibidos { get; set; }
    public long? TotalBytes { get; set; }
    public string MensajeEstado { get; set; } = string.Empty;

    // Propiedades de conveniencia
    public long BytesDescargados => BytesRecibidos;
    public string Estado => MensajeEstado;
}
