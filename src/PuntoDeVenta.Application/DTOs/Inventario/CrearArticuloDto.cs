namespace PuntoDeVenta.Application.DTOs.Inventario;

/// <summary>
/// DTO para la creación de un artículo padre y la generación combinatoria de sus variantes.
/// </summary>
public class CrearArticuloDto
{
    public string CodigoEstilo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Temporada { get; set; }
    public string? Genero { get; set; }

    public Guid CategoriaId { get; set; }
    public Guid MarcaId { get; set; }

    // Precios base que heredan las variantes generadas por defecto
    public decimal PrecioCosto { get; set; }
    public decimal PrecioLista { get; set; }
    public decimal? PrecioOferta { get; set; }
    public bool PermiteDescuentoMedioPago { get; set; } = false;
    public int StockMinimo { get; set; } = 1;

    /// <summary>
    /// Lista de talles para la matriz (ej. ["S", "M", "L"] o ["38", "40", "42"] o ["41", "42", "43"]).
    /// </summary>
    public List<string> Talles { get; set; } = new();

    /// <summary>
    /// Lista de colores para la matriz (ej. ["Negro", "Blanco", "Azul Marino"]).
    /// </summary>
    public List<string> Colores { get; set; } = new();

    /// <summary>
    /// Stock inicial específico por combinación (Talle, Color) -> Cantidad.
    /// Si una combinación no está en el diccionario, se asumirá stock inicial 0 o StockInicialDefecto.
    /// </summary>
    public Dictionary<(string Talle, string Color), int>? StockInicialPorCombinacion { get; set; }

    /// <summary>
    /// Stock inicial por defecto aplicado a cada variante si no se especificó en el diccionario.
    /// </summary>
    public int StockInicialDefecto { get; set; } = 0;

    /// <summary>
    /// Si es true y StockInicialPorCombinacion está definido, solo se crean las variantes presentes en el diccionario,
    /// evitando crear combinaciones con stock 0 no adquiridas por el comerciante.
    /// </summary>
    public bool SoloCombinacionesEspecificadas { get; set; } = false;
}
