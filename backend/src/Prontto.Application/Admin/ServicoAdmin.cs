using Microsoft.Extensions.Caching.Memory;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Prontto.Application.Common;
using Prontto.Application.Servicos;

namespace Prontto.Application.Admin;

public class ServicoAdmin(
    IRepositorioUsuario repositorioUsuarios,
    IRepositorioServico repositorioServicos,
    IRepositorioCobranca repositorioCobrancas,
    IRepositorioMensagem repositorioMensagens,
    IRepositorioAuditLog repositorioAuditLog,
    IRepositorioImagemPortfolio repositorioImagens,
    IRepositorioNotificacao repositorioNotificacoes,
    IMemoryCache cache) : IServicoAdmin
{
    private const string ChaveCacheEstatisticas = "admin:stats";

    public Task<EstatisticasAdmin> ObterEstatisticasAsync()
        => cache.GetOrCreateAsync(ChaveCacheEstatisticas, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return BuscarEstatisticasNoBancoAsync();
        })!;

    private async Task<EstatisticasAdmin> BuscarEstatisticasNoBancoAsync()
    {
        var todosUsuarios = await repositorioUsuarios.ListarNaoAdminsAsync();
        var totalServicos = await repositorioServicos.ContarTodosAsync();

        return new EstatisticasAdmin(
            Usuarios: new EstatisticasUsuarios(
                Total: todosUsuarios.Count,
                Clientes: todosUsuarios.Count(u => u.TipoConta == TipoConta.Cliente),
                Prestadores: todosUsuarios.Count(u => u.TipoConta == TipoConta.Prestador)
            ),
            Servicos: new EstatisticasServicos(
                Total: totalServicos,
                Pendentes: await repositorioServicos.ContarPorStatusAsync(StatusServico.EmNegociacao),
                EmAndamento: await repositorioServicos.ContarPorStatusAsync(StatusServico.EmAndamento),
                Concluidos: await repositorioServicos.ContarPorStatusAsync(StatusServico.Concluido)
            ),
            Receita: new EstatisticasReceita(
                Ganha: await repositorioCobrancas.SomarTaxaAdminPorStatusAsync(StatusCobranca.Pago),
                Pendente: await repositorioCobrancas.SomarTaxaAdminPorStatusAsync(StatusCobranca.Pendente),
                Gmv: await repositorioCobrancas.SomarValorTotalPorStatusAsync(StatusCobranca.Pago)
            )
        );
    }

    public async Task<IReadOnlyList<Usuario>> ListarUsuariosAsync(TipoConta? tipoConta = null, Guid? cidadeId = null)
        => await repositorioUsuarios.ListarNaoAdminsAsync(tipoConta, cidadeId);

    public async Task<Usuario> ObterUsuarioPorIdAsync(Guid id)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");
        return usuario;
    }

    public async Task BloquearUsuarioAsync(Guid id, Guid adminId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        if (usuario.Papel == Papel.Admin)
            throw new ExcecaoConflito("Não é permitido bloquear um administrador");

        usuario.DeletadoEm = DateTime.UtcNow;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await repositorioUsuarios.AtualizarAsync(usuario);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.usuario.bloqueado",
            Entidade = "Usuario",
            EntidadeId = id.ToString(),
            Detalhes = $"Usuário {usuario.Email} bloqueado pelo admin {adminId}",
        });
    }

    public async Task DesbloquearUsuarioAsync(Guid id, Guid adminId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        usuario.DeletadoEm = null;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await repositorioUsuarios.AtualizarAsync(usuario);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.usuario.desbloqueado",
            Entidade = "Usuario",
            EntidadeId = id.ToString(),
            Detalhes = $"Usuário {usuario.Email} desbloqueado pelo admin {adminId}",
        });
    }

    public async Task RevogarSessoesAsync(Guid id, Guid adminId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        await repositorioUsuarios.RevogarTodosTokensPorUsuarioAsync(id);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.usuario.sessoes_revogadas",
            Entidade = "Usuario",
            EntidadeId = id.ToString(),
            Detalhes = $"Sessões do usuário {usuario.Email} revogadas pelo admin {adminId}",
        });
    }

    public async Task<Usuario> AtualizarUsuarioAsync(Guid id, string? nome, string? telefone, Guid adminId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        if (usuario.Papel == Papel.Admin)
            throw new ExcecaoConflito("Não é permitido editar um administrador");

        if (!string.IsNullOrWhiteSpace(nome)) usuario.Nome = nome.Trim();
        if (telefone is not null) usuario.Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        usuario.AtualizadoEm = DateTime.UtcNow;
        var atualizado = await repositorioUsuarios.AtualizarAsync(usuario);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.usuario.editado",
            Entidade = "Usuario",
            EntidadeId = id.ToString(),
            Detalhes = $"Usuário {usuario.Email} editado pelo admin {adminId}",
        });
        return atualizado;
    }

    public async Task ExcluirUsuarioAsync(Guid id, Guid adminId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        if (usuario.Papel == Papel.Admin)
            throw new ExcecaoConflito("Não é permitido excluir um administrador");

        usuario.DeletadoEm = DateTime.UtcNow;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await repositorioUsuarios.AtualizarAsync(usuario);
        await repositorioUsuarios.RevogarTodosTokensPorUsuarioAsync(id);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.usuario.excluido",
            Entidade = "Usuario",
            EntidadeId = id.ToString(),
            Detalhes = $"Usuário {usuario.Email} excluído pelo admin {adminId}",
        });
    }

    public async Task<IReadOnlyList<DtoServico>> ListarServicosAsync()
    {
        var servicos = await repositorioServicos.ListarTodosAsync();
        return servicos.Select(ServicoServico.MapearDto).ToList();
    }

    public async Task<DtoServico> EditarServicoAsync(Guid idServico, string titulo, decimal preco, Guid adminId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(idServico)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (!string.IsNullOrWhiteSpace(titulo)) servico.Titulo = titulo.Trim();
        if (preco >= 0) servico.Preco = preco;
        servico.AtualizadoEm = DateTime.UtcNow;
        var atualizado = await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.servico.editado",
            Entidade = "Servico",
            EntidadeId = idServico.ToString(),
            Detalhes = $"Serviço '{servico.Titulo}' editado pelo admin {adminId}",
        });
        return ServicoServico.MapearDto(atualizado);
    }

    public async Task ExcluirServicoAsync(Guid idServico, Guid adminId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(idServico)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        servico.DeletadoEm = DateTime.UtcNow;
        servico.AtualizadoEm = DateTime.UtcNow;
        await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "admin.servico.excluido",
            Entidade = "Servico",
            EntidadeId = idServico.ToString(),
            Detalhes = $"Serviço '{servico.Titulo}' excluído pelo admin {adminId}",
        });
    }

    public async Task<DtoServico> AtualizarStatusServicoAsync(Guid idServico, StatusServico novoStatus)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(idServico)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        servico.Status = novoStatus;
        servico.AtualizadoEm = DateTime.UtcNow;

        if (novoStatus == StatusServico.Concluido)
        {
            servico.ConcluidoEm = DateTime.UtcNow;

            if (!await repositorioCobrancas.ExistePorServicoAsync(idServico))
            {
                var taxaAdmin = Math.Round(servico.Preco * servico.TaxaAdminRate, 2);
                var valorPrestador = servico.Preco - taxaAdmin;

                await repositorioCobrancas.AdicionarAsync(new Cobranca
                {
                    ServicoId = idServico,
                    ValorTotal = servico.Preco,
                    TaxaAdmin = taxaAdmin,
                    ValorPrestador = valorPrestador,
                    Status = StatusCobranca.Pago,
                    PagadoEm = DateTime.UtcNow,
                });
            }
        }

        var servicoAtualizado = await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = null,
            Acao = "admin.servico.status_alterado",
            Entidade = "Servico",
            EntidadeId = idServico.ToString(),
            Detalhes = $"Status alterado para {novoStatus}",
        });

        return ServicoServico.MapearDto(servicoAtualizado);
    }

    public async Task<IReadOnlyList<MensagemServico>> ListarMensagensServicoAsync(Guid idServico)
        => await repositorioMensagens.ListarPorServicoAsync(idServico);

    public async Task<MensagemServico> EnviarMensagemAsync(Guid idServico, Guid idRemetente, string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new ExcecaoValidacao("Mensagem vazia");

        return await repositorioMensagens.AdicionarAsync(new MensagemServico
        {
            ServicoId = idServico,
            RemetenteId = idRemetente,
            PapelRemetente = PapelRemetente.Admin,
            Conteudo = conteudo.Trim(),
        });
    }

    public async Task<IReadOnlyList<Cobranca>> ListarCobrancasAsync()
        => await repositorioCobrancas.ListarTodosAsync();

    public async Task<ResultadoPaginado<AuditLog>> ListarLogsAsync(
        int pagina, int tamanhoPagina, Guid? usuarioId, string? entidade)
    {
        tamanhoPagina = Math.Min(tamanhoPagina, 100);
        var (itens, total) = await repositorioAuditLog.ListarAsync(pagina, tamanhoPagina, usuarioId, entidade);
        return new ResultadoPaginado<AuditLog>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<ExtratoFinanceiro> ObterExtratoFinanceiroAsync()
    {
        var totalArrecadado = await repositorioCobrancas.SomarTaxaAdminPorStatusAsync(StatusCobranca.Liberado);
        var totalPendente = await repositorioCobrancas.SomarTaxaAdminPorStatusAsync(StatusCobranca.Pendente);
        var totalRetido = await repositorioCobrancas.SomarTaxaAdminPorStatusAsync(StatusCobranca.Retido);
        var ultimasCobrancas = await repositorioCobrancas.ListarUltimasComDetalhesAsync(20);

        return new ExtratoFinanceiro(totalArrecadado, totalPendente, totalRetido, ultimasCobrancas);
    }

    // ── Moderação de imagens ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<DtoImagemPendente>> ListarImagensPendentesAsync()
    {
        var imagens = await repositorioImagens.ListarPendentesModeracaoAsync();
        return imagens
            .Select(i => new DtoImagemPendente(
                Id: i.Id,
                Url: i.Url,
                PrestadorId: i.UsuarioId,
                NomePrestador: i.Usuario?.Nome ?? string.Empty,
                CriadoEm: i.CriadoEm))
            .ToList();
    }

    public async Task ModerarImagemAsync(Guid imagemId, bool aprovada, Guid adminId)
    {
        var imagem = await repositorioImagens.ObterPorIdAsync(imagemId)
            ?? throw new ExcecaoNaoEncontrado("Imagem não encontrada");

        imagem.Aprovada = aprovada;
        imagem.Moderada = true;
        await repositorioImagens.AtualizarAsync(imagem);

        var titulo = aprovada ? "Imagem aprovada" : "Imagem rejeitada";
        var mensagem = aprovada
            ? "Sua imagem de portfólio foi aprovada e já está visível no seu perfil."
            : "Sua imagem de portfólio foi rejeitada pela moderação e não será exibida no seu perfil.";

        await repositorioNotificacoes.AdicionarAsync(new Notificacao
        {
            UsuarioId = imagem.UsuarioId,
            Titulo = titulo,
            Mensagem = mensagem,
            Tipo = "moderacao_imagem",
            ReferenciaId = imagemId.ToString(),
        });

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = aprovada ? "admin.imagem.aprovada" : "admin.imagem.rejeitada",
            Entidade = "ImagemPortfolio",
            EntidadeId = imagemId.ToString(),
            Detalhes = $"Imagem {imagemId} do prestador {imagem.UsuarioId} {(aprovada ? "aprovada" : "rejeitada")} pelo admin {adminId}",
        });
    }
}
