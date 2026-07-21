using Prontto.Application.Common;

namespace Prontto.Infrastructure.Services;

public class HashSenhaBcrypt : IHashSenha
{
    public string Hashear(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, 12);
    public bool Verificar(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
