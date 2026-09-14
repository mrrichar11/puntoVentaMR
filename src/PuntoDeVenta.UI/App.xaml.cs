using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.Contracts.Peripherals;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Infrastructure.Data;
using PuntoDeVenta.Infrastructure.Peripherals;
using PuntoDeVenta.Infrastructure.Repositories;
using PuntoDeVenta.Infrastructure.Services;
using PuntoDeVenta.UI.ViewModels;
using PuntoDeVenta.UI.Views.Analiticas;
using PuntoDeVenta.UI.Views.Asistente;
using PuntoDeVenta.UI.Views.Caja;
using PuntoDeVenta.UI.Views.Clientes;
using PuntoDeVenta.UI.Views.Configuracion;
using PuntoDeVenta.UI.Views.Inventario;
using PuntoDeVenta.UI.Views.Pos;
using PuntoDeVenta.UI.Views.Proveedores;
using PuntoDeVenta.UI.Views.Seguridad;

namespace PuntoDeVenta.UI;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 1. Acceso a Datos SQLite con EF Core (Ruta Absoluta para garantizar que siempre lea la BD de la app sin importar el WorkingDirectory)
                var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "puntodeventa.db");
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={dbPath};");
                });

                // 2. Repositorios y Unit of Work
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                // 3. Servicios de Aplicación
                services.AddScoped<ICalculadorPreciosService, CalculadorPreciosService>();
                services.AddScoped<IInventarioService, InventarioService>();
                services.AddScoped<ICajaService, CajaService>();
                services.AddScoped<IVentaService, VentaService>();
                services.AddScoped<ITicketPrinterService, TicketPrinterService>();
                services.AddScoped<IClienteService, ClienteService>();
                services.AddScoped<IProveedorService, ProveedorService>();
                services.AddScoped<IConfiguracionService, ConfiguracionService>();
                services.AddScoped<IAnaliticasService, AnaliticasService>();
                services.AddScoped<IBackupService, BackupService>();
                services.AddSingleton<IAuthService, AuthService>();
                services.AddScoped<ILicenseService, LicenseService>();
                services.AddScoped<IGeminiService, GeminiService>();
                services.AddScoped<IAsistenteCargaService, AsistenteCargaService>();
                services.AddScoped<IUpdateService, GitHubUpdateService>();
                services.AddSingleton<IBarcodeService, BarcodeService>();

                // 4. ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<PosViewModel>();
                services.AddTransient<CajaViewModel>();
                services.AddTransient<InventarioViewModel>();
                services.AddSingleton<AsistenteCargaViewModel>();
                services.AddTransient<ClientesViewModel>();
                services.AddTransient<ProveedoresViewModel>();
                services.AddTransient<ConfiguracionViewModel>();
                services.AddTransient<AnaliticasViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<ActivacionLicenciaViewModel>();

                // 5. Vistas
                services.AddSingleton<MainWindow>();
                services.AddTransient<PosView>();
                services.AddTransient<CajaView>();
                services.AddTransient<InventarioView>();
                services.AddTransient<AsistenteCargaView>();
                services.AddTransient<ClientesView>();
                services.AddTransient<ProveedoresView>();
                services.AddTransient<ConfiguracionView>();
                services.AddTransient<AnaliticasView>();
                services.AddTransient<LoginView>();
                services.AddTransient<ActivacionLicenciaView>();
                services.AddSingleton<Func<ActivacionLicenciaView>>(sp => () => sp.GetRequiredService<ActivacionLicenciaView>());
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Prevenir que WPF termine la aplicación al cerrar diálogos modales (Login o Activación)
            // antes de que MainWindow se haya mostrado.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            await _host.StartAsync();

            // Inicializar esquema SQLite (WAL mode) y poblar datos semilla iniciales
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await DatabaseInitializer.InitializeAsync(dbContext);
                await DataSeeder.SeedAsync(dbContext);

                try
                {
                    var configService = scope.ServiceProvider.GetRequiredService<IConfiguracionService>();
                    var config = await configService.ObtenerConfiguracionAsync();
                    if (config.TemaInterfaz.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                    {
                        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                    }
                    else
                    {
                        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
                    }
                }
                catch
                {
                    // Fallback a tema por defecto
                }
            }

            // 1. Verificación de Licencia del Sistema
            bool licenciaValida = await VerificarLicenciaAsync();
            if (!licenciaValida)
            {
                Shutdown();
                return;
            }

            // 2. Inicio de Sesión de Usuario
            bool autenticado = IniciarSesion();
            if (!autenticado)
            {
                Shutdown();
                return;
            }

            // 3. Mostrar ventana principal
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();

            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            mainViewModel.RequestCerrarSesion += () =>
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                mainWindow.Hide();
                if (IniciarSesion())
                {
                    _ = mainViewModel.ActualizarInformacionAsync();
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    mainWindow.Show();
                }
                else
                {
                    Shutdown();
                }
            };

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Se produjo un error al iniciar la aplicación:\n\n{ex.Message}\n\nDetalles:\n{ex.InnerException?.Message ?? ex.StackTrace}",
                "MR SYS Retail - Error de Inicio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task<bool> VerificarLicenciaAsync()
    {
        using var scope = _host.Services.CreateScope();
        var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
        var estado = await licenseService.ValidarLicenciaAsync();
        if (estado.EsValida)
        {
            return true;
        }

        var activacionView = _host.Services.GetRequiredService<ActivacionLicenciaView>();
        var resultado = activacionView.ShowDialog();
        return resultado == true;
    }

    private bool IniciarSesion()
    {
        var loginView = _host.Services.GetRequiredService<LoginView>();
        var resultado = loginView.ShowDialog();
        return resultado == true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        base.OnExit(e);
    }
}
