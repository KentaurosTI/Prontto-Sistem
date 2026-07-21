using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioAuditLog(ContextoBancoDados db) : IRepositorioAuditLog
{
    public async Task RegistrarAsync(AuditLog log)
    {
        db.LogsAuditoria.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<AuditLog> Itens, int Total)> ListarAsync(
        int pagina, int tamanhoPagina, Guid? usuarioId, string? entidade)
    {
        var query = db.LogsAuditoria
            .Where(log => usuarioId == null || log.UsuarioId == usuarioId)
            .Where(log => entidade == null || log.Entidade == entidade)
            .OrderByDescending(log => log.CriadoEm);

        var total = await query.CountAsync();

        var itens = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return (itens, total);
    }
}
