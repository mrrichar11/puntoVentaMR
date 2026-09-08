namespace PuntoDeVenta.Application.DTOs.Proveedores;

public class ProveedorDto
{
    public Guid Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreContacto { get; set; }
    public string? Cuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }
    public int PlazoPagoDiasDefecto { get; set; }
    public decimal SaldoDeudorActual { get; set; }
    public decimal SaldoInicial { get; set; }
    public string? DetalleSaldoInicial { get; set; }
    public string? NumeroComprobanteSaldoInicial { get; set; }
}

public class RegistrarDeudaPreviaProveedorDto
{
    public Guid ProveedorId { get; set; }
    public decimal MontoDeudaPrevia { get; set; }
    public string? NumeroFacturaComprobante { get; set; }
    public string? DetalleObservaciones { get; set; }
    public DateTime? FechaComprobante { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}

public class LineaCompraDto
{
    public Guid VarianteId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitarioCompra { get; set; }
    public decimal? NuevoPrecioLista { get; set; }
}

public class RegistrarCompraDto
{
    public Guid ProveedorId { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public PuntoDeVenta.Domain.Entities.Proveedores.CondicionCompraProveedor Condicion { get; set; }
    public Guid? TurnoCajaId { get; set; }
    public int? PlazoDias { get; set; }
    public string? Observaciones { get; set; }
    public List<LineaCompraDto> Lineas { get; set; } = new();
}

public class RegistrarPagoProveedorDto
{
    public Guid CompraProveedorId { get; set; }
    public Guid? TurnoCajaId { get; set; }
    public decimal Monto { get; set; }
    public PuntoDeVenta.Domain.Entities.Finanzas.CanalDinero Canal { get; set; } = PuntoDeVenta.Domain.Entities.Finanzas.CanalDinero.Efectivo;
    public string? ReferenciaComprobante { get; set; }
    public string? Notas { get; set; }
}

public class CompraResumenDto
{
    public Guid Id { get; set; }
    public string NumeroComprobante { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public DateTime? FechaVencimientoPlazo { get; set; }
    public string Condicion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal TotalCompra { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public int TotalUnidadesRecibidas { get; set; }
}
