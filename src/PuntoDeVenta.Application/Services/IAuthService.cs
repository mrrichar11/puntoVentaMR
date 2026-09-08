using PuntoDeVenta.Application.DTOs.Seguridad;
using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Application.Services;

public interface IAuthService
{
    UsuarioDto? UsuarioActual { get; set; }

    Task<UsuarioDto?> AutenticarAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioDto>> ObtenerUsuariosAsync(CancellationToken cancellationToken = default);
    Task<UsuarioDto> CrearUsuarioAsync(string username, string nombreCompleto, string password, RolUsuario rol, CancellationToken cancellationToken = default);
    Task CambiarContraseñaAsync(Guid usuarioId, string passwordActual, string nuevoPassword, CancellationToken cancellationToken = default);
    void CerrarSesion();
}
