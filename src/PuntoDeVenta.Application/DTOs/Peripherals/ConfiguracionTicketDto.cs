namespace PuntoDeVenta.Application.DTOs.Peripherals;

public enum AnchoPapelTicket
{
    Mm58 = 32, // 32 caracteres estándar por línea en papel térmico de 58mm
    Mm80 = 48  // 48 caracteres estándar por línea en papel térmico de 80mm
}

public class ConfiguracionTicketDto
{
    public string NombreFantasia { get; set; } = "INDUMENTARIA & CALZADO";
    public string? RazonSocial { get; set; }
    public string? CUIT { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? MensajePie { get; set; } = "¡Gracias por su compra! Cambios dentro de los 30 días con este ticket.";

    public AnchoPapelTicket AnchoPapel { get; set; } = AnchoPapelTicket.Mm80;

    /// <summary>
    /// Nombre de la impresora instalada en Windows (ej. "EPSON TM-T20III" o "POS-58").
    /// Si es nulo o vacío, o la impresora no está conectada, el sistema emitirá el ticket en archivo.
    /// </summary>
    public string? NombreImpresoraWindows { get; set; }

    /// <summary>
    /// Ruta del directorio local donde se almacenarán las copias en archivo de los tickets.
    /// Por defecto guarda en la carpeta "Tickets" de la aplicación.
    /// </summary>
    public string RutaCarpetaTicketsArchivo { get; set; } = "Tickets";

    public bool AbrirCajonAlCobrarEfectivo { get; set; } = true;
    public bool GuardarCopiaEnArchivo { get; set; } = true;
}
