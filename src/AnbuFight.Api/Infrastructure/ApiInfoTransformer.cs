using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AnbuFight.Api.Infrastructure;

/// <summary>
/// Fills the header of the OpenAPI document, so Swagger UI opens with the context a consumer
/// needs before reading a single endpoint.
/// </summary>
public sealed class ApiInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Anbu Fight API",
            Version = "v1",
            Description =
                """
                Gestão da academia de boxe e muay thai Anbu Fight: alunos, professores, planos,
                matrículas e cobranças.

                **Autenticação** — todos os endpoints exigem um token JWT, exceto `POST /api/auth/login`
                e `POST /api/auth/refresh`. Faça login, copie o `accessToken` da resposta e informe-o
                em **Authorize**.

                **Perfis** — `Admin` tem acesso total, `Teacher` lê o cadastro da academia e
                `Student` enxerga apenas os próprios dados.

                **Listagens** são sempre paginadas (`page`, `pageSize`, máximo de 100 itens).

                **Erros** seguem RFC 7807: `400` validação, `401` credenciais, `403` permissão,
                `404` inexistente, `409` conflito de regra de negócio.
                """
        };

        return Task.CompletedTask;
    }
}
