using Prontto.Domain.Enums;

namespace Prontto.Domain.Entities;

public class MensagemServico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServicoId { get; set; }
    public Guid? RemetenteId { get; set; }
    public PapelRemetente PapelRemetente { get; set; }
    public TipoMensagem TipoMensagem { get; set; } = TipoMensagem.Texto;
    public string Conteudo { get; set; } = string.Empty;
    public decimal? ValorProposta { get; set; }
    public StatusProposta? StatusProposta { get; set; }
    public bool ImagemModerada { get; set; } = false;
    public bool? ImagemAprovada { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Servico Servico { get; set; } = null!;
    public Usuario? Remetente { get; set; }
}
