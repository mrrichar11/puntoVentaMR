using System.Windows;
using PuntoDeVenta.UI.ViewModels;
using PuntoDeVenta.UI.Views.Analiticas;
using PuntoDeVenta.UI.Views.Asistente;
using PuntoDeVenta.UI.Views.Caja;
using PuntoDeVenta.UI.Views.Clientes;
using PuntoDeVenta.UI.Views.Configuracion;
using PuntoDeVenta.UI.Views.Inventario;
using PuntoDeVenta.UI.Views.Pos;
using PuntoDeVenta.UI.Views.Proveedores;
using Wpf.Ui.Controls;

namespace PuntoDeVenta.UI;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _mainViewModel;
    private readonly PosView _posView;
    private readonly CajaView _cajaView;
    private readonly InventarioView _inventarioView;
    private readonly AsistenteCargaView _asistenteView;
    private readonly ClientesView _clientesView;
    private readonly ProveedoresView _proveedoresView;
    private readonly AnaliticasView _analiticasView;
    private readonly ConfiguracionView _configuracionView;

    public MainWindow(
        MainViewModel mainViewModel,
        PosView posView,
        CajaView cajaView,
        InventarioView inventarioView,
        AsistenteCargaView asistenteView,
        ClientesView clientesView,
        ProveedoresView proveedoresView,
        AnaliticasView analiticasView,
        ConfiguracionView configuracionView)
    {
        InitializeComponent();

        _mainViewModel = mainViewModel;
        _posView = posView;
        _cajaView = cajaView;
        _inventarioView = inventarioView;
        _asistenteView = asistenteView;
        _clientesView = clientesView;
        _proveedoresView = proveedoresView;
        _analiticasView = analiticasView;
        _configuracionView = configuracionView;

        DataContext = _mainViewModel;

        _posView.ViewModel.OnCajaModificada += async () =>
        {
            await _mainViewModel.ActualizarEstadoCajaAsync();
        };

        _cajaView.ViewModel.OnCajaModificada += async () =>
        {
            await _mainViewModel.ActualizarEstadoCajaAsync();
            await _posView.ViewModel.VerificarCajaAsync();
        };

        Loaded += async (s, e) =>
        {
            await _mainViewModel.ActualizarInformacionAsync();
            await _posView.ViewModel.VerificarCajaAsync();
            // Iniciar por defecto en la vista de Punto de Venta (TPV)
            MainContentControl.Content = _posView;
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.F1)
            {
                _mainViewModel.ToggleAsistenteFlotanteCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    private async void NavItemPos_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _posView;
        await _posView.ViewModel.VerificarCajaAsync();
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }

    private async void NavItemCaja_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _cajaView;
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }

    private async void NavItemInventario_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _inventarioView;
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }

    private async void NavItemAsistente_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MainContentControl.Content = _asistenteView;
            await _mainViewModel.ActualizarEstadoCajaAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al cargar el Asistente: {ex.Message}\n\n{ex.InnerException?.Message}", "MR SYS Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async void NavItemClientes_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _clientesView;
        if (_clientesView.DataContext is ClientesViewModel vm)
        {
            await vm.CargarDatosAsync();
        }
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }

    private async void NavItemProveedores_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _proveedoresView;
        if (_proveedoresView.DataContext is ProveedoresViewModel vm)
        {
            await vm.CargarDatosAsync();
        }
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }

    private async void NavItemAnaliticas_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _analiticasView;
        if (_analiticasView.DataContext is AnaliticasViewModel vm)
        {
            await vm.CargarReporteAsync();
        }
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }

    private async void NavItemConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        MainContentControl.Content = _configuracionView;
        if (_configuracionView.DataContext is ConfiguracionViewModel vm)
        {
            await vm.CargarDatosAsync();
        }
        await _mainViewModel.ActualizarEstadoCajaAsync();
    }
}