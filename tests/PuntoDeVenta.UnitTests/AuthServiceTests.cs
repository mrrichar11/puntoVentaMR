using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Seguridad;
using PuntoDeVenta.Domain.Entities.Seguridad;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _authService = new AuthService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CrearUsuarioYAutenticar_CredencialesCorrectas_AutenticaExitosamente()
    {
        // Arrange
        await _authService.CrearUsuarioAsync("cajera1", "Laura Gómez", "segura123", RolUsuario.Cajero);

        // Act
        var authResult = await _authService.AutenticarAsync(new LoginRequestDto
        {
            Username = "cajera1",
            Password = "segura123"
        });

        // Assert
        authResult.Should().NotBeNull();
        authResult!.Username.Should().Be("cajera1");
        authResult.NombreCompleto.Should().Be("Laura Gómez");
        authResult.Rol.Should().Be(RolUsuario.Cajero);
        _authService.UsuarioActual.Should().NotBeNull();
        _authService.UsuarioActual!.Username.Should().Be("cajera1");
    }

    [Fact]
    public async Task Autenticar_PasswordIncorrecto_RetornaNull()
    {
        // Arrange
        await _authService.CrearUsuarioAsync("admin", "Admin", "admin123", RolUsuario.Administrador);

        // Act
        var authResult = await _authService.AutenticarAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "password_erroneo"
        });

        // Assert
        authResult.Should().BeNull();
    }

    [Fact]
    public async Task Autenticar_UsuarioInexistente_RetornaNull()
    {
        // Act
        var authResult = await _authService.AutenticarAsync(new LoginRequestDto
        {
            Username = "usuario_inexistente",
            Password = "123"
        });

        // Assert
        authResult.Should().BeNull();
    }

    [Fact]
    public async Task CambiarContraseña_ActualizaCorrectamente()
    {
        // Arrange
        var usuario = await _authService.CrearUsuarioAsync("gerente", "Gerente", "vieja123", RolUsuario.Administrador);

        // Act
        await _authService.CambiarContraseñaAsync(usuario.Id, "vieja123", "nueva456");

        // Assert
        var loginViejo = await _authService.AutenticarAsync(new LoginRequestDto { Username = "gerente", Password = "vieja123" });
        loginViejo.Should().BeNull();

        var loginNuevo = await _authService.AutenticarAsync(new LoginRequestDto { Username = "gerente", Password = "nueva456" });
        loginNuevo.Should().NotBeNull();
    }

    [Fact]
    public async Task CrearUsuario_Duplicado_LanzaExcepcion()
    {
        // Arrange
        await _authService.CrearUsuarioAsync("usuario1", "Usuario 1", "pass1", RolUsuario.Cajero);

        // Act & Assert
        var act = async () => await _authService.CrearUsuarioAsync("usuario1", "Usuario Duplicado", "pass2", RolUsuario.Cajero);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
