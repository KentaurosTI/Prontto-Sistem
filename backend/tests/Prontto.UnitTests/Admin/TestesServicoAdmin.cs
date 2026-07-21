using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Prontto.Application.Admin;
using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Admin;

public class TestesServicoAdmin
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

    public TestesServicoAdmin()
    {
        _repositorioAuditLog.Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);

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
    public async Task AtualizarStatusServicoAsync_ParaConcluido_CriaCobranca()
    {
        var idServico = Guid.NewGuid();
        var servico = new Servico
        {
            Id = idServico, Preco = 100m, TaxaAdminRate = 0.2m,
            Status = StatusServico.EmAndamento
        };

        _repositorioServicos.Setup(r => r.ObterPorIdAsync(idServico)).ReturnsAsync(servico);
        _repositorioCobrancas.Setup(r => r.ExistePorServicoAsync(idServico)).ReturnsAsync(false);
        _repositorioCobrancas.Setup(r => r.AdicionarAsync(It.IsAny<Cobranca>())).ReturnsAsync((Cobranca c) => c);
        _repositorioServicos.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).ReturnsAsync((Servico s) => s);

        var resultado = await _sut.AtualizarStatusServicoAsync(idServico, StatusServico.Concluido);

        resultado.Status.Should().Be("concluido");
        resultado.ConcluidoEm.Should().NotBeNull();
        _repositorioCobrancas.Verify(r => r.AdicionarAsync(It.Is<Cobranca>(c =>
            c.TaxaAdmin == 20m &&
            c.ValorPrestador == 80m &&
            c.ValorTotal == 100m &&
            c.Status == StatusCobranca.Pago
        )), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusServicoAsync_ParaConcluido_NaoDuplicaCobranca()
    {
        var idServico = Guid.NewGuid();
        var servico = new Servico { Id = idServico, Preco = 100m, TaxaAdminRate = 0.2m };
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(idServico)).ReturnsAsync(servico);
        _repositorioCobrancas.Setup(r => r.ExistePorServicoAsync(idServico)).ReturnsAsync(true);
        _repositorioServicos.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).ReturnsAsync(servico);

        await _sut.AtualizarStatusServicoAsync(idServico, StatusServico.Concluido);

        _repositorioCobrancas.Verify(r => r.AdicionarAsync(It.IsAny<Cobranca>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarStatusServicoAsync_NaoEncontrado_LancaExcecaoNaoEncontrado()
    {
        _repositorioServicos.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Servico?)null);

        await _sut.Invoking(s => s.AtualizarStatusServicoAsync(Guid.NewGuid(), StatusServico.Cancelado))
            .Should().ThrowAsync<ExcecaoNaoEncontrado>();
    }

    [Fact]
    public async Task EnviarMensagemAsync_ConteudoVazio_LancaExcecaoValidacao()
    {
        await _sut.Invoking(s => s.EnviarMensagemAsync(Guid.NewGuid(), Guid.NewGuid(), "   "))
            .Should().ThrowAsync<ExcecaoValidacao>();
    }

    [Fact]
    public async Task ObterEstatisticasAsync_RetornaAgregacaoCorreta()
    {
        _repositorioUsuarios.Setup(r => r.ListarNaoAdminsAsync(null, null)).ReturnsAsync([
            new Usuario { TipoConta = TipoConta.Cliente },
            new Usuario { TipoConta = TipoConta.Prestador },
        ]);
        _repositorioServicos.Setup(r => r.ContarTodosAsync()).ReturnsAsync(5);
        _repositorioServicos.Setup(r => r.ContarPorStatusAsync(StatusServico.EmNegociacao)).ReturnsAsync(2);
        _repositorioServicos.Setup(r => r.ContarPorStatusAsync(StatusServico.EmAndamento)).ReturnsAsync(1);
        _repositorioServicos.Setup(r => r.ContarPorStatusAsync(StatusServico.Concluido)).ReturnsAsync(2);
        _repositorioCobrancas.Setup(r => r.SomarTaxaAdminPorStatusAsync(StatusCobranca.Pago)).ReturnsAsync(40m);
        _repositorioCobrancas.Setup(r => r.SomarTaxaAdminPorStatusAsync(StatusCobranca.Pendente)).ReturnsAsync(0m);
        _repositorioCobrancas.Setup(r => r.SomarValorTotalPorStatusAsync(StatusCobranca.Pago)).ReturnsAsync(200m);

        var stats = await _sut.ObterEstatisticasAsync();

        stats.Usuarios.Total.Should().Be(2);
        stats.Usuarios.Clientes.Should().Be(1);
        stats.Usuarios.Prestadores.Should().Be(1);
        stats.Servicos.Total.Should().Be(5);
        stats.Receita.Ganha.Should().Be(40m);
        stats.Receita.Gmv.Should().Be(200m);
    }

    // ── Novos testes ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListarUsuariosAsync_ComFiltroTipoConta_PropagaFiltro()
    {
        _repositorioUsuarios
            .Setup(r => r.ListarNaoAdminsAsync(TipoConta.Prestador, null))
            .ReturnsAsync([new Usuario { TipoConta = TipoConta.Prestador }]);

        var resultado = await _sut.ListarUsuariosAsync(TipoConta.Prestador, null);

        resultado.Should().HaveCount(1);
        resultado[0].TipoConta.Should().Be(TipoConta.Prestador);
        _repositorioUsuarios.Verify(r => r.ListarNaoAdminsAsync(TipoConta.Prestador, null), Times.Once);
    }

    [Fact]
    public async Task ObterUsuarioPorIdAsync_UsuarioExistente_RetornaUsuario()
    {
        var id = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Nome = "João" };
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);

        var resultado = await _sut.ObterUsuarioPorIdAsync(id);

        resultado.Id.Should().Be(id);
        resultado.Nome.Should().Be("João");
    }

    [Fact]
    public async Task ObterUsuarioPorIdAsync_UsuarioNaoEncontrado_LancaExcecaoNaoEncontrado()
    {
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);

        await _sut.Invoking(s => s.ObterUsuarioPorIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<ExcecaoNaoEncontrado>();
    }

    [Fact]
    public async Task BloquearUsuarioAsync_UsuarioAdmin_LancaExcecaoConflito()
    {
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Papel = Papel.Admin };
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);

        await _sut.Invoking(s => s.BloquearUsuarioAsync(id, adminId))
            .Should().ThrowAsync<ExcecaoConflito>()
            .WithMessage("*administrador*");
    }

    [Fact]
    public async Task BloquearUsuarioAsync_UsuarioValido_PreencheDeletadoEmERegistraAuditLog()
    {
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Email = "user@test.com", Papel = Papel.Usuario };
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);
        _repositorioUsuarios.Setup(r => r.AtualizarAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);

        await _sut.BloquearUsuarioAsync(id, adminId);

        usuario.DeletadoEm.Should().NotBeNull();
        _repositorioAuditLog.Verify(r => r.RegistrarAsync(It.Is<AuditLog>(
            log => log.Acao == "admin.usuario.bloqueado" && log.EntidadeId == id.ToString()
        )), Times.Once);
    }

    [Fact]
    public async Task DesbloquearUsuarioAsync_UsuarioBloqueado_LimpaDeletadoEmERegistraAuditLog()
    {
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Email = "user@test.com", DeletadoEm = DateTime.UtcNow };
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);
        _repositorioUsuarios.Setup(r => r.AtualizarAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);

        await _sut.DesbloquearUsuarioAsync(id, adminId);

        usuario.DeletadoEm.Should().BeNull();
        _repositorioAuditLog.Verify(r => r.RegistrarAsync(It.Is<AuditLog>(
            log => log.Acao == "admin.usuario.desbloqueado" && log.EntidadeId == id.ToString()
        )), Times.Once);
    }

    [Fact]
    public async Task RevogarSessoesAsync_UsuarioExistente_RevokaTokensERegistraAuditLog()
    {
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Email = "user@test.com" };
        _repositorioUsuarios.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(usuario);
        _repositorioUsuarios.Setup(r => r.RevogarTodosTokensPorUsuarioAsync(id)).Returns(Task.CompletedTask);

        await _sut.RevogarSessoesAsync(id, adminId);

        _repositorioUsuarios.Verify(r => r.RevogarTodosTokensPorUsuarioAsync(id), Times.Once);
        _repositorioAuditLog.Verify(r => r.RegistrarAsync(It.Is<AuditLog>(
            log => log.Acao == "admin.usuario.sessoes_revogadas" && log.EntidadeId == id.ToString()
        )), Times.Once);
    }

    [Fact]
    public async Task ListarLogsAsync_LimitaTamanhoPaginaA100()
    {
        _repositorioAuditLog
            .Setup(r => r.ListarAsync(1, 100, null, null))
            .ReturnsAsync((new List<AuditLog>().AsReadOnly() as IReadOnlyList<AuditLog>, 0));

        var resultado = await _sut.ListarLogsAsync(1, 200, null, null);

        resultado.TamanhoPagina.Should().Be(100);
        _repositorioAuditLog.Verify(r => r.ListarAsync(1, 100, null, null), Times.Once);
    }

    [Fact]
    public async Task ObterExtratoFinanceiroAsync_RetornaSomasCorretas()
    {
        _repositorioCobrancas.Setup(r => r.SomarTaxaAdminPorStatusAsync(StatusCobranca.Liberado)).ReturnsAsync(500m);
        _repositorioCobrancas.Setup(r => r.SomarTaxaAdminPorStatusAsync(StatusCobranca.Pendente)).ReturnsAsync(100m);
        _repositorioCobrancas.Setup(r => r.SomarTaxaAdminPorStatusAsync(StatusCobranca.Retido)).ReturnsAsync(200m);
        _repositorioCobrancas.Setup(r => r.ListarUltimasComDetalhesAsync(20)).ReturnsAsync([]);

        var extrato = await _sut.ObterExtratoFinanceiroAsync();

        extrato.TotalArrecadado.Should().Be(500m);
        extrato.TotalPendente.Should().Be(100m);
        extrato.TotalRetido.Should().Be(200m);
    }
}
