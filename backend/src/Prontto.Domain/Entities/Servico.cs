using Prontto.Domain.Enums;

namespace Prontto.Domain.Entities;

public class Servico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    // FK para entidade canônica Categoria (RF-04 RN-08)
    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public Guid? CidadeId { get; set; }
    public Cidade? Cidade { get; set; }

    public Guid? ClienteId { get; set; }
    public Guid? PrestadorId { get; set; }
    public decimal Preco { get; set; }
    public decimal TaxaAdminRate { get; set; } = 0.2000m;
    public StatusServico Status { get; set; } = StatusServico.EmNegociacao;
    public string? Endereco { get; set; }
    public DateTime? AgendadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    public DateTime? AguardandoConfirmacaoDesde { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? DeletadoEm { get; set; }

    public Usuario? Cliente { get; set; }
    public Usuario? Prestador { get; set; }
    public Cobranca? Cobranca { get; set; }
    public ICollection<MensagemServico> Mensagens { get; set; } = [];
    public Disputa? Disputa { get; set; }
}
