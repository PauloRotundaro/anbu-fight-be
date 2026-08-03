namespace AnbuFight.Infrastructure.Persistence;

public sealed class DatabaseSeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>When false, the startup only applies migrations.</summary>
    public bool Enabled { get; set; } = true;

    public string AdminEmail { get; set; } = "admin@anbufight.com";

    /// <summary>Empty means "do not create the administrator" — the seed is then skipped with a warning.</summary>
    public string AdminPassword { get; set; } = string.Empty;
}
