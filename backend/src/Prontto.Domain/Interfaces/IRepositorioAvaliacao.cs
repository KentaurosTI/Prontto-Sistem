using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioAvaliacao
{
    Task AdicionarAsync(Avaliacao avaliacao);
    Task<bool> ExisteAvaliacaoAsync(Guid servicoId, Guid avaliadorId);
    Task<(IEnumerable<Avaliacao> Items, int Total)> ListarPorAvaliadoAsync(Guid avaliadoId, int page, int pageSize);
    Task<IEnumerable<Avaliacao>> ListarPorServicoAsync(Guid servicoId);
    Task<(decimal Media, int Total)> CalcularMediaAsync(Guid avaliadoId);

    /// <summary>Retorna as avaliações mais recentes globalmente com comentário e nota alta.</summary>
    Task<List<Avaliacao>> ListarRecentesGlobaisAsync(int limite);
}
