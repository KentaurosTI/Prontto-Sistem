using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Servicos;

public class ServicoServico(
    IRepositorioServico repositorioServicos,
    IRepositorioNotificacao repositorioNotificacoes,
    IRepositorioAuditLog repositorioAuditLog,
    IRepositorioPerfilPrestador repositorioPerfil,
    IServicoFinanceiro servicoFinanceiro) : IServicoServico
{
    // ── Criação ────────────────────────────────────────────────────────────────

    public async Task<DtoServico> CriarSolicitacaoAsync(Guid clienteId, ComandoCriarServico comando)
    {
        if (string.IsNullOrWhiteSpace(comando.Titulo))
            throw new ExcecaoValidacao("Título é obrigatório");

        if (comando.CategoriaId == Guid.Empty)
            throw new ExcecaoValidacao("CategoriaId é obrigatório");

        // Matching por proximidade (nível cidade): se a solicitação é direcionada a um
        // prestador específico e tem cidade, o prestador precisa atender aquela cidade.
        if (comando.PrestadorId.HasValue && comando.CidadeId.HasValue)
        {
            var atende = await repositorioPerfil.AtendeCidadeAsync(
                comando.PrestadorId.Value, comando.CidadeId.Value);
            if (!atende)
                throw new ExcecaoValidacao(
                    "O profissional escolhido não atende a cidade informada. Selecione um profissional da sua região.");
        }

        var servico = new Servico
        {
            Titulo = comando.Titulo.Trim(),
            Descricao = comando.Descricao?.Trim(),
            CategoriaId = comando.CategoriaId,
            CidadeId = comando.CidadeId,
            ClienteId = clienteId,
            PrestadorId = comando.PrestadorId,
            Status = StatusServico.EmNegociacao,
            Endereco = comando.Endereco?.Trim(),
            AgendadoEm = comando.AgendadoEm,
            Preco = 0m,
            TaxaAdminRate = 0.2000m,
        };

        var criado = await repositorioServicos.AdicionarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = clienteId,
            Acao = "servico.criado",
            Entidade = "Servico",
            EntidadeId = criado.Id.ToString(),
            Detalhes = $"{{\"titulo\":\"{criado.Titulo}\",\"categoriaId\":\"{criado.CategoriaId}\"}}"
        });

        // Notifica o prestador se foi criado vinculado a um específico
        if (comando.PrestadorId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = comando.PrestadorId.Value,
                Titulo = "Nova solicitação de serviço",
                Mensagem = $"Você recebeu uma nova solicitação: {criado.Titulo}",
                Tipo = "servico",
                ReferenciaId = criado.Id.ToString()
            });
        }

        return await CarregarDtoAsync(criado.Id, clienteId);
    }

    // ── Consultas ──────────────────────────────────────────────────────────────

    public async Task<DtoServico> ObterPorIdAsync(Guid servicoId, Guid usuarioId)
    {
        var servico = await repositorioServicos.ObterPorIdComDetalhesAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        // Valida que o usuário é participante (cliente, prestador, ou admin verificado pelo controller)
        if (servico.ClienteId != usuarioId && servico.PrestadorId != usuarioId)
            throw new ExcecaoProibido("Acesso negado ao serviço");

        return MapearDto(servico, usuarioId);
    }

    public async Task<List<DtoServico>> ListarMeusServicosAsync(Guid usuarioId, TipoConta tipoConta)
    {
        var servicos = tipoConta == TipoConta.Cliente
            ? await repositorioServicos.ListarPorClienteAsync(usuarioId)
            : await repositorioServicos.ListarPorPrestadorAsync(usuarioId);

        return servicos.Select(s => MapearDto(s, usuarioId)).ToList();
    }

    public async Task<List<DtoServico>> ListarDisponiveisParaPrestadorAsync(Guid prestadorId)
    {
        var servicos = await repositorioServicos.ListarDisponiveisParaPrestadorAsync(prestadorId);
        // Vagas disponíveis: o prestador ainda não foi aprovado → endereço sempre oculto.
        return servicos.Select(s => MapearDto(s, prestadorId)).ToList();
    }

    // ── Transições de estado ───────────────────────────────────────────────────

    public async Task<DtoServico> VincularPrestadorAsync(Guid servicoId, Guid prestadorId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.Status != StatusServico.EmNegociacao)
            throw new ExcecaoTransicaoInvalida(
                $"Vinculação só é possível no estado 'EmNegociacao'. Estado atual: {servico.Status}");

        if (servico.PrestadorId != null)
            throw new ExcecaoConflito("Serviço já possui um prestador vinculado");

        if (servico.ClienteId == prestadorId)
            throw new ExcecaoProibido("O cliente do serviço não pode se vincular como prestador");

        servico.PrestadorId = prestadorId;
        servico.AtualizadoEm = DateTime.UtcNow;

        await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = prestadorId,
            Acao = "servico.prestador_vinculado",
            Entidade = "Servico",
            EntidadeId = servicoId.ToString(),
        });

        // Notifica o cliente
        if (servico.ClienteId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.ClienteId.Value,
                Titulo = "Prestador vinculado",
                Mensagem = $"Um prestador aceitou sua solicitação: {servico.Titulo}",
                Tipo = "servico",
                ReferenciaId = servicoId.ToString()
            });
        }

        // Prestador acabou de se vincular (status ainda em negociação) → endereço permanece oculto.
        return await CarregarDtoAsync(servicoId, prestadorId);
    }

    public async Task<DtoServico> MarcarConcluidoAsync(Guid servicoId, Guid prestadorId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.PrestadorId != prestadorId)
            throw new ExcecaoProibido("Apenas o prestador do serviço pode marcá-lo como concluído");

        if (servico.Status != StatusServico.EmAndamento)
            throw new ExcecaoTransicaoInvalida(
                $"Conclusão só é possível no estado 'EmAndamento'. Estado atual: {servico.Status}");

        servico.Status = StatusServico.AguardandoConfirmacaoCliente;
        servico.AguardandoConfirmacaoDesde = DateTime.UtcNow;
        servico.AtualizadoEm = DateTime.UtcNow;

        await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = prestadorId,
            Acao = "servico.concluido",
            Entidade = "Servico",
            EntidadeId = servicoId.ToString(),
        });

        // Notifica o cliente
        if (servico.ClienteId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.ClienteId.Value,
                Titulo = "Serviço marcado como concluído",
                Mensagem = $"O prestador marcou o serviço '{servico.Titulo}' como concluído. Confirme ou abra uma disputa.",
                Tipo = "servico",
                ReferenciaId = servicoId.ToString()
            });
        }

        return await CarregarDtoAsync(servicoId, prestadorId);
    }

    public async Task<DtoServico> ConfirmarConclusaoAsync(Guid servicoId, Guid clienteId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.ClienteId != clienteId)
            throw new ExcecaoProibido("Apenas o cliente do serviço pode confirmar a conclusão");

        if (servico.Status != StatusServico.AguardandoConfirmacaoCliente)
            throw new ExcecaoTransicaoInvalida(
                $"Confirmação só é possível no estado 'AguardandoConfirmacaoCliente'. Estado atual: {servico.Status}");

        servico.Status = StatusServico.Concluido;
        servico.ConcluidoEm = DateTime.UtcNow;
        servico.AtualizadoEm = DateTime.UtcNow;

        await repositorioServicos.AtualizarAsync(servico);
        await servicoFinanceiro.LiberarPagamentoAsync(servico.Id);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = clienteId,
            Acao = "servico.confirmado_cliente",
            Entidade = "Servico",
            EntidadeId = servicoId.ToString(),
        });

        // Notifica o prestador sobre pagamento liberado
        if (servico.PrestadorId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.PrestadorId.Value,
                Titulo = "Conclusão confirmada",
                Mensagem = $"O cliente confirmou a conclusão do serviço '{servico.Titulo}'. Pagamento liberado.",
                Tipo = "pagamento",
                ReferenciaId = servicoId.ToString()
            });
        }

        // Notifica ambas as partes sobre a janela de avaliação
        if (servico.ClienteId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.ClienteId.Value,
                Titulo = "Avalie o serviço",
                Mensagem = "Seu serviço foi concluído. Você tem 30 dias para avaliar o serviço.",
                Tipo = "avaliacao",
                ReferenciaId = servicoId.ToString()
            });
        }

        if (servico.PrestadorId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.PrestadorId.Value,
                Titulo = "Avalie o cliente",
                Mensagem = "Seu serviço foi concluído. Você tem 30 dias para avaliar o serviço.",
                Tipo = "avaliacao",
                ReferenciaId = servicoId.ToString()
            });
        }

        return await CarregarDtoAsync(servicoId, clienteId);
    }

    public async Task<DtoServico> CancelarAsync(Guid servicoId, Guid atualUsuarioId, Papel papel, string? motivo = null)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.Status == StatusServico.Concluido)
            throw new ExcecaoTransicaoInvalida("Serviços concluídos não podem ser cancelados");

        // Valida permissão por papel e estado
        if (papel != Papel.Admin)
        {
            var eParticipante = servico.ClienteId == atualUsuarioId || servico.PrestadorId == atualUsuarioId;
            if (!eParticipante)
                throw new ExcecaoProibido("Acesso negado");

            // Participantes só podem cancelar em EmNegociacao
            if (servico.Status != StatusServico.EmNegociacao)
                throw new ExcecaoTransicaoInvalida(
                    $"Cancelamento por participante só é possível no estado 'EmNegociacao'. Estado atual: {servico.Status}");
        }

        // EmDisputa: apenas admin pode agir
        if (servico.Status == StatusServico.EmDisputa && papel != Papel.Admin)
            throw new ExcecaoTransicaoInvalida("Em disputa, apenas o Admin pode alterar o serviço");

        servico.Status = StatusServico.Cancelado;
        servico.AtualizadoEm = DateTime.UtcNow;

        await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = atualUsuarioId,
            Acao = "servico.cancelado",
            Entidade = "Servico",
            EntidadeId = servicoId.ToString(),
            Detalhes = motivo is not null ? $"{{\"motivo\":\"{motivo}\"}}" : null
        });

        return await CarregarDtoAsync(servicoId, atualUsuarioId);
    }

    // ── Auto-conclusão (Job) ───────────────────────────────────────────────────

    public async Task AutoConcluirServicosAsync()
    {
        var servicos = await repositorioServicos.ListarParaAutoConclusaoAsync();

        foreach (var servico in servicos)
        {
            servico.Status = StatusServico.Concluido;
            servico.ConcluidoEm = DateTime.UtcNow;
            servico.AtualizadoEm = DateTime.UtcNow;

            await repositorioServicos.AtualizarAsync(servico);
            await servicoFinanceiro.LiberarPagamentoAsync(servico.Id);

            await repositorioAuditLog.RegistrarAsync(new AuditLog
            {
                UsuarioId = null,
                Acao = "job.servico.auto_concluido",
                Entidade = "Servico",
                EntidadeId = servico.Id.ToString()
            });

            // Notifica ambas as partes
            if (servico.ClienteId.HasValue)
            {
                await repositorioNotificacoes.AdicionarAsync(new Notificacao
                {
                    UsuarioId = servico.ClienteId.Value,
                    Titulo = "Serviço concluído automaticamente",
                    Mensagem = $"O serviço '{servico.Titulo}' foi concluído automaticamente após 7 dias sem confirmação.",
                    Tipo = "servico",
                    ReferenciaId = servico.Id.ToString()
                });
            }

            if (servico.PrestadorId.HasValue)
            {
                await repositorioNotificacoes.AdicionarAsync(new Notificacao
                {
                    UsuarioId = servico.PrestadorId.Value,
                    Titulo = "Serviço concluído automaticamente",
                    Mensagem = $"O serviço '{servico.Titulo}' foi concluído automaticamente. Pagamento liberado.",
                    Tipo = "pagamento",
                    ReferenciaId = servico.Id.ToString()
                });
            }

            // Notifica ambas as partes sobre a janela de avaliação
            if (servico.ClienteId.HasValue)
            {
                await repositorioNotificacoes.AdicionarAsync(new Notificacao
                {
                    UsuarioId = servico.ClienteId.Value,
                    Titulo = "Avalie o serviço",
                    Mensagem = "Seu serviço foi concluído. Você tem 30 dias para avaliar o serviço.",
                    Tipo = "avaliacao",
                    ReferenciaId = servico.Id.ToString()
                });
            }

            if (servico.PrestadorId.HasValue)
            {
                await repositorioNotificacoes.AdicionarAsync(new Notificacao
                {
                    UsuarioId = servico.PrestadorId.Value,
                    Titulo = "Avalie o cliente",
                    Mensagem = "Seu serviço foi concluído. Você tem 30 dias para avaliar o serviço.",
                    Tipo = "avaliacao",
                    ReferenciaId = servico.Id.ToString()
                });
            }
        }
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private async Task<DtoServico> CarregarDtoAsync(Guid servicoId, Guid viewerId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado após operação");
        return MapearDto(servico, viewerId);
    }

    /// <summary>
    /// Status a partir dos quais o endereço/horário do serviço é revelado ao PRESTADOR.
    /// Antes disso (em negociação / aguardando pagamento) o prestador só conhece a cidade,
    /// nunca o endereço exato — a revelação ocorre após a aprovação (pagamento) do cliente.
    /// </summary>
    private static readonly HashSet<StatusServico> StatusEnderecoRevelado = new()
    {
        StatusServico.Pago,
        StatusServico.EmAndamento,
        StatusServico.AguardandoConfirmacaoCliente,
        StatusServico.EmDisputa,
        StatusServico.Concluido,
    };

    /// <summary>Mapeia sem restrição de endereço (uso interno/admin — vê tudo).</summary>
    internal static DtoServico MapearDto(Servico s) => MapearDtoInterno(s, revelarEndereco: true);

    /// <summary>
    /// Mapeia aplicando a regra de privacidade do endereço conforme o observador:
    /// cliente do serviço sempre vê; prestador só vê após a aprovação do cliente.
    /// </summary>
    internal static DtoServico MapearDto(Servico s, Guid viewerId)
    {
        var eCliente = s.ClienteId == viewerId;
        var ePrestador = s.PrestadorId == viewerId;
        var revelar = eCliente
            || (ePrestador && StatusEnderecoRevelado.Contains(s.Status));
        return MapearDtoInterno(s, revelar);
    }

    /// <summary>Converte o enum de status para snake_case, formato esperado pelo frontend.</summary>
    internal static string StatusParaSnake(StatusServico status) => status switch
    {
        StatusServico.EmNegociacao => "em_negociacao",
        StatusServico.AguardandoPagamento => "aguardando_pagamento",
        StatusServico.Pago => "pago",
        StatusServico.EmAndamento => "em_andamento",
        StatusServico.AguardandoConfirmacaoCliente => "aguardando_confirmacao_cliente",
        StatusServico.EmDisputa => "em_disputa",
        StatusServico.Concluido => "concluido",
        StatusServico.Cancelado => "cancelado",
        _ => status.ToString().ToLowerInvariant(),
    };

    private static DtoServico MapearDtoInterno(Servico s, bool revelarEndereco)
        => new(
            Id: s.Id,
            Titulo: s.Titulo,
            Descricao: s.Descricao,
            CategoriaId: s.CategoriaId,
            CategoriaNome: s.Categoria?.Nome ?? string.Empty,
            CidadeId: s.CidadeId,
            CidadeNome: s.Cidade?.Nome,
            ClienteId: s.ClienteId,
            ClienteNome: s.Cliente?.Nome,
            PrestadorId: s.PrestadorId,
            PrestadorNome: s.Prestador?.Nome,
            Preco: s.Preco,
            Status: StatusParaSnake(s.Status),
            Endereco: revelarEndereco ? s.Endereco : null,
            AgendadoEm: revelarEndereco ? s.AgendadoEm : null,
            ConcluidoEm: s.ConcluidoEm,
            CriadoEm: s.CriadoEm
        );
}
