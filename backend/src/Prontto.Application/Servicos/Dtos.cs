namespace Prontto.Application.Servicos;

public record DtoServico(
    Guid Id,
    string Titulo,
    string? Descricao,
    Guid CategoriaId,
    string CategoriaNome,
    Guid? CidadeId,
    string? CidadeNome,
    Guid? ClienteId,
    string? ClienteNome,
    Guid? PrestadorId,
    string? PrestadorNome,
    decimal Preco,
    string Status,
    string? Endereco,
    DateTime? AgendadoEm,
    DateTime? ConcluidoEm,
    DateTime CriadoEm
);

public record DtoMensagemServico(
    Guid Id,
    Guid ServicoId,
    Guid? RemetenteId,
    string? RemetenteNome,
    string PapelRemetente,
    string TipoMensagem,
    string Conteudo,
    decimal? ValorProposta,
    string? StatusProposta,
    DateTime CriadoEm
);

public record ResultadoMensagensPaginadas(
    List<DtoMensagemServico> Mensagens,
    bool TemMais,
    Guid? UltimoId
);

public record DtoDisputa(
    Guid Id,
    Guid ServicoId,
    Guid AbertaPorId,
    string Motivo,
    string? Descricao,
    string Status,
    string? DecisaoAdmin,
    DateTime CriadoEm,
    DateTime? ResolvidoEm
);

public record ComandoCriarServico(
    string Titulo,
    string? Descricao,
    Guid CategoriaId,
    Guid? CidadeId,
    string? Endereco,
    DateTime? AgendadoEm,
    Guid? PrestadorId = null
);
