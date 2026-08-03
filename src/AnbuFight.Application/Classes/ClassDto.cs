using System.Linq.Expressions;

namespace AnbuFight.Application.Classes;

/// <param name="DaysOfWeek">Dias em que a aula ocorre: 0 = domingo … 6 = sábado.</param>
/// <param name="StartTime">Horário local da academia, no formato <c>HH:mm</c>.</param>
/// <param name="Capacity">Nulo significa sem limite de vagas.</param>
public sealed record ClassDto(
    Guid Id,
    string Name,
    Modality Modality,
    IReadOnlyList<int> DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid TeacherId,
    string TeacherName,
    int? Capacity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static Expression<Func<Class, ClassDto>> Projection { get; } = gymClass => new ClassDto(
        gymClass.Id,
        gymClass.Name,
        gymClass.Modality,
        gymClass.DaysOfWeek,
        gymClass.StartTime,
        gymClass.EndTime,
        gymClass.TeacherId,
        gymClass.Teacher.FirstName + " " + gymClass.Teacher.LastName,
        gymClass.Capacity,
        gymClass.IsActive,
        gymClass.CreatedAt,
        gymClass.UpdatedAt);
}
