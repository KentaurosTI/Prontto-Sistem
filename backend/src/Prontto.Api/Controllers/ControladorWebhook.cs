using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Financeiro;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class ControladorWebhook(IServicoFinanceiro servicoFinanceiro) : ControllerBase
{
    /// <summary>
    /// Recebe notificações da Pagar.me (RN-07: valida HMAC-SHA256).
    /// </summary>
    [HttpPost("pagarme")]
    public async Task<IActionResult> ReceberWebhookPagarme()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

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
        catch (Prontto.Application.Common.ExcecaoValidacao ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Erro interno ao processar webhook" });
        }
    }

    /// <summary>
    /// Recebe notificações do Stripe (payment_intent.succeeded, etc.).
    /// Valida assinatura via Stripe-Signature header.
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> ReceberWebhookStripe()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var assinatura = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        try
        {
            await servicoFinanceiro.ProcessarWebhookStripeAsync(payload, assinatura);
            return Ok(new { received = true });
        }
        catch (Prontto.Application.Common.ExcecaoNaoAutorizado)
        {
            return Unauthorized(new { error = "Assinatura Stripe inválida" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Erro interno ao processar webhook Stripe" });
        }
    }
}
