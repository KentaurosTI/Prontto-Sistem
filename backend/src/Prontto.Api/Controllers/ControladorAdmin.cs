using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Admin;
using Prontto.Application.Perfil;
using Prontto.Application.Servicos;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class ControladorAdmin(
    IServicoAdmin admin,
    IServicoDisputa servicoDisputa,
    IRepositorioSugestao repositorioSugestoes,
    IServicoCategoriaAdmin servicoCategorias,
    IArmazenamentoArquivo armazenamentoArquivo) : ControllerBase
{
    private Guid IdAdmin => User.GetRequiredUserId();

    [HttpGet("stats")]
    public async Task<IActionResult> Estatisticas() => Ok(await admin.ObterEstatisticasAsync());

    [HttpGet("sugestoes")]
    public async Task<IActionResult> Sugestoes()
    {
        var sugestoes = await repositorioSugestoes.ListarAsync();
        return Ok(new { sugestoes });
    }

    // ── Catálogo de categorias (RF — "Incluir novo serviço") ────────────────────

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias()
        => Ok(new { categorias = await servicoCategorias.ListarTodasAsync() });

    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria([FromBody] ComandoCriarCategoria cmd)
        => StatusCode(201, new { categoria = await servicoCategorias.CriarAsync(cmd) });

    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> EditarCategoria(Guid id, [FromBody] ComandoEditarCategoria cmd)
        => Ok(new { categoria = await servicoCategorias.EditarAsync(id, cmd) });

    [HttpPatch("categorias/{id:guid}/alternar-ativa")]
    public async Task<IActionResult> AlternarCategoria(Guid id)
        => Ok(new { categoria = await servicoCategorias.AlternarAtivaAsync(id) });

    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> ExcluirCategoria(Guid id)
    {
        await servicoCategorias.ExcluirAsync(id);
        return NoContent();
    }

    [HttpPost("categorias/imagem/upload")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImagemCategoria([FromForm] IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado" });
        if (arquivo.Length > 5_242_880)
            return BadRequest(new { error = "Arquivo maior que 5 MB" });

        var extensoesPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp" };
        var extensao = Path.GetExtension(arquivo.FileName);
        if (string.IsNullOrWhiteSpace(extensao) || !extensoesPermitidas.Contains(extensao))
            return BadRequest(new { error = "Tipo não permitido. Use jpg, png ou webp" });

        var url = await armazenamentoArquivo.SalvarAsync(
            arquivo.OpenReadStream(), arquivo.FileName, arquivo.ContentType);
        return StatusCode(201, new { url });
    }

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
