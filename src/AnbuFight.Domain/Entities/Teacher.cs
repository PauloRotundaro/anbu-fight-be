using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

public class Teacher : BaseEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateOnly Birthdate { get; set; }

    public required string Email { get; set; }

    public required string PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Teacher;

    public ICollection<Class> Classes { get; } = [];

    /// <summary>Sign-in credential, present only when the teacher has portal access.</summary>
    public User? User { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
