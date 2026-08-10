using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Financeiro;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class ControladorWebhooks(IServicoCheckoutStripe checkoutStripe) : ControllerBase
{
    /// <summary>Recebe eventos do Stripe (payment_intent.succeeded → confirma o pagamento).</summary>
    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> Stripe()
    {
        using var leitor = new StreamReader(Request.Body);
        var payload = await leitor.ReadToEndAsync();
        var assinatura = Request.Headers["Stripe-Signature"].ToString();

        await checkoutStripe.ProcessarWebhookAsync(payload, assinatura);
        return Ok();
    }
}
