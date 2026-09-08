namespace PuntoDeVenta.Application.DTOs.Configuracion;

public class ConfiguracionNegocioDto
{
    public Guid Id { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string VendedoraDefecto { get; set; } = string.Empty;
    public string? LogoRuta { get; set; }

    public decimal PorcentajeDescuentoEfectivo { get; set; }
    public decimal ComisionTarjetaDebito { get; set; }
    public decimal ComisionTarjetaCredito { get; set; }
    public decimal RecargoCuotasTarjetaCredito { get; set; }

    // Cuotas Diferenciadas (3, 6, 9, 12 cuotas)
    public bool Habilitar3Cuotas { get; set; } = true;
    public decimal Recargo3Cuotas { get; set; } = 15.0m;

    public bool Habilitar6Cuotas { get; set; } = true;
    public decimal Recargo6Cuotas { get; set; } = 25.0m;

    public bool Habilitar9Cuotas { get; set; } = false;
    public decimal Recargo9Cuotas { get; set; } = 35.0m;

    public bool Habilitar12Cuotas { get; set; } = false;
    public decimal Recargo12Cuotas { get; set; } = 45.0m;

    public bool CuotasIncluidasEnPrecioLista { get; set; } = false;

    public decimal MargenGananciaSugerido { get; set; }
    public decimal CostosBancariosEstimados { get; set; }

    public decimal TopeFiadoDefecto { get; set; }
    public decimal TopeMensualRetiroDueño { get; set; }

    public string TemaInterfaz { get; set; } = "Dark";

    public string GitHubRepoOwner { get; set; } = "mrrichar11";
    public string GitHubRepoName { get; set; } = "puntoVentaMR";
}

public class EstadoRetirosDueñoDto
{
    public decimal TopeMensual { get; set; }
    public decimal TotalRetiradoMes { get; set; }
    public decimal SaldoDisponible => Math.Max(0m, TopeMensual - TotalRetiradoMes);
    public decimal PorcentajeConsumido => TopeMensual > 0 ? Math.Min(100m, Math.Round((TotalRetiradoMes / TopeMensual) * 100m, 1)) : 0m;
    public bool HaSuperadoTope => TotalRetiradoMes > TopeMensual;
}
