using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Catalogo;

/// <summary>
/// Variante de Artículo (Hijo): Unidad física transaccionable en el inventario real.
/// Contiene talle, color, SKU, código de barras, stock y políticas de precios y ofertas.
/// </summary>
public class VarianteArticulo : BaseEntity
{
    public Guid ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    /// <summary>
    /// Código único SKU (ej. "ZAP-AIR-01-41-NEG").
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Código de barras legible por lector óptico (EAN-13, CODE128 o interno).
    /// </summary>
    public string? CodigoBarras { get; set; }

    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Costo de adquisición actual de la variante.
    /// </summary>
    public decimal PrecioCosto { get; set; }

    /// <summary>
    /// Precio regular base de lista al público (contempla márgenes estándar y comisiones).
    /// </summary>
    public decimal PrecioLista { get; set; }

    /// <summary>
    /// Precio promocional / liquidación (Sale). Si es nulo o cero, el ítem se encuentra a precio regular.
    /// </summary>
    public decimal? PrecioOferta { get; set; }

    /// <summary>
    /// Define si un ítem que está en oferta puede o no acumular descuentos adicionales
    /// derivados del medio de pago (ej. 10% adicional por pago en efectivo).
    /// Si el ítem no está en oferta, siempre es elegible para el descuento del medio de pago.
    /// </summary>
    public bool PermiteDescuentoMedioPago { get; set; } = false;

    public int StockActual { get; set; }
    public int StockMinimo { get; set; } = 1;
    public string? Ubicacion { get; set; }

    // --- Métodos y Propiedades de Negocio ---

    /// <summary>
    /// Determina si la variante se encuentra activa en liquidación/oferta.
    /// </summary>
    public bool EsEnOferta => PrecioOferta.HasValue && PrecioOferta.Value > 0 && PrecioOferta.Value < PrecioLista;

    /// <summary>
    /// Retorna el precio base previo a la aplicación de medios de pago.
    /// </summary>
    public decimal PrecioBaseVenta => EsEnOferta ? PrecioOferta!.Value : PrecioLista;

    /// <summary>
    /// Evalúa si el ítem es elegible para recibir descuento por medio de pago (ej. efectivo).
    /// Si es precio regular: SIEMPRE es elegible.
    /// Si está en oferta: solo es elegible si PermiteDescuentoMedioPago es true.
    /// </summary>
    public bool EsElegibleParaDescuentoMedioPago => !EsEnOferta || PermiteDescuentoMedioPago;

    /// <summary>
    /// Margen bruto unitario estimado sobre el precio base actual.
    /// </summary>
    public decimal MargenBrutoUnitarioEstimado => PrecioBaseVenta - PrecioCosto;

    /// <summary>
    /// Porcentaje de margen bruto sobre el precio base actual.
    /// </summary>
    public decimal PorcentajeMargenBrutoEstimado => PrecioBaseVenta > 0
        ? Math.Round((MargenBrutoUnitarioEstimado / PrecioBaseVenta) * 100m, 2)
        : 0m;
}
