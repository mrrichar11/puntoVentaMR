using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Inventario;

namespace PuntoDeVenta.Application.Services;

public interface IInventarioService
{
    /// <summary>
    /// Crea el artículo base y genera automáticamente la matriz combinatoria de variantes (Talles × Colores),
    /// asignando SKUs normalizados y registrando movimientos de stock inicial si corresponde.
    /// </summary>
    Task<Articulo> CrearArticuloConMatrizAsync(CrearArticuloDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza los precios, oferta y bandera de acumulación de descuento para una variante específica.
    /// </summary>
    Task<VarianteArticulo> ActualizarPreciosVarianteAsync(
        Guid varianteId,
        decimal precioCosto,
        decimal precioLista,
        decimal? precioOferta,
        bool permiteDescuentoMedioPago,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un ingreso o egreso de stock con trazabilidad inmutable en MovimientoStock.
    /// </summary>
    Task<MovimientoStock> AjustarStockAsync(AjustarStockDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Búsqueda rápida para el Terminal de Venta (TPV) o gestión por nombre, código de estilo, marca o categoría.
    /// </summary>
    Task<IReadOnlyList<VarianteArticuloDto>> BuscarVariantesAsync(string termino, CancellationToken cancellationToken = default);

    /// <summary>
    /// Búsqueda por lectura directa de lector láser de código de barras o ingreso de SKU.
    /// </summary>
    Task<VarianteArticuloDto?> ObtenerVariantePorCodigoBarrasOSkuAsync(string codigo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las variantes activas de un artículo padre.
    /// </summary>
    Task<IReadOnlyList<VarianteArticuloDto>> ObtenerVariantesPorArticuloAsync(Guid articuloId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permite editar datos de una variante existente (SKU, código de barras, talles, colores, precios, stock mínimo).
    /// </summary>
    Task<VarianteArticulo> ActualizarVarianteAsync(ActualizarVarianteDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica actualización masiva de precios a todas las variantes pertenecientes a un artículo (precios fijos o porcentaje).
    /// </summary>
    Task<int> ActualizarPreciosMasivosAsync(ActualizarPreciosMasivosDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Da de alta una nueva categoría de productos.
    /// </summary>
    Task<Categoria> CrearCategoriaAsync(string nombre, string? descripcion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Da de alta una nueva marca.
    /// </summary>
    Task<Marca> CrearMarcaAsync(string nombre, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una variante individual. Si no posee historial de ventas, se purga físicamente;
    /// si posee ventas registradas, se da de baja lógica para proteger el histórico contable.
    /// </summary>
    Task<bool> EliminarVarianteAsync(Guid varianteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina el artículo base y todas sus variantes asociadas.
    /// </summary>
    Task<int> EliminarArticuloCompletoAsync(Guid articuloId, CancellationToken cancellationToken = default);
}
