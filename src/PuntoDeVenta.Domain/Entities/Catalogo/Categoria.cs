using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Catalogo;

public class Categoria : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();
}
