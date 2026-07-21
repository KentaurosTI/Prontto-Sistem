namespace Prontto.Application.Avaliacoes;

public interface IServicoAvaliacao
{
    Task<DtoAvaliacao> RegistrarAsync(Guid servicoId, Guid avaliadorId, ComandoRegistrarAvaliacao comando);
    Task<ResultadoListaAvaliacoes> ListarPorPrestadorSlugAsync(string slug, int page, int pageSize);
    Task<IEnumerable<DtoAvaliacao>> ListarPorServicoAsync(Guid servicoId, Guid usuarioId);
}
