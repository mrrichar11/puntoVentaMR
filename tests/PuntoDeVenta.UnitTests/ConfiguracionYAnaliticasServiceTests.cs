using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Analiticas;
using PuntoDeVenta.Application.DTOs.Configuracion;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Configuracion;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Ventas;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class ConfiguracionYAnaliticasServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly ConfiguracionService _configService;
    private readonly AnaliticasService _analiticasService;

    public ConfiguracionYAnaliticasServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _configService = new ConfiguracionService(_unitOfWork);
        _analiticasService = new AnaliticasService(_unitOfWork);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ObtenerConfiguracion_SiNoExiste_CreaConfiguracionPorDefecto()
    {
        // Act
        var config = await _configService.ObtenerConfiguracionAsync();

        // Assert
        config.Should().NotBeNull();
        config.NombreComercio.Should().Be("Mi Tienda & Zapatería");
        config.PorcentajeDescuentoEfectivo.Should().Be(10.0m);
        config.TopeMensualRetiroDueño.Should().Be(600000m);
        config.MargenGananciaSugerido.Should().Be(80.0m);
        config.CostosBancariosEstimados.Should().Be(5.0m);
    }

    [Fact]
    public async Task GuardarConfiguracion_ActualizaParametrosCorrectamente()
    {
        // Arrange
        var nuevoDto = new ConfiguracionNegocioDto
        {
            NombreComercio = "Boutique Glamour",
            Direccion = "Av. Santa Fe 1234",
            Telefono = "11-9876-5432",
            Cuit = "27-30123456-4",
            VendedoraDefecto = "Laura",
            PorcentajeDescuentoEfectivo = 15.0m,
            ComisionTarjetaDebito = 2.0m,
            ComisionTarjetaCredito = 5.0m,
            RecargoCuotasTarjetaCredito = 20.0m,
            MargenGananciaSugerido = 75.0m,
            CostosBancariosEstimados = 6.0m,
            TopeFiadoDefecto = 75000m,
            TopeMensualRetiroDueño = 800000m,
            TemaInterfaz = "Light"
        };

        // Act
        await _configService.GuardarConfiguracionAsync(nuevoDto);
        var recuperado = await _configService.ObtenerConfiguracionAsync();

        // Assert
        recuperado.NombreComercio.Should().Be("Boutique Glamour");
        recuperado.PorcentajeDescuentoEfectivo.Should().Be(15.0m);
        recuperado.MargenGananciaSugerido.Should().Be(75.0m);
        recuperado.CostosBancariosEstimados.Should().Be(6.0m);
        recuperado.TopeMensualRetiroDueño.Should().Be(800000m);
        recuperado.TemaInterfaz.Should().Be("Light");
    }

    [Fact]
    public async Task GuardarConfiguracion_GuardaLogoYCuotasDiferenciadas_Correctamente()
    {
        // Arrange
        var nuevoDto = new ConfiguracionNegocioDto
        {
            NombreComercio = "Zapatería Central",
            LogoRuta = "C:\\Logos\\zapateria.png",
            Habilitar3Cuotas = true,
            Recargo3Cuotas = 12.5m,
            Habilitar6Cuotas = true,
            Recargo6Cuotas = 24.0m,
            Habilitar9Cuotas = true,
            Recargo9Cuotas = 36.0m,
            Habilitar12Cuotas = true,
            Recargo12Cuotas = 48.0m,
            CuotasIncluidasEnPrecioLista = true
        };

        // Act
        await _configService.GuardarConfiguracionAsync(nuevoDto);
        var recuperado = await _configService.ObtenerConfiguracionAsync();

        // Assert
        recuperado.LogoRuta.Should().Be("C:\\Logos\\zapateria.png");
        recuperado.Habilitar3Cuotas.Should().BeTrue();
        recuperado.Recargo3Cuotas.Should().Be(12.5m);
        recuperado.Habilitar6Cuotas.Should().BeTrue();
        recuperado.Recargo6Cuotas.Should().Be(24.0m);
        recuperado.Habilitar9Cuotas.Should().BeTrue();
        recuperado.Recargo9Cuotas.Should().Be(36.0m);
        recuperado.Habilitar12Cuotas.Should().BeTrue();
        recuperado.Recargo12Cuotas.Should().Be(48.0m);
        recuperado.CuotasIncluidasEnPrecioLista.Should().BeTrue();
    }

    [Fact]
    public void CalculoPrecioSugerido_FormulaMatematicaCalculaExacto()
    {
        // Costo = $10,000 | Margen = 80% | Costos Bancarios = 5%
        // $10,000 * 1.80 * 1.05 = $18,900
        decimal costo = 10000m;
        decimal margen = 80m;
        decimal costosBancarios = 5m;

        var factorMargen = 1m + (margen / 100m);
        var factorBancario = 1m + (costosBancarios / 100m);
        var precioSugerido = Math.Round(costo * factorMargen * factorBancario, 0);

        precioSugerido.Should().Be(18900m);
    }

    [Fact]
    public async Task ObtenerEstadoRetirosDueñoMes_CalculaTotalConsumidoYPorcentaje()
    {
        // Arrange
        await _configService.GuardarConfiguracionAsync(new ConfiguracionNegocioDto
        {
            NombreComercio = "Test",
            TopeMensualRetiroDueño = 100000m
        });

        var turno = new TurnoCaja { UsuarioApertura = "Dueño", MontoInicialEfectivo = 5000m };
        await _context.TurnosCaja.AddAsync(turno);

        var catGasto = new CategoriaGasto { Nombre = "Retiros y Operativos" };
        await _context.CategoriasGasto.AddAsync(catGasto);
        await _context.SaveChangesAsync();

        // Registrar gastos: uno personal del dueño y otro operativo
        var gastoPersonal = new Gasto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = catGasto.Id,
            Monto = 30000m,
            Descripcion = "Retiro personal del dueño",
            EsPersonal = true,
            Fecha = DateTime.UtcNow
        };
        var gastoOperativo = new Gasto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = catGasto.Id,
            Monto = 10000m,
            Descripcion = "Compra de bolsas del local",
            EsPersonal = false,
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Gastos.AddAsync(gastoPersonal);
        await _unitOfWork.Gastos.AddAsync(gastoOperativo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var estado = await _configService.ObtenerEstadoRetirosDueñoMesAsync();

        // Assert
        estado.TotalRetiradoMes.Should().Be(30000m); // Solo los personales/retiro dueño
        estado.TopeMensual.Should().Be(100000m);
        estado.SaldoDisponible.Should().Be(70000m);
        estado.PorcentajeConsumido.Should().Be(30.0m);
        estado.HaSuperadoTope.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerReporteAnaliticas_CalculaKpisYMargenHistorico()
    {
        // Arrange
        var turno = new TurnoCaja
        {
            UsuarioApertura = "Laura",
            MontoInicialEfectivo = 10000m
        };
        await _context.TurnosCaja.AddAsync(turno);

        var cat = new Categoria { Nombre = "Calzado" };
        var marca = new Marca { Nombre = "Lady Stork" };
        await _context.Categorias.AddAsync(cat);
        await _context.Marcas.AddAsync(marca);

        var art = new Articulo
        {
            Nombre = "Stiletto Classic",
            CodigoEstilo = "CAL-01",
            Categoria = cat,
            Marca = marca
        };
        await _context.Articulos.AddAsync(art);

        var var1 = new VarianteArticulo
        {
            Articulo = art,
            SKU = "SKU-01",
            CodigoBarras = "779123456",
            Talle = "37",
            Color = "Negro",
            PrecioCosto = 10000m,
            PrecioLista = 18150m,
            StockActual = 20
        };
        await _context.VariantesArticulo.AddAsync(var1);

        var metodo = new MetodoPago { Nombre = "Efectivo", PorcentajeAjuste = 0 };
        await _context.MetodosPago.AddAsync(metodo);

        // Venta con costo congelado $10,000 y precio venta $18,150
        var venta = new Venta
        {
            TurnoCajaId = turno.Id,
            NumeroComprobante = "T-0001-00000001",
            Fecha = DateTime.UtcNow,
            Vendedora = "Laura",
            CanalVenta = CanalVenta.Mostrador,
            TotalFinalCobrado = 36300m,
            CostoTotalHistorico = 20000m
        };

        var linea = new LineaVenta
        {
            Variante = var1,
            VarianteId = var1.Id,
            DescripcionArticulo = "Stiletto Classic",
            SKU = "SKU-01",
            Talle = "37",
            Color = "Negro",
            Cantidad = 2,
            PrecioListaUnitario = 18150m,
            PrecioFinalCobrado = 18150m,
            CostoUnitarioHistorico = 10000m
        };
        venta.Lineas.Add(linea);

        var pago = new PagoVenta
        {
            MetodoPago = metodo,
            MetodoPagoId = metodo.Id,
            Canal = CanalDinero.Efectivo,
            Monto = 36300m
        };
        venta.Pagos.Add(pago);

        await _context.Ventas.AddAsync(venta);
        await _context.SaveChangesAsync();

        // Act
        var reporte = await _analiticasService.ObtenerReporteAsync(PeriodoAnalitica.MesActual);

        // Assert
        reporte.CantidadVentas.Should().Be(1);
        reporte.TotalFacturado.Should().Be(36300m);
        reporte.CostoTotalMercaderia.Should().Be(20000m);
        reporte.GananciaBrutaReal.Should().Be(16300m);
        reporte.TicketPromedio.Should().Be(36300m);
        reporte.TopProductos.Should().HaveCount(1);
        reporte.TopProductos[0].CantidadVendida.Should().Be(2);
        reporte.TotalVentasMostrador.Should().Be(36300m);
    }
}
