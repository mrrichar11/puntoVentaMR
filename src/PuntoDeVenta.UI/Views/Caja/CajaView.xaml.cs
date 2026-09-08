using System.Windows.Controls;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Caja;

public partial class CajaView : UserControl
{
    public CajaViewModel ViewModel { get; }

    public CajaView(CajaViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        Loaded += async (s, e) => await ViewModel.CargarEstadoAsync();
    }
}
