namespace PuntoDeVenta.Application.DTOs.Inventario;

/// <summary>
/// Plantillas predefinidas de curvas de talles para facilitar la carga rápida en mostrador/depósito,
/// cubriendo indumentaria por letras, numeración infantil, pantalones/denim y calzado.
/// </summary>
public static class CurvasTallesPresets
{
    /// <summary>
    /// Indumentaria Adultos Letras: XS, S, M, L, XL, XXL, XXXL
    /// </summary>
    public static readonly IReadOnlyList<string> IndumentariaLetras = new[]
    {
        "XS", "S", "M", "L", "XL", "XXL", "XXXL"
    };

    /// <summary>
    /// Indumentaria Adultos Números: 1, 2, 3, 4, 5, 6
    /// </summary>
    public static readonly IReadOnlyList<string> AdultosNumeros = new[]
    {
        "1", "2", "3", "4", "5", "6"
    };

    /// <summary>
    /// Indumentaria Letras Básica: S, M, L, XL
    /// </summary>
    public static readonly IReadOnlyList<string> IndumentariaLetrasBasica = new[]
    {
        "S", "M", "L", "XL"
    };

    /// <summary>
    /// Indumentaria Infantil / Niños: 2, 4, 6, 8, 10, 12, 14, 16
    /// </summary>
    public static readonly IReadOnlyList<string> InfantilNumerica = new[]
    {
        "2", "4", "6", "8", "10", "12", "14", "16"
    };

    /// <summary>
    /// Pantalones / Jeans / Denim: 36, 38, 40, 42, 44, 46, 48, 50, 52
    /// </summary>
    public static readonly IReadOnlyList<string> PantalonesDenim = new[]
    {
        "36", "38", "40", "42", "44", "46", "48", "50", "52"
    };

    /// <summary>
    /// Calzado Adultos Estándar: 36 al 45
    /// </summary>
    public static readonly IReadOnlyList<string> CalzadoAdultos = new[]
    {
        "36", "37", "38", "39", "40", "41", "42", "43", "44", "45"
    };

    /// <summary>
    /// Calzado Niños: 24 al 35
    /// </summary>
    public static readonly IReadOnlyList<string> CalzadoNinos = new[]
    {
        "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35"
    };

    /// <summary>
    /// Talle Único para accesorios (gorros, medias, cinturones, bolsos)
    /// </summary>
    public static readonly IReadOnlyList<string> TalleUnico = new[]
    {
        "U"
    };
}
