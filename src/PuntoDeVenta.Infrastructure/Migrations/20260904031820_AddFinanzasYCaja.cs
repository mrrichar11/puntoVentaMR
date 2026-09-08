using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanzasYCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasGasto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    TipoGasto = table.Column<int>(type: "INTEGER", nullable: false),
                    EsGastoPersonal = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasGasto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TurnosCaja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioApertura = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontoInicialEfectivo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCierre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    ObservacionesApertura = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ObservacionesCierre = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    MontoRealEfectivo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MontoTeoricoEfectivo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MontoRealTransferencias = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MontoTeoricoTransferencias = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MontoRealTarjetas = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MontoTeoricoTarjetas = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosCaja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gastos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TurnoCajaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoriaGastoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    NumeroComprobante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Canal = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPersonal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gastos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gastos_CategoriasGasto_CategoriaGastoId",
                        column: x => x.CategoriaGastoId,
                        principalTable: "CategoriasGasto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gastos_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCaja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TurnoCajaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Concepto = table.Column<int>(type: "INTEGER", nullable: false),
                    Canal = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ReferenciaComprobante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCaja_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasGasto_Nombre",
                table: "CategoriasGasto",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_CategoriaGastoId",
                table: "Gastos",
                column: "CategoriaGastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_TurnoCajaId",
                table: "Gastos",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_TurnoCajaId",
                table: "MovimientosCaja",
                column: "TurnoCajaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gastos");

            migrationBuilder.DropTable(
                name: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "CategoriasGasto");

            migrationBuilder.DropTable(
                name: "TurnosCaja");
        }
    }
}
