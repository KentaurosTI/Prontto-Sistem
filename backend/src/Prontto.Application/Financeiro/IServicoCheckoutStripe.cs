namespace Prontto.Application.Financeiro;

/// <summary>Checkout de cartão via Stripe (PaymentIntent + split 80/20 quando o prestador está no Connect).</summary>
public interface IServicoCheckoutStripe
{
    /// <summary>Cria o PaymentIntent do serviço e retorna o client_secret para o Elements.</summary>
    Task<string> CriarPaymentIntentAsync(Guid servicoId, Guid usuarioId);

    /// <summary>Processa o webhook do Stripe (valida assinatura e confirma o pagamento).</summary>
    Task ProcessarWebhookAsync(string payload, string assinatura);
}
