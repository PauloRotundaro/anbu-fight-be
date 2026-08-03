using System.Text.Json.Serialization;
using AnbuFight.Api.Extensions;
using AnbuFight.Api.Infrastructure;
using AnbuFight.Application;
using AnbuFight.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Provedores de hospedagem (Railway, Render, Fly) escolhem a porta e a informam em PORT.
if (builder.Configuration["PORT"] is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddConfiguredCors(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();

// A TLS termina no proxy do provedor: sem isto a aplicação enxergaria todo tráfego como http.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // O proxy fica numa rede interna de IP desconhecido; a lista padrão o rejeitaria.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// RFC 7807 responses for every failure, including the ones raised by the framework itself.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Enums travel as names ("Monthly"), not as numbers: readable and stable for clients.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new HourMinuteTimeOnlyConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiInfoTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();

app.UseCors(CorsSetup.PolicyName);

// Em desenvolvimento a documentação vem ligada; fora dele, só quando Docs:Enabled for true —
// assim dá para abrir o Swagger num ambiente publicado e fechar depois, sem novo deploy.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Docs:Enabled"))
{
    // Anonymous: the fallback policy would otherwise lock the API explorer behind the token it exists to obtain.
    app.MapOpenApi().AllowAnonymous();

    // Swagger UI is middleware serving static assets, so it is registered before the auth
    // middleware — otherwise the fallback policy would reject the page itself.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Anbu Fight API v1");
        options.DocumentTitle = "Anbu Fight API";
        options.DisplayRequestDuration();
    });

    app.MapScalarApiReference(options => options.WithTitle("Anbu Fight API")).AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

// Anônimo e sem tocar o banco: as migrations rodam antes do servidor aceitar requisições, então
// responder aqui já significa que a aplicação subiu inteira.
app.MapHealthChecks("/health").AllowAnonymous();

app.MapApiEndpoints();

// Pending migrations and the administrator seed are applied by DatabaseInitializerHostedService
// during host startup, so a deploy never needs a manual database step.
await app.RunAsync();

/// <summary>Exposed so the integration tests can boot the real application.</summary>
public partial class Program;
