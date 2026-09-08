using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Catalogo;

public class Marca : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();
}
