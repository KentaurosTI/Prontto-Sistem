using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prontto.Application.Financeiro;
using Prontto.Application.Servicos;
using Prontto.Domain.Enums;

namespace Prontto.Api.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize]
public class ControladorServicos(
    IServicoServico servicoServico,
    IServicoNegociacao servicoNegociacao,
    IServicoDisputa servicoDisputa,
    IServicoFinanceiro servicoFinanceiro) : ControllerBase
{
    // ── Criação (apenas Cliente) ───────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CriarSolicitacao([FromBody] RequisicaoCriarServico req)
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();

        if (tipoConta != TipoConta.Cliente)
            return StatusCode(403, new { error = "Apenas clientes podem criar solicitações de serviço" });

        var comando = new ComandoCriarServico(
            req.Titulo,
            req.Descricao,
            req.CategoriaId,
            req.CidadeId,
            req.Endereco,
            req.AgendadoEm,
            req.PrestadorId);

        var servico = await servicoServico.CriarSolicitacaoAsync(userId, comando);
        return StatusCode(201, new { servico });
    }

    // ── Listagens ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ListarMeusServicos()
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();
        var servicos = await servicoServico.ListarMeusServicosAsync(userId, tipoConta);
        return Ok(new { servicos });
    }

    [HttpGet("disponiveis")]
    public async Task<IActionResult> ListarDisponiveis()
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();

        if (tipoConta != TipoConta.Prestador)
            return StatusCode(403, new { error = "Apenas prestadores podem ver solicitações disponíveis" });

        var servicos = await servicoServico.ListarDisponiveisParaPrestadorAsync(userId);
        return Ok(new { servicos });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var userId = ObterUsuarioId();
        var servico = await servicoServico.ObterPorIdAsync(id, userId);
        return Ok(new { servico });
    }

    // ── Mensagens e propostas ──────────────────────────────────────────────────

    [HttpGet("{id:guid}/mensagens")]
    public async Task<IActionResult> ListarMensagens(
        Guid id,
        [FromQuery] Guid? afterId = null,
        [FromQuery] int limite = 50)
    {
        if (limite < 1) limite = 1;
        if (limite > 100) limite = 100;

        var userId = ObterUsuarioId();
        var resultado = await servicoNegociacao.ListarMensagensPaginadasAsync(id, userId, afterId, limite);
        return Ok(resultado);
    }

    [HttpPost("{id:guid}/mensagem")]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> EnviarMensagem(Guid id, [FromBody] RequisicaoMensagemTexto req)
    {
        var userId = ObterUsuarioId();
        var papel = ObterPapelRemetente();
        var mensagem = await servicoNegociacao.EnviarMensagemTextoAsync(id, userId, papel, req.Conteudo);
        return StatusCode(201, new { mensagem });
    }

    [HttpPost("{id:guid}/proposta")]
    public async Task<IActionResult> EnviarProposta(Guid id, [FromBody] RequisicaoProposta req)
    {
        var userId = ObterUsuarioId();
        var papel = ObterPapelRemetente();
        var mensagem = await servicoNegociacao.EnviarPropostaAsync(id, userId, papel, req.Valor);
        return StatusCode(201, new { mensagem });
    }

    [HttpPatch("{id:guid}/proposta/{mensagemId:guid}/aceitar")]
    public async Task<IActionResult> AceitarProposta(Guid id, Guid mensagemId)
    {
        var userId = ObterUsuarioId();
        var servico = await servicoNegociacao.AceitarPropostaAsync(id, mensagemId, userId);
        return Ok(new { servico });
    }

    // ── Transições de estado ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/vincular")]
    public async Task<IActionResult> VincularPrestador(Guid id)
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();

        if (tipoConta != TipoConta.Prestador)
            return StatusCode(403, new { error = "Apenas prestadores podem se vincular a serviços" });

        var servico = await servicoServico.VincularPrestadorAsync(id, userId);
        return Ok(new { servico });
    }

    [HttpPatch("{id:guid}/concluir")]
    public async Task<IActionResult> MarcarConcluido(Guid id)
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();

        if (tipoConta != TipoConta.Prestador)
            return StatusCode(403, new { error = "Apenas prestadores podem marcar serviços como concluídos" });

        var servico = await servicoServico.MarcarConcluidoAsync(id, userId);
        return Ok(new { servico });
    }

    [HttpPatch("{id:guid}/confirmar")]
    public async Task<IActionResult> ConfirmarConclusao(Guid id)
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();

        if (tipoConta != TipoConta.Cliente)
            return StatusCode(403, new { error = "Apenas clientes podem confirmar a conclusão do serviço" });

        var servico = await servicoServico.ConfirmarConclusaoAsync(id, userId);
        return Ok(new { servico });
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] RequisicaoCancelamento? req = null)
    {
        var userId = ObterUsuarioId();
        var papel = ObterPapel();
        var servico = await servicoServico.CancelarAsync(id, userId, papel, req?.Motivo);
        return Ok(new { servico });
    }

    [HttpPost("{id:guid}/disputa")]
    public async Task<IActionResult> AbrirDisputa(Guid id, [FromBody] RequisicaoDisputa req)
    {
        var userId = ObterUsuarioId();
        var tipoConta = ObterTipoConta();

        if (tipoConta != TipoConta.Cliente)
            return StatusCode(403, new { error = "Apenas clientes podem abrir disputas" });

        var disputa = await servicoDisputa.AbrirDisputaAsync(id, userId, req.Motivo, req.Descricao);
        return StatusCode(201, new { disputa });
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private Guid ObterUsuarioId()
        => Guid.Parse(User.FindFirstValue("userId")!);

    private TipoConta ObterTipoConta()
    {
        var tipoConta = User.FindFirstValue("accountType") ?? "cliente";
        return Enum.TryParse<TipoConta>(tipoConta, ignoreCase: true, out var resultado)
            ? resultado
            : TipoConta.Cliente;
    }

    private Papel ObterPapel()
    {
        var papel = User.FindFirstValue(ClaimTypes.Role) ?? "usuario";
        return Enum.TryParse<Papel>(papel, ignoreCase: true, out var resultado)
            ? resultado
            : Papel.Usuario;
    }

    private PapelRemetente ObterPapelRemetente()
    {
        var tipoConta = User.FindFirstValue("accountType") ?? "cliente";
        return tipoConta.ToLower() == "prestador" ? PapelRemetente.Prestador : PapelRemetente.Cliente;
    }

    // ── Cobrança / PIX ────────────────────────────────────────────────────────

    [HttpGet("{id}/cobranca")]
    public async Task<IActionResult> ObterCobranca(Guid id)
    {
        var usuarioId = ObterUsuarioId();
        var servico = await servicoServico.ObterPorIdAsync(id, usuarioId);

        var eParticipante = servico.ClienteId == usuarioId
                         || servico.PrestadorId == usuarioId
                         || ObterPapel() == Papel.Admin;
        if (!eParticipante)
            return StatusCode(403, new { error = "Acesso negado" });

        var cobranca = await servicoFinanceiro.ObterPorServicoAsync(id);
        if (cobranca == null)
            return NotFound(new { error = "Cobrança não encontrada" });

        return Ok(new { cobranca });
    }
}

// ── Request Records ────────────────────────────────────────────────────────────

public record RequisicaoCriarServico(
    string Titulo,
    string? Descricao,
    Guid CategoriaId,
    Guid? CidadeId,
    string? Endereco,
    DateTime? AgendadoEm,
    Guid? PrestadorId = null
);

public record RequisicaoMensagemTexto(string Conteudo);
public record RequisicaoProposta(decimal Valor);
public record RequisicaoCancelamento(string? Motivo);
public record RequisicaoDisputa(string Motivo, string? Descricao);
