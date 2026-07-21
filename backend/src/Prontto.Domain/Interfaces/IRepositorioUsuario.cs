using Prontto.Domain.Entities;
using Prontto.Domain.Enums;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioUsuario
{
    Task<Usuario?> ObterPorIdAsync(Guid id);
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<Usuario?> ObterPorSlugAsync(string slug);
    Task<IReadOnlyList<Usuario>> ListarNaoAdminsAsync(TipoConta? tipoConta = null, Guid? cidadeId = null);
    Task<Usuario> AdicionarAsync(Usuario usuario);
    Task<Usuario> AtualizarAsync(Usuario usuario);
    Task<IReadOnlyList<RefreshToken>> ListarTokensAtivosPorUsuarioAsync(Guid usuarioId);
    Task RevogarTodosTokensPorUsuarioAsync(Guid usuarioId);
}
