namespace AnbuFight.Domain.Enums;

/// <summary>Access profile carried by the JWT and used by the authorization policies.</summary>
public enum UserRole
{
    /// <summary>Full access to every resource.</summary>
    Admin = 1,

    /// <summary>Reads students, plans and enrollments; manages its own profile.</summary>
    Teacher = 2,

    /// <summary>Reads only its own data.</summary>
    Student = 3
}
