namespace Prontto.Application.Financeiro;

public interface IServicoFinanceiro
{
    /// <summary>Gera PIX para uma cobrança existente (chamado após acordo de preço).</summary>
    Task<DtoCobranca> GerarPixAsync(Guid cobrancaId);

    /// <summary>Inicia pagamento por cartão via Stripe — retorna clientSecret para o frontend.</summary>
    Task<DtoInicioCartao> IniciarPagamentoCartaoAsync(Guid servicoId, int parcelas);

    /// <summary>Processa webhook da Pagar.me — valida HMAC e atualiza estados.</summary>
    Task ProcessarWebhookAsync(string payload, string assinaturaHmac);

    /// <summary>Processa webhook do Stripe — valida assinatura e atualiza estados.</summary>
    Task ProcessarWebhookStripeAsync(string payload, string assinaturaStripe);

    /// <summary>Libera pagamento ao Prestador com split 80/20 (após serviço Concluido).</summary>
    Task LiberarPagamentoAsync(Guid servicoId);

    /// <summary>Reembolsa 100% ao Cliente (cancelamento ou disputa favorável ao Cliente).</summary>
    Task ReembolsarAsync(Guid servicoId);

    /// <summary>Retorna a cobrança de um serviço.</summary>
    Task<DtoCobranca?> ObterPorServicoAsync(Guid servicoId);
}
