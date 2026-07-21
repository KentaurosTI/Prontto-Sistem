namespace Prontto.Domain.Entities;

public class Avaliacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServicoId { get; set; }
    public Servico Servico { get; set; } = null!;
    public Guid AvaliadorId { get; set; }
    public Usuario Avaliador { get; set; } = null!;
    public Guid AvaliadoId { get; set; }
    public Usuario Avaliado { get; set; } = null!;
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
