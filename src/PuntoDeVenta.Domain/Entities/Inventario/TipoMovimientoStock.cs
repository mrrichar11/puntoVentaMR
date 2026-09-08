namespace PuntoDeVenta.Domain.Entities.Inventario;

public enum TipoMovimientoStock
{
    EntradaCompra = 1,
    Venta = 2,
    DevolucionCliente = 3,
    DevolucionProveedor = 4,
    AjusteManualPositivo = 5,
    AjusteManualNegativo = 6,
    MermaRotura = 7
}
