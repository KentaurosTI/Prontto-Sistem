using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prontto.Application.Common;
using Prontto.Infrastructure.Services;

namespace Prontto.UnitTests.Infrastructure;

public class TestesArmazenamentoArquivoLocal : IDisposable
{
    private readonly string _pastaTemp;
    private readonly Mock<IHostEnvironment> _env;
    private readonly ArmazenamentoArquivoLocal _sut;

    public TestesArmazenamentoArquivoLocal()
    {
        _pastaTemp = Path.Combine(Path.GetTempPath(), "prontto_testes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pastaTemp);

        _env = new Mock<IHostEnvironment>();
        _env.Setup(e => e.ContentRootPath).Returns(_pastaTemp);

        _sut = new ArmazenamentoArquivoLocal(_env.Object, NullLogger<ArmazenamentoArquivoLocal>.Instance);
    }

    // ── SalvarAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SalvarAsync_ExtensaoInvalida_LancaExcecaoValidacao()
    {
        var conteudo = new MemoryStream(new byte[] { 0x01, 0x02 });

        var acao = () => _sut.SalvarAsync(conteudo, "arquivo.exe", "application/octet-stream");

        await acao.Should().ThrowAsync<ExcecaoValidacao>()
            .WithMessage("*Tipo de arquivo não permitido*");
    }

    [Theory]
    [InlineData("foto.jpg", "image/jpeg")]
    [InlineData("foto.jpeg", "image/jpeg")]
    [InlineData("foto.png", "image/png")]
    [InlineData("foto.webp", "image/webp")]
    [InlineData("FOTO.JPG", "image/jpeg")]  // Extensão em maiúsculas — deve ser aceita
    public async Task SalvarAsync_ExtensaoPermitida_SalvaArquivoERetornaUrlRelativa(
        string nomeOriginal, string contentType)
    {
        var conteudoBytes = new byte[] { 0xFF, 0xD8, 0xFF }; // Magic bytes JPEG simulado
        var conteudo = new MemoryStream(conteudoBytes);

        var url = await _sut.SalvarAsync(conteudo, nomeOriginal, contentType);

        url.Should().StartWith("/uploads/");
        url.Should().MatchRegex(@"^/uploads/\d{4}/\d{2}/[a-f0-9]{32}\.[a-z]+$");
    }

    [Fact]
    public async Task SalvarAsync_DoisUploads_GeramNomesUnicos()
    {
        var conteudo1 = new MemoryStream(new byte[] { 0x01 });
        var conteudo2 = new MemoryStream(new byte[] { 0x02 });

        var url1 = await _sut.SalvarAsync(conteudo1, "foto.jpg", "image/jpeg");
        var url2 = await _sut.SalvarAsync(conteudo2, "foto.jpg", "image/jpeg");

        url1.Should().NotBe(url2, "cada upload deve gerar um nome único via Guid");
    }

    [Fact]
    public async Task SalvarAsync_ArquivoValido_PersisteFisicoNaDisk()
    {
        var conteudoBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // Magic bytes PNG
        var conteudo = new MemoryStream(conteudoBytes);

        var url = await _sut.SalvarAsync(conteudo, "imagem.png", "image/png");

        // Converter URL relativa em caminho físico para verificar
        var caminhoRelativo = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var caminhoFisico = Path.Combine(_pastaTemp, caminhoRelativo);

        File.Exists(caminhoFisico).Should().BeTrue("o arquivo deve existir fisicamente após o upload");
        (await File.ReadAllBytesAsync(caminhoFisico)).Should().BeEquivalentTo(conteudoBytes);
    }

    // ── RemoverAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoverAsync_ArquivoNaoExistente_NaoLancaExcecao()
    {
        var acao = () => _sut.RemoverAsync("/uploads/2026/06/arquivo_inexistente.jpg");

        await acao.Should().NotThrowAsync("remoção de arquivo ausente deve ser silenciosa");
    }

    [Fact]
    public async Task RemoverAsync_UrlVazia_NaoLancaExcecao()
    {
        var acao = () => _sut.RemoverAsync(string.Empty);

        await acao.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoverAsync_ArquivoExistente_RemoveFisicoDoDisck()
    {
        // Arrange: salvar um arquivo primeiro
        var conteudo = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var url = await _sut.SalvarAsync(conteudo, "foto.jpg", "image/jpeg");

        var caminhoRelativo = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var caminhoFisico = Path.Combine(_pastaTemp, caminhoRelativo);
        File.Exists(caminhoFisico).Should().BeTrue("pré-condição: arquivo deve existir antes da remoção");

        // Act
        await _sut.RemoverAsync(url);

        // Assert
        File.Exists(caminhoFisico).Should().BeFalse("arquivo deve ser removido fisicamente");
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Directory.Exists(_pastaTemp))
            Directory.Delete(_pastaTemp, recursive: true);
    }
}
