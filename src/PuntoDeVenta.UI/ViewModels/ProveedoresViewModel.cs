using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.DTOs.Proveedores;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Proveedores;

namespace PuntoDeVenta.UI.ViewModels;

public partial class LineaCompraItemViewModel : ObservableObject
{
    public Guid VarianteId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private int _cantidad = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _costoUnitario;

    public decimal? NuevoPrecioLista { get; set; }

    public decimal Subtotal => Math.Round(Cantidad * CostoUnitario, 2);
}

public partial class ProveedoresViewModel : ObservableObject
{
    private readonly IProveedorService _proveedorService;
    private readonly IInventarioService _inventarioService;
    private readonly ICajaService _cajaService;

    [ObservableProperty]
    private ObservableCollection<ProveedorDto> _proveedores = new();

    [ObservableProperty]
    private ProveedorDto? _proveedorSeleccionado;

    [ObservableProperty]
    private ObservableCollection<CompraResumenDto> _historialCompras = new();

    [ObservableProperty]
    private CompraResumenDto? _compraSeleccionada;

    [ObservableProperty]
    private string _filtroTexto = string.Empty;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    // --- Alta y Edición de Proveedor ---
    [ObservableProperty]
    private string _razonSocial = string.Empty;

    [ObservableProperty]
    private string _nombreContacto = string.Empty;

    [ObservableProperty]
    private string _cuit = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _direccion = string.Empty;

    [ObservableProperty]
    private int _plazoPagoDias = 30;

    [ObservableProperty]
    private string _notas = string.Empty;

    [ObservableProperty]
    private decimal _saldoInicial;

    [ObservableProperty]
    private string _detalleSaldoInicial = "Deuda/Factura previa a la instalación del sistema";

    [ObservableProperty]
    private string _numeroComprobanteSaldoInicial = "SALDO-INICIAL";

    // --- Deuda Previa Histórica a Proveedor Existente ---
    [ObservableProperty]
    private decimal _montoDeudaPrevia;

    [ObservableProperty]
    private string _numeroFacturaPrevia = string.Empty;

    [ObservableProperty]
    private string _detalleDeudaPrevia = "Factura/saldo previo al sistema";

    [ObservableProperty]
    private DateTime _fechaDeudaPrevia = DateTime.Today;

    [ObservableProperty]
    private DateTime? _fechaVencimientoDeudaPrevia;

    // --- Edición de Proveedor Seleccionado ---
    [ObservableProperty]
    private string _editRazonSocial = string.Empty;

    [ObservableProperty]
    private string _editNombreContacto = string.Empty;

    [ObservableProperty]
    private string _editCuit = string.Empty;

    [ObservableProperty]
    private string _editTelefono = string.Empty;

    [ObservableProperty]
    private string _editEmail = string.Empty;

    [ObservableProperty]
    private string _editDireccion = string.Empty;

    [ObservableProperty]
    private string _editNotas = string.Empty;

    [ObservableProperty]
    private int _editPlazoPagoDias = 30;

    // --- Recepción de Compra / Mercadería ---
    [ObservableProperty]
    private ProveedorDto? _proveedorCompra;

    [ObservableProperty]
    private string _numeroFactura = string.Empty;

    [ObservableProperty]
    private CondicionCompraProveedor _condicionCompra = CondicionCompraProveedor.CuentaCorrienteAPlazo;

    [ObservableProperty]
    private int _plazoCompraDias = 30;

    [ObservableProperty]
    private string _observacionesCompra = string.Empty;

    // Búsqueda de variante para agregar al remito de compra
    [ObservableProperty]
    private string _busquedaVarianteTexto = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VarianteArticuloDto> _variantesEncontradas = new();

    [ObservableProperty]
    private VarianteArticuloDto? _varianteSeleccionadaParaCompra;

    [ObservableProperty]
    private int _cantidadCompra = 10;

    [ObservableProperty]
    private decimal _costoCompraUnitario = 12000m;

    [ObservableProperty]
    private decimal? _nuevoPrecioLista;

    // Carrito del remito de compra
    [ObservableProperty]
    private ObservableCollection<LineaCompraItemViewModel> _lineasCompra = new();

    [ObservableProperty]
    private decimal _totalCompraCalculado;

    // --- Pago a Proveedor (Cuentas a Pagar) ---
    [ObservableProperty]
    private decimal _montoPagoProveedor;

    [ObservableProperty]
    private CanalDinero _canalPagoProveedor = CanalDinero.Efectivo;

    [ObservableProperty]
    private string _referenciaPagoProveedor = string.Empty;

    public ProveedoresViewModel(
        IProveedorService proveedorService,
        IInventarioService inventarioService,
        ICajaService cajaService)
    {
        _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));
        _inventarioService = inventarioService ?? throw new ArgumentNullException(nameof(inventarioService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
    }

    public async Task CargarDatosAsync()
    {
        await FiltrarProveedoresAsync();
        await CargarHistorialComprasAsync();
    }

    [RelayCommand]
    public async Task FiltrarProveedoresAsync()
    {
        try
        {
            var resultados = await _proveedorService.BuscarProveedoresAsync(FiltroTexto);
            Proveedores.Clear();
            foreach (var p in resultados)
            {
                Proveedores.Add(p);
            }

            if (ProveedorCompra == null && Proveedores.Count > 0)
            {
                ProveedorCompra = Proveedores[0];
            }

            if (ProveedorSeleccionado == null && Proveedores.Count > 0)
            {
                ProveedorSeleccionado = Proveedores[0];
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al buscar proveedores: {ex.Message}", true);
        }
    }

    public async Task CargarHistorialComprasAsync()
    {
        try
        {
            var compras = await _proveedorService.ObtenerHistorialComprasAsync(ProveedorSeleccionado?.Id);
            HistorialCompras.Clear();
            foreach (var c in compras)
            {
                HistorialCompras.Add(c);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar compras: {ex.Message}", true);
        }
    }

    partial void OnProveedorSeleccionadoChanged(ProveedorDto? value)
    {
        if (value != null)
        {
            EditRazonSocial = value.RazonSocial;
            EditNombreContacto = value.NombreContacto ?? string.Empty;
            EditCuit = value.Cuit ?? string.Empty;
            EditTelefono = value.Telefono ?? string.Empty;
            EditEmail = value.Email ?? string.Empty;
            EditDireccion = value.Direccion ?? string.Empty;
            EditNotas = value.Notas ?? string.Empty;
            EditPlazoPagoDias = value.PlazoPagoDiasDefecto;
        }
        _ = CargarHistorialComprasAsync();
    }

    partial void OnCompraSeleccionadaChanged(CompraResumenDto? value)
    {
        if (value != null)
        {
            MontoPagoProveedor = value.SaldoPendiente;
        }
    }

    [RelayCommand]
    public async Task BuscarVariantesParaCompraAsync()
    {
        try
        {
            var res = await _inventarioService.BuscarVariantesAsync(BusquedaVarianteTexto);
            VariantesEncontradas.Clear();
            foreach (var v in res)
            {
                VariantesEncontradas.Add(v);
            }

            if (VariantesEncontradas.Count > 0)
            {
                VarianteSeleccionadaParaCompra = VariantesEncontradas[0];
                CostoCompraUnitario = VarianteSeleccionadaParaCompra.PrecioCosto;
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al buscar productos: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void AgregarLineaCompra()
    {
        if (VarianteSeleccionadaParaCompra == null)
        {
            MostrarMensaje("Seleccione un producto del listado para agregarlo al remito.", true);
            return;
        }

        if (CantidadCompra <= 0)
        {
            MostrarMensaje("La cantidad recibida debe ser mayor a cero.", true);
            return;
        }

        if (CostoCompraUnitario < 0)
        {
            MostrarMensaje("El costo unitario no puede ser negativo.", true);
            return;
        }

        LineasCompra.Add(new LineaCompraItemViewModel
        {
            VarianteId = VarianteSeleccionadaParaCompra.Id,
            SKU = VarianteSeleccionadaParaCompra.SKU,
            Descripcion = $"{VarianteSeleccionadaParaCompra.NombreArticulo} ({VarianteSeleccionadaParaCompra.Talle} / {VarianteSeleccionadaParaCompra.Color})",
            Cantidad = CantidadCompra,
            CostoUnitario = CostoCompraUnitario,
            NuevoPrecioLista = NuevoPrecioLista
        });

        CalcularTotalCompra();
        MostrarMensaje($"Agregado: {VarianteSeleccionadaParaCompra.SKU} x {CantidadCompra}", false);
    }

    [RelayCommand]
    public void QuitarLineaCompra(LineaCompraItemViewModel linea)
    {
        if (linea != null)
        {
            LineasCompra.Remove(linea);
            CalcularTotalCompra();
        }
    }

    private void CalcularTotalCompra()
    {
        TotalCompraCalculado = LineasCompra.Sum(l => l.Subtotal);
    }

    [RelayCommand]
    public async Task GuardarNuevoProveedorAsync()
    {
        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            MostrarMensaje("Ingrese la razón social del proveedor.", true);
            return;
        }

        try
        {
            var dto = new ProveedorDto
            {
                RazonSocial = RazonSocial.Trim(),
                NombreContacto = string.IsNullOrWhiteSpace(NombreContacto) ? null : NombreContacto.Trim(),
                Cuit = string.IsNullOrWhiteSpace(Cuit) ? null : Cuit.Trim(),
                Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim(),
                Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                PlazoPagoDiasDefecto = PlazoPagoDias > 0 ? PlazoPagoDias : 30,
                SaldoInicial = SaldoInicial,
                DetalleSaldoInicial = DetalleSaldoInicial,
                NumeroComprobanteSaldoInicial = NumeroComprobanteSaldoInicial
            };

            var creado = await _proveedorService.CrearProveedorAsync(dto);
            MostrarMensaje($"¡Proveedor '{creado.RazonSocial}' registrado con éxito!" + (dto.SaldoInicial > 0 ? $" (Saldo adeudado previo asentado: {dto.SaldoInicial:C})" : ""), false);

            RazonSocial = string.Empty;
            NombreContacto = string.Empty;
            Cuit = string.Empty;
            Telefono = string.Empty;
            Email = string.Empty;
            Direccion = string.Empty;
            Notas = string.Empty;
            SaldoInicial = 0;
            DetalleSaldoInicial = "Deuda/Factura previa a la instalación del sistema";
            NumeroComprobanteSaldoInicial = "SALDO-INICIAL";

            await FiltrarProveedoresAsync();
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.Id == creado.Id);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al registrar proveedor: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task GuardarEdicionProveedorAsync()
    {
        if (ProveedorSeleccionado == null)
        {
            MostrarMensaje("Seleccione un proveedor para editar.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(EditRazonSocial))
        {
            MostrarMensaje("La razón social del proveedor no puede estar vacía.", true);
            return;
        }

        try
        {
            var dto = new ProveedorDto
            {
                Id = ProveedorSeleccionado.Id,
                RazonSocial = EditRazonSocial.Trim(),
                NombreContacto = string.IsNullOrWhiteSpace(EditNombreContacto) ? null : EditNombreContacto.Trim(),
                Cuit = string.IsNullOrWhiteSpace(EditCuit) ? null : EditCuit.Trim(),
                Telefono = string.IsNullOrWhiteSpace(EditTelefono) ? null : EditTelefono.Trim(),
                Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim(),
                Direccion = string.IsNullOrWhiteSpace(EditDireccion) ? null : EditDireccion.Trim(),
                Notas = string.IsNullOrWhiteSpace(EditNotas) ? null : EditNotas.Trim(),
                PlazoPagoDiasDefecto = EditPlazoPagoDias > 0 ? EditPlazoPagoDias : 30
            };

            await _proveedorService.ActualizarProveedorAsync(dto);
            MostrarMensaje($"¡Datos de '{dto.RazonSocial}' actualizados con éxito!", false);

            var idActual = ProveedorSeleccionado.Id;
            await FiltrarProveedoresAsync();
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.Id == idActual);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al actualizar proveedor: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RegistrarDeudaPreviaAsync()
    {
        if (ProveedorSeleccionado == null)
        {
            MostrarMensaje("Seleccione un proveedor para asentar la deuda previa.", true);
            return;
        }

        if (MontoDeudaPrevia <= 0)
        {
            MostrarMensaje("El monto de la deuda previa debe ser mayor a cero.", true);
            return;
        }

        try
        {
            var dto = new RegistrarDeudaPreviaProveedorDto
            {
                ProveedorId = ProveedorSeleccionado.Id,
                MontoDeudaPrevia = MontoDeudaPrevia,
                NumeroFacturaComprobante = string.IsNullOrWhiteSpace(NumeroFacturaPrevia) ? "DEUDA-PREVIA" : NumeroFacturaPrevia.Trim(),
                DetalleObservaciones = DetalleDeudaPrevia,
                FechaComprobante = DateTime.SpecifyKind(FechaDeudaPrevia, DateTimeKind.Utc),
                FechaVencimiento = FechaVencimientoDeudaPrevia.HasValue ? DateTime.SpecifyKind(FechaVencimientoDeudaPrevia.Value, DateTimeKind.Utc) : null
            };

            var compra = await _proveedorService.RegistrarDeudaPreviaAsync(dto);
            MostrarMensaje($"¡Deuda previa de {MontoDeudaPrevia:C} (Comprobante '{compra.NumeroComprobante}') registrada con éxito! Ya figura en Cuentas a Pagar.", false);

            MontoDeudaPrevia = 0;
            NumeroFacturaPrevia = string.Empty;
            DetalleDeudaPrevia = "Deuda previa al sistema";

            var idActual = ProveedorSeleccionado.Id;
            await FiltrarProveedoresAsync();
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.Id == idActual);
            await CargarHistorialComprasAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al registrar deuda previa: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RegistrarCompraMercaderiaAsync()
    {
        if (ProveedorCompra == null)
        {
            MostrarMensaje("Seleccione el proveedor que envió la mercadería.", true);
            return;
        }

        if (LineasCompra.Count == 0)
        {
            MostrarMensaje("El remito de compra no contiene productos. Agregue al menos una variante recibida.", true);
            return;
        }

        try
        {
            Guid? turnoCajaId = null;
            if (CondicionCompra == CondicionCompraProveedor.ContadoEfectivo)
            {
                var turno = await _cajaService.ObtenerTurnoAbiertoAsync();
                if (turno == null)
                {
                    MostrarMensaje("Para compras de contado efectivo se requiere tener un turno de caja abierto.", true);
                    return;
                }
                turnoCajaId = turno.Id;
            }

            var dto = new RegistrarCompraDto
            {
                ProveedorId = ProveedorCompra.Id,
                NumeroComprobante = NumeroFactura,
                Condicion = CondicionCompra,
                TurnoCajaId = turnoCajaId,
                PlazoDias = PlazoCompraDias,
                Observaciones = ObservacionesCompra,
                Lineas = LineasCompra.Select(l => new LineaCompraDto
                {
                    VarianteId = l.VarianteId,
                    Cantidad = l.Cantidad,
                    CostoUnitarioCompra = l.CostoUnitario,
                    NuevoPrecioLista = l.NuevoPrecioLista
                }).ToList()
            };

            var compra = await _proveedorService.RegistrarCompraAsync(dto);
            MostrarMensaje($"¡Compra '{compra.NumeroComprobante}' registrada con éxito! El stock ingresó inmediatamente al inventario.", false);

            LineasCompra.Clear();
            CalcularTotalCompra();
            NumeroFactura = string.Empty;
            ObservacionesCompra = string.Empty;

            await FiltrarProveedoresAsync();
            await CargarHistorialComprasAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al registrar compra: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RegistrarPagoAProveedorAsync()
    {
        if (CompraSeleccionada == null)
        {
            MostrarMensaje("Seleccione una compra con saldo pendiente del historial.", true);
            return;
        }

        if (MontoPagoProveedor <= 0)
        {
            MostrarMensaje("El importe a pagar debe ser mayor a cero.", true);
            return;
        }

        try
        {
            Guid? turnoId = null;
            if (CanalPagoProveedor == CanalDinero.Efectivo)
            {
                var turno = await _cajaService.ObtenerTurnoAbiertoAsync();
                if (turno == null)
                {
                    MostrarMensaje("Para pagar en efectivo desde el comercio debe haber un turno de caja abierto.", true);
                    return;
                }
                turnoId = turno.Id;
            }

            var dto = new RegistrarPagoProveedorDto
            {
                CompraProveedorId = CompraSeleccionada.Id,
                TurnoCajaId = turnoId,
                Monto = MontoPagoProveedor,
                Canal = CanalPagoProveedor,
                ReferenciaComprobante = ReferenciaPagoProveedor,
                Notas = $"Pago de factura {CompraSeleccionada.NumeroComprobante}"
            };

            await _proveedorService.RegistrarPagoCompraAsync(dto);
            MostrarMensaje("¡Pago a proveedor asentado con éxito!", false);

            MontoPagoProveedor = 0;
            ReferenciaPagoProveedor = string.Empty;

            await FiltrarProveedoresAsync();
            await CargarHistorialComprasAsync();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al registrar pago: {ex.Message}", true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        Mensaje = texto;
        EsMensajeError = esError;
    }
}
