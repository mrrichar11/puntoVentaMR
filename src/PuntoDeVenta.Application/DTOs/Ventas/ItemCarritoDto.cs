using PuntoDeVenta.Application.DTOs.Inventario;

namespace PuntoDeVenta.Application.DTOs.Ventas;

public class ItemCarritoDto
{
    public Guid VarianteId { get; set; }
    public int Cantidad { get; set; } = 1;
    public VarianteArticuloDto? Variante { get; set; }

    // Propiedades para Venta Manual / Ítem Rápido sin Stock
    public bool EsVentaManual { get; set; }
    public string? DescripcionManual { get; set; }
    public decimal? PrecioCostoManual { get; set; }
    public decimal? PrecioVentaManual { get; set; }
    public string? TalleManual { get; set; }
    public string? ColorManual { get; set; }
}
