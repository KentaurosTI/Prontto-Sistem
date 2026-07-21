using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Financeiro;

public class ServicoFinanceiro(
    IRepositorioCobranca repositorioCobrancas,
    IRepositorioServico repositorioServicos,
    IRepositorioBanking repositorioBanking,
    IRepositorioAuditLog repositorioAuditLog,
    IRepositorioNotificacao repositorioNotificacoes,
    IProcessadorPagamento processadorPagamento,
    IConfiguration configuracao,
    ILogger<ServicoFinanceiro> logger) : IServicoFinanceiro
{
    private const int MaxRetries = 3;

    // Tipos de evento suportados pelo Pagar.me que indicam pagamento confirmado
    private static readonly HashSet<string> EventosPagamentoConfirmado =
        new(StringComparer.OrdinalIgnoreCase) { "order.paid", "charge.paid" };

    // ── Gerar PIX ──────────────────────────────────────────────────────────────

    public async Task<DtoCobranca> GerarPixAsync(Guid servicoId)
    {
        var cobranca = await repositorioCobrancas.ObterPorServicoIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Cobrança não encontrada");

        if (cobranca.Status != StatusCobranca.Pendente)
            return MapearDto(cobranca);

        var servico = await repositorioServicos.ObterPorIdAsync(cobranca.ServicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        var resultado = await processadorPagamento.GerarPixAsync(
            valor: cobranca.ValorTotal,
            descricao: $"Prontto - {servico.Titulo}",
            expiracao: TimeSpan.FromHours(24));

        cobranca.PagarmeOrderId = resultado.PagarmeOrderId;
        cobranca.PixQrCode = resultado.PixQrCode;
        cobranca.PixCopiaCola = resultado.PixCopiaCola;
        cobranca.PixExpiracaoEm = resultado.PixExpiracaoEm;
        cobranca.AtualizadoEm = DateTime.UtcNow;

        await repositorioCobrancas.AtualizarAsync(cobranca);
        return MapearDto(cobranca);
    }

    // ── Webhook Pagar.me ───────────────────────────────────────────────────────

    public async Task ProcessarWebhookAsync(string payload, string assinaturaHmac)
    {
        // RN-07: validar HMAC-SHA256
        var segredo = configuracao["PAGARME_WEBHOOK_SECRET"] ?? "";
        if (!ValidarHmac(payload, assinaturaHmac, segredo))
            throw new ExcecaoNaoAutorizado("Assinatura HMAC inválida");

        // Deserializar payload com System.Text.Json
        string? tipoEvento = null;
        string? orderId = null;
        string? chargeId = null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            tipoEvento = root.TryGetProperty("type", out var tp) ? tp.GetString() : null;

            if (root.TryGetProperty("data", out var data))
            {
                orderId = data.TryGetProperty("id", out var oid) ? oid.GetString() : null;

                if (data.TryGetProperty("charges", out var charges) && charges.ValueKind == JsonValueKind.Array
                    && charges.GetArrayLength() > 0)
                {
                    chargeId = charges[0].TryGetProperty("id", out var cid) ? cid.GetString() : null;
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Webhook: payload JSON inválido — ignorando");
            return;
        }

        // Filtrar: apenas eventos de pagamento confirmado
        if (string.IsNullOrWhiteSpace(tipoEvento) || !EventosPagamentoConfirmado.Contains(tipoEvento))
        {
            logger.LogInformation("Webhook: evento '{Tipo}' ignorado (não é confirmação de pagamento)", tipoEvento);
            return;
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            logger.LogWarning("Webhook recebido sem order_id reconhecível. Tipo={Tipo}", tipoEvento);
            return;
        }

        // RN-08: idempotência
        var cobranca = await repositorioCobrancas.ObterPorPagarmeOrderIdAsync(orderId);
        if (cobranca == null)
        {
            logger.LogWarning("Cobrança não encontrada para PagarmeOrderId {OrderId}", orderId);
            return;
        }

        if (cobranca.Status != StatusCobranca.Pendente)
        {
            logger.LogInformation("Webhook duplicado ignorado para OrderId {OrderId}", orderId);
            return;
        }

        // Pendente → Pago → Retido (imediato)
        cobranca.Status = StatusCobranca.Retido;
        cobranca.PagarmePagamentoId = chargeId ?? orderId;
        cobranca.PagadoEm = DateTime.UtcNow;
        cobranca.RetidoEm = DateTime.UtcNow;
        cobranca.AtualizadoEm = DateTime.UtcNow;
        await repositorioCobrancas.AtualizarAsync(cobranca);

        // Serviço: AguardandoPagamento → Pago → EmAndamento
        var servico = await repositorioServicos.ObterPorIdAsync(cobranca.ServicoId);
        if (servico != null && servico.Status == StatusServico.AguardandoPagamento)
        {
            servico.Status = StatusServico.EmAndamento;
            servico.AtualizadoEm = DateTime.UtcNow;
            await repositorioServicos.AtualizarAsync(servico);

            await repositorioAuditLog.RegistrarAsync(new AuditLog
            {
                UsuarioId = null,
                Acao = "pagamento.confirmado",
                Entidade = "Cobranca",
                EntidadeId = cobranca.Id.ToString(),
                Detalhes = $"{{\"pagarmeOrderId\":\"{orderId}\",\"valor\":{cobranca.ValorTotal}}}"
            });

            if (servico.ClienteId.HasValue)
                await repositorioNotificacoes.AdicionarAsync(new Notificacao
                {
                    UsuarioId = servico.ClienteId.Value,
                    Titulo = "Pagamento confirmado!",
                    Mensagem = $"PIX recebido para o serviço '{servico.Titulo}'. O prestador foi notificado.",
                    Tipo = "pagamento",
                    ReferenciaId = servico.Id.ToString()
                });

            if (servico.PrestadorId.HasValue)
                await repositorioNotificacoes.AdicionarAsync(new Notificacao
                {
                    UsuarioId = servico.PrestadorId.Value,
                    Titulo = "Pagamento recebido!",
                    Mensagem = $"O pagamento do serviço '{servico.Titulo}' foi confirmado. Você pode iniciar o trabalho.",
                    Tipo = "pagamento",
                    ReferenciaId = servico.Id.ToString()
                });
        }
    }

    // ── Liberar pagamento ao Prestador ─────────────────────────────────────────

    public async Task LiberarPagamentoAsync(Guid servicoId)
    {
        var cobranca = await repositorioCobrancas.ObterPorServicoIdAsync(servicoId);
        if (cobranca == null || cobranca.Status != StatusCobranca.Retido)
            return;

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId);
        if (servico?.PrestadorId == null)
            return;

        var dadosBancarios = await repositorioBanking.ObterPorUsuarioIdAsync(servico.PrestadorId.Value);

        for (int tentativa = 1; tentativa <= MaxRetries; tentativa++)
        {
            try
            {
                await processadorPagamento.TransferirAsync(
                    valor: cobranca.ValorPrestador,
                    chavePix: dadosBancarios?.ChavePix ?? "sem-chave",
                    nomeBeneficiario: dadosBancarios?.NomeCompleto ?? "Prestador",
                    referencia: servicoId.ToString());

                cobranca.Status = StatusCobranca.Liberado;
                cobranca.LiberadoEm = DateTime.UtcNow;
                cobranca.AtualizadoEm = DateTime.UtcNow;
                await repositorioCobrancas.AtualizarAsync(cobranca);

                await repositorioAuditLog.RegistrarAsync(new AuditLog
                {
                    UsuarioId = null,
                    Acao = "pagamento.liberado",
                    Entidade = "Cobranca",
                    EntidadeId = cobranca.Id.ToString(),
                    Detalhes = $"{{\"servicoId\":\"{servicoId}\",\"valorPrestador\":{cobranca.ValorPrestador},\"tentativa\":{tentativa}}}"
                });

                if (servico.PrestadorId.HasValue)
                    await repositorioNotificacoes.AdicionarAsync(new Notificacao
                    {
                        UsuarioId = servico.PrestadorId.Value,
                        Titulo = "Pagamento liberado!",
                        Mensagem = $"R$ {cobranca.ValorPrestador:N2} transferido para sua chave PIX.",
                        Tipo = "pagamento",
                        ReferenciaId = servicoId.ToString()
                    });

                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na liberação de pagamento para servico {ServicoId}, tentativa {Tentativa}", servicoId, tentativa);
                if (tentativa < MaxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativa)));
            }
        }

        logger.LogCritical("Repasse FALHOU após {MaxRetries} tentativas para servico {ServicoId}", MaxRetries, servicoId);
    }

    // ── Reembolsar ao Cliente ──────────────────────────────────────────────────

    public async Task ReembolsarAsync(Guid servicoId)
    {
        var cobranca = await repositorioCobrancas.ObterPorServicoIdAsync(servicoId);
        if (cobranca == null || cobranca.Status != StatusCobranca.Retido)
            return;

        if (string.IsNullOrWhiteSpace(cobranca.PagarmeOrderId))
            return;

        await processadorPagamento.ReembolsarAsync(
            pagarmeOrderId: cobranca.PagarmeOrderId,
            valor: cobranca.ValorTotal,
            referencia: servicoId.ToString());

        cobranca.Status = StatusCobranca.Reembolsado;
        cobranca.AtualizadoEm = DateTime.UtcNow;
        await repositorioCobrancas.AtualizarAsync(cobranca);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = null,
            Acao = "pagamento.reembolsado",
            Entidade = "Cobranca",
            EntidadeId = cobranca.Id.ToString(),
            Detalhes = $"{{\"servicoId\":\"{servicoId}\",\"valorTotal\":{cobranca.ValorTotal}}}"
        });

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId);
        if (servico?.ClienteId.HasValue == true)
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.ClienteId.Value,
                Titulo = "Reembolso processado",
                Mensagem = $"R$ {cobranca.ValorTotal:N2} será devolvido em até 5 dias úteis.",
                Tipo = "pagamento",
                ReferenciaId = servicoId.ToString()
            });
    }

    // ── Consulta ───────────────────────────────────────────────────────────────

    public async Task<DtoCobranca?> ObterPorServicoAsync(Guid servicoId)
    {
        var cobranca = await repositorioCobrancas.ObterPorServicoIdAsync(servicoId);
        return cobranca == null ? null : MapearDto(cobranca);
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private static bool ValidarHmac(string payload, string assinatura, string segredo)
    {
        if (string.IsNullOrWhiteSpace(segredo) || string.IsNullOrWhiteSpace(assinatura))
            return false;

        // Formato: "sha256=<hex>" ou somente "<hex>"
        var hash = assinatura.StartsWith("sha256=")
            ? assinatura[7..]
            : assinatura;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(segredo));
        var computado = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computado),
            Encoding.UTF8.GetBytes(hash.ToLowerInvariant()));
    }

    internal static DtoCobranca MapearDto(Domain.Entities.Cobranca c) => new(
        Id: c.Id,
        ServicoId: c.ServicoId,
        ValorTotal: c.ValorTotal,
        TaxaAdmin: c.TaxaAdmin,
        ValorPrestador: c.ValorPrestador,
        Status: c.Status.ToString().ToLower(),
        PixQrCode: c.PixQrCode,
        PixCopiaCola: c.PixCopiaCola,
        PixExpiracaoEm: c.PixExpiracaoEm,
        PagadoEm: c.PagadoEm,
        RetidoEm: c.RetidoEm,
        LiberadoEm: c.LiberadoEm,
        CriadoEm: c.CriadoEm
    );
}
