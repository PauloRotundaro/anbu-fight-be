using System.Reflection;
using AnbuFight.Application.Attendances;
using AnbuFight.Application.ClassSessions;
using AnbuFight.Application.ClassSessions.Queries;
using AnbuFight.Application.Common.Behaviours;
using Microsoft.Extensions.DependencyInjection;

namespace AnbuFight.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Regras de negócio compartilhadas por vários casos de uso.
        services.AddScoped<CheckInPolicy>();
        services.AddScoped<ClassSessionScheduler>();
        services.AddScoped<ClassSessionReader>();

        return services;
    }
}
