using Microsoft.EntityFrameworkCore;
using PuntoDeVenta.Domain.Entities.Catalogo;
using PuntoDeVenta.Domain.Entities.Finanzas;
using PuntoDeVenta.Domain.Entities.Ventas;

namespace PuntoDeVenta.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Métodos de Pago Comerciales
        if (!await context.MetodosPago.AnyAsync())
        {
            context.MetodosPago.AddRange(
                new MetodoPago
                {
                    Nombre = "Efectivo (10% Desc.)",
                    PorcentajeAjuste = -10.0m, // 10% bonificación
                    ComisionPorcentual = 0m,
                    PlazoAcreditacionDias = 0
                },
                new MetodoPago
                {
                    Nombre = "Transferencia Bancaria / QR",
                    PorcentajeAjuste = 0m,
                    ComisionPorcentual = 0.8m,
                    PlazoAcreditacionDias = 0,
                    RequiereReferencia = true
                },
                new MetodoPago
                {
                    Nombre = "Tarjeta Débito",
                    PorcentajeAjuste = 0m,
                    ComisionPorcentual = 1.5m,
                    PlazoAcreditacionDias = 2,
                    RequiereReferencia = true
                },
                new MetodoPago
                {
                    Nombre = "Tarjeta Crédito (1 Pago)",
                    PorcentajeAjuste = 0m,
                    ComisionPorcentual = 3.2m,
                    PlazoAcreditacionDias = 14,
                    RequiereReferencia = true
                },
                new MetodoPago
                {
                    Nombre = "Tarjeta Crédito (3 Cuotas +15%)",
                    PorcentajeAjuste = 15.0m, // 15% recargo financiamiento
                    ComisionPorcentual = 6.5m,
                    PlazoAcreditacionDias = 14,
                    RequiereReferencia = true
                }
            );
        }

        // 2. Categorías de Gastos y Retiro del Dueño
        if (!await context.CategoriasGasto.AnyAsync())
        {
            context.CategoriasGasto.AddRange(
                new CategoriaGasto
                {
                    Nombre = "Alquiler del Local",
                    TipoGasto = TipoGasto.Fijo,
                    EsGastoPersonal = false
                },
                new CategoriaGasto
                {
                    Nombre = "Servicios (Luz, Internet, Agua)",
                    TipoGasto = TipoGasto.Fijo,
                    EsGastoPersonal = false
                },
                new CategoriaGasto
                {
                    Nombre = "Bolsas y Packaging Comercial",
                    TipoGasto = TipoGasto.Packaging,
                    EsGastoPersonal = false
                },
                new CategoriaGasto
                {
                    Nombre = "Cadetería y Envíos a Domicilio",
                    TipoGasto = TipoGasto.Envio,
                    EsGastoPersonal = false
                },
                new CategoriaGasto
                {
                    Nombre = "Mermas y Roturas",
                    TipoGasto = TipoGasto.Merma,
                    EsGastoPersonal = false
                },
                new CategoriaGasto
                {
                    Nombre = "Retiro Propietario / Sueldo Emprendedor",
                    TipoGasto = TipoGasto.RetiroPropietario,
                    EsGastoPersonal = true // ¡Aislado de costos operativos!
                }
            );
        }

        // 3. Categorías y Marcas de Prueba para Calzado e Indumentaria
        if (!await context.Categorias.AnyAsync())
        {
            var catCalzado = new Categoria { Nombre = "Zapatillas y Calzado", Descripcion = "Calzado deportivo y urbano" };
            var catIndumentaria = new Categoria { Nombre = "Remeras y Tops", Descripcion = "Prendas de algodón y deportivas" };
            var catPantalones = new Categoria { Nombre = "Pantalones y Jeans", Descripcion = "Denim, jogger y gabardina" };

            var marcaNike = new Marca { Nombre = "Nike" };
            var marcaAdidas = new Marca { Nombre = "Adidas" };
            var marcaUrban = new Marca { Nombre = "Urban Street" };

            context.Categorias.AddRange(catCalzado, catIndumentaria, catPantalones);
            context.Marcas.AddRange(marcaNike, marcaAdidas, marcaUrban);

            await context.SaveChangesAsync();

            // Cargar 2 artículos de ejemplo con variantes y stock inicial
            var zapato = new Articulo
            {
                CodigoEstilo = "PEGASUS-40",
                Nombre = "Zapatilla Pegasus 40 Running",
                CategoriaId = catCalzado.Id,
                MarcaId = marcaNike.Id,
                Temporada = "Todo el año",
                Genero = "Unisex"
            };

            zapato.Variantes.Add(new VarianteArticulo
            {
                SKU = "PEGASUS-40-41-NEG",
                CodigoBarras = "7790001000410",
                Talle = "41",
                Color = "Negro",
                PrecioCosto = 35000m,
                PrecioLista = 85000m,
                PrecioOferta = null,
                PermiteDescuentoMedioPago = true,
                StockActual = 8,
                StockMinimo = 2
            });

            zapato.Variantes.Add(new VarianteArticulo
            {
                SKU = "PEGASUS-40-42-AZU",
                CodigoBarras = "7790001000427",
                Talle = "42",
                Color = "Azul Marino",
                PrecioCosto = 35000m,
                PrecioLista = 85000m,
                PrecioOferta = 69900m, // En Sale
                PermiteDescuentoMedioPago = false, // Liquidación cerrada sin descuento extra
                StockActual = 4,
                StockMinimo = 1
            });

            var remera = new Articulo
            {
                CodigoEstilo = "REM-OVERSIZE",
                Nombre = "Remera Algodón Peinado Oversize",
                CategoriaId = catIndumentaria.Id,
                MarcaId = marcaUrban.Id,
                Temporada = "Primavera-Verano",
                Genero = "Unisex"
            };

            remera.Variantes.Add(new VarianteArticulo
            {
                SKU = "REM-OVERSIZE-M-NEG",
                CodigoBarras = "7790002000021",
                Talle = "M",
                Color = "Negro",
                PrecioCosto = 8000m,
                PrecioLista = 22000m,
                StockActual = 15,
                StockMinimo = 3
            });

            remera.Variantes.Add(new VarianteArticulo
            {
                SKU = "REM-OVERSIZE-L-BLA",
                CodigoBarras = "7790002000038",
                Talle = "L",
                Color = "Blanco",
                PrecioCosto = 8000m,
                PrecioLista = 22000m,
                StockActual = 12,
                StockMinimo = 3
            });

            context.Articulos.AddRange(zapato, remera);
        }

        // 5. Configuración Inicial del Negocio
        if (!await context.Configuraciones.AnyAsync())
        {
            context.Configuraciones.Add(new PuntoDeVenta.Domain.Entities.Configuracion.ConfiguracionNegocio
            {
                NombreComercio = "Mi Tienda & Zapatería",
                Direccion = "Av. Principal 123",
                Telefono = "011-4567-8900",
                Cuit = "20-12345678-9",
                VendedoraDefecto = "Laura",
                PorcentajeDescuentoEfectivo = 10.0m,
                ComisionTarjetaDebito = 1.5m,
                ComisionTarjetaCredito = 4.5m,
                RecargoCuotasTarjetaCredito = 15.0m,
                TopeFiadoDefecto = 50000m,
                TopeMensualRetiroDueño = 600000m,
                TemaInterfaz = "Dark"
            });
        }

        // 6. Usuario Administrador Inicial
        if (!await context.Usuarios.AnyAsync())
        {
            var salt = PuntoDeVenta.Infrastructure.Services.AuthService.GenerarSalt();
            var hash = PuntoDeVenta.Infrastructure.Services.AuthService.HashearPassword("admin123", salt);
            context.Usuarios.Add(new PuntoDeVenta.Domain.Entities.Seguridad.Usuario
            {
                Username = "admin",
                NombreCompleto = "Administrador Principal",
                PasswordSalt = salt,
                PasswordHash = hash,
                Rol = PuntoDeVenta.Domain.Entities.Seguridad.RolUsuario.Administrador,
                Activo = true
            });
        }

        await context.SaveChangesAsync();
    }
}
