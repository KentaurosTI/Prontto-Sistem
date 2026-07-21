using Prontto.Domain.Enums;

namespace Prontto.Application.Auth;

public record ComandoCadastro(
    string Nome,
    string Email,
    string Senha,
    TipoConta TipoConta,
    string? Telefone = null,
    string? Especialidade = null,
    Guid? CidadeId = null,
    string? Ip = null,
    string? UserAgent = null
);
