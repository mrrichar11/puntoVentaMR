using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICajaService _cajaService;
    private readonly IAuthService _authService;
    private readonly ILicenseService _licenseService;
    private readonly IConfiguracionService _configuracionService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private string _titulo = "[MR_SYS] MR SYS Retail - Terminal POS v1.0";

    [ObservableProperty]
    private bool _hayActualizacionDisponible;

    [ObservableProperty]
    private string _nuevaVersionTexto = string.Empty;

    [ObservableProperty]
    private string _estadoCajaTexto = "Verificando caja...";

    [ObservableProperty]
    private bool _cajaAbierta;

    [ObservableProperty]
    private string _colorFondoEstadoCaja = "#C42B1C";

    [ObservableProperty]
    private string _usuarioActivoTexto = "👤 Admin";

    [ObservableProperty]
    private string _licenciaBadgeTexto = "🛡️ Licencia Activa";

    [ObservableProperty]
    private string _licenciaBadgeColor = "#107C41";

    // --- Status Bar Inferior Fija (MR SYS Neo-Brutalismo) ---
    [ObservableProperty]
    private string _localNombreTexto = "Local: Mi Tienda";

    [ObservableProperty]
    private string _nombreComercio = "MR. SYS";

    [ObservableProperty]
    private string? _logoRuta;

    [ObservableProperty]
    private bool _tieneLogoPersonalizado;

    [ObservableProperty]
    private string _turnoEstadoResumenTexto = "Turno Cerrado";

    [ObservableProperty]
    private string _statusBarIzquierdaTexto = "Local: Mi Tienda | Caja 01 | Turno Cerrado";

    [ObservableProperty]
    private string _statusBarDerechaTexto = "[● Online] MR SYS v1.0";

    // --- Asistente Flotante Global (FAB & Slide Drawer) ---
    [ObservableProperty]
    private bool _isAsistenteFlotanteAbierto;

    public AsistenteCargaViewModel AsistenteViewModel { get; }

    public event Action? RequestCerrarSesion;

    public MainViewModel(
        ICajaService cajaService,
        IAuthService authService,
        ILicenseService licenseService,
        IConfiguracionService configuracionService,
        AsistenteCargaViewModel asistenteViewModel,
        IUpdateService updateService)
    {
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        AsistenteViewModel = asistenteViewModel ?? throw new ArgumentNullException(nameof(asistenteViewModel));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
    }

    [RelayCommand]
    public void ToggleAsistenteFlotante()
    {
        IsAsistenteFlotanteAbierto = !IsAsistenteFlotanteAbierto;
    }

    [RelayCommand]
    public void CerrarAsistenteFlotante()
    {
        IsAsistenteFlotanteAbierto = false;
    }

    [RelayCommand]
    public void AbrirAsistenteFlotante()
    {
        IsAsistenteFlotanteAbierto = true;
    }

    public async Task ActualizarInformacionAsync()
    {
        await ActualizarConfiguracionLocalAsync();
        await ActualizarEstadoCajaAsync();
        await ActualizarLicenciaYUsuarioAsync();
        _ = VerificarActualizacionesEnSegundoPlanoAsync();
    }

    private async Task VerificarActualizacionesEnSegundoPlanoAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            var owner = string.IsNullOrWhiteSpace(config.GitHubRepoOwner) ? "mrrichar11" : config.GitHubRepoOwner;
            var repo = string.IsNullOrWhiteSpace(config.GitHubRepoName) ? "puntoVentaMR" : config.GitHubRepoName;

            var updateInfo = await _updateService.VerificarActualizacionesAsync(owner, repo);
            if (updateInfo.HayActualizacionDisponible)
            {
                HayActualizacionDisponible = true;
                NuevaVersionTexto = updateInfo.NuevaVersion;
            }
            else
            {
                HayActualizacionDisponible = false;
            }
        }
        catch
        {
            // Verificación silenciosa en segundo plano: no interrumpe el flujo de la aplicación
            HayActualizacionDisponible = false;
        }
    }

    public async Task ActualizarConfiguracionLocalAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            var nombre = !string.IsNullOrWhiteSpace(config.NombreComercio) ? config.NombreComercio : "Mi Tienda";
            LocalNombreTexto = $"Local: {nombre}";
            NombreComercio = !string.IsNullOrWhiteSpace(config.NombreComercio) ? config.NombreComercio : "MR. SYS";
            LogoRuta = config.LogoRuta;
            TieneLogoPersonalizado = !string.IsNullOrWhiteSpace(config.LogoRuta) && System.IO.File.Exists(config.LogoRuta);
        }
        catch
        {
            LocalNombreTexto = "Local: Mi Tienda";
            NombreComercio = "MR. SYS";
            TieneLogoPersonalizado = false;
        }
        ActualizarStatusBarIzquierda();
    }

    private void ActualizarStatusBarIzquierda()
    {
        StatusBarIzquierdaTexto = $"{LocalNombreTexto} | Caja 01 | {TurnoEstadoResumenTexto}";
    }

    public async Task ActualizarEstadoCajaAsync()
    {
        var turno = await _cajaService.ObtenerTurnoAbiertoAsync();
        if (turno != null)
        {
            CajaAbierta = true;
            TurnoEstadoResumenTexto = "Turno Abierto";
            DateTime horaArgentina;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
                horaArgentina = TimeZoneInfo.ConvertTimeFromUtc(turno.FechaApertura, tz);
            }
            catch
            {
                horaArgentina = turno.FechaApertura.AddHours(-3);
            }

            EstadoCajaTexto = $"● Caja ABIERTA - {turno.UsuarioApertura} (Inicio: {horaArgentina:HH:mm} hs)";
            ColorFondoEstadoCaja = "#107C41"; // Verde Fluent
        }
        else
        {
            CajaAbierta = false;
            TurnoEstadoResumenTexto = "Turno Cerrado";
            EstadoCajaTexto = "● Caja CERRADA - Inicie turno para vender";
            ColorFondoEstadoCaja = "#C42B1C"; // Rojo Fluent
        }
        ActualizarStatusBarIzquierda();
    }

    public async Task ActualizarLicenciaYUsuarioAsync()
    {
        var usuario = _authService.UsuarioActual;
        if (usuario != null)
        {
            UsuarioActivoTexto = $"👤 {usuario.NombreCompleto} ({usuario.Rol})";
        }
        else
        {
            UsuarioActivoTexto = "👤 Usuario";
        }

        try
        {
            var lic = await _licenseService.ValidarLicenciaAsync();
            if (lic.EsValida)
            {
                if (lic.EstaPorVencer)
                {
                    LicenciaBadgeTexto = $"⚠️ Licencia: vence en {lic.DiasRestantes}d";
                    LicenciaBadgeColor = "#F7630C"; // Naranja
                }
                else
                {
                    LicenciaBadgeTexto = $"🛡️ Licencia: {lic.DiasRestantes}d restantes";
                    LicenciaBadgeColor = "#107C41"; // Verde
                }
            }
            else
            {
                LicenciaBadgeTexto = "⛔ Licencia Vencida";
                LicenciaBadgeColor = "#D13438"; // Rojo
            }
        }
        catch
        {
            LicenciaBadgeTexto = "🛡️ Licencia";
            LicenciaBadgeColor = "#107C41";
        }
    }

    [RelayCommand]
    public void CerrarSesion()
    {
        _authService.CerrarSesion();
        RequestCerrarSesion?.Invoke();
    }
}
