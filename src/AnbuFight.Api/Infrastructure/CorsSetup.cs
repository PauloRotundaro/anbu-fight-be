namespace AnbuFight.Api.Infrastructure;

/// <summary>
/// CORS configurável. Com o frontend usando BFF, as chamadas saem do servidor e não passam pelo
/// navegador — mas isso continua necessário para consumir a API direto do Swagger ou de outras
/// ferramentas, e para um eventual cliente que chame a API do browser.
/// </summary>
public static class CorsSetup
{
    public const string PolicyName = "AnbuFight";

    private const string ConfigurationKey = "Cors:AllowedOrigins";

    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection(ConfigurationKey).Get<string[]>() ?? [];

        return services.AddCors(options => options.AddPolicy(PolicyName, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();

                return;
            }

            // Sem lista configurada: liberado em desenvolvimento, fechado em qualquer outro ambiente.
            if (environment.IsDevelopment())
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
        }));
    }
}
