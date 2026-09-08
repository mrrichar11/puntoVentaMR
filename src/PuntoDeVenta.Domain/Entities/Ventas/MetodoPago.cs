using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Ventas;

/// <summary>
/// Método de pago con reglas de recargo/descuento comercial y comisiones.
/// </summary>
public class MetodoPago : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Porcentaje de ajuste sobre el precio base del ítem elegible.
    /// Negativo para bonificaciones/descuentos (ej. -10.0m para 10% en efectivo).
    /// Positivo para recargos comerciales (ej. +15.0m en cuotas).
    /// </summary>
    public decimal PorcentajeAjuste { get; set; }

    /// <summary>
    /// Comisión porcentual cobrada por el procesador / banco (ej. 1.8% en débito, 3.5% en crédito).
    /// Utilizada para auditar el margen neto real después de comisiones financieras.
    /// </summary>
    public decimal ComisionPorcentual { get; set; }

    /// <summary>
    /// Días para acreditación real en la cuenta comercial.
    /// </summary>
    public int PlazoAcreditacionDias { get; set; } = 0;

    /// <summary>
    /// Si true, obliga al cajero a registrar número de cupón/lote/autorización o comprobante.
    /// </summary>
    public bool RequiereReferencia { get; set; }
}
