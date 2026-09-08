using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Domain.Entities.Proveedores;

public class PagoCompraProveedor : BaseEntity
{
    public Guid CompraProveedorId { get; set; }
    public CompraProveedor? CompraProveedor { get; set; }

    public Guid? TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Monto { get; set; }
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;
    public string? ReferenciaComprobante { get; set; }
    public string? Notas { get; set; }
}
