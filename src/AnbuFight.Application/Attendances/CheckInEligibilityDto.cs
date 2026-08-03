namespace AnbuFight.Application.Attendances;

/// <param name="Reason">Motivo do bloqueio, ou <c>Ok</c> quando liberado.</param>
/// <param name="HasOverdueDebt">Verdadeiro sempre que existir cobrança vencida, mesmo dentro da tolerância.</param>
/// <param name="DaysOverdue">Atraso da cobrança vencida mais antiga; 0 quando não há débito.</param>
/// <param name="GraceDays">Tolerância configurada na academia, em dias.</param>
/// <param name="GraceDaysRemaining">Quantos dias ainda restam antes do bloqueio.</param>
public sealed record CheckInEligibilityDto(
    Guid StudentId,
    bool CanCheckIn,
    CheckInBlockReason Reason,
    bool HasOverdueDebt,
    int DaysOverdue,
    decimal OverdueAmount,
    int OverdueCount,
    int GraceDays,
    int GraceDaysRemaining);
