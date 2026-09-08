using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.DTOs.Backup;
using PuntoDeVenta.Application.DTOs.Configuracion;
using PuntoDeVenta.Application.DTOs.Sistema;
using PuntoDeVenta.Application.Services;
using Wpf.Ui.Appearance;

namespace PuntoDeVenta.UI.ViewModels;

public partial class ConfiguracionViewModel : ObservableObject
{
    // --- Datos de la Tienda ---
    [ObservableProperty]
    private string _nombreComercio = string.Empty;

    [ObservableProperty]
    private string _direccion = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _cuit = string.Empty;

    [ObservableProperty]
    private string _vendedoraDefecto = string.Empty;

    // --- Logo del Comercio ---
    [ObservableProperty]
    private string? _logoRuta;

    [ObservableProperty]
    private bool _tieneLogo;

    // --- Finanzas y Políticas Comerciales ---
    [ObservableProperty]
    private decimal _porcentajeDescuentoEfectivo = 10.0m;

    [ObservableProperty]
    private decimal _comisionTarjetaDebito = 1.5m;

    [ObservableProperty]
    private decimal _comisionTarjetaCredito = 4.5m;

    [ObservableProperty]
    private decimal _recargoCuotasTarjetaCredito = 15.0m;

    // Cuotas Diferenciadas (3, 6, 9, 12)
    [ObservableProperty]
    private bool _habilitar3Cuotas = true;

    [ObservableProperty]
    private decimal _recargo3Cuotas = 15.0m;

    [ObservableProperty]
    private bool _habilitar6Cuotas = true;

    [ObservableProperty]
    private decimal _recargo6Cuotas = 25.0m;

    [ObservableProperty]
    private bool _habilitar9Cuotas = false;

    [ObservableProperty]
    private decimal _recargo9Cuotas = 35.0m;

    [ObservableProperty]
    private bool _habilitar12Cuotas = false;

    [ObservableProperty]
    private decimal _recargo12Cuotas = 45.0m;

    [ObservableProperty]
    private bool _cuotasIncluidasEnPrecioLista = false;

    [ObservableProperty]
    private decimal _margenGananciaSugerido = 80.0m;

    [ObservableProperty]
    private decimal _costosBancariosEstimados = 5.0m;

    [ObservableProperty]
    private decimal _topeFiadoDefecto = 50000m;

    [ObservableProperty]
    private decimal _topeMensualRetiroDueño = 600000m;

    [RelayCommand]
    public void SeleccionarLogo()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Seleccionar Logo del Comercio",
            Filter = "Archivos de Imagen (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Todos los archivos (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logoDir = System.IO.Path.Combine(appData, "PuntoDeVenta", "Logos");
                System.IO.Directory.CreateDirectory(logoDir);
                string extension = System.IO.Path.GetExtension(dlg.FileName);
                string destFile = System.IO.Path.Combine(logoDir, $"logo_comercio{extension}");
                System.IO.File.Copy(dlg.FileName, destFile, overwrite: true);

                LogoRuta = destFile;
                TieneLogo = true;
                MostrarMensaje("Logo seleccionado con éxito. Haga clic en Guardar para confirmar los cambios.", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar logo: {ex.Message}", true);
            }
        }
    }

    [RelayCommand]
    public void QuitarLogo()
    {
        LogoRuta = null;
        TieneLogo = false;
        MostrarMensaje("Logo quitado. Haga clic en Guardar para confirmar los cambios.", false);
    }

    // --- Retiros del Dueño (Barra de Progreso) ---
    [ObservableProperty]
    private decimal _totalRetiradoMes;

    [ObservableProperty]
    private decimal _saldoDisponibleRetiro;

    [ObservableProperty]
    private decimal _porcentajeRetiroConsumido;

    [ObservableProperty]
    private bool _haSuperadoTopeRetiro;

    [ObservableProperty]
    private string _colorBarraRetiro = "#107C41";

    // --- Tema & Apariencia ---
    [ObservableProperty]
    private string _temaSeleccionado = "Light";

    // --- Mensajes de Estado ---
    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    private readonly IConfiguracionService _configuracionService;
    private readonly IBackupService _backupService;
    private readonly ILicenseService _licenseService;
    private readonly IAuthService _authService;
    private readonly IUpdateService _updateService;

    // --- Copias de Seguridad & Backups ---
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<BackupInfoDto> _historialBackups = new();

    // --- Licenciamiento & Suscripción (Solo Cliente) ---
    [ObservableProperty]
    private string _licenciaCodigoInstalacion = string.Empty;

    [ObservableProperty]
    private string _licenciaEstadoTexto = string.Empty;

    [ObservableProperty]
    private string _licenciaFechaExpiracionTexto = string.Empty;

    [ObservableProperty]
    private int _licenciaDiasRestantes;

    [ObservableProperty]
    private string _licenciaTipoPlanTexto = string.Empty;

    [ObservableProperty]
    private bool _licenciaTieneModuloIA;

    [ObservableProperty]
    private string _licenciaClaveNuevaInput = string.Empty;

    // --- Actualizaciones del Sistema (GitHub Releases) ---
    [ObservableProperty]
    private string _gitHubRepoOwner = "mrrichar11";

    [ObservableProperty]
    private string _gitHubRepoName = "puntoVentaMR";

    [ObservableProperty]
    private string _versionActualTexto = "1.0.0";

    [ObservableProperty]
    private string _estadoVerificacionActualizacion = "Presione 'Buscar Actualizaciones' para verificar si hay versiones nuevas.";

    [ObservableProperty]
    private bool _hayNuevaVersionDisponible;

    [ObservableProperty]
    private ActualizacionDto? _nuevaVersionInfo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeBuscarActualizacion))]
    private bool _estaBuscandoActualizacion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeBuscarActualizacion))]
    [NotifyPropertyChangedFor(nameof(PuedeInstalarActualizacion))]
    private bool _estaDescargandoActualizacion;

    public bool PuedeBuscarActualizacion => !EstaBuscandoActualizacion && !EstaDescargandoActualizacion;
    public bool PuedeInstalarActualizacion => !EstaDescargandoActualizacion;

    [ObservableProperty]
    private double _progresoDescargaPorcentaje;

    [ObservableProperty]
    private string _progresoDescargaTexto = string.Empty;

    // --- Gestión de Usuarios & Seguridad ---
    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<PuntoDeVenta.Application.DTOs.Seguridad.UsuarioDto> _usuarios = new();

    [ObservableProperty]
    private string _passwordActual = string.Empty;

    [ObservableProperty]
    private string _passwordNuevo = string.Empty;

    [ObservableProperty]
    private string _passwordConfirmacion = string.Empty;

    [ObservableProperty]
    private string _nuevoUsername = string.Empty;

    [ObservableProperty]
    private string _nuevoNombreCompleto = string.Empty;

    [ObservableProperty]
    private string _nuevoPassword = string.Empty;

    [ObservableProperty]
    private PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario _nuevoRol = PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Vendedor;

    public System.Collections.Generic.IReadOnlyList<PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario> RolesDisponibles =>
        new[] {
            PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Cajero,
            PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Vendedor,
            PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Encargado,
            PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Administrador
        };

    public string UsuarioLogueadoTexto => _authService.UsuarioActual != null
        ? $"{_authService.UsuarioActual.NombreCompleto} ({_authService.UsuarioActual.Username}) - {_authService.UsuarioActual.Rol}"
        : "Usuario no identificado";

    public ConfiguracionViewModel(
        IConfiguracionService configuracionService,
        IBackupService backupService,
        ILicenseService licenseService,
        IAuthService authService,
        IUpdateService updateService)
    {
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            NombreComercio = config.NombreComercio;
            Direccion = config.Direccion;
            Telefono = config.Telefono;
            Cuit = config.Cuit;
            VendedoraDefecto = config.VendedoraDefecto;
            LogoRuta = config.LogoRuta;
            TieneLogo = !string.IsNullOrWhiteSpace(config.LogoRuta) && System.IO.File.Exists(config.LogoRuta);
            PorcentajeDescuentoEfectivo = config.PorcentajeDescuentoEfectivo;
            ComisionTarjetaDebito = config.ComisionTarjetaDebito;
            ComisionTarjetaCredito = config.ComisionTarjetaCredito;
            RecargoCuotasTarjetaCredito = config.RecargoCuotasTarjetaCredito;
            Habilitar3Cuotas = config.Habilitar3Cuotas;
            Recargo3Cuotas = config.Recargo3Cuotas;
            Habilitar6Cuotas = config.Habilitar6Cuotas;
            Recargo6Cuotas = config.Recargo6Cuotas;
            Habilitar9Cuotas = config.Habilitar9Cuotas;
            Recargo9Cuotas = config.Recargo9Cuotas;
            Habilitar12Cuotas = config.Habilitar12Cuotas;
            Recargo12Cuotas = config.Recargo12Cuotas;
            CuotasIncluidasEnPrecioLista = config.CuotasIncluidasEnPrecioLista;
            MargenGananciaSugerido = config.MargenGananciaSugerido;
            CostosBancariosEstimados = config.CostosBancariosEstimados;
            TopeFiadoDefecto = config.TopeFiadoDefecto;
            TopeMensualRetiroDueño = config.TopeMensualRetiroDueño;
            TemaSeleccionado = config.TemaInterfaz;
            GitHubRepoOwner = string.IsNullOrWhiteSpace(config.GitHubRepoOwner) ? "mrrichar11" : config.GitHubRepoOwner;
            GitHubRepoName = string.IsNullOrWhiteSpace(config.GitHubRepoName) ? "puntoVentaMR" : config.GitHubRepoName;
            VersionActualTexto = _updateService.ObtenerVersionActual();

            await RefrescarEstadoRetirosAsync();
            await CargarHistorialBackupsAsync();
            await CargarEstadoLicenciaAsync();
            await CargarUsuariosAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar configuración: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RefrescarEstadoRetirosAsync()
    {
        try
        {
            var retiros = await _configuracionService.ObtenerEstadoRetirosDueñoMesAsync();
            TotalRetiradoMes = retiros.TotalRetiradoMes;
            SaldoDisponibleRetiro = retiros.SaldoDisponible;
            PorcentajeRetiroConsumido = retiros.PorcentajeConsumido;
            HaSuperadoTopeRetiro = retiros.HaSuperadoTope;

            // Semáforo de progreso: Verde < 70%, Amarillo/Naranja 70-95%, Rojo > 95%
            if (PorcentajeRetiroConsumido < 70m)
            {
                ColorBarraRetiro = "#107C41"; // Verde
            }
            else if (PorcentajeRetiroConsumido < 95m)
            {
                ColorBarraRetiro = "#F7630C"; // Naranja
            }
            else
            {
                ColorBarraRetiro = "#D13438"; // Rojo
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al calcular retiros: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task GuardarConfiguracionAsync()
    {
        try
        {
            var dto = new ConfiguracionNegocioDto
            {
                NombreComercio = NombreComercio.Trim(),
                Direccion = Direccion.Trim(),
                Telefono = Telefono.Trim(),
                Cuit = Cuit.Trim(),
                VendedoraDefecto = VendedoraDefecto.Trim(),
                LogoRuta = LogoRuta,
                PorcentajeDescuentoEfectivo = PorcentajeDescuentoEfectivo,
                ComisionTarjetaDebito = ComisionTarjetaDebito,
                ComisionTarjetaCredito = ComisionTarjetaCredito,
                RecargoCuotasTarjetaCredito = RecargoCuotasTarjetaCredito,
                Habilitar3Cuotas = Habilitar3Cuotas,
                Recargo3Cuotas = Recargo3Cuotas,
                Habilitar6Cuotas = Habilitar6Cuotas,
                Recargo6Cuotas = Recargo6Cuotas,
                Habilitar9Cuotas = Habilitar9Cuotas,
                Recargo9Cuotas = Recargo9Cuotas,
                Habilitar12Cuotas = Habilitar12Cuotas,
                Recargo12Cuotas = Recargo12Cuotas,
                CuotasIncluidasEnPrecioLista = CuotasIncluidasEnPrecioLista,
                MargenGananciaSugerido = MargenGananciaSugerido,
                CostosBancariosEstimados = CostosBancariosEstimados,
                TopeFiadoDefecto = TopeFiadoDefecto,
                TopeMensualRetiroDueño = TopeMensualRetiroDueño,
                TemaInterfaz = TemaSeleccionado,
                GitHubRepoOwner = GitHubRepoOwner.Trim(),
                GitHubRepoName = GitHubRepoName.Trim()
            };

            await _configuracionService.GuardarConfiguracionAsync(dto);
            await RefrescarEstadoRetirosAsync();
            AplicarTema(TemaSeleccionado);

            MostrarMensaje("¡Parámetros y políticas guardados con éxito!", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al guardar configuración: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CambiarTemaAsync(string tema)
    {
        TemaSeleccionado = tema;
        AplicarTema(tema);
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            config.TemaInterfaz = tema;
            await _configuracionService.GuardarConfiguracionAsync(config);
            MostrarMensaje($"Tema visual actualizado a {(tema.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? "Oscuro (Dark)" : "Claro (Light)")}.", false);
        }
        catch
        {
            // Ignorar en errores de guardado de tema
        }
    }

    private void AplicarTema(string tema)
    {
        try
        {
            if (tema.Equals("Light", StringComparison.OrdinalIgnoreCase))
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
            }
            else
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            }
        }
        catch
        {
            // Ignorar en entornos de diseño
        }
    }

    // --- COMANDOS DE COPIAS DE SEGURIDAD (BACKUP) ---

    public async Task CargarHistorialBackupsAsync()
    {
        try
        {
            var backups = await _backupService.ObtenerHistorialBackupsAsync();
            HistorialBackups.Clear();
            foreach (var b in backups)
            {
                HistorialBackups.Add(b);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al leer historial de backups: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CrearBackupManualAsync()
    {
        try
        {
            var backup = await _backupService.CrearBackupAsync(esAutomaticoCierre: false);
            await CargarHistorialBackupsAsync();
            MostrarMensaje($"¡Copia de seguridad generada con éxito ({backup.NombreArchivo})!", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al crear copia de seguridad: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task ExportarBackupPersonalizadoAsync()
    {
        try
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar Copia de Seguridad a Pendrive o Carpeta",
                Filter = "Base de Datos SQLite (*.db)|*.db",
                FileName = $"backup_puntodeventa_export_{DateTime.Now:yyyyMMdd_HHmm}.db"
            };

            if (sfd.ShowDialog() == true)
            {
                var backup = await _backupService.CrearBackupAsync(sfd.FileName, esAutomaticoCierre: false);
                await CargarHistorialBackupsAsync();
                MostrarMensaje($"¡Copia de seguridad exportada con éxito en: {sfd.FileName}!", false);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al exportar copia de seguridad: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RestaurarBackupAsync()
    {
        try
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar Copia de Seguridad para Restaurar",
                Filter = "Base de Datos SQLite (*.db)|*.db"
            };

            if (ofd.ShowDialog() == true)
            {
                var confirmacion = System.Windows.MessageBox.Show(
                    $"¿Está seguro de que desea restaurar la base de datos desde el archivo:\n\n{ofd.FileName}?\n\n" +
                    "Se generará una copia de resguardo preventiva de la base actual antes de restaurar.",
                    "Confirmación de Restauración",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (confirmacion == System.Windows.MessageBoxResult.Yes)
                {
                    await _backupService.RestaurarBackupAsync(ofd.FileName);
                    await CargarDatosAsync();
                    MostrarMensaje("¡Base de datos restaurada con éxito! Se recomienda reiniciar el sistema.", false);
                }
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al restaurar copia de seguridad: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void AbrirCarpetaBackups()
    {
        try
        {
            var carpeta = _backupService.ObtenerCarpetaBackupsPredeterminada();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = carpeta,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudo abrir la carpeta: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CargarEstadoLicenciaAsync()
    {
        try
        {
            var lic = await _licenseService.ValidarLicenciaAsync();
            LicenciaCodigoInstalacion = lic.CodigoInstalacion;
            LicenciaEstadoTexto = lic.MensajeEstado;
            LicenciaFechaExpiracionTexto = lic.FechaExpiracion.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            LicenciaDiasRestantes = lic.DiasRestantes;
            LicenciaTieneModuloIA = lic.TieneModuloIA;
            LicenciaTipoPlanTexto = lic.TieneModuloIA
                ? "⭐ PLAN PREMIUM (CON ASISTENTE IA GEMINI)"
                : "PLAN ESTÁNDAR (SIN ASISTENTE IA)";
        }
        catch (Exception ex)
        {
            LicenciaEstadoTexto = $"Error al cargar licencia: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task RenovarLicenciaAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenciaClaveNuevaInput))
        {
            MostrarMensaje("Por favor, ingresa la clave de activación.", true);
            return;
        }

        try
        {
            var res = await _licenseService.ActivarLicenciaAsync(LicenciaClaveNuevaInput);
            if (res.Exitoso)
            {
                LicenciaClaveNuevaInput = string.Empty;
                await CargarEstadoLicenciaAsync();
                MostrarMensaje(res.Mensaje, false);
            }
            else
            {
                MostrarMensaje(res.Mensaje, true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al renovar: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void CopiarCodigoInstalacion()
    {
        try
        {
            System.Windows.Clipboard.SetText(LicenciaCodigoInstalacion);
            MostrarMensaje("¡Código de instalación copiado al portapapeles! Envíalo por WhatsApp a tu proveedor.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al copiar: {ex.Message}", true);
        }
    }

    // --- COMANDOS DE GESTIÓN DE USUARIOS & SEGURIDAD ---

    [RelayCommand]
    public async Task CargarUsuariosAsync()
    {
        try
        {
            var lista = await _authService.ObtenerUsuariosAsync();
            Usuarios.Clear();
            foreach (var u in lista)
            {
                Usuarios.Add(u);
            }
            OnPropertyChanged(nameof(UsuarioLogueadoTexto));
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar usuarios: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CambiarContraseñaAsync()
    {
        if (_authService.UsuarioActual == null)
        {
            MostrarMensaje("No hay ningún usuario activo en la sesión actual.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordActual))
        {
            MostrarMensaje("Por favor, ingresa tu contraseña actual.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordNuevo) || PasswordNuevo.Length < 4)
        {
            MostrarMensaje("La nueva contraseña debe tener al menos 4 caracteres.", true);
            return;
        }

        if (PasswordNuevo != PasswordConfirmacion)
        {
            MostrarMensaje("La nueva contraseña y su confirmación no coinciden.", true);
            return;
        }

        try
        {
            await _authService.CambiarContraseñaAsync(_authService.UsuarioActual.Id, PasswordActual, PasswordNuevo);
            PasswordActual = string.Empty;
            PasswordNuevo = string.Empty;
            PasswordConfirmacion = string.Empty;
            MostrarMensaje("¡Contraseña actualizada con éxito! Úsala la próxima vez que inicies sesión.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cambiar contraseña: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CrearUsuarioAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoUsername))
        {
            MostrarMensaje("Por favor, ingresa un nombre de usuario (ej. cajera1).", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevoPassword) || NuevoPassword.Length < 4)
        {
            MostrarMensaje("La contraseña inicial debe contener al menos 4 caracteres.", true);
            return;
        }

        try
        {
            var creado = await _authService.CrearUsuarioAsync(
                NuevoUsername.Trim(),
                NuevoNombreCompleto.Trim(),
                NuevoPassword,
                NuevoRol);

            NuevoUsername = string.Empty;
            NuevoNombreCompleto = string.Empty;
            NuevoPassword = string.Empty;
            NuevoRol = PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Vendedor;

            await CargarUsuariosAsync();
            MostrarMensaje($"¡Usuario '{creado.Username}' creado con éxito con rol {creado.Rol}!", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al crear usuario: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task BuscarActualizacionesAsync()
    {
        if (EstaBuscandoActualizacion || EstaDescargandoActualizacion) return;

        try
        {
            EstaBuscandoActualizacion = true;
            EstadoVerificacionActualizacion = "Consultando GitHub Releases...";
            var resultado = await _updateService.VerificarActualizacionesAsync(GitHubRepoOwner, GitHubRepoName);

            if (resultado.HayActualizacionDisponible)
            {
                HayNuevaVersionDisponible = true;
                NuevaVersionInfo = resultado;
                EstadoVerificacionActualizacion = $"¡Nueva versión {resultado.NuevaVersion} disponible!";
                MostrarMensaje($"¡Se encontró una nueva versión ({resultado.NuevaVersion})! Puedes descargarla e instalarla.", false);
            }
            else
            {
                HayNuevaVersionDisponible = false;
                NuevaVersionInfo = null;
                EstadoVerificacionActualizacion = string.IsNullOrWhiteSpace(resultado.Mensaje)
                    ? "El sistema está actualizado a la última versión disponible."
                    : resultado.Mensaje;
                MostrarMensaje(EstadoVerificacionActualizacion, false);
            }
        }
        catch (Exception ex)
        {
            EstadoVerificacionActualizacion = $"Error al verificar: {ex.Message}";
            MostrarMensaje($"Error al verificar actualizaciones: {ex.Message}", true);
        }
        finally
        {
            EstaBuscandoActualizacion = false;
        }
    }

    [RelayCommand]
    public async Task DescargarEInstalarActualizacionAsync()
    {
        if (NuevaVersionInfo == null || EstaDescargandoActualizacion) return;

        try
        {
            EstaDescargandoActualizacion = true;
            ProgresoDescargaPorcentaje = 0;
            ProgresoDescargaTexto = "Conectando para descargar actualización...";

            var progress = new Progress<ProgresoDescargaDto>(p =>
            {
                ProgresoDescargaPorcentaje = p.Porcentaje;
                ProgresoDescargaTexto = $"{p.Porcentaje}% ({p.BytesDescargados / 1024.0 / 1024.0:F1} MB / {p.TotalBytes / 1024.0 / 1024.0:F1} MB) - {p.Estado}";
            });

            var zipPath = await _updateService.DescargarActualizacionAsync(NuevaVersionInfo, progress);

            ProgresoDescargaTexto = "Descarga finalizada con éxito. Iniciando actualización y reiniciando el sistema...";
            await Task.Delay(1200);

            _updateService.IniciarInstalacion(zipPath);
        }
        catch (Exception ex)
        {
            EstaDescargandoActualizacion = false;
            ProgresoDescargaTexto = $"Error durante la descarga: {ex.Message}";
            MostrarMensaje($"Error al procesar actualización: {ex.Message}", true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        Mensaje = texto;
        EsMensajeError = esError;
    }
}
