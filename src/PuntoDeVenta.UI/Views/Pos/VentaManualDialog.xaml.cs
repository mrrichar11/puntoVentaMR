using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PuntoDeVenta.UI.Views.Pos;

public partial class VentaManualDialog : Window
{
    private readonly decimal _margenSugeridoPorcentaje;
    private decimal _precioSugeridoCalculado;

    public string DescripcionArticulo { get; private set; } = string.Empty;
    public int CantidadArticulo { get; private set; } = 1;
    public decimal PrecioCostoArticulo { get; private set; }
    public decimal PrecioVentaArticulo { get; private set; }
    public string TalleArticulo { get; private set; } = "-";
    public string ColorArticulo { get; private set; } = "-";

    public VentaManualDialog(decimal margenSugeridoPorcentaje = 100m, string? textoInicial = null)
    {
        InitializeComponent();
        _margenSugeridoPorcentaje = margenSugeridoPorcentaje > 0 ? margenSugeridoPorcentaje : 100m;

        if (!string.IsNullOrWhiteSpace(textoInicial))
        {
            TxtDescripcion.Text = textoInicial.Trim();
        }

        Loaded += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(TxtDescripcion.Text))
            {
                TxtDescripcion.Focus();
            }
            else
            {
                TxtPrecioVenta.Focus();
            }
        };
    }

    private void TxtPrecioCosto_TextChanged(object sender, TextChangedEventArgs e)
    {
        var texto = TxtPrecioCosto.Text?.Trim().Replace("$", "").Replace(" ", "");
        if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.CurrentCulture, out var costo) && costo > 0)
        {
            _precioSugeridoCalculado = Math.Round(costo * (1m + (_margenSugeridoPorcentaje / 100m)), 0);
            TxtTextoPrecioSugerido.Text = $"Sugerido con margen (+{_margenSugeridoPorcentaje:0.#}%): ${_precioSugeridoCalculado:N0}";
            PnlPrecioSugerido.Visibility = Visibility.Visible;
        }
        else if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var costoInv) && costoInv > 0)
        {
            _precioSugeridoCalculado = Math.Round(costoInv * (1m + (_margenSugeridoPorcentaje / 100m)), 0);
            TxtTextoPrecioSugerido.Text = $"Sugerido con margen (+{_margenSugeridoPorcentaje:0.#}%): ${_precioSugeridoCalculado:N0}";
            PnlPrecioSugerido.Visibility = Visibility.Visible;
        }
        else
        {
            PnlPrecioSugerido.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnUsarSugerido_Click(object sender, RoutedEventArgs e)
    {
        if (_precioSugeridoCalculado > 0)
        {
            TxtPrecioVenta.Text = _precioSugeridoCalculado.ToString("0");
        }
    }

    private void BtnAgregar_Click(object sender, RoutedEventArgs e)
    {
        var descripcion = TxtDescripcion.Text?.Trim();
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            MessageBox.Show("Por favor, ingrese la descripción o nombre del producto.", "Campo Obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtDescripcion.Focus();
            return;
        }

        if (!int.TryParse(TxtCantidad.Text?.Trim(), out var cantidad) || cantidad <= 0)
        {
            MessageBox.Show("La cantidad debe ser un número entero mayor a cero.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtCantidad.Focus();
            return;
        }

        var textoVenta = TxtPrecioVenta.Text?.Trim().Replace("$", "").Replace(" ", "");
        if (!decimal.TryParse(textoVenta, NumberStyles.Any, CultureInfo.CurrentCulture, out var precioVenta) &&
            !decimal.TryParse(textoVenta, NumberStyles.Any, CultureInfo.InvariantCulture, out precioVenta) || precioVenta <= 0)
        {
            MessageBox.Show("Por favor, ingrese un precio final de venta válido (mayor a 0).", "Precio Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPrecioVenta.Focus();
            return;
        }

        decimal costo = 0m;
        var textoCosto = TxtPrecioCosto.Text?.Trim().Replace("$", "").Replace(" ", "");
        if (!string.IsNullOrWhiteSpace(textoCosto))
        {
            if (!decimal.TryParse(textoCosto, NumberStyles.Any, CultureInfo.CurrentCulture, out costo) &&
                !decimal.TryParse(textoCosto, NumberStyles.Any, CultureInfo.InvariantCulture, out costo))
            {
                MessageBox.Show("El precio de costo ingresado no es válido.", "Costo Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrecioCosto.Focus();
                return;
            }
        }

        DescripcionArticulo = descripcion;
        CantidadArticulo = cantidad;
        PrecioCostoArticulo = costo;
        PrecioVentaArticulo = precioVenta;
        TalleArticulo = !string.IsNullOrWhiteSpace(TxtTalle.Text) ? TxtTalle.Text.Trim() : "-";
        ColorArticulo = !string.IsNullOrWhiteSpace(TxtColor.Text) ? TxtColor.Text.Trim() : "-";

        DialogResult = true;
        Close();
    }
}
