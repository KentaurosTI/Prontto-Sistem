using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioCategoria(ContextoBancoDados db) : IRepositorioCategoria
{
    public Task<List<Categoria>> ListarAtivasAsync()
        => db.Categorias
            .Where(c => c.Ativa)
            .OrderBy(c => c.Ordem)
            .ToListAsync();

    public Task<Categoria?> ObterPorSlugAsync(string slug)
        => db.Categorias.FirstOrDefaultAsync(c => c.Slug == slug);

    public Task<List<Categoria>> ObterPorIdsAsync(IEnumerable<Guid> ids)
        => db.Categorias
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();
}
