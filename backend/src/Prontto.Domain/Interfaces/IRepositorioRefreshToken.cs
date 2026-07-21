using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioRefreshToken
{
    Task<RefreshToken?> ObterPorHashAsync(string hash);
    Task AdicionarAsync(RefreshToken token);
    Task AtualizarAsync(RefreshToken token);
}
