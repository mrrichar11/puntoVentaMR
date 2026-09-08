using System.Windows.Controls;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Analiticas;

public partial class AnaliticasView : UserControl
{
    public AnaliticasView(AnaliticasViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (s, e) =>
        {
            await viewModel.CargarReporteAsync();
        };
    }
}
