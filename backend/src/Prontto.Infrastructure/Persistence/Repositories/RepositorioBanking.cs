using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;

namespace Prontto.Infrastructure.Persistence.Repositories;

public class RepositorioBanking(ContextoBancoDados db) : IRepositorioBanking
{
    public Task<DadosBancarios?> ObterPorUsuarioIdAsync(Guid idUsuario)
        => db.DadosBancarios.FirstOrDefaultAsync(dados => dados.UsuarioId == idUsuario);

    public async Task<DadosBancarios> SalvarAsync(DadosBancarios dadosBancarios)
    {
        var existente = await db.DadosBancarios
            .FirstOrDefaultAsync(dados => dados.UsuarioId == dadosBancarios.UsuarioId);

        if (existente is null)
        {
            db.DadosBancarios.Add(dadosBancarios);
        }
        else
        {
            existente.TipoChavePix = dadosBancarios.TipoChavePix;
            existente.ChavePix = dadosBancarios.ChavePix;
            existente.NomeCompleto = dadosBancarios.NomeCompleto;
            existente.CpfCnpj = dadosBancarios.CpfCnpj;
            existente.NomeBanco = dadosBancarios.NomeBanco;
            existente.Agencia = dadosBancarios.Agencia;
            existente.NumeroConta = dadosBancarios.NumeroConta;
            existente.TipoConta = dadosBancarios.TipoConta;
            if (dadosBancarios.PagarmeRecipientId != null)
                existente.PagarmeRecipientId = dadosBancarios.PagarmeRecipientId;
            existente.AtualizadoEm = DateTime.UtcNow;
            dadosBancarios = existente;
        }

        await db.SaveChangesAsync();
        return dadosBancarios;
    }
}
