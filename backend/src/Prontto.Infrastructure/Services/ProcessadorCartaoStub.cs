using Microsoft.Extensions.Logging;
using Prontto.Domain.Interfaces;

namespace Prontto.Infrastructure.Services;

public class ProcessadorCartaoStub(ILogger<ProcessadorCartaoStub> logger) : IProcessadorCartao
{
    public string ObterPublishableKey() => "pk_test_stub";

    public Task<ResultadoCartao> GerarCartaoAsync(decimal valor, string descricao, int parcelas)
    {
        var intentId = $"pi_stub_{Guid.NewGuid():N}";
        var resultado = new ResultadoCartao(
            PaymentIntentId: intentId,
            ClientSecret: $"{intentId}_secret_{Guid.NewGuid():N}",
            PublishableKey: "pk_test_stub",
            Parcelas: parcelas,
            ValorTotal: valor);

        logger.LogInformation("[STUB] Cartão: PaymentIntentId={Id}, Parcelas={Parcelas}, Valor={Valor}",
            intentId, parcelas, valor);

        return Task.FromResult(resultado);
    }

    public bool ValidarAssinaturaWebhook(string payload, string assinaturaStripe, out string paymentIntentId)
    {
        paymentIntentId = string.Empty;

        // Em dev, qualquer payload JSON com "pi_stub_" é aceito para testes manuais
        if (payload.Contains("\"pi_stub_") && payload.Contains("\"payment_intent.succeeded\""))
        {
            // Extrai o id do payload de forma simples (só para stub)
            var idx = payload.IndexOf("\"pi_stub_", StringComparison.Ordinal);
            if (idx >= 0)
            {
                var end = payload.IndexOf('"', idx + 1);
                if (end > idx)
                    paymentIntentId = payload[(idx + 1)..end];
            }
            return true;
        }

        logger.LogInformation("[STUB] Webhook Stripe ignorado (não é evento de teste stub).");
        paymentIntentId = "ignored";
        return true;
    }
}
