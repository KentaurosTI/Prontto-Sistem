namespace Prontto.Domain.Entities;

/// <summary>
/// Relação muitos-para-muitos entre Prestador e suas categorias de serviço.
/// Tabela: usuarios_categorias. PK composta: (UsuarioId, CategoriaId).
/// </summary>
public class CategoriaUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid CategoriaId { get; set; }

    // Navegação
    public Usuario Usuario { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
}
