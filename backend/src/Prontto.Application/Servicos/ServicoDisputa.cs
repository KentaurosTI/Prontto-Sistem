using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Servicos;

public class ServicoDisputa(
    IRepositorioServico repositorioServicos,
    IRepositorioDisputa repositorioDisputas,
    IRepositorioNotificacao repositorioNotificacoes,
    IRepositorioAuditLog repositorioAuditLog,
    IServicoFinanceiro servicoFinanceiro) : IServicoDisputa
{
    // ── Abrir disputa (cliente) ────────────────────────────────────────────────

    public async Task<DtoDisputa> AbrirDisputaAsync(
        Guid servicoId, Guid clienteId, string motivo, string? descricao)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcecaoValidacao("Motivo da disputa é obrigatório");

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.ClienteId != clienteId)
            throw new ExcecaoProibido("Apenas o cliente do serviço pode abrir uma disputa");

        if (servico.Status != StatusServico.AguardandoConfirmacaoCliente)
            throw new ExcecaoTransicaoInvalida(
                $"Disputa só pode ser aberta no estado 'AguardandoConfirmacaoCliente'. Estado atual: {servico.Status}");

        // RN-03: um serviço tem no máximo uma disputa ativa
        var disputaExistente = await repositorioDisputas.ObterPorServicoIdAsync(servicoId);
        if (disputaExistente != null)
            throw new ExcecaoConflito("Já existe uma disputa para este serviço");

        var disputa = new Disputa
        {
            ServicoId = servicoId,
            AbertaPorId = clienteId,
            Motivo = motivo.Trim(),
            Descricao = descricao?.Trim(),
            Status = StatusDisputa.Aberta,
        };

        await repositorioDisputas.AdicionarAsync(disputa);

        // Avança o serviço para EmDisputa
        servico.Status = StatusServico.EmDisputa;
        servico.AtualizadoEm = DateTime.UtcNow;
        await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = clienteId,
            Acao = "disputa.aberta",
            Entidade = "Disputa",
            EntidadeId = disputa.Id.ToString(),
            Detalhes = $"{{\"servicoId\":\"{servicoId}\",\"motivo\":\"{motivo}\"}}"
        });

        // Notifica o prestador
        if (servico.PrestadorId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.PrestadorId.Value,
                Titulo = "Disputa aberta",
                Mensagem = $"O cliente abriu uma disputa para o serviço '{servico.Titulo}': {motivo}",
                Tipo = "disputa",
                ReferenciaId = servicoId.ToString()
            });
        }

        return MapearDto(disputa);
    }

    // ── Resolver disputa (admin) ───────────────────────────────────────────────

    public async Task<DtoDisputa> ResolverDisputaAsync(
        Guid disputaId, Guid adminId, bool favorPrestador, string decisaoAdmin)
    {
        if (string.IsNullOrWhiteSpace(decisaoAdmin))
            throw new ExcecaoValidacao("A justificativa da decisão é obrigatória");

        var disputa = await repositorioDisputas.ObterPorIdAsync(disputaId)
            ?? throw new ExcecaoNaoEncontrado("Disputa não encontrada");

        if (disputa.Status is StatusDisputa.ResolvidaCliente or StatusDisputa.ResolvidaPrestador)
            throw new ExcecaoTransicaoInvalida("Esta disputa já foi resolvida");

        var servico = await repositorioServicos.ObterPorIdAsync(disputa.ServicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço associado à disputa não encontrado");

        // Resolve a disputa
        disputa.Status = favorPrestador ? StatusDisputa.ResolvidaPrestador : StatusDisputa.ResolvidaCliente;
        disputa.ResolvidaPorId = adminId;
        disputa.DecisaoAdmin = decisaoAdmin.Trim();
        disputa.ResolvidoEm = DateTime.UtcNow;

        await repositorioDisputas.AtualizarAsync(disputa);

        // Conclui o serviço
        servico.Status = StatusServico.Concluido;
        servico.ConcluidoEm = DateTime.UtcNow;
        servico.AtualizadoEm = DateTime.UtcNow;
        await repositorioServicos.AtualizarAsync(servico);

        // Executa operação financeira (RF-05)
        if (favorPrestador)
            await servicoFinanceiro.LiberarPagamentoAsync(servico.Id);
        else
            await servicoFinanceiro.ReembolsarAsync(servico.Id);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = adminId,
            Acao = "disputa.resolvida",
            Entidade = "Disputa",
            EntidadeId = disputaId.ToString(),
            Detalhes = $"{{\"servicoId\":\"{servico.Id}\",\"favorPrestador\":{favorPrestador.ToString().ToLower()},\"decisao\":\"{decisaoAdmin}\"}}"
        });

        // Notifica ambas as partes
        var mensagemResultado = favorPrestador
            ? "A disputa foi resolvida em favor do prestador."
            : "A disputa foi resolvida em seu favor. Reembolso será processado.";

        if (servico.ClienteId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.ClienteId.Value,
                Titulo = "Disputa resolvida",
                Mensagem = $"Serviço '{servico.Titulo}': {mensagemResultado}",
                Tipo = "disputa",
                ReferenciaId = servico.Id.ToString()
            });
        }

        if (servico.PrestadorId.HasValue)
        {
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.PrestadorId.Value,
                Titulo = "Disputa resolvida",
                Mensagem = $"Serviço '{servico.Titulo}': {mensagemResultado}",
                Tipo = "disputa",
                ReferenciaId = servico.Id.ToString()
            });
        }

        return MapearDto(disputa);
    }

    // ── Listagem (admin) ───────────────────────────────────────────────────────

    public async Task<List<DtoDisputa>> ListarAbertasAsync()
    {
        var disputas = await repositorioDisputas.ListarAbertasAsync();
        return disputas.Select(MapearDto).ToList();
    }

    // ── Mapeamento ─────────────────────────────────────────────────────────────

    private static DtoDisputa MapearDto(Disputa d)
        => new(
            Id: d.Id,
            ServicoId: d.ServicoId,
            AbertaPorId: d.AbertaPorId,
            Motivo: d.Motivo,
            Descricao: d.Descricao,
            Status: d.Status.ToString(),
            DecisaoAdmin: d.DecisaoAdmin,
            CriadoEm: d.CriadoEm,
            ResolvidoEm: d.ResolvidoEm
        );
}
