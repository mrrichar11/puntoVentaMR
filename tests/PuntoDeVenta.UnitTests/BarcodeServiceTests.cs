using FluentAssertions;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class BarcodeServiceTests
{
    private readonly BarcodeService _service = new();

    [Fact]
    public void SugerirSku_NombreLimpio_GeneraPrefijoValido()
    {
        var skuConDetalles = _service.SugerirSku("Remera Algodón Peinado", "Hombre", "Zara");
        skuConDetalles.Should().Be("HOM-REME-ALGO-PEIN");

        var skuSimple = _service.SugerirSku("Remera Algodón Peinado", null, null);
        skuSimple.Should().Be("REME-ALGO-PEIN");
    }

    [Fact]
    public void SugerirSku_NombreVacio_GeneraPrefijoPorDefecto()
    {
        var sku = _service.SugerirSku("", null, null);
        sku.Should().StartWith("ART-");
    }

    [Fact]
    public void SugerirSkuVariante_CombinaEstiloTalleYColor()
    {
        var skuVariante = _service.SugerirSkuVariante("JEAN-SLIM", "42", "Azul Marino");
        skuVariante.Should().Be("JEAN-SLIM-42-AZUL");
    }

    [Fact]
    public void GenerarEan13Interno_ProduceCodigo13DigitosValido()
    {
        var ean = _service.GenerarEan13Interno(12345);

        ean.Should().HaveLength(13);
        ean.Should().StartWith("20");
        _service.ValidarEan13(ean).Should().BeTrue();
    }

    [Fact]
    public void ValidarEan13_ConCodigoInvalido_RetornaFalse()
    {
        _service.ValidarEan13("123").Should().BeFalse();
        _service.ValidarEan13("2000000123459").Should().BeFalse(); // Checksum incorrecto
        _service.ValidarEan13("ABCDEFGHIJKLM").Should().BeFalse();
    }

    [Fact]
    public void GenerarPatronBarras_Ean13_ProduceSecuenciaBinariaValida()
    {
        var ean = _service.GenerarEan13Interno(99);
        var patron = _service.GenerarPatronBarras(ean);

        patron.Should().NotBeNullOrWhiteSpace();
        patron.Should().MatchRegex("^[01]+$");
        // Patrón EAN-13 estándar: 95 módulos de código + 14 módulos de quiet zones laterales para lectura térmica
        patron.Length.Should().Be(109);
    }

    [Fact]
    public void GenerarPatronBarras_Code128_ProduceSecuenciaBinariaValida()
    {
        var patron = _service.GenerarPatronBarras("REM-001");

        patron.Should().NotBeNullOrWhiteSpace();
        patron.Should().MatchRegex("^[01]+$");
    }
}
