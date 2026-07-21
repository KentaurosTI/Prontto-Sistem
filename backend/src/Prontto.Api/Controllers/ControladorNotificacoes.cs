using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prontto.Domain.Interfaces;

namespace Prontto.Api.Controllers;

/// <summary>
/// Notificações do usuário autenticado (sino do header).
/// </summary>
[ApiController]
[Route("api/notificacoes")]
[Authorize]
public class ControladorNotificacoes(IRepositorioNotificacao repositorio) : ControllerBase
{
    private Guid IdUsuario => Guid.Parse(User.FindFirstValue("userId")!);

    /// <summary>Lista as notificações (até 50) + contagem de não lidas.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool apenasNaoLidas = false)
    {
        var itens = await repositorio.ListarPorUsuarioAsync(IdUsuario, apenasNaoLidas);
        var naoLidas = await repositorio.ContarNaoLidasAsync(IdUsuario);
        return Ok(new
        {
            naoLidas,
            notificacoes = itens.Select(n => new
            {
                id = n.Id,
                titulo = n.Titulo,
                mensagem = n.Mensagem,
                lida = n.Lida,
                tipo = n.Tipo,
                referenciaId = n.ReferenciaId,
                criadoEm = n.CriadoEm,
            }),
        });
    }

    /// <summary>Apenas a contagem de não lidas (para polling leve do badge).</summary>
    [HttpGet("nao-lidas")]
    public async Task<IActionResult> ContarNaoLidas()
        => Ok(new { total = await repositorio.ContarNaoLidasAsync(IdUsuario) });

    [HttpPost("{id:guid}/lida")]
    public async Task<IActionResult> MarcarLida(Guid id)
    {
        await repositorio.MarcarComoLidaAsync(id, IdUsuario);
        return Ok(new { message = "Notificação marcada como lida" });
    }

    [HttpPost("lidas")]
    public async Task<IActionResult> MarcarTodasLidas()
    {
        await repositorio.MarcarTodasComoLidasAsync(IdUsuario);
        return Ok(new { message = "Todas as notificações marcadas como lidas" });
    }
}
