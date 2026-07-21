using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.UnitTests.Financeiro;

public class TestesServicoFinanceiro
{
    private readonly Mock<IRepositorioCobranca> _repositorioCobrancas = new();
    private readonly Mock<IRepositorioServico> _repositorioServicos = new();
    private readonly Mock<IRepositorioBanking> _repositorioBanking = new();
    private readonly Mock<IRepositorioAuditLog> _repositorioAuditLog = new();
    private readonly Mock<IRepositorioNotificacao> _repositorioNotificacoes = new();
    private readonly Mock<IProcessadorPagamento> _processadorPagamento = new();
    private readonly Mock<IConfiguration> _configuracao = new();
    private readonly ServicoFinanceiro _sut;

    private const string SegredoWebhook = "segredo-teste-hmac";

    public TestesServicoFinanceiro()
    {
        _configuracao
            .Setup(c => c["PAGARME_WEBHOOK_SECRET"])
            .Returns(SegredoWebhook);

        _repositorioAuditLog
            .Setup(r => r.RegistrarAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        _repositorioNotificacoes
            .Setup(r => r.AdicionarAsync(It.IsAny<Notificacao>()))
            .Returns(Task.CompletedTask);

        _sut = new ServicoFinanceiro(
            _repositorioCobrancas.Object,
            _repositorioServicos.Object,
            _repositorioBanking.Object,
            _repositorioAuditLog.Object,
            _repositorioNotificacoes.Object,
            _processadorPagamento.Object,
            _configuracao.Object,
            NullLogger<ServicoFinanceiro>.Instance);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string GerarHmacSha256(string payload, string segredo)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(segredo));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string MontarPayloadWebhook(string tipo, string orderId, string? chargeId = null)
    {
        var chargesJson = chargeId != null
            ? $"[{{\"id\":\"{chargeId}\"}}]"
            : "[]";

        return $"{{\"type\":\"{tipo}\",\"data\":{{\"id\":\"{orderId}\",\"charges\":{chargesJson}}}}}";
    }

    // ── GerarPixAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GerarPixAsync_CobrancaNaoEncontrada_LancaExcecaoNaoEncontrado()
    {
        var servicoId = Guid.NewGuid();
        _repositorioCobrancas
            .Setup(r => r.ObterPorServicoIdAsync(servicoId))
            .ReturnsAsync((Cobranca?)null);

        await _sut.Invoking(s => s.GerarPixAsync(servicoId))
            .Should().ThrowAsync<ExcecaoNaoEncontrado>();
    }

    [Fact]
    public async Task GerarPixAsync_CobrancaJaPaga_RetornaDtoSemChamarGateway()
    {
        var servicoId = Guid.NewGuid();
        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            ServicoId = servicoId,
            Status = StatusCobranca.Retido,
            ValorTotal = 200m,
            TaxaAdmin = 40m,
            ValorPrestador = 160m,
            PagarmeOrderId = "ORDER-EXISTENTE",
            CriadoEm = DateTime.UtcNow
        };

        _repositorioCobrancas
            .Setup(r => r.ObterPorServicoIdAsync(servicoId))
            .ReturnsAsync(cobranca);

        var resultado = await _sut.GerarPixAsync(servicoId);

        resultado.Should().NotBeNull();
        resultado.Status.Should().Be("retido");
        // Gateway NÃO deve ter sido chamado para cobrança já paga
        _processadorPagamento.Verify(
            p => p.GerarPixAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    // ── ProcessarWebhookAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessarWebhookAsync_AssinaturaInvalida_LancaExcecaoNaoAutorizado()
    {
        var payload = MontarPayloadWebhook("order.paid", Guid.NewGuid().ToString());
        const string assinaturaInvalida = "assinatura-errada";

        await _sut.Invoking(s => s.ProcessarWebhookAsync(payload, assinaturaInvalida))
            .Should().ThrowAsync<ExcecaoNaoAutorizado>()
            .WithMessage("*HMAC*");
    }

    [Fact]
    public async Task ProcessarWebhookAsync_EventoDesconhecido_IgnoraSeProcessar()
    {
        // Eventos como "order.created", "order.canceled" não devem disparar atualização de cobrança
        var orderId = Guid.NewGuid().ToString();
        var payload = MontarPayloadWebhook("order.created", orderId);
        var assinatura = GerarHmacSha256(payload, SegredoWebhook);

        await _sut.ProcessarWebhookAsync(payload, assinatura);

        // Nenhuma consulta de cobrança deve ter ocorrido
        _repositorioCobrancas.Verify(
            r => r.ObterPorPagarmeOrderIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessarWebhookAsync_EventoPago_AtualizaCobrancaParaRetido()
    {
        var orderId = "ORDER-PAGARME-123";
        var chargeId = "CHARGE-PAGARME-456";
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();

        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            ServicoId = servicoId,
            PagarmeOrderId = orderId,
            Status = StatusCobranca.Pendente,
            ValorTotal = 300m,
            CriadoEm = DateTime.UtcNow
        };

        var servico = new Servico
        {
            Id = servicoId,
            Titulo = "Instalação elétrica",
            Status = StatusServico.AguardandoPagamento,
            ClienteId = clienteId,
            PrestadorId = prestadorId
        };

        var payload = MontarPayloadWebhook("order.paid", orderId, chargeId);
        var assinatura = GerarHmacSha256(payload, SegredoWebhook);

        _repositorioCobrancas
            .Setup(r => r.ObterPorPagarmeOrderIdAsync(orderId))
            .ReturnsAsync(cobranca);
        _repositorioCobrancas
            .Setup(r => r.AtualizarAsync(It.IsAny<Cobranca>()))
            .ReturnsAsync((Cobranca c) => c);
        _repositorioServicos
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync(servico);
        _repositorioServicos
            .Setup(r => r.AtualizarAsync(It.IsAny<Servico>()))
            .ReturnsAsync((Servico s) => s);

        await _sut.ProcessarWebhookAsync(payload, assinatura);

        cobranca.Status.Should().Be(StatusCobranca.Retido);
        cobranca.PagarmePagamentoId.Should().Be(chargeId);
        cobranca.PagadoEm.Should().NotBeNull();
        cobranca.RetidoEm.Should().NotBeNull();

        servico.Status.Should().Be(StatusServico.EmAndamento);
    }

    [Fact]
    public async Task ProcessarWebhookAsync_WebhookDuplicado_IgnoraSemReprocessar()
    {
        var orderId = "ORDER-DUPLICADO";
        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            PagarmeOrderId = orderId,
            Status = StatusCobranca.Retido, // já processado anteriormente
            ValorTotal = 100m,
            CriadoEm = DateTime.UtcNow
        };

        var payload = MontarPayloadWebhook("order.paid", orderId);
        var assinatura = GerarHmacSha256(payload, SegredoWebhook);

        _repositorioCobrancas
            .Setup(r => r.ObterPorPagarmeOrderIdAsync(orderId))
            .ReturnsAsync(cobranca);

        await _sut.ProcessarWebhookAsync(payload, assinatura);

        // Não deve ter atualizado cobrança novamente (idempotência)
        _repositorioCobrancas.Verify(
            r => r.AtualizarAsync(It.IsAny<Cobranca>()),
            Times.Never);
    }

    // ── ReembolsarAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ReembolsarAsync_CobrancaJaLiberada_NaoReembolsa()
    {
        var servicoId = Guid.NewGuid();
        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            ServicoId = servicoId,
            Status = StatusCobranca.Liberado, // já liberada — não pode reembolsar
            ValorTotal = 200m,
            PagarmeOrderId = "ORDER-XYZ",
            CriadoEm = DateTime.UtcNow
        };

        _repositorioCobrancas
            .Setup(r => r.ObterPorServicoIdAsync(servicoId))
            .ReturnsAsync(cobranca);

        await _sut.ReembolsarAsync(servicoId);

        _processadorPagamento.Verify(
            p => p.ReembolsarAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── LiberarPagamentoAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task LiberarPagamentoAsync_CobrancaRetida_LiberaComSucesso()
    {
        var servicoId = Guid.NewGuid();
        var prestadorId = Guid.NewGuid();

        var cobranca = new Cobranca
        {
            Id = Guid.NewGuid(),
            ServicoId = servicoId,
            Status = StatusCobranca.Retido,
            ValorTotal = 500m,
            TaxaAdmin = 100m,
            ValorPrestador = 400m,
            PagarmeOrderId = "ORDER-LIB-001",
            CriadoEm = DateTime.UtcNow
        };

        var servico = new Servico
        {
            Id = servicoId,
            PrestadorId = prestadorId,
            Titulo = "Pintura residencial"
        };

        var dadosBancarios = new DadosBancarios
        {
            UsuarioId = prestadorId,
            ChavePix = "prestador@email.com",
            NomeCompleto = "Prestador Teste"
        };

        _repositorioCobrancas
            .Setup(r => r.ObterPorServicoIdAsync(servicoId))
            .ReturnsAsync(cobranca);
        _repositorioCobrancas
            .Setup(r => r.AtualizarAsync(It.IsAny<Cobranca>()))
            .ReturnsAsync((Cobranca c) => c);
        _repositorioServicos
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync(servico);
        _repositorioBanking
            .Setup(r => r.ObterPorUsuarioIdAsync(prestadorId))
            .ReturnsAsync(dadosBancarios);
        _processadorPagamento
            .Setup(p => p.TransferirAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.LiberarPagamentoAsync(servicoId);

        cobranca.Status.Should().Be(StatusCobranca.Liberado);
        cobranca.LiberadoEm.Should().NotBeNull();
        _processadorPagamento.Verify(
            p => p.TransferirAsync(400m, "prestador@email.com", "Prestador Teste", servicoId.ToString()),
            Times.Once);
    }
}
