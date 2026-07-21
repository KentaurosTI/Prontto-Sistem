using Microsoft.AspNetCore.Mvc;
using Prontto.Application.Perfil;

namespace Prontto.Api.Controllers;

/// <summary>
/// Endpoints públicos de descoberta de prestadores e catálogos.
/// Nenhuma autenticação necessária (CA-01, RN-07).
/// </summary>
[ApiController]
public class ControladorPrestadores(IServicoPerfilPrestador servicoPerfil) : ControllerBase
{
    /// <summary>
    /// Perfil público do prestador pela URL canônica.
    /// Rota: GET /{cidadeSlug}/{categoriaSlug}/{slug}
    /// </summary>
    [HttpGet("{cidadeSlug}/{categoriaSlug}/{slug}")]
    public async Task<IActionResult> ObterPerfilPublico(
        [FromRoute] string cidadeSlug,
        [FromRoute] string categoriaSlug,
        [FromRoute] string slug)
    {
        // cidadeSlug e categoriaSlug compõem a URL canônica para SEO,
        // mas o prestador é localizado pelo slug único — não há validação de cidade/categoria aqui
        // pois o mesmo prestador pode atuar em múltiplas cidades/categorias.
        var perfil = await servicoPerfil.ObterPerfilPublicoAsync(slug);
        return Ok(perfil);
    }

    /// <summary>
    /// Dados agregados para a página inicial: categorias, prestadores em destaque e avaliações recentes.
    /// GET /api/home [público]
    /// </summary>
    [HttpGet("api/home")]
    public async Task<IActionResult> ObterDadosHome()
    {
        var dados = await servicoPerfil.ObterDadosHomeAsync();
        return Ok(dados);
    }

    /// <summary>Lista todas as categorias ativas (para dropdowns no frontend).</summary>
    [HttpGet("api/categorias")]
    public async Task<IActionResult> ListarCategorias()
    {
        var categorias = await servicoPerfil.ListarCategoriasAsync();
        return Ok(categorias);
    }

    /// <summary>Lista todas as cidades ativas (para dropdowns no frontend).</summary>
    [HttpGet("api/cidades")]
    public async Task<IActionResult> ListarCidades()
    {
        var cidades = await servicoPerfil.ListarCidadesAsync();
        return Ok(cidades);
    }

    /// <summary>
    /// Busca paginada de prestadores por categoria (obrigatório) e cidade (opcional).
    /// Pública — sem autenticação. RN-01, RN-03, RN-04, RN-05.
    /// GET /api/prestadores?categoriaSlug=encanador&amp;cidadeSlug=sao-paulo&amp;page=1&amp;pageSize=20
    /// </summary>
    [HttpGet("api/prestadores")]
    public async Task<IActionResult> BuscarPrestadores(
        [FromQuery] string categoriaSlug,
        [FromQuery] string? cidadeSlug = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(categoriaSlug))
            return BadRequest(new { mensagem = "O parâmetro 'categoriaSlug' é obrigatório." });

        if (page < 1) page = 1;
        if (pageSize > 50) pageSize = 50;

        var resultado = await servicoPerfil.BuscarPrestadoresAsync(categoriaSlug, cidadeSlug, page, pageSize);
        return Ok(resultado);
    }
}
