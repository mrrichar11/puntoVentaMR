using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.UI.ViewModels;

public partial class CajaViewModel : ObservableObject
{
    private readonly ICajaService _cajaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackupService _backupService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private TurnoCaja? _turnoActivo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarApertura))]
    [NotifyPropertyChangedFor(nameof(TextoEstadoTurno))]
    [NotifyPropertyChangedFor(nameof(ColorEstadoTurno))]
    private bool _hayTurnoAbierto;

    public bool MostrarApertura => !HayTurnoAbierto;
    public string TextoEstadoTurno => HayTurnoAbierto ? "TURNO ABIERTO" : "TURNO CERRADO";
    public string ColorEstadoTurno => HayTurnoAbierto ? "#107C41" : "#D13438";

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    // --- Formulario Apertura ---
    [ObservableProperty]
    private string _usuarioApertura = "Cajero Principal";

    [ObservableProperty]
    private decimal _montoInicialApertura = 20000m;

    [ObservableProperty]
    private string _infoFondoInicialSugerido = string.Empty;

    [ObservableProperty]
    private string _observacionesApertura = string.Empty;

    // --- Formulario Registro de Gastos / Retiro Propietario ---
    [ObservableProperty]
    private ObservableCollection<CategoriaGasto> _categoriasGasto = new();

    [ObservableProperty]
    private CategoriaGasto? _categoriaGastoSeleccionada;

    [ObservableProperty]
    private decimal _montoGasto;

    [ObservableProperty]
    private string _descripcionGasto = string.Empty;

    [ObservableProperty]
    private string _comprobanteGasto = string.Empty;

    [ObservableProperty]
    private CanalDinero _canalGasto = CanalDinero.Efectivo;

    // --- Formulario Arqueo y Cierre ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiferenciaEfectivo))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaEfectivo))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaEfectivo))]
    [NotifyPropertyChangedFor(nameof(TotalConteo))]
    [NotifyPropertyChangedFor(nameof(DiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTotal))]
    private decimal _conteoEfectivo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiferenciaTransferencias))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTransferencias))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTransferencias))]
    [NotifyPropertyChangedFor(nameof(TotalConteo))]
    [NotifyPropertyChangedFor(nameof(DiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTotal))]
    private decimal _conteoTransferencias;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiferenciaTarjetas))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTarjetas))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTarjetas))]
    [NotifyPropertyChangedFor(nameof(TotalConteo))]
    [NotifyPropertyChangedFor(nameof(DiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTotal))]
    private decimal _conteoTarjetas;

    [ObservableProperty]
    private string _observacionesCierre = string.Empty;

    // --- Control de Visibilidad y Datos Teóricos en Vivo ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoBotonMostrarSaldos))]
    private bool _mostrarSaldosEsperados = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DiferenciaEfectivo))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaEfectivo))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaEfectivo))]
    [NotifyPropertyChangedFor(nameof(DiferenciaTransferencias))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTransferencias))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTransferencias))]
    [NotifyPropertyChangedFor(nameof(DiferenciaTarjetas))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTarjetas))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTarjetas))]
    [NotifyPropertyChangedFor(nameof(TotalTeorico))]
    [NotifyPropertyChangedFor(nameof(DiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(InfoDiferenciaTotal))]
    [NotifyPropertyChangedFor(nameof(ColorDiferenciaTotal))]
    private SaldoTeoricoTurnoDto? _saldoTeorico;

    public string TextoBotonMostrarSaldos => MostrarSaldosEsperados ? "👁️ Ocultar Teóricos" : "👁️ Ver Teóricos";

    public decimal DiferenciaEfectivo => ConteoEfectivo - (SaldoTeorico?.MontoTeoricoEfectivo ?? 0m);
    public decimal DiferenciaTransferencias => ConteoTransferencias - (SaldoTeorico?.MontoTeoricoTransferencias ?? 0m);
    public decimal DiferenciaTarjetas => ConteoTarjetas - (SaldoTeorico?.MontoTeoricoTarjetas ?? 0m);

    public decimal TotalConteo => ConteoEfectivo + ConteoTransferencias + ConteoTarjetas;
    public decimal TotalTeorico => SaldoTeorico?.TotalTeorico ?? 0m;
    public decimal DiferenciaTotal => TotalConteo - TotalTeorico;

    public string InfoDiferenciaEfectivo => ObtenerTextoDiferencia(DiferenciaEfectivo);
    public string ColorDiferenciaEfectivo => ObtenerColorDiferencia(DiferenciaEfectivo);

    public string InfoDiferenciaTransferencias => ObtenerTextoDiferencia(DiferenciaTransferencias);
    public string ColorDiferenciaTransferencias => ObtenerColorDiferencia(DiferenciaTransferencias);

    public string InfoDiferenciaTarjetas => ObtenerTextoDiferencia(DiferenciaTarjetas);
    public string ColorDiferenciaTarjetas => ObtenerColorDiferencia(DiferenciaTarjetas);

    public string InfoDiferenciaTotal => ObtenerTextoDiferencia(DiferenciaTotal);
    public string ColorDiferenciaTotal => ObtenerColorDiferencia(DiferenciaTotal);

    private static string ObtenerTextoDiferencia(decimal diff)
    {
        if (diff == 0) return "✅ Cuadrado ($ 0,00)";
        if (diff > 0) return $"🟢 Sobrante (+${diff:N2})";
        return $"🔴 Faltante (-${Math.Abs(diff):N2})";
    }

    private static string ObtenerColorDiferencia(decimal diff)
    {
        if (diff == 0) return "#107C41";
        if (diff > 0) return "#107C41";
        return "#D13438";
    }

    // --- Resumen de Cierre Realizado ---
    [ObservableProperty]
    private ResumenCierreCajaDto? _ultimoResumenCierre;

    public event Func<Task>? OnCajaModificada;

    public CajaViewModel(
        ICajaService cajaService, 
        IUnitOfWork unitOfWork, 
        IBackupService backupService,
        IAuthService authService)
    {
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task CargarEstadoAsync()
    {
        try
        {
            if (_authService.UsuarioActual != null)
            {
                UsuarioApertura = _authService.UsuarioActual.NombreCompleto;
            }

            TurnoActivo = await _cajaService.ObtenerTurnoAbiertoAsync();
            HayTurnoAbierto = TurnoActivo != null;

            if (!HayTurnoAbierto)
            {
                SaldoTeorico = null;
                var sugerido = await _cajaService.ObtenerSugerenciaFondoInicialAsync();
                MontoInicialApertura = sugerido;
                InfoFondoInicialSugerido = $"💡 Monto sugerido del cierre anterior: ${sugerido:N0} (Editable si se realizó depósito bancario o retiro).";
            }
            else
            {
                SaldoTeorico = await _cajaService.ObtenerSaldoTeoricoActualAsync(TurnoActivo!.Id);
            }

            var categorias = await _unitOfWork.CategoriasGasto.GetAllAsync();
            CategoriasGasto.Clear();
            foreach (var cat in categorias)
            {
                CategoriasGasto.Add(cat);
            }

            if (CategoriasGasto.Count > 0 && CategoriaGastoSeleccionada == null)
            {
                CategoriaGastoSeleccionada = CategoriasGasto[0];
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar caja: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void AlternarMostrarSaldosEsperados()
    {
        MostrarSaldosEsperados = !MostrarSaldosEsperados;
    }

    [RelayCommand]
    public void CopiarSaldosTeoricosAConteo()
    {
        if (SaldoTeorico == null)
        {
            MostrarMensaje("No hay información de saldos teóricos disponible.", true);
            return;
        }

        ConteoEfectivo = SaldoTeorico.MontoTeoricoEfectivo;
        ConteoTransferencias = SaldoTeorico.MontoTeoricoTransferencias;
        ConteoTarjetas = SaldoTeorico.MontoTeoricoTarjetas;
        MostrarMensaje("Valores teóricos copiados automáticamente al conteo de arqueo.", false);
    }

    [RelayCommand]
    public async Task AbrirTurnoAsync()
    {
        try
        {
            var dto = new AbrirCajaDto
            {
                Usuario = UsuarioApertura,
                MontoInicialEfectivo = MontoInicialApertura,
                Observaciones = ObservacionesApertura
            };

            TurnoActivo = await _cajaService.AbrirTurnoAsync(dto);
            HayTurnoAbierto = true;
            UltimoResumenCierre = null;
            SaldoTeorico = await _cajaService.ObtenerSaldoTeoricoActualAsync(TurnoActivo.Id);
            MostrarMensaje($"¡Turno abierto exitosamente con ${MontoInicialApertura:N2} de fondo inicial!", false);

            if (OnCajaModificada != null)
            {
                await OnCajaModificada.Invoke();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al abrir turno: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RegistrarGastoAsync()
    {
        if (TurnoActivo == null)
        {
            MostrarMensaje("Debe haber un turno de caja abierto para registrar gastos.", true);
            return;
        }

        if (CategoriaGastoSeleccionada == null)
        {
            MostrarMensaje("Seleccione una categoría de gasto.", true);
            return;
        }

        if (MontoGasto <= 0)
        {
            MostrarMensaje("El monto debe ser mayor a cero.", true);
            return;
        }

        try
        {
            var dto = new RegistrarGastoDto
            {
                TurnoCajaId = TurnoActivo.Id,
                CategoriaGastoId = CategoriaGastoSeleccionada.Id,
                Monto = MontoGasto,
                Descripcion = DescripcionGasto,
                NumeroComprobante = ComprobanteGasto,
                Canal = CanalGasto
            };

            var gasto = await _cajaService.RegistrarGastoAsync(dto);
            var tipo = gasto.EsPersonal ? "Retiro del Propietario" : "Gasto Operativo";

            MostrarMensaje($"¡{tipo} por ${MontoGasto:N2} registrado correctamente!", false);

            MontoGasto = 0m;
            DescripcionGasto = string.Empty;
            ComprobanteGasto = string.Empty;

            // Actualizar saldos teóricos en vivo
            SaldoTeorico = await _cajaService.ObtenerSaldoTeoricoActualAsync(TurnoActivo.Id);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al registrar gasto: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CerrarTurnoCiegoAsync()
    {
        if (TurnoActivo == null)
        {
            MostrarMensaje("No hay ningún turno abierto para cerrar.", true);
            return;
        }

        try
        {
            var dto = new CerrarCajaCiegoDto
            {
                TurnoCajaId = TurnoActivo.Id,
                Usuario = _authService.UsuarioActual?.NombreCompleto ?? UsuarioApertura,
                MontoRealEfectivo = ConteoEfectivo,
                MontoRealTransferencias = ConteoTransferencias,
                MontoRealTarjetas = ConteoTarjetas,
                Observaciones = ObservacionesCierre
            };

            UltimoResumenCierre = await _cajaService.CerrarTurnoCiegoAsync(dto);
            TurnoActivo = null;
            HayTurnoAbierto = false;
            SaldoTeorico = null;

            ConteoEfectivo = 0m;
            ConteoTransferencias = 0m;
            ConteoTarjetas = 0m;
            ObservacionesCierre = string.Empty;

            string infoBackup = string.Empty;
            try
            {
                var backup = await _backupService.CrearBackupAsync(esAutomaticoCierre: true);
                infoBackup = $" ✅ Respaldo automático generado ({backup.NombreArchivo}).";
            }
            catch (Exception exBackup)
            {
                infoBackup = $" (Aviso: No se pudo generar copia automática: {exBackup.Message})";
            }

            MostrarMensaje($"¡Turno cerrado y arqueo procesado con éxito!{infoBackup}", false);

            if (OnCajaModificada != null)
            {
                await OnCajaModificada.Invoke();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cerrar turno: {ex.Message}", true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        Mensaje = texto;
        EsMensajeError = esError;
    }
}
