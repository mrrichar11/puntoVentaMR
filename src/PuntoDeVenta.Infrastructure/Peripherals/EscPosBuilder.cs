using System.Text;

namespace PuntoDeVenta.Infrastructure.Peripherals;

/// <summary>
/// Generador fluido de secuencias de bytes ESC/POS para impresoras térmicas de tickets.
/// </summary>
public class EscPosBuilder
{
    private readonly MemoryStream _stream = new();
    private readonly Encoding _encoding;

    public EscPosBuilder(Encoding? encoding = null)
    {
        // ISO-8859-1 (Latin1) cubre acentos y signos estándar en la mayoría de impresoras térmicas comerciales
        _encoding = encoding ?? Encoding.Latin1;
    }

    /// <summary>
    /// Comando ESC @ (0x1B, 0x40): Inicializa la impresora y restablece los valores predeterminados.
    /// </summary>
    public EscPosBuilder Initialize()
    {
        _stream.WriteByte(0x1B);
        _stream.WriteByte(0x40);
        return this;
    }

    /// <summary>
    /// Alinea el texto al centro (ESC a 1).
    /// </summary>
    public EscPosBuilder AlignCenter()
    {
        _stream.Write(new byte[] { 0x1B, 0x61, 0x01 });
        return this;
    }

    /// <summary>
    /// Alinea el texto a la izquierda (ESC a 0).
    /// </summary>
    public EscPosBuilder AlignLeft()
    {
        _stream.Write(new byte[] { 0x1B, 0x61, 0x00 });
        return this;
    }

    /// <summary>
    /// Alinea el texto a la derecha (ESC a 2).
    /// </summary>
    public EscPosBuilder AlignRight()
    {
        _stream.Write(new byte[] { 0x1B, 0x61, 0x02 });
        return this;
    }

    /// <summary>
    /// Activa o desactiva texto en negrita (ESC E n).
    /// </summary>
    public EscPosBuilder Bold(bool enable = true)
    {
        _stream.Write(new byte[] { 0x1B, 0x45, (byte)(enable ? 0x01 : 0x00) });
        return this;
    }

    /// <summary>
    /// Fuente de doble tamaño (GS ! 0x11 para doble ancho y alto).
    /// </summary>
    public EscPosBuilder DoubleSize(bool enable = true)
    {
        _stream.Write(new byte[] { 0x1D, 0x21, (byte)(enable ? 0x11 : 0x00) });
        return this;
    }

    /// <summary>
    /// Agrega texto sin salto de línea.
    /// </summary>
    public EscPosBuilder Text(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            var bytes = _encoding.GetBytes(text);
            _stream.Write(bytes, 0, bytes.Length);
        }
        return this;
    }

    /// <summary>
    /// Agrega una línea de texto seguida de salto de línea (\n).
    /// </summary>
    public EscPosBuilder Line(string text = "")
    {
        Text(text);
        _stream.WriteByte(0x0A); // LF
        return this;
    }

    /// <summary>
    /// Genera una línea separadora continua según el ancho del papel.
    /// </summary>
    public EscPosBuilder Separator(int width = 48)
    {
        Line(new string('-', width));
        return this;
    }

    /// <summary>
    /// Genera una fila formateada en dos columnas alineadas (Izquierda y Derecha).
    /// Ideal para "TOTAL: ... $50.000" o "Efectivo: ... $20.000".
    /// </summary>
    public EscPosBuilder KeyValueLine(string key, string value, int totalWidth = 48)
    {
        var spacesCount = totalWidth - (key.Length + value.Length);
        if (spacesCount < 1) spacesCount = 1;

        Line(key + new string(' ', spacesCount) + value);
        return this;
    }

    /// <summary>
    /// Emite el pulso eléctrico por RJ11 para disparar el cajón monedero:
    /// ESC p 0 25 250 (0x1B, 0x70, 0x00, 0x19, 0xFA).
    /// </summary>
    public EscPosBuilder CashDrawerKick()
    {
        _stream.Write(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA });
        return this;
    }

    /// <summary>
    /// Avanza N líneas de papel (ESC d n).
    /// </summary>
    public EscPosBuilder FeedLines(byte lines = 3)
    {
        _stream.Write(new byte[] { 0x1B, 0x64, lines });
        return this;
    }

    /// <summary>
    /// Corta el papel automáticamente (GS V B 0 / 0x1D, 0x56, 0x42, 0x00).
    /// </summary>
    public EscPosBuilder CutPaper()
    {
        FeedLines(3);
        _stream.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 });
        return this;
    }

    /// <summary>
    /// Obtiene el arreglo final de bytes para su transmisión al spooler.
    /// </summary>
    public byte[] Build()
    {
        return _stream.ToArray();
    }
}
