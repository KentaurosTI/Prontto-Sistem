namespace Prontto.Application.Avaliacoes;

public record DtoAvaliacao(
    Guid Id,
    Guid ServicoId,
    string NomeAvaliador,
    int Nota,
    string? Comentario,
    DateTime CriadoEm
);

public record ComandoRegistrarAvaliacao(int Nota, string? Comentario);

public record ResultadoListaAvaliacoes(
    IEnumerable<DtoAvaliacao> Items,
    int Total,
    int Pagina,
    int TotalPaginas
);
