using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Prontto.Application.Common;
using Prontto.Domain.Interfaces;

namespace Prontto.Infrastructure.Services;

public class ProcessadorCartaoStripe(
    IConfiguration configuracao,
    ILogger<ProcessadorCartaoStripe> logger) : IProcessadorCartao
{
    public string ObterPublishableKey()
        => configuracao["STRIPE_PUBLISHABLE_KEY"]
            ?? throw new InvalidOperationException("STRIPE_PUBLISHABLE_KEY não configurado.");

    public async Task<ResultadoCartao> GerarCartaoAsync(decimal valor, string descricao, int parcelas)
    {
        var secretKey = configuracao["STRIPE_SECRET_KEY"]
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY não configurado.");

        var publishableKey = ObterPublishableKey();
        var stripeClient = new StripeClient(secretKey);
        var service = new PaymentIntentService(stripeClient);

        var installmentOptions = new PaymentIntentPaymentMethodOptionsCardInstallmentsOptions
        {
            Enabled = true,
        };
        if (parcelas > 1)
        {
            installmentOptions.Plan = new PaymentIntentPaymentMethodOptionsCardInstallmentsPlanOptions
            {
                Count = parcelas,
                Interval = "month",
                Type = "fixed_count",
            };
        }

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(valor * 100),
            Currency = "brl",
            Description = descricao,
            PaymentMethodTypes = ["card"],
            PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
            {
                Card = new PaymentIntentPaymentMethodOptionsCardOptions
                {
                    Installments = installmentOptions
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["parcelas"] = parcelas.ToString(),
                ["descricao"] = descricao
            }
        };

        var intent = await service.CreateAsync(options);

        logger.LogInformation("Stripe: PaymentIntent criado. Id={Id}, Valor={Valor}, Parcelas={Parcelas}",
            intent.Id, valor, parcelas);

        return new ResultadoCartao(
            PaymentIntentId: intent.Id,
            ClientSecret: intent.ClientSecret,
            PublishableKey: publishableKey,
            Parcelas: parcelas,
            ValorTotal: valor);
    }

    public bool ValidarAssinaturaWebhook(string payload, string assinaturaStripe, out string paymentIntentId)
    {
        paymentIntentId = string.Empty;

        var webhookSecret = configuracao["STRIPE_WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(webhookSecret) || string.IsNullOrWhiteSpace(assinaturaStripe))
        {
            logger.LogWarning("Stripe webhook: secret ou assinatura ausente.");
            return false;
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, assinaturaStripe, webhookSecret);

            // Apenas payment_intent.succeeded dispara a confirmação de pagamento
            if (stripeEvent.Type != "payment_intent.succeeded")
            {
                logger.LogInformation("Stripe webhook: evento '{Type}' ignorado.", stripeEvent.Type);
                paymentIntentId = "ignored";
                return true;
            }

            if (stripeEvent.Data.Object is PaymentIntent intent)
                paymentIntentId = intent.Id;

            return true;
        }
        catch (StripeException ex)
        {
            logger.LogWarning("Stripe webhook: assinatura inválida. {Message}", ex.Message);
            return false;
        }
    }
}
