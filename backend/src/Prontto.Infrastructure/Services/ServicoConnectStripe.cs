using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prontto.Application.Common;
using Prontto.Application.Financeiro;
using Prontto.Domain.Interfaces;
using Stripe;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Onboarding/status do prestador no Stripe Connect (contas Express).
/// A conta conectada é o destino do split (transfer_data.destination) no pagamento.
/// </summary>
public class ServicoConnectStripe(
    IRepositorioUsuario repositorioUsuarios,
    IConfiguration configuracao,
    ILogger<ServicoConnectStripe> logger) : IServicoConnect
{
    private StripeClient Cliente()
    {
        var chave = configuracao["STRIPE_SECRET_KEY"]
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY não configurado.");
        return new StripeClient(chave);
    }

    private string AppUrl => (configuracao["APP_URL"] ?? "https://prontto.org").TrimEnd('/');

    public async Task<ResultadoOnboarding> CriarOnboardingAsync(Guid prestadorId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(prestadorId)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado.");

        var cliente = Cliente();
        var contas = new AccountService(cliente);

        // Cria a conta conectada Express se ainda não existir.
        if (string.IsNullOrWhiteSpace(usuario.StripeAccountId))
        {
            var conta = await contas.CreateAsync(new AccountCreateOptions
            {
                Type = "express",
                Country = "BR",
                Email = usuario.Email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    // Nesta conta, a capability transfers exige card_payments junto — pedimos as duas.
                    // Usamos transfers para o repasse dos 80% (destination charge); card_payments fica
                    // habilitada por exigência do modelo da conta (não processamos cobrança no prestador).
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                },
                BusinessType = "individual",
            });

            usuario.StripeAccountId = conta.Id;
            await repositorioUsuarios.AtualizarAsync(usuario);
            logger.LogInformation("Stripe Connect: conta criada {AccountId} para prestador {PrestadorId}", conta.Id, prestadorId);
        }

        var links = new AccountLinkService(cliente);
        var link = await links.CreateAsync(new AccountLinkCreateOptions
        {
            Account = usuario.StripeAccountId,
            RefreshUrl = $"{AppUrl}/minha-area?stripe=refresh",
            ReturnUrl = $"{AppUrl}/minha-area?stripe=ok",
            Type = "account_onboarding",
        });

        return new ResultadoOnboarding(link.Url);
    }

    public async Task<StatusConnect> ObterStatusAsync(Guid prestadorId)
    {
        var usuario = await repositorioUsuarios.ObterPorIdAsync(prestadorId)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado.");

        if (string.IsNullOrWhiteSpace(usuario.StripeAccountId))
            return new StatusConnect(false, false, false, false);

        var conta = await new AccountService(Cliente()).GetAsync(usuario.StripeAccountId);
        return new StatusConnect(
            Possui: true,
            ChargesEnabled: conta.ChargesEnabled,
            DetailsSubmitted: conta.DetailsSubmitted,
            PayoutsEnabled: conta.PayoutsEnabled);
    }
}
