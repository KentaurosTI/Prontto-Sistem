using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioCidade(ContextoBancoDados db) : IRepositorioCidade
{
    public Task<List<Cidade>> ListarAtivasAsync()
        => db.Cidades
            .Where(c => c.Ativa)
            .OrderBy(c => c.Nome)
            .ToListAsync();

    public Task<Cidade?> ObterPorSlugAsync(string slug)
        => db.Cidades.FirstOrDefaultAsync(c => c.Slug == slug);

    public Task<List<Cidade>> ObterPorIdsAsync(IEnumerable<Guid> ids)
        => db.Cidades
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

    public Task<bool> ExisteAsync(Guid id)
        => db.Cidades.AnyAsync(c => c.Id == id && c.Ativa);
}
