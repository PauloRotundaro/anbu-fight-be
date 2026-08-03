using Microsoft.EntityFrameworkCore.Design;

namespace AnbuFight.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tooling (<c>dotnet ef migrations …</c>). Keeping it here means
/// generating a migration never needs the API host or a reachable database.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string ConnectionStringVariable = "ANBUFIGHT_CONNECTION_STRING";

    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5433;Database=anbufight;Username=anbufight;Password=anbufight";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(
                typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(options);
    }
}
