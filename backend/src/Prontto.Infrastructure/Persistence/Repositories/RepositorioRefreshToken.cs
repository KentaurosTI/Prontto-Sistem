using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioRefreshToken(ContextoBancoDados db) : IRepositorioRefreshToken
{
    public Task<RefreshToken?> ObterPorHashAsync(string hash)
        => db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hash);

    public async Task AdicionarAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public async Task AtualizarAsync(RefreshToken token)
    {
        db.RefreshTokens.Update(token);
        await db.SaveChangesAsync();
    }
}
