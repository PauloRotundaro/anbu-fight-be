using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace AnbuFight.Api.IntegrationTests.Common;

/// <summary>
/// Boots the real application against a throwaway PostgreSQL container. Nothing is stubbed:
/// the tests exercise the actual HTTP pipeline, JWT validation, EF Core and the migrations.
/// </summary>
public sealed class AnbuFightApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@anbufight.com";
    public const string AdminPassword = "Anbu@Fight123";

    private const string SigningKey = "integration-tests-signing-key-with-32-chars-minimum";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("anbufight_tests")
        .WithUsername("anbufight")
        .WithPassword("anbufight")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Environment variables, not ConfigureAppConfiguration: the application reads its settings while
        // registering services, which happens before the factory's configuration callbacks would run.
        SetSetting("ConnectionStrings__Default", _database.GetConnectionString());
        SetSetting("Jwt__Issuer", "AnbuFight");
        SetSetting("Jwt__Audience", "AnbuFight");
        SetSetting("Jwt__SigningKey", SigningKey);
        SetSetting("Seed__Enabled", "true");
        SetSetting("Seed__AdminEmail", AdminEmail);
        SetSetting("Seed__AdminPassword", AdminPassword);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    private static void SetSetting(string key, string value) =>
        Environment.SetEnvironmentVariable(key, value);
}

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<AnbuFightApiFactory>;
