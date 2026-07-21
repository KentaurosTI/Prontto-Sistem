using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioCidade
{
    Task<List<Cidade>> ListarAtivasAsync();
    Task<Cidade?> ObterPorSlugAsync(string slug);
    Task<List<Cidade>> ObterPorIdsAsync(IEnumerable<Guid> ids);
    Task<bool> ExisteAsync(Guid id);
}
