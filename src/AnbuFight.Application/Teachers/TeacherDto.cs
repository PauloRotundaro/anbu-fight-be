using System.Linq.Expressions;

namespace AnbuFight.Application.Teachers;

/// <param name="Role">Perfil de acesso vinculado ao professor.</param>
/// <param name="HasPortalAccess">Se existe uma credencial de login para este professor.</param>
public sealed record TeacherDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    UserRole Role,
    bool HasPortalAccess,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static Expression<Func<Teacher, TeacherDto>> Projection { get; } = teacher => new TeacherDto(
        teacher.Id,
        teacher.FirstName,
        teacher.LastName,
        teacher.FirstName + " " + teacher.LastName,
        teacher.Birthdate,
        teacher.Email,
        teacher.PhoneNumber,
        teacher.Role,
        teacher.User != null,
        teacher.CreatedAt,
        teacher.UpdatedAt);
}
