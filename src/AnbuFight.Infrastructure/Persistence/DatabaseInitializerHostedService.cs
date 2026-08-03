using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnbuFight.Infrastructure.Persistence;

/// <summary>
/// Runs the migrations and the seed as part of host startup, before the server accepts requests.
/// Registered ahead of the web host service, so a deploy is always a single step.
/// </summary>
public sealed class DatabaseInitializerHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
