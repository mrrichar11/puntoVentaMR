using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Seguridad;

public enum TipoPlanLicencia
{
    Estandar = 1,
    Premium = 2
}

/// <summary>
/// Registro persistente del licenciamiento del sistema, control de expiración y protección anti-adulteración.
/// </summary>
public class LicenciaSistema : BaseEntity
{
    /// <summary>
    /// Identificador único del equipo o instalación (Machine Fingerprint).
    /// </summary>
    public string CodigoInstalacion { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del comercio o cliente asociado.
    /// </summary>
    public string Comercio { get; set; } = string.Empty;

    /// <summary>
    /// Nivel de plan contratado (Estándar o Premium con IA).
    /// </summary>
    public TipoPlanLicencia TipoPlan { get; set; } = TipoPlanLicencia.Estandar;

    /// <summary>
    /// Indica si el plan activo incluye acceso al módulo del Asistente de IA.
    /// </summary>
    public bool TieneModuloIA => TipoPlan == TipoPlanLicencia.Premium;

    /// <summary>
    /// Fecha de expiración de la licencia (UTC).
    /// </summary>
    public DateTime FechaExpiracion { get; set; }

    /// <summary>
    /// Última clave de activación válida aplicada.
    /// </summary>
    public string ClaveActivacion { get; set; } = string.Empty;

    /// <summary>
    /// Última fecha/hora registrada de uso del sistema. Previene atrasar el reloj de la PC.
    /// </summary>
    public DateTime UltimaFechaUso { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Si la licencia está habilitada activamente.
    /// </summary>
    public bool EstaActiva { get; set; } = true;
}
