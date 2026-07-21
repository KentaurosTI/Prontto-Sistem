using Prontto.Domain.Entities;

namespace Prontto.Application.Auth;

public interface IServicoAutenticacao
{
    Task<ResultadoAutenticacao> CadastrarAsync(ComandoCadastro comando);
    Task<ResultadoAutenticacao> EntrarAsync(ComandoLogin comando);
    Task<Usuario> ObterUsuarioAtualAsync(Guid idUsuario);

    /// <summary>
    /// Renova a sessão usando o valor bruto do Refresh Token recebido via cookie.
    /// Aplica rotação obrigatória: revoga o token atual e emite um novo par.
    /// </summary>
    Task<ResultadoAutenticacao> RenovarSessaoAsync(string refreshTokenBruto, string? ip, string? userAgent);

    /// <summary>
    /// Revoga o Refresh Token do cookie, encerrando a sessão.
    /// </summary>
    Task LogoutAsync(string refreshTokenBruto);
}
