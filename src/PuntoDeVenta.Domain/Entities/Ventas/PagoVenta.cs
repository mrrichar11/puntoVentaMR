using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Domain.Entities.Ventas;

/// <summary>
/// Desglose de cada medio de pago que compone una venta (soporte para split payment/pagos combinados).
/// </summary>
public class PagoVenta : BaseEntity
{
    public Guid VentaId { get; set; }
    public Venta? Venta { get; set; }

    public Guid? MetodoPagoId { get; set; }
    public MetodoPago? MetodoPago { get; set; }

    /// <summary>
    /// Canal monetario del pago (Efectivo, Transferencia/QR, Tarjeta de Débito, Tarjeta de Crédito).
    /// </summary>
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;

    /// <summary>
    /// Monto neto imputado a la venta con este medio de pago.
    /// </summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Porcentaje de ajuste comercial aplicado (descuento si negativo, recargo si positivo).
    /// </summary>
    public decimal PorcentajeAjuste { get; set; }

    /// <summary>
    /// Importe en pesos del descuento o recargo correspondiente a este pago.
    /// </summary>
    public decimal MontoAjuste { get; set; }

    /// <summary>
    /// Comisión porcentual cobrada por el banco o procesador de pagos.
    /// </summary>
    public decimal ComisionPorcentual { get; set; }

    /// <summary>
    /// Costo financiero estimado: Monto * ComisionPorcentual.
    /// </summary>
    public decimal MontoComision => Math.Round((Monto * ComisionPorcentual) / 100m, 2);

    /// <summary>
    /// Número de cupón, lote o código de operación para transferencias y tarjetas.
    /// </summary>
    public string? Referencia { get; set; }
}
