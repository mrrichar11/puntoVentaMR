using PuntoDeVenta.UI.ViewModels;
using Wpf.Ui.Controls;

namespace PuntoDeVenta.UI.Views.Seguridad;

public partial class ActivacionLicenciaView : FluentWindow
{
    private readonly ActivacionLicenciaViewModel _viewModel;

    public ActivacionLicenciaView(ActivacionLicenciaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        _viewModel.RequestClose += () =>
        {
            try
            {
                DialogResult = _viewModel.EsActivado;
            }
            catch
            {
                Close();
            }
        };

        Loaded += async (_, _) =>
        {
            await _viewModel.InicializarAsync();
        };
    }
}
