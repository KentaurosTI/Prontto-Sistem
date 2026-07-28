using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioSugestao(ContextoBancoDados db) : IRepositorioSugestao
{
    public async Task<SugestaoServico> AdicionarAsync(SugestaoServico sugestao)
    {
        db.SugestoesServico.Add(sugestao);
        await db.SaveChangesAsync();
        return sugestao;
    }

    public Task<List<SugestaoServico>> ListarAsync(int limite = 200)
        => db.SugestoesServico
            .OrderByDescending(s => s.CriadoEm)
            .Take(limite)
            .ToListAsync();
}
