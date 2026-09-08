using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Exceptions;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class CajaServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly CajaService _cajaService;

    public CajaServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _cajaService = new CajaService(_unitOfWork);
    }

    [Fact]
    public async Task AbrirTurno_ConFondoInicial_CreaTurnoYMovimientoApertura()
    {
        // Arrange
        var dto = new AbrirCajaDto
        {
            Usuario = "Cajero_1",
            MontoInicialEfectivo = 15000m,
            Observaciones = "Turno Mañana"
        };

        // Act
        var turno = await _cajaService.AbrirTurnoAsync(dto);

        // Assert
        turno.Should().NotBeNull();
        turno.Estado.Should().Be(EstadoTurnoCaja.Abierto);
        turno.MontoInicialEfectivo.Should().Be(15000m);
        turno.UsuarioApertura.Should().Be("Cajero_1");

        // Movimiento de fondo inicial
        var movimientos = await _context.MovimientosCaja.ToListAsync();
        movimientos.Should().HaveCount(1);
        var mov = movimientos.First();
        mov.Tipo.Should().Be(TipoMovimientoCaja.Ingreso);
        mov.Concepto.Should().Be(ConceptoMovimientoCaja.FondoInicialApertura);
        mov.Canal.Should().Be(CanalDinero.Efectivo);
        mov.Monto.Should().Be(15000m);
    }

    [Fact]
    public async Task AbrirTurno_CuandoYaHayTurnoAbierto_LanzaDomainException()
    {
        // Arrange: Abrir un primer turno
        await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_1", MontoInicialEfectivo = 10000m });

        // Act: Intentar abrir un segundo turno sin cerrar el primero
        var accion = async () => await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_2", MontoInicialEfectivo = 5000m });

        // Assert
        await accion.Should().ThrowAsync<DomainException>()
            .WithMessage("*Ya existe un turno de caja abierto*");
    }

    [Fact]
    public async Task RegistrarGasto_DistingueEntreGastoOperativoYRetiroPropietario()
    {
        // Arrange
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_1", MontoInicialEfectivo = 20000m });

        var catPackaging = new CategoriaGasto
        {
            Nombre = "Bolsas y Packaging",
            TipoGasto = TipoGasto.Packaging,
            EsGastoPersonal = false // Gasto operativo comercial
        };

        var catRetiroSueldo = new CategoriaGasto
        {
            Nombre = "Retiro Propietario",
            TipoGasto = TipoGasto.RetiroPropietario,
            EsGastoPersonal = true // Retiro personal del dueño
        };

        _context.CategoriasGasto.AddRange(catPackaging, catRetiroSueldo);
        await _context.SaveChangesAsync();

        // Act: Registrar gasto operativo en efectivo
        var gastoOperativo = await _cajaService.RegistrarGastoAsync(new RegistrarGastoDto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = catPackaging.Id,
            Monto = 3500m,
            Descripcion = "100 bolsas ecológicas para calzado",
            Canal = CanalDinero.Efectivo
        });

        // Act: Registrar retiro del dueño en efectivo
        var gastoPersonal = await _cajaService.RegistrarGastoAsync(new RegistrarGastoDto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = catRetiroSueldo.Id,
            Monto = 8000m,
            Descripcion = "Retiro particular del titular",
            Canal = CanalDinero.Efectivo
        });

        // Assert
        gastoOperativo.EsPersonal.Should().BeFalse();
        gastoPersonal.EsPersonal.Should().BeTrue();

        var egresos = await _context.MovimientosCaja
            .Where(m => m.Tipo == TipoMovimientoCaja.Egreso)
            .ToListAsync();

        egresos.Should().HaveCount(2);
        egresos.Should().Contain(m => m.Concepto == ConceptoMovimientoCaja.GastoOperativo && m.Monto == 3500m);
        egresos.Should().Contain(m => m.Concepto == ConceptoMovimientoCaja.RetiroPropietario && m.Monto == 8000m);
    }

    [Fact]
    public async Task CerrarTurnoCiego_Multicanal_ReconciliaEfectivoTransferenciasYTarjetasConDiferencias()
    {
        // Arrange
        // 1. Apertura: $15,000 efectivo
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_Tarde", MontoInicialEfectivo = 15000m });

        // 2. Venta en Efectivo: $10,000
        await _cajaService.RegistrarMovimientoAsync(new RegistrarMovimientoCajaDto
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Concepto = ConceptoMovimientoCaja.Venta,
            Canal = CanalDinero.Efectivo,
            Monto = 10000m,
            Descripcion = "Venta Mostrador #101"
        });

        // 3. Venta por Transferencia Bancaria / QR: $25,000
        await _cajaService.RegistrarMovimientoAsync(new RegistrarMovimientoCajaDto
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Concepto = ConceptoMovimientoCaja.Venta,
            Canal = CanalDinero.TransferenciaQR,
            Monto = 25000m,
            Descripcion = "Venta Mostrador #102 - Transf. Banco Galicia",
            ReferenciaComprobante = "TRANSF-88992"
        });

        // 4. Venta con Tarjeta de Crédito: $30,000
        await _cajaService.RegistrarMovimientoAsync(new RegistrarMovimientoCajaDto
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Concepto = ConceptoMovimientoCaja.Venta,
            Canal = CanalDinero.TarjetaCredito,
            Monto = 30000m,
            Descripcion = "Venta Mostrador #103 - Visa Crédito 3 Cuotas",
            ReferenciaComprobante = "CUPON-4411"
        });

        // 5. Gasto operativo en efectivo: $3,000 (envío cadetería)
        var catEnvio = new CategoriaGasto { Nombre = "Cadetería Local", TipoGasto = TipoGasto.Envio, EsGastoPersonal = false };
        _context.CategoriasGasto.Add(catEnvio);
        await _context.SaveChangesAsync();

        await _cajaService.RegistrarGastoAsync(new RegistrarGastoDto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = catEnvio.Id,
            Monto = 3000m,
            Descripcion = "Moto envío a domicilio",
            Canal = CanalDinero.Efectivo
        });

        // 6. Retiro personal del dueño: $5,000 en efectivo
        var catRetiro = new CategoriaGasto { Nombre = "Retiro Propietario", TipoGasto = TipoGasto.RetiroPropietario, EsGastoPersonal = true };
        _context.CategoriasGasto.Add(catRetiro);
        await _context.SaveChangesAsync();

        await _cajaService.RegistrarGastoAsync(new RegistrarGastoDto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = catRetiro.Id,
            Monto = 5000m,
            Descripcion = "Retiro personal de caja",
            Canal = CanalDinero.Efectivo
        });

        // TEÓRICOS ESPERADOS:
        // Efectivo: $15,000 (inicio) + $10,000 (venta) - $3,000 (envío) - $5,000 (retiro dueño) = $17,000
        // Transferencias: $25,000
        // Tarjetas: $30,000

        // Act: Arqueo ciego declarado por el cajero
        var cierreCiego = new CerrarCajaCiegoDto
        {
            TurnoCajaId = turno.Id,
            Usuario = "Cajero_Tarde",
            MontoRealEfectivo = 16800m,       // Contó 16,800 -> Faltan $200
            MontoRealTransferencias = 25000m, // Coincide exacto -> Dif $0
            MontoRealTarjetas = 30500m,       // Contó $30,500 en cupones -> Sobran $500
            Observaciones = "Cierre ciego verificado"
        };

        var resumen = await _cajaService.CerrarTurnoCiegoAsync(cierreCiego);

        // Assert
        resumen.MontoTeoricoEfectivo.Should().Be(17000m);
        resumen.MontoRealEfectivo.Should().Be(16800m);
        resumen.DiferenciaEfectivo.Should().Be(-200m); // Faltante de $200

        resumen.MontoTeoricoTransferencias.Should().Be(25000m);
        resumen.MontoRealTransferencias.Should().Be(25000m);
        resumen.DiferenciaTransferencias.Should().Be(0m);

        resumen.MontoTeoricoTarjetas.Should().Be(30000m);
        resumen.MontoRealTarjetas.Should().Be(30500m);
        resumen.DiferenciaTarjetas.Should().Be(+500m); // Sobrante de $500

        // Auditoría separada de finanzas
        resumen.TotalVentasTurno.Should().Be(65000m); // 10k efectivo + 25k transf + 30k tarjeta
        resumen.TotalGastosOperativosNegocio.Should().Be(3000m);
        resumen.TotalRetirosPersonalesPropietario.Should().Be(5000m);

        // Estado del turno
        var turnoEnBd = await _context.TurnosCaja.FindAsync(turno.Id);
        turnoEnBd!.Estado.Should().Be(EstadoTurnoCaja.Cerrado);
        turnoEnBd.FechaCierre.Should().NotBeNull();
        turnoEnBd.DiferenciaEfectivo.Should().Be(-200m);
    }

    [Fact]
    public async Task RegistrarMovimiento_ConTurnoCerrado_LanzaDomainException()
    {
        // Arrange
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_1", MontoInicialEfectivo = 1000m });
        await _cajaService.CerrarTurnoCiegoAsync(new CerrarCajaCiegoDto
        {
            TurnoCajaId = turno.Id,
            Usuario = "Cajero_1",
            MontoRealEfectivo = 1000m,
            MontoRealTransferencias = 0m,
            MontoRealTarjetas = 0m
        });

        // Act: Intentar registrar movimiento con turno cerrado
        var accion = async () => await _cajaService.RegistrarMovimientoAsync(new RegistrarMovimientoCajaDto
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Concepto = ConceptoMovimientoCaja.Venta,
            Canal = CanalDinero.Efectivo,
            Monto = 5000m
        });

        // Assert
        await accion.Should().ThrowAsync<DomainException>()
            .WithMessage("*turno de caja que ya está cerrado*");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
