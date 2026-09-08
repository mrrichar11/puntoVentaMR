using PuntoDeVenta.Application.DTOs.Asistente;

namespace PuntoDeVenta.Application.Services;

public interface IAsistenteCargaService
{
    /// <summary>
    /// Procesa una entrada de texto del usuario, interpreta datos de indumentaria,
    /// actualiza el borrador en curso y responde con preguntas o con la propuesta de confirmación.
    /// Soporta múltiples artículos en una sola frase o conversación continua.
    /// </summary>
    Task<MensajeChatDto> ProcesarMensajeAsync(
        string entradaUsuario,
        BorradorCargaArticuloDto? borradorActual = null,
        List<BorradorCargaArticuloDto>? borradoresExistentes = null,
        BorradorDeudaClienteDto? deudaClientePendiente = null,
        BorradorDeudaProveedorDto? deudaProveedorPendiente = null,
        BorradorContactoDto? contactoPendiente = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda el borrador validado creando el artículo y sus variantes en la base de datos,
    /// e impactando el stock inicial correspondiente.
    /// </summary>
    Task<int> ConfirmarBorradorAsync(
        BorradorCargaArticuloDto borrador,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda una lista de borradores validados en la base de datos de manera secuencial.
    /// </summary>
    Task<int> ConfirmarTodosAsync(
        IEnumerable<BorradorCargaArticuloDto> borradores,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma y asienta la deuda previa o saldo de libreta de una clienta en su cuenta corriente.
    /// </summary>
    Task<PuntoDeVenta.Application.DTOs.Clientes.MovimientoCuentaCorrienteDto> ConfirmarDeudaClienteAsync(
        BorradorDeudaClienteDto borrador,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma y asienta una factura/deuda previa de proveedor en Cuentas a Pagar.
    /// </summary>
    Task<PuntoDeVenta.Domain.Entities.Proveedores.CompraProveedor> ConfirmarDeudaProveedorAsync(
        BorradorDeudaProveedorDto borrador,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma el alta rápida de una clienta o proveedor con o sin saldo previo.
    /// </summary>
    Task<object> ConfirmarNuevoContactoAsync(
        BorradorContactoDto borrador,
        CancellationToken cancellationToken = default);
}
