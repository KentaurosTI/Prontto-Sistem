using FluentAssertions;
using Moq;
using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Application.Servicos;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Servicos;

public class TestesServicoDisputa
{
    private readonly Mock<IRepositorioServico> _repositorioServicos = new();
    private readonly Mock<IRepositorioDisputa> _repositorioDisputas = new();
    private readonly Mock<IRepositorioNotificacao> _repositorioNotificacoes = new();
    private readonly Mock<IRepositorioAuditLog> _repositorioAuditLog = new();
    private readonly Mock<IServicoFinanceiro> _servicoFinanceiro = new();
    private readonly ServicoDisputa _sut;

    public TestesServicoDisputa()
    {
        _sut = new ServicoDisputa(
            _repositorioServicos.Object,
            _repositorioDisputas.Object,
            _repositorioNotificacoes.Object,
            _repositorioAuditLog.Object,
            _servicoFinanceiro.Object);
    }

    // ── AbrirDisputaAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AbrirDisputaAsync_MotivoVazio_LancaExcecaoValidacao()
    {
        await _sut.Invoking(s => s.AbrirDisputaAsync(Guid.NewGuid(), Guid.NewGuid(), "", null))
            .Should().ThrowAsync<ExcecaoValidacao>();
    }

    [Fact]
    public async Task AbrirDisputaAsync_UsuarioNaoECliente_LancaExcecaoProibido()
    {
        var servicoId = Guid.NewGuid();
        var outroUsuarioId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.AguardandoConfirmacaoCliente,
            ClienteId = Guid.NewGuid() // diferente do usuário tentando abrir
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.AbrirDisputaAsync(servicoId, outroUsuarioId, "Motivo", null))
            .Should().ThrowAsync<ExcecaoProibido>();
    }

    [Fact]
    public async Task AbrirDisputaAsync_StatusErrado_LancaExcecaoTransicaoInvalida()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.EmAndamento, // não é AguardandoConfirmacaoCliente
            ClienteId = clienteId
        };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);

        await _sut.Invoking(s => s.AbrirDisputaAsync(servicoId, clienteId, "Motivo", null))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task AbrirDisputaAsync_DisputaJaExiste_LancaExcecaoConflito()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.AguardandoConfirmacaoCliente,
            ClienteId = clienteId
        };
        var disputaExistente = new Disputa { Id = Guid.NewGuid(), ServicoId = servicoId };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);
        _repositorioDisputas.Setup(r => r.ObterPorServicoIdAsync(servicoId)).ReturnsAsync(disputaExistente);

        await _sut.Invoking(s => s.AbrirDisputaAsync(servicoId, clienteId, "Motivo", null))
            .Should().ThrowAsync<ExcecaoConflito>();
    }

    [Fact]
    public async Task AbrirDisputaAsync_Valido_CriaDisputaEAvancaServicoParaEmDisputa()
    {
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();
        var servico = new Servico
        {
            Id = servicoId,
            Status = StatusServico.AguardandoConfirmacaoCliente,
            ClienteId = clienteId,
            PrestadorId = prestadorId,
            Titulo = "Serviço teste"
        };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);
        _repositorioDisputas.Setup(r => r.ObterPorServicoIdAsync(servicoId)).ReturnsAsync((Disputa?)null);
        _repositorioDisputas.Setup(r => r.AdicionarAsync(It.IsAny<Disputa>())).ReturnsAsync((Disputa d) => d);
        _repositorioServicos.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).ReturnsAsync((Servico s) => s);
        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _repositorioNotificacoes.Setup(r => r.AdicionarAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

        var resultado = await _sut.AbrirDisputaAsync(servicoId, clienteId, "Serviço mal feito", "Detalhes");

        resultado.Status.Should().Be("Aberta");
        resultado.Motivo.Should().Be("Serviço mal feito");
        resultado.Descricao.Should().Be("Detalhes");

        _repositorioServicos.Verify(r => r.AtualizarAsync(
            It.Is<Servico>(s => s.Status == StatusServico.EmDisputa)
        ), Times.Once);

        _repositorioAuditLog.Verify(r => r.RegistrarAsync(
            It.Is<AuditLog>(a => a.Acao == "disputa.aberta")
        ), Times.Once);
    }

    // ── ResolverDisputaAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResolverDisputaAsync_DecisaoVazia_LancaExcecaoValidacao()
    {
        await _sut.Invoking(s => s.ResolverDisputaAsync(Guid.NewGuid(), Guid.NewGuid(), true, "  "))
            .Should().ThrowAsync<ExcecaoValidacao>();
    }

    [Fact]
    public async Task ResolverDisputaAsync_DisputaJaResolvida_LancaExcecaoTransicaoInvalida()
    {
        var disputaId = Guid.NewGuid();
        var disputa = new Disputa { Id = disputaId, Status = StatusDisputa.ResolvidaPrestador };
        _repositorioDisputas.Setup(r => r.ObterPorIdAsync(disputaId)).ReturnsAsync(disputa);

        await _sut.Invoking(s => s.ResolverDisputaAsync(disputaId, Guid.NewGuid(), true, "Decisão"))
            .Should().ThrowAsync<ExcecaoTransicaoInvalida>();
    }

    [Fact]
    public async Task ResolverDisputaAsync_FavorPrestador_ConcluidoEAuditLog()
    {
        var disputaId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();

        var disputa = new Disputa
        {
            Id = disputaId, ServicoId = servicoId, Status = StatusDisputa.Aberta
        };
        var servico = new Servico
        {
            Id = servicoId, Status = StatusServico.EmDisputa,
            ClienteId = clienteId, PrestadorId = prestadorId, Titulo = "Serviço"
        };

        _repositorioDisputas.Setup(r => r.ObterPorIdAsync(disputaId)).ReturnsAsync(disputa);
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);
        _repositorioDisputas.Setup(r => r.AtualizarAsync(It.IsAny<Disputa>())).ReturnsAsync((Disputa d) => d);
        _repositorioServicos.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).ReturnsAsync((Servico s) => s);
        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _repositorioNotificacoes.Setup(r => r.AdicionarAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

        var resultado = await _sut.ResolverDisputaAsync(disputaId, adminId, true, "Serviço realizado conforme combinado");

        resultado.Status.Should().Be("ResolvidaPrestador");
        resultado.DecisaoAdmin.Should().Be("Serviço realizado conforme combinado");

        _repositorioServicos.Verify(r => r.AtualizarAsync(
            It.Is<Servico>(s => s.Status == StatusServico.Concluido)
        ), Times.Once);

        _repositorioAuditLog.Verify(r => r.RegistrarAsync(
            It.Is<AuditLog>(a => a.Acao == "disputa.resolvida" && a.UsuarioId == adminId)
        ), Times.Once);
    }
}
