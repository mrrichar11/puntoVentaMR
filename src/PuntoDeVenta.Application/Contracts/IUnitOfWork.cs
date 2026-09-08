using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Inventario;
using PuntoDeVenta.Domain.Entities.Ventas;

namespace PuntoDeVenta.Application.Contracts;

public interface IUnitOfWork : IDisposable
{
    IRepository<Articulo> Articulos { get; }
    IRepository<VarianteArticulo> Variantes { get; }
    IRepository<Categoria> Categorias { get; }
    IRepository<Marca> Marcas { get; }
    IRepository<MovimientoStock> MovimientosStock { get; }
    IRepository<MetodoPago> MetodosPago { get; }
    IRepository<LineaVenta> LineasVenta { get; }

    IRepository<TurnoCaja> TurnosCaja { get; }
    IRepository<MovimientoCaja> MovimientosCaja { get; }
    IRepository<CategoriaGasto> CategoriasGasto { get; }
    IRepository<Gasto> Gastos { get; }

    IRepository<Venta> Ventas { get; }
    IRepository<PagoVenta> PagosVenta { get; }

    IRepository<PuntoDeVenta.Domain.Entities.Clientes.Cliente> Clientes { get; }
    IRepository<PuntoDeVenta.Domain.Entities.Clientes.MovimientoCuentaCorrienteCliente> MovimientosCuentaCorrienteClientes { get; }

    IRepository<PuntoDeVenta.Domain.Entities.Proveedores.Proveedor> Proveedores { get; }
    IRepository<PuntoDeVenta.Domain.Entities.Proveedores.CompraProveedor> ComprasProveedores { get; }
    IRepository<PuntoDeVenta.Domain.Entities.Proveedores.LineaCompraProveedor> LineasCompraProveedores { get; }
    IRepository<PuntoDeVenta.Domain.Entities.Proveedores.PagoCompraProveedor> PagosCompraProveedores { get; }

    IRepository<PuntoDeVenta.Domain.Entities.Configuracion.ConfiguracionNegocio> Configuraciones { get; }
    IRepository<PuntoDeVenta.Domain.Entities.Seguridad.Usuario> Usuarios { get; }
    IRepository<PuntoDeVenta.Domain.Entities.Seguridad.LicenciaSistema> Licencias { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
