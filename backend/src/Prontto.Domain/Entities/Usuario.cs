using Prontto.Domain.Enums;

namespace Prontto.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string HashSenha { get; set; } = string.Empty;
    public TipoConta TipoConta { get; set; }
    public Papel Papel { get; set; } = Papel.Usuario;

    // Legacy — mantido para compatibilidade
    public string? Especialidade { get; set; }

    // Substituiu Cidade (string livre) — agora FK
    public Guid? CidadeId { get; set; }

    /// <summary>Endereço padrão do usuário (usado para pré-preencher solicitações do cliente).</summary>
    public string? Endereco { get; set; }

    // Campos de perfil público
    public string? Cpf { get; set; }
    public string? FotoPerfilUrl { get; set; }
    public string? Slug { get; set; }
    public string? Descricao { get; set; }
    public decimal MediaAvaliacoes { get; set; } = 0m;
    public int TotalAvaliacoes { get; set; } = 0;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    // Soft delete
    public DateTime? DeletadoEm { get; set; }

    // Navegação
    public DadosBancarios? DadosBancarios { get; set; }
    public ICollection<Servico> ServicosComoCliente { get; set; } = [];
    public ICollection<Servico> ServicosComoPrestador { get; set; } = [];
    public ICollection<MensagemServico> Mensagens { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    // Perfil do Prestador
    public ICollection<CategoriaUsuario> Categorias { get; set; } = [];
    public ICollection<CidadeUsuario> Cidades { get; set; } = [];
    public ICollection<ImagemPortfolio> ImagensPortfolio { get; set; } = [];
}
