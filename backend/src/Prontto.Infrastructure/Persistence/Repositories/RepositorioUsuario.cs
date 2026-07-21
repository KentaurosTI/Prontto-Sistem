using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioUsuario(ContextoBancoDados db) : IRepositorioUsuario
{
    public Task<Usuario?> ObterPorIdAsync(Guid id)
        => db.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id);

    public Task<Usuario?> ObterPorEmailAsync(string email)
        => db.Usuarios.FirstOrDefaultAsync(usuario => usuario.Email == email);

    public Task<Usuario?> ObterPorSlugAsync(string slug)
        => db.Usuarios.FirstOrDefaultAsync(usuario => usuario.Slug == slug);

    public async Task<IReadOnlyList<Usuario>> ListarNaoAdminsAsync(TipoConta? tipoConta = null, Guid? cidadeId = null)
        => await db.Usuarios
            .Where(usuario => usuario.Papel != Papel.Admin)
            .Where(usuario => usuario.DeletadoEm == null)
            .Where(usuario => tipoConta == null || usuario.TipoConta == tipoConta)
            .Where(usuario => cidadeId == null || usuario.CidadeId == cidadeId)
            .OrderByDescending(usuario => usuario.CriadoEm)
            .ToListAsync();

    public async Task<Usuario> AdicionarAsync(Usuario usuario)
    {
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> AtualizarAsync(Usuario usuario)
    {
        db.Usuarios.Update(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<IReadOnlyList<RefreshToken>> ListarTokensAtivosPorUsuarioAsync(Guid usuarioId)
        => await db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null && t.ExpiracaoEm > DateTime.UtcNow)
            .ToListAsync();

    public async Task RevogarTodosTokensPorUsuarioAsync(Guid usuarioId)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null)
            .ToListAsync();

        foreach (var token in tokens)
            token.RevogadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}
