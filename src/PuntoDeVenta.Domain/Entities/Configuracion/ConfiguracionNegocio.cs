using PuntoDeVenta.Domain.Common;

namespace PuntoDeVenta.Domain.Entities.Configuracion;

/// <summary>
/// Parámetros globales de la tienda, políticas comerciales, topes de retiros y preferencias de interfaz.
/// </summary>
public class ConfiguracionNegocio : BaseEntity
{
    public string NombreComercio { get; set; } = "Mi Tienda & Zapatería";
    public string Direccion { get; set; } = "Av. Principal 123";
    public string Telefono { get; set; } = "+54 9 11 0000-0000";
    public string Cuit { get; set; } = "20-00000000-0";
    public string VendedoraDefecto { get; set; } = "Cajera Mostrador";
    public string? LogoRuta { get; set; }

    // Políticas Financieras y Sugerencia de Precios
    public decimal PorcentajeDescuentoEfectivo { get; set; } = 10.0m;
    public decimal ComisionTarjetaDebito { get; set; } = 1.5m;
    public decimal ComisionTarjetaCredito { get; set; } = 4.5m;
    public decimal RecargoCuotasTarjetaCredito { get; set; } = 15.0m;
    public decimal MargenGananciaSugerido { get; set; } = 80.0m;
    public decimal CostosBancariosEstimados { get; set; } = 5.0m;

    // Cuotas Diferenciadas (3, 6, 9 y 12 cuotas)
    public bool Habilitar3Cuotas { get; set; } = true;
    public decimal Recargo3Cuotas { get; set; } = 15.0m;

    public bool Habilitar6Cuotas { get; set; } = true;
    public decimal Recargo6Cuotas { get; set; } = 25.0m;

    public bool Habilitar9Cuotas { get; set; } = false;
    public decimal Recargo9Cuotas { get; set; } = 35.0m;

    public bool Habilitar12Cuotas { get; set; } = false;
    public decimal Recargo12Cuotas { get; set; } = 45.0m;

    /// <summary>
    /// Si es true, el Precio de Lista ya contempla la financiación (cuotas sin interés sobre precio de lista).
    /// Si es false, se calcula el tercer precio (precio financiado con recargo de cuotas sobre precio de lista).
    /// </summary>
    public bool CuotasIncluidasEnPrecioLista { get; set; } = false;
    
    // Topes y Límites
    public decimal TopeFiadoDefecto { get; set; } = 50000m;
    public decimal TopeMensualRetiroDueño { get; set; } = 600000m;

    // Interfaz
    public string TemaInterfaz { get; set; } = "Light";

    // Configuración de Actualizaciones Automáticas (GitHub Releases)
    public string GitHubRepoOwner { get; set; } = "mrrichar11";
    public string GitHubRepoName { get; set; } = "puntoVentaMR";
}
