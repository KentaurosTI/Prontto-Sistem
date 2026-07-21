using Prontto.Domain.Enums;

namespace Prontto.Domain.Entities;

public class Disputa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServicoId { get; set; }
    public Guid AbertaPorId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public StatusDisputa Status { get; set; } = StatusDisputa.Aberta;
    public Guid? ResolvidaPorId { get; set; }
    public string? DecisaoAdmin { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvidoEm { get; set; }

    public Servico? Servico { get; set; }
    public Usuario? AbertaPor { get; set; }
    public Usuario? ResolvidaPor { get; set; }
}
