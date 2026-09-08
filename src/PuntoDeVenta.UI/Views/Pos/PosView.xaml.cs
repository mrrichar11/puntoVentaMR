using System.Windows.Controls;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Pos;

public partial class PosView : UserControl
{
    public PosViewModel ViewModel { get; }

    public PosView(PosViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.VerificarCajaAsync();
            await ViewModel.CargarClientesAsync();
        };
    }
}
