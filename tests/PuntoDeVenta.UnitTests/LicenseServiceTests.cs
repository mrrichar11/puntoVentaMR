using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Domain.Entities.Seguridad;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class LicenseServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly LicenseService _licenseService;

    public LicenseServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _licenseService = new LicenseService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ValidarLicencia_CuandoNoExiste_CreaLicenciaInicialDe30DiasValida()
    {
        // Act
        var estado = await _licenseService.ValidarLicenciaAsync();

        // Assert
        estado.Should().NotBeNull();
        estado.EsValida.Should().BeTrue();
        estado.DiasRestantes.Should().BeInRange(29, 31);
        estado.RelojAdulterado.Should().BeFalse();
        estado.CodigoInstalacion.Should().StartWith("POS-");
    }

    [Fact]
    public async Task GenerarYValidarClave_PlanEstandar_PermiteActivacionSinIA()
    {
        // Arrange
        var estadoInicial = await _licenseService.ValidarLicenciaAsync();
        var codigo = estadoInicial.CodigoInstalacion;

        var nuevaFecha = DateTime.UtcNow.Date.AddDays(60).AddHours(23).AddMinutes(59).AddSeconds(59);
        var claveGenerada = _licenseService.GenerarClaveActivacion(codigo, nuevaFecha, TipoPlanLicencia.Estandar);

        claveGenerada.Should().StartWith("STD-");

        // Act
        var resultado = await _licenseService.ActivarLicenciaAsync(claveGenerada);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.NuevaFechaExpiracion.Should().NotBeNull();

        var estadoActualizado = await _licenseService.ValidarLicenciaAsync();
        estadoActualizado.EsValida.Should().BeTrue();
        estadoActualizado.TipoPlan.Should().Be(TipoPlanLicencia.Estandar);
        estadoActualizado.TieneModuloIA.Should().BeFalse();
        estadoActualizado.DiasRestantes.Should().BeInRange(59, 61);
    }

    [Fact]
    public async Task GenerarYValidarClave_PlanPremium_PermiteActivacionConIA()
    {
        // Arrange
        var estadoInicial = await _licenseService.ValidarLicenciaAsync();
        var codigo = estadoInicial.CodigoInstalacion;

        var nuevaFecha = DateTime.UtcNow.Date.AddDays(45).AddHours(23).AddMinutes(59).AddSeconds(59);
        var claveGenerada = _licenseService.GenerarClaveActivacion(codigo, nuevaFecha, TipoPlanLicencia.Premium);

        claveGenerada.Should().StartWith("PRM-");

        // Act
        var resultado = await _licenseService.ActivarLicenciaAsync(claveGenerada);

        // Assert
        resultado.Exitoso.Should().BeTrue();

        var estadoActualizado = await _licenseService.ValidarLicenciaAsync();
        estadoActualizado.EsValida.Should().BeTrue();
        estadoActualizado.TipoPlan.Should().Be(TipoPlanLicencia.Premium);
        estadoActualizado.TieneModuloIA.Should().BeTrue();
        estadoActualizado.DiasRestantes.Should().BeInRange(44, 46);
    }

    [Fact]
    public async Task ActivarLicencia_AlterarPrefijoDeEstandarAPremium_FallaPorFirmaInvalida()
    {
        // Arrange
        var estadoInicial = await _licenseService.ValidarLicenciaAsync();
        var codigo = estadoInicial.CodigoInstalacion;

        var nuevaFecha = DateTime.UtcNow.Date.AddDays(30).AddHours(23).AddMinutes(59).AddSeconds(59);
        var claveStd = _licenseService.GenerarClaveActivacion(codigo, nuevaFecha, TipoPlanLicencia.Estandar);

        // Intentar vulnerar cambiando manualmente STD por PRM sin recalcular la firma HMAC
        var claveAdulterada = "PRM" + claveStd[3..];

        // Act
        var resultado = await _licenseService.ActivarLicenciaAsync(claveAdulterada);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("no corresponde");
    }

    [Fact]
    public async Task ActivarLicencia_ClaveInvalidaOFalsificada_RechazaActivacion()
    {
        // Arrange
        await _licenseService.ValidarLicenciaAsync();

        // Clave con firma alterada
        var claveTrucha = "ACT-20271231-12345678";

        // Act
        var resultado = await _licenseService.ActivarLicenciaAsync(claveTrucha);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("no corresponde");
    }

    [Fact]
    public async Task ActivarLicencia_ClaveParaOtroEquipo_RechazaActivacion()
    {
        // Arrange
        await _licenseService.ValidarLicenciaAsync();

        var codigoOtroEquipo = "POS-OTRAPC-99999999";
        var fecha = DateTime.UtcNow.AddDays(30);
        var claveOtroEquipo = _licenseService.GenerarClaveActivacion(codigoOtroEquipo, fecha);

        // Act
        var resultado = await _licenseService.ActivarLicenciaAsync(claveOtroEquipo);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("no corresponde");
    }

    [Fact]
    public async Task ValidarLicencia_SiRelojEsAtrasado_DetectaAdulteracionYBloquea()
    {
        // Arrange
        await _licenseService.ValidarLicenciaAsync();

        // Simular que el último uso registrado en la base fue en el año 2028 (en el futuro)
        var licencia = (await _unitOfWork.Licencias.GetAllAsync()).First();
        licencia.UltimaFechaUso = DateTime.UtcNow.AddMonths(3);
        _unitOfWork.Licencias.Update(licencia);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var estado = await _licenseService.ValidarLicenciaAsync();

        // Assert
        estado.EsValida.Should().BeFalse();
        estado.RelojAdulterado.Should().BeTrue();
        estado.MensajeEstado.Should().Contain("retrasado");
    }

    [Fact]
    public async Task ValidarLicencia_SiFechaYaPaso_ReportaExpirada()
    {
        // Arrange
        var estado = await _licenseService.ValidarLicenciaAsync();
        var licencia = (await _unitOfWork.Licencias.GetAllAsync()).First();

        // Licencia vencida ayer
        licencia.FechaExpiracion = DateTime.UtcNow.AddDays(-1);
        licencia.ClaveActivacion = _licenseService.GenerarClaveActivacion(licencia.CodigoInstalacion, licencia.FechaExpiracion);
        licencia.UltimaFechaUso = DateTime.UtcNow.AddDays(-1);
        _unitOfWork.Licencias.Update(licencia);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var estadoPostVencimiento = await _licenseService.ValidarLicenciaAsync();

        // Assert
        estadoPostVencimiento.EsValida.Should().BeFalse();
        estadoPostVencimiento.DiasRestantes.Should().Be(0);
        estadoPostVencimiento.MensajeEstado.Should().Contain("venció");
    }
}
