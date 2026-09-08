using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.Contracts.Peripherals;
using PuntoDeVenta.Application.DTOs.Finanzas;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.DTOs.Peripherals;
using PuntoDeVenta.Application.DTOs.Ventas;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.UI.ViewModels;

public partial class ItemCarritoModel : ObservableObject
{
    public Guid VarianteId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Talle { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public decimal PrecioLista { get; set; }
    public decimal? PrecioOferta { get; set; }
    public bool EsEnOferta { get; set; }
    public decimal PrecioUnitarioEfectivo => EsEnOferta ? PrecioOferta!.Value : PrecioLista;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private int _cantidad = 1;

    public int StockDisponible { get; set; }

    public decimal Subtotal => Math.Round(PrecioUnitarioEfectivo * Cantidad, 2);
}

public class PlanCuotaItem
{
    public int Cuotas { get; set; }
    public decimal PorcentajeRecargo { get; set; }
    public string NombrePlan { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public decimal MontoCuota { get; set; }
    public string DescripcionVisual { get; set; } = string.Empty;

    public override string ToString() => DescripcionVisual;
}

public partial class PosViewModel : ObservableObject
{
    private readonly IVentaService _ventaService;
    private readonly ICajaService _cajaService;
    private readonly ITicketPrinterService _ticketPrinterService;
    private readonly IClienteService _clienteService;

    [ObservableProperty]
    private string _busquedaTexto = string.Empty;

    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    [ObservableProperty]
    private ObservableCollection<VarianteArticuloDto> _resultadosBusqueda = new();

    [ObservableProperty]
    private bool _tieneResultadosBusqueda;

    // --- Estado y Apertura Rápida de Caja ---
    [ObservableProperty]
    private bool _cajaCerrada;

    [ObservableProperty]
    private decimal _fondoInicialRapido = 20000m;

    [ObservableProperty]
    private string _cajeroAperturaRapida = "Cajera Principal";

    [ObservableProperty]
    private string _infoFondoSugerido = string.Empty;

    public event Func<Task>? OnCajaModificada;

    [ObservableProperty]
    private ObservableCollection<ItemCarritoModel> _carrito = new();

    // --- Cliente / Consumidor Final / DNI ---
    [ObservableProperty]
    private bool _esConsumidorFinal = true;

    [ObservableProperty]
    private string _dniBusquedaCliente = string.Empty;

    [ObservableProperty]
    private string _infoClientaIdentificada = "Cliente: Consumidor Final (Ocasional)";

    [ObservableProperty]
    private ObservableCollection<PuntoDeVenta.Application.DTOs.Clientes.ClienteDto> _clientesDisponibles = new();

    [ObservableProperty]
    private PuntoDeVenta.Application.DTOs.Clientes.ClienteDto? _clienteSeleccionado;

    public IReadOnlyList<int> OpcionesCuotas { get; } = new[] { 1, 2, 3, 6, 9, 12, 18 };

    // --- Configuración de Cuotas y Financiación ---
    [ObservableProperty]
    private bool _habilitar3Cuotas = true;

    [ObservableProperty]
    private decimal _recargo3Cuotas = 15m;

    [ObservableProperty]
    private bool _habilitar6Cuotas = true;

    [ObservableProperty]
    private decimal _recargo6Cuotas = 25m;

    [ObservableProperty]
    private bool _habilitar9Cuotas = false;

    [ObservableProperty]
    private decimal _recargo9Cuotas = 35m;

    [ObservableProperty]
    private bool _habilitar12Cuotas = false;

    [ObservableProperty]
    private decimal _recargo12Cuotas = 45m;

    [ObservableProperty]
    private bool _cuotasIncluidasEnPrecioLista = false;

    [ObservableProperty]
    private ObservableCollection<PlanCuotaItem> _planesCuotasDisponibles = new();

    [ObservableProperty]
    private PlanCuotaItem? _planCuotaSeleccionado;

    [ObservableProperty]
    private decimal _recargoFinanciacionCredito;

    public bool TieneRecargoFinanciacion => RecargoFinanciacionCredito > 0;

    // --- Comparador de 3 Precios en Mostrador (Efectivo / Lista / Cuotas) ---
    [ObservableProperty]
    private decimal _precioEfectivoTotal;

    [ObservableProperty]
    private decimal _precioListaTotal;

    [ObservableProperty]
    private decimal _precioCuotasTotal;

    [ObservableProperty]
    private string _etiquetaTercerPrecio = "3. Cuotas";

    [ObservableProperty]
    private string _detalleCuotasTexto = string.Empty;

    // --- Canal de Venta Web ---
    [ObservableProperty]
    private bool _esVentaWeb;

    [ObservableProperty]
    private string _nroPedidoWeb = string.Empty;

    // --- Resumen de Venta ---
    [ObservableProperty]
    private decimal _subtotalLista;

    [ObservableProperty]
    private decimal _totalDescuentos;

    [ObservableProperty]
    private decimal _totalCobrar;

    // --- Inputs de Pago Combinado (Split Payment) ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAbonado))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private decimal _pagoEfectivo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAbonado))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private decimal _pagoTransferencia;

    [ObservableProperty]
    private string _referenciaTransferencia = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAbonado))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private decimal _pagoTarjetaDebito;

    [ObservableProperty]
    private string _referenciaTarjetaDebito = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAbonado))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private decimal _pagoTarjetaCredito;

    [ObservableProperty]
    private string _referenciaTarjetaCredito = string.Empty;

    [ObservableProperty]
    private int _cuotasTarjetaCredito = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAbonado))]
    [NotifyPropertyChangedFor(nameof(SaldoPendiente))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    private decimal _pagoCuentaCorriente;

    // --- Descuento por Efectivo / Transferencia & Vuelto ---
    [ObservableProperty]
    private decimal _porcentajeDescuentoEfectivo = 10m;

    [ObservableProperty]
    private bool _aplicarDescuentoEfectivo;

    [ObservableProperty]
    private decimal _descuentoMedioPago;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    private decimal _montoRecibidoEfectivo;

    [ObservableProperty]
    private bool _mostrarModalCobroExitoso;

    [ObservableProperty]
    private string _ticketRutaArchivo = string.Empty;

    [ObservableProperty]
    private string _resumenNumeroComprobante = string.Empty;

    [ObservableProperty]
    private decimal _resumenTotalCobrado;

    [ObservableProperty]
    private decimal _resumenMontoRecibido;

    [ObservableProperty]
    private decimal _resumenVuelto;

    public decimal TotalAbonado => PagoEfectivo + PagoTransferencia + PagoTarjetaDebito + PagoTarjetaCredito + PagoCuentaCorriente;
    public decimal SaldoPendiente => Math.Max(0m, TotalCobrar - TotalAbonado);
    public decimal Vuelto => (PagoEfectivo > 0 && MontoRecibidoEfectivo > PagoEfectivo)
        ? (MontoRecibidoEfectivo - PagoEfectivo)
        : Math.Max(0m, TotalAbonado - TotalCobrar);
    public bool PuedeCobrar => Carrito.Count > 0 && TotalCobrar > 0 && TotalAbonado >= TotalCobrar;

    [ObservableProperty]
    private VentaRealizadaDto? _ultimaVentaExitosa;

    private readonly IConfiguracionService _configuracionService;

    public PosViewModel(
        IVentaService ventaService,
        ICajaService cajaService,
        ITicketPrinterService ticketPrinterService,
        IClienteService clienteService,
        IConfiguracionService configuracionService)
    {
        _ventaService = ventaService ?? throw new ArgumentNullException(nameof(ventaService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
        _ticketPrinterService = ticketPrinterService ?? throw new ArgumentNullException(nameof(ticketPrinterService));
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _configuracionService = configuracionService ?? throw new ArgumentNullException(nameof(configuracionService));
    }

    public async Task CargarClientesAsync()
    {
        try
        {
            var clientes = await _clienteService.BuscarClientesAsync(string.Empty);
            ClientesDisponibles.Clear();
            foreach (var c in clientes)
            {
                ClientesDisponibles.Add(c);
            }
        }
        catch
        {
            // Ignorar en carga inicial
        }
    }

    partial void OnBusquedaTextoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ResultadosBusqueda.Clear();
            TieneResultadosBusqueda = false;
        }
    }

    [RelayCommand]
    public void CerrarSugerencias()
    {
        ResultadosBusqueda.Clear();
        TieneResultadosBusqueda = false;
    }

    [RelayCommand]
    public async Task BuscarOAgregarAsync()
    {
        if (string.IsNullOrWhiteSpace(BusquedaTexto)) return;

        try
        {
            var resultados = await _ventaService.BuscarItemRapidoAsync(BusquedaTexto);

            // Si es coincidencia única por escaneo de código de barras / SKU, agregar directo
            if (resultados.Count == 1)
            {
                AgregarAlCarrito(resultados[0]);
                BusquedaTexto = string.Empty;
                ResultadosBusqueda.Clear();
                TieneResultadosBusqueda = false;
                MostrarMensaje("Producto agregado al carrito.", false);
                return;
            }

            ResultadosBusqueda.Clear();
            foreach (var item in resultados)
            {
                ResultadosBusqueda.Add(item);
            }

            TieneResultadosBusqueda = ResultadosBusqueda.Count > 0;

            if (resultados.Count == 0)
            {
                MostrarMensaje("No se encontraron productos coincidentes.", true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al buscar: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void AgregarAlCarrito(VarianteArticuloDto variante)
    {
        ArgumentNullException.ThrowIfNull(variante);

        var existente = Carrito.FirstOrDefault(c => c.VarianteId == variante.Id);
        if (existente != null)
        {
            if (existente.Cantidad + 1 > variante.StockActual)
            {
                MostrarMensaje($"Stock máximo alcanzado para SKU {variante.SKU} (Disp: {variante.StockActual}).", true);
                return;
            }
            existente.Cantidad++;
        }
        else
        {
            if (variante.StockActual < 1)
            {
                MostrarMensaje($"Sin stock disponible para SKU {variante.SKU}.", true);
                return;
            }

            var nombreCompuesto = $"{variante.NombreArticulo}";
            Carrito.Add(new ItemCarritoModel
            {
                VarianteId = variante.Id,
                SKU = variante.SKU,
                Descripcion = nombreCompuesto,
                Talle = variante.Talle,
                Color = variante.Color,
                PrecioLista = variante.PrecioLista,
                PrecioOferta = variante.PrecioOferta,
                EsEnOferta = variante.EsEnOferta,
                Cantidad = 1,
                StockDisponible = variante.StockActual
            });
        }

        RecalcularTotales();
        ResultadosBusqueda.Clear();
        TieneResultadosBusqueda = false;
        BusquedaTexto = string.Empty;
    }

    [RelayCommand]
    public void IncrementarCantidad(ItemCarritoModel item)
    {
        if (item == null) return;
        if (item.Cantidad + 1 > item.StockDisponible)
        {
            MostrarMensaje($"Stock insuficiente (Disponible: {item.StockDisponible}).", true);
            return;
        }
        item.Cantidad++;
        RecalcularTotales();
    }

    [RelayCommand]
    public void DecrementarCantidad(ItemCarritoModel item)
    {
        if (item == null) return;
        if (item.Cantidad > 1)
        {
            item.Cantidad--;
        }
        else
        {
            Carrito.Remove(item);
        }
        RecalcularTotales();
    }

    [RelayCommand]
    public void EliminarDelCarrito(ItemCarritoModel item)
    {
        if (item != null)
        {
            Carrito.Remove(item);
            RecalcularTotales();
        }
    }

    partial void OnEsConsumidorFinalChanged(bool value)
    {
        if (value)
        {
            _clienteSeleccionado = null;
            OnPropertyChanged(nameof(ClienteSeleccionado));
            DniBusquedaCliente = string.Empty;
            InfoClientaIdentificada = "Cliente: Consumidor Final (Ocasional)";
            PagoCuentaCorriente = 0m;
            NotificarCambiosPago();
        }
    }

    partial void OnClienteSeleccionadoChanged(PuntoDeVenta.Application.DTOs.Clientes.ClienteDto? value)
    {
        if (value != null)
        {
            EsConsumidorFinal = false;
            InfoClientaIdentificada = $"Cliente: {value.NombreCompleto} | Deuda: ${value.SaldoDeudorActual:N0} | Límite: ${value.LimiteCredito:N0}";
            if (string.IsNullOrWhiteSpace(DniBusquedaCliente) || DniBusquedaCliente != value.DocumentoIdentidad)
            {
                DniBusquedaCliente = !string.IsNullOrEmpty(value.DocumentoIdentidad) ? value.DocumentoIdentidad : value.NombreCompleto;
            }
        }
        else if (EsConsumidorFinal)
        {
            InfoClientaIdentificada = "Cliente: Consumidor Final (Ocasional)";
        }
    }

    [RelayCommand]
    public void QuitarClienteSeleccionado()
    {
        EsConsumidorFinal = true;
        ClienteSeleccionado = null;
        DniBusquedaCliente = string.Empty;
        InfoClientaIdentificada = "Cliente: Consumidor Final (Ocasional)";
        PagoCuentaCorriente = 0m;
        NotificarCambiosPago();
    }

    [RelayCommand]
    public async Task BuscarClientaPorDniAsync()
    {
        if (string.IsNullOrWhiteSpace(DniBusquedaCliente))
        {
            QuitarClienteSeleccionado();
            return;
        }

        var dniLimpio = DniBusquedaCliente.Trim();
        var clientes = await _clienteService.BuscarClientesAsync(dniLimpio);
        var match = clientes.FirstOrDefault(c =>
            (!string.IsNullOrEmpty(c.DocumentoIdentidad) && c.DocumentoIdentidad.Equals(dniLimpio, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(c.Telefono) && c.Telefono.Contains(dniLimpio)) ||
            (!string.IsNullOrEmpty(c.NombreCompleto) && c.NombreCompleto.Contains(dniLimpio, StringComparison.OrdinalIgnoreCase)));

        if (match != null)
        {
            // Sincronizar con la lista de clientes disponibles para que el ComboBox seleccione la misma instancia exacta
            var enLista = ClientesDisponibles.FirstOrDefault(c => c.Id == match.Id);
            if (enLista == null)
            {
                ClientesDisponibles.Add(match);
                enLista = match;
            }

            ClienteSeleccionado = enLista;
            EsConsumidorFinal = false;
            InfoClientaIdentificada = $"Cliente: {enLista.NombreCompleto} | Deuda: ${enLista.SaldoDeudorActual:N0} | Límite: ${enLista.LimiteCredito:N0}";
            MostrarMensaje($"¡Cliente reconocido: {enLista.NombreCompleto}!", false);
        }
        else
        {
            InfoClientaIdentificada = "No registrado (se emitirá como Consumidor Final)";
            MostrarMensaje("No se encontró cliente registrado con ese DNI / Nombre.", true);
        }
    }

    partial void OnAplicarDescuentoEfectivoChanged(bool value)
    {
        RecalcularTotales();
        if (PagoEfectivo > 0)
        {
            PagoEfectivo = TotalCobrar;
            MontoRecibidoEfectivo = TotalCobrar;
        }
        else if (PagoTransferencia > 0)
        {
            PagoTransferencia = TotalCobrar;
        }
        NotificarCambiosPago();
    }

    [RelayCommand]
    public void LimpiarCarrito()
    {
        Carrito.Clear();
        PagoEfectivo = 0m;
        PagoTransferencia = 0m;
        PagoTarjetaDebito = 0m;
        PagoTarjetaCredito = 0m;
        PagoCuentaCorriente = 0m;
        MontoRecibidoEfectivo = 0m;
        DescuentoMedioPago = 0m;
        AplicarDescuentoEfectivo = false;
        MostrarModalCobroExitoso = false;
        ReferenciaTransferencia = string.Empty;
        ReferenciaTarjetaDebito = string.Empty;
        ReferenciaTarjetaCredito = string.Empty;
        EsVentaWeb = false;
        NroPedidoWeb = string.Empty;
        RecalcularTotales();
        MostrarMensaje("Carrito limpiado.", false);
    }

    [RelayCommand]
    public void AutoCompletarEfectivo()
    {
        AplicarDescuentoEfectivo = true;
        RecalcularTotales();
        PagoEfectivo = TotalCobrar;
        MontoRecibidoEfectivo = TotalCobrar;
        PagoTransferencia = 0m;
        PagoTarjetaDebito = 0m;
        PagoTarjetaCredito = 0m;
        PagoCuentaCorriente = 0m;
        NotificarCambiosPago();
    }

    [RelayCommand]
    public void AutoCompletarTransferencia()
    {
        AplicarDescuentoEfectivo = true;
        RecalcularTotales();
        PagoTransferencia = TotalCobrar;
        PagoEfectivo = 0m;
        MontoRecibidoEfectivo = 0m;
        PagoTarjetaDebito = 0m;
        PagoTarjetaCredito = 0m;
        PagoCuentaCorriente = 0m;
        NotificarCambiosPago();
    }

    [RelayCommand]
    public void AutoCompletarDebito()
    {
        AplicarDescuentoEfectivo = false;
        RecalcularTotales();
        PagoTarjetaDebito = TotalCobrar;
        PagoEfectivo = 0m;
        MontoRecibidoEfectivo = 0m;
        PagoTransferencia = 0m;
        PagoTarjetaCredito = 0m;
        PagoCuentaCorriente = 0m;
        NotificarCambiosPago();
    }

    [RelayCommand]
    public void AutoCompletarCredito()
    {
        AplicarDescuentoEfectivo = false;
        RecalcularTotales();
        PagoTarjetaCredito = TotalCobrar;
        PagoEfectivo = 0m;
        MontoRecibidoEfectivo = 0m;
        PagoTransferencia = 0m;
        PagoTarjetaDebito = 0m;
        PagoCuentaCorriente = 0m;
        NotificarCambiosPago();
    }

    [RelayCommand]
    public void AutoCompletarCuentaCorriente()
    {
        if (EsConsumidorFinal || ClienteSeleccionado == null)
        {
            MostrarMensaje("Seleccione o busque una clienta por DNI para vender a cuenta corriente.", true);
            return;
        }

        AplicarDescuentoEfectivo = false;
        RecalcularTotales();
        PagoCuentaCorriente = TotalCobrar;
        PagoEfectivo = 0m;
        MontoRecibidoEfectivo = 0m;
        PagoTransferencia = 0m;
        PagoTarjetaDebito = 0m;
        PagoTarjetaCredito = 0m;
        NotificarCambiosPago();
    }

    [RelayCommand]
    public void FijarMontoRecibido(string montoStr)
    {
        if (decimal.TryParse(montoStr, out var val))
        {
            MontoRecibidoEfectivo = val;
            if (PagoEfectivo == 0 && TotalCobrar > 0)
            {
                PagoEfectivo = Math.Min(val, TotalCobrar);
            }
            NotificarCambiosPago();
        }
    }

    private void NotificarCambiosPago()
    {
        OnPropertyChanged(nameof(TotalAbonado));
        OnPropertyChanged(nameof(SaldoPendiente));
        OnPropertyChanged(nameof(Vuelto));
        OnPropertyChanged(nameof(PuedeCobrar));
    }

    [RelayCommand]
    public async Task ProcesarCobroAsync()
    {
        if (!PuedeCobrar)
        {
            MostrarMensaje("El monto total abonado no cubre el total a cobrar.", true);
            return;
        }

        if (PagoCuentaCorriente > 0 && (EsConsumidorFinal || ClienteSeleccionado == null))
        {
            MostrarMensaje("Debe identificar a la clienta para registrar la venta a Cuenta Corriente.", true);
            return;
        }

        try
        {
            var turno = await _cajaService.ObtenerTurnoAbiertoAsync();
            if (turno == null)
            {
                MostrarMensaje("No hay ningún turno de caja abierto. Abra turno antes de cobrar.", true);
                return;
            }

            var nombreClienteFinal = EsConsumidorFinal ? "Consumidor Final" : (ClienteSeleccionado?.NombreCompleto ?? DniBusquedaCliente.Trim());
            var docClienteFinal = EsConsumidorFinal ? null : (ClienteSeleccionado?.DocumentoIdentidad ?? DniBusquedaCliente.Trim());

            var dto = new RegistrarVentaDto
            {
                TurnoCajaId = turno.Id,
                ClienteId = EsConsumidorFinal ? null : ClienteSeleccionado?.Id,
                ClienteNombre = nombreClienteFinal,
                ClienteDocumento = docClienteFinal,
                CanalVenta = EsVentaWeb ? PuntoDeVenta.Domain.Entities.Ventas.CanalVenta.TiendaWeb : PuntoDeVenta.Domain.Entities.Ventas.CanalVenta.Mostrador,
                NroPedidoWeb = EsVentaWeb ? NroPedidoWeb?.Trim() : null,
                PorcentajeDescuentoEfectivo = (AplicarDescuentoEfectivo && PorcentajeDescuentoEfectivo > 0) ? PorcentajeDescuentoEfectivo : 0m,
                Items = Carrito.Select(c => new ItemCarritoDto
                {
                    VarianteId = c.VarianteId,
                    Cantidad = c.Cantidad
                }).ToList(),
                Pagos = new List<PagoDto>()
            };

            if (PagoEfectivo > 0)
            {
                dto.Pagos.Add(new PagoDto
                {
                    Canal = CanalDinero.Efectivo,
                    Monto = Math.Min(PagoEfectivo, TotalCobrar) // No registra el vuelto como ingreso
                });
            }

            if (PagoTransferencia > 0)
            {
                dto.Pagos.Add(new PagoDto
                {
                    Canal = CanalDinero.TransferenciaQR,
                    Monto = PagoTransferencia,
                    Referencia = ReferenciaTransferencia
                });
            }

            if (PagoTarjetaDebito > 0)
            {
                dto.Pagos.Add(new PagoDto
                {
                    Canal = CanalDinero.TarjetaDebito,
                    Monto = PagoTarjetaDebito,
                    Referencia = string.IsNullOrWhiteSpace(ReferenciaTarjetaDebito) ? "Tarjeta Débito" : ReferenciaTarjetaDebito.Trim()
                });
            }

            if (PagoTarjetaCredito > 0)
            {
                var refCredito = CuotasTarjetaCredito > 1 ? $"{CuotasTarjetaCredito} cuotas" : "1 pago";
                if (!string.IsNullOrWhiteSpace(ReferenciaTarjetaCredito))
                {
                    refCredito += $" - Ref: {ReferenciaTarjetaCredito.Trim()}";
                }

                dto.Pagos.Add(new PagoDto
                {
                    Canal = CanalDinero.TarjetaCredito,
                    Monto = PagoTarjetaCredito,
                    Referencia = refCredito
                });
            }

            if (PagoCuentaCorriente > 0)
            {
                dto.Pagos.Add(new PagoDto
                {
                    Canal = CanalDinero.CuentaCorriente,
                    Monto = PagoCuentaCorriente,
                    Referencia = $"CtaCte-{ClienteSeleccionado?.NombreCompleto}"
                });
            }

            var ventaRealizada = await _ventaService.ProcesarVentaAsync(dto);
            UltimaVentaExitosa = ventaRealizada;

            // Emitir comprobante térmico y/o guardar en archivo con datos del comercio
            var configNegocio = await _configuracionService.ObtenerConfiguracionAsync();
            var configTicket = new ConfiguracionTicketDto
            {
                NombreFantasia = configNegocio.NombreComercio,
                Direccion = configNegocio.Direccion,
                Telefono = configNegocio.Telefono,
                CUIT = configNegocio.Cuit,
                GuardarCopiaEnArchivo = true,
                AbrirCajonAlCobrarEfectivo = PagoEfectivo > 0
            };

            await _ticketPrinterService.ImprimirTicketVentaAsync(ventaRealizada, configTicket);

            var fechaCarpeta = ventaRealizada.Fecha.ToString("yyyy-MM-dd");
            var rutaTicket = System.IO.Path.Combine(configTicket.RutaCarpetaTicketsArchivo, fechaCarpeta, $"{ventaRealizada.NumeroComprobante}.txt");
            TicketRutaArchivo = System.IO.File.Exists(rutaTicket) ? rutaTicket : string.Empty;

            ResumenNumeroComprobante = ventaRealizada.NumeroComprobante;
            ResumenTotalCobrado = ventaRealizada.TotalFinalCobrado;
            ResumenMontoRecibido = MontoRecibidoEfectivo > 0 ? MontoRecibidoEfectivo : (PagoEfectivo > 0 ? PagoEfectivo : ventaRealizada.TotalFinalCobrado);
            ResumenVuelto = Vuelto;

            MostrarModalCobroExitoso = true;
            MostrarMensaje($"¡Venta {ventaRealizada.NumeroComprobante} cobrada con éxito!", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al procesar cobro: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void CerrarModalVentaCompletada()
    {
        MostrarModalCobroExitoso = false;
        LimpiarCarrito();
    }

    [RelayCommand]
    public void AbrirTicketArchivo()
    {
        if (!string.IsNullOrEmpty(TicketRutaArchivo) && System.IO.File.Exists(TicketRutaArchivo))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = TicketRutaArchivo,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al abrir ticket: {ex.Message}", true);
            }
        }
    }

    private bool _isUpdatingPlanes;

    partial void OnPlanCuotaSeleccionadoChanged(PlanCuotaItem? value)
    {
        if (_isUpdatingPlanes) return;
        if (value != null)
        {
            CuotasTarjetaCredito = value.Cuotas;
            RecalcularTotales();
            if (PagoTarjetaCredito > 0)
            {
                PagoTarjetaCredito = TotalCobrar;
            }
        }
    }

    partial void OnCuotasTarjetaCreditoChanged(int value)
    {
        if (_isUpdatingPlanes) return;
        var match = PlanesCuotasDisponibles.FirstOrDefault(p => p.Cuotas == value);
        if (match != null && match != PlanCuotaSeleccionado)
        {
            PlanCuotaSeleccionado = match;
        }
    }

    private void RecalcularTotales()
    {
        SubtotalLista = Carrito.Sum(c => c.PrecioLista * c.Cantidad);
        var subtotalConOfertas = Carrito.Sum(c => c.Subtotal);
        TotalDescuentos = SubtotalLista - subtotalConOfertas;

        if (AplicarDescuentoEfectivo && PorcentajeDescuentoEfectivo > 0)
        {
            DescuentoMedioPago = Math.Round(subtotalConOfertas * (PorcentajeDescuentoEfectivo / 100m), 2);
        }
        else
        {
            DescuentoMedioPago = 0m;
        }

        // Recargo de financiación en cuotas con tarjeta de crédito (si no está incluido en precio de lista)
        if (!CuotasIncluidasEnPrecioLista && PlanCuotaSeleccionado != null && PlanCuotaSeleccionado.Cuotas > 1 && PlanCuotaSeleccionado.PorcentajeRecargo > 0)
        {
            RecargoFinanciacionCredito = Math.Round(subtotalConOfertas * (PlanCuotaSeleccionado.PorcentajeRecargo / 100m), 2);
        }
        else
        {
            RecargoFinanciacionCredito = 0m;
        }

        TotalCobrar = Math.Max(0m, subtotalConOfertas - DescuentoMedioPago + RecargoFinanciacionCredito);
        OnPropertyChanged(nameof(TieneRecargoFinanciacion));

        ActualizarPlanesCuotasCalculados(subtotalConOfertas);
        ActualizarTresPrecios(subtotalConOfertas);

        NotificarCambiosPago();
    }

    private void ActualizarPlanesCuotasCalculados(decimal baseAmount)
    {
        int cuotaSeleccionada = PlanCuotaSeleccionado?.Cuotas ?? 1;
        var lista = new List<PlanCuotaItem>();

        lista.Add(new PlanCuotaItem
        {
            Cuotas = 1,
            PorcentajeRecargo = 0m,
            NombrePlan = "1 Pago",
            MontoTotal = baseAmount,
            MontoCuota = baseAmount,
            DescripcionVisual = $"1 pago de {baseAmount:C0}"
        });

        if (Habilitar3Cuotas)
        {
            decimal recargo = CuotasIncluidasEnPrecioLista ? 0m : Recargo3Cuotas;
            decimal total = Math.Round(baseAmount * (1m + (recargo / 100m)), 2);
            decimal cuota = Math.Round(total / 3m, 2);
            lista.Add(new PlanCuotaItem
            {
                Cuotas = 3,
                PorcentajeRecargo = recargo,
                NombrePlan = "3 Cuotas",
                MontoTotal = total,
                MontoCuota = cuota,
                DescripcionVisual = CuotasIncluidasEnPrecioLista ? $"3 cuotas sin interés de {cuota:C0}" : $"3 cuotas de {cuota:C0} ({total:C0} +{recargo}%)"
            });
        }

        if (Habilitar6Cuotas)
        {
            decimal recargo = CuotasIncluidasEnPrecioLista ? 0m : Recargo6Cuotas;
            decimal total = Math.Round(baseAmount * (1m + (recargo / 100m)), 2);
            decimal cuota = Math.Round(total / 6m, 2);
            lista.Add(new PlanCuotaItem
            {
                Cuotas = 6,
                PorcentajeRecargo = recargo,
                NombrePlan = "6 Cuotas",
                MontoTotal = total,
                MontoCuota = cuota,
                DescripcionVisual = CuotasIncluidasEnPrecioLista ? $"6 cuotas sin interés de {cuota:C0}" : $"6 cuotas de {cuota:C0} ({total:C0} +{recargo}%)"
            });
        }

        if (Habilitar9Cuotas)
        {
            decimal recargo = CuotasIncluidasEnPrecioLista ? 0m : Recargo9Cuotas;
            decimal total = Math.Round(baseAmount * (1m + (recargo / 100m)), 2);
            decimal cuota = Math.Round(total / 9m, 2);
            lista.Add(new PlanCuotaItem
            {
                Cuotas = 9,
                PorcentajeRecargo = recargo,
                NombrePlan = "9 Cuotas",
                MontoTotal = total,
                MontoCuota = cuota,
                DescripcionVisual = CuotasIncluidasEnPrecioLista ? $"9 cuotas sin interés de {cuota:C0}" : $"9 cuotas de {cuota:C0} ({total:C0} +{recargo}%)"
            });
        }

        if (Habilitar12Cuotas)
        {
            decimal recargo = CuotasIncluidasEnPrecioLista ? 0m : Recargo12Cuotas;
            decimal total = Math.Round(baseAmount * (1m + (recargo / 100m)), 2);
            decimal cuota = Math.Round(total / 12m, 2);
            lista.Add(new PlanCuotaItem
            {
                Cuotas = 12,
                PorcentajeRecargo = recargo,
                NombrePlan = "12 Cuotas",
                MontoTotal = total,
                MontoCuota = cuota,
                DescripcionVisual = CuotasIncluidasEnPrecioLista ? $"12 cuotas sin interés de {cuota:C0}" : $"12 cuotas de {cuota:C0} ({total:C0} +{recargo}%)"
            });
        }

        _isUpdatingPlanes = true;
        try
        {
            PlanesCuotasDisponibles.Clear();
            foreach (var p in lista)
            {
                PlanesCuotasDisponibles.Add(p);
            }

            PlanCuotaSeleccionado = PlanesCuotasDisponibles.FirstOrDefault(p => p.Cuotas == cuotaSeleccionada)
                                     ?? PlanesCuotasDisponibles.FirstOrDefault();
            CuotasTarjetaCredito = PlanCuotaSeleccionado?.Cuotas ?? 1;
        }
        finally
        {
            _isUpdatingPlanes = false;
        }
    }

    private void ActualizarTresPrecios(decimal subtotalConOfertas)
    {
        PrecioListaTotal = subtotalConOfertas;
        PrecioEfectivoTotal = Math.Max(0m, Math.Round(subtotalConOfertas * (1m - (PorcentajeDescuentoEfectivo / 100m)), 2));

        if (CuotasIncluidasEnPrecioLista)
        {
            PrecioCuotasTotal = subtotalConOfertas;
            EtiquetaTercerPrecio = "3. Cuotas Sin Interés";
            var planReferencia = PlanesCuotasDisponibles.FirstOrDefault(p => p.Cuotas == (PlanCuotaSeleccionado?.Cuotas > 1 ? PlanCuotaSeleccionado.Cuotas : 3))
                                 ?? PlanesCuotasDisponibles.FirstOrDefault(p => p.Cuotas > 1);
            if (planReferencia != null)
            {
                DetalleCuotasTexto = $"Hasta {planReferencia.Cuotas} cuotas sin interés de {planReferencia.MontoCuota:C0}";
            }
            else
            {
                DetalleCuotasTexto = "Cuotas sin interés habilitadas según plan";
            }
        }
        else
        {
            var planReferencia = PlanesCuotasDisponibles.FirstOrDefault(p => p.Cuotas == (PlanCuotaSeleccionado?.Cuotas > 1 ? PlanCuotaSeleccionado.Cuotas : 3))
                                 ?? PlanesCuotasDisponibles.FirstOrDefault(p => p.Cuotas > 1);
            if (planReferencia != null)
            {
                PrecioCuotasTotal = planReferencia.MontoTotal;
                EtiquetaTercerPrecio = $"3. Cuotas ({planReferencia.Cuotas}x)";
                DetalleCuotasTexto = $"{planReferencia.Cuotas} cuotas de {planReferencia.MontoCuota:C0} (Total {planReferencia.MontoTotal:C0} • +{planReferencia.PorcentajeRecargo}%)";
            }
            else
            {
                PrecioCuotasTotal = subtotalConOfertas;
                EtiquetaTercerPrecio = "3. Cuotas";
                DetalleCuotasTexto = "Sin recargo de cuotas";
            }
        }
    }

    public async Task VerificarCajaAsync()
    {
        await CargarConfiguracionNegocioAsync();
        var turno = await _cajaService.ObtenerTurnoAbiertoAsync();
        CajaCerrada = turno == null;
        if (CajaCerrada)
        {
            var sugerido = await _cajaService.ObtenerSugerenciaFondoInicialAsync();
            FondoInicialRapido = sugerido;
            InfoFondoSugerido = $"💡 Sugerido del día anterior: ${sugerido:N0} (Modifíquelo si parte del dinero se depositó en el banco o se retiró).";
        }
    }

    public async Task CargarConfiguracionNegocioAsync()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync();
            PorcentajeDescuentoEfectivo = config.PorcentajeDescuentoEfectivo;
            Habilitar3Cuotas = config.Habilitar3Cuotas;
            Recargo3Cuotas = config.Recargo3Cuotas;
            Habilitar6Cuotas = config.Habilitar6Cuotas;
            Recargo6Cuotas = config.Recargo6Cuotas;
            Habilitar9Cuotas = config.Habilitar9Cuotas;
            Recargo9Cuotas = config.Recargo9Cuotas;
            Habilitar12Cuotas = config.Habilitar12Cuotas;
            Recargo12Cuotas = config.Recargo12Cuotas;
            CuotasIncluidasEnPrecioLista = config.CuotasIncluidasEnPrecioLista;
        }
        catch
        {
            PorcentajeDescuentoEfectivo = 10m;
            Habilitar3Cuotas = true;
            Recargo3Cuotas = 15m;
            Habilitar6Cuotas = true;
            Recargo6Cuotas = 25m;
            Habilitar9Cuotas = false;
            Recargo9Cuotas = 35m;
            Habilitar12Cuotas = false;
            Recargo12Cuotas = 45m;
            CuotasIncluidasEnPrecioLista = false;
        }

        RecalcularTotales();
    }

    [RelayCommand]
    public async Task AbrirCajaRapidaAsync()
    {
        try
        {
            var dto = new AbrirCajaDto
            {
                Usuario = string.IsNullOrWhiteSpace(CajeroAperturaRapida) ? "Cajera Principal" : CajeroAperturaRapida.Trim(),
                MontoInicialEfectivo = FondoInicialRapido,
                Observaciones = "Apertura rápida desde terminal TPV"
            };

            await _cajaService.AbrirTurnoAsync(dto);
            CajaCerrada = false;
            MostrarMensaje($"¡Caja abierta exitosamente con ${FondoInicialRapido:N0} en efectivo!", false);

            if (OnCajaModificada != null)
            {
                await OnCajaModificada.Invoke();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al abrir caja: {ex.Message}", true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        MensajeEstado = texto;
        EsMensajeError = esError;
    }
}
