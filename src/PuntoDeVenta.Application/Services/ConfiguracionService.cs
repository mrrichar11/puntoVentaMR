using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Configuracion;
using PuntoDeVenta.Domain.Entities.Configuracion;
using PuntoDeVenta.Domain.Entities.Finanzas;

namespace PuntoDeVenta.Application.Services;

public class ConfiguracionService : IConfiguracionService
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfiguracionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ConfiguracionNegocioDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _unitOfWork.Configuraciones.GetAllAsync(cancellationToken);
        var config = configs.FirstOrDefault();

        if (config == null)
        {
            config = new ConfiguracionNegocio
            {
                NombreComercio = "Mi Tienda & Zapatería",
                Direccion = "Av. Comercial 123",
                Telefono = "+54 9 11 0000-0000",
                Cuit = "20-00000000-0",
                VendedoraDefecto = "Cajera Mostrador",
                LogoRuta = null,
                PorcentajeDescuentoEfectivo = 10.0m,
                ComisionTarjetaDebito = 1.5m,
                ComisionTarjetaCredito = 4.5m,
                RecargoCuotasTarjetaCredito = 15.0m,
                Habilitar3Cuotas = true,
                Recargo3Cuotas = 15.0m,
                Habilitar6Cuotas = true,
                Recargo6Cuotas = 25.0m,
                Habilitar9Cuotas = false,
                Recargo9Cuotas = 35.0m,
                Habilitar12Cuotas = false,
                Recargo12Cuotas = 45.0m,
                CuotasIncluidasEnPrecioLista = false,
                MargenGananciaSugerido = 80.0m,
                CostosBancariosEstimados = 5.0m,
                TopeFiadoDefecto = 50000m,
                TopeMensualRetiroDueño = 600000m,
                TemaInterfaz = "Dark"
            };

            await _unitOfWork.Configuraciones.AddAsync(config, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(config);
    }

    public async Task GuardarConfiguracionAsync(ConfiguracionNegocioDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var configs = await _unitOfWork.Configuraciones.GetAllAsync(cancellationToken);
        var config = configs.FirstOrDefault();

        bool esNuevo = false;
        if (config == null)
        {
            config = new ConfiguracionNegocio();
            esNuevo = true;
        }

        config.NombreComercio = string.IsNullOrWhiteSpace(dto.NombreComercio) ? "Mi Tienda & Zapatería" : dto.NombreComercio.Trim();
        config.Direccion = dto.Direccion?.Trim() ?? string.Empty;
        config.Telefono = dto.Telefono?.Trim() ?? string.Empty;
        config.Cuit = dto.Cuit?.Trim() ?? string.Empty;
        config.VendedoraDefecto = dto.VendedoraDefecto?.Trim() ?? string.Empty;
        config.LogoRuta = string.IsNullOrWhiteSpace(dto.LogoRuta) ? null : dto.LogoRuta.Trim();

        config.PorcentajeDescuentoEfectivo = Math.Max(0m, dto.PorcentajeDescuentoEfectivo);
        config.ComisionTarjetaDebito = Math.Max(0m, dto.ComisionTarjetaDebito);
        config.ComisionTarjetaCredito = Math.Max(0m, dto.ComisionTarjetaCredito);
        config.RecargoCuotasTarjetaCredito = Math.Max(0m, dto.RecargoCuotasTarjetaCredito);

        config.Habilitar3Cuotas = dto.Habilitar3Cuotas;
        config.Recargo3Cuotas = Math.Max(0m, dto.Recargo3Cuotas);
        config.Habilitar6Cuotas = dto.Habilitar6Cuotas;
        config.Recargo6Cuotas = Math.Max(0m, dto.Recargo6Cuotas);
        config.Habilitar9Cuotas = dto.Habilitar9Cuotas;
        config.Recargo9Cuotas = Math.Max(0m, dto.Recargo9Cuotas);
        config.Habilitar12Cuotas = dto.Habilitar12Cuotas;
        config.Recargo12Cuotas = Math.Max(0m, dto.Recargo12Cuotas);
        config.CuotasIncluidasEnPrecioLista = dto.CuotasIncluidasEnPrecioLista;

        config.MargenGananciaSugerido = Math.Max(0m, dto.MargenGananciaSugerido);
        config.CostosBancariosEstimados = Math.Max(0m, dto.CostosBancariosEstimados);

        config.TopeFiadoDefecto = Math.Max(0m, dto.TopeFiadoDefecto);
        config.TopeMensualRetiroDueño = Math.Max(0m, dto.TopeMensualRetiroDueño);
        config.TemaInterfaz = string.IsNullOrWhiteSpace(dto.TemaInterfaz) ? "Dark" : dto.TemaInterfaz.Trim();
        config.GitHubRepoOwner = string.IsNullOrWhiteSpace(dto.GitHubRepoOwner) ? "Fliac" : dto.GitHubRepoOwner.Trim();
        config.GitHubRepoName = string.IsNullOrWhiteSpace(dto.GitHubRepoName) ? "PuntoDeVenta" : dto.GitHubRepoName.Trim();

        if (esNuevo)
        {
            await _unitOfWork.Configuraciones.AddAsync(config, cancellationToken);
        }
        else
        {
            _unitOfWork.Configuraciones.Update(config);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<EstadoRetirosDueñoDto> ObtenerEstadoRetirosDueñoMesAsync(CancellationToken cancellationToken = default)
    {
        var configDto = await ObtenerConfiguracionAsync(cancellationToken);

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMes = inicioMes.AddMonths(1);

        var gastos = await _unitOfWork.Gastos.FindAsync(g =>
            g.Activo &&
            g.Fecha >= inicioMes &&
            g.Fecha < finMes &&
            g.EsPersonal,
            cancellationToken);

        var totalRetirado = gastos.Sum(g => g.Monto);

        return new EstadoRetirosDueñoDto
        {
            TopeMensual = configDto.TopeMensualRetiroDueño,
            TotalRetiradoMes = totalRetirado
        };
    }

    private static ConfiguracionNegocioDto MapToDto(ConfiguracionNegocio c)
    {
        return new ConfiguracionNegocioDto
        {
            Id = c.Id,
            NombreComercio = c.NombreComercio,
            Direccion = c.Direccion,
            Telefono = c.Telefono,
            Cuit = c.Cuit,
            VendedoraDefecto = c.VendedoraDefecto,
            LogoRuta = c.LogoRuta,
            PorcentajeDescuentoEfectivo = c.PorcentajeDescuentoEfectivo,
            ComisionTarjetaDebito = c.ComisionTarjetaDebito,
            ComisionTarjetaCredito = c.ComisionTarjetaCredito,
            RecargoCuotasTarjetaCredito = c.RecargoCuotasTarjetaCredito,
            Habilitar3Cuotas = c.Habilitar3Cuotas,
            Recargo3Cuotas = c.Recargo3Cuotas,
            Habilitar6Cuotas = c.Habilitar6Cuotas,
            Recargo6Cuotas = c.Recargo6Cuotas,
            Habilitar9Cuotas = c.Habilitar9Cuotas,
            Recargo9Cuotas = c.Recargo9Cuotas,
            Habilitar12Cuotas = c.Habilitar12Cuotas,
            Recargo12Cuotas = c.Recargo12Cuotas,
            CuotasIncluidasEnPrecioLista = c.CuotasIncluidasEnPrecioLista,
            MargenGananciaSugerido = c.MargenGananciaSugerido,
            CostosBancariosEstimados = c.CostosBancariosEstimados,
            TopeFiadoDefecto = c.TopeFiadoDefecto,
            TopeMensualRetiroDueño = c.TopeMensualRetiroDueño,
            TemaInterfaz = c.TemaInterfaz,
            GitHubRepoOwner = c.GitHubRepoOwner,
            GitHubRepoName = c.GitHubRepoName
        };
    }
}
