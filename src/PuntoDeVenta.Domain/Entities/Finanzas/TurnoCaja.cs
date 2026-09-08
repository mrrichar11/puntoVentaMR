using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Finanzas;

/// <summary>
/// Sesión de Turno de Caja con Arqueo y Cierre Ciego Multicanal
/// (Efectivo físico en mostrador, Transferencias bancarias/QR y Cupones de Tarjetas).
/// </summary>
public class TurnoCaja : BaseEntity
{
    public string UsuarioApertura { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fondo de cambio o sencillo inicial en efectivo físico.
    /// </summary>
    public decimal MontoInicialEfectivo { get; set; }

    public DateTime? FechaCierre { get; set; }
    public string? UsuarioCierre { get; set; }
    public EstadoTurnoCaja Estado { get; set; } = EstadoTurnoCaja.Abierto;

    public string? ObservacionesApertura { get; set; }
    public string? ObservacionesCierre { get; set; }

    // --- ARQUEO Y CIERRE CIEGO: EFECTIVO FÍSICO ---
    public decimal? MontoRealEfectivo { get; set; }
    public decimal? MontoTeoricoEfectivo { get; set; }
    public decimal? DiferenciaEfectivo => (MontoRealEfectivo.HasValue && MontoTeoricoEfectivo.HasValue)
        ? MontoRealEfectivo.Value - MontoTeoricoEfectivo.Value
        : null;

    // --- ARQUEO Y CIERRE CIEGO: TRANSFERENCIAS BANCARIAS / PAGOS QR ---
    public decimal? MontoRealTransferencias { get; set; }
    public decimal? MontoTeoricoTransferencias { get; set; }
    public decimal? DiferenciaTransferencias => (MontoRealTransferencias.HasValue && MontoTeoricoTransferencias.HasValue)
        ? MontoRealTransferencias.Value - MontoTeoricoTransferencias.Value
        : null;

    // --- ARQUEO Y CIERRE CIEGO: TARJETAS (DÉBITO / CRÉDITO) ---
    public decimal? MontoRealTarjetas { get; set; }
    public decimal? MontoTeoricoTarjetas { get; set; }
    public decimal? DiferenciaTarjetas => (MontoRealTarjetas.HasValue && MontoTeoricoTarjetas.HasValue)
        ? MontoRealTarjetas.Value - MontoTeoricoTarjetas.Value
        : null;

    // --- TOTALES CONSOLIDADOS ---
    public decimal? TotalTeorico => (MontoTeoricoEfectivo ?? 0m) + (MontoTeoricoTransferencias ?? 0m) + (MontoTeoricoTarjetas ?? 0m);
    public decimal? TotalReal => (MontoRealEfectivo ?? 0m) + (MontoRealTransferencias ?? 0m) + (MontoRealTarjetas ?? 0m);
    public decimal? DiferenciaTotal => (TotalReal.HasValue && TotalTeorico.HasValue)
        ? TotalReal.Value - TotalTeorico.Value
        : null;

    public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
    public ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
}
