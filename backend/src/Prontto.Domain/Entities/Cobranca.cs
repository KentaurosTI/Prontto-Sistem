using Prontto.Domain.Enums;

namespace Prontto.Domain.Entities;

public class Cobranca
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServicoId { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal TaxaAdmin { get; set; }
    public decimal ValorPrestador { get; set; }
    public StatusCobranca Status { get; set; } = StatusCobranca.Pendente;

    // Integração Pagar.me
    public string? PagarmeOrderId { get; set; }
    public string? PagarmePagamentoId { get; set; }
    public string? PixQrCode { get; set; }
    public string? PixCopiaCola { get; set; }
    public DateTime? PixExpiracaoEm { get; set; }

    public DateTime? PagadoEm { get; set; }
    public DateTime? RetidoEm { get; set; }
    public DateTime? LiberadoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public Servico Servico { get; set; } = null!;
}
