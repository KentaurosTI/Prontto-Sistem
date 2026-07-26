using System.Security.Claims;
using Prontto.Application.Common;

namespace Prontto.Api.Controllers;

/// <summary>
/// Extensões para extrair claims obrigatórias do usuário autenticado (SCRUM-33).
/// Evita o padrão `Guid.Parse(User.FindFirstValue("userId")!)`, que lançava
/// ArgumentNullException → 500 genérico quando a claim estava ausente.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Retorna o Guid do usuário a partir da claim `userId`.
    /// Lança <see cref="ExcecaoNaoAutorizado"/> (→ HTTP 401) se ausente ou inválida.
    /// </summary>
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue("userId");
        if (string.IsNullOrWhiteSpace(valor))
            throw new ExcecaoNaoAutorizado("Token inválido: claim userId ausente");
        if (!Guid.TryParse(valor, out var id))
            throw new ExcecaoNaoAutorizado("Token inválido: claim userId inválida");
        return id;
    }
}
