using PuntoDeVenta.Domain.Common;
using PuntoDeVenta.Domain.Entities.Catalogo;

namespace PuntoDeVenta.Domain.Entities.Inventario;

/// <summary>
/// Registro inmutable de cada alteración de stock para auditoría y trazabilidad.
/// </summary>
public class MovimientoStock : BaseEntity
{
    public Guid VarianteId { get; set; }
    public VarianteArticulo? Variante { get; set; }

    public TipoMovimientoStock Tipo { get; set; }
    
    /// <summary>
    /// Cantidad alterada (positiva para entradas, negativa para salidas).
    /// </summary>
    public int Cantidad { get; set; }

    public int StockPrevio { get; set; }
    public int StockResultante { get; set; }

    /// <summary>
    /// Costo unitario al momento del movimiento.
    /// </summary>
    public decimal CostoUnitario { get; set; }

    public string? Motivo { get; set; }
    public string? ReferenciaDocumento { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
