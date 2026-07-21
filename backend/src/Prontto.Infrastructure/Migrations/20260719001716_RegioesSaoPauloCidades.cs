using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prontto.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegioesSaoPauloCidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Por enquanto a plataforma atende apenas o estado de São Paulo:
            // desativa cidades de outros estados (sem apagar, preservando integridade referencial).
            migrationBuilder.Sql("UPDATE cidades SET ativo = 0 WHERE estado <> 'SP';");
            migrationBuilder.Sql("UPDATE cidades SET ativo = 1 WHERE estado = 'SP';");

            // Adiciona as principais cidades/regiões de SP (Grande SP + interior/litoral).
            migrationBuilder.Sql(@"
INSERT INTO cidades (id, ativo, estado, nome, slug) VALUES
 ('c1000000-0000-0000-0000-000000000101', 1, 'SP', 'Guarulhos', 'guarulhos'),
 ('c1000000-0000-0000-0000-000000000102', 1, 'SP', 'Osasco', 'osasco'),
 ('c1000000-0000-0000-0000-000000000103', 1, 'SP', 'Santo André', 'santo-andre'),
 ('c1000000-0000-0000-0000-000000000104', 1, 'SP', 'São Bernardo do Campo', 'sao-bernardo-do-campo'),
 ('c1000000-0000-0000-0000-000000000105', 1, 'SP', 'São Caetano do Sul', 'sao-caetano-do-sul'),
 ('c1000000-0000-0000-0000-000000000106', 1, 'SP', 'Diadema', 'diadema'),
 ('c1000000-0000-0000-0000-000000000107', 1, 'SP', 'Mauá', 'maua'),
 ('c1000000-0000-0000-0000-000000000108', 1, 'SP', 'Barueri', 'barueri'),
 ('c1000000-0000-0000-0000-000000000109', 1, 'SP', 'Cotia', 'cotia'),
 ('c1000000-0000-0000-0000-00000000010a', 1, 'SP', 'Taboão da Serra', 'taboao-da-serra'),
 ('c1000000-0000-0000-0000-00000000010b', 1, 'SP', 'Carapicuíba', 'carapicuiba'),
 ('c1000000-0000-0000-0000-00000000010c', 1, 'SP', 'Itaquaquecetuba', 'itaquaquecetuba'),
 ('c1000000-0000-0000-0000-00000000010d', 1, 'SP', 'Suzano', 'suzano'),
 ('c1000000-0000-0000-0000-00000000010e', 1, 'SP', 'Mogi das Cruzes', 'mogi-das-cruzes'),
 ('c1000000-0000-0000-0000-00000000010f', 1, 'SP', 'Jundiaí', 'jundiai'),
 ('c1000000-0000-0000-0000-000000000110', 1, 'SP', 'Embu das Artes', 'embu-das-artes'),
 ('c1000000-0000-0000-0000-000000000111', 1, 'SP', 'Ferraz de Vasconcelos', 'ferraz-de-vasconcelos'),
 ('c1000000-0000-0000-0000-000000000112', 1, 'SP', 'Santana de Parnaíba', 'santana-de-parnaiba');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM cidades WHERE id IN (
 'c1000000-0000-0000-0000-000000000101','c1000000-0000-0000-0000-000000000102',
 'c1000000-0000-0000-0000-000000000103','c1000000-0000-0000-0000-000000000104',
 'c1000000-0000-0000-0000-000000000105','c1000000-0000-0000-0000-000000000106',
 'c1000000-0000-0000-0000-000000000107','c1000000-0000-0000-0000-000000000108',
 'c1000000-0000-0000-0000-000000000109','c1000000-0000-0000-0000-00000000010a',
 'c1000000-0000-0000-0000-00000000010b','c1000000-0000-0000-0000-00000000010c',
 'c1000000-0000-0000-0000-00000000010d','c1000000-0000-0000-0000-00000000010e',
 'c1000000-0000-0000-0000-00000000010f','c1000000-0000-0000-0000-000000000110',
 'c1000000-0000-0000-0000-000000000111','c1000000-0000-0000-0000-000000000112');");
            migrationBuilder.Sql("UPDATE cidades SET ativo = 1 WHERE estado <> 'SP';");
        }
    }
}
