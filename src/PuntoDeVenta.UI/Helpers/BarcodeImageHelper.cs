using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PuntoDeVenta.UI.Helpers;

public static class BarcodeImageHelper
{
    /// <summary>
    /// Genera una imagen de código de barras nítida a nivel de píxeles a partir de un patrón binario ('1' = barra, '0' = espacio).
    /// </summary>
    public static BitmapSource? GenerarImagenCodigoBarras(string patron, int altura = 64, int anchoModulo = 2)
    {
        if (string.IsNullOrWhiteSpace(patron)) return null;

        int ancho = patron.Length * anchoModulo;
        var wb = new WriteableBitmap(ancho, altura, 96, 96, PixelFormats.Bgr32, null);
        int[] pixels = new int[ancho * altura];
        int negro = unchecked((int)0xFF000000);
        int blanco = unchecked((int)0xFFFFFFFF);

        for (int x = 0; x < ancho; x++)
        {
            int charIdx = x / anchoModulo;
            int color = (charIdx < patron.Length && patron[charIdx] == '1') ? negro : blanco;
            for (int y = 0; y < altura; y++)
            {
                pixels[y * ancho + x] = color;
            }
        }

        wb.WritePixels(new Int32Rect(0, 0, ancho, altura), pixels, ancho * 4, 0);
        wb.Freeze();
        return wb;
    }
}
