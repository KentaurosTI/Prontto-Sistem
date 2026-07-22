using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Prontto.Application.Auth;
using Prontto.Application.Common;
using Prontto.Application.Perfil;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class ControladorAuth(
    IServicoAutenticacao servicoAuth,
    IRepositorioBanking banking,
    IServicoPerfilPrestador servicoPerfil,
    IRepositorioPerfilPrestador repositorioPerfil,
    IProcessadorPagamento processadorPagamento,
    IRepositorioUsuario repositorioUsuario,
    IArmazenamentoArquivo armazenamentoArquivo,
    ILogger<ControladorAuth> logger) : ControllerBase
{
    private const string NomeCookieRefreshToken = "prontto_refresh_token";
    private const string NomeCookieAccessToken = "prontto_access_token";

    [HttpPost("register")]
    [EnableRateLimiting("cadastro")]
    public async Task<IActionResult> Cadastrar([FromBody] RequisicaoCadastro req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!Enum.TryParse<TipoConta>(req.TipoConta, ignoreCase: true, out var tipoConta))
            return BadRequest(new { error = "Tipo de conta inválido" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var resultado = await servicoAuth.CadastrarAsync(new ComandoCadastro(
            req.Nome, req.Email, req.Senha, tipoConta,
            req.Telefone, req.Especialidade, req.CidadeId,
            ip, userAgent));

        DefinirCookieRefreshToken(resultado.RefreshToken);
        DefinirCookieAccessToken(resultado.Token);

        return StatusCode(201, new { user = DtoUsuario.De(resultado.Usuario) });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Entrar([FromBody] RequisicaoLogin req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var resultado = await servicoAuth.EntrarAsync(new ComandoLogin(req.Email, req.Senha, ip, userAgent));

        DefinirCookieRefreshToken(resultado.RefreshToken);
        DefinirCookieAccessToken(resultado.Token);

        return Ok(new { user = DtoUsuario.De(resultado.Usuario) });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Renovar()
    {
        var tokenBruto = Request.Cookies[NomeCookieRefreshToken];
        if (string.IsNullOrEmpty(tokenBruto))
            return Unauthorized(new { error = "Refresh token ausente" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var resultado = await servicoAuth.RenovarSessaoAsync(tokenBruto, ip, userAgent);

        DefinirCookieRefreshToken(resultado.RefreshToken);
        DefinirCookieAccessToken(resultado.Token);

        return Ok(new { user = DtoUsuario.De(resultado.Usuario) });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var tokenBruto = Request.Cookies[NomeCookieRefreshToken];
        if (!string.IsNullOrEmpty(tokenBruto))
            await servicoAuth.LogoutAsync(tokenBruto);

        RemoverCookieRefreshToken();
        RemoverCookieAccessToken();

        return Ok(new { message = "Logout realizado com sucesso" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ObterPerfil()
    {
        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);
        var usuario = await servicoAuth.ObterUsuarioAtualAsync(idUsuario);
        return Ok(new { user = DtoUsuario.De(usuario) });
    }

    /// <summary>
    /// Atualiza o cadastro do próprio usuário (nome, telefone, cidade e endereço).
    /// Usado pela aba "Meu Perfil" do cliente.
    /// </summary>
    [HttpPut("meu-cadastro")]
    [Authorize]
    public async Task<IActionResult> AtualizarMeuCadastro([FromBody] RequisicaoMeuCadastro req)
    {
        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);
        var usuario = await repositorioUsuario.ObterPorIdAsync(idUsuario)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        if (!string.IsNullOrWhiteSpace(req.Nome))
            usuario.Nome = req.Nome.Trim();
        usuario.Telefone = string.IsNullOrWhiteSpace(req.Telefone) ? null : req.Telefone.Trim();
        usuario.CidadeId = req.CidadeId;
        usuario.Endereco = string.IsNullOrWhiteSpace(req.Endereco) ? null : req.Endereco.Trim();
        usuario.AtualizadoEm = DateTime.UtcNow;

        var atualizado = await repositorioUsuario.AtualizarAsync(usuario);
        return Ok(new { user = DtoUsuario.De(atualizado) });
    }

    [HttpGet("banking")]
    [Authorize]
    public async Task<IActionResult> ObterDadosBancarios()
    {
        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);
        var dados = await banking.ObterPorUsuarioIdAsync(idUsuario);
        return Ok(new { banking = dados });
    }

    [HttpPost("banking")]
    [Authorize]
    public async Task<IActionResult> SalvarDadosBancarios([FromBody] RequisicaoBanking req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tipoConta = User.FindFirstValue("accountType");
        var papel = User.FindFirstValue(ClaimTypes.Role);
        if (tipoConta != "prestador" && papel != "admin")
            return StatusCode(403, new { error = "Apenas prestadores podem cadastrar dados bancários" });

        if (!Enum.TryParse<TipoChavePix>(req.TipoChavePix, ignoreCase: true, out var tipoChavePix))
            return BadRequest(new { error = "Tipo de chave Pix inválido" });

        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);
        var dadosBancarios = new Prontto.Domain.Entities.DadosBancarios
        {
            UsuarioId = idUsuario,
            TipoChavePix = tipoChavePix,
            ChavePix = req.ChavePix.Trim(),
            NomeCompleto = req.NomeCompleto.Trim(),
            CpfCnpj = req.CpfCnpj.Trim(),
            NomeBanco = req.NomeBanco?.Trim(),
            Agencia = req.Agencia?.Trim(),
            NumeroConta = req.NumeroConta?.Trim(),
            TipoConta = req.TipoConta,
        };

        var resultado = await banking.SalvarAsync(dadosBancarios);

        // Criar recipient no gateway de pagamento para split automático
        // Não falhar o salvamento se a criação do recipient falhar
        if (string.IsNullOrWhiteSpace(resultado.PagarmeRecipientId))
        {
            try
            {
                var usuario = await repositorioUsuario.ObterPorIdAsync(idUsuario);
                var nomeCompleto = usuario?.Nome ?? req.NomeCompleto.Trim();
                var recipientId = await processadorPagamento.CriarRecipientAsync(resultado, nomeCompleto);
                resultado.PagarmeRecipientId = recipientId;
                await banking.SalvarAsync(resultado);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Falha ao criar recipient no gateway de pagamento para usuario {UsuarioId}. " +
                    "Dados bancários foram salvos — recipient pode ser criado manualmente.",
                    idUsuario);
                // Não relança — o prestador ainda pode operar sem o recipient
            }
        }

        return Ok(new { banking = resultado });
    }

    // ── Perfil do Prestador ────────────────────────────────────────────────────

    /// <summary>
    /// Atualiza o perfil público do prestador autenticado.
    /// O Slug é gerado na primeira chamada e nunca sobrescrito (ADR-09).
    /// Apenas prestadores podem chamar este endpoint.
    /// </summary>
    [HttpPut("perfil")]
    [Authorize]
    public async Task<IActionResult> AtualizarPerfil([FromBody] RequisicaoAtualizarPerfil req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tipoConta = User.FindFirstValue("accountType");
        if (tipoConta != "prestador")
            return StatusCode(403, new { error = "Apenas prestadores podem editar o perfil público" });

        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);

        var perfil = await servicoPerfil.AtualizarPerfilAsync(idUsuario, new ComandoAtualizarPerfil(
            req.Nome,
            req.Descricao,
            req.Especialidade,
            req.FotoPerfilUrl,
            req.CategoriaIds,
            req.CidadeIds));

        return Ok(new { perfil });
    }

    // ── Portfólio ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lista as imagens do portfólio do prestador autenticado, ordenadas por Ordem.
    /// Retorna apenas imagens aprovadas (não excluídas).
    /// </summary>
    [HttpGet("portfolio")]
    [Authorize]
    public async Task<IActionResult> ListarImagens()
    {
        var tipoConta = User.FindFirstValue("accountType");
        if (tipoConta != "prestador")
            return StatusCode(403, new { error = "Apenas prestadores têm portfólio" });

        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);
        var imagens = await repositorioPerfil.ListarImagensAprovadasAsync(idUsuario);

        var resultado = imagens.Select(i => new { id = i.Id, url = i.Url, ordem = i.Ordem });
        return Ok(new { imagens = resultado });
    }

    /// <summary>
    /// Faz upload de uma imagem de portfólio diretamente para o servidor (armazenamento local).
    /// Aceita arquivos jpg, jpeg, png e webp até 5 MB.
    /// </summary>
    [HttpPost("portfolio/upload")]
    [Authorize]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImagem([FromForm] IFormFile? arquivo)
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

        var tipoConta = User.FindFirstValue("accountType");
        if (tipoConta != "prestador")
            return StatusCode(403, new { error = "Apenas prestadores podem adicionar imagens ao portfólio" });

        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);

        var urlRelativa = await armazenamentoArquivo.SalvarAsync(
            arquivo.OpenReadStream(),
            arquivo.FileName,
            arquivo.ContentType);

        var imagem = new ImagemPortfolio
        {
            UsuarioId = idUsuario,
            Url = urlRelativa,
            CloudinaryPublicId = string.Empty, // Não usamos Cloudinary nesta implementação
            Moderada = true,  // Armazenamento local: sem moderação externa
            Aprovada = true,  // Aprovada imediatamente (sem pipeline de moderação)
        };

        await repositorioPerfil.AdicionarImagemAsync(imagem);

        logger.LogInformation(
            "Imagem de portfólio salva para usuario {UsuarioId}: {Url}",
            idUsuario, urlRelativa);

        return StatusCode(201, new { id = imagem.Id, url = imagem.Url });
    }

    /// <summary>
    /// Faz upload da foto de perfil (armazenamento local, hospedado na própria Prontto).
    /// Retorna a URL relativa; o cliente a envia ao salvar o perfil.
    /// </summary>
    [HttpPost("perfil/foto/upload")]
    [Authorize]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFotoPerfil([FromForm] IFormFile? arquivo)
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

        var urlRelativa = await armazenamentoArquivo.SalvarAsync(
            arquivo.OpenReadStream(),
            arquivo.FileName,
            arquivo.ContentType);

        return StatusCode(201, new { url = urlRelativa });
    }

    /// <summary>
    /// Registra uma imagem de portfólio pelo URL (após upload direto ao Cloudinary pelo frontend).
    /// Mantido para compatibilidade com integrações anteriores.
    /// </summary>
    [HttpPost("portfolio")]
    [Authorize]
    public async Task<IActionResult> AdicionarImagem([FromBody] RequisicaoAdicionarImagem req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tipoConta = User.FindFirstValue("accountType");
        if (tipoConta != "prestador")
            return StatusCode(403, new { error = "Apenas prestadores podem adicionar imagens ao portfólio" });

        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);

        var imagem = new ImagemPortfolio
        {
            UsuarioId = idUsuario,
            Url = req.Url.Trim(),
            CloudinaryPublicId = req.CloudinaryPublicId.Trim(),
            Moderada = false,
            Aprovada = null, // Pendente de moderação
            Ordem = req.Ordem,
        };

        await repositorioPerfil.AdicionarImagemAsync(imagem);

        return StatusCode(201, new
        {
            imagem = new { imagem.Id, imagem.Url, imagem.Ordem, status = "pendente_moderacao" }
        });
    }

    /// <summary>
    /// Remove uma imagem do portfólio (soft delete).
    /// Apenas o proprietário da imagem pode removê-la.
    /// </summary>
    [HttpDelete("portfolio/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoverImagem([FromRoute] Guid id)
    {
        var tipoConta = User.FindFirstValue("accountType");
        if (tipoConta != "prestador")
            return StatusCode(403, new { error = "Apenas prestadores podem remover imagens do portfólio" });

        var idUsuario = Guid.Parse(User.FindFirstValue("userId")!);

        var imagem = await repositorioPerfil.ObterImagemPorIdAsync(id);
        if (imagem is null)
            return NotFound(new { error = "Imagem não encontrada" });

        if (imagem.UsuarioId != idUsuario)
            return StatusCode(403, new { error = "Acesso negado" });

        await repositorioPerfil.RemoverImagemAsync(imagem);

        // Remover arquivo físico se for armazenamento local (URL começa com /uploads/)
        if (!string.IsNullOrWhiteSpace(imagem.Url) && imagem.Url.StartsWith("/uploads/"))
            await armazenamentoArquivo.RemoverAsync(imagem.Url);

        return NoContent();
    }

    // ── Helpers de cookie ──────────────────────────────────────────────────────

    private void DefinirCookieRefreshToken(string tokenBruto)
    {
        var opcoes = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            // None: cookie precisa ir em requisições cross-site (prontto.org → api.prontto.org).
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/api/auth",
        };
        Response.Cookies.Append(NomeCookieRefreshToken, tokenBruto, opcoes);
    }

    private void RemoverCookieRefreshToken()
    {
        Response.Cookies.Delete(NomeCookieRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
        });
    }

    private void DefinirCookieAccessToken(string token)
    {
        var opcoes = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15),
            Path = "/",
        };
        Response.Cookies.Append(NomeCookieAccessToken, token, opcoes);
    }

    private void RemoverCookieAccessToken()
    {
        Response.Cookies.Delete(NomeCookieAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
        });
    }
}

public record RequisicaoCadastro(
    string Nome, string Email, string Senha, string TipoConta,
    string? Telefone = null, string? Especialidade = null, Guid? CidadeId = null);

public record RequisicaoLogin(string Email, string Senha);

public record RequisicaoBanking(
    string TipoChavePix, string ChavePix, string NomeCompleto, string CpfCnpj,
    string? NomeBanco = null, string? Agencia = null,
    string? NumeroConta = null, string? TipoConta = null);

public record RequisicaoAtualizarPerfil(
    string? Nome = null,
    string? Descricao = null,
    string? Especialidade = null,
    string? FotoPerfilUrl = null,
    List<Guid>? CategoriaIds = null,
    List<Guid>? CidadeIds = null);

public record RequisicaoAdicionarImagem(
    string Url,
    string CloudinaryPublicId,
    int Ordem = 0);

public record RequisicaoMeuCadastro(
    string? Nome = null,
    string? Telefone = null,
    Guid? CidadeId = null,
    string? Endereco = null);
