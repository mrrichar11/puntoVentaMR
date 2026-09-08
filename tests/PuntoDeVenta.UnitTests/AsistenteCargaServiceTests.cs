using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Asistente;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class AsistenteCargaServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly InventarioService _inventarioService;
    private readonly ConfiguracionService _configuracionService;
    private readonly AsistenteCargaService _asistenteService;

    public AsistenteCargaServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _inventarioService = new InventarioService(_unitOfWork, new BarcodeService());
        _configuracionService = new ConfiguracionService(_unitOfWork);
        _asistenteService = new AsistenteCargaService(_inventarioService, _unitOfWork, _configuracionService);
    }

    [Fact]
    public async Task ProcesarMensaje_FraseCompletaRemeras_GeneraBorradorListoParaConfirmar()
    {
        // Arrange
        string entrada = "Compré 4 remeras básicas, talles 3 y 4 color blanca, costo $6000 y venta $14000";

        // Act
        var respuesta = await _asistenteService.ProcesarMensajeAsync(entrada);

        // Assert
        respuesta.Should().NotBeNull();
        respuesta.Borrador.Should().NotBeNull();
        respuesta.EsBorradorListoParaConfirmar.Should().BeTrue();

        var b = respuesta.Borrador!;
        b.NombreArticulo.Should().Contain("Remera");
        b.Talles.Should().Contain(new[] { "3", "4" });
        b.Colores.Should().Contain("Blanco");
        b.PrecioCosto.Should().Be(6000m);
        b.PrecioLista.Should().Be(14000m);
        b.Variantes.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcesarMensaje_FraseSinPrecios_PideDatosFaltantesYPermiteCompletar()
    {
        // 1. Usuario ingresa prenda, talles y color pero sin costos
        string entrada1 = "Cargame 6 remeras oversize marca Nike color negro y gris talles S, M, L";
        var r1 = await _asistenteService.ProcesarMensajeAsync(entrada1);

        r1.EsBorradorListoParaConfirmar.Should().BeFalse();
        r1.Borrador.Should().NotBeNull();
        r1.Borrador!.DatosFaltantes.Should().Contain(f => f.Contains("costo") || f.Contains("venta"));

        // 2. Usuario responde con los costos faltantes
        string entrada2 = "Costo $8000 venta $18000";
        var r2 = await _asistenteService.ProcesarMensajeAsync(entrada2, r1.Borrador);

        r2.EsBorradorListoParaConfirmar.Should().BeTrue();
        var b = r2.Borrador!;
        b.PrecioCosto.Should().Be(8000m);
        b.PrecioLista.Should().Be(18000m);
        b.MarcaNombre.Should().Be("Nike");
        b.Talles.Should().HaveCount(3); // S, M, L
        b.Colores.Should().HaveCount(2); // Negro, Gris
        b.Variantes.Should().HaveCount(6); // 3x2 = 6 variantes
    }

    [Fact]
    public async Task ConfirmarBorrador_ImpactaArticuloYVariantesEnBaseDeDatos()
    {
        // Arrange
        string entrada = "Ingresé 5 jeans mom talle 38 y 40 color azul, costo $15000, venta $32000";
        var respuesta = await _asistenteService.ProcesarMensajeAsync(entrada);

        respuesta.EsBorradorListoParaConfirmar.Should().BeTrue();

        // Act
        int creadas = await _asistenteService.ConfirmarBorradorAsync(respuesta.Borrador!);

        // Assert
        creadas.Should().Be(2); // Talles 38 y 40 x Azul = 2

        var articuloEnDb = await _context.Articulos
            .Include(a => a.Variantes)
            .FirstOrDefaultAsync(a => a.Nombre.Contains("Jean"));

        articuloEnDb.Should().NotBeNull();
        articuloEnDb!.Variantes.Should().HaveCount(2);
        articuloEnDb.Variantes.All(v => v.PrecioCosto == 15000m && v.PrecioLista == 32000m).Should().BeTrue();
    }

    [Fact]
    public async Task ProcesarMensaje_FraseCompuestaRemerasYJeans_SegmentaCorrectamenteYSinAsteriscos()
    {
        // Arrange
        string entrada = "Compre 2 remeras blancas talles 2 y 4 a 7000 pesos y 5 pantalones de Jeans con brillos talles 40, 42, 44, 46 y 48, uno de cada talle";

        // Act
        var respuesta = await _asistenteService.ProcesarMensajeAsync(entrada);

        // Assert
        respuesta.Should().NotBeNull();
        respuesta.Texto.Should().NotContain("**"); // Sin formato markdown crudo
        respuesta.Borradores.Should().HaveCount(2);

        // Borrador 1: Remera Blanca
        var b1 = respuesta.Borradores[0];
        b1.NombreArticulo.Should().Contain("Remera");
        b1.Talles.Should().Equal(new[] { "2", "4" });
        b1.Colores.Should().Contain("Blanco");
        b1.PrecioCosto.Should().Be(7000m);
        b1.PrecioLista.Should().BeGreaterThan(7000m); // Venta sugerida
        b1.EsValidoParaGuardar.Should().BeTrue();

        // Borrador 2: Jeans con brillos
        var b2 = respuesta.Borradores[1];
        b2.NombreArticulo.Should().Contain("Jean");
        b2.Talles.Should().Equal(new[] { "40", "42", "44", "46", "48" });
        b2.Talles.Should().NotContain("1"); // 'uno de cada talle' no debe ser talle 1
        b2.Talles.Should().NotContain("5"); // '5 pantalones' no debe ser talle 5
        b2.CantidadPorVariante.Should().Be(1);
    }

    [Fact]
    public async Task ProcesarMensaje_MultiplesProductos_ConfirmarTodosGuardaEnBaseDeDatos()
    {
        // Arrange
        string entrada = "2 remeras blancas talles 1 y 2 costo 5000 venta 10000 y 2 buzos negros talles S y M costo 12000 venta 25000";
        var respuesta = await _asistenteService.ProcesarMensajeAsync(entrada);

        respuesta.Borradores.Should().HaveCount(2);
        respuesta.Borradores.All(b => b.EsValidoParaGuardar).Should().BeTrue();

        // Act
        int totalVariantes = await _asistenteService.ConfirmarTodosAsync(respuesta.Borradores);

        // Assert
        totalVariantes.Should().Be(4); // 2 remeras + 2 buzos

        var totalArticulosDb = await _context.Articulos.CountAsync();
        totalArticulosDb.Should().Be(2);
    }

    [Fact]
    public async Task ProcesarMensaje_MensajeSeguimientoPrecioCostoColoquial_CompletaBorradorPendiente()
    {
        // 1. Mensaje inicial con remeras y jeans
        string entradaInicial = "Compre 2 remeras blancas talles 2 y 4 a 7000 pesos y 5 pantalones de Jeans con brillos talles 40, 42, 44, 46 y 48, uno de cada talle";
        var r1 = await _asistenteService.ProcesarMensajeAsync(entradaInicial);

        r1.Borradores.Should().HaveCount(2);
        var jeanPendiente = r1.Borradores[1];
        jeanPendiente.EsValidoParaGuardar.Should().BeFalse();

        // 2. Usuario responde con frase coloquial: "el precio de costo de los jeans es de 20500 y el color es unico, celeste"
        string entradaSeguimiento = "el precio de costo de los jeans es de 20500 y el color es unico, celeste";
        var r2 = await _asistenteService.ProcesarMensajeAsync(entradaSeguimiento, borradoresExistentes: r1.Borradores);

        r2.Borradores.Should().HaveCount(2);
        var jeanCompletado = r2.Borradores[1];
        jeanCompletado.PrecioCosto.Should().Be(20500m);
        jeanCompletado.PrecioLista.Should().Be(36900m); // 20500 * 1.80 sugerido
        jeanCompletado.Colores.Should().Contain("Celeste");
        jeanCompletado.EsValidoParaGuardar.Should().BeTrue();
        jeanCompletado.Variantes.Should().HaveCount(5); // 5 talles x 1 color
    }

    [Fact]
    public void MensajeChatDto_IdentificacionMotor_MuestraRemitenteYColorApropiados()
    {
        var msgGemini = new MensajeChatDto { Rol = RolMensajeChat.Asistente, MotorUtilizado = "Gemini" };
        msgGemini.NombreRemitente.Should().Contain("✨ IA");
        msgGemini.NombreRemitente.Should().NotContain("Gemini");
        msgGemini.ColorRemitente.Should().Be("#00E5FF");
        msgGemini.ColorBorde.Should().Be("#00E5FF");

        var msgLocal = new MensajeChatDto { Rol = RolMensajeChat.Asistente, MotorUtilizado = "Local" };
        msgLocal.NombreRemitente.Should().Contain("⚡ Motor Local");
        msgLocal.ColorRemitente.Should().Be("#00FF66");
        msgLocal.ColorBorde.Should().Be("#00FF66");

        var msgFallback = new MensajeChatDto { Rol = RolMensajeChat.Asistente, MotorUtilizado = "LocalFallback" };
        msgFallback.NombreRemitente.Should().Contain("⚡ Motor Local (Fallback)");
        msgFallback.ColorRemitente.Should().Be("#FFB74D");
        msgFallback.ColorBorde.Should().Be("#FFB74D");
    }

    [Fact]
    public async Task GeminiService_EntradaJeans_GeneraCincoTallesYRespetaMargen()
    {
        string rutaKey = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "publish", "gemini_key.txt");
        if (!File.Exists(rutaKey))
        {
            rutaKey = @"C:\Proyectos\PuntoDeVenta\publish\gemini_key.txt";
        }
        if (!File.Exists(rutaKey))
        {
            throw new FileNotFoundException($"No se encontró la key en: {rutaKey}");
        }

        string key = File.ReadAllText(rutaKey).Trim();
        var gemini = new PuntoDeVenta.Infrastructure.Services.GeminiService(apiKey: key);

        string entrada = "Compre 5 Jeans, uno de cada talle, partiendo del 40 color celestes, y los pague 33000 cada uno, me sugieres un precio tambien";
        var resultado = await gemini.InterpretarEntradaConGeminiAsync(entrada);

        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(1);
        var jean = resultado![0];
        jean.Talles.Should().HaveCount(5);
        jean.PrecioCosto.Should().Be(33000m);
        jean.PrecioLista.Should().Be(59400m);
        jean.Variantes.Should().HaveCount(5);
    }

    [Fact]
    public async Task ProcesarMensaje_DeudaLibretaLocal_DetectaIntencionYGeneraBorradorDeuda()
    {
        // Arrange
        string entrada = "Anotale a Laura Benítez 15000 de la libreta";

        // Act
        var respuesta = await _asistenteService.ProcesarMensajeAsync(entrada);

        // Assert
        respuesta.Should().NotBeNull();
        respuesta.TipoAccion.Should().Be(TipoAccionAsistente.RegistrarDeudaCliente);
        respuesta.BorradorDeudaCliente.Should().NotBeNull();
        respuesta.BorradorDeudaCliente!.NombreCliente.Should().Contain("Laura Benítez");
        respuesta.BorradorDeudaCliente!.Monto.Should().Be(15000m);
    }

    [Fact]
    public async Task ProcesarMensaje_VentaCtaCorriente_SinMonto_PideMontoYSegundoTurnoLoCompleta()
    {
        // 1. Usuario indica venta fiada en cta corriente sin precio
        string entrada1 = "le vendi a andrea colman un remera basica negra talle 4 en cta corriente";
        var r1 = await _asistenteService.ProcesarMensajeAsync(entrada1);

        r1.Should().NotBeNull();
        r1.TipoAccion.Should().Be(TipoAccionAsistente.RegistrarDeudaCliente);
        r1.BorradorDeudaCliente.Should().NotBeNull();
        r1.BorradorDeudaCliente!.NombreCliente.Should().Contain("andrea colman");
        r1.EsBorradorListoParaConfirmar.Should().BeFalse();
        r1.Texto.Should().Contain("importe");

        // 2. En el segundo turno el usuario responde con el importe
        string entrada2 = "$14.500";
        var r2 = await _asistenteService.ProcesarMensajeAsync(entrada2, deudaClientePendiente: r1.BorradorDeudaCliente);

        r2.Should().NotBeNull();
        r2.TipoAccion.Should().Be(TipoAccionAsistente.RegistrarDeudaCliente);
        r2.EsBorradorListoParaConfirmar.Should().BeTrue();
        r2.BorradorDeudaCliente.Should().NotBeNull();
        r2.BorradorDeudaCliente!.Monto.Should().Be(14500m);
        r2.BorradorDeudaCliente!.NombreCliente.Should().Contain("andrea colman");
    }

    [Fact]
    public async Task ProcesarMensaje_VentaCtaCorriente_ConArticuloEnDb_ObtienePrecioAutomaticamente()
    {
        // Arrange: Crear artículo y variante en la base de datos de prueba
        var cat = new Categoria { Nombre = "Remeras" };
        var marca = new Marca { Nombre = "Genérica" };
        await _unitOfWork.Categorias.AddAsync(cat);
        await _unitOfWork.Marcas.AddAsync(marca);
        await _unitOfWork.SaveChangesAsync();

        var art = new Articulo
        {
            Nombre = "Remera básica",
            CategoriaId = cat.Id,
            MarcaId = marca.Id
        };
        await _unitOfWork.Articulos.AddAsync(art);
        await _unitOfWork.SaveChangesAsync();

        var varArt = new VarianteArticulo
        {
            ArticuloId = art.Id,
            SKU = "REM-BAS-4-NEG",
            Talle = "4",
            Color = "Negro",
            PrecioCosto = 6000m,
            PrecioLista = 14000m,
            StockActual = 10
        };
        await _unitOfWork.Variantes.AddAsync(varArt);
        await _unitOfWork.SaveChangesAsync();

        string entrada = "le vendi a andrea colman un remera basica negra talle 4 en cta corriente";

        // Act
        var respuesta = await _asistenteService.ProcesarMensajeAsync(entrada);

        // Assert
        respuesta.Should().NotBeNull();
        respuesta.TipoAccion.Should().Be(TipoAccionAsistente.RegistrarDeudaCliente);
        respuesta.BorradorDeudaCliente.Should().NotBeNull();
        respuesta.BorradorDeudaCliente!.Monto.Should().Be(14000m);
        respuesta.BorradorDeudaCliente!.NombreCliente.Should().Contain("andrea colman");
        respuesta.EsBorradorListoParaConfirmar.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
