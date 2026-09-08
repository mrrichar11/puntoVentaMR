using System.Globalization;
using System.Windows.Data;

namespace PuntoDeVenta.UI.Converters;

public class CurrencyConverter : IValueConverter
{
    private static readonly CultureInfo Culture = new("es-AR");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            return d.ToString("C2", Culture);
        }

        if (value is double dbl)
        {
            return dbl.ToString("C2", Culture);
        }

        return "$ 0,00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
