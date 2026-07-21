using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prontto.Application.Servicos;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Job agendado — auto-conclusão de serviços em AguardandoConfirmacaoCliente há mais de 7 dias.
/// Executa a cada 1 hora (ADR-05).
/// Usa IServiceScopeFactory para obter DbContext com lifetime correto em background service.
/// </summary>
public class JobConclusaoAutomatica(
    IServiceScopeFactory scopeFactory,
    ILogger<JobConclusaoAutomatica> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("JobConclusaoAutomatica iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var servicoServico = scope.ServiceProvider.GetRequiredService<IServicoServico>();
                await servicoServico.AutoConcluirServicosAsync();
                logger.LogInformation("JobConclusaoAutomatica executado com sucesso");
            }
            catch (OperationCanceledException)
            {
                // Cancelamento normal — não logar como erro
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no JobConclusaoAutomatica");
            }
        }

        logger.LogInformation("JobConclusaoAutomatica encerrado");
    }
}
