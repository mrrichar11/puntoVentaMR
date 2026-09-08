using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PuntoDeVenta.Application.DTOs.Asistente;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.Infrastructure.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKeyConfigurada;

    public bool EstaDisponible => !string.IsNullOrWhiteSpace(ObtenerApiKey());

    public GeminiService(HttpClient? httpClient = null, string? apiKey = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        _apiKeyConfigurada = apiKey;
    }

    public string ObtenerApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_apiKeyConfigurada))
        {
            return _apiKeyConfigurada.Trim();
        }

        // 1. Variable de entorno
        string? envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey.Trim();
        }

        // 2. Archivo gemini_key.txt en BaseDirectory o CurrentDirectory
        string[] posiblesRutas =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gemini_key.txt"),
            Path.Combine(Directory.GetCurrentDirectory(), "gemini_key.txt"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "gemini_key.txt")
        };

        foreach (var ruta in posiblesRutas)
        {
            try
            {
                if (File.Exists(ruta))
                {
                    var lineas = File.ReadAllLines(ruta);
                    foreach (var linea in lineas)
                    {
                        var limpia = linea.Trim();
                        if (!string.IsNullOrWhiteSpace(limpia) && !limpia.StartsWith("#") && !limpia.StartsWith("//"))
                        {
                            return limpia;
                        }
                    }
                }
            }
            catch
            {
                // Ignorar error de acceso a disco
            }
        }

        return string.Empty;
    }

    public async Task<List<BorradorCargaArticuloDto>?> InterpretarEntradaConGeminiAsync(
        string entradaUsuario,
        List<BorradorCargaArticuloDto>? borradoresPrevios = null,
        CancellationToken cancellationToken = default)
    {
        string apiKey = ObtenerApiKey();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(entradaUsuario))
        {
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(20)); // Timeout de 20s para respuestas complejas

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

            string contextoPrevio = string.Empty;
            if (borradoresPrevios != null && borradoresPrevios.Count > 0)
            {
                var sbContexto = new StringBuilder();
                sbContexto.AppendLine("Borradores de productos que ya se venían conversando en este turno:");
                for (int i = 0; i < borradoresPrevios.Count; i++)
                {
                    var bp = borradoresPrevios[i];
                    var variantesStr = bp.Variantes.Count > 0
                        ? string.Join(", ", bp.Variantes.Select(v => $"{v.Talle} {v.Color} ({v.Cantidad})"))
                        : $"Talles: [{string.Join(", ", bp.Talles)}], Colores: [{string.Join(", ", bp.Colores)}]";

                    sbContexto.AppendLine($"- Producto {i + 1}: {bp.NombreArticulo} | Categoría: {bp.CategoriaNombre} | Marca: {bp.MarcaNombre} | Costo: {bp.PrecioCosto} | Venta: {bp.PrecioLista} | Variantes: {variantesStr}");
                }
                contextoPrevio = sbContexto.ToString();
            }

            string systemPrompt =
                "Sos un asistente experto en retail textil e indumentaria de Argentina para el software MR SYS Retail. " +
                "Analizá el texto del comerciante y extraé cada prenda y sus combinaciones en un array de objetos JSON.\n\n" +
                (string.IsNullOrWhiteSpace(contextoPrevio) ? "" : $"{contextoPrevio}\n" +
                "INSTRUCCIÓN DE CONTINUIDAD / CORRECCIÓN:\n" +
                "Si el nuevo mensaje del comerciante es una corrección, aclaración o actualización de una prenda previa (ej: 'era en talle 3', 'cambiame el color a rojo', 'el costo es 8000', 'dos blancas en talle 3 y dos negras en talle 4'):\n" +
                "- PRESERVA el nombreArticulo, categoriaNombre, marcaNombre, precioCosto y precioVenta de la prenda previa a menos que el usuario indique cambiarlos.\n" +
                "- Aplica las correcciones de talles, colores o cantidades sobre esa prenda en lugar de crear un producto vacío.\n\n") +
                "Devolvé ÚNICAMENTE un array JSON válido donde cada objeto tenga las siguientes propiedades:\n" +
                "- nombreArticulo (string, ej: 'Remera Basica', 'Jean Con Brillos', 'Buzo Oversize')\n" +
                "- categoriaNombre (string, ej: 'Remeras', 'Pantalones', 'Buzos', 'Abrigos', 'Calzado')\n" +
                "- marcaNombre (string, ej: 'Genérica', 'Nike', 'Adidas')\n" +
                "- talles (array de strings con los talles de esta combinación, ej: ['3'] o ['4'])\n" +
                "- colores (array de strings con los colores de esta combinación, ej: ['Blanco'] o ['Negro'])\n" +
                "- cantidadTotal (int, cantidad de prendas de esta combinación)\n" +
                "- cantidadPorVariante (int, cantidad por variante)\n" +
                "- precioCosto (decimal, precio de costo/compra, ej: 7000 si dijo 'a 7000 pesos' o 'costo 7000')\n" +
                "- precioVenta (decimal, precio de venta al público si dijo 'los vendere a 15200' o 'venta 15200', o 0 si no se especificó)\n\n" +
                "IMPORTANTE:\n" +
                "1. Si el usuario indica distribuciones específicas (ej: 'dos de color blanca talle 3 y dos de color negra talle 4'), devolvé un elemento por cada combinación con su talle y color correspondiente.\n" +
                "2. NUNCA confundas cantidades con talles. Frases como 'uno de cada talle' significan cantidadPorVariante = 1, NO talle 1.\n" +
                "3. Si el usuario indica a cuánto lo va a vender ('los vendere a 15200', 'a vender a 15200', 'venta 15200'), ponelo en precioVenta.";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\nTexto a procesar: \"{entradaUsuario}\"" }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    string errBody = await response.Content.ReadAsStringAsync(cts.Token);
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gemini_error.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] HTTP {response.StatusCode}: {errBody}\n");
                }
                catch { }
                return null; // Fallback automático al motor local
            }

            string responseBody = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                return null;
            }

            string rawJson = parts[0].GetProperty("text").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            // Deserializar array de prendas
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var articulosJson = JsonSerializer.Deserialize<List<GeminiArticuloDto>>(rawJson, options);

            if (articulosJson == null || articulosJson.Count == 0)
            {
                return null;
            }

            // Agrupar por NombreArticulo (normalizado) y Categoría para que las variantes de una misma prenda
            // (ej. talle 2 blanco y talle 4 negro) formen un solo artículo con su matriz completa.
            var grupos = articulosJson
                .GroupBy(a => (
                    Nombre: (a.NombreArticulo ?? "Artículo").Trim().ToLowerInvariant(),
                    Categoria: (a.CategoriaNombre ?? "Indumentaria").Trim().ToLowerInvariant()
                ));

            var resultado = new List<BorradorCargaArticuloDto>();

            foreach (var grupo in grupos)
            {
                var primerItem = grupo.First();
                decimal precioVentaGrupo = primerItem.PrecioVenta > 0 ? primerItem.PrecioVenta : primerItem.PrecioLista;

                var borrador = new BorradorCargaArticuloDto
                {
                    NombreArticulo = primerItem.NombreArticulo ?? "Artículo",
                    CategoriaNombre = !string.IsNullOrWhiteSpace(primerItem.CategoriaNombre) ? primerItem.CategoriaNombre : "Indumentaria",
                    MarcaNombre = !string.IsNullOrWhiteSpace(primerItem.MarcaNombre) ? primerItem.MarcaNombre : "Genérica",
                    PrecioCosto = primerItem.PrecioCosto,
                    PrecioLista = precioVentaGrupo
                };

                // Procesar combinaciones de variantes para cada item del grupo
                foreach (var item in grupo)
                {
                    var tallesItem = item.Talles ?? new List<string>();
                    var coloresItem = item.Colores ?? new List<string>();
                    if (tallesItem.Count == 0 && item.CantidadTotal > 0) tallesItem.Add("Único");
                    if (coloresItem.Count == 0) coloresItem.Add("Único");

                    decimal costoItem = item.PrecioCosto > 0 ? item.PrecioCosto : borrador.PrecioCosto;
                    decimal ventaItem = item.PrecioVenta > 0 ? item.PrecioVenta : (item.PrecioLista > 0 ? item.PrecioLista : borrador.PrecioLista);

                    if (borrador.PrecioCosto == 0 && costoItem > 0) borrador.PrecioCosto = costoItem;
                    if (borrador.PrecioLista == 0 && ventaItem > 0) borrador.PrecioLista = ventaItem;

                    int cantVar = item.CantidadPorVariante > 0 ? item.CantidadPorVariante : (item.CantidadTotal > 0 ? item.CantidadTotal : 1);

                    foreach (var t in tallesItem)
                    {
                        if (!borrador.Talles.Contains(t)) borrador.Talles.Add(t);
                        foreach (var c in coloresItem)
                        {
                            if (!borrador.Colores.Contains(c)) borrador.Colores.Add(c);

                            string prefijo = BorradorCargaArticuloDto.GenerarPrefijoSku(borrador.NombreArticulo);
                            string colorCod = c.Length >= 3 ? c.Substring(0, 3).ToUpperInvariant() : c.ToUpperInvariant();
                            string sku = $"{prefijo}-{t.ToUpperInvariant()}-{colorCod}";

                            borrador.Variantes.Add(new BorradorVarianteDto
                            {
                                Sku = sku,
                                Talle = t,
                                Color = c,
                                Cantidad = cantVar,
                                PrecioCosto = costoItem,
                                PrecioLista = ventaItem
                            });
                        }
                    }
                }

                if (borrador.PrecioCosto > 0 && borrador.PrecioLista == 0)
                {
                    borrador.PrecioLista = Math.Round(borrador.PrecioCosto * 1.80m, 0);
                    foreach (var v in borrador.Variantes)
                    {
                        if (v.PrecioLista == 0) v.PrecioLista = borrador.PrecioLista;
                    }
                }

                borrador.CantidadTotal = borrador.Variantes.Sum(v => v.Cantidad);
                resultado.Add(borrador);
            }

            return resultado;
        }
        catch (Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gemini_error.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Exception: {ex}\n");
            }
            catch { }
            // Ante cualquier falla de red, JSON o timeout, retornar null para activar el fallback local
            return null;
        }
    }

    public async Task<RespuestaUniversalAsistenteDto?> InterpretarUniversalConGeminiAsync(
        string entradaUsuario,
        List<BorradorCargaArticuloDto>? borradoresPrevios = null,
        CancellationToken cancellationToken = default)
    {
        string apiKey = ObtenerApiKey();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(entradaUsuario))
        {
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

            string contextoPrevio = string.Empty;
            if (borradoresPrevios != null && borradoresPrevios.Count > 0)
            {
                var sbContexto = new StringBuilder();
                sbContexto.AppendLine("Borradores de productos en curso:");
                for (int i = 0; i < borradoresPrevios.Count; i++)
                {
                    var bp = borradoresPrevios[i];
                    sbContexto.AppendLine($"- Prenda {i + 1}: {bp.NombreArticulo} | Talles: [{string.Join(", ", bp.Talles)}] | Colores: [{string.Join(", ", bp.Colores)}] | Costo: {bp.PrecioCosto} | Venta: {bp.PrecioLista}");
                }
                contextoPrevio = sbContexto.ToString();
            }

            string systemPrompt =
                "Sos 'Mr. Bot', el copiloto inteligente de gestión para el software de retail 'MR SYS Retail' en Argentina.\n" +
                "Tu objetivo es entender la INTENCIÓN del comerciante a partir de lo que escribe o dicta por voz.\n\n" +
                "INTENCIONES POSIBLES:\n" +
                "1. 'cargar_articulo': Cuando ingresa mercadería, ropa, calzado o prendas adquiridas o producidas para sumar al stock/inventario del local (ej: 'Compré 5 jeans talle 40 al 48 a 33000', '2 remeras blancas talle 3').\n" +
                "2. 'deuda_cliente': Cuando LE VENDIÓ O FIÓ PRENDAS A UNA CLIENTA EN CUENTA CORRIENTE, o cuando anota una deuda previa o saldo de libreta/cuaderno fiado (ej: 'Le vendí a Andrea Colman una remera básica negra talle 4 en cta corriente', 'Anotale a Laura Benítez 15000 de la libreta vieja', 'Mariana se llevó un vestido fiado', 'Laura me debe 12000').\n" +
                "   - Si es una venta fiada o a cuenta corriente, extraé en 'nombreCliente' el nombre ('Andrea Colman'), en 'detalle' la prenda/concepto ('Venta cta. corriente: remera básica negra talle 4'), y en 'monto' el importe (si el usuario no dijo el precio, poné 0).\n" +
                "3. 'deuda_proveedor': Cuando quiere anotar una factura/deuda pendiente a un proveedor o fabricante (ej: 'Anota al proveedor Textil Sur que le debemos la factura 1029 por 180000').\n" +
                "4. 'nuevo_contacto': Cuando quiere dar de alta una clienta o proveedor (ej: 'Agregá a la clienta Carla Perez celular 11223344 con deuda de 5000').\n" +
                "5. 'consulta_general': Preguntas sobre el sistema o retail (ej: 'Cómo cobro con transferencia?', 'Qué margen me sugerís?').\n\n" +
                "REGLA CRÍTICA: Si el usuario dice 'le vendí a [persona] [prenda] en cuenta corriente / cta corriente / fiado / libreta', SIEMPRE clasificalo como 'deuda_cliente', NUNCA como 'cargar_articulo'.\n\n" +
                (string.IsNullOrWhiteSpace(contextoPrevio) ? "" : $"{contextoPrevio}\n") +
                "Devolvé ÚNICAMENTE un objeto JSON válido con la siguiente estructura:\n" +
                "{\n" +
                "  \"accion\": \"cargar_articulo\" | \"deuda_cliente\" | \"deuda_proveedor\" | \"nuevo_contacto\" | \"consulta_general\",\n" +
                "  \"mensaje\": string (breve respuesta cordial en español rioplatense explicando lo que preparaste),\n" +
                "  \"articulos\": [ ... array de prendas si accion == 'cargar_articulo' (con nombreArticulo, categoriaNombre, marcaNombre, talles, colores, cantidadTotal, cantidadPorVariante, precioCosto, precioVenta) ],\n" +
                "  \"deudaCliente\": { \"nombreCliente\": string, \"monto\": decimal, \"detalle\": string } (si accion == 'deuda_cliente'),\n" +
                "  \"deudaProveedor\": { \"nombreProveedor\": string, \"monto\": decimal, \"numeroFactura\": string, \"detalle\": string } (si accion == 'deuda_proveedor'),\n" +
                "  \"contacto\": { \"tipo\": \"Cliente\" | \"Proveedor\", \"nombre\": string, \"telefono\": string, \"cuit\": string, \"saldoInicial\": decimal, \"detalleSaldoInicial\": string, \"notas\": string } (si accion == 'nuevo_contacto')\n" +
                "}";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\nEntrada del comerciante: \"{entradaUsuario}\"" }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string responseBody = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            string rawJson = parts[0].GetProperty("text").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<RespuestaUniversalAsistenteDto>(rawJson, options);
        }
        catch (Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gemini_error.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Universal Exception: {ex}\n");
            }
            catch { }
            return null;
        }
    }

    private class GeminiArticuloDto
    {
        public string? NombreArticulo { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? MarcaNombre { get; set; }
        public List<string>? Talles { get; set; }
        public List<string>? Colores { get; set; }
        public int CantidadTotal { get; set; }
        public int CantidadPorVariante { get; set; } = 1;
        public decimal PrecioCosto { get; set; }
        public decimal PrecioLista { get; set; }
        public decimal PrecioVenta { get; set; }
    }
}
