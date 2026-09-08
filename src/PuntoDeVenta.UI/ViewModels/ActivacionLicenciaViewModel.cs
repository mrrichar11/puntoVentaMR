using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.UI.ViewModels;

public partial class ActivacionLicenciaViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;

    [ObservableProperty]
    private string _codigoInstalacion = string.Empty;

    [ObservableProperty]
    private string _claveActivacionInput = string.Empty;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    [ObservableProperty]
    private bool _esActivado;

    public event Action? RequestClose;

    public ActivacionLicenciaViewModel(ILicenseService licenseService)
    {
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        CodigoInstalacion = _licenseService.ObtenerCodigoInstalacion();
    }

    public async Task InicializarAsync()
    {
        var estado = await _licenseService.ValidarLicenciaAsync();
        CodigoInstalacion = estado.CodigoInstalacion;
        Mensaje = estado.MensajeEstado;
        EsMensajeError = !estado.EsValida;
    }

    [RelayCommand]
    public void CopiarCodigo()
    {
        try
        {
            Clipboard.SetText(CodigoInstalacion);
            Mensaje = "¡Código de Instalación copiado al portapapeles! Envíalo a tu proveedor para obtener tu clave mensual.";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al copiar código: {ex.Message}";
            EsMensajeError = true;
        }
    }

    [RelayCommand]
    public async Task ActivarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ClaveActivacionInput))
            {
                Mensaje = "Por favor, ingresa o pega la clave de activación.";
                EsMensajeError = true;
                return;
            }

            var resultado = await _licenseService.ActivarLicenciaAsync(ClaveActivacionInput);
            if (resultado.Exitoso)
            {
                EsActivado = true;
                EsMensajeError = false;
                Mensaje = resultado.Mensaje;

                await Task.Delay(1000);
                RequestClose?.Invoke();
            }
            else
            {
                EsActivado = false;
                EsMensajeError = true;
                Mensaje = resultado.Mensaje;
            }
        }
        catch (Exception ex)
        {
            EsActivado = false;
            EsMensajeError = true;
            Mensaje = $"Error al procesar la clave: {ex.Message}";
        }
    }

    [RelayCommand]
    public void Cerrar()
    {
        RequestClose?.Invoke();
    }
}
