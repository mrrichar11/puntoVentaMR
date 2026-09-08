using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using PuntoDeVenta.Application.DTOs.Sistema;
using PuntoDeVenta.Application.Services;

namespace PuntoDeVenta.Infrastructure.Services;

public class GitHubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    public GitHubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public string ObtenerVersionActual()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version 
                   ?? Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    public async Task<ActualizacionDto> VerificarActualizacionesAsync(string owner, string repo, CancellationToken ct = default)
    {
        var versionActualStr = ObtenerVersionActual();
        var resultado = new ActualizacionDto
        {
            VersionActual = versionActualStr,
            HayActualizacion = false
        };

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            resultado.Mensaje = "No se ha configurado el repositorio de GitHub.";
            return resultado;
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner.Trim()}/{repo.Trim()}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PuntoDeVenta-Retail", versionActualStr));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                resultado.Mensaje = response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? "No se encontraron releases públicos en el repositorio."
                    : $"Respuesta del servidor GitHub: {(int)response.StatusCode} {response.ReasonPhrase}";
                return resultado;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var releaseTitle = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : tagName;
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            var publishedAt = root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTime(out var dt) ? dt : DateTime.UtcNow;

            var cleanTag = tagName.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(cleanTag, out var versionNuevaObj))
            {
                resultado.Mensaje = $"La etiqueta del release '{tagName}' no sigue el formato de versión (x.y.z).";
                return resultado;
            }

            if (!Version.TryParse(versionActualStr, out var versionActualObj))
            {
                versionActualObj = new Version(1, 0, 0);
            }

            if (versionNuevaObj > versionActualObj)
            {
                string downloadUrl = "";
                long assetSize = 0;
                string assetName = "";

                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        var aName = asset.TryGetProperty("name", out var anProp) ? anProp.GetString() ?? "" : "";
                        if (aName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.TryGetProperty("browser_download_url", out var dlProp) ? dlProp.GetString() ?? "" : "";
                            assetSize = asset.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0;
                            assetName = aName;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    resultado.HayActualizacion = true;
                    resultado.VersionNueva = cleanTag;
                    resultado.TituloRelease = releaseTitle;
                    resultado.NotasVersion = body;
                    resultado.UrlDescarga = downloadUrl;
                    resultado.TamanoBytes = assetSize;
                    resultado.NombreArchivo = assetName;
                    resultado.FechaPublicacion = publishedAt;
                    resultado.Mensaje = $"¡Nueva versión {cleanTag} disponible para descargar!";
                }
                else
                {
                    resultado.Mensaje = $"Existe la versión {cleanTag}, pero aún no tiene adjunto el instalador .zip en GitHub.";
                }
            }
            else
            {
                resultado.Mensaje = $"Tu versión actual ({versionActualStr}) es la más reciente. No hay actualizaciones pendientes.";
            }
        }
        catch (Exception ex)
        {
            resultado.Mensaje = $"No fue posible verificar actualizaciones: {ex.Message}";
        }

        return resultado;
    }

    public async Task<string> DescargarActualizacionAsync(ActualizacionDto info, IProgress<ProgresoDescargaDto>? progreso = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(info.UrlDescarga))
            throw new InvalidOperationException("No se especificó la URL de descarga.");

        var tempDir = Path.Combine(Path.GetTempPath(), "PuntoDeVenta_Update");
        Directory.CreateDirectory(tempDir);

        var nombreArchivo = !string.IsNullOrWhiteSpace(info.NombreArchivo) ? info.NombreArchivo : $"PuntoDeVenta_v{info.VersionNueva}.zip";
        var destinoZip = Path.Combine(tempDir, nombreArchivo);

        if (File.Exists(destinoZip))
        {
            try { File.Delete(destinoZip); } catch { }
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, info.UrlDescarga);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PuntoDeVenta-Retail", ObtenerVersionActual()));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? (info.TamanoBytes > 0 ? info.TamanoBytes : (long?)null);

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destinoZip, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;

            if (progreso != null)
            {
                double pct = totalBytes.HasValue && totalBytes.Value > 0
                    ? Math.Round((double)totalRead / totalBytes.Value * 100.0, 1)
                    : 0;

                progreso.Report(new ProgresoDescargaDto
                {
                    Porcentaje = pct,
                    BytesRecibidos = totalRead,
                    TotalBytes = totalBytes,
                    MensajeEstado = $"Descargando actualización: {totalRead / (1024 * 1024.0):F1} MB" + (totalBytes.HasValue ? $" de {totalBytes.Value / (1024 * 1024.0):F1} MB ({pct:F0}%)" : "...")
                });
            }
        }

        return destinoZip;
    }

    public void IniciarInstalacion(string rutaZipDescargado)
    {
        if (!File.Exists(rutaZipDescargado))
            throw new FileNotFoundException("No se encontró el archivo de actualización descargado.", rutaZipDescargado);

        var appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var pid = Environment.ProcessId;

        var tempUpdaterDir = Path.Combine(Path.GetTempPath(), "PuntoDeVenta_Updater");
        Directory.CreateDirectory(tempUpdaterDir);

        // Descomprimir el ZIP en una carpeta temporal extraída
        var extractedDir = Path.Combine(tempUpdaterDir, "extracted");
        if (Directory.Exists(extractedDir))
        {
            try { Directory.Delete(extractedDir, true); } catch { }
        }
        Directory.CreateDirectory(extractedDir);
        ZipFile.ExtractToDirectory(rutaZipDescargado, extractedDir, true);

        // Generar script de actualización robusto y atómico (PowerShell/CMD) que se ejecuta fuera del directorio de la app
        var scriptPath = Path.Combine(tempUpdaterDir, "apply_update.cmd");
        var scriptContent = $@"@echo off
title Actualizando Punto de Venta...
echo =======================================================
echo          ACTUALIZACION DE PUNTO DE VENTA EN CURSO
echo =======================================================
echo Esperando a que el sistema cierre limpiamente...

:: 1. Esperar a que el proceso principal libere los archivos
powershell -NoProfile -ExecutionPolicy Bypass -Command ""try {{ $p = Get-Process -Id {pid} -ErrorAction SilentlyContinue; if ($p) {{ $p.WaitForExit(15000); }} }} catch {{}}""

:: 2. Copia de respaldo preventivo de la base de datos si existe
if exist ""{appDir}\puntodeventa.db"" (
    if not exist ""{appDir}\Backups"" mkdir ""{appDir}\Backups""
    copy /y ""{appDir}\puntodeventa.db"" ""{appDir}\Backups\pre_update_backup.db"" >nul
)

:: 3. Reemplazo seguro de binarios excluyendo archivos de datos y configuraciones personales
echo Aplicando nuevos componentes...
robocopy ""{extractedDir}"" ""{appDir}"" /E /R:3 /W:1 /XF *.db* gemini_key.txt *.log /XD Backups Tickets Logos >nul

:: 4. Relanzar la aplicacion actualizada
echo Reiniciando Punto de Venta...
start """" ""{appDir}\PuntoDeVenta.UI.exe""

:: 5. Finalizar
exit
";

        File.WriteAllText(scriptPath, scriptContent, System.Text.Encoding.Default);

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            WorkingDirectory = tempUpdaterDir,
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };

        Process.Start(startInfo);

        // Cerrar la aplicación principal
        Environment.Exit(0);
    }
}
