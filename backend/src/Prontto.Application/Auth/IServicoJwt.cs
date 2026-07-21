using Prontto.Domain.Entities;

namespace Prontto.Application.Auth;

public interface IServicoJwt
{
    /// <summary>Gera o Access Token JWT (HS256, 15 minutos).</summary>
    string GerarToken(Usuario usuario);

    /// <summary>Gera um valor aleatório criptograficamente seguro para o Refresh Token (64 bytes hex).</summary>
    string GerarRefreshToken();

    /// <summary>Computa o hash SHA-256 do token bruto, retornado como string hex lowercase.</summary>
    string ComputarHashRefreshToken(string tokenBruto);
}
