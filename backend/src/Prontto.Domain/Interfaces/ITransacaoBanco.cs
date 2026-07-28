namespace Prontto.Domain.Interfaces;

/// <summary>
/// Transação de banco abstraída (mantém o EF Core fora do Domain/Application).
/// Como os repositórios compartilham o mesmo DbContext (scoped), a transação iniciada
/// por um repositório cobre os SaveChanges dos demais no mesmo escopo.
/// </summary>
public interface ITransacaoBanco : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
