using AnbuFight.Domain.Enums;

namespace AnbuFight.Application.Common.Interfaces;

/// <summary>Identity of the caller, read from the JWT of the current request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    UserRole? Role { get; }

    /// <summary>Set when the credential is linked to a student record.</summary>
    Guid? StudentId { get; }

    /// <summary>Set when the credential is linked to a teacher record.</summary>
    Guid? TeacherId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin => Role == UserRole.Admin;

    bool IsTeacher => Role == UserRole.Teacher;

    bool IsStudent => Role == UserRole.Student;
}
