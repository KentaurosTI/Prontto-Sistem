using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Prontto.Application.Common;
using Prontto.Application.Perfil;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Perfil;

public class TestesServicoPerfilPrestador
{
    private readonly Mock<IRepositorioUsuario> _repositorioUsuarios = new();
    private readonly Mock<IRepositorioPerfilPrestador> _repositorioPerfil = new();
    private readonly Mock<IRepositorioCategoria> _repositorioCategorias = new();
    private readonly Mock<IRepositorioCidade> _repositorioCidades = new();
    private readonly Mock<IRepositorioAvaliacao> _repositorioAvaliacoes = new();
    private readonly IMemoryCache _cache;
    private readonly ServicoPerfilPrestador _sut;

    public TestesServicoPerfilPrestador()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());

        _sut = new ServicoPerfilPrestador(
            _repositorioUsuarios.Object,
            _repositorioPerfil.Object,
            _repositorioCategorias.Object,
            _repositorioCidades.Object,
            _repositorioAvaliacoes.Object,
            _cache);
    }

    // ── BuscarPrestadoresAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task BuscarPrestadoresAsync_CategoriaInvalida_LancaExcecaoNaoEncontrado()
    {
        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("invalida"))
            .ReturnsAsync((Categoria?)null);

        await _sut
            .Invoking(s => s.BuscarPrestadoresAsync("invalida", null, 1, 20))
            .Should()
            .ThrowAsync<ExcecaoNaoEncontrado>();
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_CategoriaInativa_LancaExcecaoNaoEncontrado()
    {
        var categoria = new Categoria { Id = Guid.NewGuid(), Nome = "Inativa", Slug = "inativa", Ativa = false };

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("inativa"))
            .ReturnsAsync(categoria);

        await _sut
            .Invoking(s => s.BuscarPrestadoresAsync("inativa", null, 1, 20))
            .Should()
            .ThrowAsync<ExcecaoNaoEncontrado>();
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_CidadeInvalida_LancaExcecaoNaoEncontrado()
    {
        var categoria = new Categoria { Id = Guid.NewGuid(), Nome = "Encanador", Slug = "encanador", Ativa = true };

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("encanador"))
            .ReturnsAsync(categoria);
        _repositorioCidades.Setup(r => r.ObterPorSlugAsync("inexistente"))
            .ReturnsAsync((Cidade?)null);

        await _sut
            .Invoking(s => s.BuscarPrestadoresAsync("encanador", "inexistente", 1, 20))
            .Should()
            .ThrowAsync<ExcecaoNaoEncontrado>();
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_CategoriaSemCidade_RetornaPrestadores()
    {
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nome = "Eletricista", Slug = "eletricista", Ativa = true };

        var prestador = CriarPrestadorValido(categoriaId, null);

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("eletricista"))
            .ReturnsAsync(categoria);
        _repositorioPerfil.Setup(r => r.BuscarAsync(categoriaId, null, 0, 20))
            .ReturnsAsync((new List<Usuario> { prestador }, 1));

        var resultado = await _sut.BuscarPrestadoresAsync("eletricista", null, 1, 20);

        resultado.TotalCount.Should().Be(1);
        resultado.Items.Should().HaveCount(1);
        resultado.Items[0].Nome.Should().Be("Carlos");
        resultado.Items[0].Slug.Should().Be("carlos-ab12");
        resultado.Page.Should().Be(1);
        resultado.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_ComCidade_FiltraCorretamente()
    {
        var categoriaId = Guid.NewGuid();
        var cidadeId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nome = "Pintor", Slug = "pintor", Ativa = true };
        var cidade = new Cidade { Id = cidadeId, Nome = "Itapevi", Estado = "SP", Slug = "itapevi", Ativa = true };

        var prestador = CriarPrestadorValido(categoriaId, cidadeId);

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("pintor"))
            .ReturnsAsync(categoria);
        _repositorioCidades.Setup(r => r.ObterPorSlugAsync("itapevi"))
            .ReturnsAsync(cidade);
        _repositorioPerfil.Setup(r => r.BuscarAsync(categoriaId, cidadeId, 0, 20))
            .ReturnsAsync((new List<Usuario> { prestador }, 1));

        var resultado = await _sut.BuscarPrestadoresAsync("pintor", "itapevi", 1, 20);

        resultado.TotalCount.Should().Be(1);
        resultado.Items[0].Cidades.Should().HaveCount(1);
        resultado.Items[0].Cidades[0].Slug.Should().Be("itapevi");
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_SemResultados_RetornaListaVazia()
    {
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nome = "Diarista", Slug = "diarista", Ativa = true };

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("diarista"))
            .ReturnsAsync(categoria);
        _repositorioPerfil.Setup(r => r.BuscarAsync(categoriaId, null, 0, 20))
            .ReturnsAsync((new List<Usuario>(), 0));

        var resultado = await _sut.BuscarPrestadoresAsync("diarista", null, 1, 20);

        resultado.TotalCount.Should().Be(0);
        resultado.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_PageSizeAcimaDoLimite_LimitaA50()
    {
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nome = "Pedreiro", Slug = "pedreiro", Ativa = true };

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("pedreiro"))
            .ReturnsAsync(categoria);
        _repositorioPerfil.Setup(r => r.BuscarAsync(categoriaId, null, 0, 50))
            .ReturnsAsync((new List<Usuario>(), 0));

        var resultado = await _sut.BuscarPrestadoresAsync("pedreiro", null, 1, 100);

        // Deve ter sido chamado com take=50, não com 100
        _repositorioPerfil.Verify(r => r.BuscarAsync(categoriaId, null, 0, 50), Times.Once);
        resultado.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task BuscarPrestadoresAsync_NuncaExpoeDadosSensiveis()
    {
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria { Id = categoriaId, Nome = "Encanador", Slug = "encanador", Ativa = true };

        var prestador = CriarPrestadorValido(categoriaId, null);
        prestador.Email = "email@privado.com";
        prestador.Cpf = "12345678901";

        _repositorioCategorias.Setup(r => r.ObterPorSlugAsync("encanador"))
            .ReturnsAsync(categoria);
        _repositorioPerfil.Setup(r => r.BuscarAsync(categoriaId, null, 0, 20))
            .ReturnsAsync((new List<Usuario> { prestador }, 1));

        var resultado = await _sut.BuscarPrestadoresAsync("encanador", null, 1, 20);

        // DtoPrestadorBusca não deve ter Email nem Cpf
        var dto = resultado.Items[0];
        var propriedades = dto.GetType().GetProperties().Select(p => p.Name);
        propriedades.Should().NotContain("Email");
        propriedades.Should().NotContain("Cpf");
        propriedades.Should().NotContain("HashSenha");
    }

    // ── ListarCategoriasAsync (cache) ──────────────────────────────────────────

    [Fact]
    public async Task ListarCategoriasAsync_SegundaChamada_UsaCache()
    {
        var categorias = new List<Categoria>
        {
            new() { Id = Guid.NewGuid(), Nome = "Encanador", Slug = "encanador", Ativa = true, Ordem = 1 },
        };

        _repositorioCategorias.Setup(r => r.ListarAtivasAsync())
            .ReturnsAsync(categorias);

        // Primeira chamada — deve bater no repositório
        await _sut.ListarCategoriasAsync();
        // Segunda chamada — deve usar o cache
        await _sut.ListarCategoriasAsync();

        _repositorioCategorias.Verify(r => r.ListarAtivasAsync(), Times.Once);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Usuario CriarPrestadorValido(Guid categoriaId, Guid? cidadeId)
    {
        var catId = Guid.NewGuid();
        var cidId = cidadeId ?? Guid.NewGuid();

        var categoria = new Categoria { Id = categoriaId, Nome = "Categoria", Slug = "categoria", Ativa = true };
        var cidade = new Cidade { Id = cidId, Nome = "Itapevi", Estado = "SP", Slug = "itapevi", Ativa = true };

        return new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Carlos",
            Email = "carlos@test.com",
            HashSenha = "hash",
            TipoConta = TipoConta.Prestador,
            Papel = Papel.Usuario,
            Slug = "carlos-ab12",
            MediaAvaliacoes = 4.5m,
            TotalAvaliacoes = 10,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
            Categorias = new List<CategoriaUsuario>
            {
                new() { UsuarioId = Guid.NewGuid(), CategoriaId = categoriaId, Categoria = categoria },
            },
            Cidades = new List<CidadeUsuario>
            {
                new() { UsuarioId = Guid.NewGuid(), CidadeId = cidId, Cidade = cidade },
            },
        };
    }
}
