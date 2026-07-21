using Microsoft.Extensions.Logging;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Stub do IProcessadorPagamento para desenvolvimento.
/// Substitua por ProcessadorPagamentoPagarme em produção (configurado via IWebHostEnvironment em InjecaoDependencias).
/// </summary>
public class ProcessadorPagamentoStub(ILogger<ProcessadorPagamentoStub> logger) : IProcessadorPagamento
{
    public Task<ResultadoPix> GerarPixAsync(decimal valor, string descricao, TimeSpan expiracao)
    {
        var orderId = $"STUB-{Guid.NewGuid():N}";
        var resultado = new ResultadoPix(
            PagarmeOrderId: orderId,
            PixQrCode: $"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==",
            PixCopiaCola: $"00020126580014BR.GOV.BCB.PIX0136{Guid.NewGuid()}5204000053039865802BR5913Prontto Admin6009SAO PAULO62070503***6304{valor:F0}",
            PixExpiracaoEm: DateTime.UtcNow.Add(expiracao)
        );

        logger.LogInformation("[STUB] PIX gerado: OrderId={OrderId}, Valor={Valor}", orderId, valor);
        return Task.FromResult(resultado);
    }

    public Task TransferirAsync(decimal valor, string chavePix, string nomeBeneficiario, string referencia)
    {
        logger.LogInformation("[STUB] Transferência PIX: Valor={Valor}, Chave={ChavePix}, Ref={Referencia}",
            valor, chavePix, referencia);
        return Task.CompletedTask;
    }

    public Task ReembolsarAsync(string pagarmeOrderId, decimal valor, string referencia)
    {
        logger.LogInformation("[STUB] Reembolso: OrderId={OrderId}, Valor={Valor}, Ref={Referencia}",
            pagarmeOrderId, valor, referencia);
        return Task.CompletedTask;
    }

    public Task<string> CriarRecipientAsync(DadosBancarios dados, string nomeCompleto)
    {
        var recipientId = $"re_stub_{Guid.NewGuid():N}"[..20];
        logger.LogInformation("[STUB] Recipient criado: RecipientId={RecipientId}, Usuario={UsuarioId}",
            recipientId, dados.UsuarioId);
        return Task.FromResult(recipientId);
    }
}
