using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.DTOs.Ventas;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Ventas;
using PuntoDeVenta.Domain.Exceptions;

namespace PuntoDeVenta.Application.Services;

public class VentaService : IVentaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICalculadorPreciosService _calculadorPrecios;

    public VentaService(IUnitOfWork unitOfWork, ICalculadorPreciosService calculadorPrecios)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _calculadorPrecios = calculadorPrecios ?? throw new ArgumentNullException(nameof(calculadorPrecios));
    }

    public async Task<IReadOnlyList<VarianteArticuloDto>> BuscarItemRapidoAsync(string inputEscaneoOManual, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputEscaneoOManual))
            return Array.Empty<VarianteArticuloDto>();

        var entradaLimpia = inputEscaneoOManual.Trim();

        // 1. Detección directa por Lector Láser (EAN-13, CODE128 o SKU exacto)
        var varianteExacta = await _unitOfWork.Variantes.FirstOrDefaultAsync(v =>
            v.Activo && (v.CodigoBarras == entradaLimpia || v.SKU.ToUpper() == entradaLimpia.ToUpper()),
            cancellationToken);

        if (varianteExacta != null)
        {
            return new List<VarianteArticuloDto> { MapToDto(varianteExacta) };
        }

        // 2. Búsqueda Manual Interactiva por coincidencia flexible (múltiples palabras y plurales)
        var variantes = (await _unitOfWork.Variantes.GetAllAsync(cancellationToken))
            .Where(v => v.Activo && (v.Articulo == null || v.Articulo.Activo));
        var tokens = entradaLimpia.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var coincidencias = variantes.Where(v =>
        {
            var textoBusqueda = $"{v.SKU} {v.CodigoBarras} {v.Articulo?.Nombre} {v.Articulo?.CodigoEstilo} {v.Articulo?.Marca?.Nombre} {v.Articulo?.Categoria?.Nombre} {v.Talle} {v.Color}";
            return tokens.All(t => CoincideToken(textoBusqueda, t));
        }).ToList();

        return coincidencias.Select(MapToDto).ToList();
    }

    public async Task<VentaRealizadaDto> ProcesarVentaAsync(RegistrarVentaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Items == null || dto.Items.Count == 0)
            throw new ArgumentException("El carrito de compras no contiene artículos.", nameof(dto.Items));

        if (dto.Pagos == null || dto.Pagos.Count == 0)
            throw new ArgumentException("Debe especificar al menos un medio de pago.", nameof(dto.Pagos));

        // 1. Validar que la caja esté abierta
        var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la sesión de caja con Id '{dto.TurnoCajaId}'.");

        if (turno.Estado != EstadoTurnoCaja.Abierto)
            throw new DomainException("No se puede registrar la venta porque el turno de caja se encuentra cerrado.");

        // 2. Generar número de comprobante correlativo
        var totalVentas = (await _unitOfWork.Ventas.GetAllAsync(cancellationToken)).Count;
        var numeroComprobante = $"T-0001-{(totalVentas + 1):D8}";

        var venta = new Venta
        {
            NumeroComprobante = numeroComprobante,
            TurnoCajaId = turno.Id,
            Fecha = DateTime.UtcNow,
            ClienteId = dto.ClienteId,
            ClienteNombre = dto.ClienteNombre?.Trim(),
            ClienteDocumento = dto.ClienteDocumento?.Trim(),
            CanalVenta = dto.CanalVenta,
            NroPedidoWeb = dto.NroPedidoWeb?.Trim(),
            Vendedora = dto.Vendedora?.Trim()
        };

        // Determinar método de pago para cálculo de ajustes en las líneas si aplica
        MetodoPago? metodoPagoParaLineas = null;
        var primerPagoConMetodo = dto.Pagos.FirstOrDefault(p => p.MetodoPagoId.HasValue);
        if (primerPagoConMetodo != null)
        {
            metodoPagoParaLineas = await _unitOfWork.MetodosPago.GetByIdAsync(primerPagoConMetodo.MetodoPagoId!.Value, cancellationToken);
        }
        else if (dto.PorcentajeDescuentoEfectivo > 0)
        {
            metodoPagoParaLineas = new MetodoPago
            {
                Nombre = "Descuento Efectivo/Transferencia",
                PorcentajeAjuste = -Math.Abs(dto.PorcentajeDescuentoEfectivo)
            };
        }

        var movimientosStockVenta = new List<MovimientoStock>();
        decimal subtotalLista = 0m;
        decimal totalDescuentoOferta = 0m;
        decimal totalDescuentoMedioPago = 0m;
        decimal totalRecargoMedioPago = 0m;
        decimal totalFinalCobrado = 0m;
        decimal costoTotalHistorico = 0m;

        // 3. Procesar cada línea del carrito: stock y congelamiento de costos
        foreach (var item in dto.Items)
        {
            if (item.Cantidad <= 0)
                throw new ArgumentException("La cantidad de cada ítem debe ser mayor a cero.");

            var variante = await _unitOfWork.Variantes.GetByIdAsync(item.VarianteId, cancellationToken)
                ?? throw new KeyNotFoundException($"No se encontró el producto variante con Id '{item.VarianteId}'.");

            // Control de quiebre de stock
            if (variante.StockActual < item.Cantidad)
            {
                throw new StockInsuficienteException(variante.SKU, variante.StockActual, item.Cantidad);
            }

            // Calcular línea con congelamiento inmutable de precios y costos
            var linea = _calculadorPrecios.CalcularLineaVenta(variante, item.Cantidad, metodoPagoParaLineas, venta.Id);
            linea.Venta = venta;
            venta.Lineas.Add(linea);

            // Acumuladores de venta
            subtotalLista += linea.PrecioListaUnitario * linea.Cantidad;
            totalDescuentoOferta += linea.DescuentoOfertaUnitario * linea.Cantidad;
            totalDescuentoMedioPago += linea.DescuentoMedioPagoUnitario * linea.Cantidad;
            totalRecargoMedioPago += linea.RecargoMedioPagoUnitario * linea.Cantidad;
            totalFinalCobrado += linea.SubtotalCobrado;
            costoTotalHistorico += linea.CostoTotalHistorico;

            // Descuento atómico de stock
            var stockPrevio = variante.StockActual;
            variante.StockActual -= item.Cantidad;
            _unitOfWork.Variantes.Update(variante);

            // Registro inmutable de auditoría de inventario
            movimientosStockVenta.Add(new MovimientoStock
            {
                Variante = variante,
                VarianteId = variante.Id,
                Tipo = TipoMovimientoStock.Venta,
                Cantidad = -item.Cantidad,
                StockPrevio = stockPrevio,
                StockResultante = variante.StockActual,
                CostoUnitario = variante.PrecioCosto,
                ReferenciaDocumento = numeroComprobante,
                Motivo = $"Venta Ticket {numeroComprobante}",
                Fecha = DateTime.UtcNow
            });
        }

        venta.SubtotalLista = subtotalLista;
        venta.TotalDescuentoOferta = totalDescuentoOferta;
        venta.TotalDescuentoMedioPago = totalDescuentoMedioPago;
        venta.TotalRecargoMedioPago = totalRecargoMedioPago;
        venta.TotalFinalCobrado = totalFinalCobrado;
        venta.CostoTotalHistorico = costoTotalHistorico;

        // 4. Validar y procesar los pagos combinados (Split Payments)
        var totalAbonado = dto.Pagos.Sum(p => p.Monto);
        if (Math.Abs(totalAbonado - totalFinalCobrado) > 0.05m && totalAbonado < totalFinalCobrado)
        {
            throw new DomainException($"El importe total pagado (${totalAbonado:N2}) es insuficiente para cubrir el total de la venta (${totalFinalCobrado:N2}).");
        }

        var movimientosCaja = new List<MovimientoCaja>();

        foreach (var pagoDto in dto.Pagos)
        {
            if (pagoDto.Monto <= 0) continue;

            MetodoPago? metodo = null;
            if (pagoDto.MetodoPagoId.HasValue)
            {
                metodo = await _unitOfWork.MetodosPago.GetByIdAsync(pagoDto.MetodoPagoId.Value, cancellationToken);
            }

            var porcentajeAjuste = metodo?.PorcentajeAjuste ?? 0m;
            var comisionPorcentual = metodo?.ComisionPorcentual ?? 0m;

            var pagoVenta = new PagoVenta
            {
                Venta = venta,
                VentaId = venta.Id,
                MetodoPagoId = pagoDto.MetodoPagoId,
                MetodoPago = metodo,
                Canal = pagoDto.Canal,
                Monto = pagoDto.Monto,
                PorcentajeAjuste = porcentajeAjuste,
                ComisionPorcentual = comisionPorcentual,
                Referencia = pagoDto.Referencia?.Trim()
            };

            venta.Pagos.Add(pagoVenta);

            if (pagoDto.Canal == CanalDinero.CuentaCorriente)
            {
                // Si el pago es a Cuenta Corriente, no ingresa dinero a caja pero se incrementa la deuda del cliente
                if (!dto.ClienteId.HasValue)
                {
                    throw new DomainException("Para realizar una venta a Cuenta Corriente, debe seleccionar una clienta.");
                }

                var cliente = await _unitOfWork.Clientes.GetByIdAsync(dto.ClienteId.Value, cancellationToken)
                    ?? throw new KeyNotFoundException($"No se encontró la clienta con Id '{dto.ClienteId.Value}'.");

                var saldoPrevio = cliente.SaldoDeudorActual;
                var saldoResultante = saldoPrevio + pagoDto.Monto;

                if (cliente.LimiteCredito > 0 && saldoResultante > cliente.LimiteCredito)
                {
                    throw new DomainException($"El límite de crédito de {cliente.NombreCompleto} (${cliente.LimiteCredito:N2}) ha sido superado. Saldo resultante sería: ${saldoResultante:N2}.");
                }

                cliente.SaldoDeudorActual = saldoResultante;
                cliente.FechaUltimoMovimiento = DateTime.UtcNow;
                _unitOfWork.Clientes.Update(cliente);

                await _unitOfWork.MovimientosCuentaCorrienteClientes.AddAsync(new PuntoDeVenta.Domain.Entities.Clientes.MovimientoCuentaCorrienteCliente
                {
                    ClienteId = cliente.Id,
                    VentaId = venta.Id,
                    TurnoCajaId = turno.Id,
                    Tipo = PuntoDeVenta.Domain.Entities.Clientes.TipoMovimientoCuentaCorriente.CargoVenta,
                    Monto = pagoDto.Monto,
                    SaldoPrevio = saldoPrevio,
                    SaldoResultante = saldoResultante,
                    CanalCobro = CanalDinero.CuentaCorriente,
                    Detalle = $"Venta a cuenta ticket {numeroComprobante}",
                    ReferenciaComprobante = numeroComprobante,
                    Fecha = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                // Impacto automático en la caja según el canal del pago (Efectivo, Transferencia o Tarjeta)
                movimientosCaja.Add(new MovimientoCaja
                {
                    TurnoCajaId = turno.Id,
                    Tipo = TipoMovimientoCaja.Ingreso,
                    Concepto = ConceptoMovimientoCaja.Venta,
                    Canal = pagoDto.Canal,
                    Monto = pagoDto.Monto,
                    Descripcion = $"Cobro Venta {numeroComprobante} ({pagoDto.Canal})",
                    ReferenciaComprobante = pagoDto.Referencia?.Trim(),
                    Fecha = DateTime.UtcNow
                });
            }
        }

        // 5. Persistencia Atómica
        await _unitOfWork.Ventas.AddAsync(venta, cancellationToken);
        await _unitOfWork.MovimientosStock.AddRangeAsync(movimientosStockVenta, cancellationToken);
        await _unitOfWork.MovimientosCaja.AddRangeAsync(movimientosCaja, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Construir DTO de respuesta
        return new VentaRealizadaDto
        {
            VentaId = venta.Id,
            NumeroComprobante = venta.NumeroComprobante,
            Fecha = venta.Fecha,
            ClienteNombre = venta.ClienteNombre,
            SubtotalLista = venta.SubtotalLista,
            TotalDescuentoOferta = venta.TotalDescuentoOferta,
            TotalDescuentoMedioPago = venta.TotalDescuentoMedioPago,
            TotalRecargoMedioPago = venta.TotalRecargoMedioPago,
            TotalFinalCobrado = venta.TotalFinalCobrado,
            CostoTotalHistorico = venta.CostoTotalHistorico,
            MargenBrutoReal = venta.MargenBrutoReal,
            PorcentajeMargenBrutoReal = venta.PorcentajeMargenBrutoReal,
            Lineas = venta.Lineas.Select(l => new LineaVentaResumenDto
            {
                SKU = l.SKU,
                Descripcion = l.DescripcionArticulo,
                Talle = l.Talle,
                Color = l.Color,
                Cantidad = l.Cantidad,
                PrecioListaUnitario = l.PrecioListaUnitario,
                DescuentoOfertaUnitario = l.DescuentoOfertaUnitario,
                DescuentoMedioPagoUnitario = l.DescuentoMedioPagoUnitario,
                RecargoMedioPagoUnitario = l.RecargoMedioPagoUnitario,
                PrecioFinalCobrado = l.PrecioFinalCobrado,
                CostoUnitarioHistorico = l.CostoUnitarioHistorico,
                SubtotalCobrado = l.SubtotalCobrado,
                MargenBrutoReal = l.MargenBrutoReal
            }).ToList(),
            Pagos = venta.Pagos.Select(p => new PagoResumenDto
            {
                Canal = p.Canal,
                Monto = p.Monto,
                Referencia = p.Referencia
            }).ToList()
        };
    }

    private static VarianteArticuloDto MapToDto(VarianteArticulo v)
    {
        return new VarianteArticuloDto
        {
            Id = v.Id,
            ArticuloId = v.ArticuloId,
            CodigoEstilo = v.Articulo?.CodigoEstilo ?? string.Empty,
            NombreArticulo = v.Articulo?.Nombre ?? string.Empty,
            CategoriaNombre = v.Articulo?.Categoria?.Nombre ?? string.Empty,
            MarcaNombre = v.Articulo?.Marca?.Nombre ?? string.Empty,
            SKU = v.SKU,
            CodigoBarras = v.CodigoBarras,
            Talle = v.Talle,
            Color = v.Color,
            PrecioCosto = v.PrecioCosto,
            PrecioLista = v.PrecioLista,
            PrecioOferta = v.PrecioOferta,
            EsEnOferta = v.EsEnOferta,
            PermiteDescuentoMedioPago = v.PermiteDescuentoMedioPago,
            PrecioBaseVenta = v.PrecioBaseVenta,
            StockActual = v.StockActual,
            StockMinimo = v.StockMinimo,
            Ubicacion = v.Ubicacion
        };
    }

    private static bool CoincideToken(string texto, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return true;
        if (texto.Contains(token, StringComparison.OrdinalIgnoreCase)) return true;

        if (token.Length > 3 && token.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            var singular = token[..^1];
            if (texto.Contains(singular, StringComparison.OrdinalIgnoreCase)) return true;
        }

        if (token.Length > 4 && token.EndsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            var singular = token[..^2];
            if (texto.Contains(singular, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
