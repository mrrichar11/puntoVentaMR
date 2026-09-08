using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Proveedores;

public class Proveedor : BaseEntity
{
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreContacto { get; set; }
    public string? Cuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }
    public int PlazoPagoDiasDefecto { get; set; } = 30;

    public decimal SaldoDeudorActual { get; set; } = 0m;

    public ICollection<CompraProveedor> Compras { get; set; } = new List<CompraProveedor>();
}
