using FluentAssertions;
using PuntoDeVenta.Application.DTOs.Peripherals;
using PuntoDeVenta.Application.DTOs.Ventas;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Infrastructure.Peripherals;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class TicketPrinterServiceTests
{
    private readonly TicketPrinterService _ticketService = new();

    [Fact]
    public void EscPosBuilder_GeneraSecuenciasDeEscapeCorrectas()
    {
        // Act
        var bytes = new EscPosBuilder()
            .Initialize()
            .AlignCenter()
            .Bold()
            .Line("TIENDA DE MODA")
            .Bold(false)
            .AlignLeft()
            .Line("Item 1")
            .CutPaper()
            .Build();

        // Assert
        bytes.Should().NotBeEmpty();

        // Verifica ESC @ (0x1B, 0x40) de inicialización
        bytes[0].Should().Be(0x1B);
        bytes[1].Should().Be(0x40);

        // Verifica secuencia de corte de papel (0x1D, 0x56, 0x42, 0x00)
        bytes.Should().ContainInOrder(new byte[] { 0x1D, 0x56, 0x42, 0x00 });
    }

    [Fact]
    public void EscPosBuilder_GeneraPulsoCajonDineroCorrecto()
    {
        // Act: Generar pulso de apertura de cajón
        var bytes = new EscPosBuilder()
            .CashDrawerKick()
            .Build();

        // Assert: ESC p 0 25 250 (0x1B, 0x70, 0x00, 0x19, 0xFA)
        bytes.Should().BeEquivalentTo(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA });
    }

    [Fact]
    public async Task GenerarTicketArchivo_Ancho58mm_GeneraFormatoMonoespaciadoAlineado()
    {
        // Arrange
        var venta = CrearVentaEjemplo();
        var config = new ConfiguracionTicketDto
        {
            NombreFantasia = "OUTLET CALZADO",
            AnchoPapel = AnchoPapelTicket.Mm58, // 32 columnas
            GuardarCopiaEnArchivo = false
        };

        // Act
        var textoTicket = await _ticketService.GenerarTicketArchivoAsync(venta, config);

        // Assert
        textoTicket.Should().NotBeNullOrWhiteSpace();
        textoTicket.Should().Contain("OUTLET CALZADO");
        textoTicket.Should().Contain("T-0001-00000001");
        textoTicket.Should().Contain("Zapatilla Running Pro");
        textoTicket.Should().Contain("TOTAL COBRADO:");
        textoTicket.Should().Contain("POWERED BY [MR_SYS]");
        textoTicket.Should().Contain("mrsys.app");

        // Cada línea debe tener un ancho máximo razonable acorde al papel de 58mm
        var lineas = textoTicket.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lineas.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerarTicketArchivo_Ancho80mm_GeneraFormatoMonoespaciadoAlineado()
    {
        // Arrange
        var venta = CrearVentaEjemplo();
        var config = new ConfiguracionTicketDto
        {
            NombreFantasia = "BOUTIQUE CENTRAL",
            AnchoPapel = AnchoPapelTicket.Mm80, // 48 columnas
            GuardarCopiaEnArchivo = false
        };

        // Act
        var textoTicket = await _ticketService.GenerarTicketArchivoAsync(venta, config);

        // Assert
        textoTicket.Should().NotBeNullOrWhiteSpace();
        textoTicket.Should().Contain("BOUTIQUE CENTRAL");
        textoTicket.Should().Contain("$70.000,00");
    }

    [Fact]
    public async Task ImprimirTicketVenta_ModoArchivo_GuardaTicketEnDiscoExitosamente()
    {
        // Arrange
        var carpetaPrueba = Path.Combine(Path.GetTempPath(), "POS_Tickets_Tests_" + Guid.NewGuid().ToString("N"));
        var venta = CrearVentaEjemplo();
        var config = new ConfiguracionTicketDto
        {
            NombreFantasia = "TIENDA URBAN",
            NombreImpresoraWindows = null, // Sin impresora física configurada
            GuardarCopiaEnArchivo = true,
            RutaCarpetaTicketsArchivo = carpetaPrueba
        };

        try
        {
            // Act
            var resultado = await _ticketService.ImprimirTicketVentaAsync(venta, config);

            // Assert
            resultado.Should().BeTrue();

            var fechaCarpeta = venta.Fecha.ToString("yyyy-MM-dd");
            var rutaArchivoEsperada = Path.Combine(carpetaPrueba, fechaCarpeta, $"{venta.NumeroComprobante}.txt");

            File.Exists(rutaArchivoEsperada).Should().BeTrue();
            var contenido = await File.ReadAllTextAsync(rutaArchivoEsperada);
            contenido.Should().Contain("TIENDA URBAN");
            contenido.Should().Contain(venta.NumeroComprobante);
        }
        finally
        {
            if (Directory.Exists(carpetaPrueba))
            {
                Directory.Delete(carpetaPrueba, true);
            }
        }
    }

    private static VentaRealizadaDto CrearVentaEjemplo()
    {
        return new VentaRealizadaDto
        {
            VentaId = Guid.NewGuid(),
            NumeroComprobante = "T-0001-00000001",
            Fecha = new DateTime(2026, 9, 4, 15, 30, 0),
            ClienteNombre = "Sofia Gonzalez",
            SubtotalLista = 80000m,
            TotalDescuentoOferta = 10000m,
            TotalDescuentoMedioPago = 0m,
            TotalRecargoMedioPago = 0m,
            TotalFinalCobrado = 70000m,
            CostoTotalHistorico = 35000m,
            MargenBrutoReal = 35000m,
            PorcentajeMargenBrutoReal = 50.0m,
            Lineas = new List<LineaVentaResumenDto>
            {
                new LineaVentaResumenDto
                {
                    SKU = "ZAP-RUN-41-NEG",
                    Descripcion = "Zapatilla Running Pro",
                    Talle = "41",
                    Color = "Negro",
                    Cantidad = 1,
                    PrecioListaUnitario = 80000m,
                    DescuentoOfertaUnitario = 10000m,
                    PrecioFinalCobrado = 70000m,
                    CostoUnitarioHistorico = 35000m,
                    SubtotalCobrado = 70000m,
                    MargenBrutoReal = 35000m
                }
            },
            Pagos = new List<PagoResumenDto>
            {
                new PagoResumenDto
                {
                    Canal = CanalDinero.TransferenciaQR,
                    Monto = 70000m,
                    Referencia = "TR-MP-994411"
                }
            }
        };
    }
}
