using PuntoDeVenta.Application.DTOs.Asistente;

namespace PuntoDeVenta.Application.Services;

public interface IGeminiService
{
    /// <summary>
    /// Indica si el servicio de Gemini cuenta con una clave activa para operar.
    /// </summary>
    bool EstaDisponible { get; }

    /// <summary>
    /// Envía la entrada del usuario a la API de Google Gemini (gemini-1.5-flash)
    /// y devuelve los borradores de productos interpretados en formato estructurado.
    /// Devuelve null si no hay conexión, hay timeout o la API no está configurada.
    /// </summary>
    Task<List<BorradorCargaArticuloDto>?> InterpretarEntradaConGeminiAsync(
        string entradaUsuario,
        List<BorradorCargaArticuloDto>? borradoresPrevios = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Interpreta de forma universal la intención del usuario con Gemini:
    /// ya sea carga de indumentaria, registro de deuda de clienta (libreta),
    /// factura de proveedor, nuevo contacto o consulta general.
    /// </summary>
    Task<RespuestaUniversalAsistenteDto?> InterpretarUniversalConGeminiAsync(
        string entradaUsuario,
        List<BorradorCargaArticuloDto>? borradoresPrevios = null,
        CancellationToken cancellationToken = default);
}
