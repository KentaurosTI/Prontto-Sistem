namespace Prontto.Domain.Interfaces;

public interface IProcessadorCartao
{
    string ObterPublishableKey();
    Task<ResultadoCartao> GerarCartaoAsync(decimal valor, string descricao, int parcelas);
    bool ValidarAssinaturaWebhook(string payload, string assinaturaStripe, out string paymentIntentId);
}

public record ResultadoCartao(
    string PaymentIntentId,
    string ClientSecret,
    string PublishableKey,
    int Parcelas,
    decimal ValorTotal
);
