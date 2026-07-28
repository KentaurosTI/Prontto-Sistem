using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prontto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaDescricaoImagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "categorias",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Imagem",
                table: "categorias",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000002"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000003"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000004"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000005"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000006"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000007"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000008"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000009"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000a"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "categorias",
                keyColumn: "id",
                keyValue: new Guid("c0000000-0000-0000-0000-00000000000b"),
                columns: new[] { "Descricao", "Imagem" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "categorias");

            migrationBuilder.DropColumn(
                name: "Imagem",
                table: "categorias");
        }
    }
}
