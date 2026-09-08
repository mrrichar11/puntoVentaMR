using PuntoDeVenta.Application.Contracts;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Ventas;
using PuntoDeVenta.Infrastructure.Data;

namespace PuntoDeVenta.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private bool _disposed;

    public IRepository<Articulo> Articulos { get; }
    public IRepository<VarianteArticulo> Variantes { get; }
    public IRepository<Categoria> Categorias { get; }
    public IRepository<Marca> Marcas { get; }
    public IRepository<MovimientoStock> MovimientosStock { get; }
    public IRepository<MetodoPago> MetodosPago { get; }
    public IRepository<LineaVenta> LineasVenta { get; }

    public IRepository<TurnoCaja> TurnosCaja { get; }
    public IRepository<MovimientoCaja> MovimientosCaja { get; }
    public IRepository<CategoriaGasto> CategoriasGasto { get; }
    public IRepository<Gasto> Gastos { get; }

    public IRepository<Venta> Ventas { get; }
    public IRepository<PagoVenta> PagosVenta { get; }

    public IRepository<PuntoDeVenta.Domain.Entities.Clientes.Cliente> Clientes { get; }
    public IRepository<PuntoDeVenta.Domain.Entities.Clientes.MovimientoCuentaCorrienteCliente> MovimientosCuentaCorrienteClientes { get; }

    public IRepository<PuntoDeVenta.Domain.Entities.Proveedores.Proveedor> Proveedores { get; }
    public IRepository<PuntoDeVenta.Domain.Entities.Proveedores.CompraProveedor> ComprasProveedores { get; }
    public IRepository<PuntoDeVenta.Domain.Entities.Proveedores.LineaCompraProveedor> LineasCompraProveedores { get; }
    public IRepository<PuntoDeVenta.Domain.Entities.Proveedores.PagoCompraProveedor> PagosCompraProveedores { get; }

    public IRepository<PuntoDeVenta.Domain.Entities.Configuracion.ConfiguracionNegocio> Configuraciones { get; }
    public IRepository<PuntoDeVenta.Domain.Entities.Seguridad.Usuario> Usuarios { get; }
    public IRepository<PuntoDeVenta.Domain.Entities.Seguridad.LicenciaSistema> Licencias { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        Articulos = new Repository<Articulo>(_context);
        Variantes = new Repository<VarianteArticulo>(_context);
        Categorias = new Repository<Categoria>(_context);
        Marcas = new Repository<Marca>(_context);
        MovimientosStock = new Repository<MovimientoStock>(_context);
        MetodosPago = new Repository<MetodoPago>(_context);
        LineasVenta = new Repository<LineaVenta>(_context);

        TurnosCaja = new Repository<TurnoCaja>(_context);
        MovimientosCaja = new Repository<MovimientoCaja>(_context);
        CategoriasGasto = new Repository<CategoriaGasto>(_context);
        Gastos = new Repository<Gasto>(_context);

        Ventas = new Repository<Venta>(_context);
        PagosVenta = new Repository<PagoVenta>(_context);

        Clientes = new Repository<PuntoDeVenta.Domain.Entities.Clientes.Cliente>(_context);
        MovimientosCuentaCorrienteClientes = new Repository<PuntoDeVenta.Domain.Entities.Clientes.MovimientoCuentaCorrienteCliente>(_context);

        Proveedores = new Repository<PuntoDeVenta.Domain.Entities.Proveedores.Proveedor>(_context);
        ComprasProveedores = new Repository<PuntoDeVenta.Domain.Entities.Proveedores.CompraProveedor>(_context);
        LineasCompraProveedores = new Repository<PuntoDeVenta.Domain.Entities.Proveedores.LineaCompraProveedor>(_context);
        PagosCompraProveedores = new Repository<PuntoDeVenta.Domain.Entities.Proveedores.PagoCompraProveedor>(_context);

        Configuraciones = new Repository<PuntoDeVenta.Domain.Entities.Configuracion.ConfiguracionNegocio>(_context);
        Usuarios = new Repository<PuntoDeVenta.Domain.Entities.Seguridad.Usuario>(_context);
        Licencias = new Repository<PuntoDeVenta.Domain.Entities.Seguridad.LicenciaSistema>(_context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}
