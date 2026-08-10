using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Stripe;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Checkout de cartão via Stripe. Cria o PaymentIntent do valor negociado e, quando o prestador
/// tem conta conectada (Connect), aplica o split: 20% de application_fee para a plataforma e o
/// restante transferido ao prestador (destination charge). Sem Connect, cobra 100% na plataforma.
/// </summary>
public class ServicoCheckoutStripe(
    IRepositorioCobranca repositorioCobrancas,
    IRepositorioServico repositorioServicos,
    IRepositorioUsuario repositorioUsuarios,
    IServicoFinanceiro servicoFinanceiro,
    IConfiguration configuracao,
    ILogger<ServicoCheckoutStripe> logger) : IServicoCheckoutStripe
{
    private StripeClient Cliente() => new(configuracao["STRIPE_SECRET_KEY"]
        ?? throw new InvalidOperationException("STRIPE_SECRET_KEY não configurado."));

    public async Task<string> CriarPaymentIntentAsync(Guid servicoId, Guid usuarioId)
    {
        var cobranca = await repositorioCobrancas.ObterPorServicoIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Cobrança não encontrada. Aceite a proposta antes de pagar.");
        if (cobranca.Status != StatusCobranca.Pendente)
            throw new ExcecaoValidacao("Esta cobrança não está pendente de pagamento.");

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado.");
        if (servico.ClienteId != usuarioId)
            throw new ExcecaoProibido("Apenas o contratante pode pagar este serviço.");
        if (servico.Status != StatusServico.AguardandoPagamento)
            throw new ExcecaoValidacao("O serviço não está aguardando pagamento.");

        var valorCentavos = (long)decimal.Round(cobranca.ValorTotal * 100, 0, MidpointRounding.AwayFromZero);
        if (valorCentavos <= 0) throw new ExcecaoValidacao("Valor inválido para cobrança.");

        var opcoes = new PaymentIntentCreateOptions
        {
            Amount = valorCentavos,
            Currency = "brl",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
            {
                Card = new PaymentIntentPaymentMethodOptionsCardOptions
                {
                    Installments = new PaymentIntentPaymentMethodOptionsCardInstallmentsOptions { Enabled = true },
                },
            },
            Metadata = new Dictionary<string, string>
            {
                ["servicoId"] = servicoId.ToString(),
                ["cobrancaId"] = cobranca.Id.ToString(),
            },
            Description = $"Prontto - {servico.Titulo}",
        };

        // Split 80/20 quando o prestador tem conta conectada (Connect).
        var prestador = servico.PrestadorId.HasValue
            ? await repositorioUsuarios.ObterPorIdAsync(servico.PrestadorId.Value)
            : null;
        if (!string.IsNullOrWhiteSpace(prestador?.StripeAccountId))
        {
            var taxaCentavos = (long)decimal.Round(cobranca.TaxaAdmin * 100, 0, MidpointRounding.AwayFromZero);
            opcoes.ApplicationFeeAmount = taxaCentavos;
            opcoes.TransferData = new PaymentIntentTransferDataOptions { Destination = prestador!.StripeAccountId };
        }
        else
        {
            logger.LogWarning("Serviço {ServicoId}: prestador sem conta Connect — cobrança sem split (100% plataforma).", servicoId);
        }

        var pi = await new PaymentIntentService(Cliente()).CreateAsync(opcoes);

        cobranca.PagarmeOrderId = pi.Id; // reutiliza o campo genérico de referência do gateway
        cobranca.AtualizadoEm = DateTime.UtcNow;
        await repositorioCobrancas.AtualizarAsync(cobranca);

        return pi.ClientSecret!;
    }

    public async Task ProcessarWebhookAsync(string payload, string assinatura)
    {
        var segredo = configuracao["STRIPE_WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(segredo))
        {
            logger.LogError("STRIPE_WEBHOOK_SECRET não configurado — webhook ignorado.");
            return;
        }

        Event evento;
        try
        {
            evento = EventUtility.ConstructEvent(payload, assinatura, segredo);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Webhook Stripe: assinatura inválida.");
            throw new ExcecaoNaoAutorizado("Assinatura do webhook inválida.");
        }

        if (evento.Type == "payment_intent.succeeded" && evento.Data.Object is PaymentIntent pi)
        {
            await servicoFinanceiro.ConfirmarPagamentoPorGatewayAsync(pi.Id, pi.LatestChargeId);
            logger.LogInformation("Webhook Stripe: pagamento confirmado PI {Id}", pi.Id);
        }
        else
        {
            logger.LogInformation("Webhook Stripe: evento '{Tipo}' ignorado.", evento.Type);
        }
    }
}
