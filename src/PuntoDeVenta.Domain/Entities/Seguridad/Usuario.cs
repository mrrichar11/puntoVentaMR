using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Seguridad;

/// <summary>
/// Usuario del sistema POS con credenciales criptográficas y rol asignado.
/// </summary>
public class Usuario : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Cajero;
    public DateTime? UltimoIngreso { get; set; }
}
