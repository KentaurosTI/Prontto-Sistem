using FluentAssertions;
using Moq;
using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Application.Servicos;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Servicos;

public class TestesServicoServico
{
    private readonly Mock<IRepositorioServico> _repositorioServicos = new();
    private readonly Mock<IRepositorioNotificacao> _repositorioNotificacoes = new();
    private readonly Mock<IRepositorioAuditLog> _repositorioAuditLog = new();
    private readonly Mock<IRepositorioPerfilPrestador> _repositorioPerfil = new();
    private readonly Mock<IServicoFinanceiro> _servicoFinanceiro = new();
    private readonly ServicoServico _sut;

    public TestesServicoServico()
    {
        // Por padrão, o prestador atende a cidade (matching por proximidade).
        _repositorioPerfil
            .Setup(r => r.AtendeCidadeAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        _sut = new ServicoServico(
            _repositorioServicos.Object,
            _repositorioNotificacoes.Object,
            _repositorioAuditLog.Object,
            _repositorioPerfil.Object,
            _servicoFinanceiro.Object);
    }

    // ── CriarSolicitacaoAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CriarSolicitacaoAsync_TituloVazio_LancaExcecaoValidacao()
    {
        var comando = new ComandoCriarServico("", null, Guid.NewGuid(), null, null, null);

        await _sut.Invoking(s => s.CriarSolicitacaoAsync(Guid.NewGuid(), comando))
            .Should().ThrowAsync<ExcecaoValidacao>();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_CategoriaIdVazio_LancaExcecaoValidacao()
    {
        var comando = new ComandoCriarServico("Serviço", null, Guid.Empty, null, null, null);

        await _sut.Invoking(s => s.CriarSolicitacaoAsync(Guid.NewGuid(), comando))
            .Should().ThrowAsync<ExcecaoValidacao>();
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_DadosValidos_CriaServicoEmNegociacao()
    {
        var clienteId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var comando = new ComandoCriarServico("Instalação elétrica", null, categoriaId, null, null, null);

        _repositorioServicos
            .Setup(r => r.AdicionarAsync(It.IsAny<Servico>()))
            .ReturnsAsync((Servico s) => s);

        var servicoRetornado = new Servico
        {
            Id = Guid.NewGuid(), Titulo = "Instalação elétrica",
            CategoriaId = categoriaId, Status = StatusServico.EmNegociacao
        };
        _repositorioServicos
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(servicoRetornado);

        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _repositorioNotificacoes.Setup(r => r.AdicionarAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

        var resultado = await _sut.CriarSolicitacaoAsync(clienteId, comando);

        resultado.Status.Should().Be("em_negociacao");
        _repositorioAuditLog.Verify(r => r.RegistrarAsync(
            It.Is<AuditLog>(a => a.Acao == "servico.criado" && a.UsuarioId == clienteId)
        ), Times.Once);
    }

    // ── VincularPrestadorAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task VincularPrestadorAsync_StatusNaoEmNegociacao_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var servico = new Servico { Id = servicoId, Status = StatusServico.EmAndamento };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.VincularPrestadorAsync(servicoId, Guid.NewGuid()))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task VincularPrestadorAsync_PrestadorJaVinculado_LancaExcecaoConflito()
    {
        var servicoId = Guid.NewGuid();
        var prestadorExistente = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.EmNegociacao,
            PrestadorId = prestadorExistente
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.VincularPrestadorAsync(servicoId, Guid.NewGuid()))
            .Should().ThrowAsync<ExcecaoConflito>();
    }

    [Fact]
    public async Task VincularPrestadorAsync_ClienteTentaSerPrestador_LancaExcecaoProibido()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmNegociacao, ClienteId = clienteId
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.VincularPrestadorAsync(servicoId, clienteId))
            .Should().ThrowAsync<ExcecaoProibido>();
    }

    // ── MarcarConcluidoAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task MarcarConcluidoAsync_StatusNaoEmAndamento_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmNegociacao, PrestadorId = prestadorId
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.MarcarConcluidoAsync(servicoId, prestadorId))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task MarcarConcluidoAsync_PrestadorErrado_LancaExcecaoProibido()
    {
        var servicoId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmAndamento, PrestadorId = Guid.NewGuid()
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.MarcarConcluidoAsync(servicoId, Guid.NewGuid()))
            .Should().ThrowAsync<ExcecaoProibido>();
    }

    [Fact]
    public async Task MarcarConcluidoAsync_Valido_AvancaParaAguardandoConfirmacao()
    {
        var servicoId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.EmAndamento,
            PrestadorId = prestadorId,
            ClienteId = clienteId,
            Titulo = "Serviço teste"
        };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);
        _repositorioServicos.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).ReturnsAsync((Servico s) => s);
        _repositorioNotificacoes.Setup(r => r.AdicionarAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

        var resultado = await _sut.MarcarConcluidoAsync(servicoId, prestadorId);

        resultado.Status.Should().Be("aguardando_confirmacao_cliente");
        _repositorioServicos.Verify(r => r.AtualizarAsync(
            It.Is<Servico>(s =>
                s.Status == StatusServico.AguardandoConfirmacaoCliente &&
                s.AguardandoConfirmacaoDesde != null)
        ), Times.Once);
    }

    // ── ConfirmarConclusaoAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmarConclusaoAsync_StatusErrado_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmAndamento, ClienteId = clienteId
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.ConfirmarConclusaoAsync(servicoId, clienteId))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task ConfirmarConclusaoAsync_ClienteErrado_LancaExcecaoProibido()
    {
        var servicoId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.AguardandoConfirmacaoCliente,
            ClienteId = Guid.NewGuid()
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.ConfirmarConclusaoAsync(servicoId, Guid.NewGuid()))
            .Should().ThrowAsync<ExcecaoProibido>();
    }

    // ── CancelarAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelarAsync_ServiçoConcluido_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico { Id = servicoId, Status = StatusServico.Concluido, ClienteId = clienteId };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.CancelarAsync(servicoId, clienteId, Papel.Usuario))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task CancelarAsync_ParticipanteCancela_EmNegociacao_Sucesso()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmNegociacao, ClienteId = clienteId
        };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);
        _repositorioServicos.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).ReturnsAsync((Servico s) => s);
        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);

        var resultado = await _sut.CancelarAsync(servicoId, clienteId, Papel.Usuario);

        resultado.Status.Should().Be("cancelado");
    }

    [Fact]
    public async Task CancelarAsync_ParticipanteCancela_EmAndamento_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmAndamento, ClienteId = clienteId
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.CancelarAsync(servicoId, clienteId, Papel.Usuario))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task CancelarAsync_EmDisputa_ParticipanteNaoPodeAgir_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmDisputa, ClienteId = clienteId
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.CancelarAsync(servicoId, clienteId, Papel.Usuario))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }
}
