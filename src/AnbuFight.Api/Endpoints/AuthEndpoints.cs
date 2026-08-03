using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Auth;
using AnbuFight.Application.Auth.Commands;
using AnbuFight.Application.Auth.Queries;

namespace AnbuFight.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(command, cancellationToken)))
            .AllowAnonymous()
            .WithSummary("Signs in with e-mail and password and returns the token pair.")
            .Produces<AuthenticationResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", async (
                RegisterCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/students/{id}", new CreatedResponse(id));
            })
            .AllowAnonymous()
            .WithSummary("Auto-cadastro do aluno. Nasce pendente de aprovação pela gestão.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/refresh", async (
                RefreshSessionCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(command, cancellationToken)))
            .AllowAnonymous()
            .WithSummary("Exchanges a refresh token for a new token pair (the old one is revoked).")
            .Produces<AuthenticationResult>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", async (
                LogoutCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithSummary("Revokes the given refresh token, or every session when the body is empty.");

        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetCurrentUserQuery(), cancellationToken)))
            .WithSummary("Profile of the signed-in user.")
            .Produces<AuthenticatedUserDto>();

        group.MapPatch("/password", async (
                ChangePasswordCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithSummary("Troca a própria senha e encerra as demais sessões.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/forgot-password", async (
                ForgotPasswordCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return TypedResults.NoContent();
            })
            .AllowAnonymous()
            .WithSummary("Envia o link de redefinição. Responde 204 mesmo se o e-mail não existir.")
            .ProducesValidationProblem();

        group.MapPost("/reset-password", async (
                ResetPasswordCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return TypedResults.NoContent();
            })
            .AllowAnonymous()
            .WithSummary("Consome o token recebido por e-mail e grava a nova senha.")
            .ProducesValidationProblem();

        return app;
    }
}
