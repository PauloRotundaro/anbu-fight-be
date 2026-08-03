using System.Text;
using AnbuFight.Application.Common.Options;
using AnbuFight.Application.Common.Security;
using AnbuFight.Infrastructure.Identity;
using AnbuFight.Infrastructure.Persistence;
using AnbuFight.Infrastructure.Persistence.Interceptors;
using AnbuFight.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AnbuFight.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.Configure<GymOptions>(configuration.GetSection(GymOptions.SectionName));
        services.AddSingleton<IGymClock, GymClock>();

        // Sem provedor de e-mail configurado, a mensagem vai para o log — o fluxo de recuperação
        // de senha funciona de ponta a ponta em desenvolvimento.
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.AddPersistence(configuration);
        services.AddSecurity(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string \"Default\" is not configured.");

        services.Configure<DatabaseSeedOptions>(configuration.GetSection(DatabaseSeedOptions.SectionName));

        // Singleton because it only depends on TimeProvider — required by the pooled context below.
        services.AddSingleton<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        // Pooling reuses the context instances and their compiled query caches across requests,
        // which removes most of the per-request setup cost under load.
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<ISaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<DatabaseInitializer>();
        services.AddHostedService<DatabaseInitializerHostedService>();
    }

    private static void AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = JwtClaims.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(Policies.Staff, policy =>
                policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Teacher)));
    }
}
