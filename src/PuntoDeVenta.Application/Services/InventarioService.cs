using System.Text.RegularExpressions;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Exceptions;

namespace PuntoDeVenta.Application.Services;

public class InventarioService : IInventarioService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventarioService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Articulo> CrearArticuloConMatrizAsync(CrearArticuloDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.CodigoEstilo))
            throw new ArgumentException("El código de estilo o modelo es obligatorio.", nameof(dto.CodigoEstilo));

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ArgumentException("El nombre del artículo es obligatorio.", nameof(dto.Nombre));

        if (dto.Talles == null || dto.Talles.Count == 0)
            throw new ArgumentException("Debe especificar al menos un talle para generar las variantes.", nameof(dto.Talles));

        if (dto.Colores == null || dto.Colores.Count == 0)
            throw new ArgumentException("Debe especificar al menos un color para generar las variantes.", nameof(dto.Colores));

        // Normalización del código de estilo (ej. "  zap air 01 " -> "ZAP-AIR-01")
        var codigoEstiloLimpio = LimpiarIdentificador(dto.CodigoEstilo);

        // Verificar que no exista otro artículo con el mismo código de estilo
        var existente = await _unitOfWork.Articulos.FirstOrDefaultAsync(a => a.CodigoEstilo == codigoEstiloLimpio, cancellationToken);
        if (existente != null)
        {
            throw new DomainException($"Ya existe un artículo con el código de estilo '{codigoEstiloLimpio}'.");
        }

        var articulo = new Articulo
        {
            CodigoEstilo = codigoEstiloLimpio,
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Temporada = dto.Temporada?.Trim(),
            Genero = dto.Genero?.Trim(),
            CategoriaId = dto.CategoriaId,
            MarcaId = dto.MarcaId
        };

        var movimientosIniciales = new List<MovimientoStock>();

        // Generación del Producto Cartesiano: Talles × Colores
        foreach (var talle in dto.Talles.Select(t => t.Trim().ToUpperInvariant()).Distinct())
        {
            if (string.IsNullOrWhiteSpace(talle)) continue;

            foreach (var color in dto.Colores.Select(c => c.Trim()).Distinct())
            {
                if (string.IsNullOrWhiteSpace(color)) continue;

                var colorSlug = GenerarSlugColor(color);
                var skuGenerado = $"{codigoEstiloLimpio}-{talle}-{colorSlug}";

                // Determinación del stock inicial
                var stockInicial = dto.StockInicialDefecto;
                if (dto.StockInicialPorCombinacion != null)
                {
                    // Buscar coincidencia exacta o insensible a mayúsculas
                    var keyEncontrada = dto.StockInicialPorCombinacion.Keys
                        .FirstOrDefault(k => string.Equals(k.Talle, talle, StringComparison.OrdinalIgnoreCase) &&
                                             string.Equals(k.Color, color, StringComparison.OrdinalIgnoreCase));

                    if (keyEncontrada != default)
                    {
                        stockInicial = dto.StockInicialPorCombinacion[keyEncontrada];
                    }
                    else if (dto.SoloCombinacionesEspecificadas)
                    {
                        // Se especificaron combinaciones exactas y esta no fue incluida: omitir creación
                        continue;
                    }
                }

                if (stockInicial < 0) stockInicial = 0;

                var variante = new VarianteArticulo
                {
                    Articulo = articulo,
                    ArticuloId = articulo.Id,
                    SKU = skuGenerado,
                    Talle = talle,
                    Color = color,
                    PrecioCosto = dto.PrecioCosto,
                    PrecioLista = dto.PrecioLista,
                    PrecioOferta = dto.PrecioOferta,
                    PermiteDescuentoMedioPago = dto.PermiteDescuentoMedioPago,
                    StockActual = stockInicial,
                    StockMinimo = dto.StockMinimo
                };

                articulo.Variantes.Add(variante);

                // Si se cargó stock inicial > 0, se genera automáticamente el movimiento de auditoría
                if (stockInicial > 0)
                {
                    movimientosIniciales.Add(new MovimientoStock
                    {
                        Variante = variante,
                        VarianteId = variante.Id,
                        Tipo = TipoMovimientoStock.EntradaCompra,
                        Cantidad = stockInicial,
                        StockPrevio = 0,
                        StockResultante = stockInicial,
                        CostoUnitario = dto.PrecioCosto,
                        Motivo = "Carga inicial de inventario",
                        Fecha = DateTime.UtcNow
                    });
                }
            }
        }

        await _unitOfWork.Articulos.AddAsync(articulo, cancellationToken);

        if (movimientosIniciales.Count > 0)
        {
            await _unitOfWork.MovimientosStock.AddRangeAsync(movimientosIniciales, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return articulo;
    }

    public async Task<VarianteArticulo> ActualizarPreciosVarianteAsync(
        Guid varianteId,
        decimal precioCosto,
        decimal precioLista,
        decimal? precioOferta,
        bool permiteDescuentoMedioPago,
        CancellationToken cancellationToken = default)
    {
        var variante = await _unitOfWork.Variantes.GetByIdAsync(varianteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la variante con Id '{varianteId}'.");

        if (precioCosto < 0) throw new ArgumentException("El costo no puede ser negativo.", nameof(precioCosto));
        if (precioLista <= 0) throw new ArgumentException("El precio de lista debe ser mayor a cero.", nameof(precioLista));

        variante.PrecioCosto = precioCosto;
        variante.PrecioLista = precioLista;
        variante.PrecioOferta = precioOferta;
        variante.PermiteDescuentoMedioPago = permiteDescuentoMedioPago;

        _unitOfWork.Variantes.Update(variante);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variante;
    }

    public async Task<MovimientoStock> AjustarStockAsync(AjustarStockDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Cantidad == 0)
            throw new ArgumentException("La cantidad a ajustar no puede ser cero.", nameof(dto.Cantidad));

        var variante = await _unitOfWork.Variantes.GetByIdAsync(dto.VarianteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la variante con Id '{dto.VarianteId}'.");

        var stockPrevio = variante.StockActual;
        var stockResultante = stockPrevio + dto.Cantidad;

        if (stockResultante < 0)
        {
            throw new StockInsuficienteException(variante.SKU, stockPrevio, Math.Abs(dto.Cantidad));
        }

        variante.StockActual = stockResultante;
        _unitOfWork.Variantes.Update(variante);

        var movimiento = new MovimientoStock
        {
            VarianteId = variante.Id,
            Tipo = dto.Tipo,
            Cantidad = dto.Cantidad,
            StockPrevio = stockPrevio,
            StockResultante = stockResultante,
            CostoUnitario = variante.PrecioCosto,
            Motivo = dto.Motivo?.Trim(),
            ReferenciaDocumento = dto.ReferenciaDocumento?.Trim(),
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.MovimientosStock.AddAsync(movimiento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movimiento;
    }

    public async Task<IReadOnlyList<VarianteArticuloDto>> BuscarVariantesAsync(string termino, CancellationToken cancellationToken = default)
    {
        var variantes = (await _unitOfWork.Variantes.GetAllAsync(cancellationToken))
            .Where(v => v.Activo && (v.Articulo == null || v.Articulo.Activo));

        if (string.IsNullOrWhiteSpace(termino))
        {
            return variantes.Select(MapToDto).ToList();
        }

        var tokens = termino.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var resultados = variantes.Where(v =>
        {
            var textoBusqueda = $"{v.SKU} {v.CodigoBarras} {v.Articulo?.Nombre} {v.Articulo?.CodigoEstilo} {v.Articulo?.Marca?.Nombre} {v.Articulo?.Categoria?.Nombre} {v.Talle} {v.Color}";
            return tokens.All(t => CoincideToken(textoBusqueda, t));
        }).ToList();

        return resultados.Select(MapToDto).ToList();
    }

    public async Task<VarianteArticuloDto?> ObtenerVariantePorCodigoBarrasOSkuAsync(string codigo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;

        var codigoLimpio = codigo.Trim();

        var variante = await _unitOfWork.Variantes.FirstOrDefaultAsync(v =>
            v.Activo && (v.CodigoBarras == codigoLimpio || v.SKU == codigoLimpio),
            cancellationToken);

        return variante != null ? MapToDto(variante) : null;
    }

    public async Task<IReadOnlyList<VarianteArticuloDto>> ObtenerVariantesPorArticuloAsync(Guid articuloId, CancellationToken cancellationToken = default)
    {
        var variantes = await _unitOfWork.Variantes.FindAsync(v => v.ArticuloId == articuloId && v.Activo, cancellationToken);
        return variantes.Select(MapToDto).ToList();
    }

    public async Task<VarianteArticulo> ActualizarVarianteAsync(ActualizarVarianteDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var variante = await _unitOfWork.Variantes.GetByIdAsync(dto.VarianteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la variante con Id '{dto.VarianteId}'.");

        if (string.IsNullOrWhiteSpace(dto.SKU))
            throw new ArgumentException("El SKU es obligatorio.", nameof(dto.SKU));

        if (dto.PrecioCosto < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(dto.PrecioCosto));

        if (dto.PrecioLista <= 0)
            throw new ArgumentException("El precio de lista debe ser mayor a cero.", nameof(dto.PrecioLista));

        // Validar unicidad de SKU
        var skuLimpio = dto.SKU.Trim().ToUpperInvariant();
        var existenteSku = await _unitOfWork.Variantes.FirstOrDefaultAsync(v => v.Id != dto.VarianteId && v.SKU.ToUpper() == skuLimpio, cancellationToken);
        if (existenteSku != null)
        {
            throw new DomainException($"Ya existe otra variante con el SKU '{skuLimpio}'.");
        }

        // Validar unicidad de código de barras
        var codBarrasLimpio = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim();
        if (codBarrasLimpio != null)
        {
            var existenteBarra = await _unitOfWork.Variantes.FirstOrDefaultAsync(v => v.Id != dto.VarianteId && v.CodigoBarras == codBarrasLimpio, cancellationToken);
            if (existenteBarra != null)
            {
                throw new DomainException($"Ya existe otra variante con el código de barras '{codBarrasLimpio}'.");
            }
        }

        variante.SKU = skuLimpio;
        variante.CodigoBarras = codBarrasLimpio;
        variante.Talle = dto.Talle?.Trim().ToUpperInvariant() ?? variante.Talle;
        variante.Color = dto.Color?.Trim() ?? variante.Color;
        variante.PrecioCosto = dto.PrecioCosto;
        variante.PrecioLista = dto.PrecioLista;
        variante.PrecioOferta = dto.PrecioOferta;
        variante.PermiteDescuentoMedioPago = dto.PermiteDescuentoMedioPago;
        variante.StockMinimo = Math.Max(0, dto.StockMinimo);
        variante.Ubicacion = dto.Ubicacion?.Trim();

        if (!string.IsNullOrWhiteSpace(dto.NombreArticulo) && variante.Articulo != null)
        {
            variante.Articulo.Nombre = dto.NombreArticulo.Trim();
            _unitOfWork.Articulos.Update(variante.Articulo);
        }

        _unitOfWork.Variantes.Update(variante);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variante;
    }

    public async Task<int> ActualizarPreciosMasivosAsync(ActualizarPreciosMasivosDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var articulo = await _unitOfWork.Articulos.GetByIdAsync(dto.ArticuloId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el artículo con Id '{dto.ArticuloId}'.");

        var variantes = await _unitOfWork.Variantes.FindAsync(v => v.ArticuloId == dto.ArticuloId && v.Activo, cancellationToken);
        if (variantes.Count == 0) return 0;

        foreach (var v in variantes)
        {
            // 1. Costo
            if (dto.NuevoPrecioCosto.HasValue && dto.NuevoPrecioCosto.Value >= 0)
            {
                v.PrecioCosto = dto.NuevoPrecioCosto.Value;
            }
            else if (dto.PorcentajeAumentoCosto.HasValue && dto.PorcentajeAumentoCosto.Value != 0)
            {
                var factor = 1m + (dto.PorcentajeAumentoCosto.Value / 100m);
                v.PrecioCosto = Math.Round(v.PrecioCosto * factor, 2);
            }

            // 2. Precio de Lista
            if (dto.NuevoPrecioLista.HasValue && dto.NuevoPrecioLista.Value > 0)
            {
                v.PrecioLista = dto.NuevoPrecioLista.Value;
            }
            else if (dto.PorcentajeAumentoLista.HasValue && dto.PorcentajeAumentoLista.Value != 0)
            {
                var factor = 1m + (dto.PorcentajeAumentoLista.Value / 100m);
                v.PrecioLista = Math.Round(v.PrecioLista * factor, 2);
            }

            // 3. Oferta
            if (dto.NuevoPrecioOferta.HasValue)
            {
                v.PrecioOferta = dto.NuevoPrecioOferta.Value > 0 ? dto.NuevoPrecioOferta.Value : null;
            }

            // 4. Bandera descuento medio de pago
            if (dto.PermiteDescuentoMedioPago.HasValue)
            {
                v.PermiteDescuentoMedioPago = dto.PermiteDescuentoMedioPago.Value;
            }

            _unitOfWork.Variantes.Update(v);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return variantes.Count;
    }

    public async Task<Categoria> CrearCategoriaAsync(string nombre, string? descripcion = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría es obligatorio.", nameof(nombre));

        var nombreLimpio = nombre.Trim();
        var existente = await _unitOfWork.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToUpper() == nombreLimpio.ToUpper(), cancellationToken);
        if (existente != null)
        {
            return existente;
        }

        var nueva = new Categoria
        {
            Nombre = nombreLimpio,
            Descripcion = descripcion?.Trim()
        };

        await _unitOfWork.Categorias.AddAsync(nueva, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return nueva;
    }

    public async Task<Marca> CrearMarcaAsync(string nombre, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la marca es obligatorio.", nameof(nombre));

        var nombreLimpio = nombre.Trim();
        var existente = await _unitOfWork.Marcas.FirstOrDefaultAsync(m => m.Nombre.ToUpper() == nombreLimpio.ToUpper(), cancellationToken);
        if (existente != null)
        {
            return existente;
        }

        var nueva = new Marca
        {
            Nombre = nombreLimpio
        };

        await _unitOfWork.Marcas.AddAsync(nueva, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return nueva;
    }

    public async Task<bool> EliminarVarianteAsync(Guid varianteId, CancellationToken cancellationToken = default)
    {
        var variante = await _unitOfWork.Variantes.GetByIdAsync(varianteId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la variante con Id '{varianteId}'.");

        var tieneVentas = await _unitOfWork.LineasVenta.FirstOrDefaultAsync(lv => lv.VarianteId == varianteId, cancellationToken) != null;

        if (tieneVentas)
        {
            // Baja lógica para preservar el histórico contable
            variante.Activo = false;
            _unitOfWork.Variantes.Update(variante);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false; // Soft deleted
        }
        else
        {
            // Eliminación física: borrar movimientos de stock y la variante
            var movimientos = await _unitOfWork.MovimientosStock.FindAsync(m => m.VarianteId == varianteId, cancellationToken);
            foreach (var m in movimientos)
            {
                _unitOfWork.MovimientosStock.Remove(m);
            }

            _unitOfWork.Variantes.Remove(variante);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Si el artículo padre no tiene más variantes, eliminarlo o desactivarlo
            var articuloId = variante.ArticuloId;
            var restantes = await _unitOfWork.Variantes.FindAsync(v => v.ArticuloId == articuloId, cancellationToken);
            if (restantes.Count == 0)
            {
                var articulo = await _unitOfWork.Articulos.GetByIdAsync(articuloId, cancellationToken);
                if (articulo != null)
                {
                    _unitOfWork.Articulos.Remove(articulo);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            return true; // Hard deleted
        }
    }

    public async Task<int> EliminarArticuloCompletoAsync(Guid articuloId, CancellationToken cancellationToken = default)
    {
        var articulo = await _unitOfWork.Articulos.GetByIdAsync(articuloId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el artículo con Id '{articuloId}'.");

        var variantes = await _unitOfWork.Variantes.FindAsync(v => v.ArticuloId == articuloId, cancellationToken);
        int eliminadas = 0;
        bool algunaConVentas = false;

        foreach (var v in variantes)
        {
            var tieneVentas = await _unitOfWork.LineasVenta.FirstOrDefaultAsync(lv => lv.VarianteId == v.Id, cancellationToken) != null;
            if (tieneVentas)
            {
                algunaConVentas = true;
                v.Activo = false;
                _unitOfWork.Variantes.Update(v);
            }
            else
            {
                var movimientos = await _unitOfWork.MovimientosStock.FindAsync(m => m.VarianteId == v.Id, cancellationToken);
                foreach (var m in movimientos)
                {
                    _unitOfWork.MovimientosStock.Remove(m);
                }
                _unitOfWork.Variantes.Remove(v);
            }
            eliminadas++;
        }

        if (algunaConVentas)
        {
            articulo.Activo = false;
            _unitOfWork.Articulos.Update(articulo);
        }
        else
        {
            _unitOfWork.Articulos.Remove(articulo);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return eliminadas;
    }

    // --- Helpers de Mapeo y Slug ---

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

    private static string LimpiarIdentificador(string input)
    {
        var sinEspacios = Regex.Replace(input.Trim(), @"\s+", "-");
        return Regex.Replace(sinEspacios, @"[^a-zA-Z0-9\-_]", "").ToUpperInvariant();
    }

    private static string GenerarSlugColor(string color)
    {
        var limpio = LimpiarIdentificador(color);
        // Si el color es largo (ej. AZULMARINO), tomamos hasta 6 caracteres para mantener el SKU legible
        return limpio.Length > 6 ? limpio[..6] : limpio;
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
