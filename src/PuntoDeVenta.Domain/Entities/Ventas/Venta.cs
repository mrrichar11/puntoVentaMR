using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Domain.Entities.Ventas;

/// <summary>
/// Cabecera de Venta con auditoría financiera, congelamiento inmutable de costos y soporte de pagos combinados.
/// </summary>
public class Venta : BaseEntity
{
    /// <summary>
    /// Número correlativo del ticket o factura (ej. "T-0001-00000001").
    /// </summary>
    public string NumeroComprobante { get; set; } = string.Empty;

    public Guid TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Guid? ClienteId { get; set; }
    public PuntoDeVenta.Domain.Entities.Clientes.Cliente? Cliente { get; set; }

    public string? ClienteNombre { get; set; }
    public string? ClienteDocumento { get; set; }

    public CanalVenta CanalVenta { get; set; } = CanalVenta.Mostrador;
    public string? NroPedidoWeb { get; set; }
    public string? Vendedora { get; set; }

    // --- Resumen Económico del Ticket ---
    public decimal SubtotalLista { get; set; }
    public decimal TotalDescuentoOferta { get; set; }
    public decimal TotalDescuentoMedioPago { get; set; }
    public decimal TotalRecargoMedioPago { get; set; }

    /// <summary>
    /// Importe total cobrado al cliente final.
    /// </summary>
    public decimal TotalFinalCobrado { get; set; }

    /// <summary>
    /// Costo total de adquisición de la mercadería vendida congelado inmutablemente.
    /// </summary>
    public decimal CostoTotalHistorico { get; set; }

    /// <summary>
    /// Margen bruto real en pesos: TotalFinalCobrado - CostoTotalHistorico.
    /// </summary>
    public decimal MargenBrutoReal => TotalFinalCobrado - CostoTotalHistorico;

    /// <summary>
    /// Porcentaje de margen bruto real sobre el importe cobrado.
    /// </summary>
    public decimal PorcentajeMargenBrutoReal => TotalFinalCobrado > 0
        ? Math.Round((MargenBrutoReal / TotalFinalCobrado) * 100m, 2)
        : 0m;

    public ICollection<LineaVenta> Lineas { get; set; } = new List<LineaVenta>();
    public ICollection<PagoVenta> Pagos { get; set; } = new List<PagoVenta>();
}
