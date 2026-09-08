namespace PuntoDeVenta.Application.DTOs.Finanzas;

public class SaldoTeoricoTurnoDto
{
    public Guid TurnoId { get; set; }
    public decimal FondoInicial { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal GastosEfectivo { get; set; }
    public decimal MontoTeoricoEfectivo { get; set; }

    public decimal VentasTransferencias { get; set; }
    public decimal GastosTransferencias { get; set; }
    public decimal MontoTeoricoTransferencias { get; set; }

    public decimal VentasTarjetas { get; set; }
    public decimal GastosTarjetas { get; set; }
    public decimal MontoTeoricoTarjetas { get; set; }

    public decimal TotalTeorico => MontoTeoricoEfectivo + MontoTeoricoTransferencias + MontoTeoricoTarjetas;
}
