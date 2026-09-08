using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Catalogo;

/// <summary>
/// Artículo Base (Padre): Define la identidad conceptual de la prenda o calzado
/// (marca, modelo/estilo, categoría, género, temporada) sin stock directo.
/// </summary>
public class Articulo : BaseEntity
{
    public string CodigoEstilo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Temporada { get; set; }
    public string? Genero { get; set; }

    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public Guid MarcaId { get; set; }
    public Marca? Marca { get; set; }

    public ICollection<VarianteArticulo> Variantes { get; set; } = new List<VarianteArticulo>();
}
