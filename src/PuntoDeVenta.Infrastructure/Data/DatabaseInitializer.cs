using Microsoft.EntityFrameworkCore;

namespace PuntoDeVenta.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // Aplica migraciones pendientes en el archivo local SQLite
        await context.Database.MigrateAsync();

        // Configuración de alto rendimiento para entorno local-first POS
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;");

        // Compatibilidad retroactiva para columnas agregadas
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Proveedores ADD COLUMN Notas TEXT;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN LogoRuta TEXT;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Habilitar3Cuotas INTEGER NOT NULL DEFAULT 1;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Recargo3Cuotas TEXT NOT NULL DEFAULT '15.0';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Habilitar6Cuotas INTEGER NOT NULL DEFAULT 1;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Recargo6Cuotas TEXT NOT NULL DEFAULT '25.0';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Habilitar9Cuotas INTEGER NOT NULL DEFAULT 0;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Recargo9Cuotas TEXT NOT NULL DEFAULT '35.0';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Habilitar12Cuotas INTEGER NOT NULL DEFAULT 0;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN Recargo12Cuotas TEXT NOT NULL DEFAULT '45.0';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN CuotasIncluidasEnPrecioLista INTEGER NOT NULL DEFAULT 0;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN GitHubRepoOwner TEXT NOT NULL DEFAULT 'mrrichar11';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Configuraciones ADD COLUMN GitHubRepoName TEXT NOT NULL DEFAULT 'puntoVentaMR';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("UPDATE Configuraciones SET GitHubRepoOwner = 'mrrichar11', GitHubRepoName = 'puntoVentaMR' WHERE GitHubRepoOwner = 'Fliac' OR GitHubRepoOwner IS NULL OR GitHubRepoOwner = '';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE LineasVenta ADD COLUMN EsVentaManual INTEGER NOT NULL DEFAULT 0;"); } catch { }
    }
}
