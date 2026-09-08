using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoPlanLicencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoPlan",
                table: "Licencias",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoPlan",
                table: "Licencias");
        }
    }
}
