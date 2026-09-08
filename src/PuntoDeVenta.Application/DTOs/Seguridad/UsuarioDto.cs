using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Application.DTOs.Seguridad;

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoIngreso { get; set; }
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
