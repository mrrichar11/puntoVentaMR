using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.Services;

public interface ICajaService
{
    /// <summary>
    /// Abre una nueva sesión de turno de caja con fondo inicial en efectivo.
    /// Valida que no exista ya un turno abierto en la terminal.
    /// </summary>
    Task<TurnoCaja> AbrirTurnoAsync(AbrirCajaDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el turno actualmente abierto si existe.
    /// </summary>
    Task<TurnoCaja?> ObtenerTurnoAbiertoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un movimiento directo de caja (ingreso o egreso) en efectivo, transferencia o tarjeta.
    /// </summary>
    Task<MovimientoCaja> RegistrarMovimientoAsync(RegistrarMovimientoCajaDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un gasto comercial o retiro personal.
    /// Si es retiro de propietario, lo clasifica como gasto personal y crea el movimiento de egreso correspondiente.
    /// </summary>
    Task<Gasto> RegistrarGastoAsync(RegistrarGastoDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta el arqueo y cierre ciego de caja comparando lo declarado por el cajero
    /// contra los teóricos del sistema en efectivo, transferencias y cupones de tarjetas.
    /// </summary>
    Task<ResumenCierreCajaDto> CerrarTurnoCiegoAsync(CerrarCajaCiegoDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el fondo inicial sugerido para apertura de caja (el efectivo real del último cierre o un valor base).
    /// </summary>
    Task<decimal> ObtenerSugerenciaFondoInicialAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el balance teórico en tiempo real de un turno de caja (efectivo, transferencias y tarjetas).
    /// </summary>
    Task<SaldoTeoricoTurnoDto> ObtenerSaldoTeoricoActualAsync(Guid turnoId, CancellationToken cancellationToken = default);
}
