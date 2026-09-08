namespace PuntoDeVenta.Application.DTOs.Clientes;

public class ClienteDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? DocumentoIdentidad { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal SaldoDeudorActual { get; set; }
    public DateTime? FechaUltimoMovimiento { get; set; }
    public decimal SaldoInicial { get; set; }
    public string? DetalleSaldoInicial { get; set; }
}

public class RegistrarSaldoPrevioClienteDto
{
    public Guid ClienteId { get; set; }
    public decimal MontoDeudaPrevia { get; set; }
    public string? Detalle { get; set; }
    public DateTime? FechaHistorica { get; set; }
}

public class RegistrarEntregaCuentaCorrienteDto
{
    public Guid ClienteId { get; set; }
    public Guid TurnoCajaId { get; set; }
    public decimal Monto { get; set; }
    public PuntoDeVenta.Domain.Entities.Finanzas.CanalDinero Canal { get; set; } = PuntoDeVenta.Domain.Entities.Finanzas.CanalDinero.Efectivo;
    public string? Detalle { get; set; }
    public string? ReferenciaComprobante { get; set; }
}

public class MovimientoCuentaCorrienteDto
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal SaldoPrevio { get; set; }
    public decimal SaldoResultante { get; set; }
    public string CanalCobro { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string? ReferenciaComprobante { get; set; }
}
