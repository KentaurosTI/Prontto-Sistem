using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

/// <summary>
/// Abstração do gateway de pagamento (ADR-01).
/// Implementação concreta em Infrastructure — nunca importar SDK diretamente no Domain/Application.
/// </summary>
public interface IProcessadorPagamento
{
    /// <summary>Gera uma cobrança PIX no gateway e retorna os dados para exibição ao Cliente.</summary>
    Task<ResultadoPix> GerarPixAsync(decimal valor, string descricao, TimeSpan expiracao);

    /// <summary>Transfere o valor ao Prestador após conclusão do serviço (split 80/20).</summary>
    Task TransferirAsync(decimal valor, string chavePix, string nomeBeneficiario, string referencia);

    /// <summary>Reembolsa 100% do valor ao Cliente (cancelamento ou disputa favorável ao Cliente).</summary>
    Task ReembolsarAsync(string pagarmeOrderId, decimal valor, string referencia);

    /// <summary>
    /// Cria um recipient no gateway de pagamento para o prestador receber splits automáticos.
    /// Retorna o recipientId (ex: "re_xxxx...").
    /// </summary>
    Task<string> CriarRecipientAsync(DadosBancarios dados, string nomeCompleto);
}

public record ResultadoPix(
    string PagarmeOrderId,
    string PixQrCode,
    string PixCopiaCola,
    DateTime PixExpiracaoEm
);
