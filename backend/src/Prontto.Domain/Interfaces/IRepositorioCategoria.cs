using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioCategoria
{
    Task<List<Categoria>> ListarAtivasAsync();
    Task<Categoria?> ObterPorSlugAsync(string slug);
    Task<List<Categoria>> ObterPorIdsAsync(IEnumerable<Guid> ids);

    // Administração do catálogo
    Task<List<Categoria>> ListarTodasAsync();
    Task<Categoria?> ObterPorIdAsync(Guid id);
    Task<Categoria> AdicionarAsync(Categoria categoria);
    Task AtualizarAsync(Categoria categoria);
    Task RemoverAsync(Categoria categoria);
}
