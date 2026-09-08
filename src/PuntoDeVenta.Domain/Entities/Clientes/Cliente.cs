using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Clientes;

public class Cliente : BaseEntity
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? DocumentoIdentidad { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }

    public decimal LimiteCredito { get; set; } = 0m;
    public decimal SaldoDeudorActual { get; set; } = 0m;
    public DateTime? FechaUltimoMovimiento { get; set; }

    public ICollection<MovimientoCuentaCorrienteCliente> MovimientosCuentaCorriente { get; set; } = new List<MovimientoCuentaCorrienteCliente>();
}
