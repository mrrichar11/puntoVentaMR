using PuntoDeVenta.Application.DTOs.Sistema;

namespace PuntoDeVenta.Application.Services;

public interface IUpdateService
{
    string ObtenerVersionActual();
    Task<ActualizacionDto> VerificarActualizacionesAsync(string owner, string repo, CancellationToken ct = default);
    Task<string> DescargarActualizacionAsync(ActualizacionDto info, IProgress<ProgresoDescargaDto>? progreso = null, CancellationToken ct = default);
    void IniciarInstalacion(string rutaZipDescargado);
}
