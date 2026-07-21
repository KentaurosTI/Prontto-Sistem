using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Admin;
using Prontto.Application.Servicos;
using Prontto.Domain.Enums;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class ControladorAdmin(
    IServicoAdmin admin,
    IServicoDisputa servicoDisputa) : ControllerBase
{
    private Guid IdAdmin => Guid.Parse(User.FindFirstValue("userId")!);

    [HttpGet("stats")]
    public async Task<IActionResult> Estatisticas() => Ok(await admin.ObterEstatisticasAsync());

    // ── Usuários ──────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> Usuarios(
        [FromQuery] string? tipoConta,
        [FromQuery] Guid? cidadeId)
    {
        TipoConta? tipoContaFiltro = null;
        if (!string.IsNullOrWhiteSpace(tipoConta))
        {
            if (!Enum.TryParse<TipoConta>(tipoConta, ignoreCase: true, out var tipoContaParsed))
                return BadRequest(new { error = "tipoConta inválido. Use 'Cliente' ou 'Prestador'" });
            tipoContaFiltro = tipoContaParsed;
        }

        var usuarios = await admin.ListarUsuariosAsync(tipoContaFiltro, cidadeId);
        return Ok(new { users = usuarios.Select(DtoUsuario.De) });
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> ObterUsuario(Guid id)
    {
        var usuario = await admin.ObterUsuarioPorIdAsync(id);
        return Ok(new { user = DtoUsuario.De(usuario) });
    }

    [HttpPost("users/{id:guid}/bloquear")]
    public async Task<IActionResult> BloquearUsuario(Guid id)
    {
        await admin.BloquearUsuarioAsync(id, IdAdmin);
        return Ok(new { message = "Usuário bloqueado com sucesso" });
    }

    [HttpPost("users/{id:guid}/desbloquear")]
    public async Task<IActionResult> DesbloquearUsuario(Guid id)
    {
        await admin.DesbloquearUsuarioAsync(id, IdAdmin);
        return Ok(new { message = "Usuário desbloqueado com sucesso" });
    }

    [HttpPost("users/{id:guid}/revogar-sessoes")]
    public async Task<IActionResult> RevogarSessoes(Guid id)
    {
        await admin.RevogarSessoesAsync(id, IdAdmin);
        return Ok(new { message = "Sessões revogadas com sucesso" });
    }

    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> AtualizarUsuario(Guid id, [FromBody] RequisicaoEditarUsuario req)
    {
        var usuario = await admin.AtualizarUsuarioAsync(id, req.Nome, req.Telefone, IdAdmin);
        return Ok(new { user = usuario });
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> ExcluirUsuario(Guid id)
    {
        await admin.ExcluirUsuarioAsync(id, IdAdmin);
        return Ok(new { message = "Usuário excluído com sucesso" });
    }

    // ── Serviços ──────────────────────────────────────────────────────────────

    [HttpGet("services")]
    public async Task<IActionResult> Servicos()
    {
        var servicos = await admin.ListarServicosAsync();
        return Ok(new { services = servicos });
    }

    [HttpPatch("services/{id:guid}")]
    public async Task<IActionResult> AtualizarStatusServico(Guid id, [FromBody] RequisicaoStatus req)
    {
        if (!Enum.TryParse<StatusServico>(req.Status, ignoreCase: true, out var status))
            return BadRequest(new { error = "Status inválido" });

        var servico = await admin.AtualizarStatusServicoAsync(id, status);
        return Ok(new { service = servico });
    }

    [HttpPatch("services/{id:guid}/editar")]
    public async Task<IActionResult> EditarServico(Guid id, [FromBody] RequisicaoEditarServico req)
    {
        var servico = await admin.EditarServicoAsync(id, req.Titulo, req.Preco, IdAdmin);
        return Ok(new { service = servico });
    }

    [HttpDelete("services/{id:guid}")]
    public async Task<IActionResult> ExcluirServico(Guid id)
    {
        await admin.ExcluirServicoAsync(id, IdAdmin);
        return Ok(new { message = "Serviço excluído com sucesso" });
    }

    [HttpGet("services/{id:guid}/messages")]
    public async Task<IActionResult> ListarMensagens(Guid id)
    {
        var mensagens = await admin.ListarMensagensServicoAsync(id);
        return Ok(new { messages = mensagens });
    }

    [HttpPost("services/{id:guid}/messages")]
    public async Task<IActionResult> EnviarMensagem(Guid id, [FromBody] RequisicaoMensagem req)
    {
        var mensagem = await admin.EnviarMensagemAsync(id, IdAdmin, req.Conteudo);
        return StatusCode(201, new { message = mensagem });
    }

    // ── Cobranças ─────────────────────────────────────────────────────────────

    [HttpGet("charges")]
    public async Task<IActionResult> Cobrancas()
    {
        var cobrancas = await admin.ListarCobrancasAsync();
        return Ok(new { charges = cobrancas });
    }

    // ── Financeiro ────────────────────────────────────────────────────────────

    [HttpGet("financeiro")]
    public async Task<IActionResult> Financeiro()
    {
        var extrato = await admin.ObterExtratoFinanceiroAsync();
        return Ok(new
        {
            totalArrecadado = extrato.TotalArrecadado,
            totalPendente = extrato.TotalPendente,
            totalRetido = extrato.TotalRetido,
            ultimasCobrancas = extrato.UltimasCobrancas,
        });
    }

    // ── Audit Logs ────────────────────────────────────────────────────────────

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? usuarioId = null,
        [FromQuery] string? entidade = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var resultado = await admin.ListarLogsAsync(page, pageSize, usuarioId, entidade);
        return Ok(new
        {
            total = resultado.Total,
            pagina = resultado.Pagina,
            tamanhoPagina = resultado.TamanhoPagina,
            itens = resultado.Itens,
        });
    }

    // ── Imagens de portfólio ──────────────────────────────────────────────────

    [HttpGet("imagens/pendentes")]
    public async Task<IActionResult> ImagensPendentes()
    {
        var imagens = await admin.ListarImagensPendentesAsync();
        return Ok(new { imagens });
    }

    [HttpPatch("imagens/{id:guid}/moderar")]
    public async Task<IActionResult> ModerarImagem(Guid id, [FromBody] RequisicaoModeracao req)
    {
        await admin.ModerarImagemAsync(id, req.Aprovada, IdAdmin);
        return Ok(new { message = req.Aprovada ? "Imagem aprovada com sucesso" : "Imagem rejeitada com sucesso" });
    }

    // ── Disputas ──────────────────────────────────────────────────────────────

    [HttpGet("disputas")]
    public async Task<IActionResult> ListarDisputas()
    {
        var disputas = await servicoDisputa.ListarAbertasAsync();
        return Ok(new { disputas });
    }

    [HttpPatch("disputas/{id:guid}/resolver")]
    public async Task<IActionResult> ResolverDisputa(Guid id, [FromBody] RequisicaoResolverDisputa req)
    {
        if (string.IsNullOrWhiteSpace(req.DecisaoAdmin))
            return BadRequest(new { error = "A justificativa da decisão é obrigatória" });

        var disputa = await servicoDisputa.ResolverDisputaAsync(id, IdAdmin, req.FavorPrestador, req.DecisaoAdmin);
        return Ok(new { disputa });
    }
}

public record RequisicaoStatus(string Status);
public record RequisicaoEditarUsuario(string? Nome, string? Telefone);
public record RequisicaoEditarServico(string Titulo, decimal Preco);
public record RequisicaoMensagem(string Conteudo);
public record RequisicaoResolverDisputa(bool FavorPrestador, string DecisaoAdmin);
public record RequisicaoModeracao(bool Aprovada);
