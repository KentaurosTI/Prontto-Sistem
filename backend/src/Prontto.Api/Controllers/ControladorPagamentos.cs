using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Financeiro;
using Prontto.Domain.Enums;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/pagamentos")]
[Authorize]
public class ControladorPagamentos(IServicoConnect servicoConnect) : ControllerBase
{
    private Guid UsuarioId => User.GetRequiredUserId();

    private TipoConta TipoConta =>
        Enum.TryParse<TipoConta>(User.FindFirst("accountType")?.Value, ignoreCase: true, out var t)
            ? t : TipoConta.Cliente;

    /// <summary>Cria/continua o onboarding do prestador no Stripe Connect e retorna o link.</summary>
    [HttpPost("connect/onboarding")]
    public async Task<IActionResult> CriarOnboarding()
    {
        if (TipoConta != TipoConta.Prestador)
            return StatusCode(403, new { error = "Apenas prestadores configuram recebimento." });

        var resultado = await servicoConnect.CriarOnboardingAsync(UsuarioId);
        return Ok(new { url = resultado.Url });
    }

    /// <summary>Retorna a situação da conta conectada do prestador.</summary>
    [HttpGet("connect/status")]
    public async Task<IActionResult> Status()
    {
        if (TipoConta != TipoConta.Prestador)
            return StatusCode(403, new { error = "Apenas prestadores possuem conta de recebimento." });

        var status = await servicoConnect.ObterStatusAsync(UsuarioId);
        return Ok(status);
    }
}
