using PuntoDeVenta.Domain.Entities.Inventario;

namespace PuntoDeVenta.Application.DTOs.Inventario;

public class AjustarStockDto
{
    public Guid VarianteId { get; set; }
    
    /// <summary>
    /// Cantidad a alterar: Positiva para ingresos (compras, ajustes positivos), negativa para egresos (mermas, roturas).
    /// </summary>
    public int Cantidad { get; set; }

    public TipoMovimientoStock Tipo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? ReferenciaDocumento { get; set; }
}
