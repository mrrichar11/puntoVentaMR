using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Catalogo;

namespace PuntoDeVenta.Domain.Entities.Proveedores;

public class LineaCompraProveedor : BaseEntity
{
    public Guid CompraProveedorId { get; set; }
    public CompraProveedor? CompraProveedor { get; set; }

    public Guid VarianteId { get; set; }
    public VarianteArticulo? Variante { get; set; }

    public int Cantidad { get; set; }
    public decimal CostoUnitarioCompra { get; set; }
    public decimal Subtotal => Cantidad * CostoUnitarioCompra;
}
