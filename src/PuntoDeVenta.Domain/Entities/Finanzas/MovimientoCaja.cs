using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Finanzas;

/// <summary>
/// Registro detallado de cada entrada o salida de dinero durante la sesión de caja,
/// discriminando si fue en efectivo de mostrador, transferencia bancaria o tarjeta.
/// </summary>
public class MovimientoCaja : BaseEntity
{
    public Guid TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public TipoMovimientoCaja Tipo { get; set; }
    public ConceptoMovimientoCaja Concepto { get; set; }
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;

    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ReferenciaComprobante { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
