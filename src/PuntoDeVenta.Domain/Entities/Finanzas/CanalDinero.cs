namespace PuntoDeVenta.Domain.Entities.Finanzas;

/// <summary>
/// Canal o medio por el cual fluye el dinero en el comercio,
/// permitiendo el control y arqueo tanto de caja física como de acreditaciones bancarias.
/// </summary>
public enum CanalDinero
{
    Efectivo = 1,
    TransferenciaQR = 2,
    TarjetaDebito = 3,
    TarjetaCredito = 4,
    Otro = 5,
    CuentaCorriente = 6
}
