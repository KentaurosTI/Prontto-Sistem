using Prontto.Domain.Enums;

namespace Prontto.Application.Servicos;

public interface IServicoNegociacao
{
    Task<DtoMensagemServico> EnviarPropostaAsync(Guid servicoId, Guid remetenteId, PapelRemetente papel, decimal valor);
    Task<DtoServico> AceitarPropostaAsync(Guid servicoId, Guid mensagemId, Guid usuarioId);
    Task<DtoMensagemServico> EnviarMensagemTextoAsync(Guid servicoId, Guid remetenteId, PapelRemetente papel, string conteudo);
    Task<List<DtoMensagemServico>> ListarMensagensAsync(Guid servicoId, Guid usuarioId);
    Task<ResultadoMensagensPaginadas> ListarMensagensPaginadasAsync(Guid servicoId, Guid usuarioId, Guid? afterId, int limite);
}
