using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.DTOs.Analiticas;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.UI.ViewModels;

public partial class AnaliticasViewModel : ObservableObject
{
    private readonly IAnaliticasService _analiticasService;

    [ObservableProperty]
    private PeriodoAnalitica _periodoActual = PeriodoAnalitica.MesActual;

    [ObservableProperty]
    private string _periodoTexto = "Mes Actual";

    [ObservableProperty]
    private ReporteAnaliticasDto? _reporte;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    public AnaliticasViewModel(IAnaliticasService analiticasService)
    {
        _analiticasService = analiticasService ?? throw new ArgumentNullException(nameof(analiticasService));
    }

    public async Task CargarReporteAsync()
    {
        EstaCargando = true;
        try
        {
            Reporte = await _analiticasService.ObtenerReporteAsync(PeriodoActual);
            Mensaje = $"Datos actualizados: {Reporte.FechaInicio.ToLocalTime():dd/MM/yyyy} al {Reporte.FechaFin.ToLocalTime():dd/MM/yyyy}";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar analíticas: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    public async Task CambiarPeriodoAsync(string periodo)
    {
        switch (periodo?.ToLowerInvariant())
        {
            case "hoy":
            case "dia":
                PeriodoActual = PeriodoAnalitica.Hoy;
                PeriodoTexto = "Hoy (Día)";
                break;

            case "semana":
                PeriodoActual = PeriodoAnalitica.SemanaActual;
                PeriodoTexto = "Esta Semana";
                break;

            case "mes":
                PeriodoActual = PeriodoAnalitica.MesActual;
                PeriodoTexto = "Este Mes";
                break;

            case "año":
            case "anio":
                PeriodoActual = PeriodoAnalitica.AñoActual;
                PeriodoTexto = "Este Año";
                break;
        }

        await CargarReporteAsync();
    }
}
