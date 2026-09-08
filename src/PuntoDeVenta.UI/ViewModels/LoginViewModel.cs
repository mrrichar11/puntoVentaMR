using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.DTOs.Seguridad;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILicenseService _licenseService;
    private readonly IConfiguracionService _configuracionService;

    [ObservableProperty]
    private string? _logoRuta;

    [ObservableProperty]
    private bool _tieneLogoPersonalizado;

    [ObservableProperty]
    private string _nombreComercio = string.Empty;

    public string NombreComercioOIdentidad => !string.IsNullOrWhiteSpace(NombreComercio) ? NombreComercio : "MR SYS Retail";

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    [ObservableProperty]
    private string _licenciaTexto = string.Empty;

    [ObservableProperty]
    private bool _estaPorVencer;

    [ObservableProperty]
    private bool _fueAutenticado;

    public UsuarioDto? UsuarioAutenticado { get; private set; }

    public event Action? RequestClose;
    public event Action? RequestAbrirActivacion;

    public LoginViewModel(
        IAuthService authService,
        ILicenseService licenseService,
        IConfiguracionService configuracionService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
    }

    public async Task InicializarAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            NombreComercio = config.NombreComercio;
            LogoRuta = config.LogoRuta;
            TieneLogoPersonalizado = !string.IsNullOrWhiteSpace(config.LogoRuta) && System.IO.File.Exists(config.LogoRuta);
            OnPropertyChanged(nameof(NombreComercioOIdentidad));
        }
        catch
        {
            // Fallback silencioso a MR SYS Retail
        }

        try
        {
            var estado = await _licenseService.ValidarLicenciaAsync();
            LicenciaTexto = estado.MensajeEstado;
            EstaPorVencer = estado.EstaPorVencer;
        }
        catch (Exception ex)
        {
            LicenciaTexto = $"Estado licencia: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task IngresarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                Mensaje = "Por favor, ingresa tu usuario.";
                EsMensajeError = true;
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                Mensaje = "Por favor, ingresa tu contraseña.";
                EsMensajeError = true;
                return;
            }

            var request = new LoginRequestDto
            {
                Username = Username.Trim(),
                Password = Password
            };

            var usuario = await _authService.AutenticarAsync(request);
            if (usuario == null)
            {
                Mensaje = "Usuario o contraseña incorrectos.";
                EsMensajeError = true;
                return;
            }

            UsuarioAutenticado = usuario;
            FueAutenticado = true;
            EsMensajeError = false;
            Mensaje = $"¡Bienvenido/a {usuario.NombreCompleto}!";

            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al iniciar sesión: {ex.Message}";
            EsMensajeError = true;
        }
    }

    [RelayCommand]
    public void AbrirActivacion()
    {
        RequestAbrirActivacion?.Invoke();
    }

    [RelayCommand]
    public void Salir()
    {
        RequestClose?.Invoke();
    }
}
