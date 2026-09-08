using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Ventas;

using PuntoDeVenta.Domain.Entities.Clientes;
using PuntoDeVenta.Domain.Entities.Configuracion;
using PuntoDeVenta.Domain.Entities.Proveedores;
using PuntoDeVenta.Domain.Entities.Seguridad;

namespace PuntoDeVenta.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<VarianteArticulo> VariantesArticulo => Set<VarianteArticulo>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<LineaVenta> LineasVenta => Set<LineaVenta>();

    // Finanzas y Caja
    public DbSet<TurnoCaja> TurnosCaja => Set<TurnoCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<CategoriaGasto> CategoriasGasto => Set<CategoriaGasto>();
    public DbSet<Gasto> Gastos => Set<Gasto>();

    // Ventas y Pagos
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();

    // Clientes y Cuentas Corrientes
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<MovimientoCuentaCorrienteCliente> MovimientosCuentaCorrienteClientes => Set<MovimientoCuentaCorrienteCliente>();

    // Proveedores y Compras
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<CompraProveedor> ComprasProveedores => Set<CompraProveedor>();
    public DbSet<LineaCompraProveedor> LineasCompraProveedores => Set<LineaCompraProveedor>();
    public DbSet<PagoCompraProveedor> PagosCompraProveedores => Set<PagoCompraProveedor>();

    // Configuración Global y Seguridad
    public DbSet<ConfiguracionNegocio> Configuraciones => Set<ConfiguracionNegocio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<LicenciaSistema> Licencias => Set<LicenciaSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Categoria ---
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Descripcion).HasMaxLength(250);
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        // --- Marca ---
        modelBuilder.Entity<Marca>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(m => m.Nombre).IsUnique();
        });

        // --- Articulo (Padre) ---
        modelBuilder.Entity<Articulo>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.CodigoEstilo).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(a => a.Descripcion).HasMaxLength(500);
            entity.Property(a => a.Temporada).HasMaxLength(50);
            entity.Property(a => a.Genero).HasMaxLength(30);

            entity.HasIndex(a => a.CodigoEstilo).IsUnique();

            entity.HasOne(a => a.Categoria)
                  .WithMany(c => c.Articulos)
                  .HasForeignKey(a => a.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Marca)
                  .WithMany(m => m.Articulos)
                  .HasForeignKey(a => a.MarcaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(a => a.Variantes)
                  .WithOne(v => v.Articulo)
                  .HasForeignKey(v => v.ArticuloId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(a => a.Categoria).AutoInclude();
            entity.Navigation(a => a.Marca).AutoInclude();
        });

        // --- VarianteArticulo (Hijo) ---
        modelBuilder.Entity<VarianteArticulo>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.SKU).IsRequired().HasMaxLength(60);
            entity.Property(v => v.CodigoBarras).HasMaxLength(60);
            entity.Property(v => v.Talle).IsRequired().HasMaxLength(30);
            entity.Property(v => v.Color).IsRequired().HasMaxLength(50);
            entity.Property(v => v.Ubicacion).HasMaxLength(60);

            entity.Property(v => v.PrecioCosto).HasPrecision(18, 2);
            entity.Property(v => v.PrecioLista).HasPrecision(18, 2);
            entity.Property(v => v.PrecioOferta).HasPrecision(18, 2);

            entity.HasIndex(v => v.SKU).IsUnique();
            entity.HasIndex(v => v.CodigoBarras)
                  .IsUnique()
                  .HasFilter("CodigoBarras IS NOT NULL AND CodigoBarras <> ''");

            entity.Navigation(v => v.Articulo).AutoInclude();
        });

        // --- MovimientoStock ---
        modelBuilder.Entity<MovimientoStock>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Tipo).IsRequired();
            entity.Property(m => m.CostoUnitario).HasPrecision(18, 2);
            entity.Property(m => m.Motivo).HasMaxLength(250);
            entity.Property(m => m.ReferenciaDocumento).HasMaxLength(100);

            entity.HasOne(m => m.Variante)
                  .WithMany()
                  .HasForeignKey(m => m.VarianteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- MetodoPago ---
        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.HasKey(mp => mp.Id);
            entity.Property(mp => mp.Nombre).IsRequired().HasMaxLength(60);
            entity.Property(mp => mp.PorcentajeAjuste).HasPrecision(6, 2);
            entity.Property(mp => mp.ComisionPorcentual).HasPrecision(6, 2);
            entity.HasIndex(mp => mp.Nombre).IsUnique();
        });

        // --- LineaVenta ---
        modelBuilder.Entity<LineaVenta>(entity =>
        {
            entity.HasKey(lv => lv.Id);
            entity.Property(lv => lv.DescripcionArticulo).IsRequired().HasMaxLength(200);
            entity.Property(lv => lv.SKU).IsRequired().HasMaxLength(60);
            entity.Property(lv => lv.Talle).IsRequired().HasMaxLength(30);
            entity.Property(lv => lv.Color).IsRequired().HasMaxLength(50);

            entity.Property(lv => lv.PrecioListaUnitario).HasPrecision(18, 2);
            entity.Property(lv => lv.DescuentoOfertaUnitario).HasPrecision(18, 2);
            entity.Property(lv => lv.DescuentoMedioPagoUnitario).HasPrecision(18, 2);
            entity.Property(lv => lv.RecargoMedioPagoUnitario).HasPrecision(18, 2);
            entity.Property(lv => lv.PrecioFinalCobrado).HasPrecision(18, 2);
            entity.Property(lv => lv.CostoUnitarioHistorico).HasPrecision(18, 2);

            entity.HasOne(lv => lv.Variante)
                  .WithMany()
                  .HasForeignKey(lv => lv.VarianteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- TurnoCaja ---
        modelBuilder.Entity<TurnoCaja>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.UsuarioApertura).IsRequired().HasMaxLength(60);
            entity.Property(t => t.UsuarioCierre).HasMaxLength(60);
            entity.Property(t => t.ObservacionesApertura).HasMaxLength(300);
            entity.Property(t => t.ObservacionesCierre).HasMaxLength(300);

            entity.Property(t => t.MontoInicialEfectivo).HasPrecision(18, 2);
            entity.Property(t => t.MontoRealEfectivo).HasPrecision(18, 2);
            entity.Property(t => t.MontoTeoricoEfectivo).HasPrecision(18, 2);

            entity.Property(t => t.MontoRealTransferencias).HasPrecision(18, 2);
            entity.Property(t => t.MontoTeoricoTransferencias).HasPrecision(18, 2);

            entity.Property(t => t.MontoRealTarjetas).HasPrecision(18, 2);
            entity.Property(t => t.MontoTeoricoTarjetas).HasPrecision(18, 2);

            entity.HasMany(t => t.Movimientos)
                  .WithOne(m => m.TurnoCaja)
                  .HasForeignKey(m => m.TurnoCajaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Gastos)
                  .WithOne(g => g.TurnoCaja)
                  .HasForeignKey(g => g.TurnoCajaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MovimientoCaja ---
        modelBuilder.Entity<MovimientoCaja>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Monto).HasPrecision(18, 2);
            entity.Property(m => m.Descripcion).IsRequired().HasMaxLength(250);
            entity.Property(m => m.ReferenciaComprobante).HasMaxLength(100);
        });

        // --- CategoriaGasto ---
        modelBuilder.Entity<CategoriaGasto>(entity =>
        {
            entity.HasKey(cg => cg.Id);
            entity.Property(cg => cg.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(cg => cg.Descripcion).HasMaxLength(250);
            entity.HasIndex(cg => cg.Nombre).IsUnique();
        });

        // --- Gasto ---
        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Monto).HasPrecision(18, 2);
            entity.Property(g => g.Descripcion).IsRequired().HasMaxLength(250);
            entity.Property(g => g.NumeroComprobante).HasMaxLength(100);

            entity.HasOne(g => g.CategoriaGasto)
                  .WithMany(cg => cg.Gastos)
                  .HasForeignKey(g => g.CategoriaGastoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Venta ---
        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.NumeroComprobante).IsRequired().HasMaxLength(50);
            entity.Property(v => v.ClienteNombre).HasMaxLength(150);
            entity.Property(v => v.ClienteDocumento).HasMaxLength(50);
            entity.Property(v => v.NroPedidoWeb).HasMaxLength(50);
            entity.Property(v => v.Vendedora).HasMaxLength(100);

            entity.Property(v => v.SubtotalLista).HasPrecision(18, 2);
            entity.Property(v => v.TotalDescuentoOferta).HasPrecision(18, 2);
            entity.Property(v => v.TotalDescuentoMedioPago).HasPrecision(18, 2);
            entity.Property(v => v.TotalRecargoMedioPago).HasPrecision(18, 2);
            entity.Property(v => v.TotalFinalCobrado).HasPrecision(18, 2);
            entity.Property(v => v.CostoTotalHistorico).HasPrecision(18, 2);

            entity.HasIndex(v => v.NumeroComprobante).IsUnique();

            entity.HasOne(v => v.TurnoCaja)
                  .WithMany()
                  .HasForeignKey(v => v.TurnoCajaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(v => v.Lineas)
                  .WithOne(lv => lv.Venta)
                  .HasForeignKey(lv => lv.VentaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.Pagos)
                  .WithOne(p => p.Venta)
                  .HasForeignKey(p => p.VentaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(v => v.Lineas).AutoInclude();
            entity.Navigation(v => v.Pagos).AutoInclude();
        });

        // --- PagoVenta ---
        modelBuilder.Entity<PagoVenta>(entity =>
        {
            entity.HasKey(pv => pv.Id);
            entity.Property(pv => pv.Monto).HasPrecision(18, 2);
            entity.Property(pv => pv.PorcentajeAjuste).HasPrecision(6, 2);
            entity.Property(pv => pv.MontoAjuste).HasPrecision(18, 2);
            entity.Property(pv => pv.ComisionPorcentual).HasPrecision(6, 2);
            entity.Property(pv => pv.Referencia).HasMaxLength(100);

            entity.HasOne(pv => pv.MetodoPago)
                  .WithMany()
                  .HasForeignKey(pv => pv.MetodoPagoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Cliente ---
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.NombreCompleto).IsRequired().HasMaxLength(150);
            entity.Property(c => c.DocumentoIdentidad).HasMaxLength(30);
            entity.Property(c => c.Telefono).HasMaxLength(40);
            entity.Property(c => c.Email).HasMaxLength(100);
            entity.Property(c => c.Direccion).HasMaxLength(200);
            entity.Property(c => c.Notas).HasMaxLength(500);

            entity.Property(c => c.LimiteCredito).HasPrecision(18, 2);
            entity.Property(c => c.SaldoDeudorActual).HasPrecision(18, 2);

            entity.HasIndex(c => c.NombreCompleto);
            entity.HasIndex(c => c.DocumentoIdentidad);

            entity.HasMany(c => c.MovimientosCuentaCorriente)
                  .WithOne(m => m.Cliente)
                  .HasForeignKey(m => m.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MovimientoCuentaCorrienteCliente ---
        modelBuilder.Entity<MovimientoCuentaCorrienteCliente>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Monto).HasPrecision(18, 2);
            entity.Property(m => m.SaldoPrevio).HasPrecision(18, 2);
            entity.Property(m => m.SaldoResultante).HasPrecision(18, 2);
            entity.Property(m => m.Detalle).IsRequired().HasMaxLength(250);
            entity.Property(m => m.ReferenciaComprobante).HasMaxLength(100);

            entity.HasOne(m => m.Venta)
                  .WithMany()
                  .HasForeignKey(m => m.VentaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.TurnoCaja)
                  .WithMany()
                  .HasForeignKey(m => m.TurnoCajaId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Proveedor ---
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.RazonSocial).IsRequired().HasMaxLength(150);
            entity.Property(p => p.NombreContacto).HasMaxLength(100);
            entity.Property(p => p.Cuit).HasMaxLength(30);
            entity.Property(p => p.Telefono).HasMaxLength(40);
            entity.Property(p => p.Email).HasMaxLength(100);
            entity.Property(p => p.Direccion).HasMaxLength(200);
            entity.Property(p => p.Notas).HasMaxLength(500);
            entity.Property(p => p.SaldoDeudorActual).HasPrecision(18, 2);

            entity.HasIndex(p => p.RazonSocial);

            entity.HasMany(p => p.Compras)
                  .WithOne(c => c.Proveedor)
                  .HasForeignKey(c => c.ProveedorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CompraProveedor ---
        modelBuilder.Entity<CompraProveedor>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.NumeroComprobante).IsRequired().HasMaxLength(50);
            entity.Property(c => c.TotalCompra).HasPrecision(18, 2);
            entity.Property(c => c.TotalPagado).HasPrecision(18, 2);
            entity.Property(c => c.Observaciones).HasMaxLength(500);

            entity.HasOne(c => c.TurnoCaja)
                  .WithMany()
                  .HasForeignKey(c => c.TurnoCajaId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(c => c.Lineas)
                  .WithOne(l => l.CompraProveedor)
                  .HasForeignKey(l => l.CompraProveedorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Pagos)
                  .WithOne(p => p.CompraProveedor)
                  .HasForeignKey(p => p.CompraProveedorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- LineaCompraProveedor ---
        modelBuilder.Entity<LineaCompraProveedor>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.CostoUnitarioCompra).HasPrecision(18, 2);

            entity.HasOne(l => l.Variante)
                  .WithMany()
                  .HasForeignKey(l => l.VarianteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PagoCompraProveedor ---
        modelBuilder.Entity<PagoCompraProveedor>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Monto).HasPrecision(18, 2);
            entity.Property(p => p.ReferenciaComprobante).HasMaxLength(100);
            entity.Property(p => p.Notas).HasMaxLength(250);

            entity.HasOne(p => p.TurnoCaja)
                  .WithMany()
                  .HasForeignKey(p => p.TurnoCajaId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // --- ConfiguracionNegocio ---
        modelBuilder.Entity<ConfiguracionNegocio>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.NombreComercio).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Direccion).HasMaxLength(250);
            entity.Property(c => c.Telefono).HasMaxLength(50);
            entity.Property(c => c.Cuit).HasMaxLength(50);
            entity.Property(c => c.VendedoraDefecto).HasMaxLength(100);
            entity.Property(c => c.LogoRuta).HasMaxLength(500);
            entity.Property(c => c.PorcentajeDescuentoEfectivo).HasPrecision(18, 2);
            entity.Property(c => c.ComisionTarjetaDebito).HasPrecision(18, 2);
            entity.Property(c => c.ComisionTarjetaCredito).HasPrecision(18, 2);
            entity.Property(c => c.RecargoCuotasTarjetaCredito).HasPrecision(18, 2);
            entity.Property(c => c.Recargo3Cuotas).HasPrecision(18, 2);
            entity.Property(c => c.Recargo6Cuotas).HasPrecision(18, 2);
            entity.Property(c => c.Recargo9Cuotas).HasPrecision(18, 2);
            entity.Property(c => c.Recargo12Cuotas).HasPrecision(18, 2);
            entity.Property(c => c.MargenGananciaSugerido).HasPrecision(18, 2);
            entity.Property(c => c.CostosBancariosEstimados).HasPrecision(18, 2);
            entity.Property(c => c.TopeFiadoDefecto).HasPrecision(18, 2);
            entity.Property(c => c.TopeMensualRetiroDueño).HasPrecision(18, 2);
            entity.Property(c => c.TemaInterfaz).HasMaxLength(50);
            entity.Property(c => c.GitHubRepoOwner).HasMaxLength(100);
            entity.Property(c => c.GitHubRepoName).HasMaxLength(100);
        });

        // --- Usuario ---
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(250);
            entity.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(100);
        });

        // --- LicenciaSistema ---
        modelBuilder.Entity<LicenciaSistema>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.CodigoInstalacion).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Comercio).HasMaxLength(150);
            entity.Property(l => l.ClaveActivacion).HasMaxLength(100);
        });
    }
}
