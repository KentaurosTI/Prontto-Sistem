namespace Prontto.Domain.Enums;

public enum StatusCobranca
{
    Pendente,
    Pago,
    Retido,
    Liberado,
    Reembolsado,
    Cancelado,
    /// <summary>Reembolso solicitado mas ainda não confirmado pelo gateway (todas as tentativas falharam) — SCRUM-52.</summary>
    ReembolsoPendente
}
