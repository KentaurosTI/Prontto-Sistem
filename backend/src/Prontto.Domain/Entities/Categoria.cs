namespace Prontto.Domain.Entities;

/// <summary>
/// Catálogo canônico de categorias de serviço.
/// Todas as referências a categoria usam FK para esta tabela — nunca string livre.
/// </summary>
public class Categoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Categorias inativas não aparecem na busca ou no cadastro de prestadores.</summary>
    public bool Ativa { get; set; } = true;

    /// <summary>Ordena a exibição no frontend.</summary>
    public int Ordem { get; set; } = 0;

    // Navegação
    public ICollection<CategoriaUsuario> Usuarios { get; set; } = [];
}
