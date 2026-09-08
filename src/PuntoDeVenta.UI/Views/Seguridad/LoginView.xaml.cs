using System.Windows;
using PuntoDeVenta.UI.ViewModels;
using Wpf.Ui.Controls;

namespace PuntoDeVenta.UI.Views.Seguridad;

public partial class LoginView : FluentWindow
{
    private readonly LoginViewModel _viewModel;
    private readonly Func<ActivacionLicenciaView> _activacionViewFactory;

    public LoginView(LoginViewModel viewModel, Func<ActivacionLicenciaView> activacionViewFactory)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _activacionViewFactory = activacionViewFactory ?? throw new ArgumentNullException(nameof(activacionViewFactory));

        DataContext = _viewModel;

        _viewModel.RequestClose += () =>
        {
            try
            {
                DialogResult = _viewModel.FueAutenticado;
            }
            catch
            {
                Close();
            }
        };

        _viewModel.RequestAbrirActivacion += () =>
        {
            var activacionWindow = _activacionViewFactory();
            activacionWindow.Owner = this;
            activacionWindow.ShowDialog();
            _ = _viewModel.InicializarAsync();
        };

        Loaded += async (_, _) =>
        {
            await _viewModel.InicializarAsync();
        };
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = TxtPassword.Password;
    }
}
