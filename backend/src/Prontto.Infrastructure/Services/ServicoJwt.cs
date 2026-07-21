using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Prontto.Application.Auth;
using Prontto.Domain.Entities;

namespace Prontto.Infrastructure.Services;

public class ServicoJwt(IConfiguration configuracao, IHostEnvironment ambiente) : IServicoJwt
{
    private readonly string _segredo = ResolverSegredo(configuracao, ambiente);

    private static string ResolverSegredo(IConfiguration configuracao, IHostEnvironment ambiente)
    {
        var segredo = configuracao["SESSION_SECRET"];
        if (string.IsNullOrWhiteSpace(segredo))
        {
            if (ambiente.IsProduction())
                throw new InvalidOperationException(
                    "SESSION_SECRET é obrigatória em produção. Configure a variável de ambiente antes de iniciar a aplicação.");

            segredo = "prontto-secret-dev-local-32chars!!";
        }
        return segredo;
    }

    public string GerarToken(Usuario usuario)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_segredo));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("accountType", usuario.TipoConta.ToString().ToLower()),
            new Claim(ClaimTypes.Role, usuario.Papel.ToString().ToLower()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credenciais
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GerarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string ComputarHashRefreshToken(string tokenBruto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenBruto));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
