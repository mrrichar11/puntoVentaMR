using System.Windows.Controls;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Configuracion;

public partial class ConfiguracionView : UserControl
{
    private readonly ConfiguracionViewModel _viewModel;

    public ConfiguracionView(ConfiguracionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        Loaded += async (s, e) =>
        {
            await _viewModel.CargarDatosAsync();
        };
    }

    private void TxtPassActual_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.PasswordActual = TxtPassActual.Password;
    }

    private void TxtPassNuevo_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.PasswordNuevo = TxtPassNuevo.Password;
    }

    private void TxtPassConfirm_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.PasswordConfirmacion = TxtPassConfirm.Password;
    }

    private void TxtNuevoUserPass_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.NuevoPassword = TxtNuevoUserPass.Password;
    }
}
