using Prontto.Domain.Enums;

namespace Prontto.Domain.Entities;

public class DadosBancarios
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public TipoChavePix TipoChavePix { get; set; }
    public string ChavePix { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;
    public string? NomeBanco { get; set; }
    public string? Agencia { get; set; }
    public string? NumeroConta { get; set; }
    public string? TipoConta { get; set; }
    public string? PagarmeRecipientId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;
}
