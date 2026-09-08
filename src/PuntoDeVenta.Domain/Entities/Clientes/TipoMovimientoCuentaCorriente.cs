namespace PuntoDeVenta.Domain.Entities.Clientes;

public enum TipoMovimientoCuentaCorriente
{
    CargoVenta = 1,     // Se añade deuda por una compra a cuenta
    EntregaPago = 2,    // Se reduce deuda por un pago/entrega de dinero
    AjusteManual = 3,   // Corrección manual de saldo
    SaldoInicial = 4    // Deuda previa / saldo inicial histórico anterior al sistema
}
