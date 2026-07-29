namespace Prontto.Application.Financeiro;

/// <summary>Link de onboarding do prestador no Stripe Connect.</summary>
public record ResultadoOnboarding(string Url);

/// <summary>Situação da conta conectada do prestador no Stripe.</summary>
public record StatusConnect(bool Possui, bool ChargesEnabled, bool DetailsSubmitted, bool PayoutsEnabled);

/// <summary>
/// Onboarding e status do prestador no Stripe Connect (necessário para receber o repasse do split).
/// </summary>
public interface IServicoConnect
{
    /// <summary>Cria (se necessário) a conta conectada do prestador e retorna o link de onboarding.</summary>
    Task<ResultadoOnboarding> CriarOnboardingAsync(Guid prestadorId);

    /// <summary>Retorna a situação da conta conectada do prestador.</summary>
    Task<StatusConnect> ObterStatusAsync(Guid prestadorId);
}
