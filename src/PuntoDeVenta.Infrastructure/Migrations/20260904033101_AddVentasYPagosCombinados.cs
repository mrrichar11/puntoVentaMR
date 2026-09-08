using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVentasYPagosCombinados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroComprobante = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TurnoCajaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClienteNombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    ClienteDocumento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SubtotalLista = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalDescuentoOferta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalDescuentoMedioPago = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalRecargoMedioPago = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalFinalCobrado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CostoTotalHistorico = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosVenta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VentaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetodoPagoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Canal = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PorcentajeAjuste = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    MontoAjuste = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ComisionPorcentual = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosVenta_MetodosPago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "MetodosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagosVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineasVenta_VentaId",
                table: "LineasVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosVenta_MetodoPagoId",
                table: "PagosVenta",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosVenta_VentaId",
                table: "PagosVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_NumeroComprobante",
                table: "Ventas",
                column: "NumeroComprobante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TurnoCajaId",
                table: "Ventas",
                column: "TurnoCajaId");

            migrationBuilder.AddForeignKey(
                name: "FK_LineasVenta_Ventas_VentaId",
                table: "LineasVenta",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LineasVenta_Ventas_VentaId",
                table: "LineasVenta");

            migrationBuilder.DropTable(
                name: "PagosVenta");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_LineasVenta_VentaId",
                table: "LineasVenta");
        }
    }
}
