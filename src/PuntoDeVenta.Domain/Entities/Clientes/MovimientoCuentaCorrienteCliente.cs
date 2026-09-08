using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Ventas;

namespace PuntoDeVenta.Domain.Entities.Clientes;

public class MovimientoCuentaCorrienteCliente : BaseEntity
{
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public Guid? VentaId { get; set; }
    public Venta? Venta { get; set; }

    public Guid? TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public TipoMovimientoCuentaCorriente Tipo { get; set; }
    public decimal Monto { get; set; }
    public decimal SaldoPrevio { get; set; }
    public decimal SaldoResultante { get; set; }

    public CanalDinero CanalCobro { get; set; } = CanalDinero.Efectivo;
    public string Detalle { get; set; } = string.Empty;
    public string? ReferenciaComprobante { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
