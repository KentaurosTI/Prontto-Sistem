using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    IRepositorioUsuario repositorioUsuarios,
    IServicoEmail servicoEmail,
    IProcessadorPagamento processadorPagamento,
    IConfiguration configuracao,
    ILogger<ServicoFinanceiro> logger) : IServicoFinanceiro
{
    private const int MaxRetries = 3;
    // Teto do backoff por tentativa (SCRUM-51): evita que aumentar MaxRetries gere esperas enormes.
    private const int MaxDelaySegundos = 30;

    /// <summary>
    /// Delay do retry com backoff exponencial LIMITADO por MaxDelaySegundos + jitter aleatório (até 1s).
    /// O jitter distribui as tentativas no tempo, evitando thundering herd sob falha do gateway (SCRUM-51).
    /// Retorna o delay real aplicado, para log.
    /// </summary>
    private static async Task<double> AguardarBackoffAsync(int tentativa)
    {
        var expo = Math.Min(Math.Pow(2, tentativa), MaxDelaySegundos);
        var jitter = Random.Shared.NextDouble(); // 0..1s
        var segundos = expo + jitter;
        await Task.Delay(TimeSpan.FromSeconds(segundos));
        return segundos;
    }

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

        // Deserializar payload com desserialização tipada
        PagarmeWebhookPayload? evento;
        try
        {
            evento = JsonSerializer.Deserialize<PagarmeWebhookPayload>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            var truncado = payload.Length > 500 ? payload[..500] : payload;
            logger.LogWarning(ex, "Webhook: payload JSON inválido — payload (truncado): {Payload}", truncado);
            throw new ExcecaoValidacao("Payload do webhook inválido: JSON malformado");
        }

        if (evento is null)
        {
            logger.LogWarning("Webhook: deserialização retornou nulo — payload (truncado): {Payload}",
                payload.Length > 500 ? payload[..500] : payload);
            throw new ExcecaoValidacao("Payload do webhook inválido");
        }

        var tipoEvento = evento.Type;
        var orderId = evento.Data?.Id;
        var chargeId = evento.Data?.Charges?.FirstOrDefault()?.Id;

        // Filtrar: apenas eventos de pagamento confirmado
        if (string.IsNullOrWhiteSpace(tipoEvento) || !EventosPagamentoConfirmado.Contains(tipoEvento))
        {
            logger.LogInformation("Webhook: evento '{Tipo}' ignorado (não é confirmação de pagamento)", tipoEvento);
            return;
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            logger.LogWarning("Webhook: campo obrigatório 'data.id' ausente. Tipo={Tipo}", tipoEvento);
            throw new ExcecaoValidacao("Payload do webhook inválido: campo 'data.id' ausente");
        }

        // Confirmação idempotente + notificações + e-mails (compartilhado com o Stripe).
        await ConfirmarPagamentoPorGatewayAsync(orderId, chargeId);
    }

    /// <summary>Localiza a cobrança pelo id do gateway (order/PaymentIntent) e confirma o pagamento.</summary>
    public async Task ConfirmarPagamentoPorGatewayAsync(string gatewayOrderId, string? chargeId)
    {
        var cobranca = await repositorioCobrancas.ObterPorPagarmeOrderIdAsync(gatewayOrderId);
        if (cobranca == null)
        {
            logger.LogWarning("Cobrança não encontrada para gateway id {OrderId}", gatewayOrderId);
            return;
        }
        await ConfirmarPagamentoAsync(cobranca, chargeId);
    }

    /// <summary>
    /// Confirma o pagamento (idempotente): Retido → serviço EmAndamento (revela endereço) →
    /// auditoria → notificações + e-mails às duas partes. Usado por Pagar.me e Stripe.
    /// </summary>
    public async Task ConfirmarPagamentoAsync(Cobranca cobranca, string? gatewayPagamentoId)
    {
        if (cobranca.Status != StatusCobranca.Pendente)
        {
            logger.LogInformation("Confirmação duplicada ignorada para cobrança {Id}", cobranca.Id);
            return;
        }

        cobranca.Status = StatusCobranca.Retido;
        cobranca.PagarmePagamentoId = gatewayPagamentoId ?? cobranca.PagarmeOrderId;
        cobranca.PagadoEm = DateTime.UtcNow;
        cobranca.RetidoEm = DateTime.UtcNow;
        cobranca.AtualizadoEm = DateTime.UtcNow;
        await repositorioCobrancas.AtualizarAsync(cobranca);

        var servico = await repositorioServicos.ObterPorIdAsync(cobranca.ServicoId);
        if (servico == null || servico.Status != StatusServico.AguardandoPagamento) return;

        servico.Status = StatusServico.EmAndamento;
        servico.AtualizadoEm = DateTime.UtcNow;
        await repositorioServicos.AtualizarAsync(servico);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = null,
            Acao = "pagamento.confirmado",
            Entidade = "Cobranca",
            EntidadeId = cobranca.Id.ToString(),
            Detalhes = JsonSerializer.Serialize(new { gatewayId = cobranca.PagarmeOrderId, valor = cobranca.ValorTotal })
        });

        var agendamentoTexto = MontarTextoAgendamento(servico);

        if (servico.ClienteId.HasValue)
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.ClienteId.Value,
                Titulo = "Serviço agendado!",
                Mensagem = $"Pagamento confirmado para '{servico.Titulo}'.{agendamentoTexto} O prestador foi notificado e recebeu o endereço.",
                Tipo = "pagamento",
                ReferenciaId = servico.Id.ToString()
            });

        if (servico.PrestadorId.HasValue)
            await repositorioNotificacoes.AdicionarAsync(new Notificacao
            {
                UsuarioId = servico.PrestadorId.Value,
                Titulo = "Serviço agendado — pagamento recebido!",
                Mensagem = $"O pagamento de '{servico.Titulo}' foi confirmado.{agendamentoTexto} O endereço completo já está disponível na tela do serviço.",
                Tipo = "pagamento",
                ReferenciaId = servico.Id.ToString()
            });

        await EnviarEmailsPagamentoAsync(servico, cobranca, agendamentoTexto);
    }

    /// <summary>E-mails estilizados de pagamento confirmado (cliente + prestador com endereço/horário).</summary>
    private async Task EnviarEmailsPagamentoAsync(Servico servico, Cobranca cobranca, string agendamentoTexto)
    {
        var appUrl = (configuracao["APP_URL"] ?? "https://prontto.org").TrimEnd('/');
        var url = $"{appUrl}/servico/{servico.Id}";
        var agendamentoHtml = string.IsNullOrWhiteSpace(agendamentoTexto)
            ? string.Empty
            : $@"<p style=""margin:0 0 10px;""><b>Quando:</b>{agendamentoTexto}</p>";

        if (servico.ClienteId.HasValue)
        {
            var cliente = await repositorioUsuarios.ObterPorIdAsync(servico.ClienteId.Value);
            if (!string.IsNullOrWhiteSpace(cliente?.Email))
                _ = servicoEmail.EnviarAsync(cliente!.Email, cliente.Nome, "Pagamento confirmado — Prontto",
                    ModelosEmail.PagamentoConfirmadoCliente(cliente.Nome, servico.Titulo, cobranca.ValorTotal, agendamentoHtml, url));
        }

        if (servico.PrestadorId.HasValue)
        {
            var prestador = await repositorioUsuarios.ObterPorIdAsync(servico.PrestadorId.Value);
            if (!string.IsNullOrWhiteSpace(prestador?.Email))
            {
                var enderecoHtml = string.IsNullOrWhiteSpace(servico.Endereco)
                    ? string.Empty
                    : $@"<p style=""margin:0 0 10px;""><b>Endereço:</b> {System.Net.WebUtility.HtmlEncode(servico.Endereco)}</p>";
                _ = servicoEmail.EnviarAsync(prestador!.Email, prestador.Nome, "Pagamento recebido — Prontto",
                    ModelosEmail.PagamentoConfirmadoPrestador(prestador.Nome, servico.Titulo, cobranca.ValorPrestador, enderecoHtml, agendamentoHtml, url));
            }
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
                    Detalhes = JsonSerializer.Serialize(new { servicoId, valorPrestador = cobranca.ValorPrestador, tentativa })
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
                {
                    var delay = await AguardarBackoffAsync(tentativa);
                    logger.LogWarning("Retry de liberação: servico {ServicoId}, próxima tentativa após {Delay:F1}s", servicoId, delay);
                }
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

        // referencia = servicoId serve como idempotency key no gateway, evitando duplo
        // reembolso caso uma tentativa seja reexecutada após falha transitória (SCRUM-52).
        var referencia = servicoId.ToString();

        for (int tentativa = 1; tentativa <= MaxRetries; tentativa++)
        {
            try
            {
                await processadorPagamento.ReembolsarAsync(
                    pagarmeOrderId: cobranca.PagarmeOrderId,
                    valor: cobranca.ValorTotal,
                    referencia: referencia);

                // Só persiste status + audit + notificação APÓS a confirmação do gateway,
                // e de forma atômica (transação) — SCRUM-52.
                await using var transacao = await repositorioCobrancas.IniciarTransacaoAsync();
                try
                {
                    cobranca.Status = StatusCobranca.Reembolsado;
                    cobranca.AtualizadoEm = DateTime.UtcNow;
                    await repositorioCobrancas.AtualizarAsync(cobranca);

                    await repositorioAuditLog.RegistrarAsync(new AuditLog
                    {
                        UsuarioId = null,
                        Acao = "pagamento.reembolsado",
                        Entidade = "Cobranca",
                        EntidadeId = cobranca.Id.ToString(),
                        Detalhes = JsonSerializer.Serialize(new { servicoId, valorTotal = cobranca.ValorTotal, tentativa })
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

                    await transacao.CommitAsync();
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }

                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha no reembolso do servico {ServicoId}, tentativa {Tentativa}", servicoId, tentativa);
                await repositorioAuditLog.RegistrarAsync(new AuditLog
                {
                    UsuarioId = null,
                    Acao = "pagamento.reembolso.falha",
                    Entidade = "Cobranca",
                    EntidadeId = cobranca.Id.ToString(),
                    Detalhes = JsonSerializer.Serialize(new { servicoId, tentativa, erro = ex.Message })
                });

                if (tentativa < MaxRetries)
                {
                    var delay = await AguardarBackoffAsync(tentativa);
                    logger.LogWarning("Retry de reembolso: servico {ServicoId}, próxima tentativa após {Delay:F1}s", servicoId, delay);
                }
            }
        }

        // Todas as tentativas falharam: NÃO marca como Reembolsado. Marca ReembolsoPendente
        // (para reprocessamento manual/job) e sinaliza o erro ao chamador — SCRUM-52.
        cobranca.Status = StatusCobranca.ReembolsoPendente;
        cobranca.AtualizadoEm = DateTime.UtcNow;
        await repositorioCobrancas.AtualizarAsync(cobranca);

        logger.LogCritical("Reembolso FALHOU após {MaxRetries} tentativas para servico {ServicoId} — status ReembolsoPendente", MaxRetries, servicoId);
        throw new InvalidOperationException(
            $"Reembolso não confirmado pelo gateway após {MaxRetries} tentativas. Cobrança marcada como ReembolsoPendente para reprocessamento.");
    }

    // ── Consulta ───────────────────────────────────────────────────────────────

    public async Task<DtoCobranca?> ObterPorServicoAsync(Guid servicoId)
    {
        var cobranca = await repositorioCobrancas.ObterPorServicoIdAsync(servicoId);
        return cobranca == null ? null : MapearDto(cobranca);
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    /// <summary>Monta o trecho " Agendado para dd/MM/yyyy, das HH:mm às HH:mm." conforme os dados disponíveis.</summary>
    private static string MontarTextoAgendamento(Servico servico)
    {
        var temData = servico.AgendadoEm.HasValue;
        var temJanela = servico.JanelaInicio.HasValue && servico.JanelaFim.HasValue;

        if (!temData && !temJanela)
            return string.Empty;

        var partes = new StringBuilder(" Agendado para ");
        if (temData)
            partes.Append(servico.AgendadoEm!.Value.ToString("dd/MM/yyyy"));
        if (temJanela)
        {
            partes.Append(temData ? ", das " : "das ");
            partes.Append($"{servico.JanelaInicio!.Value:HH\\:mm} às {servico.JanelaFim!.Value:HH\\:mm}");
        }
        partes.Append('.');
        return partes.ToString();
    }

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

    // ── DTOs internos para deserialização tipada do webhook Pagar.me ──────────

    private sealed record PagarmeWebhookPayload(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("data")] PagarmeWebhookData? Data
    );

    private sealed record PagarmeWebhookData(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("charges")] List<PagarmeWebhookCharge>? Charges
    );

    private sealed record PagarmeWebhookCharge(
        [property: JsonPropertyName("id")] string? Id
    );
}
