using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Application.DTOs.Seguridad;

public class EstadoLicenciaDto
{
    public string CodigoInstalacion { get; set; } = string.Empty;
    public string Comercio { get; set; } = string.Empty;
    public TipoPlanLicencia TipoPlan { get; set; } = TipoPlanLicencia.Estandar;
    public bool TieneModuloIA { get; set; }
    public string NombrePlan => TipoPlan == TipoPlanLicencia.Premium ? "Plan Premium (con IA)" : "Plan Estándar";
    public DateTime FechaExpiracion { get; set; }
    public int DiasRestantes { get; set; }
    public bool EsValida { get; set; }
    public bool EstaPorVencer { get; set; }
    public bool RelojAdulterado { get; set; }
    public string MensajeEstado { get; set; } = string.Empty;
}

public class ResultadoActivacionDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? NuevaFechaExpiracion { get; set; }
    public TipoPlanLicencia? TipoPlan { get; set; }
}
