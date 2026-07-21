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
