using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Application.Servicos;

namespace Prontto.Application.Admin;

public interface IServicoAdmin
{
    Task<EstatisticasAdmin> ObterEstatisticasAsync();
    Task<IReadOnlyList<Usuario>> ListarUsuariosAsync(TipoConta? tipoConta = null, Guid? cidadeId = null);
    Task<Usuario> ObterUsuarioPorIdAsync(Guid id);
    Task BloquearUsuarioAsync(Guid id, Guid adminId);
    Task DesbloquearUsuarioAsync(Guid id, Guid adminId);
    Task RevogarSessoesAsync(Guid id, Guid adminId);
    Task<Usuario> AtualizarUsuarioAsync(Guid id, string? nome, string? telefone, Guid adminId);
    Task ExcluirUsuarioAsync(Guid id, Guid adminId);
    Task<IReadOnlyList<DtoServico>> ListarServicosAsync();
    Task<DtoServico> AtualizarStatusServicoAsync(Guid idServico, StatusServico novoStatus);
    Task<DtoServico> EditarServicoAsync(Guid idServico, string titulo, decimal preco, Guid adminId);
    Task ExcluirServicoAsync(Guid idServico, Guid adminId);
    Task<IReadOnlyList<MensagemServico>> ListarMensagensServicoAsync(Guid idServico);
    Task<MensagemServico> EnviarMensagemAsync(Guid idServico, Guid idRemetente, string conteudo);
    Task<IReadOnlyList<Cobranca>> ListarCobrancasAsync();
    Task<ResultadoPaginado<AuditLog>> ListarLogsAsync(int pagina, int tamanhoPagina, Guid? usuarioId, string? entidade);
    Task<ExtratoFinanceiro> ObterExtratoFinanceiroAsync();

    // ── Moderação de imagens ──────────────────────────────────────────────────
    Task<IReadOnlyList<DtoImagemPendente>> ListarImagensPendentesAsync();
    Task ModerarImagemAsync(Guid imagemId, bool aprovada, Guid adminId);
}

public record ResultadoPaginado<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina);

public record ExtratoFinanceiro(
    decimal TotalArrecadado,
    decimal TotalPendente,
    decimal TotalRetido,
    IReadOnlyList<Cobranca> UltimasCobrancas);

public record DtoImagemPendente(
    Guid Id,
    string Url,
    Guid PrestadorId,
    string NomePrestador,
    DateTime CriadoEm);
