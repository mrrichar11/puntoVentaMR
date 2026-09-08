using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PuntoDeVenta.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetodosPago",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PorcentajeAjuste = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    ComisionPorcentual = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                    PlazoAcreditacionDias = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiereReferencia = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodosPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoEstilo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Temporada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Genero = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MarcaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articulos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Articulos_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VariantesArticulo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArticuloId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CodigoBarras = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Talle = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrecioLista = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrecioOferta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    PermiteDescuentoMedioPago = table.Column<bool>(type: "INTEGER", nullable: false),
                    StockActual = table.Column<int>(type: "INTEGER", nullable: false),
                    StockMinimo = table.Column<int>(type: "INTEGER", nullable: false),
                    Ubicacion = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantesArticulo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantesArticulo_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasVenta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VentaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VarianteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DescripcionArticulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Talle = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioListaUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DescuentoOfertaUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DescuentoMedioPagoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RecargoMedioPagoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrecioFinalCobrado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CostoUnitarioHistorico = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasVenta_VariantesArticulo_VarianteId",
                        column: x => x.VarianteId,
                        principalTable: "VariantesArticulo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VarianteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    StockPrevio = table.Column<int>(type: "INTEGER", nullable: false),
                    StockResultante = table.Column<int>(type: "INTEGER", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    ReferenciaDocumento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_VariantesArticulo_VarianteId",
                        column: x => x.VarianteId,
                        principalTable: "VariantesArticulo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_CategoriaId",
                table: "Articulos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_CodigoEstilo",
                table: "Articulos",
                column: "CodigoEstilo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_MarcaId",
                table: "Articulos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasVenta_VarianteId",
                table: "LineasVenta",
                column: "VarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre",
                table: "Marcas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetodosPago_Nombre",
                table: "MetodosPago",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_VarianteId",
                table: "MovimientosStock",
                column: "VarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantesArticulo_ArticuloId",
                table: "VariantesArticulo",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantesArticulo_CodigoBarras",
                table: "VariantesArticulo",
                column: "CodigoBarras",
                unique: true,
                filter: "CodigoBarras IS NOT NULL AND CodigoBarras <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_VariantesArticulo_SKU",
                table: "VariantesArticulo",
                column: "SKU",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasVenta");

            migrationBuilder.DropTable(
                name: "MetodosPago");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "VariantesArticulo");

            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Marcas");
        }
    }
}
