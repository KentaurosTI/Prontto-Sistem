using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioDisputa(ContextoBancoDados db) : IRepositorioDisputa
{
    public Task<Disputa?> ObterPorServicoIdAsync(Guid servicoId)
        => db.Disputas
            .Include(d => d.AbertaPor)
            .Include(d => d.ResolvidaPor)
            .FirstOrDefaultAsync(d => d.ServicoId == servicoId);

    public Task<Disputa?> ObterPorIdAsync(Guid id)
        => db.Disputas
            .Include(d => d.AbertaPor)
            .Include(d => d.ResolvidaPor)
            .Include(d => d.Servico)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<List<Disputa>> ListarAbertasAsync()
        => await db.Disputas
            .Include(d => d.Servico)
                .ThenInclude(s => s!.Cliente)
            .Include(d => d.Servico)
                .ThenInclude(s => s!.Prestador)
            .Include(d => d.AbertaPor)
            .Where(d =>
                d.Status == StatusDisputa.Aberta ||
                d.Status == StatusDisputa.EmAnalise)
            .OrderBy(d => d.CriadoEm)
            .ToListAsync();

    public async Task<Disputa> AdicionarAsync(Disputa disputa)
    {
        db.Disputas.Add(disputa);
        await db.SaveChangesAsync();
        return disputa;
    }

    public async Task<Disputa> AtualizarAsync(Disputa disputa)
    {
        db.Disputas.Update(disputa);
        await db.SaveChangesAsync();
        return disputa;
    }
}
