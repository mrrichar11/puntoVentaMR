using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Catalogo;

namespace PuntoDeVenta.Domain.Entities.Ventas;

/// <summary>
/// Detalle de Línea de Venta con congelamiento inmutable de precios, descuentos y costos históricos.
/// Permite auditar el Margen Bruto Real de cada producto vendido sin depender de cambios futuros en catálogo.
/// </summary>
public class LineaVenta : BaseEntity
{
    public Guid VentaId { get; set; }
    public Venta? Venta { get; set; }

    public Guid VarianteId { get; set; }
    public VarianteArticulo? Variante { get; set; }

    // Snapshot descriptivo inmutable
    public string DescripcionArticulo { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    /// <summary>
    /// Precio de lista regular unitario vigente al momento de la venta.
    /// </summary>
    public decimal PrecioListaUnitario { get; set; }

    /// <summary>
    /// Descuento unitario por liquidación/oferta (PrecioListaUnitario - PrecioOferta).
    /// </summary>
    public decimal DescuentoOfertaUnitario { get; set; }

    /// <summary>
    /// Descuento unitario adicional derivado del medio de pago (ej. 10% en efectivo sobre ítem elegible).
    /// </summary>
    public decimal DescuentoMedioPagoUnitario { get; set; }

    /// <summary>
    /// Recargo unitario derivado del medio de pago (ej. financiación).
    /// </summary>
    public decimal RecargoMedioPagoUnitario { get; set; }

    /// <summary>
    /// Precio unitario efectivamente cobrado al cliente final tras todos los ajustes.
    /// </summary>
    public decimal PrecioFinalCobrado { get; set; }

    /// <summary>
    /// Costo de reposición/adquisición unitario congelado al instante de la venta.
    /// </summary>
    public decimal CostoUnitarioHistorico { get; set; }

    // --- Totales y Auditoría de Margen ---

    public decimal SubtotalCobrado => Math.Round(PrecioFinalCobrado * Cantidad, 2);
    public decimal CostoTotalHistorico => Math.Round(CostoUnitarioHistorico * Cantidad, 2);
    public decimal MargenBrutoReal => SubtotalCobrado - CostoTotalHistorico;
    public decimal PorcentajeMargenBrutoReal => SubtotalCobrado > 0
        ? Math.Round((MargenBrutoReal / SubtotalCobrado) * 100m, 2)
        : 0m;
}
