using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVenta.Application.DTOs.Clientes;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.UI.ViewModels;

public partial class ClientesViewModel : ObservableObject
{
    private readonly IClienteService _clienteService;
    private readonly ICajaService _cajaService;

    [ObservableProperty]
    private ObservableCollection<ClienteDto> _clientes = new();

    [ObservableProperty]
    private ClienteDto? _clienteSeleccionado;

    [ObservableProperty]
    private ObservableCollection<MovimientoCuentaCorrienteDto> _historialMovimientos = new();

    [ObservableProperty]
    private string _filtroTexto = string.Empty;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _esMensajeError;

    // --- Alta y Edición de Cliente ---
    [ObservableProperty]
    private string _nombreCompleto = string.Empty;

    [ObservableProperty]
    private string _documentoIdentidad = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _direccion = string.Empty;

    [ObservableProperty]
    private decimal _limiteCredito = 50000m;

    [ObservableProperty]
    private string _notas = string.Empty;

    // Saldo Inicial al dar de alta
    [ObservableProperty]
    private decimal _saldoInicial;

    [ObservableProperty]
    private string _detalleSaldoInicial = "Deuda previa al sistema (libreta de fiados)";

    // --- Cobro / Entrega a Cuenta ---
    [ObservableProperty]
    private decimal _montoEntrega;

    [ObservableProperty]
    private CanalDinero _canalEntrega = CanalDinero.Efectivo;

    [ObservableProperty]
    private string _detalleEntrega = "Entrega a cuenta mensual";

    [ObservableProperty]
    private string _referenciaEntrega = string.Empty;

    // --- Registrar Saldo Previo Histórico en Clienta Existente ---
    [ObservableProperty]
    private decimal _montoSaldoHistorico;

    [ObservableProperty]
    private string _detalleSaldoHistorico = "Deuda previa anotada en cuaderno/libreta";

    [ObservableProperty]
    private DateTime _fechaSaldoHistorico = DateTime.Today;

    // --- Edición de Clienta Seleccionada ---
    [ObservableProperty]
    private string _editNombreCompleto = string.Empty;

    [ObservableProperty]
    private string _editDocumentoIdentidad = string.Empty;

    [ObservableProperty]
    private string _editTelefono = string.Empty;

    [ObservableProperty]
    private string _editEmail = string.Empty;

    [ObservableProperty]
    private string _editDireccion = string.Empty;

    [ObservableProperty]
    private decimal _editLimiteCredito;

    [ObservableProperty]
    private string _editNotas = string.Empty;

    public ClientesViewModel(IClienteService clienteService, ICajaService cajaService)
    {
        _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
        _cajaService = cajaService ?? throw new ArgumentNullException(nameof(cajaService));
    }

    public async Task CargarDatosAsync()
    {
        await FiltrarClientesAsync();
    }

    [RelayCommand]
    public async Task FiltrarClientesAsync()
    {
        try
        {
            var resultados = await _clienteService.BuscarClientesAsync(FiltroTexto);
            Clientes.Clear();
            foreach (var c in resultados)
            {
                Clientes.Add(c);
            }

            if (ClienteSeleccionado == null && Clientes.Count > 0)
            {
                ClienteSeleccionado = Clientes[0];
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al buscar clientas: {ex.Message}", true);
        }
    }

    partial void OnClienteSeleccionadoChanged(ClienteDto? value)
    {
        if (value != null)
        {
            EditNombreCompleto = value.NombreCompleto;
            EditDocumentoIdentidad = value.DocumentoIdentidad ?? string.Empty;
            EditTelefono = value.Telefono ?? string.Empty;
            EditEmail = value.Email ?? string.Empty;
            EditDireccion = value.Direccion ?? string.Empty;
            EditLimiteCredito = value.LimiteCredito;
            EditNotas = value.Notas ?? string.Empty;

            _ = CargarHistorialCuentaCorrienteAsync(value.Id);
        }
        else
        {
            HistorialMovimientos.Clear();
        }
    }

    public async Task CargarHistorialCuentaCorrienteAsync(Guid clienteId)
    {
        try
        {
            var historial = await _clienteService.ObtenerHistorialCuentaCorrienteAsync(clienteId);
            HistorialMovimientos.Clear();
            foreach (var m in historial)
            {
                HistorialMovimientos.Add(m);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar historial de cuenta: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task GuardarNuevoClienteAsync()
    {
        if (string.IsNullOrWhiteSpace(NombreCompleto))
        {
            MostrarMensaje("Ingrese el nombre completo de la clienta.", true);
            return;
        }

        try
        {
            var dto = new ClienteDto
            {
                NombreCompleto = NombreCompleto.Trim(),
                DocumentoIdentidad = string.IsNullOrWhiteSpace(DocumentoIdentidad) ? null : DocumentoIdentidad.Trim(),
                Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim(),
                LimiteCredito = LimiteCredito,
                Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                SaldoInicial = SaldoInicial,
                DetalleSaldoInicial = DetalleSaldoInicial
            };

            var creado = await _clienteService.CrearClienteAsync(dto);
            MostrarMensaje($"¡Clienta '{creado.NombreCompleto}' registrada con éxito!" + (dto.SaldoInicial > 0 ? $" (Saldo inicial cargado: {dto.SaldoInicial:C})" : ""), false);

            NombreCompleto = string.Empty;
            DocumentoIdentidad = string.Empty;
            Telefono = string.Empty;
            Email = string.Empty;
            Direccion = string.Empty;
            Notas = string.Empty;
            SaldoInicial = 0;
            DetalleSaldoInicial = "Deuda previa al sistema (libreta de fiados)";

            await FiltrarClientesAsync();
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == creado.Id);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al guardar cliente: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task GuardarEdicionClienteAsync()
    {
        if (ClienteSeleccionado == null)
        {
            MostrarMensaje("Seleccione una clienta para editar.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(EditNombreCompleto))
        {
            MostrarMensaje("El nombre de la clienta no puede estar vacío.", true);
            return;
        }

        try
        {
            var dto = new ClienteDto
            {
                Id = ClienteSeleccionado.Id,
                NombreCompleto = EditNombreCompleto.Trim(),
                DocumentoIdentidad = string.IsNullOrWhiteSpace(EditDocumentoIdentidad) ? null : EditDocumentoIdentidad.Trim(),
                Telefono = string.IsNullOrWhiteSpace(EditTelefono) ? null : EditTelefono.Trim(),
                Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim(),
                Direccion = string.IsNullOrWhiteSpace(EditDireccion) ? null : EditDireccion.Trim(),
                LimiteCredito = EditLimiteCredito,
                Notas = string.IsNullOrWhiteSpace(EditNotas) ? null : EditNotas.Trim()
            };

            await _clienteService.ActualizarClienteAsync(dto);
            MostrarMensaje($"¡Datos de '{dto.NombreCompleto}' actualizados con éxito!", false);

            var idActual = ClienteSeleccionado.Id;
            await FiltrarClientesAsync();
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == idActual);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al actualizar clienta: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task RegistrarSaldoHistoricoAsync()
    {
        if (ClienteSeleccionado == null)
        {
            MostrarMensaje("Seleccione una clienta para registrar saldo previo.", true);
            return;
        }

        if (MontoSaldoHistorico <= 0)
        {
            MostrarMensaje("El monto de la deuda previa debe ser mayor a cero.", true);
            return;
        }

        try
        {
            var dto = new RegistrarSaldoPrevioClienteDto
            {
                ClienteId = ClienteSeleccionado.Id,
                MontoDeudaPrevia = MontoSaldoHistorico,
                Detalle = DetalleSaldoHistorico,
                FechaHistorica = DateTime.SpecifyKind(FechaSaldoHistorico, DateTimeKind.Utc)
            };

            var mov = await _clienteService.RegistrarSaldoPrevioHistoricoAsync(dto);
            MostrarMensaje($"¡Deuda previa de {MontoSaldoHistorico:C} registrada con éxito! Nuevo saldo deudor: {mov.SaldoResultante:C}.", false);

            MontoSaldoHistorico = 0;
            DetalleSaldoHistorico = "Deuda previa anotada en cuaderno/libreta";

            var idActual = ClienteSeleccionado.Id;
            await FiltrarClientesAsync();
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == idActual);
            if (ClienteSeleccionado != null)
            {
                await CargarHistorialCuentaCorrienteAsync(ClienteSeleccionado.Id);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al registrar deuda previa: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public async Task CobrarEntregaEnCajaAsync()
    {
        if (ClienteSeleccionado == null)
        {
            MostrarMensaje("Seleccione una clienta para registrar la entrega.", true);
            return;
        }

        if (MontoEntrega <= 0)
        {
            MostrarMensaje("El monto de la entrega debe ser mayor a cero.", true);
            return;
        }

        try
        {
            var turnoAbierto = await _cajaService.ObtenerTurnoAbiertoAsync();
            if (turnoAbierto == null)
            {
                MostrarMensaje("No hay ningún turno de caja abierto. Debe abrir la caja para ingresar dinero.", true);
                return;
            }

            var dto = new RegistrarEntregaCuentaCorrienteDto
            {
                ClienteId = ClienteSeleccionado.Id,
                TurnoCajaId = turnoAbierto.Id,
                Monto = MontoEntrega,
                Canal = CanalEntrega,
                Detalle = DetalleEntrega,
                ReferenciaComprobante = ReferenciaEntrega
            };

            var mov = await _clienteService.RegistrarEntregaEnCajaAsync(dto);
            MostrarMensaje($"¡Entrega de {MontoEntrega:C} registrada en caja con éxito! Nuevo saldo deudor: {mov.SaldoResultante:C}.", false);

            MontoEntrega = 0;
            ReferenciaEntrega = string.Empty;

            // Recargar datos de cliente e historial
            await FiltrarClientesAsync();
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == dto.ClienteId);
            if (ClienteSeleccionado != null)
            {
                await CargarHistorialCuentaCorrienteAsync(ClienteSeleccionado.Id);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al procesar entrega: {ex.Message}", true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        Mensaje = texto;
        EsMensajeError = esError;
    }
}
