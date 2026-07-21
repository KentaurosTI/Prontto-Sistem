namespace Prontto.Domain.Entities;

/// <summary>
/// Relação muitos-para-muitos entre Prestador e cidades onde atua.
/// Tabela: usuarios_cidades. PK composta: (UsuarioId, CidadeId).
/// </summary>
public class CidadeUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid CidadeId { get; set; }

    // Navegação
    public Usuario Usuario { get; set; } = null!;
    public Cidade Cidade { get; set; } = null!;
}
