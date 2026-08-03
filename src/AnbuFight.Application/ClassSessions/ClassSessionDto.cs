using System.Linq.Expressions;

namespace AnbuFight.Application.ClassSessions;

/// <param name="AttendanceCount">Quantos alunos já bateram presença.</param>
/// <param name="AlreadyCheckedIn">Se o aluno que fez a requisição já marcou presença nesta sessão.</param>
/// <param name="CanCheckIn">
/// Se o aluno que fez a requisição pode marcar presença agora. Falso para quem não é aluno.
/// </param>
/// <param name="CheckInUnavailableReason">Texto pronto para exibir quando <c>canCheckIn</c> é falso.</param>
public sealed record ClassSessionDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Modality Modality,
    Guid TeacherId,
    string TeacherName,
    DateOnly Date,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? Capacity,
    int AttendanceCount,
    bool IsCancelled,
    string? CancellationReason,
    bool AlreadyCheckedIn,
    bool CanCheckIn,
    string? CheckInUnavailableReason)
{
    /// <summary>
    /// <paramref name="studentId"/> entra como parâmetro da consulta para que "já fiz check-in"
    /// venha resolvido pelo banco, sem uma segunda ida ao servidor.
    /// </summary>
    public static Expression<Func<ClassSession, ClassSessionDto>> Projection(Guid? studentId) =>
        session => new ClassSessionDto(
            session.Id,
            session.ClassId,
            session.Class.Name,
            session.Class.Modality,
            session.Class.TeacherId,
            session.Class.Teacher.FirstName + " " + session.Class.Teacher.LastName,
            session.Date,
            session.StartsAt,
            session.EndsAt,
            session.Class.Capacity,
            session.Attendances.Count,
            session.IsCancelled,
            session.CancellationReason,
            studentId != null && session.Attendances.Any(attendance => attendance.StudentId == studentId),
            false,
            null);
}
