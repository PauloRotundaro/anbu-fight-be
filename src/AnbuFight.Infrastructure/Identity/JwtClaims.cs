namespace AnbuFight.Infrastructure.Identity;

/// <summary>
/// Short claim names, written and read in one place. Short names keep the token small,
/// which matters because it travels on every request.
/// </summary>
public static class JwtClaims
{
    public const string Role = "role";

    public const string StudentId = "student_id";

    public const string TeacherId = "teacher_id";
}
