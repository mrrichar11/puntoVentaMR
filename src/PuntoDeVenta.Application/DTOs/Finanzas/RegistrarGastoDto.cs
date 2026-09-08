using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.DTOs.Finanzas;

public class RegistrarGastoDto
{
    public Guid TurnoCajaId { get; set; }
    public Guid CategoriaGastoId { get; set; }
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;
}
