namespace PuntoDeVenta.Domain.Exceptions;

public class StockInsuficienteException : DomainException
{
    public string SKU { get; }
    public int StockDisponible { get; }
    public int CantidadSolicitada { get; }

    public StockInsuficienteException(string sku, int stockDisponible, int cantidadSolicitada)
        : base($"Stock insuficiente para el artículo SKU '{sku}'. Disponible: {stockDisponible}, Solicitado: {cantidadSolicitada}.")
    {
        SKU = sku;
        StockDisponible = stockDisponible;
        CantidadSolicitada = cantidadSolicitada;
    }
}
