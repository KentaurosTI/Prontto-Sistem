using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioImagemPortfolio(ContextoBancoDados db) : IRepositorioImagemPortfolio
{
    public async Task<IReadOnlyList<ImagemPortfolio>> ListarPendentesModeracaoAsync()
        => await db.ImagensPortfolio
            .Include(i => i.Usuario)
            .Where(i => i.Aprovada == null && i.DeletadoEm == null)
            .OrderBy(i => i.CriadoEm)
            .AsNoTracking()
            .ToListAsync();

    public async Task<ImagemPortfolio?> ObterPorIdAsync(Guid id)
        => await db.ImagensPortfolio
            .Include(i => i.Usuario)
            .FirstOrDefaultAsync(i => i.Id == id && i.DeletadoEm == null);

    public async Task AtualizarAsync(ImagemPortfolio imagem)
    {
        db.ImagensPortfolio.Update(imagem);
        await db.SaveChangesAsync();
    }
}
