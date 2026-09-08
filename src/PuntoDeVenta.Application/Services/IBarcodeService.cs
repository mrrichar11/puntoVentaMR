namespace PuntoDeVenta.Application.Services;

public interface IBarcodeService
{
    /// <summary>
    /// Genera un código de barras EAN-13 estándar para uso interno (prefijo 20 + 10 dígitos + dígito verificador).
    /// </summary>
    string GenerarEan13Interno(long correlativo);

    /// <summary>
    /// Genera un código de barras EAN-13 aleatorio único con prefijo 20 y checksum válido.
    /// </summary>
    string GenerarEan13Aleatorio();

    /// <summary>
    /// Valida si un código de barras EAN-13 cumple con la estructura y dígito de control módulo 10.
    /// </summary>
    bool ValidarEan13(string codigo);

    /// <summary>
    /// Sugiere un SKU / Código de estilo conciso y ordenado a partir del nombre, categoría y marca del producto.
    /// </summary>
    string SugerirSku(string nombreArticulo, string? categoria = null, string? marca = null);

    /// <summary>
    /// Genera el SKU completo para una variante combinando el código de estilo base con talle y color.
    /// </summary>
    string SugerirSkuVariante(string skuBase, string talle, string color);

    /// <summary>
    /// Genera los módulos binarios (barras y espacios) para renderizar un código EAN-13 o Code 128.
    /// Retorna una cadena de '1' (barra negra) y '0' (espacio blanco).
    /// </summary>
    string GenerarPatronBarras(string codigo);
}
