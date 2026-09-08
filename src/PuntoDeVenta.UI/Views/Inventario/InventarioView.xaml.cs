using System.Windows.Controls;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Inventario;

public partial class InventarioView : UserControl
{
    private readonly InventarioViewModel _viewModel;

    public InventarioView(InventarioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (s, e) => await _viewModel.CargarDatosAsync();
    }
}
