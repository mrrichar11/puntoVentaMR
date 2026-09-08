using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.DTOs.Finanzas;

public class RegistrarMovimientoCajaDto
{
    public Guid TurnoCajaId { get; set; }
    public TipoMovimientoCaja Tipo { get; set; }
    public ConceptoMovimientoCaja Concepto { get; set; }
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ReferenciaComprobante { get; set; }
}
