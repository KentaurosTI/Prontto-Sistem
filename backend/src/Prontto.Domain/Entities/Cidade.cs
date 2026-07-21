namespace Prontto.Domain.Entities;

/// <summary>
/// Catálogo de cidades cobertas pela plataforma.
/// Todas as referências a cidade usam FK para esta tabela — nunca string livre.
/// </summary>
public class Cidade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;

    /// <summary>Sigla da UF em maiúsculas. Ex: SP, RJ, MG.</summary>
    public string Estado { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Cidades inativas não aparecem na busca.</summary>
    public bool Ativa { get; set; } = true;

    // Navegação
    public ICollection<CidadeUsuario> Usuarios { get; set; } = [];
}
