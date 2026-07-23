using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Servicos;

public class ServicoNegociacao(
    IRepositorioServico repositorioServicos,
    IRepositorioMensagem repositorioMensagens,
    IRepositorioCobranca repositorioCobrancas,
    IRepositorioNotificacao repositorioNotificacoes,
    IRepositorioAuditLog repositorioAuditLog,
    IServicoFinanceiro servicoFinanceiro) : IServicoNegociacao
{
    // ── Envio de proposta ──────────────────────────────────────────────────────

    public async Task<DtoMensagemServico> EnviarPropostaAsync(
        Guid servicoId, Guid remetenteId, PapelRemetente papel, decimal valor)
    {
        if (valor <= 0)
            throw new ExcecaoValidacao("Valor da proposta deve ser maior que zero");

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        ValidarParticipante(servico, remetenteId, papel);

        if (servico.Status == StatusServico.EmDisputa)
            throw new ExcecaoTransicaoInvalida("Em disputa, apenas o Admin pode agir no serviço");

        if (servico.Status != StatusServico.EmNegociacao)
            throw new ExcecaoTransicaoInvalida(
                $"Propostas só podem ser enviadas no estado 'EmNegociacao'. Estado atual: {servico.Status}");

        // RN-02: expirar proposta pendente anterior
        var propostaPendente = await repositorioMensagens.ObterPropostaPendenteAsync(servicoId);
        if (propostaPendente != null)
        {
            propostaPendente.StatusProposta = StatusProposta.Expirada;
            await repositorioMensagens.AtualizarAsync(propostaPendente);
        }

        var mensagem = new MensagemServico
        {
            ServicoId = servicoId,
            RemetenteId = remetenteId,
            PapelRemetente = papel,
            TipoMensagem = TipoMensagem.Proposta,
            Conteudo = $"Proposta: R$ {valor:N2}",
            ValorProposta = valor,
            StatusProposta = StatusProposta.Pendente,
        };

        await repositorioMensagens.AdicionarAsync(mensagem);

        // Notifica o outro participante
        var destinatarioId = papel == PapelRemetente.Cliente
            ? servico.PrestadorId
            : servico.ClienteId;

        if (destinatarioId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = destinatarioId.Value,
                Titulo = "Nova proposta recebida",
                Mensagem = $"Proposta de R$ {valor:N2} para o serviço '{servico.Titulo}'",
                Tipo = "proposta",
                ReferenciaId = servicoId.ToString()
            });
        }

        return MapearMensagemDto(mensagem);
    }

    // ── Aceitar proposta ───────────────────────────────────────────────────────

    public async Task<DtoServico> AceitarPropostaAsync(Guid servicoId, Guid mensagemId, Guid usuarioId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.Status != StatusServico.EmNegociacao)
            throw new ExcecaoTransicaoInvalida(
                $"Aceite de proposta só é possível no estado 'EmNegociacao'. Estado atual: {servico.Status}");

        // Obtém a proposta especificada
        var mensagens = await repositorioMensagens.ListarPorServicoAsync(servicoId);
        var proposta = mensagens.FirstOrDefault(m => m.Id == mensagemId)
            ?? throw new ExcecaoNaoEncontrado("Proposta não encontrada");

        if (proposta.TipoMensagem != TipoMensagem.Proposta)
            throw new ExcecaoValidacao("A mensagem especificada não é uma proposta");

        if (proposta.StatusProposta != StatusProposta.Pendente)
            throw new ExcecaoTransicaoInvalida("Apenas propostas pendentes podem ser aceitas");

        // Quem aceita não pode ser quem enviou (não se aceita a própria proposta)
        if (proposta.RemetenteId == usuarioId)
            throw new ExcecaoProibido("Você não pode aceitar sua própria proposta");

        // Valida que o aceitante é participante
        var eParticipante = servico.ClienteId == usuarioId || servico.PrestadorId == usuarioId;
        if (!eParticipante)
            throw new ExcecaoProibido("Acesso negado ao serviço");

        // Aceita a proposta
        proposta.StatusProposta = StatusProposta.Aceita;
        await repositorioMensagens.AtualizarAsync(proposta);

        // Atualiza o serviço
        servico.Preco = proposta.ValorProposta!.Value;
        servico.Status = StatusServico.AguardandoPagamento;
        servico.AtualizadoEm = DateTime.UtcNow;

        await repositorioServicos.AtualizarAsync(servico);

        // Cria cobrança e gera PIX (RF-05)
        var taxaAdmin = Math.Round(servico.Preco * servico.TaxaAdminRate, 2);
        var cobranca = await repositorioCobrancas.AdicionarAsync(new Cobranca
        {
            ServicoId = servicoId,
            ValorTotal = servico.Preco,
            TaxaAdmin = taxaAdmin,
            ValorPrestador = servico.Preco - taxaAdmin,
            Status = StatusCobranca.Pendente,
        });

        // Gera PIX via gateway (usando o Id da cobrança como ServicoId é o link)
        await servicoFinanceiro.GerarPixAsync(cobranca.ServicoId);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = usuarioId,
            Acao = "proposta.aceita",
            Entidade = "Servico",
            EntidadeId = servicoId.ToString(),
            Detalhes = $"{{\"valorAcordado\":{servico.Preco},\"mensagemId\":\"{mensagemId}\"}}"
        });

        // Notifica o remetente da proposta
        if (proposta.RemetenteId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = proposta.RemetenteId.Value,
                Titulo = "Proposta aceita!",
                Mensagem = $"Sua proposta de R$ {servico.Preco:N2} para '{servico.Titulo}' foi aceita.",
                Tipo = "proposta",
                ReferenciaId = servicoId.ToString()
            });
        }

        return ServicoServico.MapearDto(servico);
    }

    // ── Mensagem de texto ──────────────────────────────────────────────────────

    public async Task<DtoMensagemServico> EnviarMensagemTextoAsync(
        Guid servicoId, Guid remetenteId, PapelRemetente papel, string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new ExcecaoValidacao("Conteúdo da mensagem não pode estar vazio");

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        ValidarParticipante(servico, remetenteId, papel);

        if (servico.Status == StatusServico.EmDisputa)
            throw new ExcecaoTransicaoInvalida("Em disputa, apenas o Admin pode enviar mensagens");

        if (servico.Status is StatusServico.Concluido or StatusServico.Cancelado)
            throw new ExcecaoTransicaoInvalida(
                $"Não é possível enviar mensagens em serviços com status '{servico.Status}'");

        var mensagem = new MensagemServico
        {
            ServicoId = servicoId,
            RemetenteId = remetenteId,
            PapelRemetente = papel,
            TipoMensagem = TipoMensagem.Texto,
            Conteudo = conteudo.Trim(),
        };

        await repositorioMensagens.AdicionarAsync(mensagem);

        // Notifica o outro participante que recebeu uma nova mensagem.
        var destinatarioId = papel == PapelRemetente.Cliente
            ? servico.PrestadorId
            : servico.ClienteId;

        if (destinatarioId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = destinatarioId.Value,
                Titulo = "Nova mensagem",
                Mensagem = $"Você recebeu uma mensagem no serviço '{servico.Titulo}'.",
                Tipo = "mensagem",
                ReferenciaId = servicoId.ToString(),
            });
        }

        return MapearMensagemDto(mensagem);
    }

    // ── Listagem de mensagens ──────────────────────────────────────────────────

    public async Task<List<DtoMensagemServico>> ListarMensagensAsync(Guid servicoId, Guid usuarioId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        var eParticipante = servico.ClienteId == usuarioId || servico.PrestadorId == usuarioId;
        if (!eParticipante)
            throw new ExcecaoProibido("Acesso negado ao serviço");

        var mensagens = await repositorioMensagens.ListarPorServicoAsync(servicoId);
        return mensagens.Select(MapearMensagemDto).ToList();
    }

    public async Task<ResultadoMensagensPaginadas> ListarMensagensPaginadasAsync(
        Guid servicoId, Guid usuarioId, Guid? afterId, int limite)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        var eParticipante = servico.ClienteId == usuarioId || servico.PrestadorId == usuarioId;
        if (!eParticipante)
            throw new ExcecaoProibido("Acesso negado ao serviço");

        // Busca limite + 1 para detectar se há mais páginas sem custo extra
        var mensagens = await repositorioMensagens.ListarPorServicoAsync(servicoId, afterId, limite + 1);

        var temMais = mensagens.Count > limite;
        var pagina = mensagens.Take(limite).Select(MapearMensagemDto).ToList();
        var ultimoId = pagina.Count > 0 ? pagina[^1].Id : (Guid?)null;

        return new ResultadoMensagensPaginadas(pagina, temMais, ultimoId);
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private static void ValidarParticipante(
        Prontto.Domain.Entities.Servico servico, Guid usuarioId, PapelRemetente papel)
    {
        var valido = papel switch
        {
            PapelRemetente.Cliente => servico.ClienteId == usuarioId,
            PapelRemetente.Prestador => servico.PrestadorId == usuarioId,
            PapelRemetente.Admin => true,
            _ => false
        };

        if (!valido)
            throw new ExcecaoProibido("Acesso negado ao serviço");
    }

    internal static DtoMensagemServico MapearMensagemDto(MensagemServico m)
        => new(
            Id: m.Id,
            ServicoId: m.ServicoId,
            RemetenteId: m.RemetenteId,
            RemetenteNome: m.Remetente?.Nome,
            PapelRemetente: m.PapelRemetente.ToString().ToLower(),
            TipoMensagem: m.TipoMensagem.ToString().ToLower(),
            Conteudo: m.Conteudo,
            ValorProposta: m.ValorProposta,
            StatusProposta: m.StatusProposta?.ToString().ToLower(),
            CriadoEm: m.CriadoEm
        );
}
