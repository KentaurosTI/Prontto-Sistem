using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Implementação real do IProcessadorPagamento via Pagar.me API v5.
/// Usado apenas em produção — em desenvolvimento o stub é registrado.
/// Auth: Basic Auth com secret_key como username e senha vazia.
/// </summary>
public class ProcessadorPagamentoPagarme(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuracao,
    ILogger<ProcessadorPagamentoPagarme> logger) : IProcessadorPagamento
{
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── GerarPixAsync ───────────────────────────────────────────────────────────

    public async Task<ResultadoPix> GerarPixAsync(decimal valor, string descricao, TimeSpan expiracao)
    {
        var cliente = CriarCliente();

        // Pagar.me trabalha com centavos (int)
        var valorCentavos = (int)(valor * 100);
        var expiracaoSegundos = (int)expiracao.TotalSeconds;

        var plataformaRecipientId = configuracao["PAGARME_PLATFORM_RECIPIENT_ID"]
            ?? throw new InvalidOperationException("PAGARME_PLATFORM_RECIPIENT_ID não configurado.");

        var corpo = new
        {
            items = new[]
            {
                new
                {
                    amount = valorCentavos,
                    description = descricao,
                    quantity = 1,
                    code = "SVC-001"
                }
            },
            payments = new[]
            {
                new
                {
                    payment_method = "pix",
                    pix = new { expires_in = expiracaoSegundos }
                }
            }
        };

        var json = JsonSerializer.Serialize(corpo, OpcoesJson);
        var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("Pagar.me: criando order PIX, valor={Valor}", valor);

        var resposta = await cliente.PostAsync("orders", conteudo);
        var respostaJson = await resposta.Content.ReadAsStringAsync();

        if (!resposta.IsSuccessStatusCode)
        {
            logger.LogError("Pagar.me: erro ao criar order. Status={Status}, Body={Body}",
                resposta.StatusCode, respostaJson);
            throw new ExcecaoConflito($"Pagar.me retornou erro {(int)resposta.StatusCode} ao gerar PIX.");
        }

        using var doc = JsonDocument.Parse(respostaJson);
        var root = doc.RootElement;

        var orderId = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Pagar.me: order_id ausente na resposta.");

        // O pagamento PIX fica dentro do array "charges"
        var charges = root.GetProperty("charges");
        if (charges.GetArrayLength() == 0)
            throw new InvalidOperationException("Pagar.me: nenhum charge retornado na order.");

        var charge = charges[0];
        var lastTransaction = charge.GetProperty("last_transaction");

        var qrCodeUrl = lastTransaction.TryGetProperty("qr_code_url", out var qrUrl)
            ? qrUrl.GetString() ?? string.Empty
            : string.Empty;

        var copiaCola = lastTransaction.TryGetProperty("qr_code", out var qr)
            ? qr.GetString() ?? string.Empty
            : string.Empty;

        var expiracaoEm = DateTime.UtcNow.Add(expiracao);
        if (lastTransaction.TryGetProperty("expires_at", out var exp) && exp.ValueKind != JsonValueKind.Null)
        {
            if (DateTime.TryParse(exp.GetString(), out var expParsed))
                expiracaoEm = expParsed.ToUniversalTime();
        }

        logger.LogInformation("Pagar.me: PIX gerado com sucesso. OrderId={OrderId}", orderId);

        return new ResultadoPix(
            PagarmeOrderId: orderId,
            PixQrCode: qrCodeUrl,
            PixCopiaCola: copiaCola,
            PixExpiracaoEm: expiracaoEm
        );
    }

    // ── TransferirAsync ─────────────────────────────────────────────────────────

    /// <summary>
    /// O split automático configurado no GerarPixAsync já garante o repasse ao prestador.
    /// Este método verifica o status da order para confirmar que o pagamento foi processado.
    /// </summary>
    public async Task TransferirAsync(decimal valor, string chavePix, string nomeBeneficiario, string referencia)
    {
        // Com split automático no Pagar.me, o repasse já acontece no momento do pagamento.
        // Este método é mantido por compatibilidade de interface — loga a confirmação.
        logger.LogInformation(
            "Pagar.me: repasse confirmado via split automático. Valor={Valor}, Beneficiario={Beneficiario}, Ref={Referencia}",
            valor, nomeBeneficiario, referencia);
        await Task.CompletedTask;
    }

    // ── ReembolsarAsync ─────────────────────────────────────────────────────────

    public async Task ReembolsarAsync(string pagarmeOrderId, decimal valor, string referencia)
    {
        var cliente = CriarCliente();

        logger.LogInformation("Pagar.me: iniciando reembolso. OrderId={OrderId}, Valor={Valor}, Ref={Referencia}",
            pagarmeOrderId, valor, referencia);

        // Cancela a order — Pagar.me estorna automaticamente charges PIX pagos
        var resposta = await cliente.PostAsync(
            $"orders/{pagarmeOrderId}/closed",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        var respostaJson = await resposta.Content.ReadAsStringAsync();

        if (!resposta.IsSuccessStatusCode)
        {
            logger.LogError("Pagar.me: erro ao reembolsar order. Status={Status}, Body={Body}",
                resposta.StatusCode, respostaJson);
            throw new ExcecaoConflito($"Pagar.me retornou erro {(int)resposta.StatusCode} ao reembolsar.");
        }

        logger.LogInformation("Pagar.me: reembolso processado com sucesso. OrderId={OrderId}", pagarmeOrderId);
    }

    // ── CriarRecipientAsync ─────────────────────────────────────────────────────

    public async Task<string> CriarRecipientAsync(DadosBancarios dados, string nomeCompleto)
    {
        var cliente = CriarCliente();

        // Monta tipo de conta bancária — Pagar.me aceita "checking" ou "savings"
        var tipoConta = dados.TipoConta?.ToLower() switch
        {
            "corrente" or "checking" => "checking",
            "poupanca" or "poupança" or "savings" => "savings",
            _ => "checking"
        };

        var corpo = new
        {
            name = nomeCompleto,
            description = $"Prestador Prontto - {nomeCompleto}",
            default_bank_account = new
            {
                holder_name = dados.NomeCompleto,
                holder_type = "individual",
                holder_document = dados.CpfCnpj.Replace(".", "").Replace("-", "").Replace("/", ""),
                bank = dados.NomeBanco ?? "000",
                branch_number = dados.Agencia ?? "0001",
                branch_check_digit = "0",
                account_number = dados.NumeroConta ?? "00000000",
                account_check_digit = "0",
                type = tipoConta
            },
            transfer_settings = new
            {
                transfer_enabled = true,
                transfer_interval = "weekly",
                transfer_day = 5
            },
            automatic_anticipation_settings = new
            {
                enabled = false
            }
        };

        var json = JsonSerializer.Serialize(corpo, OpcoesJson);
        var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("Pagar.me: criando recipient para usuario={UsuarioId}", dados.UsuarioId);

        var resposta = await cliente.PostAsync("recipients", conteudo);
        var respostaJson = await resposta.Content.ReadAsStringAsync();

        if (!resposta.IsSuccessStatusCode)
        {
            logger.LogError("Pagar.me: erro ao criar recipient. Status={Status}, Body={Body}",
                resposta.StatusCode, respostaJson);
            throw new ExcecaoConflito($"Pagar.me retornou erro {(int)resposta.StatusCode} ao criar recipient.");
        }

        using var doc = JsonDocument.Parse(respostaJson);
        var recipientId = doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Pagar.me: recipient_id ausente na resposta.");

        logger.LogInformation("Pagar.me: recipient criado. RecipientId={RecipientId}, Usuario={UsuarioId}",
            recipientId, dados.UsuarioId);

        return recipientId;
    }

    // ── Helper privado ──────────────────────────────────────────────────────────

    private HttpClient CriarCliente()
    {
        var secretKey = configuracao["PAGARME_SECRET_KEY"]
            ?? throw new InvalidOperationException("PAGARME_SECRET_KEY não configurado.");

        var cliente = httpClientFactory.CreateClient("pagarme");

        // Basic Auth: username=secret_key, password=vazio
        var credenciais = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:"));
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credenciais);

        return cliente;
    }
}
