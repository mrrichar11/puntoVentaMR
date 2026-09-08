using System.Windows.Controls;

namespace PuntoDeVenta.UI.Views.Clientes;

public partial class ClientesView : UserControl
{
    private readonly PuntoDeVenta.UI.ViewModels.ClientesViewModel _viewModel;

    public ClientesView(PuntoDeVenta.UI.ViewModels.ClientesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        Loaded += async (s, e) =>
        {
            await _viewModel.CargarDatosAsync();
        };
    }
}
