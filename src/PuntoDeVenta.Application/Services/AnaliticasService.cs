using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Analiticas;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Ventas;

namespace PuntoDeVenta.Application.Services;

public class AnaliticasService : IAnaliticasService
{
    private readonly IUnitOfWork _unitOfWork;

    public AnaliticasService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReporteAnaliticasDto> ObtenerReporteAsync(
        PeriodoAnalitica periodo,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        DateTime desde;
        DateTime hasta;

        switch (periodo)
        {
            case PeriodoAnalitica.Hoy:
                desde = ahora.Date;
                hasta = desde.AddDays(1);
                break;

            case PeriodoAnalitica.SemanaActual:
                // Inicio de la semana (Lunes)
                var diff = (7 + (ahora.DayOfWeek - DayOfWeek.Monday)) % 7;
                desde = ahora.Date.AddDays(-1 * diff);
                hasta = desde.AddDays(7);
                break;

            case PeriodoAnalitica.MesActual:
                desde = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                hasta = desde.AddMonths(1);
                break;

            case PeriodoAnalitica.AñoActual:
                desde = new DateTime(ahora.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                hasta = desde.AddYears(1);
                break;

            case PeriodoAnalitica.Personalizado:
            default:
                desde = (fechaInicio ?? ahora.Date.AddDays(-30)).ToUniversalTime();
                hasta = (fechaFin ?? ahora.Date.AddDays(1)).ToUniversalTime();
                break;
        }

        var ventas = await _unitOfWork.Ventas.FindAsync(v =>
            v.Activo &&
            v.Fecha >= desde &&
            v.Fecha < hasta,
            cancellationToken);

        var reporte = new ReporteAnaliticasDto
        {
            Periodo = periodo,
            FechaInicio = desde,
            FechaFin = hasta,

            TotalFacturado = ventas.Sum(v => v.TotalFinalCobrado),
            CostoTotalMercaderia = ventas.Sum(v => v.CostoTotalHistorico),
            GananciaBrutaReal = ventas.Sum(v => v.MargenBrutoReal),
            CantidadVentas = ventas.Count,
            CantidadPrendasVendidas = ventas.SelectMany(v => v.Lineas).Sum(l => l.Cantidad),

            TotalVentasMostrador = ventas.Where(v => v.CanalVenta == CanalVenta.Mostrador).Sum(v => v.TotalFinalCobrado),
            CantidadVentasMostrador = ventas.Count(v => v.CanalVenta == CanalVenta.Mostrador),
            TotalVentasWeb = ventas.Where(v => v.CanalVenta != CanalVenta.Mostrador).Sum(v => v.TotalFinalCobrado),
            CantidadVentasWeb = ventas.Count(v => v.CanalVenta != CanalVenta.Mostrador),

            TotalEfectivo = ventas.SelectMany(v => v.Pagos).Where(p => p.Canal == CanalDinero.Efectivo).Sum(p => p.Monto),
            TotalTarjetaDebito = ventas.SelectMany(v => v.Pagos).Where(p => p.Canal == CanalDinero.TarjetaDebito).Sum(p => p.Monto),
            TotalTarjetaCredito = ventas.SelectMany(v => v.Pagos).Where(p => p.Canal == CanalDinero.TarjetaCredito).Sum(p => p.Monto),
            TotalTransferencia = ventas.SelectMany(v => v.Pagos).Where(p => p.Canal == CanalDinero.TransferenciaQR).Sum(p => p.Monto),
            TotalCuentaCorriente = ventas.SelectMany(v => v.Pagos).Where(p => p.Canal == CanalDinero.CuentaCorriente).Sum(p => p.Monto)
        };

        // Ranking de Productos Más Vendidos
        var lineasAgrupadas = ventas.SelectMany(v => v.Lineas)
            .GroupBy(l => new { l.DescripcionArticulo, l.SKU })
            .Select(g => new RankingProductoDto
            {
                Descripcion = g.Key.DescripcionArticulo,
                SKU = g.Key.SKU,
                CantidadVendida = g.Sum(x => x.Cantidad),
                TotalRecaudado = g.Sum(x => x.SubtotalCobrado),
                GananciaBruta = g.Sum(x => x.MargenBrutoReal)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .ThenByDescending(x => x.TotalRecaudado)
            .Take(10)
            .ToList();

        reporte.TopProductos = lineasAgrupadas;

        // Historial Detallado
        reporte.VentasDetalle = ventas
            .OrderByDescending(v => v.Fecha)
            .Select(v => new VentaHistorialItemDto
            {
                VentaId = v.Id,
                NumeroComprobante = v.NumeroComprobante,
                Fecha = v.Fecha.ToLocalTime(),
                ClienteNombre = string.IsNullOrWhiteSpace(v.ClienteNombre) ? "Consumidor Final" : v.ClienteNombre,
                CanalVenta = v.CanalVenta.ToString(),
                NroPedidoWeb = v.NroPedidoWeb,
                TotalCobrado = v.TotalFinalCobrado,
                GananciaBruta = v.MargenBrutoReal,
                MediosPago = string.Join(", ", v.Pagos.Select(p => $"{p.Canal} ()"))
            })
            .ToList();

        return reporte;
    }
}
