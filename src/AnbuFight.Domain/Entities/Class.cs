using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

/// <summary>
/// Aula recorrente da grade semanal. É uma regra ("Muay Thai, segundas e quartas, 19h"),
/// não uma ocorrência — a ocorrência concreta é <see cref="ClassSession"/>.
/// </summary>
public class Class : BaseEntity
{
    public required string Name { get; set; }

    public Modality Modality { get; set; }

    /// <summary>Dias em que a aula acontece, no padrão de <see cref="System.DayOfWeek"/> (0 = domingo).</summary>
    public List<int> DaysOfWeek { get; set; } = [];

    /// <summary>Horário local da academia.</summary>
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public Guid TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    /// <summary>Nulo significa sem limite de vagas.</summary>
    public int? Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ClassSession> Sessions { get; } = [];

    /// <summary>Se a aula ocorre no dia da semana informado.</summary>
    public bool OccursOn(DayOfWeek dayOfWeek) => DaysOfWeek.Contains((int)dayOfWeek);

    /// <summary>Se dois horários se sobrepõem — usado para impedir choque na agenda do professor.</summary>
    public bool OverlapsWith(TimeOnly otherStart, TimeOnly otherEnd, IEnumerable<int> otherDays) =>
        StartTime < otherEnd && otherStart < EndTime && otherDays.Any(DaysOfWeek.Contains);
}
