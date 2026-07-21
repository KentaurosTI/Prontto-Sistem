using Prontto.Domain.Entities;
using Prontto.Domain.Enums;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioCobranca
{
    Task<IReadOnlyList<Cobranca>> ListarTodosAsync();
    Task<IReadOnlyList<Cobranca>> ListarUltimasComDetalhesAsync(int quantidade);
    Task<decimal> SomarTaxaAdminPorStatusAsync(StatusCobranca status);
    Task<decimal> SomarValorTotalPorStatusAsync(StatusCobranca status);
    Task<bool> ExistePorServicoAsync(Guid idServico);
    Task<Cobranca> AdicionarAsync(Cobranca cobranca);
    Task<Cobranca> AtualizarAsync(Cobranca cobranca);
    Task<Cobranca?> ObterPorServicoIdAsync(Guid servicoId);
    Task<Cobranca?> ObterPorPagarmeOrderIdAsync(string pagarmeOrderId);
    Task<List<Cobranca>> ListarPendentesExpiradosAsync();
}
