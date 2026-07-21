using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioAvaliacao(ContextoBancoDados contexto) : IRepositorioAvaliacao
{
    public async Task AdicionarAsync(Avaliacao avaliacao)
    {
        await contexto.Avaliacoes.AddAsync(avaliacao);
        await contexto.SaveChangesAsync();
    }

    public async Task<bool> ExisteAvaliacaoAsync(Guid servicoId, Guid avaliadorId)
        => await contexto.Avaliacoes
            .AnyAsync(a => a.ServicoId == servicoId && a.AvaliadorId == avaliadorId);

    public async Task<(IEnumerable<Avaliacao> Items, int Total)> ListarPorAvaliadoAsync(
        Guid avaliadoId, int page, int pageSize)
    {
        var query = contexto.Avaliacoes
            .Include(a => a.Avaliador)
            .Where(a => a.AvaliadoId == avaliadoId)
            .OrderByDescending(a => a.CriadoEm)
            .AsNoTracking();

        var total = await query.CountAsync();
        var itens = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (itens, total);
    }

    public async Task<IEnumerable<Avaliacao>> ListarPorServicoAsync(Guid servicoId)
        => await contexto.Avaliacoes
            .Include(a => a.Avaliador)
            .Where(a => a.ServicoId == servicoId)
            .OrderByDescending(a => a.CriadoEm)
            .AsNoTracking()
            .ToListAsync();

    public async Task<(decimal Media, int Total)> CalcularMediaAsync(Guid avaliadoId)
    {
        var avaliacoes = await contexto.Avaliacoes
            .Where(a => a.AvaliadoId == avaliadoId)
            .AsNoTracking()
            .ToListAsync();

        if (avaliacoes.Count == 0)
            return (0m, 0);

        var media = (decimal)avaliacoes.Average(a => a.Nota);
        return (Math.Round(media, 2), avaliacoes.Count);
    }

    public async Task<List<Avaliacao>> ListarRecentesGlobaisAsync(int limite)
        => await contexto.Avaliacoes
            .Include(a => a.Avaliador)
            .Include(a => a.Servico)
            .Where(a => !string.IsNullOrEmpty(a.Comentario) && a.Nota >= 4)
            .OrderByDescending(a => a.CriadoEm)
            .Take(limite)
            .AsNoTracking()
            .ToListAsync();
}
