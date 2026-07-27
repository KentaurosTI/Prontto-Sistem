using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prontto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Parcelas",
                table: "cobrancas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "cobrancas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Parcelas",
                table: "cobrancas");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "cobrancas");
        }
    }
}
