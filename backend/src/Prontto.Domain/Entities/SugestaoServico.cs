namespace Prontto.Domain.Entities;

/// <summary>
/// Sugestão de serviço enviada por um visitante/cliente quando a busca não encontra
/// o serviço desejado (RF — "sugerir serviço"). O admin recebe por e-mail e no painel.
/// </summary>
public class SugestaoServico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Atendida { get; set; } = false;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
