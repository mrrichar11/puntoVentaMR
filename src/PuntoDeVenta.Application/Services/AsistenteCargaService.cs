using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Asistente;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Domain.Entities.Catalogo;

namespace PuntoDeVenta.Application.Services;

public class AsistenteCargaService : IAsistenteCargaService
{
    // Diccionarios de prendas y categorías asociadas
    // IMPORTANTE: Frases compuestas van primero para evitar coincidencias parciales tempranas
    private static readonly (string Pattern, string ArticuloBase, string Categoria)[] PrendasCompuestas = new[]
    {
        ("pantalones de jeans", "Jean", "Pantalones"),
        ("pantalon de jeans", "Jean", "Pantalones"),
        ("pantalones de jean", "Jean", "Pantalones"),
        ("pantalon de jean", "Jean", "Pantalones"),
        ("campera de cuero", "Campera de Cuero", "Abrigos"),
        ("campera de jean", "Campera de Jean", "Abrigos"),
        ("short de bano", "Short de Baño", "Shorts y Bermudas"),
        ("short de baño", "Short de Baño", "Shorts y Bermudas"),
        ("remera manga larga", "Remera Manga Larga", "Remeras"),
        ("remera manga corta", "Remera Manga Corta", "Remeras"),
        ("camisa de lino", "Camisa de Lino", "Camisas"),
        ("vestido de fiesta", "Vestido de Fiesta", "Vestidos")
    };

    private static readonly Dictionary<string, (string ArticuloBase, string Categoria)> PrendasSimples = new(StringComparer.OrdinalIgnoreCase)
    {
        { "remera", ("Remera", "Remeras") },
        { "remeras", ("Remera", "Remeras") },
        { "chomba", ("Chomba", "Chombas") },
        { "chombas", ("Chomba", "Chombas") },
        { "camisa", ("Camisa", "Camisas") },
        { "camisas", ("Camisa", "Camisas") },
        { "jean", ("Jean", "Pantalones") },
        { "jeans", ("Jean", "Pantalones") },
        { "pantalon", ("Pantalón", "Pantalones") },
        { "pantalones", ("Pantalón", "Pantalones") },
        { "buzo", ("Buzo", "Buzos") },
        { "buzos", ("Buzo", "Buzos") },
        { "campera", ("Campera", "Abrigos") },
        { "camperas", ("Campera", "Abrigos") },
        { "short", ("Short", "Shorts y Bermudas") },
        { "shorts", ("Short", "Shorts y Bermudas") },
        { "bermuda", ("Bermuda", "Shorts y Bermudas") },
        { "bermudas", ("Bermuda", "Shorts y Bermudas") },
        { "calza", ("Calza", "Calzas") },
        { "calzas", ("Calza", "Calzas") },
        { "pollera", ("Pollera", "Polleras") },
        { "polleras", ("Pollera", "Polleras") },
        { "falda", ("Falda", "Polleras") },
        { "faldas", ("Falda", "Polleras") },
        { "vestido", ("Vestido", "Vestidos") },
        { "vestidos", ("Vestido", "Vestidos") },
        { "top", ("Top", "Tops y Musculosas") },
        { "tops", ("Top", "Tops y Musculosas") },
        { "musculosa", ("Musculosa", "Tops y Musculosas") },
        { "musculosas", ("Musculosa", "Tops y Musculosas") },
        { "zapatilla", ("Zapatillas", "Calzado") },
        { "zapatillas", ("Zapatillas", "Calzado") },
        { "zapato", ("Zapatos", "Calzado") },
        { "zapatos", ("Zapatos", "Calzado") },
        { "bota", ("Botas", "Calzado") },
        { "botas", ("Botas", "Calzado") },
        { "sandalia", ("Sandalias", "Calzado") },
        { "sandalias", ("Sandalias", "Calzado") },
        { "gorra", ("Gorra", "Accesorios") },
        { "gorras", ("Gorra", "Accesorios") }
    };

    private static readonly string[] ModificadoresPrenda = new[]
    {
        "con brillos", "con tachas", "con roturas", "roturas", "tiro alto", "tiro medio", "tiro bajo",
        "wide leg", "cuello redondo", "cuello en v", "cuello v", "manga corta", "manga larga",
        "sin mangas", "con capucha", "oversize", "basica", "basicas", "basico", "basicos",
        "lisa", "lisas", "liso", "lisos", "estampada", "estampadas", "estampado", "estampados",
        "mom", "skinny", "cargo", "chupin", "recto", "oxford", "engomado", "engomada", "corta", "larga", "termica"
    };

    private static readonly Dictionary<string, string> ColoresEstandar = new(StringComparer.OrdinalIgnoreCase)
    {
        { "blanco", "Blanco" }, { "blanca", "Blanco" }, { "blancos", "Blanco" }, { "blancas", "Blanco" },
        { "negro", "Negro" }, { "negra", "Negro" }, { "negros", "Negro" }, { "negras", "Negro" },
        { "gris", "Gris" }, { "grises", "Gris" }, { "gris topo", "Gris Topo" }, { "gris melange", "Gris Melange" },
        { "azul", "Azul" }, { "azules", "Azul" }, { "azul marino", "Azul Marino" }, { "azul francia", "Azul Francia" },
        { "rojo", "Rojo" }, { "roja", "Rojo" }, { "rojos", "Rojo" }, { "rojas", "Rojo" },
        { "verde", "Verde" }, { "verdes", "Verde" }, { "verde militar", "Verde Militar" },
        { "amarillo", "Amarillo" }, { "amarilla", "Amarillo" },
        { "rosa", "Rosa" }, { "rosado", "Rosa" }, { "rosada", "Rosa" }, { "fucsia", "Fucsia" },
        { "beige", "Beige" }, { "crudo", "Crudo" }, { "cruda", "Crudo" }, { "marron", "Marrón" }, { "camel", "Camel" },
        { "naranja", "Naranja" }, { "naranjo", "Naranja" },
        { "violeta", "Violeta" }, { "lila", "Lila" },
        { "celeste", "Celeste" }, { "celestes", "Celeste" },
        { "bordo", "Bordó" }, { "bordo oscuro", "Bordó" }
    };

    private static readonly Dictionary<string, string> TallesTextoANumero = new(StringComparer.OrdinalIgnoreCase)
    {
        { "uno", "1" }, { "dos", "2" }, { "tres", "3" },
        { "cuatro", "4" }, { "cinco", "5" }, { "seis", "6" }
    };

    private readonly IInventarioService _inventarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguracionService _configuracionService;
    private readonly IGeminiService? _geminiService;
    private readonly ILicenseService? _licenseService;
    private readonly IClienteService? _clienteService;
    private readonly IProveedorService? _proveedorService;

    public AsistenteCargaService(
        IInventarioService inventarioService,
        IUnitOfWork unitOfWork,
        IConfiguracionService configuracionService,
        IGeminiService? geminiService = null,
        ILicenseService? licenseService = null,
        IClienteService? clienteService = null,
        IProveedorService? proveedorService = null)
    {
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _geminiService = geminiService;
        _licenseService = licenseService;
        _clienteService = clienteService;
        _proveedorService = proveedorService;
    }

    public async Task<MensajeChatDto> ProcesarMensajeAsync(
        string entradaUsuario,
        BorradorCargaArticuloDto? borradorActual = null,
        List<BorradorCargaArticuloDto>? borradoresExistentes = null,
        BorradorDeudaClienteDto? deudaClientePendiente = null,
        BorradorDeudaProveedorDto? deudaProveedorPendiente = null,
        BorradorContactoDto? contactoPendiente = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entradaUsuario))
        {
            return MensajeChatDto.CrearMensajeAsistente("No alcancé a leer tu mensaje. Por favor, decime qué prendas compraste o deseás registrar.");
        }

        // 1. Validación de Plan de Licencia (Premium con IA)
        if (_licenseService != null)
        {
            var licencia = await _licenseService.ValidarLicenciaAsync(cancellationToken);
            if (!licencia.TieneModuloIA)
            {
                return MensajeChatDto.CrearMensajeAsistente(
                    "⭐ El Asistente Inteligente [MR_BOT] es una funcionalidad exclusiva del Plan Premium de MR SYS Retail. " +
                    "Para desbloquearlo en tu local, contactá a soporte comercial o solicitá tu clave de activación Premium.");
            }
        }

        // 0. Si había una deuda de cliente pendiente de monto y el usuario responde con el importe
        if (deudaClientePendiente != null && deudaClientePendiente.Monto <= 0)
        {
            if (IntentarExtraerMonto(entradaUsuario, out decimal montoResp) && montoResp > 0)
            {
                string motor = (_geminiService != null && _geminiService.EstaDisponible) ? "Gemini" : "Local";
                return await PrepararMensajeDeudaClienteAsync(
                    deudaClientePendiente.NombreCliente,
                    montoResp,
                    deudaClientePendiente.Detalle,
                    motor,
                    cancellationToken);
            }
        }

        decimal margenSugerido = 80m; // Margen por defecto 80%

        // Borradores previos en curso (para contexto conversacional y correcciones)
        var borradoresPrevios = new List<BorradorCargaArticuloDto>();
        if (borradoresExistentes != null && borradoresExistentes.Count > 0)
        {
            borradoresPrevios.AddRange(borradoresExistentes);
        }
        else if (borradorActual != null)
        {
            borradoresPrevios.Add(borradorActual);
        }

        bool huboIntentoGemini = false;

        // 2. Consulta a Google Gemini en la nube si está disponible
        if (_geminiService != null && _geminiService.EstaDisponible)
        {
            huboIntentoGemini = true;

            // Intento universal: entiende deudas de clientas, facturas proveedores, contactos y stock
            var universalGemini = await _geminiService.InterpretarUniversalConGeminiAsync(entradaUsuario, borradoresPrevios, cancellationToken);
            if (universalGemini != null)
            {
                if (universalGemini.Accion == "deuda_cliente" && universalGemini.DeudaCliente != null)
                {
                    decimal monto = universalGemini.DeudaCliente.Monto;
                    string nombre = !string.IsNullOrWhiteSpace(universalGemini.DeudaCliente.NombreCliente) ? universalGemini.DeudaCliente.NombreCliente : "Clienta";
                    string detalle = !string.IsNullOrWhiteSpace(universalGemini.DeudaCliente.Detalle) ? universalGemini.DeudaCliente.Detalle : $"Venta en cuenta corriente ({entradaUsuario})";

                    if (monto <= 0)
                    {
                        monto = await IntentarObtenerPrecioDeArticuloAsync(detalle, entradaUsuario, cancellationToken);
                    }

                    if (monto > 0)
                    {
                        var msg = await PrepararMensajeDeudaClienteAsync(nombre, monto, detalle, "Gemini", cancellationToken);
                        return msg;
                    }
                    else
                    {
                        var borradorPendiente = new BorradorDeudaClienteDto
                        {
                            NombreCliente = nombre,
                            Monto = 0,
                            Detalle = detalle,
                            Fecha = DateTime.Today
                        };

                        return new MensajeChatDto
                        {
                            Rol = RolMensajeChat.Asistente,
                            Texto = $"📋 Entendí la venta en cuenta corriente para **{nombre}** (*{detalle}*).\n\n¿Por qué importe querés registrar la deuda en su libreta?",
                            MotorUtilizado = "Gemini",
                            TipoAccion = TipoAccionAsistente.RegistrarDeudaCliente,
                            BorradorDeudaCliente = borradorPendiente,
                            EsBorradorListoParaConfirmar = false
                        };
                    }
                }
                if (universalGemini.Accion == "deuda_proveedor" && universalGemini.DeudaProveedor != null && universalGemini.DeudaProveedor.Monto > 0)
                {
                    var msg = await PrepararMensajeDeudaProveedorAsync(universalGemini.DeudaProveedor.NombreProveedor, universalGemini.DeudaProveedor.Monto, universalGemini.DeudaProveedor.NumeroFactura, universalGemini.DeudaProveedor.Detalle, "Gemini", cancellationToken);
                    return msg;
                }
                if (universalGemini.Accion == "nuevo_contacto" && universalGemini.Contacto != null)
                {
                    var msg = PrepararMensajeNuevoContacto(universalGemini.Contacto, "Gemini");
                    return msg;
                }
                if (universalGemini.Accion == "consulta_general" && !string.IsNullOrWhiteSpace(universalGemini.Mensaje))
                {
                    return new MensajeChatDto
                    {
                        Rol = RolMensajeChat.Asistente,
                        Texto = universalGemini.Mensaje,
                        MotorUtilizado = "Gemini",
                        TipoAccion = TipoAccionAsistente.ConsultaGeneral
                    };
                }
                if (universalGemini.Accion == "cargar_articulo" && universalGemini.Articulos != null && universalGemini.Articulos.Count > 0)
                {
                    var borradoresGemini = MapearPrendasGemini(universalGemini.Articulos);
                    var respGemini = GenerarRespuestaMultiBorrador(borradoresGemini, margenSugerido);
                    respGemini.MotorUtilizado = "Gemini";
                    return respGemini;
                }
            }

            // Fallback a parser directo de prendas con Gemini si la respuesta universal fue vacía
            var borradoresGeminiAlt = await _geminiService.InterpretarEntradaConGeminiAsync(entradaUsuario, borradoresPrevios, cancellationToken);
            if (borradoresGeminiAlt != null && borradoresGeminiAlt.Count > 0)
            {
                var respGemini = GenerarRespuestaMultiBorrador(borradoresGeminiAlt, margenSugerido);
                respGemini.MotorUtilizado = "Gemini";
                return respGemini;
            }
        }

        string motorLocal = huboIntentoGemini ? "LocalFallback" : "Local";

        string textoLimpio = entradaUsuario.Trim();
        string textoNormalizado = RemoverAcentos(textoLimpio.ToLowerInvariant());

        // 3. Detección Local de Deuda de Clienta (Libreta de Fiados o Venta en Cta Corriente)
        if (EsIntencionDeudaClienteLocal(textoNormalizado, textoLimpio, out var dCliNombre, out var dCliMonto, out var dCliDetalle))
        {
            if (dCliMonto <= 0)
            {
                dCliMonto = await IntentarObtenerPrecioDeArticuloAsync(dCliDetalle, textoLimpio, cancellationToken);
            }

            if (dCliMonto > 0)
            {
                return await PrepararMensajeDeudaClienteAsync(dCliNombre, dCliMonto, dCliDetalle, motorLocal, cancellationToken);
            }
            else
            {
                var borradorPendiente = new BorradorDeudaClienteDto
                {
                    NombreCliente = dCliNombre,
                    Monto = 0,
                    Detalle = dCliDetalle,
                    Fecha = DateTime.Today
                };

                return new MensajeChatDto
                {
                    Rol = RolMensajeChat.Asistente,
                    Texto = $"📋 Entendí la venta en cuenta corriente para **{dCliNombre}** (*{dCliDetalle}*).\n\n¿Por qué importe querés registrar la deuda en su libreta?",
                    MotorUtilizado = motorLocal,
                    TipoAccion = TipoAccionAsistente.RegistrarDeudaCliente,
                    BorradorDeudaCliente = borradorPendiente,
                    EsBorradorListoParaConfirmar = false
                };
            }
        }

        // 4. Detección Local de Deuda a Proveedor
        if (EsIntencionDeudaProveedorLocal(textoNormalizado, textoLimpio, out var dProvNombre, out var dProvMonto, out var dProvFact, out var dProvDetalle))
        {
            return await PrepararMensajeDeudaProveedorAsync(dProvNombre, dProvMonto, dProvFact, dProvDetalle, motorLocal, cancellationToken);
        }

        bool esContextoCompra = Regex.IsMatch(textoNormalizado, @"\b(?:compr[eé]|ingres[eé]|cargame|carg[aá]|pagu[eé])\b");

        // 1. Segmentar en frases si el usuario ingresó múltiples productos en un solo mensaje
        var segmentos = SegmentarEntradaMultiProducto(textoLimpio, textoNormalizado);

        // Lista de borradores acumulados
        var borradoresResultado = new List<BorradorCargaArticuloDto>(borradoresPrevios);

        if (segmentos.Count > 1)
        {
            // Múltiples productos ingresados a la vez
            var nuevosBorradores = new List<BorradorCargaArticuloDto>();
            foreach (var seg in segmentos)
            {
                var borradorSeg = new BorradorCargaArticuloDto();
                ProcesarTextoEnBorrador(seg, esContextoCompra, margenSugerido, borradorSeg);
                nuevosBorradores.Add(borradorSeg);
            }

            var resp = GenerarRespuestaMultiBorrador(nuevosBorradores, margenSugerido);
            resp.MotorUtilizado = motorLocal;
            return resp;
        }
        else
        {
            // Un solo segmento
            // Si ya teníamos borradores previos incompletos, determinar si este mensaje completa alguno
            BorradorCargaArticuloDto borradorTrabajo;
            if (borradoresResultado.Count > 0)
            {
                // Buscar si coincide con alguno existente o tomar el primer incompleto
                var borradorIncompleto = borradoresResultado.FirstOrDefault(b => !b.EsValidoParaGuardar) ?? borradoresResultado.Last();
                borradorTrabajo = borradorIncompleto;
            }
            else
            {
                borradorTrabajo = new BorradorCargaArticuloDto();
                borradoresResultado.Add(borradorTrabajo);
            }

            ProcesarTextoEnBorrador(textoLimpio, esContextoCompra, margenSugerido, borradorTrabajo);

            if (borradoresResultado.Count > 1)
            {
                var resp = GenerarRespuestaMultiBorrador(borradoresResultado, margenSugerido);
                resp.MotorUtilizado = motorLocal;
                return resp;
            }
            else
            {
                var resp = GenerarRespuestaConversacional(borradorTrabajo, margenSugerido);
                resp.MotorUtilizado = motorLocal;
                return resp;
            }
        }
    }

    public async Task<int> ConfirmarBorradorAsync(
        BorradorCargaArticuloDto borrador,
        CancellationToken cancellationToken = default)
    {
        if (borrador.Colores.Count == 0)
        {
            borrador.Colores.Add("Único");
            if (borrador.Variantes.Count == 0)
            {
                borrador.GenerarMatrizVariantes();
            }
        }

        if (!borrador.EsValidoParaGuardar)
        {
            throw new InvalidOperationException("El borrador no tiene todos los datos necesarios para ser guardado.");
        }

        // 1. Asegurar Categoría
        var categorias = await _unitOfWork.Categorias.GetAllAsync(cancellationToken);
        var categoria = categorias.FirstOrDefault(c => c.Nombre.Equals(borrador.CategoriaNombre, StringComparison.OrdinalIgnoreCase));
        if (categoria == null)
        {
            categoria = new Categoria { Nombre = borrador.CategoriaNombre, Descripcion = "Creada automáticamente por MR_BOT" };
            await _unitOfWork.Categorias.AddAsync(categoria, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 2. Asegurar Marca
        var marcas = await _unitOfWork.Marcas.GetAllAsync(cancellationToken);
        var marca = marcas.FirstOrDefault(m => m.Nombre.Equals(borrador.MarcaNombre, StringComparison.OrdinalIgnoreCase));
        if (marca == null)
        {
            marca = new Marca { Nombre = borrador.MarcaNombre };
            await _unitOfWork.Marcas.AddAsync(marca, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 3. Generar Código de Estilo si está vacío
        if (string.IsNullOrWhiteSpace(borrador.CodigoEstilo))
        {
            string slug = Regex.Replace(borrador.NombreArticulo.ToUpper(), @"[^A-Z0-9]", "");
            string corto = slug.Length > 8 ? slug.Substring(0, 8) : slug;
            borrador.CodigoEstilo = $"{corto}-{DateTime.Now:MMdd}";
        }

        // 4. Mapear al DTO del servicio de Inventario
        var dto = new CrearArticuloDto
        {
            CodigoEstilo = borrador.CodigoEstilo,
            Nombre = borrador.NombreArticulo,
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            Temporada = borrador.Temporada,
            Genero = borrador.Genero,
            PrecioCosto = borrador.PrecioCosto,
            PrecioLista = borrador.PrecioLista,
            PrecioOferta = borrador.PrecioOferta,
            PermiteDescuentoMedioPago = true,
            StockMinimo = 1,
            Talles = borrador.Talles,
            Colores = borrador.Colores,
            StockInicialDefecto = borrador.CantidadPorVariante
        };

        // Si tenemos stock específico por variante en el borrador, mapearlo
        if (borrador.Variantes.Count > 0)
        {
            var dictStock = new Dictionary<(string Talle, string Color), int>();
            foreach (var v in borrador.Variantes)
            {
                dictStock[(v.Talle, v.Color)] = v.Cantidad;
            }
            dto.StockInicialPorCombinacion = dictStock;
            dto.SoloCombinacionesEspecificadas = true;
        }

        var articulo = await _inventarioService.CrearArticuloConMatrizAsync(dto, cancellationToken);
        return articulo.Variantes?.Count ?? (borrador.Talles.Count * borrador.Colores.Count);
    }

    public async Task<int> ConfirmarTodosAsync(
        IEnumerable<BorradorCargaArticuloDto> borradores,
        CancellationToken cancellationToken = default)
    {
        int totalVariantes = 0;
        foreach (var b in borradores.Where(b => b.EsValidoParaGuardar))
        {
            totalVariantes += await ConfirmarBorradorAsync(b, cancellationToken);
        }
        return totalVariantes;
    }

    /// <summary>
    /// Divide frases que describen múltiples productos separadas por saltos de línea,
    /// signos de puntuación, o conectores 'y', 'ademas', 'tambien' seguidos por una nueva prenda o cantidad.
    /// </summary>
    private static List<string> SegmentarEntradaMultiProducto(string original, string normalizado)
    {
        var lista = new List<string>();

        // Regex para detectar cortes de prenda: ' y 5 pantalones', ' ademas 2 buzos', '; remeras', etc.
        // Se asegura de NO dividir en 'talles 2 y 4' ni en 'blanco y negro'
        var patronCorte = new Regex(
            @"(?:[\r\n]+|;\s*|\.\s+|\b(?:y|e|ademas|tambien)\s+(?=(?:(?:compr[eé]|ingres[eé]|cargame|carg[aá]|tengo|hay)\s+)?(?:\d+\s+)?(?:un[as]?\s+)?(?:remeras?|chombas?|camisas?|jeans?|pantalones?|pantalon|buzos?|camperas?|shorts?|bermudas?|calzas?|polleras?|faldas?|vestidos?|tops?|musculosas?|zapatillas?|zapatos?|botas?|sandalias?|gorras?)\b))",
            RegexOptions.IgnoreCase);

        var matches = patronCorte.Matches(normalizado);
        if (matches.Count == 0)
        {
            lista.Add(original);
            return lista;
        }

        int ultimoIndice = 0;
        foreach (Match match in matches)
        {
            string segmento = original.Substring(ultimoIndice, match.Index - ultimoIndice).Trim();
            if (!string.IsNullOrWhiteSpace(segmento))
            {
                lista.Add(segmento);
            }
            ultimoIndice = match.Index + match.Length;
        }

        if (ultimoIndice < original.Length)
        {
            string ultimoSegmento = original.Substring(ultimoIndice).Trim();
            if (!string.IsNullOrWhiteSpace(ultimoSegmento))
            {
                lista.Add(ultimoSegmento);
            }
        }

        return lista.Count > 0 ? lista : new List<string> { original };
    }

    private static void ProcesarTextoEnBorrador(
        string textoLimpio,
        bool esContextoCompra,
        decimal margenSugerido,
        BorradorCargaArticuloDto borrador)
    {
        string normalizado = RemoverAcentos(textoLimpio.ToLowerInvariant());

        // 1. Extraer Prenda y Modelo
        ExtraerPrendaYModelo(normalizado, textoLimpio, borrador);

        // 2. Extraer Marca
        ExtraerMarca(normalizado, textoLimpio, borrador);

        // 3. Extraer Cantidades
        ExtraerCantidades(normalizado, borrador);

        // 4. Extraer Talles (con delimitación estricta)
        ExtraerTalles(normalizado, borrador);

        // 5. Extraer Colores
        ExtraerColores(normalizado, borrador);

        // 6. Extraer Precios y Costos
        ExtraerPrecios(normalizado, esContextoCompra, borrador, margenSugerido);

        // 7. Ajustar cantidades y matriz
        if (borrador.Talles.Count > 0)
        {
            if (borrador.Colores.Count == 0 && !string.IsNullOrWhiteSpace(borrador.NombreArticulo))
            {
                // Si aún no especificó color, para armar la matriz provisional asumimos 'Único'
                // solo si ya tiene costos listos
                if (borrador.PrecioCosto > 0 && borrador.PrecioLista > 0)
                {
                    borrador.Colores.Add("Único");
                }
            }

            if (borrador.Colores.Count > 0)
            {
                borrador.GenerarMatrizVariantes();
            }
        }
    }

    private static void ExtraerPrendaYModelo(string normalizado, string original, BorradorCargaArticuloDto borrador)
    {
        if (!string.IsNullOrWhiteSpace(borrador.NombreArticulo))
            return;

        string? articuloBase = null;
        string? categoria = null;

        // 1. Buscar frases compuestas primero (ej: pantalones de jeans -> Jean)
        foreach (var (pattern, art, cat) in PrendasCompuestas)
        {
            if (normalizado.Contains(pattern))
            {
                articuloBase = art;
                categoria = cat;
                break;
            }
        }

        // 2. Si no hubo coincidencia compuesta, buscar prendas simples
        if (articuloBase == null)
        {
            foreach (var kvp in PrendasSimples)
            {
                if (Regex.IsMatch(normalizado, $@"\b{Regex.Escape(kvp.Key)}\b"))
                {
                    articuloBase = kvp.Value.ArticuloBase;
                    categoria = kvp.Value.Categoria;
                    break;
                }
            }
        }

        if (articuloBase != null)
        {
            // Buscar modificador (ej: con brillos, oversize, basica, mom, etc.)
            string modificadorEncontrado = string.Empty;
            foreach (var mod in ModificadoresPrenda)
            {
                if (normalizado.Contains(mod))
                {
                    modificadorEncontrado = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mod);
                    break;
                }
            }

            borrador.NombreArticulo = string.IsNullOrEmpty(modificadorEncontrado)
                ? articuloBase
                : $"{articuloBase} {modificadorEncontrado}";

            borrador.CategoriaNombre = categoria ?? "Indumentaria";
            return;
        }

        // Fallback genérico: "Compré X..."
        var match = Regex.Match(original, @"(?:compr[eé]|ingres[eé]|cargame|carg[aá]|cargar)\s+(?:\d+\s+)?([A-Za-zÁÉÍÓÚáéíóúñÑ\s]+?)(?:,\s*|talles?|color|marca|costo|precio|\$|$)", RegexOptions.IgnoreCase);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            string candidato = match.Groups[1].Value.Trim();
            if (candidato.Length > 2 && !candidato.Equals("el", StringComparison.OrdinalIgnoreCase) && !candidato.Equals("la", StringComparison.OrdinalIgnoreCase))
            {
                borrador.NombreArticulo = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(candidato);
            }
        }
    }

    private static void ExtraerMarca(string normalizado, string original, BorradorCargaArticuloDto borrador)
    {
        var matchMarcaExplicita = Regex.Match(original, @"\bmarca\s+([A-Za-z0-9\.\-\']+)", RegexOptions.IgnoreCase);
        if (matchMarcaExplicita.Success)
        {
            borrador.MarcaNombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(matchMarcaExplicita.Groups[1].Value.Trim());
            return;
        }

        string[] marcasComunes = { "nike", "adidas", "puma", "zara", "topper", "fila", "converse", "vans", "levis", "lacoste", "tommy" };
        foreach (var m in marcasComunes)
        {
            if (Regex.IsMatch(normalizado, $@"\b{m}\b"))
            {
                borrador.MarcaNombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m);
                return;
            }
        }
    }

    private static void ExtraerCantidades(string normalizado, BorradorCargaArticuloDto borrador)
    {
        // 1. Detectar 'uno de cada talle', '1 de cada talle', 'dos de cada', etc.
        if (Regex.IsMatch(normalizado, @"\b(?:uno|una|1)\s+de\s+cada\b"))
        {
            borrador.CantidadPorVariante = 1;
        }
        else if (Regex.IsMatch(normalizado, @"\b(?:dos|2)\s+de\s+cada\b"))
        {
            borrador.CantidadPorVariante = 2;
        }
        else
        {
            var matchDeCada = Regex.Match(normalizado, @"\b(\d+)\s+de\s+cada\b");
            if (matchDeCada.Success && int.TryParse(matchDeCada.Groups[1].Value, out int cDeCada))
            {
                borrador.CantidadPorVariante = cDeCada;
            }
        }

        // 2. Detectar cantidad total ingresada: 'compre 2 remeras', '5 pantalones', '6 unidades'
        var matchCantidad = Regex.Match(normalizado, @"\b(?:compr[eé]|ingres[eé]|cargame|carg[aá]|tengo|hay)\s+(\d+)\b");
        if (matchCantidad.Success && int.TryParse(matchCantidad.Groups[1].Value, out int cant))
        {
            borrador.CantidadTotal = cant;
        }
        else
        {
            var matchPrendas = Regex.Match(normalizado, @"\b(\d+)\s*(?:remeras|pantalones|jeans|buzos|camisas|camperas|shorts|bermudas|calzas|polleras|vestidos|prendas|unidades|pares|u\.?)\b");
            if (matchPrendas.Success && int.TryParse(matchPrendas.Groups[1].Value, out int cantP))
            {
                borrador.CantidadTotal = cantP;
            }
            else
            {
                // Número al inicio de frase: ej '2 remeras blancas...' o '5 pantalones...'
                var matchInicio = Regex.Match(normalizado, @"^\s*(\d+)\s+[a-z]");
                if (matchInicio.Success && int.TryParse(matchInicio.Groups[1].Value, out int cantIni))
                {
                    borrador.CantidadTotal = cantIni;
                }
            }
        }
    }

    private static void ExtraerTalles(string normalizado, BorradorCargaArticuloDto borrador)
    {
        // Delimitación estricta de la cláusula de talles
        // Se detiene al encontrar precios ('a 7000', 'costo', '$'), palabras clave ('uno de cada', 'color', 'marca'), o signos de puntuación
        var matchTalles = Regex.Match(normalizado,
            @"\btalles?\s*(?:de|del|son|:)?\s*(.+?)(?=\b(?:color|colores|marca|costo|costos|pagu[eé]|sali[oó]|precio|precios|venta|ventas|a\s+\$?\d+|\$|pesos|uno de cada|una de cada|\d+\s+de cada|de cada talle|cada uno|cada una|con brillos|con tachas|con roturas)\b|[\.;]|$)",
            RegexOptions.IgnoreCase);

        if (matchTalles.Success)
        {
            string textoTalles = matchTalles.Groups[1].Value.Trim();

            // Rango numérico: 'del 1 al 5', '1 al 5', 'del 40 al 48'
            var matchRango = Regex.Match(textoTalles, @"(?:del\s+)?(\d+)\s*(?:al|a|-)\s*(\d+)");
            if (matchRango.Success && int.TryParse(matchRango.Groups[1].Value, out int rInicio) && int.TryParse(matchRango.Groups[2].Value, out int rFin))
            {
                if (rFin >= rInicio && (rFin - rInicio) <= 15)
                {
                    // Si el inicio es par y el fin es par y >= 34 (pantalones/calzado), avanzar de a 2
                    int paso = (rInicio >= 34 && rInicio % 2 == 0 && rFin % 2 == 0) ? 2 : 1;
                    for (int i = rInicio; i <= rFin; i += paso)
                    {
                        string strI = i.ToString();
                        if (!borrador.Talles.Contains(strI)) borrador.Talles.Add(strI);
                    }
                }
            }

            // Talles en palabras escritas ('tres y cuatro')
            foreach (var kvp in TallesTextoANumero)
            {
                if (Regex.IsMatch(textoTalles, $@"\b{kvp.Key}\b"))
                {
                    if (!borrador.Talles.Contains(kvp.Value))
                    {
                        borrador.Talles.Add(kvp.Value);
                    }
                }
            }

            // Talles numéricos individuales (1 al 64)
            var regexNum = new Regex(@"\b([1-9]|[1-5][0-9]|6[0-4])\b");
            var matchesNum = regexNum.Matches(textoTalles);
            foreach (Match m in matchesNum)
            {
                string val = m.Groups[1].Value;
                if (!borrador.Talles.Contains(val))
                {
                    borrador.Talles.Add(val);
                }
            }

            // Talles por letras (XS, S, M, L, XL, XXL, etc.)
            string[] letrasTalles = { "xxs", "xs", "s", "m", "l", "xl", "xxl", "2xl", "3xl", "4xl", "u", "unico" };
            foreach (var lt in letrasTalles)
            {
                if (Regex.IsMatch(textoTalles, $@"\b{lt}\b"))
                {
                    string talleUpper = lt.ToUpperInvariant();
                    if (!borrador.Talles.Contains(talleUpper))
                    {
                        borrador.Talles.Add(talleUpper);
                    }
                }
            }
        }
        else
        {
            // Si no hubo palabra 'talle/s', SOLO reconocer letras inequívocas de talle (S, M, L, XL)
            // o talles de calzado/pantalón (36 a 54) si vienen en lista, NUNCA números 1 al 6 aislados
            string[] letrasTalles = { "xxs", "xs", "s", "m", "l", "xl", "xxl" };
            foreach (var lt in letrasTalles)
            {
                if (Regex.IsMatch(normalizado, $@"\b{lt}\b"))
                {
                    string talleUpper = lt.ToUpperInvariant();
                    if (!borrador.Talles.Contains(talleUpper))
                    {
                        borrador.Talles.Add(talleUpper);
                    }
                }
            }

            // Si el texto tiene lista de números grandes (ej: '38, 40, 42')
            var matchesGrandes = Regex.Matches(normalizado, @"\b(3[4-9]|4[0-8]|5[0-4])\b");
            if (matchesGrandes.Count >= 2)
            {
                foreach (Match m in matchesGrandes)
                {
                    if (!borrador.Talles.Contains(m.Value))
                    {
                        borrador.Talles.Add(m.Value);
                    }
                }
            }
        }
    }

    private static void ExtraerColores(string normalizado, BorradorCargaArticuloDto borrador)
    {
        foreach (var kvp in ColoresEstandar)
        {
            if (Regex.IsMatch(normalizado, $@"\b{Regex.Escape(kvp.Key)}\b"))
            {
                if (!borrador.Colores.Contains(kvp.Value))
                {
                    borrador.Colores.Add(kvp.Value);
                }
            }
        }
    }

    private static void ExtraerPrecios(string normalizado, bool esContextoCompra, BorradorCargaArticuloDto borrador, decimal margenSugerido)
    {
        // 1. Costo explícito:
        // Ejemplos admitidos:
        //   "el precio de costo de los pantalones es de 20500"
        //   "el precio de costo de los jeans es de 20500"
        //   "costo de los jeans es de 20500"
        //   "costo $6000", "costo 6.000", "pague 6000", "salieron 6000", "costaron 6000"
        //   "20500 de costo", "20500 costo"
        var matchCosto = Regex.Match(normalizado,
            @"(?:precio\s+de\s+costo|costo|pagu[eé]|costaron|sali[oó])(?:\s+(?:de|del|los|las|el|la|[a-z]+)){0,6}\s*(?:es\s+(?:de\s+)?)?\$?\s*(\d+(?:[\.,]\d+)?)",
            RegexOptions.IgnoreCase);

        if (matchCosto.Success && decimal.TryParse(matchCosto.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal costo))
        {
            borrador.PrecioCosto = costo;
        }
        else
        {
            // Número seguido de 'de costo' (ej: '20500 de costo', '20500 costo')
            var matchNumCosto = Regex.Match(normalizado, @"(\d+(?:[\.,]\d+)?)\s*(?:pesos|\$)?\s*(?:de\s+)?costo\b", RegexOptions.IgnoreCase);
            if (matchNumCosto.Success && decimal.TryParse(matchNumCosto.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nCosto))
            {
                borrador.PrecioCosto = nCosto;
            }
        }

        // 2. Venta explícita:
        // Ejemplos admitidos:
        //   "el precio de venta de los pantalones es de 35000"
        //   "precio de venta es de 35000", "venta es de 35000"
        //   "venta $14000", "venta 14000", "vender a 14000", "vendo a 14000", "los vendere a 15200"
        //   "35000 de venta", "35000 venta"
        // NOTA: No debe capturar si es 'precio de costo'
        var matchVenta = Regex.Match(normalizado,
            @"(?:precio\s+de\s+venta|precio\s+lista|venta|vender[eé]|venderia|venderemos|vender|vendo|lista)(?:\s+(?:de|del|los|las|el|la|[a-z]+)){0,6}\s*(?:es\s+(?:de\s+)?|a\s+)?\$?\s*(\d+(?:[\.,]\d+)?)",
            RegexOptions.IgnoreCase);

        if (matchVenta.Success && decimal.TryParse(matchVenta.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal venta))
        {
            borrador.PrecioLista = venta;
        }
        else
        {
            // "precio $14000" o "precio 14000" solo si no dice 'precio de costo'
            if (!normalizado.Contains("precio de costo") && !normalizado.Contains("de costo"))
            {
                var matchPrecioSolo = Regex.Match(normalizado, @"\bprecio\s*(?:es\s+(?:de\s+)?)?\$?\s*(\d+(?:[\.,]\d+)?)", RegexOptions.IgnoreCase);
                if (matchPrecioSolo.Success && decimal.TryParse(matchPrecioSolo.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pSolo))
                {
                    borrador.PrecioLista = pSolo;
                }
            }

            if (borrador.PrecioLista == 0)
            {
                // Número seguido de 'de venta'
                var matchNumVenta = Regex.Match(normalizado, @"(\d+(?:[\.,]\d+)?)\s*(?:pesos|\$)?\s*(?:de\s+)?(?:venta|lista)\b", RegexOptions.IgnoreCase);
                if (matchNumVenta.Success && decimal.TryParse(matchNumVenta.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nVenta))
                {
                    borrador.PrecioLista = nVenta;
                }
            }
        }

        // 3. Precio ambiguo: "a 7000 pesos", "a 7000", "a $7000"
        if (borrador.PrecioCosto == 0)
        {
            var matchA = Regex.Match(normalizado, @"\ba\s+\$?\s*(\d+(?:[\.,]\d+)?)\s*(?:pesos|\$)?(?:\s|$|,|\.)");
            if (matchA.Success && decimal.TryParse(matchA.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precioA))
            {
                // Si el contexto general fue de compra ('compré', 'ingresé') o no se dijo venta, se asume costo de compra
                borrador.PrecioCosto = precioA;
            }
        }

        // 4. Si el usuario escribe únicamente un número en este turno (ej: '20500' o '$20500')
        if (borrador.PrecioCosto == 0)
        {
            var matchNumeroSolo = Regex.Match(normalizado, @"^\s*\$?(\d+(?:[\.,]\d+)?)\s*(?:pesos|\$)?\s*$");
            if (matchNumeroSolo.Success && decimal.TryParse(matchNumeroSolo.Groups[1].Value.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soloNum))
            {
                borrador.PrecioCosto = soloNum;
            }
        }

        // Si tenemos costo y no venta, calculamos la venta sugerida con el margen por defecto
        if (borrador.PrecioCosto > 0 && borrador.PrecioLista == 0)
        {
            borrador.PrecioLista = Math.Round(borrador.PrecioCosto * (1m + (margenSugerido / 100m)), 0);
        }
    }

    private static MensajeChatDto GenerarRespuestaConversacional(BorradorCargaArticuloDto borrador, decimal margenSugerido)
    {
        var sb = new StringBuilder();
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(borrador.NombreArticulo))
        {
            faltantes.Add("el tipo de prenda (ej. remera básica, buzo, jean)");
        }

        if (borrador.Talles.Count == 0)
        {
            faltantes.Add("los talles (ej. S, M, L o números 1, 2, 3)");
        }

        if (borrador.Colores.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(borrador.NombreArticulo) && borrador.Talles.Count > 0 && borrador.PrecioCosto > 0)
            {
                borrador.Colores.Add("Único");
            }
            else
            {
                faltantes.Add("el color (ej. blanco, negro o único)");
            }
        }

        if (borrador.PrecioCosto <= 0)
        {
            faltantes.Add("el precio de costo");
        }

        if (borrador.PrecioLista <= 0)
        {
            faltantes.Add("el precio de venta al público");
        }

        borrador.DatosFaltantes = faltantes;

        // Caso 1: Faltan datos esenciales
        if (faltantes.Count > 0)
        {
            sb.AppendLine("Entendido. Identifiqué los siguientes datos:");
            if (!string.IsNullOrWhiteSpace(borrador.NombreArticulo))
                sb.AppendLine($"• Producto: {borrador.NombreArticulo}");
            if (borrador.Talles.Count > 0)
                sb.AppendLine($"• Talles: {string.Join(", ", borrador.Talles)}");
            if (borrador.Colores.Count > 0)
                sb.AppendLine($"• Colores: {string.Join(", ", borrador.Colores)}");
            if (borrador.PrecioCosto > 0)
                sb.AppendLine($"• Costo: ${borrador.PrecioCosto:N0}");
            if (borrador.PrecioLista > 0)
                sb.AppendLine($"• Precio de Venta: ${borrador.PrecioLista:N0}");

            sb.AppendLine();
            sb.AppendLine($"👉 Para completar el borrador, por favor indicame: {string.Join(" y ", faltantes)}.");

            return MensajeChatDto.CrearMensajeAsistente(sb.ToString(), borrador, listoParaConfirmar: false);
        }

        // Caso 2: Tenemos todos los datos necesarios para armar la matriz
        if (borrador.Variantes.Count == 0)
        {
            borrador.GenerarMatrizVariantes();
        }

        sb.AppendLine($"¡Excelente! He preparado el borrador para {borrador.NombreArticulo}.");
        sb.AppendLine($"• Marca: {borrador.MarcaNombre} | Categoría: {borrador.CategoriaNombre}");

        if (borrador.Variantes.Count > 0)
        {
            var resumen = string.Join(", ", borrador.Variantes.Select(v => $"{v.Talle} {v.Color} ({v.Cantidad})"));
            sb.AppendLine($"• Matriz: {borrador.Variantes.Count} variantes ({resumen}) = {borrador.TotalPrendas} prendas en total.");
        }
        else
        {
            sb.AppendLine($"• Matriz: {borrador.Talles.Count} talles ({string.Join(", ", borrador.Talles)}) × {borrador.Colores.Count} colores ({string.Join(", ", borrador.Colores)}) = {borrador.Variantes.Count} variantes ({borrador.TotalPrendas} prendas en total).");
        }
        decimal precioCalculado = Math.Round(borrador.PrecioCosto * (1m + (margenSugerido / 100m)), 0);
        string detalleMargenConv = borrador.PrecioLista == precioCalculado ? $" (Precio sugerido +{margenSugerido:N0}%)" : "";
        sb.AppendLine($"• Finanzas: Costo: ${borrador.PrecioCosto:N0} | Precio Lista: ${borrador.PrecioLista:N0}{detalleMargenConv}");
        sb.AppendLine();
        sb.AppendLine("Revisá la tabla de variantes en pantalla y pulsá [🚀 Confirmar e Ingresar a Inventario] para darles de alta.");

        return MensajeChatDto.CrearMensajeAsistente(sb.ToString(), borrador, listoParaConfirmar: true);
    }

    private static MensajeChatDto GenerarRespuestaMultiBorrador(List<BorradorCargaArticuloDto> borradores, decimal margenSugerido)
    {
        var sb = new StringBuilder();
        int listos = 0;

        sb.AppendLine($"¡Excelente! Procesé {borradores.Count} productos de tu mensaje:");
        sb.AppendLine();

        for (int i = 0; i < borradores.Count; i++)
        {
            var b = borradores[i];
            var faltantes = new List<string>();

            if (string.IsNullOrWhiteSpace(b.NombreArticulo)) faltantes.Add("tipo de prenda");
            if (b.Talles.Count == 0) faltantes.Add("talles");
            if (b.Colores.Count == 0)
            {
                if (b.PrecioCosto > 0 && b.Talles.Count > 0)
                {
                    b.Colores.Add("Único");
                }
                else
                {
                    faltantes.Add("color");
                }
            }
            if (b.PrecioCosto <= 0) faltantes.Add("precio de costo");
            if (b.PrecioLista <= 0) faltantes.Add("precio de venta");

            b.DatosFaltantes = faltantes;

            if (b.Variantes.Count == 0 && b.Talles.Count > 0 && b.Colores.Count > 0)
            {
                b.GenerarMatrizVariantes();
            }

            bool estaListo = b.EsValidoParaGuardar;
            if (estaListo) listos++;

            string nombre = string.IsNullOrWhiteSpace(b.NombreArticulo) ? $"Producto {i + 1}" : b.NombreArticulo;
            sb.AppendLine($"{i + 1}. {nombre}");
            sb.AppendLine($"• Categoría: {b.CategoriaNombre} | Marca: {b.MarcaNombre}");

            if (b.Variantes.Count > 0)
            {
                var resumenVariantes = string.Join(", ", b.Variantes.Select(v => $"{v.Talle} {v.Color} ({v.Cantidad})"));
                sb.AppendLine($"• Matriz: {b.Variantes.Count} variantes ({resumenVariantes}) = {b.TotalPrendas} prendas en total.");
            }
            else if (b.Talles.Count > 0)
            {
                string infoColores = b.Colores.Count > 0 ? $" × {string.Join(", ", b.Colores)}" : "";
                sb.AppendLine($"• Matriz: {b.Talles.Count} talles ({string.Join(", ", b.Talles)}){infoColores} = {b.TotalPrendas} prendas en total.");
            }

            if (b.PrecioCosto > 0)
            {
                decimal precioCalculado = Math.Round(b.PrecioCosto * (1m + (margenSugerido / 100m)), 0);
                string detalleMargen = b.PrecioLista == precioCalculado ? $" (Precio sugerido +{margenSugerido:N0}%)" : "";
                sb.AppendLine($"• Finanzas: Costo: ${b.PrecioCosto:N0} | Precio Lista: ${b.PrecioLista:N0}{detalleMargen}");
            }

            if (estaListo)
            {
                sb.AppendLine("✓ ¡Borrador listo para guardar!");
            }
            else
            {
                sb.AppendLine($"👉 Para este producto, por favor indicame: {string.Join(" y ", faltantes)}.");
            }

            sb.AppendLine();
        }

        if (listos == borradores.Count)
        {
            sb.AppendLine("Todos los borradores están listos. Podés revisarlos en el panel derecho y pulsar [🚀 Confirmar Todos] o guardar uno por uno.");
        }
        else
        {
            sb.AppendLine("Podés revisar los borradores en el panel derecho e indicarme los datos faltantes para completarlos.");
        }

        bool alMenosUnoListo = listos > 0;
        return MensajeChatDto.CrearMensajeAsistenteMultiplesBorradores(sb.ToString().TrimEnd(), borradores, listoParaConfirmar: alMenosUnoListo);
    }

    private static string RemoverAcentos(string texto)
    {
        string normalized = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public async Task<PuntoDeVenta.Application.DTOs.Clientes.MovimientoCuentaCorrienteDto> ConfirmarDeudaClienteAsync(
        BorradorDeudaClienteDto borrador,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(borrador);
        if (borrador.Monto <= 0)
            throw new InvalidOperationException("El monto debe ser mayor a cero.");

        Guid clienteId;
        if (borrador.ClienteId.HasValue)
        {
            clienteId = borrador.ClienteId.Value;
        }
        else
        {
            string nombreBuscado = borrador.NombreCliente.Trim().ToLowerInvariant();
            var clientes = await _unitOfWork.Clientes.FindAsync(c => c.Activo && c.NombreCompleto.ToLower().Contains(nombreBuscado), cancellationToken);
            var clienteExistente = clientes.FirstOrDefault();
            if (clienteExistente != null)
            {
                clienteId = clienteExistente.Id;
            }
            else
            {
                if (_clienteService != null)
                {
                    var nuevo = await _clienteService.CrearClienteAsync(new PuntoDeVenta.Application.DTOs.Clientes.ClienteDto
                    {
                        NombreCompleto = borrador.NombreCliente.Trim(),
                        SaldoInicial = borrador.Monto,
                        DetalleSaldoInicial = borrador.Detalle
                    }, cancellationToken);
                    var hist = await _clienteService.ObtenerHistorialCuentaCorrienteAsync(nuevo.Id, cancellationToken);
                    return hist.FirstOrDefault() ?? new PuntoDeVenta.Application.DTOs.Clientes.MovimientoCuentaCorrienteDto
                    {
                        Monto = borrador.Monto,
                        SaldoResultante = borrador.Monto,
                        Detalle = borrador.Detalle,
                        Fecha = DateTime.UtcNow
                    };
                }
                throw new InvalidOperationException("No se encontró el servicio de clientes para dar de alta.");
            }
        }

        if (_clienteService != null)
        {
            var mov = await _clienteService.RegistrarSaldoPrevioHistoricoAsync(new PuntoDeVenta.Application.DTOs.Clientes.RegistrarSaldoPrevioClienteDto
            {
                ClienteId = clienteId,
                MontoDeudaPrevia = borrador.Monto,
                Detalle = borrador.Detalle,
                FechaHistorica = DateTime.SpecifyKind(borrador.Fecha, DateTimeKind.Utc)
            }, cancellationToken);

            return new PuntoDeVenta.Application.DTOs.Clientes.MovimientoCuentaCorrienteDto
            {
                Id = mov.Id,
                Tipo = mov.Tipo.ToString(),
                Monto = mov.Monto,
                SaldoPrevio = mov.SaldoPrevio,
                SaldoResultante = mov.SaldoResultante,
                CanalCobro = mov.CanalCobro.ToString(),
                Detalle = mov.Detalle,
                ReferenciaComprobante = mov.ReferenciaComprobante,
                Fecha = mov.Fecha
            };
        }

        throw new InvalidOperationException("Servicio de clientes no disponible.");
    }

    public async Task<PuntoDeVenta.Domain.Entities.Proveedores.CompraProveedor> ConfirmarDeudaProveedorAsync(
        BorradorDeudaProveedorDto borrador,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(borrador);
        if (borrador.Monto <= 0)
            throw new InvalidOperationException("El monto debe ser mayor a cero.");

        Guid proveedorId;
        if (borrador.ProveedorId.HasValue)
        {
            proveedorId = borrador.ProveedorId.Value;
        }
        else
        {
            string nombreBuscado = borrador.NombreProveedor.Trim().ToLowerInvariant();
            var provs = await _unitOfWork.Proveedores.FindAsync(p => p.Activo && p.RazonSocial.ToLower().Contains(nombreBuscado), cancellationToken);
            var pExistente = provs.FirstOrDefault();
            if (pExistente != null)
            {
                proveedorId = pExistente.Id;
            }
            else
            {
                if (_proveedorService != null)
                {
                    var nuevo = await _proveedorService.CrearProveedorAsync(new PuntoDeVenta.Application.DTOs.Proveedores.ProveedorDto
                    {
                        RazonSocial = borrador.NombreProveedor.Trim(),
                        SaldoInicial = borrador.Monto,
                        NumeroComprobanteSaldoInicial = borrador.NumeroComprobante,
                        DetalleSaldoInicial = borrador.Detalle
                    }, cancellationToken);
                    proveedorId = nuevo.Id;
                }
                else
                {
                    throw new InvalidOperationException("Servicio de proveedores no disponible.");
                }
            }
        }

        if (_proveedorService != null)
        {
            return await _proveedorService.RegistrarDeudaPreviaAsync(new PuntoDeVenta.Application.DTOs.Proveedores.RegistrarDeudaPreviaProveedorDto
            {
                ProveedorId = proveedorId,
                MontoDeudaPrevia = borrador.Monto,
                NumeroFacturaComprobante = borrador.NumeroComprobante,
                DetalleObservaciones = borrador.Detalle,
                FechaComprobante = DateTime.SpecifyKind(borrador.FechaEmision, DateTimeKind.Utc),
                FechaVencimiento = borrador.FechaVencimiento.HasValue ? DateTime.SpecifyKind(borrador.FechaVencimiento.Value, DateTimeKind.Utc) : null
            }, cancellationToken);
        }

        throw new InvalidOperationException("Servicio de proveedores no disponible.");
    }

    public async Task<object> ConfirmarNuevoContactoAsync(
        BorradorContactoDto borrador,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(borrador);
        if (borrador.TipoContacto.Equals("Proveedor", StringComparison.OrdinalIgnoreCase))
        {
            if (_proveedorService == null) throw new InvalidOperationException("Servicio de proveedores no disponible.");
            return await _proveedorService.CrearProveedorAsync(new PuntoDeVenta.Application.DTOs.Proveedores.ProveedorDto
            {
                RazonSocial = borrador.Nombre.Trim(),
                Telefono = borrador.Telefono,
                Cuit = borrador.Cuit,
                Email = borrador.Email,
                Direccion = borrador.Direccion,
                Notas = borrador.Notas,
                SaldoInicial = borrador.SaldoInicial,
                DetalleSaldoInicial = borrador.DetalleSaldoInicial
            }, cancellationToken);
        }
        else
        {
            if (_clienteService == null) throw new InvalidOperationException("Servicio de clientes no disponible.");
            return await _clienteService.CrearClienteAsync(new PuntoDeVenta.Application.DTOs.Clientes.ClienteDto
            {
                NombreCompleto = borrador.Nombre.Trim(),
                Telefono = borrador.Telefono,
                DocumentoIdentidad = borrador.Cuit,
                Email = borrador.Email,
                Direccion = borrador.Direccion,
                Notas = borrador.Notas,
                SaldoInicial = borrador.SaldoInicial,
                DetalleSaldoInicial = borrador.DetalleSaldoInicial
            }, cancellationToken);
        }
    }

    private async Task<MensajeChatDto> PrepararMensajeDeudaClienteAsync(string nombreCliente, decimal monto, string? detalle, string motor, CancellationToken cancellationToken)
    {
        string nombreLimpio = (nombreCliente ?? string.Empty).Trim();
        string det = string.IsNullOrWhiteSpace(detalle) ? "Deuda previa anotada en libreta" : detalle.Trim();

        var borrador = new BorradorDeudaClienteDto
        {
            NombreCliente = nombreLimpio,
            Monto = monto,
            Detalle = det,
            Fecha = DateTime.Today
        };

        var clientes = await _unitOfWork.Clientes.FindAsync(c => c.Activo && c.NombreCompleto.ToLower().Contains(nombreLimpio.ToLowerInvariant()), cancellationToken);
        var cliEncontrada = clientes.FirstOrDefault();

        var sb = new StringBuilder();
        if (cliEncontrada != null)
        {
            borrador.ClienteId = cliEncontrada.Id;
            borrador.NombreCliente = cliEncontrada.NombreCompleto;
            borrador.ClienteExisteEnDb = true;

            sb.AppendLine($"📋 Encontré la ficha de la clienta **{cliEncontrada.NombreCompleto}**.");
            sb.AppendLine($"• Saldo deudor actual: **${cliEncontrada.SaldoDeudorActual:N2}**");
            sb.AppendLine($"• Monto a anotar: **${monto:N2}**");
            sb.AppendLine($"• Concepto: {det}");
            sb.AppendLine();
            sb.AppendLine("Hacé clic en **[✓ Confirmar Deuda en Cuenta Corriente]** para asentar la deuda sin mover caja.");
        }
        else
        {
            borrador.ClienteExisteEnDb = false;
            sb.AppendLine($"📋 Preparé el registro de deuda para **{nombreLimpio}** por **${monto:N2}**.");
            sb.AppendLine($"• Concepto: {det}");
            sb.AppendLine();
            sb.AppendLine("Nota: Esta clienta aún no figura en el sistema. Al confirmar, se dará de alta automáticamente con este saldo deudor.");
        }

        return new MensajeChatDto
        {
            Rol = RolMensajeChat.Asistente,
            Texto = sb.ToString().TrimEnd(),
            MotorUtilizado = motor,
            TipoAccion = TipoAccionAsistente.RegistrarDeudaCliente,
            BorradorDeudaCliente = borrador,
            EsBorradorListoParaConfirmar = true
        };
    }

    private async Task<MensajeChatDto> PrepararMensajeDeudaProveedorAsync(string nombreProveedor, decimal monto, string? nroFactura, string? detalle, string motor, CancellationToken cancellationToken)
    {
        string nombreLimpio = (nombreProveedor ?? string.Empty).Trim();
        string comp = string.IsNullOrWhiteSpace(nroFactura) ? "FAC-PREVIA" : nroFactura.Trim();
        string det = string.IsNullOrWhiteSpace(detalle) ? "Factura previa adeudada" : detalle.Trim();

        var borrador = new BorradorDeudaProveedorDto
        {
            NombreProveedor = nombreLimpio,
            Monto = monto,
            NumeroComprobante = comp,
            Detalle = det,
            FechaEmision = DateTime.Today
        };

        var provs = await _unitOfWork.Proveedores.FindAsync(p => p.Activo && p.RazonSocial.ToLower().Contains(nombreLimpio.ToLowerInvariant()), cancellationToken);
        var provEncontrado = provs.FirstOrDefault();

        var sb = new StringBuilder();
        if (provEncontrado != null)
        {
            borrador.ProveedorId = provEncontrado.Id;
            borrador.NombreProveedor = provEncontrado.RazonSocial;
            borrador.ProveedorExisteEnDb = true;

            sb.AppendLine($"🏭 Encontré al proveedor **{provEncontrado.RazonSocial}**.");
            sb.AppendLine($"• Deuda acumulada actual: **${provEncontrado.SaldoDeudorActual:N2}**");
            sb.AppendLine($"• Factura / Comprobante: **{comp}**");
            sb.AppendLine($"• Importe a asentar: **${monto:N2}**");
            sb.AppendLine();
            sb.AppendLine("Hacé clic en **[✓ Asentar Factura en Cuentas a Pagar]** para registrar la deuda sin ingresar stock.");
        }
        else
        {
            borrador.ProveedorExisteEnDb = false;
            sb.AppendLine($"🏭 Preparé la deuda a proveedor para **{nombreLimpio}** por **${monto:N2}** (Comprobante: {comp}).");
            sb.AppendLine();
            sb.AppendLine("Nota: Este proveedor es nuevo. Al confirmar, se creará su ficha y se asentará la factura en cuentas a pagar.");
        }

        return new MensajeChatDto
        {
            Rol = RolMensajeChat.Asistente,
            Texto = sb.ToString().TrimEnd(),
            MotorUtilizado = motor,
            TipoAccion = TipoAccionAsistente.RegistrarDeudaProveedor,
            BorradorDeudaProveedor = borrador,
            EsBorradorListoParaConfirmar = true
        };
    }

    private MensajeChatDto PrepararMensajeNuevoContacto(ContactoGeminiDto c, string motor)
    {
        var borrador = new BorradorContactoDto
        {
            TipoContacto = c.Tipo ?? "Cliente",
            Nombre = c.Nombre,
            Telefono = c.Telefono,
            Cuit = c.Cuit,
            SaldoInicial = c.SaldoInicial,
            DetalleSaldoInicial = c.DetalleSaldoInicial,
            Notas = c.Notas
        };

        string saldoTexto = borrador.SaldoInicial > 0 ? $" (Saldo inicial: ${borrador.SaldoInicial:N2})" : "";
        string texto = $"👤 Preparé el alta de **{borrador.TipoContacto}**: **{borrador.Nombre}**{saldoTexto}.\n\n¿Confirmás guardarlo en el sistema?";

        return new MensajeChatDto
        {
            Rol = RolMensajeChat.Asistente,
            Texto = texto,
            MotorUtilizado = motor,
            TipoAccion = borrador.TipoContacto == "Proveedor" ? TipoAccionAsistente.CrearProveedor : TipoAccionAsistente.CrearCliente,
            BorradorContacto = borrador,
            EsBorradorListoParaConfirmar = true
        };
    }

    private async Task<decimal> IntentarObtenerPrecioDeArticuloAsync(string detalle, string entradaUsuario, CancellationToken cancellationToken)
    {
        try
        {
            var variantes = await _unitOfWork.Variantes.GetAllAsync(cancellationToken);
            if (variantes == null || !variantes.Any()) return 0;

            var articulos = await _unitOfWork.Articulos.GetAllAsync(cancellationToken);
            var dictArticulos = articulos.ToDictionary(a => a.Id, a => a.Nombre);

            string normalizado = RemoverAcentos($"{detalle} {entradaUsuario}".ToLowerInvariant());

            // 1. Buscar variante cuyo artículo padre coincida con el texto
            var candidato = variantes
                .Where(v => v.Activo && v.PrecioLista > 0 && dictArticulos.ContainsKey(v.ArticuloId))
                .OrderByDescending(v => dictArticulos[v.ArticuloId].Length)
                .FirstOrDefault(v => normalizado.Contains(RemoverAcentos(dictArticulos[v.ArticuloId].ToLowerInvariant())));

            if (candidato != null)
            {
                return candidato.PrecioLista;
            }

            // 2. Buscar por palabras clave de prenda (ej: remera, jean, pantalon, vestido)
            var palabras = normalizado.Split(new[] { ' ', ',', '.', '-', '/', ';', ':', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 4 && !EsPalabraNoArticulo(p))
                .ToList();

            foreach (var v in variantes.Where(v => v.Activo && v.PrecioLista > 0 && dictArticulos.ContainsKey(v.ArticuloId)))
            {
                string nombreNorm = RemoverAcentos(dictArticulos[v.ArticuloId].ToLowerInvariant());
                if (palabras.Any(p => nombreNorm.Contains(p)))
                {
                    return v.PrecioLista;
                }
            }
        }
        catch
        {
            // Ignorar y continuar
        }

        return 0;
    }

    private static bool EsPalabraNoArticulo(string palabra)
    {
        string p = palabra.ToLowerInvariant();
        return p is "vendi" or "vendio" or "venta" or "anota" or "anotale" or "ponele" or "cliente" or "clienta"
            or "cuenta" or "corriente" or "fiado" or "libreta" or "cuaderno" or "para" or "talle" or "talla"
            or "color" or "pesos" or "cada" or "negro" or "negra" or "blanco" or "blanca" or "azul" or "rojo"
            or "verde" or "rosa" or "gris" or "andrea" or "colman";
    }

    private static bool IntentarExtraerMonto(string texto, out decimal monto)
    {
        monto = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        string normalizado = RemoverAcentos(texto.ToLowerInvariant().Trim());

        // Patrón miles: "15 mil", "15k", "15.5 mil"
        var matchMil = Regex.Match(normalizado, @"(?:\$|\b)(\d+(?:[.,]\d+)?)\s*(?:mil|k)\b");
        if (matchMil.Success && decimal.TryParse(matchMil.Groups[1].Value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var miles))
        {
            monto = miles * 1000m;
            return true;
        }

        // Patrón con separador de miles y opcional decimal: "$14.500", "14.500,50", "14500", "$ 15000"
        var matchMonto = Regex.Match(normalizado, @"(?:\$|\b)(\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d{1,7}(?:[.,]\d{1,2})?)\s*(?:pesos|pe|\$)?\b");
        if (matchMonto.Success)
        {
            string raw = matchMonto.Groups[1].Value;
            if (raw.Contains('.') && Regex.IsMatch(raw, @"^\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?$"))
            {
                raw = raw.Replace(".", "").Replace(",", ".");
            }
            else
            {
                raw = raw.Replace(",", ".");
            }

            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var m) && m > 0)
            {
                monto = m;
                return true;
            }
        }

        return false;
    }

    private static bool EsIntencionDeudaClienteLocal(string normalizado, string original, out string nombre, out decimal monto, out string detalle)
    {
        nombre = string.Empty;
        monto = 0;
        detalle = "Deuda previa anotada en libreta/cuaderno";

        bool tienePalabraClave = Regex.IsMatch(normalizado, @"\b(?:anota|anotale|anotar|ponele|cargale|debe|deuda|libreta|cuaderno|fiado|vendi|le vendi|se llevo|cuenta corriente|cta corriente|cta cte)\b");
        if (!tienePalabraClave) return false;

        bool esCtaCorriente = Regex.IsMatch(normalizado, @"\b(?:cuenta corriente|cta corriente|cta cte|fiado|libreta|cuaderno)\b");
        bool esVenta = Regex.IsMatch(normalizado, @"\b(?:vendi|le vendi|se llevo)\b");

        // Si no es libreta/cta corriente y tampoco dice anota/debe, descartar
        if (!esCtaCorriente && !Regex.IsMatch(normalizado, @"\b(?:anota|anotale|anotar|ponele|cargale|debe|deuda)\b"))
        {
            return false;
        }

        // Intentar extraer monto sin confundir números de talle (ej: "talle 4")
        string sinTalles = Regex.Replace(normalizado, @"\b(?:talle|talla|t)\s*\d+\b", "", RegexOptions.IgnoreCase);
        if (IntentarExtraerMonto(sinTalles, out decimal montoParsed))
        {
            monto = montoParsed;
        }

        // Extraer nombre de la persona (ej: "a andrea colman", "para andrea", "de andrea")
        var matchNombre = Regex.Match(original, @"(?i)\b(?:a|para|de|clienta)\s+([A-ZÁÉÍÓÚÑa-záéíóúñ]+(?:\s+[A-ZÁÉÍÓÚÑa-záéíóúñ]+)?)\b");
        if (matchNombre.Success)
        {
            string cand = matchNombre.Groups[1].Value.Trim();
            string candNorm = RemoverAcentos(cand.ToLowerInvariant());
            if (!candNorm.Equals("la") && !candNorm.Equals("el") && !candNorm.Equals("un") && !candNorm.Equals("una") && !candNorm.Equals("cuenta") && !candNorm.Equals("cta") && !candNorm.Equals("mi"))
            {
                nombre = cand;
            }
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = "Clienta";
        }

        if (esVenta || esCtaCorriente)
        {
            detalle = $"Venta cta. corriente ({original})";
        }
        else
        {
            detalle = $"Deuda anotada por asistente ({original})";
        }

        return true;
    }

    private static bool EsIntencionDeudaProveedorLocal(string normalizado, string original, out string nombre, out decimal monto, out string nroFactura, out string detalle)
    {
        nombre = string.Empty;
        monto = 0;
        nroFactura = "FAC-PREVIA";
        detalle = "Factura previa adeudada a proveedor";

        bool tienePalabraClave = Regex.IsMatch(normalizado, @"\b(?:proveedor|fabricante|debemos|debemos a|le debemos)\b");
        if (!tienePalabraClave) return false;

        var matchMonto = Regex.Match(normalizado, @"(?:\$|\b)(\d{1,7}(?:[.,]\d{2})?)\s*(?:pesos|pe|\$)?\b");
        if (!matchMonto.Success || !decimal.TryParse(matchMonto.Groups[1].Value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var montoParsed) || montoParsed <= 0)
        {
            return false;
        }
        monto = montoParsed;

        var matchFact = Regex.Match(original, @"(?i)(?:factura|fac|comprobante)\s*(?:nro|n°|num)?\s*([A-Za-z0-9-]+)");
        if (matchFact.Success)
        {
            nroFactura = matchFact.Groups[1].Value.Trim();
        }

        var matchNombre = Regex.Match(original, @"(?i)\b(?:proveedor|fabricante|a)\s+([A-ZÁÉÍÓÚÑa-záéíóúñ0-9]+(?:\s+[A-ZÁÉÍÓÚÑa-záéíóúñ0-9]+)?)\b");
        if (matchNombre.Success)
        {
            nombre = matchNombre.Groups[1].Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = "Proveedor";
        }

        detalle = $"Factura adeudada ({original})";
        return true;
    }

    private List<BorradorCargaArticuloDto> MapearPrendasGemini(List<PrendaGeminiDto> prendas)
    {
        var resultado = new List<BorradorCargaArticuloDto>();
        foreach (var p in prendas)
        {
            var b = new BorradorCargaArticuloDto
            {
                NombreArticulo = p.NombreArticulo,
                CategoriaNombre = p.CategoriaNombre,
                MarcaNombre = p.MarcaNombre,
                Talles = p.Talles ?? new List<string>(),
                Colores = p.Colores ?? new List<string>(),
                PrecioCosto = p.PrecioCosto,
                PrecioLista = p.PrecioVenta > 0 ? p.PrecioVenta : Math.Round(p.PrecioCosto * 1.80m, 0)
            };
            int cant = p.CantidadPorVariante > 0 ? p.CantidadPorVariante : (p.CantidadTotal > 0 ? p.CantidadTotal : 1);
            foreach (var t in b.Talles)
            {
                foreach (var c in b.Colores)
                {
                    b.Variantes.Add(new BorradorVarianteDto
                    {
                        Sku = $"{p.NombreArticulo.Substring(0, Math.Min(3, p.NombreArticulo.Length)).ToUpper()}-{t}-{c.Substring(0, Math.Min(3, c.Length)).ToUpper()}",
                        Talle = t,
                        Color = c,
                        Cantidad = cant,
                        PrecioCosto = b.PrecioCosto,
                        PrecioLista = b.PrecioLista
                    });
                }
            }
            b.CantidadTotal = b.Variantes.Sum(v => v.Cantidad);
            resultado.Add(b);
        }
        return resultado;
    }
}
