using Prontto.Domain.Entities;
using Prontto.Domain.Enums;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioServico
{
    Task<Servico?> ObterPorIdAsync(Guid id);
    Task<Servico?> ObterPorIdComDetalhesAsync(Guid id);
    Task<IReadOnlyList<Servico>> ListarTodosAsync();
    Task<List<Servico>> ListarPorClienteAsync(Guid clienteId);
    Task<List<Servico>> ListarPorPrestadorAsync(Guid prestadorId);
    Task<List<Servico>> ListarDisponiveisParaPrestadorAsync(Guid prestadorId);
    Task<List<Servico>> ListarParaAutoConclusaoAsync();
    Task<int> ContarPorStatusAsync(StatusServico status);
    Task<int> ContarTodosAsync();
    Task<Servico> AdicionarAsync(Servico servico);
    Task<Servico> AtualizarAsync(Servico servico);
}
