using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Perfil;

public class ServicoPerfilPrestador(
    IRepositorioUsuario repositorioUsuarios,
    IRepositorioPerfilPrestador repositorioPerfil,
    IRepositorioCategoria repositorioCategorias,
    IRepositorioCidade repositorioCidades,
    IRepositorioAvaliacao repositorioAvaliacoes,
    IMemoryCache cache) : IServicoPerfilPrestador
{
    private const int MaxTentativasSlug = 5;
    private const string ChaveCacheCategorias = "categorias";
    private static readonly TimeSpan TtlCategorias = TimeSpan.FromHours(1);
    private static readonly TimeSpan TtlPerfilPrestador = TimeSpan.FromMinutes(5);

    public async Task<DtoPerfilPublico> AtualizarPerfilAsync(Guid usuarioId, ComandoAtualizarPerfil comando)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(usuarioId)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

        if (usuario.TipoConta != TipoConta.Prestador)
            throw new ExcecaoProibido("Apenas prestadores podem editar o perfil público");

        // Atualiza campos editáveis
        if (!string.IsNullOrWhiteSpace(comando.Nome))
            usuario.Nome = comando.Nome.Trim();

        if (comando.Descricao is not null)
            usuario.Descricao = comando.Descricao.Trim();

        if (comando.Especialidade is not null)
            usuario.Especialidade = comando.Especialidade.Trim();

        if (comando.FotoPerfilUrl is not null)
            usuario.FotoPerfilUrl = string.IsNullOrWhiteSpace(comando.FotoPerfilUrl)
                ? null
                : comando.FotoPerfilUrl.Trim();

        // Slug: write-once (ADR-09) — gerado apenas se ainda não existir
        if (string.IsNullOrEmpty(usuario.Slug))
            usuario.Slug = await GerarSlugUnicoAsync(usuario.Nome);

        usuario.AtualizadoEm = DateTime.UtcNow;

        var categoriaIds = comando.CategoriaIds ?? [];
        var cidadeIds = comando.CidadeIds ?? [];

        await repositorioPerfil.AtualizarPerfilAsync(usuario, categoriaIds, cidadeIds);

        // Recarrega com navegação para montar o DTO
        var perfilAtualizado = await repositorioPerfil.ObterPorSlugAsync(usuario.Slug!)
            ?? throw new ExcecaoNaoEncontrado("Perfil não encontrado após atualização");

        var dtoAtualizado = MapearParaDto(perfilAtualizado);

        // Invalida/atualiza o cache do perfil público para refletir a mudança na hora
        // (antes, a alteração só aparecia após expirar o TTL de 5 min).
        cache.Set($"perfil:{usuario.Slug}", dtoAtualizado, TtlPerfilPrestador);

        return dtoAtualizado;
    }

    public async Task<DtoPerfilPublico> ObterPerfilPublicoAsync(string slug)
    {
        // Cache por slug — TTL 5 minutos (RN-07)
        var chaveCache = $"perfil:{slug}";

        if (!cache.TryGetValue(chaveCache, out DtoPerfilPublico? dto) || dto is null)
        {
            var usuario = await repositorioPerfil.ObterPorSlugAsync(slug)
                ?? throw new ExcecaoNaoEncontrado("Prestador não encontrado");

            dto = MapearParaDto(usuario);
            cache.Set(chaveCache, dto, TtlPerfilPrestador);
        }

        return dto;
    }

    public async Task<List<DtoCategoriaPublica>> ListarCategoriasAsync()
    {
        // Cache de categorias — TTL 1 hora (RN-06)
        if (!cache.TryGetValue(ChaveCacheCategorias, out List<DtoCategoriaPublica>? categorias) || categorias is null)
        {
            var entidades = await repositorioCategorias.ListarAtivasAsync();
            categorias = entidades.Select(c => new DtoCategoriaPublica(c.Id, c.Nome, c.Slug)).ToList();
            cache.Set(ChaveCacheCategorias, categorias, TtlCategorias);
        }

        return categorias;
    }

    public async Task<List<DtoCidadePublica>> ListarCidadesAsync()
    {
        var cidades = await repositorioCidades.ListarAtivasAsync();
        return cidades.Select(c => new DtoCidadePublica(c.Id, c.Nome, c.Estado, c.Slug)).ToList();
    }

    public async Task<ResultadoPaginado<DtoPrestadorBusca>> BuscarPrestadoresAsync(
        string categoriaSlug,
        string? cidadeSlug,
        int page,
        int pageSize)
    {
        // RN-08: pageSize máximo 50
        pageSize = Math.Min(pageSize, 50);

        // Resolve categoriaId pelo slug — valida que existe e está ativa
        var categoria = await repositorioCategorias.ObterPorSlugAsync(categoriaSlug);
        if (categoria is null || !categoria.Ativa)
            throw new ExcecaoNaoEncontrado($"Categoria '{categoriaSlug}' não encontrada");

        // Resolve cidadeId pelo slug (opcional)
        Guid? cidadeId = null;
        if (!string.IsNullOrWhiteSpace(cidadeSlug))
        {
            var cidade = await repositorioCidades.ObterPorSlugAsync(cidadeSlug);
            if (cidade is null || !cidade.Ativa)
                throw new ExcecaoNaoEncontrado($"Cidade '{cidadeSlug}' não encontrada");

            cidadeId = cidade.Id;
        }

        var skip = (page - 1) * pageSize;
        var (items, total) = await repositorioPerfil.BuscarAsync(categoria.Id, cidadeId, skip, pageSize);

        var dtos = items.Select(u => new DtoPrestadorBusca(
            Id: u.Id,
            Nome: u.Nome,
            FotoPerfilUrl: u.FotoPerfilUrl,
            Slug: u.Slug!,
            MediaAvaliacoes: u.MediaAvaliacoes,
            TotalAvaliacoes: u.TotalAvaliacoes,
            Categorias: u.Categorias
                .Select(cu => new DtoCategoriaPublica(cu.Categoria.Id, cu.Categoria.Nome, cu.Categoria.Slug))
                .ToList(),
            Cidades: u.Cidades
                .Select(cu => new DtoCidadePublica(cu.Cidade.Id, cu.Cidade.Nome, cu.Cidade.Estado, cu.Cidade.Slug))
                .ToList()
        )).ToList();

        return new ResultadoPaginado<DtoPrestadorBusca>(dtos, total, page, pageSize);
    }

    public async Task<DtoDadosHome> ObterDadosHomeAsync()
    {
        var categorias = await ListarCategoriasAsync();

        var prestadores = await repositorioPerfil.ListarDestaqueAsync(6);
        var prestadoresDto = prestadores.Select(u => new DtoPrestadorBusca(
            Id: u.Id,
            Nome: u.Nome,
            FotoPerfilUrl: u.FotoPerfilUrl,
            Slug: u.Slug!,
            MediaAvaliacoes: u.MediaAvaliacoes,
            TotalAvaliacoes: u.TotalAvaliacoes,
            Categorias: u.Categorias
                .Select(cu => new DtoCategoriaPublica(cu.Categoria.Id, cu.Categoria.Nome, cu.Categoria.Slug))
                .ToList(),
            Cidades: u.Cidades
                .Select(cu => new DtoCidadePublica(cu.Cidade.Id, cu.Cidade.Nome, cu.Cidade.Estado, cu.Cidade.Slug))
                .ToList()
        )).ToList();

        var avaliacoes = await repositorioAvaliacoes.ListarRecentesGlobaisAsync(6);
        var avaliacoesDto = avaliacoes.Select(a => new DtoAvaliacaoHome(
            NomeAvaliador: a.Avaliador.Nome,
            Nota: a.Nota,
            Comentario: a.Comentario!,
            ServicoTitulo: a.Servico.Titulo,
            Cidade: a.Avaliador.Especialidade ?? "Brasil"
        )).ToList();

        return new DtoDadosHome(categorias, prestadoresDto, avaliacoesDto);
    }

    // ── Slug generation (ADR-08) ───────────────────────────────────────────────

    private async Task<string> GerarSlugUnicoAsync(string nome)
    {
        var base64Slug = NormalizarSlug(nome);

        for (var tentativa = 0; tentativa < MaxTentativasSlug; tentativa++)
        {
            // Sufixo de 4 chars hex aleatório (ex: "a8f3")
            var sufixo = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(2))
                .ToLowerInvariant();

            var candidato = $"{base64Slug}-{sufixo}";

            // Verifica unicidade — ignora soft-deleted (filtro global ativo)
            var existente = await repositorioUsuarios.ObterPorSlugAsync(candidato);
            if (existente is null)
                return candidato;
        }

        throw new InvalidOperationException("Não foi possível gerar um slug único após múltiplas tentativas");
    }

    /// <summary>
    /// Normaliza o nome para formato slug: [a-z0-9\-], sem acentos, separado por hífen.
    /// Exemplo: "João da Silva" → "joao-da-silva"
    /// </summary>
    private static string NormalizarSlug(string nome)
    {
        // Remove diacríticos (acentos)
        var normalizada = nome.Normalize(NormalizationForm.FormD);
        var semAcentos = new StringBuilder();

        foreach (var c in normalizada)
        {
            var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
                semAcentos.Append(c);
        }

        var slug = semAcentos.ToString().ToLowerInvariant();

        // Substitui qualquer char que não seja letra/dígito por hífen
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");

        // Remove hífens no início e no fim
        slug = slug.Trim('-');

        // Limita a 40 caracteres para manter URLs razoáveis
        if (slug.Length > 40)
            slug = slug[..40].TrimEnd('-');

        return string.IsNullOrEmpty(slug) ? "prestador" : slug;
    }

    // ── Mapeamento ──────────────────────────────────────────────────────────────

    private static DtoPerfilPublico MapearParaDto(Usuario usuario)
        => new(
            Id: usuario.Id,
            Nome: usuario.Nome,
            FotoPerfilUrl: usuario.FotoPerfilUrl,
            Slug: usuario.Slug,
            Descricao: usuario.Descricao,
            Especialidade: usuario.Especialidade,
            MediaAvaliacoes: usuario.MediaAvaliacoes,
            TotalAvaliacoes: usuario.TotalAvaliacoes,
            Categorias: usuario.Categorias
                .Select(cu => new DtoCategoriaPublica(cu.Categoria.Id, cu.Categoria.Nome, cu.Categoria.Slug))
                .ToList(),
            Cidades: usuario.Cidades
                .Select(cu => new DtoCidadePublica(cu.Cidade.Id, cu.Cidade.Nome, cu.Cidade.Estado, cu.Cidade.Slug))
                .ToList(),
            ImagensPortfolio: usuario.ImagensPortfolio
                .Where(i => i.Aprovada == true)
                .OrderBy(i => i.Ordem)
                .Select(i => new DtoImagemPortfolio(i.Id, i.Url, i.Ordem))
                .ToList()
        );
}
