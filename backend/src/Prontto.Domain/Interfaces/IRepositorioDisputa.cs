using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioDisputa
{
    Task<Disputa?> ObterPorServicoIdAsync(Guid servicoId);
    Task<Disputa?> ObterPorIdAsync(Guid id);
    Task<List<Disputa>> ListarAbertasAsync();
    Task<Disputa> AdicionarAsync(Disputa disputa);
    Task<Disputa> AtualizarAsync(Disputa disputa);
}
