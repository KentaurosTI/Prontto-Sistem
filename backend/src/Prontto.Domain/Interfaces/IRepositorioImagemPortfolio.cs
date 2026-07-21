using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioImagemPortfolio
{
    Task<IReadOnlyList<ImagemPortfolio>> ListarPendentesModeracaoAsync();
    Task<ImagemPortfolio?> ObterPorIdAsync(Guid id);
    Task AtualizarAsync(ImagemPortfolio imagem);
}
