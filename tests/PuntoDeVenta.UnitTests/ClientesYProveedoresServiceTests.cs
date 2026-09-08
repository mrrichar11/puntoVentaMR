using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Application.DTOs.Clientes;
using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Application.DTOs.Proveedores;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Proveedores;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Repositories;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class ClientesYProveedoresServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly ClienteService _clienteService;
    private readonly ProveedorService _proveedorService;
    private readonly CajaService _cajaService;

    public ClientesYProveedoresServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);
        _clienteService = new ClienteService(_unitOfWork);
        _proveedorService = new ProveedorService(_unitOfWork);
        _cajaService = new CajaService(_unitOfWork);
    }

    [Fact]
    public async Task CrearCliente_Y_RegistrarEntregaEnCaja_DisminuyeDeudaYRegistraMovimiento()
    {
        // 1. Crear clienta
        var nuevoCliente = await _clienteService.CrearClienteAsync(new ClienteDto
        {
            NombreCompleto = "Laura Gomez",
            Telefono = "11-2345-6789",
            LimiteCredito = 100000m
        });

        nuevoCliente.SaldoDeudorActual.Should().Be(0);

        // 2. Simular que tiene una deuda por compra en cuenta corriente
        nuevoCliente.SaldoDeudorActual = 35000m;
        await _unitOfWork.SaveChangesAsync();

        // 3. Abrir caja para poder recibir la entrega
        var turno = await _cajaService.AbrirTurnoAsync(new AbrirCajaDto
        {
            Usuario = "Cajera",
            MontoInicialEfectivo = 10000m
        });

        // 4. Registrar entrega de $15,000 en efectivo
        var mov = await _clienteService.RegistrarEntregaEnCajaAsync(new RegistrarEntregaCuentaCorrienteDto
        {
            ClienteId = nuevoCliente.Id,
            TurnoCajaId = turno.Id,
            Monto = 15000m,
            Canal = CanalDinero.Efectivo,
            Detalle = "Pago a cuenta de saldo",
            ReferenciaComprobante = "REC-001"
        });

        // Assert
        mov.SaldoResultante.Should().Be(20000m);
        
        var clienteActualizado = await _clienteService.ObtenerPorIdAsync(nuevoCliente.Id);
        clienteActualizado!.SaldoDeudorActual.Should().Be(20000m);

        // Verificar que ingresó un movimiento a la caja
        var movs = await _unitOfWork.MovimientosCaja.GetAllAsync();
        var movCobro = movs.FirstOrDefault(m => m.Concepto == ConceptoMovimientoCaja.CobroCuentaCorrienteCliente);
        movCobro.Should().NotBeNull();
        movCobro!.Monto.Should().Be(15000m);
        movCobro.Canal.Should().Be(CanalDinero.Efectivo);
    }

    [Fact]
    public async Task RegistrarCompraProveedor_AumentaStockYGeneraCuentaAPagar()
    {
        // 1. Crear Proveedor
        var prov = await _proveedorService.CrearProveedorAsync(new ProveedorDto
        {
            RazonSocial = "Textil Avellaneda Mayorista",
            Cuit = "30-55667788-9",
            PlazoPagoDiasDefecto = 30
        });

        // 2. Crear categoría, marca, artículo y variante de prueba
        var cat = new Categoria { Nombre = "Camisas" };
        var marca = new Marca { Nombre = "Zara" };
        await _unitOfWork.Categorias.AddAsync(cat);
        await _unitOfWork.Marcas.AddAsync(marca);

        var art = new Articulo
        {
            CodigoEstilo = "CAM-LINO",
            Nombre = "Camisa Lino",
            Categoria = cat,
            Marca = marca
        };
        await _unitOfWork.Articulos.AddAsync(art);

        var variante = new VarianteArticulo
        {
            Articulo = art,
            SKU = "CAM-LINO-BLA-M",
            Talle = "M",
            Color = "Blanco",
            PrecioCosto = 12000m,
            PrecioLista = 28000m,
            StockActual = 5,
            StockMinimo = 2
        };
        await _unitOfWork.Variantes.AddAsync(variante);
        await _unitOfWork.SaveChangesAsync();

        // 3. Registrar Remito/Factura de Compra a plazo (Cuenta corriente proveedor)
        var dto = new RegistrarCompraDto
        {
            ProveedorId = prov.Id,
            NumeroComprobante = "FC-0001-00029",
            Condicion = CondicionCompraProveedor.CuentaCorrienteAPlazo,
            PlazoDias = 30,
            Lineas = new List<LineaCompraDto>
            {
                new LineaCompraDto
                {
                    VarianteId = variante.Id,
                    Cantidad = 20,
                    CostoUnitarioCompra = 14000m,
                    NuevoPrecioLista = 32000m
                }
            }
        };

        var compra = await _proveedorService.RegistrarCompraAsync(dto);

        // Assert
        compra.Should().NotBeNull();
        compra.TotalCompra.Should().Be(20 * 14000m);
        compra.SaldoPendiente.Should().Be(280000m);
        compra.Estado.Should().Be(EstadoCompraProveedor.PendientePago);

        // El stock debe haberse incrementado inmediatamente: 5 + 20 = 25
        var varianteDb = await _unitOfWork.Variantes.GetByIdAsync(variante.Id);
        varianteDb!.StockActual.Should().Be(25);
        varianteDb.PrecioCosto.Should().Be(14000m);
        varianteDb.PrecioLista.Should().Be(32000m);

        // Comprobar historial de cuentas a pagar
        var comprasProveedor = await _proveedorService.ObtenerHistorialComprasAsync(prov.Id);
        comprasProveedor.Should().HaveCount(1);
        comprasProveedor[0].SaldoPendiente.Should().Be(280000m);
    }

    [Fact]
    public async Task CrearCliente_ConSaldoInicial_RegistraMovimientoInicialYActualizaSaldo()
    {
        var nuevo = await _clienteService.CrearClienteAsync(new ClienteDto
        {
            NombreCompleto = "Ana Martínez",
            Telefono = "11-9876-5432",
            SaldoInicial = 18500m,
            DetalleSaldoInicial = "Libreta vieja año anterior"
        });

        nuevo.SaldoDeudorActual.Should().Be(18500m);

        var historial = await _clienteService.ObtenerHistorialCuentaCorrienteAsync(nuevo.Id);
        historial.Should().HaveCount(1);
        historial[0].Monto.Should().Be(18500m);
        historial[0].SaldoResultante.Should().Be(18500m);
        historial[0].Tipo.Should().Be(PuntoDeVenta.Domain.Entities.Clientes.TipoMovimientoCuentaCorriente.SaldoInicial.ToString());
        historial[0].Detalle.Should().Contain("Libreta vieja");
    }

    [Fact]
    public async Task RegistrarSaldoPrevioHistorico_SumaDeudaSinAfectarCaja()
    {
        var cliente = await _clienteService.CrearClienteAsync(new ClienteDto
        {
            NombreCompleto = "Beatriz Rossi",
            Telefono = "11-5555-4444"
        });
        cliente.SaldoDeudorActual.Should().Be(0);

        var mov = await _clienteService.RegistrarSaldoPrevioHistoricoAsync(new RegistrarSaldoPrevioClienteDto
        {
            ClienteId = cliente.Id,
            MontoDeudaPrevia = 25000m,
            Detalle = "Deuda encontrada en libreta física",
            FechaHistorica = DateTime.UtcNow.AddMonths(-1)
        });

        mov.SaldoResultante.Should().Be(25000m);

        var dbCliente = await _clienteService.ObtenerPorIdAsync(cliente.Id);
        dbCliente!.SaldoDeudorActual.Should().Be(25000m);

        // Verificar que NO afectó ningún movimiento de caja
        var movsCaja = await _unitOfWork.MovimientosCaja.GetAllAsync();
        movsCaja.Should().BeEmpty();
    }

    [Fact]
    public async Task CrearProveedor_ConSaldoInicial_CreaCompraPendientePagoYRegistraNotas()
    {
        var prov = await _proveedorService.CrearProveedorAsync(new ProveedorDto
        {
            RazonSocial = "Confecciones del Norte",
            Cuit = "30-11223344-5",
            Notas = "Alias CBU: confecciones.norte - Vendedor: Carlos",
            SaldoInicial = 150000m,
            DetalleSaldoInicial = "Factura B 0001-445 impaga pre-sistema",
            NumeroComprobanteSaldoInicial = "FC-ANT-445"
        });

        prov.Notas.Should().Be("Alias CBU: confecciones.norte - Vendedor: Carlos");

        var compras = await _proveedorService.ObtenerHistorialComprasAsync(prov.Id);
        compras.Should().HaveCount(1);
        compras[0].NumeroComprobante.Should().Be("FC-ANT-445");
        compras[0].TotalCompra.Should().Be(150000m);
        compras[0].SaldoPendiente.Should().Be(150000m);
        compras[0].Estado.Should().Be(EstadoCompraProveedor.PendientePago.ToString());
    }

    [Fact]
    public async Task RegistrarDeudaPreviaProveedor_CreaFacturaPendientePagoSinModificarStock()
    {
        var prov = await _proveedorService.CrearProveedorAsync(new ProveedorDto
        {
            RazonSocial = "Indumentaria Mayorista Sur"
        });

        var compra = await _proveedorService.RegistrarDeudaPreviaAsync(new RegistrarDeudaPreviaProveedorDto
        {
            ProveedorId = prov.Id,
            MontoDeudaPrevia = 95000m,
            NumeroFacturaComprobante = "FAC-PREV-882",
            DetalleObservaciones = "Factura de telas mes anterior",
            FechaComprobante = DateTime.UtcNow.AddDays(-20)
        });

        compra.Should().NotBeNull();
        compra.NumeroComprobante.Should().Be("FAC-PREV-882");
        compra.TotalCompra.Should().Be(95000m);
        compra.SaldoPendiente.Should().Be(95000m);
        compra.Estado.Should().Be(EstadoCompraProveedor.PendientePago);

        var compras = await _proveedorService.ObtenerHistorialComprasAsync(prov.Id);
        compras.Should().HaveCount(1);

        // El inventario no debe tener variantes creadas por esta deuda
        var variantes = await _unitOfWork.Variantes.GetAllAsync();
        variantes.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
