using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.Infrastructure.Services;

public class BarcodeService : IBarcodeService
{
    // Rango 200-299: Prefijo estándar internacional reservado para códigos internos de comercios minoristas
    private const string PrefijoInterno = "20";

    public string GenerarEan13Interno(long correlativo)
    {
        if (correlativo < 0) correlativo = Math.Abs(correlativo);
        // 2 dígitos de prefijo (20) + 10 dígitos correlativos = 12 dígitos
        var base12 = $"{PrefijoInterno}{(correlativo % 10000000000L):D10}";
        var checksum = CalcularChecksumEan13(base12);
        return $"{base12}{checksum}";
    }

    public string GenerarEan13Aleatorio()
    {
        // 2 dígitos prefijo (20) + 6 dígitos timestamp (segundos del día / fecha) + 4 dígitos aleatorios
        var timestampPart = DateTime.UtcNow.ToString("yyMMdd");
        var randomPart = RandomNumberGenerator.GetInt32(1000, 10000); // 4 dígitos
        var base12 = $"{PrefijoInterno}{timestampPart}{randomPart}";
        var checksum = CalcularChecksumEan13(base12);
        return $"{base12}{checksum}";
    }

    public bool ValidarEan13(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 13 || !codigo.All(char.IsDigit))
            return false;

        var base12 = codigo[..12];
        var checksumEsperado = CalcularChecksumEan13(base12);
        return (codigo[12] - '0') == checksumEsperado;
    }

    public static int CalcularChecksumEan13(string doceDigitos)
    {
        if (doceDigitos.Length != 12 || !doceDigitos.All(char.IsDigit))
            throw new ArgumentException("Se requieren exactamente 12 dígitos para calcular el checksum EAN-13.", nameof(doceDigitos));

        int suma = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = doceDigitos[i] - '0';
            // Ponderación estándar EAN-13: índices pares (0, 2, 4...) peso 1; impares (1, 3, 5...) peso 3
            suma += (i % 2 == 0) ? d : d * 3;
        }

        int mod = suma % 10;
        return (10 - mod) % 10;
    }

    public string SugerirSku(string nombreArticulo, string? categoria = null, string? marca = null)
    {
        if (string.IsNullOrWhiteSpace(nombreArticulo))
            return "ART-001";

        var textoNormalizado = RemoverAcentos(nombreArticulo.Trim());
        var palabrasIgnoradas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "de", "del", "la", "las", "el", "los", "en", "con", "para", "por", "un", "una", "y", "a", "al"
        };

        var tokens = Regex.Matches(textoNormalizado, @"[a-zA-Z0-9]+")
            .Select(m => m.Value)
            .Where(w => !palabrasIgnoradas.Contains(w) && w.Length > 1)
            .ToList();

        if (tokens.Count == 0)
        {
            tokens = Regex.Matches(textoNormalizado, @"[a-zA-Z0-9]+")
                .Select(m => m.Value)
                .ToList();
        }

        var partesSku = new List<string>();

        // Si tenemos categoría clara (ej. Remeras -> REM, Calzado -> CAL, Zapatillas -> ZAP, Pantalones -> PAN)
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var catLimpia = RemoverAcentos(categoria.Trim());
            var siglaCat = ObtenerSigla(catLimpia, 3);
            if (!string.IsNullOrEmpty(siglaCat) && !tokens.Any(t => t.StartsWith(siglaCat, StringComparison.OrdinalIgnoreCase)))
            {
                partesSku.Add(siglaCat);
            }
        }

        // Tomar hasta 3 tokens representativos del nombre del producto
        foreach (var token in tokens.Take(3))
        {
            var siglaToken = ObtenerSigla(token, token.Length <= 4 ? token.Length : (token.Length <= 6 ? 4 : 4));
            partesSku.Add(siglaToken);
        }

        var skuFinal = string.Join("-", partesSku).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(skuFinal) ? "ART-001" : skuFinal;
    }

    public string SugerirSkuVariante(string skuBase, string talle, string color)
    {
        var baseLimpia = string.IsNullOrWhiteSpace(skuBase) ? "ART" : skuBase.Trim().ToUpperInvariant();
        var talleLimpio = string.IsNullOrWhiteSpace(talle) ? "U" : talle.Trim().ToUpperInvariant();
        var colorLimpio = GenerarSlugColor(color);

        return $"{baseLimpia}-{talleLimpio}-{colorLimpio}";
    }

    private static string ObtenerSigla(string palabra, int longitudMax = 4)
    {
        if (string.IsNullOrWhiteSpace(palabra)) return string.Empty;
        var limpia = Regex.Replace(palabra, @"[^a-zA-Z0-9]", "");
        return limpia.Length <= longitudMax ? limpia.ToUpperInvariant() : limpia[..longitudMax].ToUpperInvariant();
    }

    private static string GenerarSlugColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return "UNI";
        var limpio = RemoverAcentos(color.Trim().ToUpperInvariant());
        limpio = Regex.Replace(limpio, @"[^A-Z0-9]", "");
        return limpio.Length <= 4 ? limpio : limpio[..4];
    }

    private static string RemoverAcentos(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;
        var normalized = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Genera el patrón binario estándar de barras y espacios (1 = barra negra, 0 = espacio)
    /// para visualización en pantalla o impresión térmica.
    /// Soporta EAN-13 completo y Code-128 para texto libre/SKU.
    /// </summary>
    public string GenerarPatronBarras(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "";

        var limpio = codigo.Trim();
        if (limpio.Length == 13 && limpio.All(char.IsDigit) && ValidarEan13(limpio))
        {
            return GenerarPatronEan13(limpio);
        }

        return GenerarPatronCode128B(limpio);
    }

    #region Tablas EAN-13
    private static readonly string[] L_Code = {
        "0001101", "0011001", "0010011", "0111101", "0100011",
        "0110001", "0101111", "0111011", "0110111", "0001011"
    };
    private static readonly string[] G_Code = {
        "0100111", "0110011", "0011011", "0100001", "0011101",
        "0111001", "0000101", "0010001", "0001001", "0010111"
    };
    private static readonly string[] R_Code = {
        "1110010", "1100110", "1101100", "1000010", "1011100",
        "1001110", "1010000", "1000100", "1001000", "1110100"
    };

    private static readonly string[] FirstDigitStructure = {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"
    };

    private string GenerarPatronEan13(string ean13)
    {
        int primerDigito = ean13[0] - '0';
        string estructura = FirstDigitStructure[primerDigito];
        var sb = new StringBuilder();

        // Quiet zone izquierda + Marcador de inicio (101)
        sb.Append("0000000101");

        // 6 dígitos izquierdos
        for (int i = 1; i <= 6; i++)
        {
            int d = ean13[i] - '0';
            char modo = estructura[i - 1];
            sb.Append(modo == 'L' ? L_Code[d] : G_Code[d]);
        }

        // Marcador central (01010)
        sb.Append("01010");

        // 6 dígitos derechos (R-code)
        for (int i = 7; i <= 12; i++)
        {
            int d = ean13[i] - '0';
            sb.Append(R_Code[d]);
        }

        // Marcador final (101) + Quiet zone derecha
        sb.Append("1010000000");
        return sb.ToString();
    }
    #endregion

    #region Code 128B
    private static readonly string[] Code128Patterns = {
        "11011001100","11001101100","11001100110","10010011000","10010001100","10001001100",
        "10011001000","10011000100","10001100100","11001001000","11001000100","11000100100",
        "10110011100","10011011100","10011001110","10111001100","10011101100","10011100110",
        "11001110010","11001011100","11001001110","11011100100","11001110100","11101101110",
        "11101001100","11100101100","11100100110","11101100100","11100110100","11100110010",
        "11011011000","11011000110","11000110110","10100011000","10001011000","10001000110",
        "10110001000","10001101000","10001100010","11010001000","11000101000","11000100010",
        "10110111000","10110001110","10001101110","10111011000","10111000110","10001110110",
        "11101110110","11010001110","11000101110","11011101000","11011100010","11011101110",
        "11101011000","11101000110","11100010110","11101101000","11101100010","11100011010",
        "11101111010","11001000010","11110001010","10100110000","10100001100","10010110000",
        "10010000110","10000101100","10000100110","10110010000","10110000100","10011010000",
        "10011000010","10000110100","10000110010","11000010010","11001010000","11110111010",
        "11000010100","10001111010","10100111100","10010111100","10010011110","10111100100",
        "10011110100","10011110010","11110100100","11110010100","11110010010","11011011110",
        "11011110110","11110110110","10101111000","10100011110","10001011110","10111101000",
        "10111100010","11110101000","11110100010","10111011110","10111101110","11101011110",
        "11110101110","11010000100","11010010000","11010011100","11000111010"
    };
    private const string Code128Stop = "1100011101011";
    private const int Code128StartB = 104;

    private string GenerarPatronCode128B(string texto)
    {
        var sb = new StringBuilder();
        sb.Append("0000000000"); // Quiet zone

        int checksum = Code128StartB;
        sb.Append(Code128Patterns[Code128StartB]);

        int peso = 1;
        foreach (char c in texto)
        {
            int val = c - 32;
            if (val < 0 || val > 105) val = 0;
            checksum += val * peso;
            sb.Append(Code128Patterns[val]);
            peso++;
        }

        int checksumSymbol = checksum % 103;
        sb.Append(Code128Patterns[checksumSymbol]);
        sb.Append(Code128Stop);
        sb.Append("0000000000"); // Quiet zone
        return sb.ToString();
    }
    #endregion
}
