using System.Text;
using PuntoDeVenta.Application.Contracts.Peripherals;
using PuntoDeVenta.Application.DTOs.Peripherals;
using PuntoDeVenta.Application.DTOs.Ventas;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Infrastructure.Peripherals;

public class TicketPrinterService : ITicketPrinterService
{
    public async Task<bool> ImprimirTicketVentaAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(venta);
        ArgumentNullException.ThrowIfNull(config);

        var ancho = (int)config.AnchoPapel;
        var builder = new EscPosBuilder();

        // 1. Inicialización y Encabezado del Comercio
        builder.Initialize()
               .AlignCenter()
               .Bold()
               .DoubleSize()
               .Line(config.NombreFantasia)
               .DoubleSize(false);

        if (!string.IsNullOrWhiteSpace(config.RazonSocial))
            builder.Line(config.RazonSocial);

        if (!string.IsNullOrWhiteSpace(config.CUIT))
            builder.Line($"CUIT: {config.CUIT}");

        if (!string.IsNullOrWhiteSpace(config.Direccion))
            builder.Line(config.Direccion);

        if (!string.IsNullOrWhiteSpace(config.Telefono))
            builder.Line($"Tel: {config.Telefono}");

        builder.Separator(ancho)
               .AlignLeft()
               .Line($"COMPROBANTE: {venta.NumeroComprobante}")
               .Line($"FECHA: {venta.Fecha:dd/MM/yyyy HH:mm:ss}");

        if (!string.IsNullOrWhiteSpace(venta.ClienteNombre))
            builder.Line($"CLIENTE: {venta.ClienteNombre}");

        builder.Separator(ancho)
               .Bold()
               .Line("DETALLE DE ARTICULOS")
               .Bold(false);

        // 2. Líneas de Venta
        foreach (var linea in venta.Lineas)
        {
            var cantYDesc = $"{linea.Cantidad} x {linea.Descripcion}";
            if (cantYDesc.Length > ancho) cantYDesc = cantYDesc[..ancho];
            builder.Line(cantYDesc);

            var detalleVariante = $"  Talle: {linea.Talle} | Color: {linea.Color}";
            var precioSubtotal = $"${linea.SubtotalCobrado:N2}";
            builder.KeyValueLine(detalleVariante, precioSubtotal, ancho);

            if (linea.DescuentoOfertaUnitario > 0 || linea.DescuentoMedioPagoUnitario > 0)
            {
                var totalDesc = (linea.DescuentoOfertaUnitario + linea.DescuentoMedioPagoUnitario) * linea.Cantidad;
                builder.Line($"  (Bonificación total: -${totalDesc:N2})");
            }
        }

        // 3. Totales
        builder.Separator(ancho);

        if (venta.TotalDescuentoOferta > 0 || venta.TotalDescuentoMedioPago > 0)
        {
            builder.KeyValueLine("Subtotal Lista:", $"${venta.SubtotalLista:N2}", ancho);
            if (venta.TotalDescuentoOferta > 0)
                builder.KeyValueLine("Descuento Oferta:", $"-${venta.TotalDescuentoOferta:N2}", ancho);
            if (venta.TotalDescuentoMedioPago > 0)
                builder.KeyValueLine("Descuento Medio Pago:", $"-${venta.TotalDescuentoMedioPago:N2}", ancho);
        }

        if (venta.TotalRecargoMedioPago > 0)
        {
            builder.KeyValueLine("Recargo Financiación:", $"+${venta.TotalRecargoMedioPago:N2}", ancho);
        }

        builder.Bold()
               .DoubleSize()
               .KeyValueLine("TOTAL:", $"${venta.TotalFinalCobrado:N2}", ancho / 2) // compensa doble tamaño
               .DoubleSize(false)
               .Bold(false)
               .Separator(ancho)
               .Bold()
               .Line("FORMA DE PAGO:")
               .Bold(false);

        // 4. Medios de Pago
        foreach (var pago in venta.Pagos)
        {
            var textoPago = pago.Canal.ToString();
            if (!string.IsNullOrWhiteSpace(pago.Referencia))
                textoPago += $" ({pago.Referencia})";

            builder.KeyValueLine($"  {textoPago}", $"${pago.Monto:N2}", ancho);
        }

        // 5. Pie de ticket
        builder.Separator(ancho)
               .AlignCenter();

        if (!string.IsNullOrWhiteSpace(config.MensajePie))
            builder.Line(config.MensajePie);

        // 6. Pulso de Cajón Monedero
        if (config.AbrirCajonAlCobrarEfectivo && venta.Pagos.Any(p => p.Canal == CanalDinero.Efectivo))
        {
            builder.CashDrawerKick();
        }

        // 7. Corte de papel
        builder.CutPaper();

        var bytes = builder.Build();

        // Guardar copia en archivo si está configurado
        if (config.GuardarCopiaEnArchivo)
        {
            await GenerarTicketArchivoAsync(venta, config, cancellationToken);
        }

        // Si hay una impresora física configurada en Windows, enviamos los bytes por RawPrinterHelper
        if (!string.IsNullOrWhiteSpace(config.NombreImpresoraWindows))
        {
            return RawPrinterHelper.SendBytesToPrinter(config.NombreImpresoraWindows, bytes, $"Ticket-{venta.NumeroComprobante}");
        }

        return true;
    }

    public Task<bool> AbrirCajonDineroAsync(string nombreImpresora, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreImpresora))
            return Task.FromResult(false);

        var bytes = new EscPosBuilder()
            .Initialize()
            .CashDrawerKick()
            .Build();

        var resultado = RawPrinterHelper.SendBytesToPrinter(nombreImpresora, bytes, "Apertura Cajon Monedero");
        return Task.FromResult(resultado);
    }

    public async Task<string> GenerarTicketArchivoAsync(VentaRealizadaDto venta, ConfiguracionTicketDto config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(venta);
        ArgumentNullException.ThrowIfNull(config);

        var ancho = (int)config.AnchoPapel;
        var sb = new StringBuilder();

        string FormatearCentro(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            if (texto.Length >= ancho) return texto[..ancho];
            var padding = (ancho - texto.Length) / 2;
            return new string(' ', padding) + texto;
        }

        string FormatearDosColumnas(string izq, string der)
        {
            var espacio = ancho - (izq.Length + der.Length);
            if (espacio < 1) espacio = 1;
            return izq + new string(' ', espacio) + der;
        }

        sb.AppendLine(FormatearCentro(config.NombreFantasia));
        if (!string.IsNullOrWhiteSpace(config.RazonSocial))
            sb.AppendLine(FormatearCentro(config.RazonSocial));
        if (!string.IsNullOrWhiteSpace(config.CUIT))
            sb.AppendLine(FormatearCentro($"CUIT: {config.CUIT}"));
        if (!string.IsNullOrWhiteSpace(config.Direccion))
            sb.AppendLine(FormatearCentro(config.Direccion));
        if (!string.IsNullOrWhiteSpace(config.Telefono))
            sb.AppendLine(FormatearCentro($"Tel: {config.Telefono}"));

        sb.AppendLine(new string('-', ancho));
        sb.AppendLine($"COMPROBANTE: {venta.NumeroComprobante}");
        sb.AppendLine($"FECHA: {venta.Fecha:dd/MM/yyyy HH:mm:ss}");

        if (!string.IsNullOrWhiteSpace(venta.ClienteNombre))
            sb.AppendLine($"CLIENTE: {venta.ClienteNombre}");

        sb.AppendLine(new string('-', ancho));
        sb.AppendLine("DETALLE DE ARTICULOS");

        foreach (var linea in venta.Lineas)
        {
            var cantYDesc = $"{linea.Cantidad} x {linea.Descripcion}";
            if (cantYDesc.Length > ancho) cantYDesc = cantYDesc[..ancho];
            sb.AppendLine(cantYDesc);

            var detalle = $"  Talle: {linea.Talle} | Color: {linea.Color}";
            var precio = $"${linea.SubtotalCobrado:N2}";
            sb.AppendLine(FormatearDosColumnas(detalle, precio));

            if (linea.DescuentoOfertaUnitario > 0 || linea.DescuentoMedioPagoUnitario > 0)
            {
                var totalDesc = (linea.DescuentoOfertaUnitario + linea.DescuentoMedioPagoUnitario) * linea.Cantidad;
                sb.AppendLine($"  (Bonificación total: -${totalDesc:N2})");
            }
        }

        sb.AppendLine(new string('-', ancho));

        if (venta.TotalDescuentoOferta > 0 || venta.TotalDescuentoMedioPago > 0)
        {
            sb.AppendLine(FormatearDosColumnas("Subtotal Lista:", $"${venta.SubtotalLista:N2}"));
            if (venta.TotalDescuentoOferta > 0)
                sb.AppendLine(FormatearDosColumnas("Descuento Oferta:", $"-${venta.TotalDescuentoOferta:N2}"));
            if (venta.TotalDescuentoMedioPago > 0)
                sb.AppendLine(FormatearDosColumnas("Descuento Medio Pago:", $"-${venta.TotalDescuentoMedioPago:N2}"));
        }

        if (venta.TotalRecargoMedioPago > 0)
        {
            sb.AppendLine(FormatearDosColumnas("Recargo Financiación:", $"+${venta.TotalRecargoMedioPago:N2}"));
        }

        sb.AppendLine(FormatearDosColumnas("TOTAL COBRADO:", $"${venta.TotalFinalCobrado:N2}"));
        sb.AppendLine(new string('-', ancho));
        sb.AppendLine("FORMA DE PAGO:");

        foreach (var pago in venta.Pagos)
        {
            var canalTexto = pago.Canal.ToString();
            if (!string.IsNullOrWhiteSpace(pago.Referencia))
                canalTexto += $" ({pago.Referencia})";

            sb.AppendLine(FormatearDosColumnas($"  {canalTexto}", $"${pago.Monto:N2}"));
        }

        sb.AppendLine(new string('-', ancho));
        if (!string.IsNullOrWhiteSpace(config.MensajePie))
        {
            sb.AppendLine(FormatearCentro(config.MensajePie));
            sb.AppendLine(new string('-', ancho));
        }

        sb.AppendLine(FormatearCentro("POWERED BY [MR_SYS]"));
        sb.AppendLine(FormatearCentro("Terminal POS v1.0 • mrsys.app"));
        sb.AppendLine(new string('-', ancho));

        var contenidoTicket = sb.ToString();

        // Almacenar físicamente en disco
        try
        {
            var fechaCarpeta = venta.Fecha.ToString("yyyy-MM-dd");
            var directorioDestino = Path.Combine(config.RutaCarpetaTicketsArchivo, fechaCarpeta);
            Directory.CreateDirectory(directorioDestino);

            var nombreArchivo = $"{venta.NumeroComprobante}.txt";
            var rutaCompleta = Path.Combine(directorioDestino, nombreArchivo);

            await File.WriteAllTextAsync(rutaCompleta, contenidoTicket, Encoding.UTF8, cancellationToken);
        }
        catch
        {
            // Failsafe: Si hay problemas de permisos o ruta, no interrumpe el flujo principal
        }

        return contenidoTicket;
    }
}
