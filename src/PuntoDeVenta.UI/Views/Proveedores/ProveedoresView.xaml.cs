using System.Windows.Controls;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Proveedores;

public partial class ProveedoresView : UserControl
{
    private readonly ProveedoresViewModel _viewModel;

    public ProveedoresView(ProveedoresViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (s, e) => await _viewModel.CargarDatosAsync();
    }
}
