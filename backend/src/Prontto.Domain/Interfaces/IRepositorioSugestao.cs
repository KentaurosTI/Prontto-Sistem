using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioSugestao
{
    Task<SugestaoServico> AdicionarAsync(SugestaoServico sugestao);
    Task<List<SugestaoServico>> ListarAsync(int limite = 200);
}
