using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Infrastructure.Data;
using Xunit;

namespace PuntoDeVenta.UnitTests;

public class DataSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public DataSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SeedAsync_PueblaMetodosPagoCategoriasGastoYCatalogoInicial()
    {
        // Act
        await DataSeeder.SeedAsync(_context);

        // Assert: Métodos de Pago
        var metodos = await _context.MetodosPago.ToListAsync();
        metodos.Should().HaveCountGreaterThanOrEqualTo(4);
        metodos.Should().Contain(m => m.Nombre.Contains("Efectivo") && m.PorcentajeAjuste == -10m);
        metodos.Should().Contain(m => m.Nombre.Contains("Transferencia"));

        // Assert: Categorías de Gasto con Retiro de Propietario Aislado
        var categoriasGasto = await _context.CategoriasGasto.ToListAsync();
        categoriasGasto.Should().HaveCountGreaterThanOrEqualTo(5);
        categoriasGasto.Should().Contain(cg => cg.EsGastoPersonal && cg.Nombre.Contains("Retiro Propietario"));

        // Assert: Catálogo de Indumentaria y Calzado con Variantes
        var articulos = await _context.Articulos.Include(a => a.Variantes).ToListAsync();
        articulos.Should().HaveCountGreaterThanOrEqualTo(2);
        articulos.SelectMany(a => a.Variantes).Should().HaveCountGreaterThanOrEqualTo(4);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
