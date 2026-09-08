using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Application.DTOs.Ventas;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Ventas;
using PuntoDeVenta.Domain.Exceptions;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class VentaServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly CalculadorPreciosService _calculadorPrecios;
    private readonly CajaService _cajaService;
    private readonly VentaService _ventaService;

    private readonly Categoria _categoria;
    private readonly Marca _marca;
    private readonly Articulo _articulo;
    private readonly VarianteArticulo _variante1;
    private readonly VarianteArticulo _variante2;
    private readonly MetodoPago _metodoEfectivo;
    private readonly MetodoPago _metodoTransferencia;

    public VentaServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _calculadorPrecios = new CalculadorPreciosService();
        _cajaService = new CajaService(_unitOfWork);
        _ventaService = new VentaService(_unitOfWork, _calculadorPrecios);

        // Seeding Inicial
        _categoria = new Categoria { Nombre = "Calzado Deportivo" };
        _marca = new Marca { Nombre = "Nike" };
        _context.Categorias.Add(_categoria);
        _context.Marcas.Add(_marca);

        _articulo = new Articulo
        {
            CodigoEstilo = "PEGASUS-40",
            Nombre = "Zapatilla Air Zoom Pegasus 40",
            Categoria = _categoria,
            Marca = _marca
        };
        _context.Articulos.Add(_articulo);

        _variante1 = new VarianteArticulo
        {
            Articulo = _articulo,
            SKU = "PEGASUS-40-41-NEGRO",
            CodigoBarras = "7791234567890",
            Talle = "41",
            Color = "Negro",
            PrecioCosto = 20000m,
            PrecioLista = 50000m,
            StockActual = 10,
            StockMinimo = 2
        };

        _variante2 = new VarianteArticulo
        {
            Articulo = _articulo,
            SKU = "PEGASUS-40-42-AZUL",
            CodigoBarras = "7791234567891",
            Talle = "42",
            Color = "Azul",
            PrecioCosto = 22000m,
            PrecioLista = 55000m,
            StockActual = 3,
            StockMinimo = 1
        };

        _context.VariantesArticulo.AddRange(_variante1, _variante2);

        _metodoEfectivo = new MetodoPago
        {
            Nombre = "Efectivo",
            PorcentajeAjuste = -10.0m // 10% descuento
        };

        _metodoTransferencia = new MetodoPago
        {
            Nombre = "Transferencia Bancaria / QR",
            PorcentajeAjuste = 0m,
            ComisionPorcentual = 0.8m
        };

        _context.MetodosPago.AddRange(_metodoEfectivo, _metodoTransferencia);
        _context.SaveChanges();
    }

    [Fact]
    public async Task BuscarItemRapido_CodigoBarrasExacto_RetornaVarianteInmediatamente()
    {
        // Act: Simular escaneo con lector óptico láser
        var resultados = await _ventaService.BuscarItemRapidoAsync("7791234567890");

        // Assert
        resultados.Should().HaveCount(1);
        var item = resultados.First();
        item.SKU.Should().Be("PEGASUS-40-41-NEGRO");
        item.Talle.Should().Be("41");
        item.StockActual.Should().Be(10);
    }

    [Fact]
    public async Task BuscarItemRapido_BusquedaManualTexto_EncuentraVariantePorTalleColorYModelo()
    {
        // Act: El cajero busca manualmente "Azul" o "42"
        var resultados = await _ventaService.BuscarItemRapidoAsync("Azul");

        // Assert
        resultados.Should().HaveCount(1);
        resultados.First().SKU.Should().Be("PEGASUS-40-42-AZUL");
        resultados.First().Talle.Should().Be("42");
    }

    [Fact]
    public async Task ProcesarVenta_VentaSimpleEfectivo_CongelaCostosDescuentaStockEImpactaCaja()
    {
        // Arrange: Abrir turno de caja
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_Central", MontoInicialEfectivo = 10000m });

        // Venta de 2 unidades de Variante 1 (Precio lista $50,000, costo $20,000, con 10% descuento en efectivo -> $45,000 c/u)
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            ClienteNombre = "Juan Perez",
            Items = new List<ItemCarritoDto>
            {
                new ItemCarritoDto { VarianteId = _variante1.Id, Cantidad = 2 }
            },
            Pagos = new List<PagoDto>
            {
                new PagoDto
                {
                    MetodoPagoId = _metodoEfectivo.Id,
                    Canal = CanalDinero.Efectivo,
                    Monto = 90000m // 45,000 * 2
                }
            }
        };

        // Act
        var venta = await _ventaService.ProcesarVentaAsync(dto);

        // Assert: Resumen de Venta
        venta.NumeroComprobante.Should().StartWith("T-0001-");
        venta.SubtotalLista.Should().Be(100000m); // 50k * 2
        venta.TotalDescuentoMedioPago.Should().Be(10000m); // 5k * 2 (10%)
        venta.TotalFinalCobrado.Should().Be(90000m);
        venta.CostoTotalHistorico.Should().Be(40000m); // 20k * 2
        venta.MargenBrutoReal.Should().Be(50000m); // 90k - 40k
        venta.PorcentajeMargenBrutoReal.Should().Be(55.56m);

        // Assert: Descuento de stock en base de datos
        var varianteEnBd = await _context.VariantesArticulo.FindAsync(_variante1.Id);
        varianteEnBd!.StockActual.Should().Be(8); // 10 - 2

        // Assert: Movimiento de stock inmutable
        var movStock = await _context.MovimientosStock
            .FirstOrDefaultAsync(m => m.VarianteId == _variante1.Id && m.Tipo == TipoMovimientoStock.Venta);
        movStock.Should().NotBeNull();
        movStock!.Cantidad.Should().Be(-2);
        movStock.StockPrevio.Should().Be(10);
        movStock.StockResultante.Should().Be(8);
        movStock.CostoUnitario.Should().Be(20000m);

        // Assert: Movimiento en Caja
        var movCaja = await _context.MovimientosCaja
            .FirstOrDefaultAsync(m => m.TurnoCajaId == turno.Id && m.Concepto == ConceptoMovimientoCaja.Venta);
        movCaja.Should().NotBeNull();
        movCaja!.Canal.Should().Be(CanalDinero.Efectivo);
        movCaja.Monto.Should().Be(90000m);
    }

    [Fact]
    public async Task ProcesarVenta_PagoCombinado_EfectivoYTransferencia_RegistraMovimientosCajaCorrectos()
    {
        // Arrange
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_1", MontoInicialEfectivo = 5000m });

        // Venta de 1 unidad de $55,000
        // Pagada en split: $20,000 en Efectivo + $35,000 por Transferencia/QR
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            Items = new List<ItemCarritoDto>
            {
                new ItemCarritoDto { VarianteId = _variante2.Id, Cantidad = 1 }
            },
            Pagos = new List<PagoDto>
            {
                new PagoDto
                {
                    MetodoPagoId = _metodoEfectivo.Id,
                    Canal = CanalDinero.Efectivo,
                    Monto = 20000m
                },
                new PagoDto
                {
                    MetodoPagoId = _metodoTransferencia.Id,
                    Canal = CanalDinero.TransferenciaQR,
                    Monto = 35000m,
                    Referencia = "TR-GALICIA-5544"
                }
            }
        };

        // Act
        var venta = await _ventaService.ProcesarVentaAsync(dto);

        // Assert
        venta.Pagos.Should().HaveCount(2);

        // Verificar que en la caja se crearon 2 ingresos con sus canales respectivos
        var movimientosCaja = await _context.MovimientosCaja
            .Where(m => m.TurnoCajaId == turno.Id && m.Concepto == ConceptoMovimientoCaja.Venta)
            .ToListAsync();

        movimientosCaja.Should().HaveCount(2);
        movimientosCaja.Should().Contain(m => m.Canal == CanalDinero.Efectivo && m.Monto == 20000m);
        movimientosCaja.Should().Contain(m => m.Canal == CanalDinero.TransferenciaQR && m.Monto == 35000m && m.ReferenciaComprobante == "TR-GALICIA-5544");
    }

    [Fact]
    public async Task ProcesarVenta_StockInsuficiente_LanzaStockInsuficienteExceptionYNoModificaNada()
    {
        // Arrange
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_1", MontoInicialEfectivo = 1000m });

        // Intentar vender 5 unidades cuando solo hay 3 en stock
        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            Items = new List<ItemCarritoDto>
            {
                new ItemCarritoDto { VarianteId = _variante2.Id, Cantidad = 5 }
            },
            Pagos = new List<PagoDto>
            {
                new PagoDto { Canal = CanalDinero.Efectivo, Monto = 250000m }
            }
        };

        // Act & Assert
        var accion = async () => await _ventaService.ProcesarVentaAsync(dto);
        await accion.Should().ThrowAsync<StockInsuficienteException>()
            .WithMessage("*Stock insuficiente*");

        // Stock original intacto
        var varianteEnBd = await _context.VariantesArticulo.FindAsync(_variante2.Id);
        varianteEnBd!.StockActual.Should().Be(3);

        // Cero ventas creadas
        var ventasEnBd = await _context.Ventas.ToListAsync();
        ventasEnBd.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcesarVenta_ConCajaCerrada_LanzaDomainException()
    {
        // Arrange: Abrir y cerrar turno
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto { Usuario = "Cajero_1", MontoInicialEfectivo = 1000m });
        await _cajaService.CerrarTurnoCiegoAsync(new CerrarCajaCiegoDto { TurnoCajaId = turno.Id, Usuario = "Cajero_1", MontoRealEfectivo = 1000m });

        var dto = new RegistrarVentaDto
        {
            TurnoCajaId = turno.Id,
            Items = new List<ItemCarritoDto>
            {
                new ItemCarritoDto { VarianteId = _variante1.Id, Cantidad = 1 }
            },
            Pagos = new List<PagoDto>
            {
                new PagoDto { Canal = CanalDinero.Efectivo, Monto = 50000m }
            }
        };

        // Act & Assert
        var accion = async () => await _ventaService.ProcesarVentaAsync(dto);
        await accion.Should().ThrowAsync<DomainException>()
            .WithMessage("*turno de caja se encuentra cerrado*");
    }

    [Fact]
    public async Task BuscarItemRapidoAsync_ConTerminosMultiplesYPlurales_DebeRetornarCoincidencias()
    {
        // Act: El usuario escribe en plural "Zapatillas Pegasus" cuando el producto se llama "Zapatilla Air Zoom Pegasus 40"
        var resultados = await _ventaService.BuscarItemRapidoAsync("Zapatillas Pegasus");

        // Assert
        resultados.Should().HaveCount(2);
        resultados.Should().Contain(r => r.SKU == "PEGASUS-40-41-NEGRO");
        resultados.Should().Contain(r => r.SKU == "PEGASUS-40-42-AZUL");
    }

    [Fact]
    public async Task BuscarItemRapidoAsync_ConModeloYTalle_DebeRetornarVarianteEspecifica()
    {
        // Act: Buscar por "Pegasus 42"
        var resultados = await _ventaService.BuscarItemRapidoAsync("Pegasus 42");

        // Assert
        resultados.Should().HaveCount(1);
        resultados[0].SKU.Should().Be("PEGASUS-40-42-AZUL");
        resultados[0].Talle.Should().Be("42");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
