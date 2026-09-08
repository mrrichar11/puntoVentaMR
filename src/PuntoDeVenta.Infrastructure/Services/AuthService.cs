using System.Security.Cryptography;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Seguridad;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    private readonly IUnitOfWork _unitOfWork;

    public UsuarioDto? UsuarioActual { get; set; }

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UsuarioDto?> AutenticarAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var usuarios = await _unitOfWork.Usuarios.FindAsync(u => u.Activo && u.Username.ToLower() == username.ToLower(), cancellationToken);
        var usuario = usuarios.FirstOrDefault();

        if (usuario == null)
        {
            return null;
        }

        if (!VerificarPassword(request.Password, usuario.PasswordHash, usuario.PasswordSalt))
        {
            return null;
        }

        usuario.UltimoIngreso = DateTime.UtcNow;
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(usuario);
        UsuarioActual = dto;
        return dto;
    }

    public async Task<IReadOnlyList<UsuarioDto>> ObtenerUsuariosAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await _unitOfWork.Usuarios.GetAllAsync(cancellationToken);
        return usuarios.Where(u => u.Activo).OrderBy(u => u.Username).Select(MapToDto).ToList();
    }

    public async Task<UsuarioDto> CrearUsuarioAsync(string username, string nombreCompleto, string password, RolUsuario rol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("El nombre de usuario no puede estar vacío.", nameof(username));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        var existentes = await _unitOfWork.Usuarios.FindAsync(u => u.Username.ToLower() == username.Trim().ToLower(), cancellationToken);
        if (existentes.Any())
        {
            throw new InvalidOperationException($"El usuario '{username}' ya existe en el sistema.");
        }

        var salt = GenerarSalt();
        var hash = HashearPassword(password, salt);

        var usuario = new Usuario
        {
            Username = username.Trim(),
            NombreCompleto = string.IsNullOrWhiteSpace(nombreCompleto) ? username.Trim() : nombreCompleto.Trim(),
            PasswordSalt = salt,
            PasswordHash = hash,
            Rol = rol,
            Activo = true
        };

        await _unitOfWork.Usuarios.AddAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(usuario);
    }

    public async Task CambiarContraseñaAsync(Guid usuarioId, string passwordActual, string nuevoPassword, CancellationToken cancellationToken = default)
    {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId, cancellationToken)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (!VerificarPassword(passwordActual, usuario.PasswordHash, usuario.PasswordSalt))
        {
            throw new InvalidOperationException("La contraseña actual no es correcta.");
        }

        if (string.IsNullOrWhiteSpace(nuevoPassword))
        {
            throw new ArgumentException("La nueva contraseña no puede estar vacía.", nameof(nuevoPassword));
        }

        var nuevoSalt = GenerarSalt();
        var nuevoHash = HashearPassword(nuevoPassword, nuevoSalt);

        usuario.PasswordSalt = nuevoSalt;
        usuario.PasswordHash = nuevoHash;
        usuario.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;
    }

    public static string HashearPassword(string password, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, HashSize);
        return Convert.ToBase64String(hash);
    }

    public static string GenerarSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    public static bool VerificarPassword(string password, string hashEsperado, string saltBase64)
    {
        var hashCalculado = HashearPassword(password, saltBase64);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hashCalculado),
            Convert.FromBase64String(hashEsperado));
    }

    private static UsuarioDto MapToDto(Usuario u)
    {
        return new UsuarioDto
        {
            Id = u.Id,
            Username = u.Username,
            NombreCompleto = u.NombreCompleto,
            Rol = u.Rol,
            Activo = u.Activo,
            UltimoIngreso = u.UltimoIngreso
        };
    }
}
