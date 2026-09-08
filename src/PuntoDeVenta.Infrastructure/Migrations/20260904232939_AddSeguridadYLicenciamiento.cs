using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeguridadYLicenciamiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Licencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoInstalacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Comercio = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClaveActivacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UltimaFechaUso = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstaActiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoIngreso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Username",
                table: "Usuarios",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Licencias");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
