using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/sugestoes")]
public class ControladorSugestoes(
    IRepositorioSugestao repositorioSugestoes,
    IServicoEmail servicoEmail) : ControllerBase
{
    /// <summary>
    /// Recebe uma sugestão de serviço (quando a busca não encontra o serviço).
    /// Público: o visitante pode não estar logado. Salva no banco e notifica o admin por e-mail.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("servicos-criticos")]
    public async Task<IActionResult> Enviar([FromBody] RequisicaoSugestao req)
    {
        if (string.IsNullOrWhiteSpace(req.Titulo))
            return BadRequest(new { error = "Informe o título do serviço." });
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "Informe um e-mail para contato." });

        var sugestao = await repositorioSugestoes.AdicionarAsync(new SugestaoServico
        {
            Email = req.Email.Trim(),
            Telefone = (req.Telefone ?? string.Empty).Trim(),
            Titulo = req.Titulo.Trim(),
            Descricao = (req.Descricao ?? string.Empty).Trim(),
        });

        var corpo =
            $"<h2>Nova sugestão de serviço</h2>" +
            $"<p><b>Serviço:</b> {WebUtility.HtmlEncode(sugestao.Titulo)}</p>" +
            $"<p><b>Descrição:</b> {WebUtility.HtmlEncode(sugestao.Descricao)}</p>" +
            $"<p><b>E-mail:</b> {WebUtility.HtmlEncode(sugestao.Email)}</p>" +
            $"<p><b>Telefone:</b> {WebUtility.HtmlEncode(sugestao.Telefone)}</p>" +
            $"<p><small>Recebido em {sugestao.CriadoEm:dd/MM/yyyy HH:mm} (UTC)</small></p>";

        await servicoEmail.EnviarParaAdminAsync(
            assunto: $"[Prontto] Sugestão de serviço: {sugestao.Titulo}",
            corpoHtml: corpo,
            responderPara: sugestao.Email);

        return StatusCode(201, new { ok = true });
    }
}

public record RequisicaoSugestao(string Email, string? Telefone, string Titulo, string? Descricao);
