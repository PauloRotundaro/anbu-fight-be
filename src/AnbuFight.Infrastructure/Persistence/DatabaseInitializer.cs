using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnbuFight.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and makes sure an administrator exists, so a fresh environment
/// is usable right after startup. Both steps are idempotent.
/// </summary>
public sealed class DatabaseInitializer(
    ApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<DatabaseSeedOptions> seedOptions,
    ILogger<DatabaseInitializer> logger)
{
    private readonly DatabaseSeedOptions _options = seedOptions.Value;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Database migrations applied.");

        await SeedAdministratorAsync(cancellationToken);
    }

    private async Task SeedAdministratorAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            logger.LogWarning(
                "Administrator seed skipped: no password configured in \"{Section}:AdminPassword\".",
                DatabaseSeedOptions.SectionName);

            return;
        }

        var email = _options.AdminEmail.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return;
        }

        context.Users.Add(new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(_options.AdminPassword),
            Role = UserRole.Admin
        });

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrator {Email} created.", email);
    }
}
