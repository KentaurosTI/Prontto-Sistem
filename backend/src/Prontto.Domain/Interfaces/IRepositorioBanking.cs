using Prontto.Domain.Entities;

namespace Prontto.Domain.Interfaces;

public interface IRepositorioBanking
{
    Task<DadosBancarios?> ObterPorUsuarioIdAsync(Guid idUsuario);
    Task<DadosBancarios> SalvarAsync(DadosBancarios dadosBancarios);
}
