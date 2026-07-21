using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Prontto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogoCategoriasCidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "categorias",
                columns: new[] { "id", "ativo", "nome", "ordem_exibicao", "slug" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000001"), true, "Reformas e Reparos", 1, "reformas" },
                    { new Guid("c0000000-0000-0000-0000-000000000002"), true, "Pintura", 2, "pintura" },
                    { new Guid("c0000000-0000-0000-0000-000000000003"), true, "Limpeza", 3, "limpeza" },
                    { new Guid("c0000000-0000-0000-0000-000000000004"), true, "Climatização", 4, "clima" },
                    { new Guid("c0000000-0000-0000-0000-000000000005"), true, "Jardinagem", 5, "jardim" },
                    { new Guid("c0000000-0000-0000-0000-000000000006"), true, "Montagem e Móveis", 6, "montagem" },
                    { new Guid("c0000000-0000-0000-0000-000000000007"), true, "Mudança", 7, "mudanca" },
                    { new Guid("c0000000-0000-0000-0000-000000000008"), true, "Assistência Técnica", 8, "assistencia" },
                    { new Guid("c0000000-0000-0000-0000-000000000009"), true, "Segurança", 9, "seguranca" },
                    { new Guid("c0000000-0000-0000-0000-00000000000a"), true, "Serralheria", 10, "serralheria" },
                    { new Guid("c0000000-0000-0000-0000-00000000000b"), true, "Autos", 11, "autos" }
                });

            migrationBuilder.InsertData(
                table: "cidades",
                columns: new[] { "id", "ativo", "estado", "nome", "slug" },
                values: new object[,]
                {
                    { new Guid("c1000000-0000-0000-0000-000000000001"), true, "SP", "São Paulo", "sao-paulo" },
                    { new Guid("c1000000-0000-0000-0000-000000000002"), true, "RJ", "Rio de Janeiro", "rio-de-janeiro" },
                    { new Guid("c1000000-0000-0000-0000-000000000003"), true, "MG", "Belo Horizonte", "belo-horizonte" },
                    { new Guid("c1000000-0000-0000-0000-000000000004"), true, "PR", "Curitiba", "curitiba" },
                    { new Guid("c1000000-0000-0000-0000-000000000005"), true, "RS", "Porto Alegre", "porto-alegre" },
                    { new Guid("c1000000-0000-0000-0000-000000000006"), true, "DF", "Brasília", "brasilia" },
                    { new Guid("c1000000-0000-0000-0000-000000000007"), true, "BA", "Salvador", "salvador" },
                    { new Guid("c1000000-0000-0000-0000-000000000008"), true, "PE", "Recife", "recife" },
                    { new Guid("c1000000-0000-0000-0000-000000000009"), true, "CE", "Fortaleza", "fortaleza" },
                    { new Guid("c1000000-0000-0000-0000-00000000000a"), true, "SP", "Campinas", "campinas" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000a"));

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000b"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "cidades",
                keyColumn: "id",
                keyValue: new Guid("c1000000-0000-0000-0000-00000000000a"));
        }
    }
}
