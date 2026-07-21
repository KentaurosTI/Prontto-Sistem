using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioMensagem
{
    Task<IReadOnlyList<MensagemServico>> ListarPorServicoAsync(Guid idServico);
    Task<IReadOnlyList<MensagemServico>> ListarPorServicoAsync(Guid servicoId, Guid? afterId, int limite);
    Task<MensagemServico?> ObterPropostaPendenteAsync(Guid servicoId);
    Task<MensagemServico> AdicionarAsync(MensagemServico mensagem);
    Task AtualizarAsync(MensagemServico mensagem);
}
