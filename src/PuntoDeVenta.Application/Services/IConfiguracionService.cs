using PuntoDeVenta.Application.DTOs.Configuracion;

namespace PuntoDeVenta.Application.Services;

public interface IConfiguracionService
{
    Task<ConfiguracionNegocioDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default);
    Task GuardarConfiguracionAsync(ConfiguracionNegocioDto dto, CancellationToken cancellationToken = default);
    Task<EstadoRetirosDueñoDto> ObtenerEstadoRetirosDueñoMesAsync(CancellationToken cancellationToken = default);
}
