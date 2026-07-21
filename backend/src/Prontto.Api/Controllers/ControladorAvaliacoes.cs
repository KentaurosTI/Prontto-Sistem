using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Avaliacoes;

namespace Prontto.Api.Controllers;

[ApiController]
public class ControladorAvaliacoes(IServicoAvaliacao servicoAvaliacao) : ControllerBase
{
    /// <summary>
    /// Registra avaliação de um participante após conclusão do serviço.
    /// POST /api/servicos/{id}/avaliacoes [Authorize]
    /// </summary>
    [HttpPost("api/servicos/{id:guid}/avaliacoes")]
    [Authorize]
    public async Task<IActionResult> Registrar(Guid id, [FromBody] RequisicaoAvaliacao req)
    {
        var avaliadorId = ObterUsuarioId();
        var comando = new ComandoRegistrarAvaliacao(req.Nota, req.Comentario);
        var avaliacao = await servicoAvaliacao.RegistrarAsync(id, avaliadorId, comando);
        return StatusCode(201, new { avaliacao });
    }

    /// <summary>
    /// Lista avaliações públicas de um prestador pelo slug.
    /// GET /api/prestadores/{slug}/avaliacoes [público]
    /// </summary>
    [HttpGet("api/prestadores/{slug}/avaliacoes")]
    [AllowAnonymous]
    public async Task<IActionResult> ListarPorPrestador(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize > 50) pageSize = 50;

        var resultado = await servicoAvaliacao.ListarPorPrestadorSlugAsync(slug, page, pageSize);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista avaliações de um serviço específico (apenas para participantes).
    /// GET /api/servicos/{id}/avaliacoes [Authorize]
    /// </summary>
    [HttpGet("api/servicos/{id:guid}/avaliacoes")]
    [Authorize]
    public async Task<IActionResult> ListarPorServico(Guid id)
    {
        var usuarioId = ObterUsuarioId();
        var avaliacoes = await servicoAvaliacao.ListarPorServicoAsync(id, usuarioId);
        return Ok(new { avaliacoes });
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private Guid ObterUsuarioId()
        => Guid.Parse(User.FindFirstValue("userId")!);
}

// ── Request Records ────────────────────────────────────────────────────────────

public record RequisicaoAvaliacao(int Nota, string? Comentario);
