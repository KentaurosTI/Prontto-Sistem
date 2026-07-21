using FluentAssertions;
using Moq;
using Prontto.Application.Auth;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Auth;

public class TestesServicoAutenticacao
{
    private readonly Mock<IRepositorioUsuario> _repositorioUsuarios = new();
    private readonly Mock<IRepositorioRefreshToken> _repositorioRefreshTokens = new();
    private readonly Mock<IRepositorioAuditLog> _repositorioAuditLog = new();
    private readonly Mock<IRepositorioCidade> _repositorioCidades = new();
    private readonly Mock<IServicoJwt> _jwt = new();
    private readonly Mock<IHashSenha> _hashSenha = new();
    private readonly ServicoAutenticacao _sut;

    public TestesServicoAutenticacao()
    {
        _jwt.Setup(j => j.GerarRefreshToken()).Returns("refresh-bruto");
        _jwt.Setup(j => j.ComputarHashRefreshToken(It.IsAny<string>())).Returns("hash-refresh");
        _repositorioRefreshTokens
            .Setup(r => r.AdicionarAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);

        _sut = new ServicoAutenticacao(
            _repositorioUsuarios.Object,
            _repositorioRefreshTokens.Object,
            _repositorioAuditLog.Object,
            _repositorioCidades.Object,
            _jwt.Object,
            _hashSenha.Object);
    }

    // ── CadastrarAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CadastrarAsync_EmailNovo_RetornaAccessTokenERefreshTokenEUsuario()
    {
        var comando = new ComandoCadastro("Ana", "ana@test.com", "senha123", TipoConta.Cliente);
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync("ana@test.com")).ReturnsAsync((Usuario?)null);
        _hashSenha.Setup(h => h.Hashear("senha123")).Returns("hash");
        _repositorioUsuarios.Setup(r => r.AdicionarAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);
        _jwt.Setup(j => j.GerarToken(It.IsAny<Usuario>())).Returns("jwt-token");

        var resultado = await _sut.CadastrarAsync(comando);

        resultado.Token.Should().Be("jwt-token");
        resultado.RefreshToken.Should().Be("refresh-bruto");
        resultado.Usuario.Email.Should().Be("ana@test.com");
        resultado.Usuario.HashSenha.Should().Be("hash");
    }

    [Fact]
    public async Task CadastrarAsync_EmailDuplicado_LancaExcecaoConflito()
    {
        var comando = new ComandoCadastro("Ana", "ana@test.com", "senha123", TipoConta.Cliente);
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync("ana@test.com"))
              .ReturnsAsync(new Usuario { Email = "ana@test.com" });

        await _sut.Invoking(s => s.CadastrarAsync(comando))
            .Should().ThrowAsync<ExcecaoConflito>()
            .WithMessage("E-mail já cadastrado");
    }

    [Fact]
    public async Task CadastrarAsync_EmailNovo_PersistirRefreshToken()
    {
        var comando = new ComandoCadastro("Ana", "ana@test.com", "senha123", TipoConta.Prestador);
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);
        _hashSenha.Setup(h => h.Hashear(It.IsAny<string>())).Returns("hash");
        _repositorioUsuarios.Setup(r => r.AdicionarAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);
        _jwt.Setup(j => j.GerarToken(It.IsAny<Usuario>())).Returns("jwt-token");

        await _sut.CadastrarAsync(comando);

        _repositorioRefreshTokens.Verify(r => r.AdicionarAsync(It.Is<RefreshToken>(t =>
            t.Token == "hash-refresh" &&
            t.ExpiracaoEm > DateTime.UtcNow
        )), Times.Once);
    }

    // ── EntrarAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EntrarAsync_CredenciaisValidas_RetornaTokens()
    {
        var usuario = new Usuario { Id = Guid.NewGuid(), Email = "ana@test.com", HashSenha = "hash" };
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync("ana@test.com")).ReturnsAsync(usuario);
        _hashSenha.Setup(h => h.Verificar("senha123", "hash")).Returns(true);
        _jwt.Setup(j => j.GerarToken(usuario)).Returns("jwt-token");

        var resultado = await _sut.EntrarAsync(new ComandoLogin("ana@test.com", "senha123"));

        resultado.Token.Should().Be("jwt-token");
        resultado.RefreshToken.Should().Be("refresh-bruto");
    }

    [Fact]
    public async Task EntrarAsync_CredenciaisValidas_PersistirRefreshToken()
    {
        var usuario = new Usuario { Id = Guid.NewGuid(), Email = "ana@test.com", HashSenha = "hash" };
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync("ana@test.com")).ReturnsAsync(usuario);
        _hashSenha.Setup(h => h.Verificar(It.IsAny<string>(), "hash")).Returns(true);
        _jwt.Setup(j => j.GerarToken(It.IsAny<Usuario>())).Returns("jwt-token");

        await _sut.EntrarAsync(new ComandoLogin("ana@test.com", "senha123", "127.0.0.1", "TestAgent/1.0"));

        _repositorioRefreshTokens.Verify(r => r.AdicionarAsync(It.Is<RefreshToken>(t =>
            t.Token == "hash-refresh" &&
            t.UsuarioId == usuario.Id &&
            t.Ip == "127.0.0.1" &&
            t.UserAgent == "TestAgent/1.0"
        )), Times.Once);
    }

    [Fact]
    public async Task EntrarAsync_SenhaErrada_LancaExcecaoNaoAutorizado()
    {
        var usuario = new Usuario { Email = "ana@test.com", HashSenha = "hash" };
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync("ana@test.com")).ReturnsAsync(usuario);
        _hashSenha.Setup(h => h.Verificar("errada", "hash")).Returns(false);

        await _sut.Invoking(s => s.EntrarAsync(new ComandoLogin("ana@test.com", "errada")))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>();
    }

    [Fact]
    public async Task EntrarAsync_EmailDesconhecido_LancaExcecaoNaoAutorizado()
    {
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

        await _sut.Invoking(s => s.EntrarAsync(new ComandoLogin("x@x.com", "q")))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>();
    }

    [Fact]
    public async Task EntrarAsync_UsuarioSoftDeleted_LancaExcecaoNaoAutorizado()
    {
        var usuario = new Usuario
        {
            Email = "deletado@test.com",
            HashSenha = "hash",
            DeletadoEm = DateTime.UtcNow.AddDays(-1)
        };
        _repositorioUsuarios.Setup(r => r.ObterPorEmailAsync("deletado@test.com")).ReturnsAsync(usuario);
        _hashSenha.Setup(h => h.Verificar(It.IsAny<string>(), "hash")).Returns(true);

        await _sut.Invoking(s => s.EntrarAsync(new ComandoLogin("deletado@test.com", "senha")))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>();
    }

    // ── RenovarSessaoAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RenovarSessaoAsync_TokenValido_RetornaNovoParDeTokens()
    {
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario { Id = usuarioId, Email = "ana@test.com" };
        var tokenAtual = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Token = "hash-refresh",
            ExpiracaoEm = DateTime.UtcNow.AddDays(29),
        };

        _jwt.Setup(j => j.ComputarHashRefreshToken("refresh-bruto")).Returns("hash-refresh");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync("hash-refresh")).ReturnsAsync(tokenAtual);
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(usuarioId)).ReturnsAsync(usuario);
        _jwt.Setup(j => j.GerarToken(usuario)).Returns("novo-jwt");
        _jwt.Setup(j => j.GerarRefreshToken()).Returns("novo-refresh-bruto");
        _jwt.Setup(j => j.ComputarHashRefreshToken("novo-refresh-bruto")).Returns("novo-hash-refresh");
        _repositorioRefreshTokens
            .Setup(r => r.AtualizarAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        var resultado = await _sut.RenovarSessaoAsync("refresh-bruto", null, null);

        resultado.Token.Should().Be("novo-jwt");
        resultado.RefreshToken.Should().Be("novo-refresh-bruto");
        tokenAtual.RevogadoEm.Should().NotBeNull();
        tokenAtual.SubstituidoPor.Should().Be("novo-hash-refresh");
    }

    [Fact]
    public async Task RenovarSessaoAsync_TokenRevogado_LancaExcecaoNaoAutorizado()
    {
        var tokenRevogado = new RefreshToken
        {
            Token = "hash-refresh",
            ExpiracaoEm = DateTime.UtcNow.AddDays(29),
            RevogadoEm = DateTime.UtcNow.AddHours(-1),
        };

        _jwt.Setup(j => j.ComputarHashRefreshToken("refresh-bruto")).Returns("hash-refresh");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync("hash-refresh")).ReturnsAsync(tokenRevogado);

        await _sut.Invoking(s => s.RenovarSessaoAsync("refresh-bruto", null, null))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>()
            .WithMessage("Refresh token revogado");
    }

    [Fact]
    public async Task RenovarSessaoAsync_TokenExpirado_LancaExcecaoNaoAutorizado()
    {
        var tokenExpirado = new RefreshToken
        {
            Token = "hash-refresh",
            ExpiracaoEm = DateTime.UtcNow.AddDays(-1), // já expirou
        };

        _jwt.Setup(j => j.ComputarHashRefreshToken("refresh-bruto")).Returns("hash-refresh");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync("hash-refresh")).ReturnsAsync(tokenExpirado);

        await _sut.Invoking(s => s.RenovarSessaoAsync("refresh-bruto", null, null))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>()
            .WithMessage("Refresh token expirado");
    }

    [Fact]
    public async Task RenovarSessaoAsync_TokenNaoEncontrado_LancaExcecaoNaoAutorizado()
    {
        _jwt.Setup(j => j.ComputarHashRefreshToken(It.IsAny<string>())).Returns("hash-qualquer");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        await _sut.Invoking(s => s.RenovarSessaoAsync("token-invalido", null, null))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>();
    }

    // ── LogoutAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_TokenValido_RevogaToken()
    {
        var token = new RefreshToken
        {
            Token = "hash-refresh",
            ExpiracaoEm = DateTime.UtcNow.AddDays(29),
        };

        _jwt.Setup(j => j.ComputarHashRefreshToken("refresh-bruto")).Returns("hash-refresh");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync("hash-refresh")).ReturnsAsync(token);
        _repositorioRefreshTokens
            .Setup(r => r.AtualizarAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        await _sut.LogoutAsync("refresh-bruto");

        token.RevogadoEm.Should().NotBeNull();
        _repositorioRefreshTokens.Verify(r => r.AtualizarAsync(token), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_TokenJaRevogado_NaoLancaExcecao()
    {
        var tokenRevogado = new RefreshToken
        {
            Token = "hash-refresh",
            ExpiracaoEm = DateTime.UtcNow.AddDays(29),
            RevogadoEm = DateTime.UtcNow.AddHours(-1),
        };

        _jwt.Setup(j => j.ComputarHashRefreshToken("refresh-bruto")).Returns("hash-refresh");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync("hash-refresh")).ReturnsAsync(tokenRevogado);

        // Deve ser idempotente — não lança exceção, não chama AtualizarAsync
        await _sut.Invoking(s => s.LogoutAsync("refresh-bruto"))
            .Should().NotThrowAsync();

        _repositorioRefreshTokens.Verify(r => r.AtualizarAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_TokenNaoEncontrado_NaoLancaExcecao()
    {
        _jwt.Setup(j => j.ComputarHashRefreshToken(It.IsAny<string>())).Returns("hash-qualquer");
        _repositorioRefreshTokens.Setup(r => r.ObterPorHashAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        await _sut.Invoking(s => s.LogoutAsync("token-invalido"))
            .Should().NotThrowAsync();
    }
}
