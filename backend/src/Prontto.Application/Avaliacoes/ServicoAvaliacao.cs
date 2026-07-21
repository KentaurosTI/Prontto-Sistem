using Prontto.Application.Common;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Application.Avaliacoes;

public class ServicoAvaliacao(
    IRepositorioAvaliacao repositorioAvaliacoes,
    IRepositorioServico repositorioServicos,
    IRepositorioUsuario repositorioUsuarios) : IServicoAvaliacao
{
    public async Task<DtoAvaliacao> RegistrarAsync(Guid servicoId, Guid avaliadorId, ComandoRegistrarAvaliacao comando)
    {
        if (comando.Nota < 1 || comando.Nota > 5)
            throw new ExcecaoValidacao("A nota deve ser entre 1 e 5");

        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        if (servico.Status != StatusServico.Concluido)
            throw new ExcecaoTransicaoInvalida("Serviço não concluído");

        if (!servico.ConcluidoEm.HasValue || DateTime.UtcNow > servico.ConcluidoEm.Value.AddDays(30))
            throw new ExcecaoTransicaoInvalida("Prazo para avaliação expirado");

        var eParticipante = avaliadorId == servico.ClienteId || avaliadorId == servico.PrestadorId;
        if (!eParticipante)
            throw new ExcecaoProibido("Apenas participantes do serviço podem avaliar");

        var jaAvaliou = await repositorioAvaliacoes.ExisteAvaliacaoAsync(servicoId, avaliadorId);
        if (jaAvaliou)
            throw new ExcecaoConflito("Você já avaliou este serviço");

        // Define quem é o avaliado: se avaliador é o cliente, avaliado é o prestador, e vice-versa
        var avaliadoId = avaliadorId == servico.ClienteId
            ? servico.PrestadorId!.Value
            : servico.ClienteId!.Value;

        var avaliacao = new Avaliacao
        {
            ServicoId = servicoId,
            AvaliadorId = avaliadorId,
            AvaliadoId = avaliadoId,
            Nota = comando.Nota,
            Comentario = comando.Comentario?.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        await repositorioAvaliacoes.AdicionarAsync(avaliacao);

        // Recalcula e atualiza média do avaliado
        var avaliado = await repositorioUsuarios.ObterPorIdAsync(avaliadoId)
            ?? throw new ExcecaoNaoEncontrado("Usuário avaliado não encontrado");

        var (media, total) = await repositorioAvaliacoes.CalcularMediaAsync(avaliadoId);
        avaliado.MediaAvaliacoes = media;
        avaliado.TotalAvaliacoes = total;
        avaliado.AtualizadoEm = DateTime.UtcNow;

        await repositorioUsuarios.AtualizarAsync(avaliado);

        // Precisa carregar o avaliador para exibir o nome
        var avaliador = await repositorioUsuarios.ObterPorIdAsync(avaliadorId)
            ?? throw new ExcecaoNaoEncontrado("Avaliador não encontrado");

        return MapearDto(avaliacao, avaliador.Nome);
    }

    public async Task<ResultadoListaAvaliacoes> ListarPorPrestadorSlugAsync(string slug, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize > 50) pageSize = 50;
        if (pageSize < 1) pageSize = 20;

        var prestador = await repositorioUsuarios.ObterPorSlugAsync(slug)
            ?? throw new ExcecaoNaoEncontrado("Prestador não encontrado");

        var (itens, total) = await repositorioAvaliacoes.ListarPorAvaliadoAsync(prestador.Id, page, pageSize);

        var totalPaginas = (int)Math.Ceiling((double)total / pageSize);

        var dtos = itens.Select(a => MapearDto(a, a.Avaliador?.Nome ?? string.Empty));

        return new ResultadoListaAvaliacoes(dtos, total, page, totalPaginas);
    }

    public async Task<IEnumerable<DtoAvaliacao>> ListarPorServicoAsync(Guid servicoId, Guid usuarioId)
    {
        var servico = await repositorioServicos.ObterPorIdAsync(servicoId)
            ?? throw new ExcecaoNaoEncontrado("Serviço não encontrado");

        var eParticipante = usuarioId == servico.ClienteId || usuarioId == servico.PrestadorId;
        if (!eParticipante)
            throw new ExcecaoProibido("Apenas participantes do serviço podem ver as avaliações");

        var avaliacoes = await repositorioAvaliacoes.ListarPorServicoAsync(servicoId);

        return avaliacoes.Select(a => MapearDto(a, a.Avaliador?.Nome ?? string.Empty));
    }

    // Aplica LGPD: exibe apenas o primeiro nome do avaliador
    private static DtoAvaliacao MapearDto(Avaliacao avaliacao, string nomeCompleto)
    {
        var primeiroNome = nomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        return new DtoAvaliacao(
            Id: avaliacao.Id,
            ServicoId: avaliacao.ServicoId,
            NomeAvaliador: primeiroNome,
            Nota: avaliacao.Nota,
            Comentario: avaliacao.Comentario,
            CriadoEm: avaliacao.CriadoEm
        );
    }
}
