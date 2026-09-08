namespace PuntoDeVenta.Domain.Entities.Proveedores;

public enum CondicionCompraProveedor
{
    ContadoEfectivo = 1,
    ContadoTransferencia = 2,
    CuentaCorrienteAPlazo = 3
}

public enum EstadoCompraProveedor
{
    Recibida = 1,
    PendientePago = 2,
    Pagada = 3,
    Cancelada = 4
}
