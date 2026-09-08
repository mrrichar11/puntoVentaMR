using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionYAnaliticas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CanalVenta",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NroPedidoWeb",
                table: "Ventas",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vendedora",
                table: "Ventas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Configuraciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NombreComercio = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cuit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    VendedoraDefecto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PorcentajeDescuentoEfectivo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ComisionTarjetaDebito = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ComisionTarjetaCredito = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RecargoCuotasTarjetaCredito = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TopeFiadoDefecto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TopeMensualRetiroDueño = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TemaInterfaz = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuraciones", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuraciones");

            migrationBuilder.DropColumn(
                name: "CanalVenta",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "NroPedidoWeb",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Vendedora",
                table: "Ventas");
        }
    }
}
