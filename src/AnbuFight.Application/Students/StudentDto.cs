using System.Linq.Expressions;

namespace AnbuFight.Application.Students;

/// <param name="Status">Situação do cadastro: pendente de aprovação, ativo ou inativo.</param>
/// <param name="IsActive">Atalho derivado de <c>Status</c>: verdadeiro apenas quando ativo.</param>
/// <param name="Role">Perfil de acesso vinculado ao aluno.</param>
/// <param name="HasPortalAccess">Se existe uma credencial de login para este aluno.</param>
public sealed record StudentDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    StudentStatus Status,
    bool IsActive,
    string? EmergencyContact,
    string? EmergencyPhoneNumber,
    UserRole Role,
    bool HasPortalAccess,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Projected in SQL: the database returns exactly these columns, no entity is ever materialized
    /// and no change tracking snapshot is taken.
    /// </summary>
    public static Expression<Func<Student, StudentDto>> Projection { get; } = student => new StudentDto(
        student.Id,
        student.FirstName,
        student.LastName,
        student.FirstName + " " + student.LastName,
        student.Birthdate,
        student.Email,
        student.PhoneNumber,
        student.Status,
        student.Status == StudentStatus.Active,
        student.EmergencyContact,
        student.EmergencyPhoneNumber,
        student.Role,
        student.User != null,
        student.CreatedAt,
        student.UpdatedAt);
}
