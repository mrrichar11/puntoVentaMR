using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Finanzas;

public class CategoriaGasto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoGasto TipoGasto { get; set; }

    /// <summary>
    /// Si true, indica que es un retiro personal del dueño (Sueldo Emprendedor / Retiro de Propietario).
    /// Regla contable: Se deduce del arqueo de caja física o saldo bancario, pero NO se descuenta
    /// como costo operativo al calcular el Margen Operativo o Rentabilidad de la Tienda.
    /// </summary>
    public bool EsGastoPersonal { get; set; }

    public ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();
}
