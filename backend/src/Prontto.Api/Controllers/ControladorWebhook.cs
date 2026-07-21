using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Financeiro;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class ControladorWebhook(IServicoFinanceiro servicoFinanceiro) : ControllerBase
{
    /// <summary>
    /// Recebe notificações da Pagar.me (RN-07: valida HMAC-SHA256).
    /// Responde 200 imediatamente; processamento é feito na mesma thread mas sem bloquear o gateway.
    /// </summary>
    [HttpPost("pagarme")]
    public async Task<IActionResult> ReceberWebhookPagarme()
    {
        // Lê o payload bruto para validação HMAC
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        // Pagar.me envia assinatura no header X-Pagarme-Signature
        var assinatura = Request.Headers["X-Pagarme-Signature"].FirstOrDefault()
                      ?? Request.Headers["X-Hub-Signature-256"].FirstOrDefault()
                      ?? string.Empty;

        try
        {
            await servicoFinanceiro.ProcessarWebhookAsync(payload, assinatura);
            return Ok(new { received = true });
        }
        catch (Prontto.Application.Common.ExcecaoNaoAutorizado)
        {
            return Unauthorized(new { error = "Assinatura inválida" });
        }
        catch (Exception)
        {
            // Retorna 200 para evitar reenvios excessivos do gateway; log já feito no serviço
            return Ok(new { received = true, warning = "processed_with_error" });
        }
    }
}
