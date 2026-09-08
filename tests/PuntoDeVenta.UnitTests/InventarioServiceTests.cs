using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Exceptions;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class InventarioServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly InventarioService _inventarioService;
    private readonly Categoria _categoriaPrueba;
    private readonly Marca _marcaPrueba;

    public InventarioServiceTests()
    {
        // Configuración de SQLite in-memory para tests limpios e independientes
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _inventarioService = new InventarioService(_unitOfWork, new BarcodeService());

        // Seeding de categoría y marca
        _categoriaPrueba = new Categoria { Nombre = "Remeras", Descripcion = "Remeras de algodón" };
        _marcaPrueba = new Marca { Nombre = "UrbanWear" };

        _context.Categorias.Add(_categoriaPrueba);
        _context.Marcas.Add(_marcaPrueba);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CrearArticuloConMatriz_IndumentariaLetras_GeneraCartesianoYSkusCorrectos()
    {
        // Arrange: Remera con talles XS, S, M, L y 2 colores (Negro, Blanco)
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "REM-URBAN",
            Nombre = "Remera Básica Oversize",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 5000m,
            PrecioLista = 12000m,
            Talles = new List<string> { "XS", "S", "M", "L" },
            Colores = new List<string> { "Negro", "Blanco" },
            StockInicialDefecto = 5
        };

        // Act
        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);

        // Assert
        articulo.Should().NotBeNull();
        articulo.Variantes.Should().HaveCount(8); // 4 talles * 2 colores = 8 variantes

        var skuEjemplo = articulo.Variantes.FirstOrDefault(v => v.Talle == "M" && v.Color == "Negro");
        skuEjemplo.Should().NotBeNull();
        skuEjemplo!.SKU.Should().Be("REM-URBAN-M-NEGRO");
        skuEjemplo.StockActual.Should().Be(5);
        skuEjemplo.PrecioCosto.Should().Be(5000m);
        skuEjemplo.PrecioLista.Should().Be(12000m);

        // Verifica que se hayan registrado 8 movimientos de stock inicial
        var movimientos = await _context.MovimientosStock.ToListAsync();
        movimientos.Should().HaveCount(8);
        movimientos.All(m => m.Cantidad == 5 && m.Tipo == TipoMovimientoStock.EntradaCompra).Should().BeTrue();
    }

    [Fact]
    public async Task CrearArticuloConMatriz_PantalonesDenimNumerico_GeneraVariantesPorCurva()
    {
        // Arrange: Pantalones jeans con curva numérica (38, 40, 42) y color Azul
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "JNS-SLIM",
            Nombre = "Jean Slim Fit Denim",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 12000m,
            PrecioLista = 28000m,
            Talles = new List<string> { "38", "40", "42" },
            Colores = new List<string> { "Azul" },
            StockInicialDefecto = 2
        };

        // Act
        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);

        // Assert
        articulo.Variantes.Should().HaveCount(3);
        articulo.Variantes.Select(v => v.SKU).Should().Contain(new[]
        {
            "JNS-SLIM-38-AZUL",
            "JNS-SLIM-40-AZUL",
            "JNS-SLIM-42-AZUL"
        });
    }

    [Fact]
    public async Task CrearArticuloConMatriz_SoloCombinacionesEspecificadas_NoCreaVariantesFantasma()
    {
        // Arrange: Remeras con talles 2 y 4, colores Blanco y Negro, pero solo 2 combinaciones compradas (2 Blanco y 4 Negro)
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "REM-BASICA",
            Nombre = "Remera Básica",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 7000m,
            PrecioLista = 15200m,
            Talles = new List<string> { "2", "4" },
            Colores = new List<string> { "Blanco", "Negro" },
            StockInicialPorCombinacion = new Dictionary<(string Talle, string Color), int>
            {
                { ("2", "Blanco"), 2 },
                { ("4", "Negro"), 2 }
            },
            SoloCombinacionesEspecificadas = true
        };

        // Act
        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);

        // Assert: Debe haber creado exactamente 2 variantes, NO 4
        articulo.Variantes.Should().HaveCount(2);
        articulo.Variantes.Should().Contain(v => v.Talle == "2" && v.Color == "Blanco" && v.StockActual == 2);
        articulo.Variantes.Should().Contain(v => v.Talle == "4" && v.Color == "Negro" && v.StockActual == 2);
        articulo.Variantes.Should().NotContain(v => v.Talle == "2" && v.Color == "Negro");
        articulo.Variantes.Should().NotContain(v => v.Talle == "4" && v.Color == "Blanco");
    }

    [Fact]
    public async Task CrearArticuloConMatriz_AdultosNumeros_GeneraVariantesCorrectas()
    {
        // Arrange: Remera o buzo con numeración de adultos 1 al 6 (CurvasTallesPresets.AdultosNumeros)
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "BUZ-HOOD",
            Nombre = "Buzo Hoodie Oversize",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 15000m,
            PrecioLista = 32000m,
            Talles = CurvasTallesPresets.AdultosNumeros.ToList(),
            Colores = new List<string> { "Gris" },
            StockInicialDefecto = 4
        };

        // Act
        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);

        // Assert
        articulo.Variantes.Should().HaveCount(6);
        articulo.Variantes.Select(v => v.Talle).Should().ContainInOrder("1", "2", "3", "4", "5", "6");
        articulo.Variantes.Select(v => v.SKU).Should().Contain(new[]
        {
            "BUZ-HOOD-1-GRIS",
            "BUZ-HOOD-2-GRIS",
            "BUZ-HOOD-3-GRIS",
            "BUZ-HOOD-4-GRIS",
            "BUZ-HOOD-5-GRIS",
            "BUZ-HOOD-6-GRIS"
        });
    }


    [Fact]
    public async Task AjustarStock_SalidaMayorAlStockDisponible_LanzaStockInsuficienteException()
    {
        // Arrange: Crear producto con 3 unidades
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "TOP-CROP",
            Nombre = "Top Crop Deportivo",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 3000m,
            PrecioLista = 7000m,
            Talles = new List<string> { "U" },
            Colores = new List<string> { "Negro" },
            StockInicialDefecto = 3
        };

        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);
        var variante = articulo.Variantes.First();

        var ajusteEgresoExcesivo = new AjustarStockDto
        {
            VarianteId = variante.Id,
            Cantidad = -5, // Intentar egresar 5 cuando solo hay 3
            Tipo = TipoMovimientoStock.MermaRotura,
            Motivo = "Mercadería rota"
        };

        // Act & Assert
        var accion = async () => await _inventarioService.AjustarStockAsync(ajusteEgresoExcesivo);
        await accion.Should().ThrowAsync<StockInsuficienteException>()
            .WithMessage("*Stock insuficiente*");
    }

    [Fact]
    public async Task AjustarStock_IngresoValido_ActualizaStockYRegistraMovimiento()
    {
        // Arrange: Artículo con stock inicial 2
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "CAL-RUN",
            Nombre = "Zapatilla Running Pro",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 20000m,
            PrecioLista = 45000m,
            Talles = new List<string> { "41" },
            Colores = new List<string> { "Gris" },
            StockInicialDefecto = 2
        };

        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);
        var variante = articulo.Variantes.First();

        var ajusteIngreso = new AjustarStockDto
        {
            VarianteId = variante.Id,
            Cantidad = +10, // Ingreso por compra
            Tipo = TipoMovimientoStock.EntradaCompra,
            Motivo = "Recepción de pedido proveedor",
            ReferenciaDocumento = "FAC-A-0001-999"
        };

        // Act
        var movimiento = await _inventarioService.AjustarStockAsync(ajusteIngreso);

        // Assert
        movimiento.StockPrevio.Should().Be(2);
        movimiento.Cantidad.Should().Be(10);
        movimiento.StockResultante.Should().Be(12);
        movimiento.CostoUnitario.Should().Be(20000m);

        // Verificar persistencia real en la base de datos
        var varianteActualizada = await _unitOfWork.Variantes.GetByIdAsync(variante.Id);
        varianteActualizada!.StockActual.Should().Be(12);
    }

    [Fact]
    public async Task EliminarVariante_SinVentas_EliminaFisicamenteDeBaseDeDatos()
    {
        // Arrange
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "TEST-BORRAR-01",
            Nombre = "Remera de Prueba a Borrar",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 1000m,
            PrecioLista = 3000m,
            Talles = new List<string> { "S" },
            Colores = new List<string> { "Rojo" },
            StockInicialDefecto = 1
        };

        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);
        var varianteId = articulo.Variantes.First().Id;

        // Act
        bool resultado = await _inventarioService.EliminarVarianteAsync(varianteId);

        // Assert
        resultado.Should().BeTrue(); // Eliminado físicamente
        var enDb = await _unitOfWork.Variantes.GetByIdAsync(varianteId);
        enDb.Should().BeNull();

        // Verificar que no aparezca en búsquedas
        var busqueda = await _inventarioService.BuscarVariantesAsync("TEST-BORRAR-01");
        busqueda.Should().BeEmpty();
    }

    [Fact]
    public async Task EliminarArticuloCompleto_EliminaArticuloYTodasSusVariantes()
    {
        // Arrange
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = "TEST-ART-TODO",
            Nombre = "Pantalón Todo a Borrar",
            CategoriaId = _categoriaPrueba.Id,
            MarcaId = _marcaPrueba.Id,
            PrecioCosto = 5000m,
            PrecioLista = 12000m,
            Talles = new List<string> { "38", "40" },
            Colores = new List<string> { "Negro" },
            StockInicialDefecto = 2
        };

        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto);
        var articuloId = articulo.Id;

        // Act
        int eliminadas = await _inventarioService.EliminarArticuloCompletoAsync(articuloId);

        // Assert
        eliminadas.Should().Be(2);

        var artEnDb = await _unitOfWork.Articulos.GetByIdAsync(articuloId);
        artEnDb.Should().BeNull();

        var busqueda = await _inventarioService.BuscarVariantesAsync("TEST-ART-TODO");
        busqueda.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
