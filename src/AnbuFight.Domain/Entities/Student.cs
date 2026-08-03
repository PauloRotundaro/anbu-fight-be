using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

public class Student : BaseEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateOnly Birthdate { get; set; }

    public required string Email { get; set; }

    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Situação do cadastro. Alunos criados pela gestão já nascem <c>Active</c>;
    /// o auto-cadastro nasce <c>PendingApproval</c>.
    /// </summary>
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public string? EmergencyContact { get; set; }

    public string? EmergencyPhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Student;

    public ICollection<StudentPlan> StudentPlans { get; } = [];

    public ICollection<Payment> Payments { get; } = [];

    public ICollection<Attendance> Attendances { get; } = [];

    /// <summary>Sign-in credential, present only when the student has portal access.</summary>
    public User? User { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Atalho de leitura: só quem está aprovado e frequentando é "ativo".</summary>
    public bool IsActive => Status == StudentStatus.Active;

    public void Approve() => Status = StudentStatus.Active;
}
