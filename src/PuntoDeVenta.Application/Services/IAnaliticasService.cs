using PuntoDeVenta.Application.DTOs.Analiticas;

namespace PuntoDeVenta.Application.Services;

public interface IAnaliticasService
{
    Task<ReporteAnaliticasDto> ObtenerReporteAsync(PeriodoAnalitica periodo, DateTime? fechaInicio = null, DateTime? fechaFin = null, CancellationToken cancellationToken = default);
}
