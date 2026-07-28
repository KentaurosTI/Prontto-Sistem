using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioPerfilPrestador(ContextoBancoDados db) : IRepositorioPerfilPrestador
{
    public Task<Usuario?> ObterPorSlugAsync(string slug)
        => db.Usuarios
            .Include(u => u.Categorias).ThenInclude(cu => cu.Categoria)
            .Include(u => u.Cidades).ThenInclude(cu => cu.Cidade)
            .Include(u => u.ImagensPortfolio.Where(i => i.Aprovada == true))
            .FirstOrDefaultAsync(u => u.Slug == slug);

    public async Task AtualizarPerfilAsync(
        Usuario usuario,
        IEnumerable<Guid> categoriaIds,
        IEnumerable<Guid> cidadeIds)
    {
        var catIds = categoriaIds.Distinct().ToList();
        var cidIds = cidadeIds.Distinct().ToList();

        // Valida existência antes de inserir (evita FK violation silenciosa) — SCRUM-50.
        if (catIds.Count > 0)
        {
            var existentesCat = await db.Categorias.Where(c => catIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
            var invalidas = catIds.Except(existentesCat).ToList();
            if (invalidas.Count > 0)
                throw new InvalidOperationException($"Categoria(s) inexistente(s): {string.Join(", ", invalidas)}");
        }
        if (cidIds.Count > 0)
        {
            var existentesCid = await db.Cidades.Where(c => cidIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
            var invalidas = cidIds.Except(existentesCid).ToList();
            if (invalidas.Count > 0)
                throw new InvalidOperationException($"Cidade(s) inexistente(s): {string.Join(", ", invalidas)}");
        }

        // Envolve Remove + Add + SaveChanges numa transação explícita para garantir
        // atomicidade sob concorrência (evita perda de categorias/cidades) — SCRUM-50.
        await using var transacao = await db.Database.BeginTransactionAsync();
        try
        {
            var categoriasAtuais = await db.CategoriasUsuario
                .Where(cu => cu.UsuarioId == usuario.Id)
                .ToListAsync();
            db.CategoriasUsuario.RemoveRange(categoriasAtuais);

            var cidadesAtuais = await db.CidadesUsuario
                .Where(cu => cu.UsuarioId == usuario.Id)
                .ToListAsync();
            db.CidadesUsuario.RemoveRange(cidadesAtuais);

            foreach (var catId in catIds)
                db.CategoriasUsuario.Add(new CategoriaUsuario { UsuarioId = usuario.Id, CategoriaId = catId });

            foreach (var cidId in cidIds)
                db.CidadesUsuario.Add(new CidadeUsuario { UsuarioId = usuario.Id, CidadeId = cidId });

            db.Usuarios.Update(usuario);
            await db.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        catch
        {
            await transacao.RollbackAsync();
            throw;
        }
    }

    public Task<List<ImagemPortfolio>> ListarImagensAprovadasAsync(Guid usuarioId)
        => db.ImagensPortfolio
            .Where(i => i.UsuarioId == usuarioId && i.Aprovada == true)
            .OrderBy(i => i.Ordem)
            .ToListAsync();

    public Task<ImagemPortfolio?> ObterImagemPorIdAsync(Guid id)
        => db.ImagensPortfolio.FirstOrDefaultAsync(i => i.Id == id);

    public async Task AdicionarImagemAsync(ImagemPortfolio imagem)
    {
        db.ImagensPortfolio.Add(imagem);
        await db.SaveChangesAsync();
    }

    public async Task RemoverImagemAsync(ImagemPortfolio imagem)
    {
        // Soft delete — mantém registro para auditoria e job de limpeza do Cloudinary
        imagem.DeletadoEm = DateTime.UtcNow;
        db.ImagensPortfolio.Update(imagem);
        await db.SaveChangesAsync();
    }

    public async Task<List<Usuario>> ListarDestaqueAsync(int limite)
        => await db.Usuarios
            .Where(u =>
                u.TipoConta == TipoConta.Prestador &&
                u.Slug != null &&
                u.TotalAvaliacoes > 0)
            .OrderByDescending(u => u.MediaAvaliacoes)
            .ThenByDescending(u => u.TotalAvaliacoes)
            .Take(limite)
            .Include(u => u.Categorias).ThenInclude(cu => cu.Categoria)
            .Include(u => u.Cidades).ThenInclude(cu => cu.Cidade)
            .AsNoTracking()
            .ToListAsync();

    public async Task<(List<Usuario> Items, int Total)> BuscarAsync(
        Guid categoriaId,
        Guid? cidadeId,
        int skip,
        int take)
    {
        // Base query: prestadores com perfil completo e não deletados
        // O filtro global de soft delete (DeletadoEm IS NULL) já é aplicado pelo EF Core.
        var query = db.Usuarios
            .Where(u =>
                u.TipoConta == TipoConta.Prestador &&
                u.Slug != null &&
                u.Categorias.Any(cu => cu.CategoriaId == categoriaId));

        // Filtro de cidade (opcional)
        if (cidadeId.HasValue)
            query = query.Where(u => u.Cidades.Any(cu => cu.CidadeId == cidadeId.Value));

        // Contagem total para paginação
        var total = await query.CountAsync();

        // Busca com navegação para montar o DTO
        var items = await query
            .OrderByDescending(u => u.MediaAvaliacoes)
            .ThenBy(u => u.Nome)
            .Skip(skip)
            .Take(take)
            .Include(u => u.Categorias).ThenInclude(cu => cu.Categoria)
            .Include(u => u.Cidades).ThenInclude(cu => cu.Cidade)
            .ToListAsync();

        return (items, total);
    }

    public Task<bool> AtendeCidadeAsync(Guid prestadorId, Guid cidadeId)
        => db.Usuarios.AnyAsync(u =>
            u.Id == prestadorId &&
            u.TipoConta == TipoConta.Prestador &&
            u.Cidades.Any(cu => cu.CidadeId == cidadeId));
}
