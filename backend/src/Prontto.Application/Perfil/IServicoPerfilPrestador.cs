namespace Prontto.Application.Perfil;

public interface IServicoPerfilPrestador
{
    /// <summary>
    /// Atualiza o perfil público do prestador.
    /// O Slug é gerado na primeira chamada (write-once, ADR-09) e ignorado em edições posteriores.
    /// </summary>
    Task<DtoPerfilPublico> AtualizarPerfilAsync(Guid usuarioId, ComandoAtualizarPerfil comando);

    /// <summary>Retorna o perfil público pelo slug. Lança ExcecaoNaoEncontrado se não existir.</summary>
    Task<DtoPerfilPublico> ObterPerfilPublicoAsync(string slug);

    /// <summary>Lista todas as categorias ativas, ordenadas por Ordem.</summary>
    Task<List<DtoCategoriaPublica>> ListarCategoriasAsync();

    /// <summary>Lista todas as cidades ativas, ordenadas por Nome.</summary>
    Task<List<DtoCidadePublica>> ListarCidadesAsync();

    /// <summary>
    /// Busca paginada de prestadores por categoria e cidade (RF-03).
    /// Lança ExcecaoNaoEncontrado se categoriaSlug ou cidadeSlug não forem encontrados ou estiverem inativos.
    /// </summary>
    Task<ResultadoPaginado<DtoPrestadorBusca>> BuscarPrestadoresAsync(
        string categoriaSlug,
        string? cidadeSlug,
        int page,
        int pageSize);

    /// <summary>Retorna dados agregados para a página inicial: categorias, destaques e avaliações recentes.</summary>
    Task<DtoDadosHome> ObterDadosHomeAsync();
}
