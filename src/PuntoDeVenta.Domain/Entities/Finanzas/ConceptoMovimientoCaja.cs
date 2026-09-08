namespace PuntoDeVenta.Domain.Entities.Finanzas;

public enum ConceptoMovimientoCaja
{
    FondoInicialApertura = 1,
    Venta = 2,
    GastoOperativo = 3,
    RetiroPropietario = 4, // Sueldo emprendedor / retiros particulares
    AporteCapital = 5,
    AjusteManualSobrante = 6,
    AjusteManualFaltante = 7,
    CobroCuentaCorrienteCliente = 8,
    PagoAProveedor = 9
}
