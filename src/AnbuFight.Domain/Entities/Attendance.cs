using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

/// <summary>Presença de um aluno numa sessão de aula.</summary>
public class Attendance : BaseEntity
{
    public Guid StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public Guid ClassSessionId { get; set; }

    public ClassSession ClassSession { get; set; } = null!;

    public DateTimeOffset CheckedInAt { get; set; }

    public AttendanceOrigin RegisteredBy { get; set; }
}
