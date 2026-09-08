using PuntoDeVenta.Application.DTOs.Proveedores;
using PuntoDeVenta.Domain.Entities.Proveedores;

namespace PuntoDeVenta.Application.Services;

public interface IProveedorService
{
    Task<Proveedor> CrearProveedorAsync(ProveedorDto dto, CancellationToken cancellationToken = default);
    Task<Proveedor> ActualizarProveedorAsync(ProveedorDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProveedorDto>> BuscarProveedoresAsync(string termino, CancellationToken cancellationToken = default);
    Task<ProveedorDto?> ObtenerPorIdAsync(Guid proveedorId, CancellationToken cancellationToken = default);
    Task<CompraProveedor> RegistrarCompraAsync(RegistrarCompraDto dto, CancellationToken cancellationToken = default);
    Task<PagoCompraProveedor> RegistrarPagoCompraAsync(RegistrarPagoProveedorDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompraResumenDto>> ObtenerHistorialComprasAsync(Guid? proveedorId = null, CancellationToken cancellationToken = default);
    Task<CompraProveedor> RegistrarDeudaPreviaAsync(RegistrarDeudaPreviaProveedorDto dto, CancellationToken cancellationToken = default);
}
