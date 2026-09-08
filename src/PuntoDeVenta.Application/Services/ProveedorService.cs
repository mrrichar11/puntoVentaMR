using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Proveedores;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Proveedores;
using PuntoDeVenta.Domain.Exceptions;

namespace PuntoDeVenta.Application.Services;

public class ProveedorService : IProveedorService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProveedorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Proveedor> CrearProveedorAsync(ProveedorDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.RazonSocial))
            throw new ArgumentException("La razón social del proveedor es obligatoria.", nameof(dto.RazonSocial));

        decimal saldoInicial = Math.Max(0, dto.SaldoInicial);

        var proveedor = new Proveedor
        {
            RazonSocial = dto.RazonSocial.Trim(),
            NombreContacto = dto.NombreContacto?.Trim(),
            Cuit = dto.Cuit?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Email = dto.Email?.Trim(),
            Direccion = dto.Direccion?.Trim(),
            Notas = dto.Notas?.Trim(),
            PlazoPagoDiasDefecto = dto.PlazoPagoDiasDefecto > 0 ? dto.PlazoPagoDiasDefecto : 30,
            SaldoDeudorActual = saldoInicial
        };

        await _unitOfWork.Proveedores.AddAsync(proveedor, cancellationToken);

        if (saldoInicial > 0)
        {
            string nroComp = string.IsNullOrWhiteSpace(dto.NumeroComprobanteSaldoInicial)
                ? $"SALDO-PREVIO-{DateTime.Now:yyyyMMdd}"
                : dto.NumeroComprobanteSaldoInicial.Trim();

            string detalle = string.IsNullOrWhiteSpace(dto.DetalleSaldoInicial)
                ? "Saldo deudor / Factura previa al sistema"
                : dto.DetalleSaldoInicial.Trim();

            var compraPrevia = new CompraProveedor
            {
                ProveedorId = proveedor.Id,
                NumeroComprobante = nroComp,
                Fecha = DateTime.UtcNow,
                FechaVencimientoPlazo = DateTime.UtcNow.AddDays(dto.PlazoPagoDiasDefecto > 0 ? dto.PlazoPagoDiasDefecto : 30),
                Condicion = CondicionCompraProveedor.CuentaCorrienteAPlazo,
                Estado = EstadoCompraProveedor.PendientePago,
                TotalCompra = saldoInicial,
                TotalPagado = 0m,
                Observaciones = $"[Saldo Previo Histórico] {detalle}"
            };

            await _unitOfWork.ComprasProveedores.AddAsync(compraPrevia, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return proveedor;
    }

    public async Task<Proveedor> ActualizarProveedorAsync(ProveedorDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el proveedor con Id '{dto.Id}'.");

        proveedor.RazonSocial = dto.RazonSocial.Trim();
        proveedor.NombreContacto = dto.NombreContacto?.Trim();
        proveedor.Cuit = dto.Cuit?.Trim();
        proveedor.Telefono = dto.Telefono?.Trim();
        proveedor.Email = dto.Email?.Trim();
        proveedor.Direccion = dto.Direccion?.Trim();
        proveedor.Notas = dto.Notas?.Trim();
        proveedor.PlazoPagoDiasDefecto = dto.PlazoPagoDiasDefecto > 0 ? dto.PlazoPagoDiasDefecto : 30;

        _unitOfWork.Proveedores.Update(proveedor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return proveedor;
    }

    public async Task<IReadOnlyList<ProveedorDto>> BuscarProveedoresAsync(string termino, CancellationToken cancellationToken = default)
    {
        var terminoLimpio = termino?.Trim().ToUpperInvariant() ?? string.Empty;

        var proveedores = await _unitOfWork.Proveedores.FindAsync(p =>
            p.Activo && (
                p.RazonSocial.ToUpper().Contains(terminoLimpio) ||
                (p.NombreContacto != null && p.NombreContacto.ToUpper().Contains(terminoLimpio)) ||
                (p.Cuit != null && p.Cuit.Contains(terminoLimpio))
            ),
            cancellationToken);

        return proveedores.OrderBy(p => p.RazonSocial).Select(p => new ProveedorDto
        {
            Id = p.Id,
            RazonSocial = p.RazonSocial,
            NombreContacto = p.NombreContacto,
            Cuit = p.Cuit,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion,
            Notas = p.Notas,
            PlazoPagoDiasDefecto = p.PlazoPagoDiasDefecto,
            SaldoDeudorActual = p.SaldoDeudorActual
        }).ToList();
    }

    public async Task<ProveedorDto?> ObtenerPorIdAsync(Guid proveedorId, CancellationToken cancellationToken = default)
    {
        var p = await _unitOfWork.Proveedores.GetByIdAsync(proveedorId, cancellationToken);
        if (p == null) return null;

        return new ProveedorDto
        {
            Id = p.Id,
            RazonSocial = p.RazonSocial,
            NombreContacto = p.NombreContacto,
            Cuit = p.Cuit,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion,
            Notas = p.Notas,
            PlazoPagoDiasDefecto = p.PlazoPagoDiasDefecto,
            SaldoDeudorActual = p.SaldoDeudorActual
        };
    }

    public async Task<CompraProveedor> RegistrarCompraAsync(RegistrarCompraDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Lineas == null || dto.Lineas.Count == 0)
            throw new ArgumentException("Debe ingresar al menos un producto recibido en la compra.", nameof(dto.Lineas));

        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(dto.ProveedorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el proveedor con Id '{dto.ProveedorId}'.");

        var plazo = dto.PlazoDias.HasValue && dto.PlazoDias.Value > 0 ? dto.PlazoDias.Value : proveedor.PlazoPagoDiasDefecto;

        var compra = new CompraProveedor
        {
            ProveedorId = proveedor.Id,
            NumeroComprobante = string.IsNullOrWhiteSpace(dto.NumeroComprobante) ? $"FAC-{DateTime.UtcNow:yyyyMMddHHmm}" : dto.NumeroComprobante.Trim(),
            Fecha = DateTime.UtcNow,
            Condicion = dto.Condicion,
            TurnoCajaId = dto.TurnoCajaId,
            Observaciones = dto.Observaciones?.Trim()
        };

        if (dto.Condicion == CondicionCompraProveedor.CuentaCorrienteAPlazo)
        {
            compra.FechaVencimientoPlazo = DateTime.UtcNow.AddDays(plazo);
            compra.Estado = EstadoCompraProveedor.PendientePago;
        }
        else
        {
            compra.Estado = EstadoCompraProveedor.Pagada;
        }

        var movimientosStock = new List<MovimientoStock>();
        decimal totalCompra = 0m;

        foreach (var lineaDto in dto.Lineas)
        {
            if (lineaDto.Cantidad <= 0)
                throw new ArgumentException("La cantidad recibida de cada producto debe ser mayor a cero.");

            if (lineaDto.CostoUnitarioCompra < 0)
                throw new ArgumentException("El costo unitario de compra no puede ser negativo.");

            var variante = await _unitOfWork.Variantes.GetByIdAsync(lineaDto.VarianteId, cancellationToken)
                ?? throw new KeyNotFoundException($"No se encontró la variante con Id '{lineaDto.VarianteId}'.");

            // Incrementar stock en almacén
            var stockPrevio = variante.StockActual;
            variante.StockActual += lineaDto.Cantidad;

            // Actualizar costo de reposición
            variante.PrecioCosto = lineaDto.CostoUnitarioCompra;
            if (lineaDto.NuevoPrecioLista.HasValue && lineaDto.NuevoPrecioLista.Value > 0)
            {
                variante.PrecioLista = lineaDto.NuevoPrecioLista.Value;
            }

            _unitOfWork.Variantes.Update(variante);

            var subtotal = lineaDto.Cantidad * lineaDto.CostoUnitarioCompra;
            totalCompra += subtotal;

            compra.Lineas.Add(new LineaCompraProveedor
            {
                CompraProveedorId = compra.Id,
                VarianteId = variante.Id,
                Cantidad = lineaDto.Cantidad,
                CostoUnitarioCompra = lineaDto.CostoUnitarioCompra
            });

            // Registrar movimiento trazable de stock
            movimientosStock.Add(new MovimientoStock
            {
                VarianteId = variante.Id,
                Tipo = TipoMovimientoStock.EntradaCompra,
                Cantidad = lineaDto.Cantidad,
                StockPrevio = stockPrevio,
                StockResultante = variante.StockActual,
                CostoUnitario = lineaDto.CostoUnitarioCompra,
                Motivo = $"Compra a proveedor {proveedor.RazonSocial}",
                ReferenciaDocumento = compra.NumeroComprobante,
                Fecha = DateTime.UtcNow
            });
        }

        compra.TotalCompra = totalCompra;

        if (dto.Condicion == CondicionCompraProveedor.CuentaCorrienteAPlazo)
        {
            compra.TotalPagado = 0m;
            proveedor.SaldoDeudorActual += totalCompra;
            _unitOfWork.Proveedores.Update(proveedor);
        }
        else
        {
            // Contado: se registra el pago total inmediato
            compra.TotalPagado = totalCompra;
            var canal = dto.Condicion == CondicionCompraProveedor.ContadoEfectivo ? CanalDinero.Efectivo : CanalDinero.TransferenciaQR;

            compra.Pagos.Add(new PagoCompraProveedor
            {
                CompraProveedorId = compra.Id,
                TurnoCajaId = dto.TurnoCajaId,
                Fecha = DateTime.UtcNow,
                Monto = totalCompra,
                Canal = canal,
                ReferenciaComprobante = compra.NumeroComprobante,
                Notas = "Pago de contado al recibir mercadería"
            });

            // Si se pagó de contado y hay turno de caja asignado, registrar egreso en caja
            if (dto.TurnoCajaId.HasValue)
            {
                var movCaja = new MovimientoCaja
                {
                    TurnoCajaId = dto.TurnoCajaId.Value,
                    Tipo = TipoMovimientoCaja.Egreso,
                    Concepto = ConceptoMovimientoCaja.PagoAProveedor,
                    Canal = canal,
                    Monto = totalCompra,
                    Descripcion = $"Pago compra a {proveedor.RazonSocial} ({compra.NumeroComprobante})",
                    ReferenciaComprobante = compra.NumeroComprobante,
                    Fecha = DateTime.UtcNow
                };
                await _unitOfWork.MovimientosCaja.AddAsync(movCaja, cancellationToken);
            }
        }

        await _unitOfWork.ComprasProveedores.AddAsync(compra, cancellationToken);
        await _unitOfWork.MovimientosStock.AddRangeAsync(movimientosStock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return compra;
    }

    public async Task<PagoCompraProveedor> RegistrarPagoCompraAsync(RegistrarPagoProveedorDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Monto <= 0)
            throw new ArgumentException("El monto a pagar debe ser mayor a cero.", nameof(dto.Monto));

        var compra = await _unitOfWork.ComprasProveedores.GetByIdAsync(dto.CompraProveedorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la compra con Id '{dto.CompraProveedorId}'.");

        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(compra.ProveedorId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el proveedor correspondiente a la compra.");

        var pago = new PagoCompraProveedor
        {
            CompraProveedorId = compra.Id,
            TurnoCajaId = dto.TurnoCajaId,
            Fecha = DateTime.UtcNow,
            Monto = dto.Monto,
            Canal = dto.Canal,
            ReferenciaComprobante = dto.ReferenciaComprobante?.Trim(),
            Notas = dto.Notas?.Trim()
        };

        compra.TotalPagado += dto.Monto;
        if (compra.SaldoPendiente <= 0m)
        {
            compra.Estado = EstadoCompraProveedor.Pagada;
        }

        proveedor.SaldoDeudorActual = Math.Max(0m, proveedor.SaldoDeudorActual - dto.Monto);

        _unitOfWork.ComprasProveedores.Update(compra);
        _unitOfWork.Proveedores.Update(proveedor);
        await _unitOfWork.PagosCompraProveedores.AddAsync(pago, cancellationToken);

        // Si se paga desde la caja física
        if (dto.TurnoCajaId.HasValue)
        {
            var movCaja = new MovimientoCaja
            {
                TurnoCajaId = dto.TurnoCajaId.Value,
                Tipo = TipoMovimientoCaja.Egreso,
                Concepto = ConceptoMovimientoCaja.PagoAProveedor,
                Canal = dto.Canal,
                Monto = dto.Monto,
                Descripcion = $"Pago proveedor {proveedor.RazonSocial} Factura {compra.NumeroComprobante}",
                ReferenciaComprobante = dto.ReferenciaComprobante?.Trim(),
                Fecha = DateTime.UtcNow
            };
            await _unitOfWork.MovimientosCaja.AddAsync(movCaja, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return pago;
    }

    public async Task<IReadOnlyList<CompraResumenDto>> ObtenerHistorialComprasAsync(Guid? proveedorId = null, CancellationToken cancellationToken = default)
    {
        var compras = proveedorId.HasValue
            ? await _unitOfWork.ComprasProveedores.FindAsync(c => c.ProveedorId == proveedorId.Value && c.Activo, cancellationToken)
            : await _unitOfWork.ComprasProveedores.GetAllAsync(cancellationToken);

        var proveedores = (await _unitOfWork.Proveedores.GetAllAsync(cancellationToken)).ToDictionary(p => p.Id, p => p.RazonSocial);

        return compras.OrderByDescending(c => c.Fecha).Select(c => new CompraResumenDto
        {
            Id = c.Id,
            NumeroComprobante = c.NumeroComprobante,
            ProveedorNombre = proveedores.TryGetValue(c.ProveedorId, out var nom) ? nom : "Proveedor",
            Fecha = c.Fecha,
            FechaVencimientoPlazo = c.FechaVencimientoPlazo,
            Condicion = c.Condicion.ToString(),
            Estado = c.Estado.ToString(),
            TotalCompra = c.TotalCompra,
            TotalPagado = c.TotalPagado,
            SaldoPendiente = c.SaldoPendiente,
            TotalUnidadesRecibidas = c.Lineas.Sum(l => l.Cantidad)
        }).ToList();
    }

    public async Task<CompraProveedor> RegistrarDeudaPreviaAsync(RegistrarDeudaPreviaProveedorDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.MontoDeudaPrevia <= 0)
            throw new ArgumentException("El monto de la deuda previa debe ser mayor a cero.", nameof(dto.MontoDeudaPrevia));

        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(dto.ProveedorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el proveedor con Id '{dto.ProveedorId}'.");

        proveedor.SaldoDeudorActual += dto.MontoDeudaPrevia;
        _unitOfWork.Proveedores.Update(proveedor);

        string nroComp = string.IsNullOrWhiteSpace(dto.NumeroFacturaComprobante)
            ? $"PREVIO-{DateTime.Now:yyyyMMdd-HHmm}"
            : dto.NumeroFacturaComprobante.Trim();

        string obs = string.IsNullOrWhiteSpace(dto.DetalleObservaciones)
            ? "Factura / Deuda previa a la utilización del sistema"
            : dto.DetalleObservaciones.Trim();

        var compraPrevia = new CompraProveedor
        {
            ProveedorId = proveedor.Id,
            NumeroComprobante = nroComp,
            Fecha = dto.FechaComprobante ?? DateTime.UtcNow,
            FechaVencimientoPlazo = dto.FechaVencimiento ?? DateTime.UtcNow.AddDays(proveedor.PlazoPagoDiasDefecto > 0 ? proveedor.PlazoPagoDiasDefecto : 30),
            Condicion = CondicionCompraProveedor.CuentaCorrienteAPlazo,
            Estado = EstadoCompraProveedor.PendientePago,
            TotalCompra = dto.MontoDeudaPrevia,
            TotalPagado = 0m,
            Observaciones = $"[Saldo Previo Histórico] {obs}"
        };

        await _unitOfWork.ComprasProveedores.AddAsync(compraPrevia, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return compraPrevia;
    }
}

internal static class ProveedorExtensions
{
    public static int PlazoPagoDiasDefecto(this Proveedor p, int valor) => valor > 0 ? valor : 30;
}
