using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.DTOs.Ventas;

public class PagoDto
{
    public Guid? MetodoPagoId { get; set; }
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
