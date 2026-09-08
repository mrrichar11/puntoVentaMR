using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Clientes;
using PuntoDeVenta.Domain.Entities.Clientes;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Exceptions;

namespace PuntoDeVenta.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Cliente> CrearClienteAsync(ClienteDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(dto.NombreCompleto));

        decimal saldoInicial = Math.Max(0, dto.SaldoInicial);
        var ahora = DateTime.UtcNow;

        var cliente = new Cliente
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            DocumentoIdentidad = dto.DocumentoIdentidad?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Email = dto.Email?.Trim(),
            Direccion = dto.Direccion?.Trim(),
            Notas = dto.Notas?.Trim(),
            LimiteCredito = Math.Max(0, dto.LimiteCredito),
            SaldoDeudorActual = saldoInicial,
            FechaUltimoMovimiento = saldoInicial > 0 ? ahora : null
        };

        await _unitOfWork.Clientes.AddAsync(cliente, cancellationToken);

        if (saldoInicial > 0)
        {
            string detalle = string.IsNullOrWhiteSpace(dto.DetalleSaldoInicial)
                ? "Saldo inicial / Deuda previa al sistema (anotada en libreta/cuaderno)"
                : dto.DetalleSaldoInicial.Trim();

            var movInicial = new MovimientoCuentaCorrienteCliente
            {
                ClienteId = cliente.Id,
                Tipo = TipoMovimientoCuentaCorriente.SaldoInicial,
                Monto = saldoInicial,
                SaldoPrevio = 0m,
                SaldoResultante = saldoInicial,
                CanalCobro = CanalDinero.CuentaCorriente,
                Detalle = detalle,
                ReferenciaComprobante = "SALDO-INICIAL",
                Fecha = ahora
            };

            await _unitOfWork.MovimientosCuentaCorrienteClientes.AddAsync(movInicial, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return cliente;
    }

    public async Task<Cliente> ActualizarClienteAsync(ClienteDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var cliente = await _unitOfWork.Clientes.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el cliente con Id '{dto.Id}'.");

        cliente.NombreCompleto = dto.NombreCompleto.Trim();
        cliente.DocumentoIdentidad = dto.DocumentoIdentidad?.Trim();
        cliente.Telefono = dto.Telefono?.Trim();
        cliente.Email = dto.Email?.Trim();
        cliente.Direccion = dto.Direccion?.Trim();
        cliente.Notas = dto.Notas?.Trim();
        cliente.LimiteCredito = Math.Max(0, dto.LimiteCredito);

        _unitOfWork.Clientes.Update(cliente);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return cliente;
    }

    public async Task<IReadOnlyList<ClienteDto>> BuscarClientesAsync(string termino, CancellationToken cancellationToken = default)
    {
        var terminoLimpio = termino?.Trim().ToUpperInvariant() ?? string.Empty;

        var clientes = await _unitOfWork.Clientes.FindAsync(c =>
            c.Activo && (
                c.NombreCompleto.ToUpper().Contains(terminoLimpio) ||
                (c.DocumentoIdentidad != null && c.DocumentoIdentidad.Contains(terminoLimpio)) ||
                (c.Telefono != null && c.Telefono.Contains(terminoLimpio))
            ),
            cancellationToken);

        return clientes.OrderBy(c => c.NombreCompleto).Select(c => new ClienteDto
        {
            Id = c.Id,
            NombreCompleto = c.NombreCompleto,
            DocumentoIdentidad = c.DocumentoIdentidad,
            Telefono = c.Telefono,
            Email = c.Email,
            Direccion = c.Direccion,
            Notas = c.Notas,
            LimiteCredito = c.LimiteCredito,
            SaldoDeudorActual = c.SaldoDeudorActual,
            FechaUltimoMovimiento = c.FechaUltimoMovimiento
        }).ToList();
    }

    public async Task<ClienteDto?> ObtenerPorIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.Clientes.GetByIdAsync(clienteId, cancellationToken);
        if (c == null) return null;

        return new ClienteDto
        {
            Id = c.Id,
            NombreCompleto = c.NombreCompleto,
            DocumentoIdentidad = c.DocumentoIdentidad,
            Telefono = c.Telefono,
            Email = c.Email,
            Direccion = c.Direccion,
            Notas = c.Notas,
            LimiteCredito = c.LimiteCredito,
            SaldoDeudorActual = c.SaldoDeudorActual,
            FechaUltimoMovimiento = c.FechaUltimoMovimiento
        };
    }

    public async Task<IReadOnlyList<MovimientoCuentaCorrienteDto>> ObtenerHistorialCuentaCorrienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        var movimientos = await _unitOfWork.MovimientosCuentaCorrienteClientes.FindAsync(m =>
            m.ClienteId == clienteId && m.Activo,
            cancellationToken);

        return movimientos
            .OrderByDescending(m => m.Fecha)
            .Select(m => new MovimientoCuentaCorrienteDto
            {
                Id = m.Id,
                Fecha = m.Fecha,
                Tipo = m.Tipo.ToString(),
                Monto = m.Monto,
                SaldoPrevio = m.SaldoPrevio,
                SaldoResultante = m.SaldoResultante,
                CanalCobro = m.CanalCobro.ToString(),
                Detalle = m.Detalle,
                ReferenciaComprobante = m.ReferenciaComprobante
            }).ToList();
    }

    public async Task<MovimientoCuentaCorrienteCliente> RegistrarEntregaEnCajaAsync(RegistrarEntregaCuentaCorrienteDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Monto <= 0)
            throw new ArgumentException("El importe de la entrega debe ser mayor a cero.", nameof(dto.Monto));

        var cliente = await _unitOfWork.Clientes.GetByIdAsync(dto.ClienteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el cliente con Id '{dto.ClienteId}'.");

        var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el turno de caja con Id '{dto.TurnoCajaId}'.");

        if (turno.Estado != EstadoTurnoCaja.Abierto)
            throw new DomainException("El turno de caja se encuentra cerrado. Abra una sesión para cobrar la entrega.");

        var saldoPrevio = cliente.SaldoDeudorActual;
        var saldoResultante = Math.Max(0m, saldoPrevio - dto.Monto);

        cliente.SaldoDeudorActual = saldoResultante;
        cliente.FechaUltimoMovimiento = DateTime.UtcNow;
        _unitOfWork.Clientes.Update(cliente);

        // Movimiento en Cuenta Corriente
        var movCc = new MovimientoCuentaCorrienteCliente
        {
            ClienteId = cliente.Id,
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCuentaCorriente.EntregaPago,
            Monto = dto.Monto,
            SaldoPrevio = saldoPrevio,
            SaldoResultante = saldoResultante,
            CanalCobro = dto.Canal,
            Detalle = string.IsNullOrWhiteSpace(dto.Detalle) ? $"Cobro entrega a cuenta de {cliente.NombreCompleto}" : dto.Detalle.Trim(),
            ReferenciaComprobante = dto.ReferenciaComprobante?.Trim(),
            Fecha = DateTime.UtcNow
        };

        // Movimiento de Ingreso Directo a Caja
        var movCaja = new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Concepto = ConceptoMovimientoCaja.CobroCuentaCorrienteCliente,
            Canal = dto.Canal,
            Monto = dto.Monto,
            Descripcion = $"Cobro cta. cte. {cliente.NombreCompleto}",
            ReferenciaComprobante = dto.ReferenciaComprobante?.Trim(),
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.MovimientosCuentaCorrienteClientes.AddAsync(movCc, cancellationToken);
        await _unitOfWork.MovimientosCaja.AddAsync(movCaja, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movCc;
    }

    public async Task<MovimientoCuentaCorrienteCliente> RegistrarCargoPorVentaAsync(Guid clienteId, Guid ventaId, decimal monto, string numeroComprobante, CancellationToken cancellationToken = default)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(clienteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el cliente con Id '{clienteId}'.");

        var saldoPrevio = cliente.SaldoDeudorActual;
        var saldoResultante = saldoPrevio + monto;

        if (cliente.LimiteCredito > 0 && saldoResultante > cliente.LimiteCredito)
        {
            throw new DomainException($"El límite de crédito de {cliente.NombreCompleto} (${cliente.LimiteCredito:N2}) ha sido superado. Saldo resultante sería: ${saldoResultante:N2}.");
        }

        cliente.SaldoDeudorActual = saldoResultante;
        cliente.FechaUltimoMovimiento = DateTime.UtcNow;
        _unitOfWork.Clientes.Update(cliente);

        var movCc = new MovimientoCuentaCorrienteCliente
        {
            ClienteId = cliente.Id,
            VentaId = ventaId,
            Tipo = TipoMovimientoCuentaCorriente.CargoVenta,
            Monto = monto,
            SaldoPrevio = saldoPrevio,
            SaldoResultante = saldoResultante,
            CanalCobro = CanalDinero.CuentaCorriente,
            Detalle = $"Cargo venta ticket {numeroComprobante}",
            ReferenciaComprobante = numeroComprobante,
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.MovimientosCuentaCorrienteClientes.AddAsync(movCc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movCc;
    }

    public async Task<MovimientoCuentaCorrienteCliente> RegistrarSaldoPrevioHistoricoAsync(RegistrarSaldoPrevioClienteDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.MontoDeudaPrevia <= 0)
            throw new ArgumentException("El monto de la deuda previa debe ser mayor a cero.", nameof(dto.MontoDeudaPrevia));

        var cliente = await _unitOfWork.Clientes.GetByIdAsync(dto.ClienteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el cliente con Id '{dto.ClienteId}'.");

        var saldoPrevio = cliente.SaldoDeudorActual;
        var saldoResultante = saldoPrevio + dto.MontoDeudaPrevia;

        cliente.SaldoDeudorActual = saldoResultante;
        cliente.FechaUltimoMovimiento = DateTime.UtcNow;
        _unitOfWork.Clientes.Update(cliente);

        string detalle = string.IsNullOrWhiteSpace(dto.Detalle)
            ? "Ajuste / Saldo histórico previo al sistema (libreta de fiados)"
            : dto.Detalle.Trim();

        var mov = new MovimientoCuentaCorrienteCliente
        {
            ClienteId = cliente.Id,
            Tipo = TipoMovimientoCuentaCorriente.SaldoInicial,
            Monto = dto.MontoDeudaPrevia,
            SaldoPrevio = saldoPrevio,
            SaldoResultante = saldoResultante,
            CanalCobro = CanalDinero.CuentaCorriente,
            Detalle = detalle,
            ReferenciaComprobante = "SALDO-HISTORICO",
            Fecha = dto.FechaHistorica ?? DateTime.UtcNow
        };

        await _unitOfWork.MovimientosCuentaCorrienteClientes.AddAsync(mov, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mov;
    }
}
