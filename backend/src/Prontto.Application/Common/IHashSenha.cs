namespace Prontto.Application.Common;

public interface IHashSenha
{
    string Hashear(string senha);
    bool Verificar(string senha, string hash);
}
