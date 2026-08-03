using System.Text.Json.Serialization;
using AnbuFight.Api.Extensions;
using AnbuFight.Api.Infrastructure;
using AnbuFight.Application;
using AnbuFight.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddConfiguredCors(builder.Configuration, builder.Environment);

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

app.UseExceptionHandler();

app.UseCors(CorsSetup.PolicyName);

if (app.Environment.IsDevelopment())
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

app.MapApiEndpoints();

// Pending migrations and the administrator seed are applied by DatabaseInitializerHostedService
// during host startup, so a deploy never needs a manual database step.
await app.RunAsync();

/// <summary>Exposed so the integration tests can boot the real application.</summary>
public partial class Program;
