using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Prontto.Application.Admin;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Admin;

public class TestesServicoAdminModeracao
{
    private readonly Mock<IRepositorioUsuario> _repositorioUsuarios = new();
    private readonly Mock<IRepositorioServico> _repositorioServicos = new();
    private readonly Mock<IRepositorioCobranca> _repositorioCobrancas = new();
    private readonly Mock<IRepositorioMensagem> _repositorioMensagens = new();
    private readonly Mock<IRepositorioAuditLog> _repositorioAuditLog = new();
    private readonly Mock<IRepositorioImagemPortfolio> _repositorioImagens = new();
    private readonly Mock<IRepositorioNotificacao> _repositorioNotificacoes = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ServicoAdmin _sut;

    public TestesServicoAdminModeracao()
    {
        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _repositorioNotificacoes.Setup(r => r.AdicionarAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

        _sut = new ServicoAdmin(
            _repositorioUsuarios.Object,
            _repositorioServicos.Object,
            _repositorioCobrancas.Object,
            _repositorioMensagens.Object,
            _repositorioAuditLog.Object,
            _repositorioImagens.Object,
            _repositorioNotificacoes.Object,
            _cache);
    }

    [Fact]
    public async Task ListarImagensPendentesAsync_RetornaApenasNaoModeradas()
    {
        var prestadorId = Guid.NewGuid();
        var imagens = new List<ImagemPortfolio>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Url = "https://res.cloudinary.com/img1.jpg",
                UsuarioId = prestadorId,
                Aprovada = null,
                CriadoEm = DateTime.UtcNow,
                Usuario = new Usuario { Id = prestadorId, Nome = "João Silva" }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Url = "https://res.cloudinary.com/img2.jpg",
                UsuarioId = prestadorId,
                Aprovada = null,
                CriadoEm = DateTime.UtcNow.AddMinutes(-5),
                Usuario = new Usuario { Id = prestadorId, Nome = "João Silva" }
            }
        };

        _repositorioImagens
            .Setup(r => r.ListarPendentesModeracaoAsync())
            .ReturnsAsync(imagens);

        var resultado = await _sut.ListarImagensPendentesAsync();

        resultado.Should().HaveCount(2);
        resultado.Should().AllSatisfy(dto =>
        {
            dto.PrestadorId.Should().Be(prestadorId);
            dto.NomePrestador.Should().Be("João Silva");
        });
    }

    [Fact]
    public async Task ListarImagensPendentesAsync_SemImagens_RetornaListaVazia()
    {
        _repositorioImagens
            .Setup(r => r.ListarPendentesModeracaoAsync())
            .ReturnsAsync(new List<ImagemPortfolio>());

        var resultado = await _sut.ListarImagensPendentesAsync();

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ModerarImagemAsync_Aprovada_SetaAprovadaTrueENotificaPrestador()
    {
        var imagemId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var imagem = new ImagemPortfolio
        {
            Id = imagemId,
            UsuarioId = prestadorId,
            Aprovada = null,
            Moderada = false,
            Usuario = new Usuario { Id = prestadorId, Nome = "Maria" }
        };

        _repositorioImagens.Setup(r => r.ObterPorIdAsync(imagemId)).ReturnsAsync(imagem);
        _repositorioImagens.Setup(r => r.AtualizarAsync(It.IsAny<ImagemPortfolio>())).Returns(Task.CompletedTask);

        await _sut.ModerarImagemAsync(imagemId, aprovada: true, adminId);

        imagem.Aprovada.Should().BeTrue();
        imagem.Moderada.Should().BeTrue();

        _repositorioImagens.Verify(r => r.AtualizarAsync(It.Is<ImagemPortfolio>(
            i => i.Id == imagemId && i.Aprovada == true && i.Moderada == true
        )), Times.Once);

        _repositorioNotificacoes.Verify(r => r.AdicionarAsync(It.Is<Notificacao>(
            n => n.UsuarioId == prestadorId &&
                 n.Titulo == "Imagem aprovada" &&
                 n.ReferenciaId == imagemId.ToString()
        )), Times.Once);

        _repositorioAuditLog.Verify(r => r.RegistrarAsync(It.Is<AuditLog>(
            log => log.Acao == "admin.imagem.aprovada" &&
                   log.EntidadeId == imagemId.ToString() &&
                   log.UsuarioId == adminId
        )), Times.Once);
    }

    [Fact]
    public async Task ModerarImagemAsync_Rejeitada_SetaAprovadaFalseENotificaPrestador()
    {
        var imagemId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var imagem = new ImagemPortfolio
        {
            Id = imagemId,
            UsuarioId = prestadorId,
            Aprovada = null,
            Moderada = false,
            Usuario = new Usuario { Id = prestadorId, Nome = "Carlos" }
        };

        _repositorioImagens.Setup(r => r.ObterPorIdAsync(imagemId)).ReturnsAsync(imagem);
        _repositorioImagens.Setup(r => r.AtualizarAsync(It.IsAny<ImagemPortfolio>())).Returns(Task.CompletedTask);

        await _sut.ModerarImagemAsync(imagemId, aprovada: false, adminId);

        imagem.Aprovada.Should().BeFalse();
        imagem.Moderada.Should().BeTrue();

        _repositorioNotificacoes.Verify(r => r.AdicionarAsync(It.Is<Notificacao>(
            n => n.UsuarioId == prestadorId &&
                 n.Titulo == "Imagem rejeitada" &&
                 n.ReferenciaId == imagemId.ToString()
        )), Times.Once);

        _repositorioAuditLog.Verify(r => r.RegistrarAsync(It.Is<AuditLog>(
            log => log.Acao == "admin.imagem.rejeitada" &&
                   log.EntidadeId == imagemId.ToString()
        )), Times.Once);
    }

    [Fact]
    public async Task ModerarImagemAsync_ImagemNaoEncontrada_LancaExcecaoNaoEncontrado()
    {
        _repositorioImagens
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ImagemPortfolio?)null);

        await _sut
            .Invoking(s => s.ModerarImagemAsync(Guid.NewGuid(), true, Guid.NewGuid()))
            .Should().ThrowAsync<ExcecaoNaoEncontrado>()
            .WithMessage("*Imagem*");
    }
}
