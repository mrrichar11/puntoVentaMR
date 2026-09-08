namespace PuntoDeVenta.Application.DTOs.Analiticas;

public enum PeriodoAnalitica
{
    Hoy = 1,
    SemanaActual = 2,
    MesActual = 3,
    AñoActual = 4,
    Personalizado = 5
}

public class ReporteAnaliticasDto
{
    public PeriodoAnalitica Periodo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Resumen Ejecutivo (KPIs)
    public decimal TotalFacturado { get; set; }
    public decimal CostoTotalMercaderia { get; set; }
    public decimal GananciaBrutaReal { get; set; }
    public decimal MargenBrutoPorcentual => TotalFacturado > 0 ? Math.Round((GananciaBrutaReal / TotalFacturado) * 100m, 2) : 0m;

    public int CantidadVentas { get; set; }
    public decimal TicketPromedio => CantidadVentas > 0 ? Math.Round(TotalFacturado / CantidadVentas, 2) : 0m;
    public int CantidadPrendasVendidas { get; set; }

    // Ventas por Canal
    public decimal TotalVentasMostrador { get; set; }
    public int CantidadVentasMostrador { get; set; }
    public decimal TotalVentasWeb { get; set; }
    public int CantidadVentasWeb { get; set; }

    // Desglose por Medio de Pago
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjetaDebito { get; set; }
    public decimal TotalTarjetaCredito { get; set; }
    public decimal TotalTransferencia { get; set; }
    public decimal TotalCuentaCorriente { get; set; }

    // Ranking de Artículos Más Vendidos
    public List<RankingProductoDto> TopProductos { get; set; } = new();

    // Detalle de Ventas del Período
    public List<VentaHistorialItemDto> VentasDetalle { get; set; } = new();
}

public class RankingProductoDto
{
    public string Descripcion { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CantidadVendida { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal GananciaBruta { get; set; }
}

public class VentaHistorialItemDto
{
    public Guid VentaId { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string CanalVenta { get; set; } = string.Empty;
    public string? NroPedidoWeb { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal GananciaBruta { get; set; }
    public string MediosPago { get; set; } = string.Empty;
}
