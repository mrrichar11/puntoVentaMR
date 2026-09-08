using PuntoDeVenta.Application.DTOs.Clientes;
using PuntoDeVenta.Domain.Entities.Clientes;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.Services;

public interface IClienteService
{
    Task<Cliente> CrearClienteAsync(ClienteDto dto, CancellationToken cancellationToken = default);
    Task<Cliente> ActualizarClienteAsync(ClienteDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClienteDto>> BuscarClientesAsync(string termino, CancellationToken cancellationToken = default);
    Task<ClienteDto?> ObtenerPorIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoCuentaCorrienteDto>> ObtenerHistorialCuentaCorrienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
    Task<MovimientoCuentaCorrienteCliente> RegistrarEntregaEnCajaAsync(RegistrarEntregaCuentaCorrienteDto dto, CancellationToken cancellationToken = default);
    Task<MovimientoCuentaCorrienteCliente> RegistrarCargoPorVentaAsync(Guid clienteId, Guid ventaId, decimal monto, string numeroComprobante, CancellationToken cancellationToken = default);
    Task<MovimientoCuentaCorrienteCliente> RegistrarSaldoPrevioHistoricoAsync(RegistrarSaldoPrevioClienteDto dto, CancellationToken cancellationToken = default);
}
