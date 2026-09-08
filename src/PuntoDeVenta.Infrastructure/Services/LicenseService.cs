using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Application.DTOs.Seguridad;
using PuntoDeVenta.Application.Services;
using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    // Clave secreta maestra para firma criptográfica offline (HMAC-SHA256)
    private static readonly byte[] MasterSecretKey = Encoding.UTF8.GetBytes("PuntoDeVenta-Retail-Master-Secret-Licensing-Key-2026-Fliac#Secured!");

    private readonly IUnitOfWork _unitOfWork;

    public LicenseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EstadoLicenciaDto> ValidarLicenciaAsync(CancellationToken cancellationToken = default)
    {
        var licencia = await ObtenerOCrearLicenciaAsync(cancellationToken);

        var ahoraUtc = DateTime.UtcNow;

        // 1. Verificación de adulteración de reloj del sistema
        // Si el reloj de la máquina marca un tiempo anterior al último uso registrado (con tolerancia de 5 min)
        if (ahoraUtc < licencia.UltimaFechaUso.AddMinutes(-5))
        {
            return new EstadoLicenciaDto
            {
                CodigoInstalacion = licencia.CodigoInstalacion,
                Comercio = licencia.Comercio,
                FechaExpiracion = licencia.FechaExpiracion,
                DiasRestantes = 0,
                EsValida = false,
                EstaPorVencer = false,
                RelojAdulterado = true,
                MensajeEstado = "Se detectó que el reloj del equipo fue retrasado. Corrija la fecha y hora de Windows para continuar."
            };
        }

        // Actualizar último uso registrado
        if (ahoraUtc > licencia.UltimaFechaUso)
        {
            licencia.UltimaFechaUso = ahoraUtc;
            _unitOfWork.Licencias.Update(licencia);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 2. Validar firma criptográfica de la clave actual según el prefijo firmado
        var planVerificacion = licencia.TipoPlan;
        if (licencia.ClaveActivacion.StartsWith("STD-", StringComparison.OrdinalIgnoreCase))
        {
            planVerificacion = TipoPlanLicencia.Estandar;
        }
        else if (licencia.ClaveActivacion.StartsWith("PRM-", StringComparison.OrdinalIgnoreCase))
        {
            planVerificacion = TipoPlanLicencia.Premium;
        }

        var claveEsperada = GenerarClaveActivacion(licencia.CodigoInstalacion, licencia.FechaExpiracion, planVerificacion);
        bool firmaValida = string.Equals(licencia.ClaveActivacion, claveEsperada, StringComparison.OrdinalIgnoreCase);

        // Soporte retrocompatible para claves legacy tipo ACT-
        if (!firmaValida && licencia.ClaveActivacion.StartsWith("ACT-", StringComparison.OrdinalIgnoreCase))
        {
            var claveLegacy = GenerarClaveLegacy(licencia.CodigoInstalacion, licencia.FechaExpiracion);
            firmaValida = string.Equals(licencia.ClaveActivacion, claveLegacy, StringComparison.OrdinalIgnoreCase);
            if (firmaValida)
            {
                planVerificacion = TipoPlanLicencia.Premium;
            }
        }

        if (firmaValida && licencia.TipoPlan != planVerificacion)
        {
            licencia.TipoPlan = planVerificacion;
        }

        // 3. Validar expiración
        bool noExpirada = ahoraUtc <= licencia.FechaExpiracion;
        bool esValida = licencia.EstaActiva && firmaValida && noExpirada;

        var diasRestantes = Math.Max(0, (int)Math.Ceiling((licencia.FechaExpiracion - ahoraUtc).TotalDays));
        bool estaPorVencer = esValida && diasRestantes <= 5;

        string mensaje;
        if (!firmaValida)
        {
            mensaje = "La clave de activación almacenada no es válida o fue modificada.";
            esValida = false;
        }
        else if (!noExpirada)
        {
            mensaje = $"Tu licencia mensual venció el {licencia.FechaExpiracion.ToLocalTime():dd/MM/yyyy}. Ingresa una nueva clave de renovación.";
        }
        else if (estaPorVencer)
        {
            mensaje = $"⚠️ Atención: Tu licencia vencerá en {diasRestantes} {(diasRestantes == 1 ? "día" : "días")} (el {licencia.FechaExpiracion.ToLocalTime():dd/MM/yyyy}). Solicita tu renovación mensual.";
        }
        else
        {
            string detallePlan = licencia.TieneModuloIA ? "Premium (con IA)" : "Estándar";
            mensaje = $"Licencia {detallePlan} activa hasta el {licencia.FechaExpiracion.ToLocalTime():dd/MM/yyyy} ({diasRestantes} días restantes).";
        }

        return new EstadoLicenciaDto
        {
            CodigoInstalacion = licencia.CodigoInstalacion,
            Comercio = licencia.Comercio,
            TipoPlan = licencia.TipoPlan,
            TieneModuloIA = esValida && licencia.TieneModuloIA,
            FechaExpiracion = licencia.FechaExpiracion,
            DiasRestantes = diasRestantes,
            EsValida = esValida,
            EstaPorVencer = estaPorVencer,
            RelojAdulterado = false,
            MensajeEstado = mensaje
        };
    }

    public async Task<ResultadoActivacionDto> ActivarLicenciaAsync(string claveActivacion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(claveActivacion))
        {
            return new ResultadoActivacionDto
            {
                Exitoso = false,
                Mensaje = "Por favor, ingresa una clave de activación."
            };
        }

        var claveLimpia = claveActivacion.Trim().ToUpperInvariant();

        // Formatos admitidos: PRM-YYYYMMDD-XXXXXXXX, STD-YYYYMMDD-XXXXXXXX o legacy ACT-YYYYMMDD-XXXXXXXX
        var partes = claveLimpia.Split('-');
        if (partes.Length != 3 || (partes[0] != "PRM" && partes[0] != "STD" && partes[0] != "ACT") || partes[1].Length != 8 || partes[2].Length != 8)
        {
            return new ResultadoActivacionDto
            {
                Exitoso = false,
                Mensaje = "El formato de la clave no es correcto. Debe ser: STD-YYYYMMDD-XXXXXXXX o PRM-YYYYMMDD-XXXXXXXX."
            };
        }

        if (!DateTime.TryParseExact(partes[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fechaExp))
        {
            return new ResultadoActivacionDto
            {
                Exitoso = false,
                Mensaje = "La fecha contenida en la clave no es válida."
            };
        }

        // Fijar fin del día especificado en UTC (23:59:59)
        fechaExp = new DateTime(fechaExp.Year, fechaExp.Month, fechaExp.Day, 23, 59, 59, DateTimeKind.Utc);

        var licencia = await ObtenerOCrearLicenciaAsync(cancellationToken);

        // Determinar plan de la clave
        var plan = partes[0] == "PRM" || partes[0] == "ACT" ? TipoPlanLicencia.Premium : TipoPlanLicencia.Estandar;

        // Validar firma para esta instalación específica
        string claveCalculada = partes[0] == "ACT"
            ? GenerarClaveLegacy(licencia.CodigoInstalacion, fechaExp)
            : GenerarClaveActivacion(licencia.CodigoInstalacion, fechaExp, plan);

        if (!string.Equals(claveLimpia, claveCalculada, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoActivacionDto
            {
                Exitoso = false,
                Mensaje = "La clave no corresponde a este equipo o la firma criptográfica es incorrecta."
            };
        }

        if (fechaExp <= DateTime.UtcNow)
        {
            return new ResultadoActivacionDto
            {
                Exitoso = false,
                Mensaje = "La clave corresponde a un período que ya ha caducado."
            };
        }

        // Aplicar activación
        licencia.TipoPlan = plan;
        licencia.FechaExpiracion = fechaExp;
        licencia.ClaveActivacion = claveLimpia;
        licencia.EstaActiva = true;
        licencia.UltimaFechaUso = DateTime.UtcNow;
        licencia.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Licencias.Update(licencia);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string nombrePlan = plan == TipoPlanLicencia.Premium ? "Plan Premium (con Asistente IA)" : "Plan Estándar";
        return new ResultadoActivacionDto
        {
            Exitoso = true,
            TipoPlan = plan,
            Mensaje = $"¡Sistema activado con éxito en {nombrePlan}! Válido hasta el {fechaExp.ToLocalTime():dd/MM/yyyy}.",
            NuevaFechaExpiracion = fechaExp
        };
    }

    public string GenerarClaveActivacion(string codigoInstalacion, DateTime fechaExpiracion, TipoPlanLicencia plan = TipoPlanLicencia.Estandar)
    {
        var codigoLimpio = codigoInstalacion?.Trim().ToUpperInvariant() ?? string.Empty;
        var fechaStr = fechaExpiracion.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var prefijo = plan == TipoPlanLicencia.Premium ? "PRM" : "STD";

        var payload = $"{prefijo}:{codigoLimpio}:{fechaStr}";
        using var hmac = new HMACSHA256(MasterSecretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        var firmaCorta = Convert.ToHexString(hash)[..8].ToUpperInvariant();
        return $"{prefijo}-{fechaStr}-{firmaCorta}";
    }

    private static string GenerarClaveLegacy(string codigoInstalacion, DateTime fechaExpiracion)
    {
        var codigoLimpio = codigoInstalacion?.Trim().ToUpperInvariant() ?? string.Empty;
        var fechaStr = fechaExpiracion.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var payload = $"{codigoLimpio}:{fechaStr}";
        using var hmac = new HMACSHA256(MasterSecretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        var firmaCorta = Convert.ToHexString(hash)[..8].ToUpperInvariant();
        return $"ACT-{fechaStr}-{firmaCorta}";
    }

    public string ObtenerCodigoInstalacion()
    {
        var machine = Environment.MachineName.Trim().ToUpperInvariant();
        var safeMachine = machine.Length > 8 ? machine[..8] : machine;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(machine + Environment.UserName));
        var hex = Convert.ToHexString(hash)[..8];
        return $"POS-{safeMachine}-{hex}";
    }

    private async Task<LicenciaSistema> ObtenerOCrearLicenciaAsync(CancellationToken cancellationToken)
    {
        var licencias = await _unitOfWork.Licencias.GetAllAsync(cancellationToken);
        var licencia = licencias.FirstOrDefault();

        if (licencia == null)
        {
            var codigo = ObtenerCodigoInstalacion();
            // Período de cortesía inicial: 30 días con Plan Premium habilitado para probar el Asistente IA
            var expiracionInicial = DateTime.UtcNow.Date.AddDays(30).AddHours(23).AddMinutes(59).AddSeconds(59);
            var claveInicial = GenerarClaveActivacion(codigo, expiracionInicial, TipoPlanLicencia.Premium);

            licencia = new LicenciaSistema
            {
                CodigoInstalacion = codigo,
                Comercio = "Mi Tienda",
                TipoPlan = TipoPlanLicencia.Premium,
                FechaExpiracion = expiracionInicial,
                ClaveActivacion = claveInicial,
                UltimaFechaUso = DateTime.UtcNow,
                EstaActiva = true
            };

            await _unitOfWork.Licencias.AddAsync(licencia, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return licencia;
    }
}
