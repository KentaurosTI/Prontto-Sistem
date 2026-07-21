using Prontto.Domain.Entities;

namespace Prontto.Application.Auth;

/// <param name="Token">Access Token JWT (15 minutos). Retornado no corpo da resposta.</param>
/// <param name="RefreshToken">Valor bruto do Refresh Token. Deve ser enviado via cookie HttpOnly — nunca exposto no corpo.</param>
/// <param name="Usuario">Dados do usuário autenticado.</param>
public record ResultadoAutenticacao(string Token, string RefreshToken, Usuario Usuario);
