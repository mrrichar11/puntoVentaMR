using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMargenSugeridoConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostosBancariosEstimados",
                table: "Configuraciones",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 5.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MargenGananciaSugerido",
                table: "Configuraciones",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 80.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostosBancariosEstimados",
                table: "Configuraciones");

            migrationBuilder.DropColumn(
                name: "MargenGananciaSugerido",
                table: "Configuraciones");
        }
    }
}
