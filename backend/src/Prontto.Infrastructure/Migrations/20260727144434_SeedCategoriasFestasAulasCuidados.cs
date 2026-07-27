using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Prontto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategoriasFestasAulasCuidados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "categorias",
                columns: new[] { "id", "ativo", "nome", "ordem_exibicao", "slug" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-00000000000c"), true, "Festas e Eventos", 12, "festas" },
                    { new Guid("c0000000-0000-0000-0000-00000000000d"), true, "Aulas Particulares", 13, "aulas" },
                    { new Guid("c0000000-0000-0000-0000-00000000000e"), true, "Cuidados e Pets", 14, "cuidados" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000c"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000d"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000e"));
        }
    }
}
