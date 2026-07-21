namespace Prontto.Application.Auth;

public record ComandoLogin(
    string Email,
    string Senha,
    string? Ip = null,
    string? UserAgent = null
);
