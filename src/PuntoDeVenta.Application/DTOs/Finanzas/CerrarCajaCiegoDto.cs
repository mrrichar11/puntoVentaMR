namespace PuntoDeVenta.Application.DTOs.Finanzas;

/// <summary>
/// DTO para arqueo y cierre ciego: el cajero declara lo que tiene físicamente
/// (efectivo en mostrador, suma de transferencias recibidas y total de cupones de tarjeta).
/// El sistema calcula en el backend el teórico y las diferencias correspondientes.
/// </summary>
public class CerrarCajaCiegoDto
{
    public Guid TurnoCajaId { get; set; }
    public string Usuario { get; set; } = string.Empty;

    public decimal MontoRealEfectivo { get; set; }
    public decimal MontoRealTransferencias { get; set; }
    public decimal MontoRealTarjetas { get; set; }

    public string? Observaciones { get; set; }
}
