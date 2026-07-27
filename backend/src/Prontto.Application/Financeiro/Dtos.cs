namespace Prontto.Application.Financeiro;

public record DtoCobranca(
    Guid Id,
    Guid ServicoId,
    decimal ValorTotal,
    decimal TaxaAdmin,
    decimal ValorPrestador,
    string Status,
    string? PixQrCode,
    string? PixCopiaCola,
    DateTime? PixExpiracaoEm,
    string? StripePaymentIntentId,
    int Parcelas,
    DateTime? PagadoEm,
    DateTime? RetidoEm,
    DateTime? LiberadoEm,
    DateTime CriadoEm
);

public record DtoExtratoFinanceiro(
    decimal ReceitaTotal,
    decimal ReceitaPendente,
    decimal Gmv,
    List<DtoCobranca> Cobrancas
);

public record DtoInicioCartao(
    string ClientSecret,
    string PublishableKey,
    int Parcelas,
    decimal ValorTotal
);
