using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using WpfUiControls = Wpf.Ui.Controls;
using WpfButton = System.Windows.Controls.Button;

namespace GeneradorLicencias;

public partial class MainWindow : WpfUiControls.FluentWindow
{
    private static readonly byte[] MasterSecretKey = Encoding.UTF8.GetBytes("PuntoDeVenta-Retail-Master-Secret-Licensing-Key-2026-Fliac#Secured!");

    public MainWindow()
    {
        InitializeComponent();
        DpFechaExpiracion.SelectedDate = DateTime.Today.AddDays(30);
    }

    private void BtnSumarDias_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && int.TryParse(btn.Tag?.ToString(), out var dias))
        {
            DpFechaExpiracion.SelectedDate = DateTime.Today.AddDays(dias);
        }
        else if (sender is WpfUiControls.Button uiBtn && int.TryParse(uiBtn.Tag?.ToString(), out var diasUi))
        {
            DpFechaExpiracion.SelectedDate = DateTime.Today.AddDays(diasUi);
        }
    }

    private void DpFechaExpiracion_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ActualizarClave();
    }

    private void RbPlan_Checked(object sender, RoutedEventArgs e)
    {
        ActualizarClave();
    }

    private void BtnGenerar_Click(object sender, RoutedEventArgs e)
    {
        ActualizarClave(mostrarAlerta: true);
    }

    private void ActualizarClave(bool mostrarAlerta = false)
    {
        var codigo = TxtCodigoInstalacion?.Text?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            if (mostrarAlerta)
            {
                System.Windows.MessageBox.Show(
                    "Por favor, ingresa el Código de Instalación del cliente (ej. POS-DESKTOP-XXXXXXXX).",
                    "Dato requerido",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            return;
        }

        var fecha = DpFechaExpiracion?.SelectedDate ?? DateTime.Today.AddDays(30);
        var fechaFinalUtc = new DateTime(fecha.Year, fecha.Month, fecha.Day, 23, 59, 59, DateTimeKind.Utc);

        bool esPremium = RbPlanPremium?.IsChecked ?? true;
        var planNombre = esPremium ? "Plan Premium (con Asistente IA Gemini)" : "Plan Estándar";

        var clave = GenerarClaveActivacion(codigo, fechaFinalUtc, esPremium);
        if (TxtClaveResultado != null)
        {
            TxtClaveResultado.Text = clave;
        }

        if (TxtMensajeWhatsApp != null)
        {
            TxtMensajeWhatsApp.Text =
                $"¡Hola! Tu clave de activación para el Punto de Venta [{planNombre}] es:\n{clave}\n" +
                $"Válida hasta el {fecha:dd/MM/yyyy}. Ingresala en la sección de Activación / Configuración del sistema.";
        }

        if (mostrarAlerta)
        {
            System.Windows.MessageBox.Show(
                $"¡Clave generada con éxito!\n\nPlan: {planNombre}\nClave: {clave}\nVálida hasta: {fecha:dd/MM/yyyy}",
                "Clave Generada",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private string GenerarClaveActivacion(string codigoInstalacion, DateTime fechaExpiracion, bool esPremium)
    {
        var codigoLimpio = codigoInstalacion.Trim().ToUpperInvariant();
        var fechaStr = fechaExpiracion.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var prefijo = esPremium ? "PRM" : "STD";

        var payload = $"{prefijo}:{codigoLimpio}:{fechaStr}";
        using var hmac = new HMACSHA256(MasterSecretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        var firmaCorta = Convert.ToHexString(hash)[..8].ToUpperInvariant();
        return $"{prefijo}-{fechaStr}-{firmaCorta}";
    }

    private void BtnCopiarClave_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtClaveResultado.Text))
        {
            Clipboard.SetText(TxtClaveResultado.Text);
            System.Windows.MessageBox.Show("¡Clave copiada al portapapeles!", "Copiado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    private void BtnCopiarWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtMensajeWhatsApp.Text))
        {
            Clipboard.SetText(TxtMensajeWhatsApp.Text);
            System.Windows.MessageBox.Show("¡Mensaje completo copiado! Puedes pegarlo directamente en WhatsApp Web o Chat.", "Copiado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
