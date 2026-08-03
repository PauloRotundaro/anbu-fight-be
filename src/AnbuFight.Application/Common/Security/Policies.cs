namespace AnbuFight.Application.Common.Security;

/// <summary>
/// Authorization policy names shared by the endpoints and the policy registration,
/// so a typo cannot silently leave an endpoint unprotected.
/// </summary>
public static class Policies
{
    /// <summary>Administration: full access.</summary>
    public const string Admin = nameof(Admin);

    /// <summary>Gym staff: administrators and teachers.</summary>
    public const string Staff = nameof(Staff);
}
