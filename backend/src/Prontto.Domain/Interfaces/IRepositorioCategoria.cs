using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioCategoria
{
    Task<List<Categoria>> ListarAtivasAsync();
    Task<Categoria?> ObterPorSlugAsync(string slug);
    Task<List<Categoria>> ObterPorIdsAsync(IEnumerable<Guid> ids);
}
