using FluentAssertions;
using Moq;
using Prontto.Application.Avaliacoes;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Avaliacoes;

public class TestesServicoAvaliacao
{
    private readonly Mock<IRepositorioAvaliacao> _repositorioAvaliacoes = new();
    private readonly Mock<IRepositorioServico> _repositorioServicos = new();
    private readonly Mock<IRepositorioUsuario> _repositorioUsuarios = new();
    private readonly ServicoAvaliacao _sut;

    private readonly Guid _clienteId = Guid.NewGuid();
    private readonly Guid _prestadorId = Guid.NewGuid();
    private readonly Guid _servicoId = Guid.NewGuid();

    public TestesServicoAvaliacao()
    {
        _sut = new ServicoAvaliacao(
            _repositorioAvaliacoes.Object,
            _repositorioServicos.Object,
            _repositorioUsuarios.Object);
    }

    // ── RegistrarAsync — Sucesso ───────────────────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_DadosValidos_RetornaDtoAvaliacao()
    {
        // Arrange
        var servico = CriarServicoConcluido();
        var avaliador = new Usuario { Id = _clienteId, Nome = "João Silva" };
        var avaliado = new Usuario { Id = _prestadorId, Nome = "Maria Santos" };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync(servico);
        _repositorioAvaliacoes.Setup(r => r.ExisteAvaliacaoAsync(_servicoId, _clienteId)).ReturnsAsync(false);
        _repositorioAvaliacoes.Setup(r => r.AdicionarAsync(It.IsAny<Avaliacao>())).Returns(Task.CompletedTask);
        _repositorioAvaliacoes.Setup(r => r.CalcularMediaAsync(_prestadorId)).ReturnsAsync((4.50m, 1));
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(_prestadorId)).ReturnsAsync(avaliado);
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(_clienteId)).ReturnsAsync(avaliador);
        _repositorioUsuarios.Setup(r => r.AtualizarAsync(avaliado)).ReturnsAsync(avaliado);

        var comando = new ComandoRegistrarAvaliacao(5, "Excelente serviço!");

        // Act
        var resultado = await _sut.RegistrarAsync(_servicoId, _clienteId, comando);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Nota.Should().Be(5);
        resultado.Comentario.Should().Be("Excelente serviço!");
        resultado.NomeAvaliador.Should().Be("João"); // apenas o primeiro nome (LGPD)
        resultado.ServicoId.Should().Be(_servicoId);
    }

    // ── RegistrarAsync — Serviço não encontrado ────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_ServicoInexistente_LancaExcecaoNaoEncontrado()
    {
        // Arrange
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync((Servico?)null);

        var comando = new ComandoRegistrarAvaliacao(5, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, _clienteId, comando))
            .Should().ThrowAsync<ExcecaoNaoEncontrado>()
            .WithMessage("Serviço não encontrado");
    }

    // ── RegistrarAsync — Serviço não concluído ────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_ServicoNaoConcluido_LancaExcecaoTransicaoInvalida()
    {
        // Arrange
        var servico = new Servico
        {
            Id = _servicoId,
            ClienteId = _clienteId,
            PrestadorId = _prestadorId,
            Status = StatusServico.EmAndamento,
            ConcluidoEm = null
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync(servico);

        var comando = new ComandoRegistrarAvaliacao(4, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, _clienteId, comando))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>()
            .WithMessage("Serviço não concluído");
    }

    // ── RegistrarAsync — Prazo expirado ───────────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_PrazoExpirado_LancaExcecaoTransicaoInvalida()
    {
        // Arrange
        var servico = new Servico
        {
            Id = _servicoId,
            ClienteId = _clienteId,
            PrestadorId = _prestadorId,
            Status = StatusServico.Concluido,
            ConcluidoEm = DateTime.UtcNow.AddDays(-31) // expirado há 1 dia
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync(servico);

        var comando = new ComandoRegistrarAvaliacao(3, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, _clienteId, comando))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>()
            .WithMessage("Prazo para avaliação expirado");
    }

    // ── RegistrarAsync — Não participante ─────────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_UsuarioNaoParticipante_LancaExcecaoProibido()
    {
        // Arrange
        var terceiro = Guid.NewGuid();
        var servico = CriarServicoConcluido();
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync(servico);

        var comando = new ComandoRegistrarAvaliacao(5, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, terceiro, comando))
            .Should().ThrowAsync<ExcecaoProibido>()
            .WithMessage("Apenas participantes do serviço podem avaliar");
    }

    // ── RegistrarAsync — Avaliação duplicada ──────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_AvaliacaoDuplicada_LancaExcecaoConflito()
    {
        // Arrange
        var servico = CriarServicoConcluido();
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync(servico);
        _repositorioAvaliacoes.Setup(r => r.ExisteAvaliacaoAsync(_servicoId, _clienteId)).ReturnsAsync(true);

        var comando = new ComandoRegistrarAvaliacao(5, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, _clienteId, comando))
            .Should().ThrowAsync<ExcecaoConflito>()
            .WithMessage("Você já avaliou este serviço");
    }

    // ── RegistrarAsync — Nota inválida: 0 ────────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_NotaZero_LancaExcecaoValidacao()
    {
        // Arrange
        var comando = new ComandoRegistrarAvaliacao(0, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, _clienteId, comando))
            .Should().ThrowAsync<ExcecaoValidacao>()
            .WithMessage("A nota deve ser entre 1 e 5");
    }

    // ── RegistrarAsync — Nota inválida: 6 ────────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_NotaSeis_LancaExcecaoValidacao()
    {
        // Arrange
        var comando = new ComandoRegistrarAvaliacao(6, null);

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarAsync(_servicoId, _clienteId, comando))
            .Should().ThrowAsync<ExcecaoValidacao>()
            .WithMessage("A nota deve ser entre 1 e 5");
    }

    // ── RegistrarAsync — Avaliado correto (inversão de papel) ─────────────────

    [Fact]
    public async Task RegistrarAsync_PrestadorAvalia_AvaliadoEhCliente()
    {
        // Arrange
        var servico = CriarServicoConcluido();
        var avaliador = new Usuario { Id = _prestadorId, Nome = "Maria Santos" };
        var avaliado = new Usuario { Id = _clienteId, Nome = "João Silva" };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(_servicoId)).ReturnsAsync(servico);
        _repositorioAvaliacoes.Setup(r => r.ExisteAvaliacaoAsync(_servicoId, _prestadorId)).ReturnsAsync(false);

        Avaliacao? avaliacaoCriada = null;
        _repositorioAvaliacoes
            .Setup(r => r.AdicionarAsync(It.IsAny<Avaliacao>()))
            .Callback<Avaliacao>(a => avaliacaoCriada = a)
            .Returns(Task.CompletedTask);

        _repositorioAvaliacoes.Setup(r => r.CalcularMediaAsync(_clienteId)).ReturnsAsync((3.00m, 1));
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(_clienteId)).ReturnsAsync(avaliado);
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(_prestadorId)).ReturnsAsync(avaliador);
        _repositorioUsuarios.Setup(r => r.AtualizarAsync(avaliado)).ReturnsAsync(avaliado);

        var comando = new ComandoRegistrarAvaliacao(3, "Cliente pontual");

        // Act
        await _sut.RegistrarAsync(_servicoId, _prestadorId, comando);

        // Assert: o avaliado deve ser o cliente, não o prestador
        avaliacaoCriada.Should().NotBeNull();
        avaliacaoCriada!.AvaliadoId.Should().Be(_clienteId);
        avaliacaoCriada.AvaliadorId.Should().Be(_prestadorId);
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private Servico CriarServicoConcluido() => new()
    {
        Id = _servicoId,
        ClienteId = _clienteId,
        PrestadorId = _prestadorId,
        Status = StatusServico.Concluido,
        ConcluidoEm = DateTime.UtcNow.AddDays(-1) // concluído ontem, dentro da janela de 30 dias
    };
}
