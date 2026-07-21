using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prontto.Application.Admin;
using Prontto.Application.Auth;
using Prontto.Application.Avaliacoes;
using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Application.Perfil;
using Prontto.Application.Servicos;
using Prontto.Domain.Interfaces;
using Prontto.Infrastructure.Persistence.Context;
using Prontto.Infrastructure.Persistence.Repositories;
using Prontto.Infrastructure.Services;

namespace Prontto.Infrastructure.DependencyInjection;

public static class InjecaoDependencias
{
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos, IConfiguration configuracao, IHostEnvironment env)
    {
        var connectionString = configuracao.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");

        servicos.AddDbContext<ContextoBancoDados>(opt =>
            opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // ── Repositórios ──────────────────────────────────────────────────────
        servicos.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
        servicos.AddScoped<IRepositorioServico, RepositorioServico>();
        servicos.AddScoped<IRepositorioCobranca, RepositorioCobranca>();
        servicos.AddScoped<IRepositorioBanking, RepositorioBanking>();
        servicos.AddScoped<IRepositorioMensagem, RepositorioMensagem>();
        servicos.AddScoped<IRepositorioRefreshToken, RepositorioRefreshToken>();
        servicos.AddScoped<IRepositorioCategoria, RepositorioCategoria>();
        servicos.AddScoped<IRepositorioCidade, RepositorioCidade>();
        servicos.AddScoped<IRepositorioPerfilPrestador, RepositorioPerfilPrestador>();
        servicos.AddScoped<IRepositorioDisputa, RepositorioDisputa>();
        servicos.AddScoped<IRepositorioNotificacao, RepositorioNotificacao>();
        servicos.AddScoped<IRepositorioAuditLog, RepositorioAuditLog>();
        servicos.AddScoped<IRepositorioAvaliacao, RepositorioAvaliacao>();
        servicos.AddScoped<IRepositorioImagemPortfolio, RepositorioImagemPortfolio>();

        // ── Armazenamento de arquivos ─────────────────────────────────────────
        servicos.AddScoped<IArmazenamentoArquivo, ArmazenamentoArquivoLocal>();

        // ── Serviços de Aplicação ─────────────────────────────────────────────
        servicos.AddScoped<IHashSenha, HashSenhaBcrypt>();
        servicos.AddScoped<IServicoJwt, ServicoJwt>();
        servicos.AddScoped<IServicoAutenticacao, ServicoAutenticacao>();
        servicos.AddScoped<IServicoAdmin, ServicoAdmin>();
        servicos.AddScoped<IServicoPerfilPrestador, ServicoPerfilPrestador>();
        servicos.AddScoped<IServicoServico, ServicoServico>();
        servicos.AddScoped<IServicoNegociacao, ServicoNegociacao>();
        servicos.AddScoped<IServicoDisputa, ServicoDisputa>();
        servicos.AddScoped<IServicoFinanceiro, ServicoFinanceiro>();
        servicos.AddScoped<IServicoAvaliacao, ServicoAvaliacao>();

        // ── Gateway de pagamento ──────────────────────────────────────────────
        // Em desenvolvimento usa stub local; em produção usa Pagar.me real
        if (env.IsDevelopment())
        {
            servicos.AddScoped<IProcessadorPagamento, ProcessadorPagamentoStub>();
        }
        else
        {
            servicos.AddHttpClient("pagarme", c =>
            {
                c.BaseAddress = new Uri("https://api.pagar.me/core/v5/");
                c.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });
            servicos.AddScoped<IProcessadorPagamento, ProcessadorPagamentoPagarme>();
        }

        // ── Jobs ──────────────────────────────────────────────────────────────
        servicos.AddHostedService<JobConclusaoAutomatica>();
        servicos.AddHostedService<JobExpiracaoPix>();

        return servicos;
    }
}
