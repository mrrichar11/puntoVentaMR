using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Finanzas;

/// <summary>
/// Registro inmutable de un gasto comercial o retiro personal realizado durante el turno.
/// </summary>
public class Gasto : BaseEntity
{
    public Guid TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public Guid CategoriaGastoId { get; set; }
    public CategoriaGasto? CategoriaGasto { get; set; }

    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }

    /// <summary>
    /// Canal por donde se abonó el gasto (Efectivo de la caja física, Transferencia bancaria del negocio, etc.).
    /// </summary>
    public CanalDinero Canal { get; set; } = CanalDinero.Efectivo;

    /// <summary>
    /// Snapshot inmutable: si es true, es un retiro del dueño (no se deduce como costo operativo comercial).
    /// </summary>
    public bool EsPersonal { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
