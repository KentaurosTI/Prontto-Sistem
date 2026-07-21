using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Prontto.Infrastructure.Persistence.Context;

/// <summary>
/// Factory de design-time para geração de migrations sem necessidade de conexão ativa.
/// Usado exclusivamente pelo dotnet-ef — não é registrado no DI de produção.
/// </summary>
public class ContextoBancoDadosFactory : IDesignTimeDbContextFactory<ContextoBancoDados>
{
    public ContextoBancoDados CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<ContextoBancoDados>();

        // Versão do MySQL usada no Hostinger — MySQL 8.0.x
        // ServerVersion estático para design-time: evita conexão ao banco durante migrations
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        opcoes.UseMySql(
            "Server=localhost;Database=prontto_design;User=root;Password=root;",
            serverVersion);

        return new ContextoBancoDados(opcoes.Options);
    }
}
