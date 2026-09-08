using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.DTOs.Asistente;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.UI.ViewModels;

public partial class AsistenteCargaViewModel : ObservableObject
{
    private readonly IAsistenteCargaService _asistenteService;
    private readonly ILicenseService _licenseService;

    [ObservableProperty]
    private bool _esPlanPremiumActivo = true;

    [ObservableProperty]
    private string _codigoInstalacion = string.Empty;

    public bool NoEsPlanPremiumActivo => !EsPlanPremiumActivo;

    partial void OnEsPlanPremiumActivoChanged(bool value)
    {
        OnPropertyChanged(nameof(NoEsPlanPremiumActivo));
    }

    [ObservableProperty]
    private ObservableCollection<MensajeChatDto> _mensajes = new();

    [ObservableProperty]
    private string _textoEntrada = string.Empty;

    [ObservableProperty]
    private ObservableCollection<BorradorCargaArticuloDto> _borradores = new();

    [ObservableProperty]
    private BorradorCargaArticuloDto? _borradorActual;

    partial void OnBorradorActualChanged(BorradorCargaArticuloDto? value)
    {
        OnPropertyChanged(nameof(TieneBorradorActivo));
        OnPropertyChanged(nameof(NoTieneBorradorActivo));
    }

    public bool TieneBorradorActivo => BorradorActual != null;
    public bool NoTieneBorradorActivo => BorradorActual == null;

    [ObservableProperty]
    private int _indiceBorradorSeleccionado;

    [ObservableProperty]
    private bool _tieneBorradorListo;

    [ObservableProperty]
    private bool _tieneMultiplesBorradores;

    [ObservableProperty]
    private bool _puedeConfirmarTodos;

    [ObservableProperty]
    private bool _estaProcesando;

    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    // --- Acciones Universales Pendientes ---
    [ObservableProperty]
    private BorradorDeudaClienteDto? _deudaClientePendiente;

    [ObservableProperty]
    private BorradorDeudaProveedorDto? _deudaProveedorPendiente;

    [ObservableProperty]
    private BorradorContactoDto? _contactoPendiente;

    [ObservableProperty]
    private TipoAccionAsistente _tipoAccionPendiente = TipoAccionAsistente.CargarArticulo;

    public bool TieneDeudaClientePendiente => DeudaClientePendiente != null;
    public bool TieneDeudaProveedorPendiente => DeudaProveedorPendiente != null;
    public bool TieneContactoPendiente => ContactoPendiente != null;

    partial void OnDeudaClientePendienteChanged(BorradorDeudaClienteDto? value) => OnPropertyChanged(nameof(TieneDeudaClientePendiente));
    partial void OnDeudaProveedorPendienteChanged(BorradorDeudaProveedorDto? value) => OnPropertyChanged(nameof(TieneDeudaProveedorPendiente));
    partial void OnContactoPendienteChanged(BorradorContactoDto? value) => OnPropertyChanged(nameof(TieneContactoPendiente));

    public AsistenteCargaViewModel(
        IAsistenteCargaService asistenteService,
        ILicenseService licenseService)
    {
        _asistenteService = asistenteService ?? throw new ArgumentNullException(nameof(asistenteService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));

        // Inicializar chat y verificar licencia
        InicializarChat();
    }

    public void InicializarChat()
    {
        Mensajes.Clear();
        Borradores.Clear();
        BorradorActual = null;
        DeudaClientePendiente = null;
        DeudaProveedorPendiente = null;
        ContactoPendiente = null;
        TipoAccionPendiente = TipoAccionAsistente.CargarArticulo;
        TieneBorradorListo = false;
        TieneMultiplesBorradores = false;
        PuedeConfirmarTodos = false;

        _ = VerificarLicenciaAsync();

        var bienvenida = MensajeChatDto.CrearMensajeAsistente(
            "👋 ¡Hola! Soy **[MR_BOT]**, tu copiloto inteligente para MR SYS Retail.\n\n" +
            "Puedo ayudarte por voz o texto desde cualquier parte del software:\n\n" +
            "• **Carga de mercadería:** *\"Compré 5 jeans talle 40 al 48 a $33.000\"*\n" +
            "• **Anotar fiados de libreta:** *\"Anotale a Laura Benítez $15.000 de la libreta vieja\"*\n" +
            "• **Facturas de proveedores:** *\"Anota al proveedor Textil Sur factura 1029 por $180.000\"*\n" +
            "• **Consultas del negocio:** *\"¿Qué margen me sugerís para camperas?\"*\n\n" +
            "Decime qué necesitás registrar y te preparo la confirmación.");

        Mensajes.Add(bienvenida);
    }

    public async Task VerificarLicenciaAsync()
    {
        try
        {
            var estado = await _licenseService.ValidarLicenciaAsync();
            CodigoInstalacion = estado.CodigoInstalacion;
            EsPlanPremiumActivo = estado.TieneModuloIA;
        }
        catch
        {
            EsPlanPremiumActivo = true; // Tolerancia en caso de error
        }
    }

    [RelayCommand]
    public void SolicitarActivacionWhatsApp()
    {
        try
        {
            string texto = Uri.EscapeDataString($"¡Hola! Me gustaría activar el Módulo de Asistente IA Premium [MR_BOT] para mi local.\nCódigo de mi equipo: {CodigoInstalacion}");
            string url = $"https://wa.me/?text={texto}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignorar
        }
    }

    [RelayCommand]
    public async Task EnviarMensajeAsync()
    {
        if (string.IsNullOrWhiteSpace(TextoEntrada) || EstaProcesando) return;

        string entrada = TextoEntrada.Trim();
        TextoEntrada = string.Empty;

        // 1. Agregar mensaje del usuario
        Mensajes.Add(MensajeChatDto.CrearMensajeUsuario(entrada));

        EstaProcesando = true;
        MensajeEstado = EsPlanPremiumActivo ? "Consultando con IA..." : "MR_BOT procesando...";

        try
        {
            // 2. Procesar con el servicio de asistente
            var respuesta = await _asistenteService.ProcesarMensajeAsync(
                entrada,
                BorradorActual,
                Borradores.ToList(),
                DeudaClientePendiente,
                DeudaProveedorPendiente,
                ContactoPendiente);

            // 3. Actualizar acciones universales y borradores
            TipoAccionPendiente = respuesta.TipoAccion;
            DeudaClientePendiente = respuesta.BorradorDeudaCliente;
            DeudaProveedorPendiente = respuesta.BorradorDeudaProveedor;
            ContactoPendiente = respuesta.BorradorContacto;

            if (respuesta.Borradores.Count > 0)
            {
                Borradores.Clear();
                foreach (var b in respuesta.Borradores)
                {
                    Borradores.Add(b);
                }

                // Seleccionar el primer borrador listo o el primero
                BorradorActual = Borradores.FirstOrDefault(b => b.EsValidoParaGuardar) ?? Borradores.FirstOrDefault();
                IndiceBorradorSeleccionado = BorradorActual != null ? Borradores.IndexOf(BorradorActual) : 0;
            }
            else if (respuesta.Borrador != null)
            {
                if (!Borradores.Contains(respuesta.Borrador))
                {
                    Borradores.Clear();
                    Borradores.Add(respuesta.Borrador);
                }
                BorradorActual = respuesta.Borrador;
                IndiceBorradorSeleccionado = 0;
            }
            else if (respuesta.TipoAccion != TipoAccionAsistente.CargarArticulo)
            {
                Borradores.Clear();
                BorradorActual = null;
            }

            ActualizarEstadosBorradores();

            // 4. Agregar respuesta del bot
            Mensajes.Add(respuesta);
            MensajeEstado = string.Empty;
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al procesar: {ex.Message}";
            EsMensajeError = true;
            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente($"⚠️ Ocurrió un inconveniente al interpretar el mensaje: {ex.Message}. Por favor, intentá nuevamente."));
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmarDeudaClienteAsync()
    {
        if (DeudaClientePendiente == null || EstaProcesando) return;

        EstaProcesando = true;
        MensajeEstado = "Registrando deuda en cuenta corriente...";

        try
        {
            var mov = await _asistenteService.ConfirmarDeudaClienteAsync(DeudaClientePendiente);
            Mensajes.Add(new MensajeChatDto
            {
                Rol = RolMensajeChat.Asistente,
                Texto = $"✅ ¡Deuda de **${DeudaClientePendiente.Monto:N2}** registrada con éxito para **{DeudaClientePendiente.NombreCliente}**!\n\n" +
                        $"• Nuevo saldo adeudado: **${mov.SaldoResultante:N2}**\n" +
                        $"• Se asentó en su ficha de Cuenta Corriente sin alterar los fondos de caja.",
                MotorUtilizado = "Local"
            });

            DeudaClientePendiente = null;
            TipoAccionPendiente = TipoAccionAsistente.ConsultaGeneral;
            MensajeEstado = "¡Deuda registrada con éxito!";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al asentar deuda: {ex.Message}";
            EsMensajeError = true;
            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente($"❌ Error al asentar deuda: {ex.Message}"));
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmarDeudaProveedorAsync()
    {
        if (DeudaProveedorPendiente == null || EstaProcesando) return;

        EstaProcesando = true;
        MensajeEstado = "Registrando comprobante en Cuentas a Pagar...";

        try
        {
            var compra = await _asistenteService.ConfirmarDeudaProveedorAsync(DeudaProveedorPendiente);
            Mensajes.Add(new MensajeChatDto
            {
                Rol = RolMensajeChat.Asistente,
                Texto = $"✅ ¡Factura **{compra.NumeroComprobante}** por **${compra.TotalCompra:N2}** registrada con éxito para **{DeudaProveedorPendiente.NombreProveedor}**!\n\n" +
                        $"• Incorporada a Cuentas a Pagar en estado Pendiente de Pago.\n" +
                        $"• Podés saldarla desde el módulo de Proveedores en cualquier momento.",
                MotorUtilizado = "Local"
            });

            DeudaProveedorPendiente = null;
            TipoAccionPendiente = TipoAccionAsistente.ConsultaGeneral;
            MensajeEstado = "¡Factura previa asentada con éxito!";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al asentar factura: {ex.Message}";
            EsMensajeError = true;
            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente($"❌ Error al asentar factura: {ex.Message}"));
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmarNuevoContactoAsync()
    {
        if (ContactoPendiente == null || EstaProcesando) return;

        EstaProcesando = true;
        MensajeEstado = "Guardando contacto en el sistema...";

        try
        {
            await _asistenteService.ConfirmarNuevoContactoAsync(ContactoPendiente);
            string saldo = ContactoPendiente.SaldoInicial > 0 ? $" con un saldo inicial de ${ContactoPendiente.SaldoInicial:N2}" : "";
            Mensajes.Add(new MensajeChatDto
            {
                Rol = RolMensajeChat.Asistente,
                Texto = $"✅ ¡Alta de {ContactoPendiente.TipoContacto} **{ContactoPendiente.Nombre}** completada con éxito{saldo}!",
                MotorUtilizado = "Local"
            });

            ContactoPendiente = null;
            TipoAccionPendiente = TipoAccionAsistente.ConsultaGeneral;
            MensajeEstado = "¡Contacto registrado con éxito!";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al guardar contacto: {ex.Message}";
            EsMensajeError = true;
            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente($"❌ Error al guardar contacto: {ex.Message}"));
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    [RelayCommand]
    public void DescartarAccionPendiente()
    {
        DeudaClientePendiente = null;
        DeudaProveedorPendiente = null;
        ContactoPendiente = null;
        Borradores.Clear();
        BorradorActual = null;
        TipoAccionPendiente = TipoAccionAsistente.ConsultaGeneral;
        ActualizarEstadosBorradores();
        Mensajes.Add(MensajeChatDto.CrearMensajeAsistente("Acción descartada. ¿En qué más puedo ayudarte?"));
    }

    [RelayCommand]
    public void SeleccionarBorrador(BorradorCargaArticuloDto? borrador)
    {
        if (borrador == null) return;
        BorradorActual = borrador;
        IndiceBorradorSeleccionado = Borradores.IndexOf(borrador);
        ActualizarEstadosBorradores();
    }

    [RelayCommand]
    public async Task ConfirmarCargaInventarioAsync()
    {
        if (BorradorActual == null || !TieneBorradorListo || EstaProcesando) return;

        EstaProcesando = true;
        MensajeEstado = "Guardando en inventario y generando stock...";

        try
        {
            string nombreArticulo = BorradorActual.NombreArticulo;
            int variantesCreadas = await _asistenteService.ConfirmarBorradorAsync(BorradorActual);

            // Remover el borrador guardado de la lista
            Borradores.Remove(BorradorActual);

            if (Borradores.Count > 0)
            {
                BorradorActual = Borradores.FirstOrDefault();
                IndiceBorradorSeleccionado = 0;
                ActualizarEstadosBorradores();

                Mensajes.Add(MensajeChatDto.CrearMensajeAsistente(
                    $"🚀 ¡Operación completada con éxito!\n" +
                    $"Se crearon {variantesCreadas} variantes del artículo '{nombreArticulo}' con sus SKUs y stock inicial.\n" +
                    $"Quedan {Borradores.Count} producto(s) en la lista para revisar y confirmar.",
                    listoParaConfirmar: TieneBorradorListo));
            }
            else
            {
                BorradorActual = null;
                ActualizarEstadosBorradores();

                Mensajes.Add(MensajeChatDto.CrearMensajeAsistente(
                    $"🚀 ¡Operación completada con éxito!\n" +
                    $"Se crearon {variantesCreadas} variantes del artículo '{nombreArticulo}' con sus SKUs y stock inicial.\n" +
                    $"Ya podés visualizarlas en Catálogo y Stock o facturarlas en el Punto de Venta.",
                    listoParaConfirmar: false));
            }

            MensajeEstado = $"¡{variantesCreadas} variantes ingresadas al inventario!";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al guardar: {ex.Message}";
            EsMensajeError = true;
            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente($"❌ Error al guardar en base de datos: {ex.Message}"));
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmarTodosLosBorradoresAsync()
    {
        var listos = Borradores.Where(b => b.EsValidoParaGuardar).ToList();
        if (listos.Count == 0 || EstaProcesando) return;

        EstaProcesando = true;
        MensajeEstado = "Guardando todos los productos listos en inventario...";

        try
        {
            int totalVariantes = await _asistenteService.ConfirmarTodosAsync(listos);

            foreach (var b in listos)
            {
                Borradores.Remove(b);
            }

            BorradorActual = Borradores.FirstOrDefault();
            IndiceBorradorSeleccionado = BorradorActual != null ? Borradores.IndexOf(BorradorActual) : 0;
            ActualizarEstadosBorradores();

            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente(
                $"🚀 ¡Todos los productos listos ({listos.Count}) fueron ingresados con éxito!\n" +
                $"Se dieron de alta un total de {totalVariantes} variantes en el catálogo con sus respectivos stocks iniciales.\n" +
                (Borradores.Count > 0 ? $"Aún tenés {Borradores.Count} borrador(es) pendiente(s) de completar datos." : "Todo el catálogo quedó actualizado."),
                listoParaConfirmar: false));

            MensajeEstado = $"¡{totalVariantes} variantes ingresadas al inventario!";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al guardar: {ex.Message}";
            EsMensajeError = true;
            Mensajes.Add(MensajeChatDto.CrearMensajeAsistente($"❌ Error al guardar en base de datos: {ex.Message}"));
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    [RelayCommand]
    public void CancelarBorrador()
    {
        if (BorradorActual != null)
        {
            Borradores.Remove(BorradorActual);
            BorradorActual = Borradores.FirstOrDefault();
            IndiceBorradorSeleccionado = BorradorActual != null ? Borradores.IndexOf(BorradorActual) : 0;
            ActualizarEstadosBorradores();
        }

        Mensajes.Add(MensajeChatDto.CrearMensajeAsistente("Borrador descartado. ¿Qué otro producto deseás registrar?"));
    }

    [RelayCommand]
    public void LimpiarChat()
    {
        InicializarChat();
        TextoEntrada = string.Empty;
    }

    [RelayCommand]
    public void UsarEjemploRapido(string ejemplo)
    {
        TextoEntrada = ejemplo;
    }

    private void ActualizarEstadosBorradores()
    {
        TieneMultiplesBorradores = Borradores.Count > 1;
        TieneBorradorListo = BorradorActual != null && BorradorActual.EsValidoParaGuardar;
        PuedeConfirmarTodos = Borradores.Count(b => b.EsValidoParaGuardar) >= 2;
    }
}
