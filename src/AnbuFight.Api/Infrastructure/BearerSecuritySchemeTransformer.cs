using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AnbuFight.Api.Infrastructure;

/// <summary>
/// Declares the bearer scheme in the OpenAPI document so the API explorer can send
/// an Authorization header, instead of every protected endpoint answering 401.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token returned by POST /api/auth/login."
        };

        return Task.CompletedTask;
    }
}
