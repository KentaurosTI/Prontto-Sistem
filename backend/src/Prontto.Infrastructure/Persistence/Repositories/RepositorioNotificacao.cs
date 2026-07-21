using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioNotificacao(ContextoBancoDados db) : IRepositorioNotificacao
{
    public async Task AdicionarAsync(Notificacao notificacao)
    {
        db.Notificacoes.Add(notificacao);
        await db.SaveChangesAsync();
    }

    public async Task<List<Notificacao>> ListarPorUsuarioAsync(Guid usuarioId, bool apenasNaoLidas = false)
    {
        var query = db.Notificacoes
            .Where(n => n.UsuarioId == usuarioId);

        if (apenasNaoLidas)
            query = query.Where(n => !n.Lida);

        return await query
            .OrderByDescending(n => n.CriadoEm)
            .Take(50)
            .ToListAsync();
    }

    public Task<int> ContarNaoLidasAsync(Guid usuarioId)
        => db.Notificacoes.CountAsync(n => n.UsuarioId == usuarioId && !n.Lida);

    public async Task MarcarComoLidaAsync(Guid id, Guid usuarioId)
    {
        var notificacao = await db.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId);
        if (notificacao is null || notificacao.Lida) return;
        notificacao.Lida = true;
        await db.SaveChangesAsync();
    }

    public async Task MarcarTodasComoLidasAsync(Guid usuarioId)
    {
        var naoLidas = await db.Notificacoes
            .Where(n => n.UsuarioId == usuarioId && !n.Lida)
            .ToListAsync();
        if (naoLidas.Count == 0) return;
        foreach (var n in naoLidas) n.Lida = true;
        await db.SaveChangesAsync();
    }
}
