using AnbuFight.Domain.Common;

namespace AnbuFight.Domain.Entities;

/// <summary>
/// Ocorrência concreta de uma aula num dia específico. É o alvo do check-in e o que a gestão
/// cancela num feriado, sem mexer na grade.
/// </summary>
public class ClassSession : BaseEntity
{
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;

    /// <summary>Data local da academia.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Instante absoluto de início, já resolvido no fuso da academia.</summary>
    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public bool IsCancelled { get; set; }

    public string? CancellationReason { get; set; }

    public ICollection<Attendance> Attendances { get; } = [];

    public void Cancel(string? reason)
    {
        IsCancelled = true;
        CancellationReason = reason;
    }

    /// <summary>
    /// Janela em que o check-in é aceito: de <paramref name="minutesBefore"/> antes do início
    /// até o fim da aula.
    /// </summary>
    public bool IsWithinCheckInWindow(DateTimeOffset now, int minutesBefore) =>
        now >= StartsAt.AddMinutes(-minutesBefore) && now <= EndsAt;
}
