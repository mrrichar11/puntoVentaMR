namespace PuntoDeVenta.Application.DTOs.Finanzas;

public class ResumenCierreCajaDto
{
    public Guid TurnoCajaId { get; set; }
    public string UsuarioApertura { get; set; } = string.Empty;
    public string UsuarioCierre { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }

    public decimal MontoInicialEfectivo { get; set; }

    // Desglose Efectivo
    public decimal IngresosEfectivo { get; set; }
    public decimal EgresosEfectivoOperativos { get; set; }
    public decimal EgresosEfectivoRetiroPropietario { get; set; }
    public decimal MontoTeoricoEfectivo { get; set; }
    public decimal MontoRealEfectivo { get; set; }
    public decimal DiferenciaEfectivo { get; set; }

    // Desglose Transferencias / QR
    public decimal IngresosTransferencias { get; set; }
    public decimal EgresosTransferencias { get; set; }
    public decimal MontoTeoricoTransferencias { get; set; }
    public decimal MontoRealTransferencias { get; set; }
    public decimal DiferenciaTransferencias { get; set; }

    // Desglose Tarjetas (Débito + Crédito)
    public decimal IngresosTarjetas { get; set; }
    public decimal MontoTeoricoTarjetas { get; set; }
    public decimal MontoRealTarjetas { get; set; }
    public decimal DiferenciaTarjetas { get; set; }

    // Totales Consolidados
    public decimal TotalVentasTurno { get; set; }
    public decimal TotalGastosOperativosNegocio { get; set; }
    public decimal TotalRetirosPersonalesPropietario { get; set; }

    public decimal TotalTeorico { get; set; }
    public decimal TotalReal { get; set; }
    public decimal DiferenciaTotal { get; set; }

    public string? Observaciones { get; set; }
}
