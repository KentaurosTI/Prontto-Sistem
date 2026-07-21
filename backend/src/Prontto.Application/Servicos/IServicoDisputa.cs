namespace Prontto.Application.Servicos;

public interface IServicoDisputa
{
    Task<DtoDisputa> AbrirDisputaAsync(Guid servicoId, Guid clienteId, string motivo, string? descricao);
    Task<DtoDisputa> ResolverDisputaAsync(Guid disputaId, Guid adminId, bool favorPrestador, string decisaoAdmin);
    Task<List<DtoDisputa>> ListarAbertasAsync();
}
