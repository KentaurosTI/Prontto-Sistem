namespace Prontto.Domain.Entities;

/// <summary>
/// Imagens de trabalhos anteriores do Prestador hospedadas no Cloudinary.
/// Exibidas publicamente apenas quando Aprovada = true e DeletadoEm IS NULL.
/// </summary>
public class ImagemPortfolio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string CloudinaryPublicId { get; set; } = string.Empty;

    /// <summary>Default false. Preenchido quando Cloudinary processa a moderação.</summary>
    public bool Moderada { get; set; } = false;

    /// <summary>null = pendente, true = aprovada, false = rejeitada.</summary>
    public bool? Aprovada { get; set; }

    /// <summary>Ordena exibição na galeria do perfil.</summary>
    public int Ordem { get; set; } = 0;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Soft delete — imagens deletadas não aparecem no perfil.</summary>
    public DateTime? DeletadoEm { get; set; }

    // Navegação
    public Usuario Usuario { get; set; } = null!;
}
