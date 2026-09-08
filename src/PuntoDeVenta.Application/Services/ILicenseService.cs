using PuntoDeVenta.Application.DTOs.Seguridad;
using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Application.Services;

public interface ILicenseService
{
    Task<EstadoLicenciaDto> ValidarLicenciaAsync(CancellationToken cancellationToken = default);
    Task<ResultadoActivacionDto> ActivarLicenciaAsync(string claveActivacion, CancellationToken cancellationToken = default);
    string GenerarClaveActivacion(string codigoInstalacion, DateTime fechaExpiracion, TipoPlanLicencia plan = TipoPlanLicencia.Estandar);
    string ObtenerCodigoInstalacion();
}
