using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prontto.Domain.Entities;
using Prontto.Domain.Enums;
using Prontto.Domain.Interfaces;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Job SY-02: cancela cobranças com PIX expirado a cada 15 minutos.
/// </summary>
public class JobExpiracaoPix(IServiceScopeFactory scopeFactory, ILogger<JobExpiracaoPix> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var repositorioCobrancas = scope.ServiceProvider.GetRequiredService<IRepositorioCobranca>();
                var repositorioServicos = scope.ServiceProvider.GetRequiredService<IRepositorioServico>();
                var repositorioAuditLog = scope.ServiceProvider.GetRequiredService<IRepositorioAuditLog>();
                var repositorioNotificacoes = scope.ServiceProvider.GetRequiredService<IRepositorioNotificacao>();

                var expiradas = await repositorioCobrancas.ListarPendentesExpiradosAsync();

                foreach (var cobranca in expiradas)
                {
                    cobranca.Status = StatusCobranca.Cancelado;
                    cobranca.AtualizadoEm = DateTime.UtcNow;
                    await repositorioCobrancas.AtualizarAsync(cobranca);

                    var servico = await repositorioServicos.ObterPorIdAsync(cobranca.ServicoId);
                    if (servico != null && servico.Status == StatusServico.AguardandoPagamento)
                    {
                        servico.Status = StatusServico.Cancelado;
                        servico.AtualizadoEm = DateTime.UtcNow;
                        await repositorioServicos.AtualizarAsync(servico);

                        if (servico.ClienteId.HasValue)
                            await repositorioNotificacoes.AdicionarAsync(new Notificacao
                            {
                                UsuarioId = servico.ClienteId.Value,
                                Titulo = "PIX expirado",
                                Mensagem = $"O prazo de pagamento do serviço '{servico.Titulo}' expirou. Você pode solicitar novamente.",
                                Tipo = "pagamento",
                                ReferenciaId = servico.Id.ToString()
                            });
                    }

                    await repositorioAuditLog.RegistrarAsync(new AuditLog
                    {
                        UsuarioId = null,
                        Acao = "job.pix.expirado",
                        Entidade = "Cobranca",
                        EntidadeId = cobranca.Id.ToString(),
                        Detalhes = $"{{\"servicoId\":\"{cobranca.ServicoId}\"}}"
                    });

                    logger.LogInformation("PIX expirado cancelado: CobrancaId={CobrancaId}", cobranca.Id);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Erro no JobExpiracaoPix");
            }
        }
    }
}
