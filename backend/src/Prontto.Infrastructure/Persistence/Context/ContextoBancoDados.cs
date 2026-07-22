using Microsoft.EntityFrameworkCore;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;

namespace Prontto.Infrastructure.Persistence.Context;

public class ContextoBancoDados(DbContextOptions<ContextoBancoDados> opcoes) : DbContext(opcoes)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DadosBancarios> DadosBancarios => Set<DadosBancarios>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<MensagemServico> MensagensServico => Set<MensagemServico>();
    public DbSet<Cobranca> Cobrancas => Set<Cobranca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Cidade> Cidades => Set<Cidade>();
    public DbSet<CategoriaUsuario> CategoriasUsuario => Set<CategoriaUsuario>();
    public DbSet<CidadeUsuario> CidadesUsuario => Set<CidadeUsuario>();
    public DbSet<ImagemPortfolio> ImagensPortfolio => Set<ImagemPortfolio>();
    public DbSet<Disputa> Disputas => Set<Disputa>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<AuditLog> LogsAuditoria => Set<AuditLog>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Usuario ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Slug).IsUnique();
            e.HasIndex(u => new { u.TipoConta, u.Slug }); // acelera filtro de prestadores com slug na busca da home

            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Nome).HasColumnName("nome");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.Telefone).HasColumnName("telefone");
            e.Property(u => u.HashSenha).HasColumnName("hash_senha");
            e.Property(u => u.TipoConta).HasColumnName("tipo_conta").HasConversion<string>();
            e.Property(u => u.Papel).HasColumnName("papel").HasConversion<string>();
            e.Property(u => u.Especialidade).HasColumnName("especialidade");
            e.Property(u => u.CidadeId).HasColumnName("cidade_id");
            e.Property(u => u.Cpf).HasColumnName("cpf");
            e.Property(u => u.FotoPerfilUrl).HasColumnName("url_foto_perfil");
            e.Property(u => u.Slug).HasColumnName("slug");
            e.Property(u => u.Descricao).HasColumnName("descricao");
            e.Property(u => u.MediaAvaliacoes).HasColumnName("media_avaliacoes").HasPrecision(3, 2);
            e.Property(u => u.TotalAvaliacoes).HasColumnName("total_avaliacoes");
            e.Property(u => u.CriadoEm).HasColumnName("criado_em");
            e.Property(u => u.AtualizadoEm).HasColumnName("atualizado_em");
            e.Property(u => u.DeletadoEm).HasColumnName("deletado_em");

            // Filtro global de soft delete — usuários deletados ficam invisíveis
            e.HasQueryFilter(u => u.DeletadoEm == null);
        });

        // ── RefreshToken ───────────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("tokens_renovacao");
            e.HasIndex(t => t.Token).IsUnique();
            e.HasIndex(t => new { t.UsuarioId, t.RevogadoEm });

            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.UsuarioId).HasColumnName("usuario_id");
            e.Property(t => t.Token).HasColumnName("token");
            e.Property(t => t.ExpiracaoEm).HasColumnName("expira_em");
            e.Property(t => t.RevogadoEm).HasColumnName("revogado_em");
            e.Property(t => t.SubstituidoPor).HasColumnName("substituido_por");
            e.Property(t => t.Ip).HasColumnName("endereco_ip");
            e.Property(t => t.UserAgent).HasColumnName("user_agent");
            e.Property(t => t.CriadoEm).HasColumnName("criado_em");

            e.HasOne(t => t.Usuario)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DadosBancarios ─────────────────────────────────────────────────────
        modelBuilder.Entity<DadosBancarios>(e =>
        {
            e.ToTable("dados_bancarios");

            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.UsuarioId).HasColumnName("usuario_id");
            e.Property(b => b.TipoChavePix).HasColumnName("tipo_chave_pix").HasConversion<string>();
            e.Property(b => b.ChavePix).HasColumnName("chave_pix");
            e.Property(b => b.NomeCompleto).HasColumnName("nome_completo");
            e.Property(b => b.CpfCnpj).HasColumnName("cpf_cnpj");
            e.Property(b => b.NomeBanco).HasColumnName("nome_banco");
            e.Property(b => b.Agencia).HasColumnName("agencia");
            e.Property(b => b.NumeroConta).HasColumnName("numero_conta");
            e.Property(b => b.TipoConta).HasColumnName("tipo_conta");
            e.Property(b => b.PagarmeRecipientId).HasColumnName("pagarme_recipient_id").HasMaxLength(50).IsRequired(false);
            e.Property(b => b.CriadoEm).HasColumnName("criado_em");
            e.Property(b => b.AtualizadoEm).HasColumnName("atualizado_em");

            e.HasOne(b => b.Usuario)
                .WithOne(u => u.DadosBancarios)
                .HasForeignKey<DadosBancarios>(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Servico ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Servico>(e =>
        {
            e.ToTable("servicos");

            e.HasIndex(s => s.ClienteId);
            e.HasIndex(s => s.PrestadorId);
            e.HasIndex(s => s.Status);
            e.HasIndex(s => s.AguardandoConfirmacaoDesde);

            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Titulo).HasColumnName("titulo");
            e.Property(s => s.Descricao).HasColumnName("descricao");
            e.Property(s => s.CategoriaId).HasColumnName("categoria_id");
            e.Property(s => s.CidadeId).HasColumnName("cidade_id");
            e.Property(s => s.ClienteId).HasColumnName("cliente_id");
            e.Property(s => s.PrestadorId).HasColumnName("prestador_id");
            e.Property(s => s.Preco).HasColumnName("preco").HasPrecision(10, 2);
            e.Property(s => s.TaxaAdminRate).HasColumnName("taxa_admin_percentual").HasPrecision(5, 4);
            e.Property(s => s.Status).HasColumnName("status").HasConversion<string>();
            e.Property(s => s.Endereco).HasColumnName("endereco");
            e.Property(s => s.AgendadoEm).HasColumnName("agendado_em");
            e.Property(s => s.ConcluidoEm).HasColumnName("concluido_em");
            e.Property(s => s.AguardandoConfirmacaoDesde).HasColumnName("aguardando_confirmacao_desde");
            e.Property(s => s.CriadoEm).HasColumnName("criado_em");
            e.Property(s => s.AtualizadoEm).HasColumnName("atualizado_em");
            e.Property(s => s.DeletadoEm).HasColumnName("deletado_em");

            e.HasQueryFilter(s => s.DeletadoEm == null);

            e.HasOne(s => s.Cliente)
                .WithMany(u => u.ServicosComoCliente)
                .HasForeignKey(s => s.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(s => s.Prestador)
                .WithMany(u => u.ServicosComoPrestador)
                .HasForeignKey(s => s.PrestadorId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(s => s.Categoria)
                .WithMany()
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Cidade)
                .WithMany()
                .HasForeignKey(s => s.CidadeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MensagemServico ────────────────────────────────────────────────────
        modelBuilder.Entity<MensagemServico>(e =>
        {
            e.ToTable("mensagens_servico");

            e.HasIndex(m => new { m.ServicoId, m.CriadoEm });

            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.ServicoId).HasColumnName("servico_id");
            e.Property(m => m.RemetenteId).HasColumnName("remetente_id");
            e.Property(m => m.PapelRemetente).HasColumnName("papel_remetente").HasConversion<string>();
            e.Property(m => m.TipoMensagem).HasColumnName("tipo_mensagem").HasConversion<string>();
            e.Property(m => m.Conteudo).HasColumnName("conteudo");
            e.Property(m => m.ValorProposta).HasColumnName("valor_proposta").HasPrecision(10, 2);
            e.Property(m => m.StatusProposta).HasColumnName("status_proposta").HasConversion<string>();
            e.Property(m => m.ImagemModerada).HasColumnName("imagem_moderada");
            e.Property(m => m.ImagemAprovada).HasColumnName("imagem_aprovada");
            e.Property(m => m.CriadoEm).HasColumnName("criado_em");

            e.HasOne(m => m.Servico)
                .WithMany(s => s.Mensagens)
                .HasForeignKey(m => m.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Remetente)
                .WithMany(u => u.Mensagens)
                .HasForeignKey(m => m.RemetenteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Cobranca ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Cobranca>(e =>
        {
            e.ToTable("cobrancas");

            e.HasIndex(c => c.ServicoId).IsUnique();
            e.HasIndex(c => new { c.Status, c.PixExpiracaoEm });
            e.HasIndex(c => c.PagarmeOrderId).IsUnique();

            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.ServicoId).HasColumnName("servico_id");
            e.Property(c => c.ValorTotal).HasColumnName("valor_total").HasPrecision(10, 2);
            e.Property(c => c.TaxaAdmin).HasColumnName("taxa_admin").HasPrecision(10, 2);
            e.Property(c => c.ValorPrestador).HasColumnName("valor_prestador").HasPrecision(10, 2);
            e.Property(c => c.Status).HasColumnName("status").HasConversion<string>();
            e.Property(c => c.PagarmeOrderId).HasColumnName("pagarme_order_id");
            e.Property(c => c.PagarmePagamentoId).HasColumnName("pagarme_payment_id");
            e.Property(c => c.PixQrCode).HasColumnName("pix_qr_code");
            e.Property(c => c.PixCopiaCola).HasColumnName("pix_copia_cola");
            e.Property(c => c.PixExpiracaoEm).HasColumnName("pix_expira_em");
            e.Property(c => c.PagadoEm).HasColumnName("pago_em");
            e.Property(c => c.RetidoEm).HasColumnName("retido_em");
            e.Property(c => c.LiberadoEm).HasColumnName("liberado_em");
            e.Property(c => c.CriadoEm).HasColumnName("criado_em");
            e.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

            e.HasOne(c => c.Servico)
                .WithOne(s => s.Cobranca)
                .HasForeignKey<Cobranca>(c => c.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Categoria ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("categorias");
            e.HasIndex(c => c.Slug).IsUnique();

            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Nome).HasColumnName("nome");
            e.Property(c => c.Slug).HasColumnName("slug");
            e.Property(c => c.Ativa).HasColumnName("ativo");
            e.Property(c => c.Ordem).HasColumnName("ordem_exibicao");

            // Seed do catálogo canônico de categorias (alinhado ao catbar do frontend)
            e.HasData(
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000001"), Nome = "Reformas e Reparos", Slug = "reformas", Ativa = true, Ordem = 1 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000002"), Nome = "Pintura", Slug = "pintura", Ativa = true, Ordem = 2 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000003"), Nome = "Limpeza", Slug = "limpeza", Ativa = true, Ordem = 3 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000004"), Nome = "Climatização", Slug = "clima", Ativa = true, Ordem = 4 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000005"), Nome = "Jardinagem", Slug = "jardim", Ativa = true, Ordem = 5 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000006"), Nome = "Montagem e Móveis", Slug = "montagem", Ativa = true, Ordem = 6 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000007"), Nome = "Mudança", Slug = "mudanca", Ativa = true, Ordem = 7 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000008"), Nome = "Assistência Técnica", Slug = "assistencia", Ativa = true, Ordem = 8 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-000000000009"), Nome = "Segurança", Slug = "seguranca", Ativa = true, Ordem = 9 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-00000000000a"), Nome = "Serralheria", Slug = "serralheria", Ativa = true, Ordem = 10 },
                new Categoria { Id = new Guid("c0000000-0000-0000-0000-00000000000b"), Nome = "Autos", Slug = "autos", Ativa = true, Ordem = 11 }
            );
        });

        // ── Cidade ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Cidade>(e =>
        {
            e.ToTable("cidades");
            e.HasIndex(c => c.Slug).IsUnique();

            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Nome).HasColumnName("nome");
            e.Property(c => c.Estado).HasColumnName("estado");
            e.Property(c => c.Slug).HasColumnName("slug");
            e.Property(c => c.Ativa).HasColumnName("ativo");

            // Seed de cidades principais cobertas
            e.HasData(
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000001"), Nome = "São Paulo", Estado = "SP", Slug = "sao-paulo", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000002"), Nome = "Rio de Janeiro", Estado = "RJ", Slug = "rio-de-janeiro", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000003"), Nome = "Belo Horizonte", Estado = "MG", Slug = "belo-horizonte", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000004"), Nome = "Curitiba", Estado = "PR", Slug = "curitiba", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000005"), Nome = "Porto Alegre", Estado = "RS", Slug = "porto-alegre", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000006"), Nome = "Brasília", Estado = "DF", Slug = "brasilia", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000007"), Nome = "Salvador", Estado = "BA", Slug = "salvador", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000008"), Nome = "Recife", Estado = "PE", Slug = "recife", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-000000000009"), Nome = "Fortaleza", Estado = "CE", Slug = "fortaleza", Ativa = true },
                new Cidade { Id = new Guid("c1000000-0000-0000-0000-00000000000a"), Nome = "Campinas", Estado = "SP", Slug = "campinas", Ativa = true }
            );
        });

        // ── CategoriaUsuario ───────────────────────────────────────────────────
        modelBuilder.Entity<CategoriaUsuario>(e =>
        {
            e.ToTable("usuarios_categorias");
            e.HasKey(cu => new { cu.UsuarioId, cu.CategoriaId });
            e.HasIndex(cu => cu.CategoriaId); // acelera Any() por categoria na busca de prestadores

            e.Property(cu => cu.UsuarioId).HasColumnName("usuario_id");
            e.Property(cu => cu.CategoriaId).HasColumnName("categoria_id");

            e.HasOne(cu => cu.Usuario)
                .WithMany(u => u.Categorias)
                .HasForeignKey(cu => cu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cu => cu.Categoria)
                .WithMany(c => c.Usuarios)
                .HasForeignKey(cu => cu.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── CidadeUsuario ──────────────────────────────────────────────────────
        modelBuilder.Entity<CidadeUsuario>(e =>
        {
            e.ToTable("usuarios_cidades");
            e.HasKey(cu => new { cu.UsuarioId, cu.CidadeId });
            e.HasIndex(cu => cu.CidadeId); // acelera filtro opcional por cidade na busca de prestadores

            e.Property(cu => cu.UsuarioId).HasColumnName("usuario_id");
            e.Property(cu => cu.CidadeId).HasColumnName("cidade_id");

            e.HasOne(cu => cu.Usuario)
                .WithMany(u => u.Cidades)
                .HasForeignKey(cu => cu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cu => cu.Cidade)
                .WithMany(c => c.Usuarios)
                .HasForeignKey(cu => cu.CidadeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ImagemPortfolio ────────────────────────────────────────────────────
        modelBuilder.Entity<ImagemPortfolio>(e =>
        {
            e.ToTable("imagens_portfolio");

            e.HasIndex(i => new { i.UsuarioId, i.Ordem });

            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.UsuarioId).HasColumnName("usuario_id");
            e.Property(i => i.Url).HasColumnName("url");
            e.Property(i => i.CloudinaryPublicId).HasColumnName("cloudinary_public_id");
            e.Property(i => i.Moderada).HasColumnName("moderado");
            e.Property(i => i.Aprovada).HasColumnName("aprovado");
            e.Property(i => i.Ordem).HasColumnName("ordem_exibicao");
            e.Property(i => i.CriadoEm).HasColumnName("criado_em");
            e.Property(i => i.DeletadoEm).HasColumnName("deletado_em");

            // Soft delete: imagens deletadas são invisíveis por padrão
            e.HasQueryFilter(i => i.DeletadoEm == null);

            e.HasOne(i => i.Usuario)
                .WithMany(u => u.ImagensPortfolio)
                .HasForeignKey(i => i.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Disputa ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Disputa>(e =>
        {
            e.ToTable("disputas");

            e.HasIndex(d => d.ServicoId).IsUnique();
            e.HasIndex(d => d.Status);

            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.ServicoId).HasColumnName("servico_id");
            e.Property(d => d.AbertaPorId).HasColumnName("aberto_por_id");
            e.Property(d => d.Motivo).HasColumnName("motivo");
            e.Property(d => d.Descricao).HasColumnName("descricao");
            e.Property(d => d.Status).HasColumnName("status").HasConversion<string>();
            e.Property(d => d.ResolvidaPorId).HasColumnName("resolvido_por_id");
            e.Property(d => d.DecisaoAdmin).HasColumnName("decisao_admin");
            e.Property(d => d.CriadoEm).HasColumnName("criado_em");
            e.Property(d => d.ResolvidoEm).HasColumnName("resolvido_em");

            e.HasOne(d => d.Servico)
                .WithOne(s => s.Disputa)
                .HasForeignKey<Disputa>(d => d.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.AbertaPor)
                .WithMany()
                .HasForeignKey(d => d.AbertaPorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(d => d.ResolvidaPor)
                .WithMany()
                .HasForeignKey(d => d.ResolvidaPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Notificacao ────────────────────────────────────────────────────────
        modelBuilder.Entity<Notificacao>(e =>
        {
            e.ToTable("notificacoes");

            e.HasIndex(n => new { n.UsuarioId, n.Lida, n.CriadoEm });

            e.Property(n => n.Id).HasColumnName("id");
            e.Property(n => n.UsuarioId).HasColumnName("usuario_id");
            e.Property(n => n.Titulo).HasColumnName("titulo");
            e.Property(n => n.Mensagem).HasColumnName("mensagem");
            e.Property(n => n.Lida).HasColumnName("lido");
            e.Property(n => n.Tipo).HasColumnName("tipo");
            e.Property(n => n.ReferenciaId).HasColumnName("referencia_id");
            e.Property(n => n.CriadoEm).HasColumnName("criado_em");

            e.HasOne(n => n.Usuario)
                .WithMany()
                .HasForeignKey(n => n.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AuditLog ───────────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("logs_auditoria");

            e.HasIndex(a => new { a.UsuarioId, a.CriadoEm });
            e.HasIndex(a => new { a.Entidade, a.EntidadeId });
            e.HasIndex(a => a.CriadoEm);

            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.UsuarioId).HasColumnName("usuario_id");
            e.Property(a => a.Acao).HasColumnName("acao");
            e.Property(a => a.Entidade).HasColumnName("entidade");
            e.Property(a => a.EntidadeId).HasColumnName("entidade_id");
            e.Property(a => a.Ip).HasColumnName("endereco_ip");
            e.Property(a => a.UserAgent).HasColumnName("user_agent");
            e.Property(a => a.Detalhes).HasColumnName("detalhes");
            e.Property(a => a.CriadoEm).HasColumnName("criado_em");

            e.HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Avaliacao ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Avaliacao>(e =>
        {
            e.ToTable("avaliacoes");
            e.HasKey(a => a.Id);

            e.HasIndex(a => new { a.ServicoId, a.AvaliadorId }).IsUnique();
            e.HasIndex(a => a.AvaliadoId);

            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.ServicoId).HasColumnName("service_id");
            e.Property(a => a.AvaliadorId).HasColumnName("reviewer_id");
            e.Property(a => a.AvaliadoId).HasColumnName("reviewed_id");
            e.Property(a => a.Nota).HasColumnName("rating");
            e.Property(a => a.Comentario).HasColumnName("comment");
            e.Property(a => a.CriadoEm).HasColumnName("created_at");

            e.HasOne(a => a.Servico)
                .WithMany()
                .HasForeignKey(a => a.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Avaliador)
                .WithMany()
                .HasForeignKey(a => a.AvaliadorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Avaliado)
                .WithMany()
                .HasForeignKey(a => a.AvaliadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
