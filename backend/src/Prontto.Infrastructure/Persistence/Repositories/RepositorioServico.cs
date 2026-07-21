using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioServico(ContextoBancoDados db) : IRepositorioServico
{
    public Task<Servico?> ObterPorIdAsync(Guid id)
        => db.Servicos
            .Include(s => s.Cliente)
            .Include(s => s.Prestador)
            .Include(s => s.Categoria)
            .Include(s => s.Cidade)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<Servico?> ObterPorIdComDetalhesAsync(Guid id)
        => db.Servicos
            .Include(s => s.Cliente)
            .Include(s => s.Prestador)
            .Include(s => s.Categoria)
            .Include(s => s.Cidade)
            .Include(s => s.Cobranca)
            .Include(s => s.Mensagens.OrderBy(m => m.CriadoEm))
                .ThenInclude(m => m.Remetente)
            .Include(s => s.Disputa)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IReadOnlyList<Servico>> ListarTodosAsync()
        => await db.Servicos
            .Where(s => s.DeletadoEm == null)
            .Include(s => s.Cliente)
            .Include(s => s.Prestador)
            .Include(s => s.Categoria)
            .Include(s => s.Cidade)
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync();

    public async Task<List<Servico>> ListarPorClienteAsync(Guid clienteId)
        => await db.Servicos
            .Include(s => s.Categoria)
            .Include(s => s.Cidade)
            .Include(s => s.Prestador)
            .Where(s => s.ClienteId == clienteId)
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync();

    public async Task<List<Servico>> ListarPorPrestadorAsync(Guid prestadorId)
        => await db.Servicos
            .Include(s => s.Categoria)
            .Include(s => s.Cidade)
            .Include(s => s.Cliente)
            .Where(s => s.PrestadorId == prestadorId)
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync();

    /// <summary>
    /// Lista serviços em EmNegociacao sem prestador vinculado, cujas categoria e cidade
    /// correspondam a pelo menos uma das categorias/cidades do prestador.
    /// </summary>
    public async Task<List<Servico>> ListarDisponiveisParaPrestadorAsync(Guid prestadorId)
    {
        // Obtém as categorias e cidades do prestador
        var categoriasPrestador = await db.CategoriasUsuario
            .Where(cu => cu.UsuarioId == prestadorId)
            .Select(cu => cu.CategoriaId)
            .ToListAsync();

        var cidadesPrestador = await db.CidadesUsuario
            .Where(cu => cu.UsuarioId == prestadorId)
            .Select(cu => cu.CidadeId)
            .ToListAsync();

        var query = db.Servicos
            .Include(s => s.Categoria)
            .Include(s => s.Cidade)
            .Include(s => s.Cliente)
            .Where(s =>
                s.Status == StatusServico.EmNegociacao &&
                s.PrestadorId == null &&
                categoriasPrestador.Contains(s.CategoriaId));

        // Filtra por cidade do prestador se o prestador tem cidades cadastradas
        if (cidadesPrestador.Count > 0)
        {
            query = query.Where(s =>
                s.CidadeId == null ||
                cidadesPrestador.Contains(s.CidadeId!.Value));
        }

        return await query
            .OrderByDescending(s => s.CriadoEm)
            .ToListAsync();
    }

    /// <summary>
    /// Lista serviços em AguardandoConfirmacaoCliente há mais de 7 dias (para auto-conclusão).
    /// </summary>
    public async Task<List<Servico>> ListarParaAutoConclusaoAsync()
    {
        var limite = DateTime.UtcNow.AddDays(-7);
        return await db.Servicos
            .Where(s =>
                s.Status == StatusServico.AguardandoConfirmacaoCliente &&
                s.AguardandoConfirmacaoDesde != null &&
                s.AguardandoConfirmacaoDesde < limite)
            .ToListAsync();
    }

    public Task<int> ContarPorStatusAsync(StatusServico status)
        => db.Servicos.CountAsync(s => s.Status == status);

    public Task<int> ContarTodosAsync()
        => db.Servicos.CountAsync();

    public async Task<Servico> AdicionarAsync(Servico servico)
    {
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();
        return servico;
    }

    public async Task<Servico> AtualizarAsync(Servico servico)
    {
        db.Servicos.Update(servico);
        await db.SaveChangesAsync();
        return servico;
    }
}
