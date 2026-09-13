using PuntoDeVenta.Application.DTOs.Inventario;
using PuntoDeVenta.Application.DTOs.Ventas;

namespace PuntoDeVenta.Application.Services;

public interface IVentaService
{
    /// <summary>
    /// Búsqueda de alta velocidad para el mostrador:
    /// 1. Si coincide con un código de barras o SKU exacto (lector láser), retorna inmediatamente.
    /// 2. Si es texto manual, busca por modelo, talle, color, marca o descripción.
    /// </summary>
    Task<IReadOnlyList<VarianteArticuloDto>> BuscarItemRapidoAsync(string inputEscaneoOManual, CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa la venta atómica: valida turno de caja, descuenta stock de variantes regulares,
    /// procesa ítems de venta manual sin stock, congela costos históricos e impacta la caja en sus respectivos canales.
    /// </summary>
    Task<VentaRealizadaDto> ProcesarVentaAsync(RegistrarVentaDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permite actualizar el costo histórico de una línea de venta realizada con anterioridad
    /// (ej. venta manual donde no se tenía el costo a mano al momento del cobro).
    /// </summary>
    Task<bool> ActualizarCostoLineaVentaAsync(Guid lineaVentaId, decimal nuevoCosto, CancellationToken cancellationToken = default);
}
