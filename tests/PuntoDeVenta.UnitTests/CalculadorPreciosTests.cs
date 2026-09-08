using FluentAssertions;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Ventas;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class CalculadorPreciosTests
{
    private readonly CalculadorPreciosService _calculador = new();

    [Fact]
    public void CalcularLineaVenta_ArticuloRegularConDescuentoEfectivo_AplicaDescuentoCompleto()
    {
        // Arrange: Remera de algodón a precio lista $10,000, costo $4,000
        var variante = new VarianteArticulo
        {
            SKU = "REM-BAS-M-NEG",
            Talle = "M",
            Color = "Negro",
            PrecioCosto = 4000m,
            PrecioLista = 10000m,
            PrecioOferta = null,
            StockActual = 10
        };

        var pagoEfectivo = new MetodoPago
        {
            Nombre = "Efectivo",
            PorcentajeAjuste = -10.0m // 10% de descuento
        };

        // Act
        var linea = _calculador.CalcularLineaVenta(variante, cantidad: 2, metodoPago: pagoEfectivo);

        // Assert
        linea.PrecioListaUnitario.Should().Be(10000m);
        linea.DescuentoOfertaUnitario.Should().Be(0m);
        linea.DescuentoMedioPagoUnitario.Should().Be(1000m); // 10% de $10,000
        linea.PrecioFinalCobrado.Should().Be(9000m);
        linea.CostoUnitarioHistorico.Should().Be(4000m);

        // Totales de línea (2 unidades)
        linea.SubtotalCobrado.Should().Be(18000m); // 9,000 * 2
        linea.CostoTotalHistorico.Should().Be(8000m); // 4,000 * 2
        linea.MargenBrutoReal.Should().Be(10000m); // 18,000 - 8,000
        linea.PorcentajeMargenBrutoReal.Should().Be(55.56m); // (10,000 / 18,000) * 100
    }

    [Fact]
    public void CalcularLineaVenta_ArticuloEnOfertaSinAcumulacion_NoAplicaDescuentoMedioPago()
    {
        // Arrange: Zapatilla en liquidación $25,000 (Lista $35,000), NO acumula descuento efectivo
        var variante = new VarianteArticulo
        {
            SKU = "ZAP-RUN-41-AZU",
            Talle = "41",
            Color = "Azul",
            PrecioCosto = 15000m,
            PrecioLista = 35000m,
            PrecioOferta = 25000m,
            PermiteDescuentoMedioPago = false, // Liquidación cerrada
            StockActual = 5
        };

        var pagoEfectivo = new MetodoPago
        {
            Nombre = "Efectivo",
            PorcentajeAjuste = -10.0m // 10% de descuento en efectivo
        };

        // Act
        var linea = _calculador.CalcularLineaVenta(variante, cantidad: 1, metodoPago: pagoEfectivo);

        // Assert
        linea.PrecioListaUnitario.Should().Be(35000m);
        linea.DescuentoOfertaUnitario.Should().Be(10000m); // 35,000 - 25,000
        linea.DescuentoMedioPagoUnitario.Should().Be(0m); // ¡Regla clave: no acumula!
        linea.PrecioFinalCobrado.Should().Be(25000m);
        linea.CostoUnitarioHistorico.Should().Be(15000m);
        linea.MargenBrutoReal.Should().Be(10000m); // 25,000 - 15,000
    }

    [Fact]
    public void CalcularLineaVenta_ArticuloEnOfertaConAcumulacionPermitida_AplicaDescuentoSobrePrecioOferta()
    {
        // Arrange: Campera de liquidación especial que sí permite 10% extra en efectivo
        var variante = new VarianteArticulo
        {
            SKU = "CAM-WTR-L-NEG",
            Talle = "L",
            Color = "Negro",
            PrecioCosto = 20000m,
            PrecioLista = 50000m,
            PrecioOferta = 40000m,
            PermiteDescuentoMedioPago = true, // Acumulación permitida
            StockActual = 3
        };

        var pagoEfectivo = new MetodoPago
        {
            Nombre = "Efectivo",
            PorcentajeAjuste = -10.0m
        };

        // Act
        var linea = _calculador.CalcularLineaVenta(variante, cantidad: 1, metodoPago: pagoEfectivo);

        // Assert
        linea.PrecioListaUnitario.Should().Be(50000m);
        linea.DescuentoOfertaUnitario.Should().Be(10000m); // 50,000 - 40,000
        linea.DescuentoMedioPagoUnitario.Should().Be(4000m); // 10% de $40,000
        linea.PrecioFinalCobrado.Should().Be(36000m); // 40,000 - 4,000
        linea.CostoUnitarioHistorico.Should().Be(20000m);
        linea.MargenBrutoReal.Should().Be(16000m); // 36,000 - 20,000
    }

    [Fact]
    public void CalcularLineaVenta_RecargoTarjetaCreditoCuotas_AumentaPrecioSegunPorcentaje()
    {
        // Arrange: Artículo regular con recargo de 15% por financiación
        var variante = new VarianteArticulo
        {
            SKU = "PAN-JNS-42-AZU",
            Talle = "42",
            Color = "Azul",
            PrecioCosto = 8000m,
            PrecioLista = 20000m,
            StockActual = 8
        };

        var pagoCuotas = new MetodoPago
        {
            Nombre = "Tarjeta Crédito 3 Cuotas",
            PorcentajeAjuste = 15.0m // 15% de recargo
        };

        // Act
        var linea = _calculador.CalcularLineaVenta(variante, cantidad: 1, metodoPago: pagoCuotas);

        // Assert
        linea.PrecioListaUnitario.Should().Be(20000m);
        linea.DescuentoOfertaUnitario.Should().Be(0m);
        linea.DescuentoMedioPagoUnitario.Should().Be(0m);
        linea.RecargoMedioPagoUnitario.Should().Be(3000m); // 15% de 20,000
        linea.PrecioFinalCobrado.Should().Be(23000m);
        linea.CostoUnitarioHistorico.Should().Be(8000m);
        linea.MargenBrutoReal.Should().Be(15000m); // 23,000 - 8,000
    }
}
