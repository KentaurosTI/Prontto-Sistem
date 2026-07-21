using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioCobranca(ContextoBancoDados db) : IRepositorioCobranca
{
    public async Task<IReadOnlyList<Cobranca>> ListarTodosAsync()
        => await db.Cobrancas
            .Include(cobranca => cobranca.Servico)
            .OrderByDescending(cobranca => cobranca.CriadoEm)
            .ToListAsync();

    public async Task<IReadOnlyList<Cobranca>> ListarUltimasComDetalhesAsync(int quantidade)
        => await db.Cobrancas
            .Include(cobranca => cobranca.Servico)
                .ThenInclude(servico => servico.Cliente)
            .Include(cobranca => cobranca.Servico)
                .ThenInclude(servico => servico.Prestador)
            .OrderByDescending(cobranca => cobranca.CriadoEm)
            .Take(quantidade)
            .ToListAsync();

    public async Task<decimal> SomarTaxaAdminPorStatusAsync(StatusCobranca status)
        => await db.Cobrancas
            .Where(cobranca => cobranca.Status == status)
            .SumAsync(cobranca => (decimal?)cobranca.TaxaAdmin) ?? 0m;

    public async Task<decimal> SomarValorTotalPorStatusAsync(StatusCobranca status)
        => await db.Cobrancas
            .Where(cobranca => cobranca.Status == status)
            .SumAsync(cobranca => (decimal?)cobranca.ValorTotal) ?? 0m;

    public Task<bool> ExistePorServicoAsync(Guid idServico)
        => db.Cobrancas.AnyAsync(cobranca => cobranca.ServicoId == idServico);

    public async Task<Cobranca> AdicionarAsync(Cobranca cobranca)
    {
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();
        return cobranca;
    }

    public async Task<Cobranca> AtualizarAsync(Cobranca cobranca)
    {
        db.Cobrancas.Update(cobranca);
        await db.SaveChangesAsync();
        return cobranca;
    }

    public Task<Cobranca?> ObterPorServicoIdAsync(Guid servicoId)
        => db.Cobrancas.FirstOrDefaultAsync(c => c.ServicoId == servicoId);

    public Task<Cobranca?> ObterPorPagarmeOrderIdAsync(string pagarmeOrderId)
        => db.Cobrancas.FirstOrDefaultAsync(c => c.PagarmeOrderId == pagarmeOrderId);

    public async Task<List<Cobranca>> ListarPendentesExpiradosAsync()
        => await db.Cobrancas
            .Where(c => c.Status == StatusCobranca.Pendente
                     && c.PixExpiracaoEm != null
                     && c.PixExpiracaoEm < DateTime.UtcNow)
            .ToListAsync();
}
