using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Ventas;

namespace PuntoDeVenta.Application.Services;

public interface ICalculadorPreciosService
{
    LineaVenta CalcularLineaVenta(VarianteArticulo variante, int cantidad, MetodoPago? metodoPago = null, Guid? ventaId = null);
}

/// <summary>
/// Motor de cálculo comercial de precios, ofertas, descuentos por medio de pago y congelamiento de costos.
/// </summary>
public class CalculadorPreciosService : ICalculadorPreciosService
{
    public LineaVenta CalcularLineaVenta(VarianteArticulo variante, int cantidad, MetodoPago? metodoPago = null, Guid? ventaId = null)
    {
        ArgumentNullException.ThrowIfNull(variante);
        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(cantidad));
        }

        var precioLista = variante.PrecioLista;
        decimal descuentoOferta = 0m;
        decimal baseParaMedioPago = precioLista;

        // 1. Evaluación de liquidación / Sale
        if (variante.EsEnOferta)
        {
            descuentoOferta = precioLista - variante.PrecioOferta!.Value;
            baseParaMedioPago = variante.PrecioOferta.Value;
        }

        decimal descuentoMedioPago = 0m;
        decimal recargoMedioPago = 0m;

        // 2. Evaluación de ajuste por medio de pago
        if (metodoPago != null && metodoPago.PorcentajeAjuste != 0m)
        {
            if (metodoPago.PorcentajeAjuste < 0) // Descuento / Bonificación (ej. -10% efectivo)
            {
                // Regla comercial: solo aplica descuento si el ítem es elegible
                // (precio regular o ítem en oferta con PermiteDescuentoMedioPago activo)
                if (variante.EsElegibleParaDescuentoMedioPago)
                {
                    var porcentajeDescuento = Math.Abs(metodoPago.PorcentajeAjuste);
                    descuentoMedioPago = Math.Round((baseParaMedioPago * porcentajeDescuento) / 100m, 2);
                }
            }
            else // Recargo comercial (ej. +15% cuotas)
            {
                recargoMedioPago = Math.Round((baseParaMedioPago * metodoPago.PorcentajeAjuste) / 100m, 2);
            }
        }

        var precioFinalCobrado = Math.Round(baseParaMedioPago - descuentoMedioPago + recargoMedioPago, 2);

        // Snapshot descriptivo
        var nombreArticulo = variante.Articulo?.Nombre ?? "Artículo";
        var descripcionCompleta = $"{nombreArticulo} - Talle: {variante.Talle} - Color: {variante.Color}";

        return new LineaVenta
        {
            VentaId = ventaId ?? Guid.Empty,
            VarianteId = variante.Id,
            Variante = variante,
            DescripcionArticulo = descripcionCompleta,
            SKU = variante.SKU,
            Talle = variante.Talle,
            Color = variante.Color,
            Cantidad = cantidad,
            PrecioListaUnitario = precioLista,
            DescuentoOfertaUnitario = descuentoOferta,
            DescuentoMedioPagoUnitario = descuentoMedioPago,
            RecargoMedioPagoUnitario = recargoMedioPago,
            PrecioFinalCobrado = precioFinalCobrado,
            CostoUnitarioHistorico = variante.PrecioCosto // ¡Costo congelado inmutable!
        };
    }
}
