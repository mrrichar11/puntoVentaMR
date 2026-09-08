using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Domain.Entities.Proveedores;

public class CompraProveedor : BaseEntity
{
    public string NumeroComprobante { get; set; } = string.Empty;
    public Guid ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public Guid? TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public DateTime? FechaVencimientoPlazo { get; set; }

    public CondicionCompraProveedor Condicion { get; set; } = CondicionCompraProveedor.CuentaCorrienteAPlazo;
    public EstadoCompraProveedor Estado { get; set; } = EstadoCompraProveedor.Recibida;

    public decimal TotalCompra { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente => Math.Max(0m, TotalCompra - TotalPagado);

    public string? Observaciones { get; set; }

    public ICollection<LineaCompraProveedor> Lineas { get; set; } = new List<LineaCompraProveedor>();
    public ICollection<PagoCompraProveedor> Pagos { get; set; } = new List<PagoCompraProveedor>();
}
