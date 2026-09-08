using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Exceptions;

namespace PuntoDeVenta.Application.Services;

public class CajaService : ICajaService
{
    private readonly IUnitOfWork _unitOfWork;

    public CajaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<TurnoCaja> AbrirTurnoAsync(AbrirCajaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Usuario))
            throw new ArgumentException("El usuario de apertura es obligatorio.", nameof(dto.Usuario));

        if (dto.MontoInicialEfectivo < 0)
            throw new ArgumentException("El monto inicial de efectivo no puede ser negativo.", nameof(dto.MontoInicialEfectivo));

        // Verificar que no exista un turno ya abierto
        var turnoAbierto = await ObtenerTurnoAbiertoAsync(cancellationToken);
        if (turnoAbierto != null)
        {
            throw new DomainException($"Ya existe un turno de caja abierto (Id: {turnoAbierto.Id}) iniciado por {turnoAbierto.UsuarioApertura}. Debe cerrarlo antes de abrir uno nuevo.");
        }

        var nuevoTurno = new TurnoCaja
        {
            UsuarioApertura = dto.Usuario.Trim(),
            FechaApertura = DateTime.UtcNow,
            MontoInicialEfectivo = dto.MontoInicialEfectivo,
            Estado = EstadoTurnoCaja.Abierto,
            ObservacionesApertura = dto.Observaciones?.Trim()
        };

        await _unitOfWork.TurnosCaja.AddAsync(nuevoTurno, cancellationToken);

        // Registro del movimiento de apertura con el fondo inicial
        if (dto.MontoInicialEfectivo > 0)
        {
            var movimientoApertura = new MovimientoCaja
            {
                TurnoCaja = nuevoTurno,
                TurnoCajaId = nuevoTurno.Id,
                Tipo = TipoMovimientoCaja.Ingreso,
                Concepto = ConceptoMovimientoCaja.FondoInicialApertura,
                Canal = CanalDinero.Efectivo,
                Monto = dto.MontoInicialEfectivo,
                Descripcion = "Fondo inicial de cambio / apertura",
                Fecha = DateTime.UtcNow
            };

            await _unitOfWork.MovimientosCaja.AddAsync(movimientoApertura, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return nuevoTurno;
    }

    public async Task<TurnoCaja?> ObtenerTurnoAbiertoAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.TurnosCaja.FirstOrDefaultAsync(t => t.Estado == EstadoTurnoCaja.Abierto, cancellationToken);
    }

    public async Task<MovimientoCaja> RegistrarMovimientoAsync(RegistrarMovimientoCajaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.", nameof(dto.Monto));

        var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el turno con Id '{dto.TurnoCajaId}'.");

        if (turno.Estado != EstadoTurnoCaja.Abierto)
            throw new DomainException("No se pueden registrar movimientos en un turno de caja que ya está cerrado.");

        var movimiento = new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = dto.Tipo,
            Concepto = dto.Concepto,
            Canal = dto.Canal,
            Monto = dto.Monto,
            Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
            ReferenciaComprobante = dto.ReferenciaComprobante?.Trim(),
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.MovimientosCaja.AddAsync(movimiento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movimiento;
    }

    public async Task<Gasto> RegistrarGastoAsync(RegistrarGastoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Monto <= 0)
            throw new ArgumentException("El monto del gasto debe ser mayor a cero.", nameof(dto.Monto));

        var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el turno con Id '{dto.TurnoCajaId}'.");

        if (turno.Estado != EstadoTurnoCaja.Abierto)
            throw new DomainException("No se pueden registrar gastos en un turno de caja cerrado.");

        var categoria = await _unitOfWork.CategoriasGasto.GetByIdAsync(dto.CategoriaGastoId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la categoría de gasto con Id '{dto.CategoriaGastoId}'.");

        // Crear la entidad Gasto
        var gasto = new Gasto
        {
            TurnoCajaId = turno.Id,
            CategoriaGastoId = categoria.Id,
            CategoriaGasto = categoria,
            Monto = dto.Monto,
            Descripcion = dto.Descripcion?.Trim() ?? categoria.Nombre,
            NumeroComprobante = dto.NumeroComprobante?.Trim(),
            Canal = dto.Canal,
            EsPersonal = categoria.EsGastoPersonal, // Snapshot inmutable
            Fecha = DateTime.UtcNow
        };

        // Generar automáticamente el egreso en MovimientoCaja
        var concepto = categoria.EsGastoPersonal
            ? ConceptoMovimientoCaja.RetiroPropietario
            : ConceptoMovimientoCaja.GastoOperativo;

        var prefijo = categoria.EsGastoPersonal ? "Retiro Propietario" : "Gasto Operativo";

        var movimientoEgreso = new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            Tipo = TipoMovimientoCaja.Egreso,
            Concepto = concepto,
            Canal = dto.Canal,
            Monto = dto.Monto,
            Descripcion = $"{prefijo}: {categoria.Nombre} - {dto.Descripcion}",
            ReferenciaComprobante = dto.NumeroComprobante?.Trim(),
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Gastos.AddAsync(gasto, cancellationToken);
        await _unitOfWork.MovimientosCaja.AddAsync(movimientoEgreso, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return gasto;
    }

    public async Task<ResumenCierreCajaDto> CerrarTurnoCiegoAsync(CerrarCajaCiegoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Usuario))
            throw new ArgumentException("El usuario de cierre es obligatorio.", nameof(dto.Usuario));

        var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el turno con Id '{dto.TurnoCajaId}'.");

        if (turno.Estado != EstadoTurnoCaja.Abierto)
            throw new DomainException("El turno de caja ya se encuentra cerrado.");

        // Obtener todos los movimientos del turno para calcular los saldos teóricos
        var movimientos = await _unitOfWork.MovimientosCaja.FindAsync(m => m.TurnoCajaId == turno.Id, cancellationToken);

        // --- CÁLCULO TEÓRICO EFECTIVO ---
        var ingresosEfectivo = movimientos
            .Where(m => m.Canal == CanalDinero.Efectivo && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);

        var egresosEfectivoOperativos = movimientos
            .Where(m => m.Canal == CanalDinero.Efectivo && m.Tipo == TipoMovimientoCaja.Egreso && m.Concepto != ConceptoMovimientoCaja.RetiroPropietario)
            .Sum(m => m.Monto);

        var egresosEfectivoRetiroPropietario = movimientos
            .Where(m => m.Canal == CanalDinero.Efectivo && m.Tipo == TipoMovimientoCaja.Egreso && m.Concepto == ConceptoMovimientoCaja.RetiroPropietario)
            .Sum(m => m.Monto);

        var montoTeoricoEfectivo = ingresosEfectivo - (egresosEfectivoOperativos + egresosEfectivoRetiroPropietario);

        // --- CÁLCULO TEÓRICO TRANSFERENCIAS / QR ---
        var ingresosTransferencias = movimientos
            .Where(m => m.Canal == CanalDinero.TransferenciaQR && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);

        var egresosTransferencias = movimientos
            .Where(m => m.Canal == CanalDinero.TransferenciaQR && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);

        var montoTeoricoTransferencias = ingresosTransferencias - egresosTransferencias;

        // --- CÁLCULO TEÓRICO TARJETAS ---
        var ingresosTarjetas = movimientos
            .Where(m => (m.Canal == CanalDinero.TarjetaDebito || m.Canal == CanalDinero.TarjetaCredito) && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);

        var egresosTarjetas = movimientos
            .Where(m => (m.Canal == CanalDinero.TarjetaDebito || m.Canal == CanalDinero.TarjetaCredito) && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);

        var montoTeoricoTarjetas = ingresosTarjetas - egresosTarjetas;

        // --- TOTALES Y DIFERENCIAS ---
        var diferenciaEfectivo = dto.MontoRealEfectivo - montoTeoricoEfectivo;
        var diferenciaTransferencias = dto.MontoRealTransferencias - montoTeoricoTransferencias;
        var diferenciaTarjetas = dto.MontoRealTarjetas - montoTeoricoTarjetas;

        // Actualizar entidad TurnoCaja con los datos del arqueo ciego
        turno.FechaCierre = DateTime.UtcNow;
        turno.UsuarioCierre = dto.Usuario.Trim();
        turno.ObservacionesCierre = dto.Observaciones?.Trim();
        turno.Estado = EstadoTurnoCaja.Cerrado;

        turno.MontoTeoricoEfectivo = montoTeoricoEfectivo;
        turno.MontoRealEfectivo = dto.MontoRealEfectivo;

        turno.MontoTeoricoTransferencias = montoTeoricoTransferencias;
        turno.MontoRealTransferencias = dto.MontoRealTransferencias;

        turno.MontoTeoricoTarjetas = montoTeoricoTarjetas;
        turno.MontoRealTarjetas = dto.MontoRealTarjetas;

        _unitOfWork.TurnosCaja.Update(turno);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Ventas totales del turno
        var totalVentas = movimientos
            .Where(m => m.Concepto == ConceptoMovimientoCaja.Venta && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);

        // Total gastos operativos del negocio (cualquier canal)
        var totalGastosOperativos = movimientos
            .Where(m => m.Concepto == ConceptoMovimientoCaja.GastoOperativo && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);

        // Total retiros de propietario (cualquier canal)
        var totalRetirosPropietario = movimientos
            .Where(m => m.Concepto == ConceptoMovimientoCaja.RetiroPropietario && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);

        var totalTeorico = montoTeoricoEfectivo + montoTeoricoTransferencias + montoTeoricoTarjetas;
        var totalReal = dto.MontoRealEfectivo + dto.MontoRealTransferencias + dto.MontoRealTarjetas;
        var diferenciaTotal = totalReal - totalTeorico;

        return new ResumenCierreCajaDto
        {
            TurnoCajaId = turno.Id,
            UsuarioApertura = turno.UsuarioApertura,
            UsuarioCierre = turno.UsuarioCierre,
            FechaApertura = turno.FechaApertura,
            FechaCierre = turno.FechaCierre.Value,
            MontoInicialEfectivo = turno.MontoInicialEfectivo,

            IngresosEfectivo = ingresosEfectivo,
            EgresosEfectivoOperativos = egresosEfectivoOperativos,
            EgresosEfectivoRetiroPropietario = egresosEfectivoRetiroPropietario,
            MontoTeoricoEfectivo = montoTeoricoEfectivo,
            MontoRealEfectivo = dto.MontoRealEfectivo,
            DiferenciaEfectivo = diferenciaEfectivo,

            IngresosTransferencias = ingresosTransferencias,
            EgresosTransferencias = egresosTransferencias,
            MontoTeoricoTransferencias = montoTeoricoTransferencias,
            MontoRealTransferencias = dto.MontoRealTransferencias,
            DiferenciaTransferencias = diferenciaTransferencias,

            IngresosTarjetas = ingresosTarjetas,
            MontoTeoricoTarjetas = montoTeoricoTarjetas,
            MontoRealTarjetas = dto.MontoRealTarjetas,
            DiferenciaTarjetas = diferenciaTarjetas,

            TotalVentasTurno = totalVentas,
            TotalGastosOperativosNegocio = totalGastosOperativos,
            TotalRetirosPersonalesPropietario = totalRetirosPropietario,

            TotalTeorico = totalTeorico,
            TotalReal = totalReal,
            DiferenciaTotal = diferenciaTotal,
            Observaciones = turno.ObservacionesCierre
        };
    }

    public async Task<decimal> ObtenerSugerenciaFondoInicialAsync(CancellationToken cancellationToken = default)
    {
        var turnos = await _unitOfWork.TurnosCaja.GetAllAsync(cancellationToken);
        var ultimoCerrado = turnos
            .Where(t => t.Estado == EstadoTurnoCaja.Cerrado)
            .OrderByDescending(t => t.FechaCierre ?? t.FechaApertura)
            .FirstOrDefault();

        if (ultimoCerrado != null && ultimoCerrado.MontoRealEfectivo.HasValue)
        {
            return ultimoCerrado.MontoRealEfectivo.Value;
        }

        return 20000m; // Sugerencia inicial base si es la primera vez
    }

    public async Task<SaldoTeoricoTurnoDto> ObtenerSaldoTeoricoActualAsync(Guid turnoId, CancellationToken cancellationToken = default)
    {
        var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(turnoId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el turno con Id '{turnoId}'.");

        var movimientos = await _unitOfWork.MovimientosCaja.FindAsync(m => m.TurnoCajaId == turno.Id, cancellationToken);

        // --- EFECTIVO ---
        var fondoInicial = turno.MontoInicialEfectivo;
        var ventasEfectivo = movimientos
            .Where(m => m.Canal == CanalDinero.Efectivo && m.Tipo == TipoMovimientoCaja.Ingreso && m.Concepto != ConceptoMovimientoCaja.FondoInicialApertura)
            .Sum(m => m.Monto);
        var gastosEfectivo = movimientos
            .Where(m => m.Canal == CanalDinero.Efectivo && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);
        var montoTeoricoEfectivo = (fondoInicial + ventasEfectivo) - gastosEfectivo;

        // --- TRANSFERENCIAS ---
        var ventasTransferencias = movimientos
            .Where(m => m.Canal == CanalDinero.TransferenciaQR && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);
        var gastosTransferencias = movimientos
            .Where(m => m.Canal == CanalDinero.TransferenciaQR && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);
        var montoTeoricoTransferencias = ventasTransferencias - gastosTransferencias;

        // --- TARJETAS ---
        var ventasTarjetas = movimientos
            .Where(m => (m.Canal == CanalDinero.TarjetaDebito || m.Canal == CanalDinero.TarjetaCredito) && m.Tipo == TipoMovimientoCaja.Ingreso)
            .Sum(m => m.Monto);
        var gastosTarjetas = movimientos
            .Where(m => (m.Canal == CanalDinero.TarjetaDebito || m.Canal == CanalDinero.TarjetaCredito) && m.Tipo == TipoMovimientoCaja.Egreso)
            .Sum(m => m.Monto);
        var montoTeoricoTarjetas = ventasTarjetas - gastosTarjetas;

        return new SaldoTeoricoTurnoDto
        {
            TurnoId = turno.Id,
            FondoInicial = fondoInicial,
            VentasEfectivo = ventasEfectivo,
            GastosEfectivo = gastosEfectivo,
            MontoTeoricoEfectivo = montoTeoricoEfectivo,

            VentasTransferencias = ventasTransferencias,
            GastosTransferencias = gastosTransferencias,
            MontoTeoricoTransferencias = montoTeoricoTransferencias,

            VentasTarjetas = ventasTarjetas,
            GastosTarjetas = gastosTarjetas,
            MontoTeoricoTarjetas = montoTeoricoTarjetas
        };
    }
}
