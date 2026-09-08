using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientesYProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Ventas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DocumentoIdentidad = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LimiteCredito = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SaldoDeudorActual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaUltimoMovimiento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NombreContacto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Cuit = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlazoPagoDiasDefecto = table.Column<int>(type: "INTEGER", nullable: false),
                    SaldoDeudorActual = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCuentaCorrienteClientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VentaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TurnoCajaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SaldoPrevio = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SaldoResultante = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CanalCobro = table.Column<int>(type: "INTEGER", nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ReferenciaComprobante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCuentaCorrienteClientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCuentaCorrienteClientes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientosCuentaCorrienteClientes_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimientosCuentaCorrienteClientes_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComprasProveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroComprobante = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProveedorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TurnoCajaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaVencimientoPlazo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Condicion = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCompra = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalPagado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasProveedores_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComprasProveedores_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LineasCompraProveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompraProveedorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VarianteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    CostoUnitarioCompra = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasCompraProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasCompraProveedores_ComprasProveedores_CompraProveedorId",
                        column: x => x.CompraProveedorId,
                        principalTable: "ComprasProveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineasCompraProveedores_VariantesArticulo_VarianteId",
                        column: x => x.VarianteId,
                        principalTable: "VariantesArticulo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosCompraProveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompraProveedorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TurnoCajaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Canal = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenciaComprobante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosCompraProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosCompraProveedores_ComprasProveedores_CompraProveedorId",
                        column: x => x.CompraProveedorId,
                        principalTable: "ComprasProveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagosCompraProveedores_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_DocumentoIdentidad",
                table: "Clientes",
                column: "DocumentoIdentidad");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NombreCompleto",
                table: "Clientes",
                column: "NombreCompleto");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProveedores_ProveedorId",
                table: "ComprasProveedores",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProveedores_TurnoCajaId",
                table: "ComprasProveedores",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasCompraProveedores_CompraProveedorId",
                table: "LineasCompraProveedores",
                column: "CompraProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasCompraProveedores_VarianteId",
                table: "LineasCompraProveedores",
                column: "VarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuentaCorrienteClientes_ClienteId",
                table: "MovimientosCuentaCorrienteClientes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuentaCorrienteClientes_TurnoCajaId",
                table: "MovimientosCuentaCorrienteClientes",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuentaCorrienteClientes_VentaId",
                table: "MovimientosCuentaCorrienteClientes",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosCompraProveedores_CompraProveedorId",
                table: "PagosCompraProveedores",
                column: "CompraProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosCompraProveedores_TurnoCajaId",
                table: "PagosCompraProveedores",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_RazonSocial",
                table: "Proveedores",
                column: "RazonSocial");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "LineasCompraProveedores");

            migrationBuilder.DropTable(
                name: "MovimientosCuentaCorrienteClientes");

            migrationBuilder.DropTable(
                name: "PagosCompraProveedores");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "ComprasProveedores");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ClienteId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Ventas");
        }
    }
}
