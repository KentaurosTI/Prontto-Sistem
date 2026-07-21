namespace Prontto.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }

    /// <summary>Hash SHA-256 do token bruto. Nunca armazenar o valor bruto.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiracaoEm { get; set; }
    public DateTime? RevogadoEm { get; set; }

    /// <summary>Hash SHA-256 do token que substituiu este (rastreabilidade de rotação).</summary>
    public string? SubstituidoPor { get; set; }

    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public Usuario? Usuario { get; set; }

    public bool EstaRevogado => RevogadoEm.HasValue;
    public bool EstaExpirado => DateTime.UtcNow >= ExpiracaoEm;
    public bool EstaAtivo => !EstaRevogado && !EstaExpirado;
}
