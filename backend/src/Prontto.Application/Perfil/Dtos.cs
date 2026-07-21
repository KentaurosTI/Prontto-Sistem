namespace Prontto.Application.Perfil;

/// <summary>
/// Resultado de um prestador na busca paginada (RF-03).
/// LGPD: nunca incluir email, CPF, hashSenha ou dados bancários.
/// </summary>
public record DtoPrestadorBusca(
    Guid Id,
    string Nome,
    string? FotoPerfilUrl,
    string Slug,
    decimal MediaAvaliacoes,
    int TotalAvaliacoes,
    List<DtoCategoriaPublica> Categorias,
    List<DtoCidadePublica> Cidades
);

/// <summary>Resposta paginada genérica (RF-03).</summary>
public record ResultadoPaginado<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// Perfil público do prestador.
/// LGPD: nunca incluir email, CPF, hashSenha ou dados bancários.
/// </summary>
public record DtoPerfilPublico(
    Guid Id,
    string Nome,
    string? FotoPerfilUrl,
    string? Slug,
    string? Descricao,
    string? Especialidade,
    decimal MediaAvaliacoes,
    int TotalAvaliacoes,
    List<DtoCategoriaPublica> Categorias,
    List<DtoCidadePublica> Cidades,
    List<DtoImagemPortfolio> ImagensPortfolio
);

public record DtoCategoriaPublica(Guid Id, string Nome, string Slug);

public record DtoCidadePublica(Guid Id, string Nome, string Estado, string Slug);

public record DtoImagemPortfolio(Guid Id, string Url, int Ordem);

/// <summary>Dados agregados para a página inicial (RF-01).</summary>
public record DtoDadosHome(
    List<DtoCategoriaPublica> Categorias,
    List<DtoPrestadorBusca> PrestadoresDestaque,
    List<DtoAvaliacaoHome> AvaliacoesRecentes
);

/// <summary>Avaliação simplificada para exibição na home.</summary>
public record DtoAvaliacaoHome(
    string NomeAvaliador,
    int Nota,
    string Comentario,
    string ServicoTitulo,
    string Cidade
);

/// <summary>Payload para criação/edição de perfil do prestador.</summary>
public record ComandoAtualizarPerfil(
    string? Nome,
    string? Descricao,
    string? Especialidade,
    string? FotoPerfilUrl,
    List<Guid>? CategoriaIds,
    List<Guid>? CidadeIds
);
