using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Asistente;

public partial class AsistenteFlotanteView : UserControl
{
    public AsistenteFlotanteView()
    {
        InitializeComponent();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is AsistenteCargaViewModel vm)
        {
            if (vm.EnviarMensajeCommand.CanExecute(null))
            {
                vm.EnviarMensajeCommand.Execute(null);
                e.Handled = true;
                ChatScrollViewer.ScrollToEnd();
            }
        }
    }

    private void BtnNuevoDialogo_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AsistenteCargaViewModel vm)
        {
            vm.LimpiarChatCommand.Execute(null);
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        // Encontrar ventana principal o DataContext padre si es necesario
        var window = Window.GetWindow(this);
        if (window?.DataContext is MainViewModel mainVm)
        {
            mainVm.CerrarAsistenteFlotanteCommand.Execute(null);
        }
    }
}
