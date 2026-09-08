using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;
using PuntoDeVenta.UI.ViewModels;

namespace PuntoDeVenta.UI.Views.Asistente;

public partial class AsistenteCargaView : UserControl
{
    public AsistenteCargaViewModel ViewModel { get; }

    public AsistenteCargaView(AsistenteCargaViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        // Auto-scroll hacia abajo cada vez que se agregue un nuevo mensaje
        if (ViewModel.Mensajes is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ChatScrollViewer?.ScrollToBottom();
                    });
                }
            };
        }

        Loaded += (s, e) => InputTextBox?.Focus();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            if (ViewModel.EnviarMensajeCommand.CanExecute(null))
            {
                ViewModel.EnviarMensajeCommand.Execute(null);
            }
        }
    }

    private void BtnNuevoDialogo_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ViewModel.InicializarChat();
        InputTextBox?.Focus();
    }
}
