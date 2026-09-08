using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.UI.Helpers;

namespace PuntoDeVenta.UI.Views.Inventario;

public partial class ImprimirEtiquetasDialog : Window
{
    private readonly VarianteArticuloDto _variante;
    private readonly string _nombreComercio;
    private readonly decimal _descuentoEfectivoPorcentaje;
    private readonly IBarcodeService _barcodeService;

    public ImprimirEtiquetasDialog(
        VarianteArticuloDto variante,
        string nombreComercio,
        decimal descuentoEfectivoPorcentaje,
        IBarcodeService barcodeService)
    {
        InitializeComponent();

        _variante = variante ?? throw new ArgumentNullException(nameof(variante));
        _nombreComercio = string.IsNullOrWhiteSpace(nombreComercio) ? "MR. SYS" : nombreComercio.Trim();
        _descuentoEfectivoPorcentaje = descuentoEfectivoPorcentaje;
        _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));

        CargarImpresoras();
        ActualizarVistaPrevia();
    }

    private void CargarImpresoras()
    {
        CmbImpresoras.Items.Clear();
        string defaultPrinter = "";

        try
        {
            var defaultQueue = LocalPrintServer.GetDefaultPrintQueue();
            defaultPrinter = defaultQueue?.Name ?? "";

            using var server = new LocalPrintServer();
            var printQueues = server.GetPrintQueues();
            foreach (var pq in printQueues)
            {
                CmbImpresoras.Items.Add(pq.Name);
            }
        }
        catch
        {
            // Fallback
        }

        if (CmbImpresoras.Items.Count == 0)
        {
            CmbImpresoras.Items.Add("Impresora predeterminada de Windows");
            CmbImpresoras.SelectedIndex = 0;
        }
        else
        {
            int defaultIndex = CmbImpresoras.Items.IndexOf(defaultPrinter);
            CmbImpresoras.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
        }
    }

    private void ActualizarVistaPrevia()
    {
        TxtEtiquetaComercio.Text = _nombreComercio.ToUpperInvariant();
        TxtEtiquetaProducto.Text = _variante.NombreArticulo;
        TxtEtiquetaVariante.Text = $"TALLE: {_variante.Talle}  |  COLOR: {_variante.Color}";
        TxtEtiquetaPrecioLista.Text = $"${_variante.PrecioLista:N0}";

        if (ChkMostrarPrecioEfectivo.IsChecked == true && _descuentoEfectivoPorcentaje > 0)
        {
            decimal precioEfvo = Math.Round(_variante.PrecioLista * (1m - (_descuentoEfectivoPorcentaje / 100m)), 0);
            TxtEtiquetaPrecioEfectivo.Text = $" (Efvo: ${precioEfvo:N0})";
            TxtEtiquetaPrecioEfectivo.Visibility = Visibility.Visible;
        }
        else
        {
            TxtEtiquetaPrecioEfectivo.Visibility = Visibility.Collapsed;
        }

        var codigo = !string.IsNullOrWhiteSpace(_variante.CodigoBarras)
            ? _variante.CodigoBarras.Trim()
            : (!string.IsNullOrWhiteSpace(_variante.SKU) ? _variante.SKU.Trim() : "200000000001");

        TxtEtiquetaCodigoNumerico.Text = codigo;

        var patron = _barcodeService.GenerarPatronBarras(codigo);
        var imagen = BarcodeImageHelper.GenerarImagenCodigoBarras(patron, altura: 50, anchoModulo: 2);
        ImgCodigoBarras.Source = imagen;
    }

    private void BtnMenosCopias_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtCantidad.Text, out int cant) && cant > 1)
        {
            TxtCantidad.Text = (cant - 1).ToString();
        }
    }

    private void BtnMasCopias_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtCantidad.Text, out int cant))
        {
            TxtCantidad.Text = (cant + 1).ToString();
        }
        else
        {
            TxtCantidad.Text = "1";
        }
    }

    private void TxtCantidad_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Validar que sea número
    }

    private void CmbImpresoras_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void CmbTamano_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BorderEtiquetaVisual == null) return;

        switch (CmbTamano.SelectedIndex)
        {
            case 0: // 50x30 mm
                BorderEtiquetaVisual.Width = 260;
                BorderEtiquetaVisual.MinHeight = 160;
                break;
            case 1: // 40x25 mm
                BorderEtiquetaVisual.Width = 220;
                BorderEtiquetaVisual.MinHeight = 140;
                break;
            case 2: // Rollo 58 mm
                BorderEtiquetaVisual.Width = 240;
                BorderEtiquetaVisual.MinHeight = 150;
                break;
        }
    }

    private void ChkMostrarPrecio_Changed(object sender, RoutedEventArgs e)
    {
        if (TxtEtiquetaPrecioEfectivo != null)
        {
            ActualizarVistaPrevia();
        }
    }

    private void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtCantidad.Text, out int cantidad) || cantidad <= 0)
        {
            MessageBox.Show("Ingrese una cantidad de copias válida (mayor a 0).", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            BtnImprimir.IsEnabled = false;
            TxtEstadoImpresion.Text = "Enviando a la impresora...";

            var printDialog = new PrintDialog();
            string? printerName = CmbImpresoras.SelectedItem?.ToString();

            if (!string.IsNullOrWhiteSpace(printerName) && printerName != "Impresora predeterminada de Windows")
            {
                var server = new LocalPrintServer();
                var queue = server.GetPrintQueues().FirstOrDefault(q => q.Name == printerName);
                if (queue != null)
                {
                    printDialog.PrintQueue = queue;
                }
            }

            for (int i = 0; i < cantidad; i++)
            {
                printDialog.PrintVisual(BorderEtiquetaVisual, $"Etiqueta - {_variante.SKU} ({i + 1}/{cantidad})");
            }

            TxtEstadoImpresion.Text = $"¡{cantidad} etiqueta(s) enviada(s) con éxito!";
            MessageBox.Show($"Se enviaron {cantidad} etiqueta(s) a la impresora.", "Impresión Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            TxtEstadoImpresion.Text = "Error al imprimir.";
            MessageBox.Show($"No se pudo completar la impresión: {ex.Message}\n\nAsegúrese de que la impresora esté conectada y encendida.", "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnImprimir.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
