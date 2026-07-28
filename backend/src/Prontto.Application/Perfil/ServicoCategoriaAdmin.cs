using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Perfil;

public record DtoCategoriaAdmin(
    Guid Id, string Nome, string Slug, string? Descricao, string? Imagem, bool Ativa, int Ordem);

public record ComandoCriarCategoria(string Nome, string? Descricao, string? Imagem, int? Ordem);
public record ComandoEditarCategoria(string Nome, string? Descricao, string? Imagem, bool Ativa, int? Ordem);

public interface IServicoCategoriaAdmin
{
    Task<List<DtoCategoriaAdmin>> ListarTodasAsync();
    Task<DtoCategoriaAdmin> CriarAsync(ComandoCriarCategoria cmd);
    Task<DtoCategoriaAdmin> EditarAsync(Guid id, ComandoEditarCategoria cmd);
    Task<DtoCategoriaAdmin> AlternarAtivaAsync(Guid id);
    Task ExcluirAsync(Guid id);
}

public class ServicoCategoriaAdmin(
    IRepositorioCategoria repositorio,
    IMemoryCache cache) : IServicoCategoriaAdmin
{
    private const string ChaveCacheCategorias = "categorias";

    public async Task<List<DtoCategoriaAdmin>> ListarTodasAsync()
    {
        var lista = await repositorio.ListarTodasAsync();
        return lista.Select(Mapear).ToList();
    }

    public async Task<DtoCategoriaAdmin> CriarAsync(ComandoCriarCategoria cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Nome))
            throw new ExcecaoValidacao("O nome da categoria é obrigatório.");

        var slug = await GerarSlugUnicoAsync(cmd.Nome);
        var categoria = new Categoria
        {
            Nome = cmd.Nome.Trim(),
            Slug = slug,
            Descricao = string.IsNullOrWhiteSpace(cmd.Descricao) ? null : cmd.Descricao.Trim(),
            Imagem = string.IsNullOrWhiteSpace(cmd.Imagem) ? null : cmd.Imagem.Trim(),
            Ativa = true,
            Ordem = cmd.Ordem ?? 999,
        };

        var criada = await repositorio.AdicionarAsync(categoria);
        InvalidarCache();
        return Mapear(criada);
    }

    public async Task<DtoCategoriaAdmin> EditarAsync(Guid id, ComandoEditarCategoria cmd)
    {
        var categoria = await repositorio.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Categoria não encontrada.");

        if (string.IsNullOrWhiteSpace(cmd.Nome))
            throw new ExcecaoValidacao("O nome da categoria é obrigatório.");

        // Regenera o slug apenas se o nome mudou.
        if (!string.Equals(categoria.Nome, cmd.Nome.Trim(), StringComparison.Ordinal))
            categoria.Slug = await GerarSlugUnicoAsync(cmd.Nome, id);

        categoria.Nome = cmd.Nome.Trim();
        categoria.Descricao = string.IsNullOrWhiteSpace(cmd.Descricao) ? null : cmd.Descricao.Trim();
        categoria.Imagem = string.IsNullOrWhiteSpace(cmd.Imagem) ? null : cmd.Imagem.Trim();
        categoria.Ativa = cmd.Ativa;
        if (cmd.Ordem.HasValue) categoria.Ordem = cmd.Ordem.Value;

        await repositorio.AtualizarAsync(categoria);
        InvalidarCache();
        return Mapear(categoria);
    }

    public async Task<DtoCategoriaAdmin> AlternarAtivaAsync(Guid id)
    {
        var categoria = await repositorio.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Categoria não encontrada.");
        categoria.Ativa = !categoria.Ativa;
        await repositorio.AtualizarAsync(categoria);
        InvalidarCache();
        return Mapear(categoria);
    }

    public async Task ExcluirAsync(Guid id)
    {
        var categoria = await repositorio.ObterPorIdAsync(id)
            ?? throw new ExcecaoNaoEncontrado("Categoria não encontrada.");
        await repositorio.RemoverAsync(categoria);
        InvalidarCache();
    }

    private void InvalidarCache() => cache.Remove(ChaveCacheCategorias);

    private static DtoCategoriaAdmin Mapear(Categoria c)
        => new(c.Id, c.Nome, c.Slug, c.Descricao, c.Imagem, c.Ativa, c.Ordem);

    private async Task<string> GerarSlugUnicoAsync(string nome, Guid? ignorarId = null)
    {
        var baseSlug = Slugificar(nome);
        var slug = baseSlug;
        var n = 2;
        while (true)
        {
            var existente = await repositorio.ObterPorSlugAsync(slug);
            if (existente is null || existente.Id == ignorarId) return slug;
            slug = $"{baseSlug}-{n++}";
        }
    }

    private static string Slugificar(string texto)
    {
        var normalizado = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalizado)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') sb.Append('-');
        }
        var slug = sb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
