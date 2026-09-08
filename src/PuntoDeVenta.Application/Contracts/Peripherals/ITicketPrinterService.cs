using PuntoDeVenta.Application.DTOs.Peripherals;
using PuntoDeVenta.Application.DTOs.Ventas;

namespace PuntoDeVenta.Application.Contracts.Peripherals;

public interface ITicketPrinterService
{
    /// <summary>
    /// Imprime el ticket de venta en la impresora térmica configurada y/o genera una copia en archivo según configuración.
    /// </summary>
    Task<bool> ImprimirTicketVentaAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía la secuencia ESC p por el puerto de la impresora para abrir el cajón monedero.
    /// </summary>
    Task<bool> AbrirCajonDineroAsync(string nombreImpresora, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera la representación textual o en archivo formateada con el ancho de papel (58mm u 80mm).
    /// </summary>
    Task<string> GenerarTicketArchivoAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default);
}
