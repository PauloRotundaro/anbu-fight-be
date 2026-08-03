using System.Linq.Expressions;

namespace AnbuFight.Application.Attendances;

/// <param name="RegisteredBy">Quem registrou: o próprio aluno (check-in) ou a gestão (lançamento manual).</param>
public sealed record AttendanceDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ClassSessionId,
    Guid ClassId,
    string ClassName,
    Modality Modality,
    DateOnly SessionDate,
    DateTimeOffset CheckedInAt,
    AttendanceOrigin RegisteredBy);

public static class AttendanceProjections
{
    public static Expression<Func<Attendance, AttendanceDto>> ToDto { get; } = attendance => new AttendanceDto(
        attendance.Id,
        attendance.StudentId,
        attendance.Student.FirstName + " " + attendance.Student.LastName,
        attendance.ClassSessionId,
        attendance.ClassSession.ClassId,
        attendance.ClassSession.Class.Name,
        attendance.ClassSession.Class.Modality,
        attendance.ClassSession.Date,
        attendance.CheckedInAt,
        attendance.RegisteredBy);
}
