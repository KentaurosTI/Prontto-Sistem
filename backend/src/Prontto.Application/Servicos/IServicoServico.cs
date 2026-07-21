using Prontto.Domain.Enums;

namespace Prontto.Application.Servicos;

public interface IServicoServico
{
    Task<DtoServico> CriarSolicitacaoAsync(Guid clienteId, ComandoCriarServico comando);
    Task<DtoServico> ObterPorIdAsync(Guid servicoId, Guid usuarioId);
    Task<List<DtoServico>> ListarMeusServicosAsync(Guid usuarioId, TipoConta tipoConta);
    Task<List<DtoServico>> ListarDisponiveisParaPrestadorAsync(Guid prestadorId);
    Task<DtoServico> VincularPrestadorAsync(Guid servicoId, Guid prestadorId);
    Task<DtoServico> MarcarConcluidoAsync(Guid servicoId, Guid prestadorId);
    Task<DtoServico> ConfirmarConclusaoAsync(Guid servicoId, Guid clienteId);
    Task<DtoServico> CancelarAsync(Guid servicoId, Guid atualUsuarioId, Papel papel, string? motivo = null);
    Task AutoConcluirServicosAsync();
}
