namespace PuntoDeVenta.Application.DTOs.Finanzas;

public class AbrirCajaDto
{
    public string Usuario { get; set; } = string.Empty;
    public decimal MontoInicialEfectivo { get; set; }
    public string? Observaciones { get; set; }
}
