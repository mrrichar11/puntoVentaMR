namespace PuntoDeVenta.Application.DTOs.Inventario;

public class ActualizarVarianteDto
{
    public Guid VarianteId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? NombreArticulo { get; set; }
    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioLista { get; set; }
    public decimal? PrecioOferta { get; set; }
    public bool PermiteDescuentoMedioPago { get; set; }
    public int StockMinimo { get; set; }
    public string? Ubicacion { get; set; }
}

public class ActualizarPreciosMasivosDto
{
    public Guid ArticuloId { get; set; }
    public decimal? NuevoPrecioCosto { get; set; }
    public decimal? NuevoPrecioLista { get; set; }
    public decimal? NuevoPrecioOferta { get; set; }
    public decimal? PorcentajeAumentoLista { get; set; }
    public decimal? PorcentajeAumentoCosto { get; set; }
    public bool? PermiteDescuentoMedioPago { get; set; }
}
