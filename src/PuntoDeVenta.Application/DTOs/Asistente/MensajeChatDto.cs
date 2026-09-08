namespace PuntoDeVenta.Application.DTOs.Asistente;

public enum RolMensajeChat
{
    Usuario,
    Asistente,
    Sistema
}

public enum TipoAccionAsistente
{
    CargarArticulo,
    RegistrarDeudaCliente,
    RegistrarDeudaProveedor,
    CrearCliente,
    CrearProveedor,
    ConsultaGeneral
}

public class BorradorDeudaClienteDto
{
    public Guid? ClienteId { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Detalle { get; set; } = "Deuda previa anotada en cuaderno/libreta";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public bool ClienteExisteEnDb { get; set; }
}

public class BorradorDeudaProveedorDto
{
    public Guid? ProveedorId { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string NumeroComprobante { get; set; } = "FAC-PREVIA";
    public string Detalle { get; set; } = "Factura/deuda previa al sistema";
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public DateTime? FechaVencimiento { get; set; }
    public bool ProveedorExisteEnDb { get; set; }
}

public class BorradorContactoDto
{
    public string TipoContacto { get; set; } = "Cliente"; // "Cliente" o "Proveedor"
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public decimal SaldoInicial { get; set; }
    public string? DetalleSaldoInicial { get; set; }
    public string? Notas { get; set; }
}

public class RespuestaUniversalAsistenteDto
{
    public string Accion { get; set; } = "cargar_articulo"; // "cargar_articulo", "deuda_cliente", "deuda_proveedor", "nuevo_contacto", "consulta_general"
    public string? Mensaje { get; set; }
    public List<PrendaGeminiDto>? Articulos { get; set; }
    public DeudaClienteGeminiDto? DeudaCliente { get; set; }
    public DeudaProveedorGeminiDto? DeudaProveedor { get; set; }
    public ContactoGeminiDto? Contacto { get; set; }
}

public class PrendaGeminiDto
{
    public string NombreArticulo { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = "General";
    public string MarcaNombre { get; set; } = "Genérica";
    public List<string> Talles { get; set; } = new();
    public List<string> Colores { get; set; } = new();
    public int CantidadTotal { get; set; }
    public int CantidadPorVariante { get; set; } = 1;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
}

public class DeudaClienteGeminiDto
{
    public string NombreCliente { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Detalle { get; set; }
}

public class DeudaProveedorGeminiDto
{
    public string NombreProveedor { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? NumeroFactura { get; set; }
    public string? Detalle { get; set; }
}

public class ContactoGeminiDto
{
    public string Tipo { get; set; } = "Cliente";
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Cuit { get; set; }
    public decimal SaldoInicial { get; set; }
    public string? DetalleSaldoInicial { get; set; }
    public string? Notas { get; set; }
}

public class MensajeChatDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RolMensajeChat Rol { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public bool EsBorradorListoParaConfirmar { get; set; }
    public TipoAccionAsistente TipoAccion { get; set; } = TipoAccionAsistente.CargarArticulo;
    public BorradorDeudaClienteDto? BorradorDeudaCliente { get; set; }
    public BorradorDeudaProveedorDto? BorradorDeudaProveedor { get; set; }
    public BorradorContactoDto? BorradorContacto { get; set; }
    public bool TieneAccionParaConfirmar => EsBorradorListoParaConfirmar ||
                                           BorradorDeudaCliente != null ||
                                           BorradorDeudaProveedor != null ||
                                           BorradorContacto != null;
    public List<BorradorCargaArticuloDto> Borradores { get; set; } = new();
    public BorradorCargaArticuloDto? Borrador
    {
        get => Borradores.FirstOrDefault();
        set
        {
            Borradores.Clear();
            if (value != null)
            {
                Borradores.Add(value);
            }
        }
    }
    /// <summary>
    /// Motor que generó la respuesta: "Gemini", "Local", "LocalFallback" o vacío.
    /// </summary>
    public string MotorUtilizado { get; set; } = string.Empty;

    public string NombreRemitente
    {
        get
        {
            if (Rol == RolMensajeChat.Usuario) return "TÚ (VENDEDOR)";
            if (MotorUtilizado == "Gemini") return "[MR_BOT • ✨ IA]";
            if (MotorUtilizado == "LocalFallback") return "[MR_BOT • ⚡ Motor Local (Fallback)]";
            if (MotorUtilizado == "Local") return "[MR_BOT • ⚡ Motor Local]";
            return "[MR_BOT]";
        }
    }

    public string ColorRemitente
    {
        get
        {
            if (Rol == RolMensajeChat.Usuario) return "#B4D6FA";
            if (MotorUtilizado == "Gemini") return "#00E5FF";
            if (MotorUtilizado == "LocalFallback") return "#FFB74D";
            return "#00FF66";
        }
    }

    public string ColorFondo => Rol == RolMensajeChat.Usuario ? "#005A9E" : "#222725";

    public string ColorBorde
    {
        get
        {
            if (Rol == RolMensajeChat.Usuario) return "Transparent";
            if (MotorUtilizado == "Gemini") return "#00E5FF";
            if (MotorUtilizado == "LocalFallback") return "#FFB74D";
            return "#00FF66";
        }
    }

    public string ColorTexto => Rol == RolMensajeChat.Usuario ? "#FFFFFF" : "#EAEAEA";
    public string Alineacion => Rol == RolMensajeChat.Usuario ? "Right" : "Left";

    public static MensajeChatDto CrearMensajeUsuario(string texto) => new()
    {
        Rol = RolMensajeChat.Usuario,
        Texto = texto,
        FechaHora = DateTime.Now
    };

    public static MensajeChatDto CrearMensajeAsistente(string texto, BorradorCargaArticuloDto? borrador = null, bool listoParaConfirmar = false)
    {
        var msg = new MensajeChatDto
        {
            Rol = RolMensajeChat.Asistente,
            Texto = texto,
            FechaHora = DateTime.Now,
            EsBorradorListoParaConfirmar = listoParaConfirmar
        };
        if (borrador != null)
        {
            msg.Borradores.Add(borrador);
        }
        return msg;
    }

    public static MensajeChatDto CrearMensajeAsistenteMultiplesBorradores(string texto, List<BorradorCargaArticuloDto> borradores, bool listoParaConfirmar = false) => new()
    {
        Rol = RolMensajeChat.Asistente,
        Texto = texto,
        FechaHora = DateTime.Now,
        Borradores = borradores,
        EsBorradorListoParaConfirmar = listoParaConfirmar
    };
}
