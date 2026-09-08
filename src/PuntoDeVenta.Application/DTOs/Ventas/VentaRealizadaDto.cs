using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.DTOs.Ventas;

public class LineaVentaResumenDto
{
    public string SKU { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioListaUnitario { get; set; }
    public decimal DescuentoOfertaUnitario { get; set; }
    public decimal DescuentoMedioPagoUnitario { get; set; }
    public decimal RecargoMedioPagoUnitario { get; set; }
    public decimal PrecioFinalCobrado { get; set; }
    public decimal CostoUnitarioHistorico { get; set; }
    public decimal SubtotalCobrado { get; set; }
    public decimal MargenBrutoReal { get; set; }
}

public class PagoResumenDto
{
    public CanalDinero Canal { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}

public class VentaRealizadaDto
{
    public Guid VentaId { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? ClienteNombre { get; set; }

    public decimal SubtotalLista { get; set; }
    public decimal TotalDescuentoOferta { get; set; }
    public decimal TotalDescuentoMedioPago { get; set; }
    public decimal TotalRecargoMedioPago { get; set; }
    public decimal TotalFinalCobrado { get; set; }
    public decimal CostoTotalHistorico { get; set; }
    public decimal MargenBrutoReal { get; set; }
    public decimal PorcentajeMargenBrutoReal { get; set; }

    public List<LineaVentaResumenDto> Lineas { get; set; } = new();
    public List<PagoResumenDto> Pagos { get; set; } = new();
}
