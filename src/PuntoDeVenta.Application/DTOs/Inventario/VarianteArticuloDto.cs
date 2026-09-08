namespace PuntoDeVenta.Application.DTOs.Inventario;

public class VarianteArticuloDto
{
    public Guid Id { get; set; }
    public Guid ArticuloId { get; set; }
    public string CodigoEstilo { get; set; } = string.Empty;
    public string NombreArticulo { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string MarcaNombre { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public decimal PrecioCosto { get; set; }
    public decimal PrecioLista { get; set; }
    public decimal? PrecioOferta { get; set; }
    public bool EsEnOferta { get; set; }
    public bool PermiteDescuentoMedioPago { get; set; }
    public decimal PrecioBaseVenta { get; set; }

    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public string? Ubicacion { get; set; }
}
