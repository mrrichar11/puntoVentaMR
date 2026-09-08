namespace PuntoDeVenta.Application.DTOs.Ventas;

public class RegistrarVentaDto
{
    public Guid TurnoCajaId { get; set; }
    public Guid? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteDocumento { get; set; }
    public PuntoDeVenta.Domain.Entities.Ventas.CanalVenta CanalVenta { get; set; } = PuntoDeVenta.Domain.Entities.Ventas.CanalVenta.Mostrador;
    public string? NroPedidoWeb { get; set; }
    public string? Vendedora { get; set; }

    public decimal PorcentajeDescuentoEfectivo { get; set; }

    public List<ItemCarritoDto> Items { get; set; } = new();
    public List<PagoDto> Pagos { get; set; } = new();
}
