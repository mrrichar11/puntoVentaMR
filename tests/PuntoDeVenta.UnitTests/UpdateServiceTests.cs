using System.Net;
using System.Text;
using FluentAssertions;
using PuntoDeVenta.Application.DTOs.Sistema;
using PuntoDeVenta.Infrastructure.Services;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class UpdateServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public void ObtenerVersionActual_DebeRetornarVersionValida()
    {
        var service = new GitHubUpdateService();
        var version = service.ObtenerVersionActual();

        version.Should().NotBeNullOrWhiteSpace();
        Version.TryParse(version, out _).Should().BeTrue();
    }

    [Fact]
    public async Task VerificarActualizaciones_ConOwnerORepoVacio_DebeRetornarSinActualizacion()
    {
        var service = new GitHubUpdateService();
        var resultado = await service.VerificarActualizacionesAsync("", "");

        resultado.HayActualizacion.Should().BeFalse();
        resultado.HayActualizacionDisponible.Should().BeFalse();
        resultado.Mensaje.Should().Contain("No se ha configurado");
    }

    [Fact]
    public async Task VerificarActualizaciones_CuandoHayNuevaVersionConZip_DebeDetectarActualizacion()
    {
        var jsonResponse = @"{
            ""tag_name"": ""v99.0.0"",
            ""name"": ""MR SYS v99.0.0 Super Release"",
            ""body"": ""- Nueva funcionalidad de IA\n- Mejoras en tickets"",
            ""published_at"": ""2026-09-08T10:00:00Z"",
            ""assets"": [
                {
                    ""name"": ""PuntoDeVenta_v99.0.0.zip"",
                    ""browser_download_url"": ""https://github.com/mrrichar11/puntoVentaMR/releases/download/v99.0.0/PuntoDeVenta_v99.0.0.zip"",
                    ""size"": 15000000
                }
            ]
        }";

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handler);
        var service = new GitHubUpdateService(httpClient);

        var resultado = await service.VerificarActualizacionesAsync("mrrichar11", "puntoVentaMR");

        resultado.HayActualizacion.Should().BeTrue();
        resultado.HayActualizacionDisponible.Should().BeTrue();
        resultado.NuevaVersion.Should().Be("99.0.0");
        resultado.Titulo.Should().Be("MR SYS v99.0.0 Super Release");
        resultado.NotasCambios.Should().Contain("Nueva funcionalidad de IA");
        resultado.UrlDescarga.Should().Be("https://github.com/mrrichar11/puntoVentaMR/releases/download/v99.0.0/PuntoDeVenta_v99.0.0.zip");
        resultado.NombreArchivo.Should().Be("PuntoDeVenta_v99.0.0.zip");
        resultado.TamanoBytes.Should().Be(15000000);
    }

    [Fact]
    public async Task VerificarActualizaciones_CuandoVersionEsMenor_DebeIndicarAlDia()
    {
        var jsonResponse = @"{
            ""tag_name"": ""v0.0.1"",
            ""name"": ""v0.0.1"",
            ""body"": ""Version antigua"",
            ""published_at"": ""2024-01-01T00:00:00Z"",
            ""assets"": []
        }";

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handler);
        var service = new GitHubUpdateService(httpClient);

        var resultado = await service.VerificarActualizacionesAsync("Fliac", "PuntoDeVenta");

        resultado.HayActualizacion.Should().BeFalse();
        resultado.HayActualizacionDisponible.Should().BeFalse();
        resultado.Mensaje.Should().Contain("más reciente");
    }

    [Fact]
    public async Task VerificarActualizaciones_CuandoNoExisteRelease_DebeManejar404Gracefully()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "{\"message\": \"Not Found\"}");
        var httpClient = new HttpClient(handler);
        var service = new GitHubUpdateService(httpClient);

        var resultado = await service.VerificarActualizacionesAsync("Fliac", "PuntoDeVentaInexistente");

        resultado.HayActualizacion.Should().BeFalse();
        resultado.HayActualizacionDisponible.Should().BeFalse();
        resultado.Mensaje.Should().Contain("No se encontraron nuevas versiones");
    }

    [Fact]
    public void ActualizacionDto_Aliases_DebenCoincidir()
    {
        var dto = new ActualizacionDto
        {
            VersionNueva = "2.1.0",
            HayActualizacion = true,
            TituloRelease = "Release 2.1",
            NotasVersion = "Changelog"
        };

        dto.NuevaVersion.Should().Be("2.1.0");
        dto.HayActualizacionDisponible.Should().BeTrue();
        dto.Titulo.Should().Be("Release 2.1");
        dto.NotasCambios.Should().Be("Changelog");
    }
}
