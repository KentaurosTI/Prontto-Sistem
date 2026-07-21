using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioAuditLog
{
    Task RegistrarAsync(AuditLog log);
    Task<(IReadOnlyList<AuditLog> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina, Guid? usuarioId, string? entidade);
}
