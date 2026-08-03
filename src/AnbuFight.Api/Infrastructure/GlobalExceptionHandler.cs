using AnbuFight.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AnbuFight.Api.Infrastructure;

/// <summary>
/// Translates application exceptions into RFC 7807 responses. Handlers throw meaningful exceptions
/// and never deal with status codes, and no unexpected error can leak internals to the client.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = Describe(exception);

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails Describe(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors.ToDictionary(error => error.Key, error => error.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        },

        NotFoundException notFound => Problem(
            StatusCodes.Status404NotFound, "Resource not found.", notFound.Message,
            "https://tools.ietf.org/html/rfc9110#section-15.5.5"),

        ConflictException conflict => Problem(
            StatusCodes.Status409Conflict, "Request conflicts with the current state.", conflict.Message,
            "https://tools.ietf.org/html/rfc9110#section-15.5.10"),

        ForbiddenAccessException forbidden => Problem(
            StatusCodes.Status403Forbidden, "Forbidden.", forbidden.Message,
            "https://tools.ietf.org/html/rfc9110#section-15.5.4"),

        AuthenticationFailedException authentication => Problem(
            StatusCodes.Status401Unauthorized, "Unauthorized.", authentication.Message,
            "https://tools.ietf.org/html/rfc9110#section-15.5.2"),

        // Anything unexpected is reported without details; the stack trace goes to the logs only.
        _ => Problem(
            StatusCodes.Status500InternalServerError, "An unexpected error occurred.",
            "The request could not be processed. Please try again later.",
            "https://tools.ietf.org/html/rfc9110#section-15.6.1")
    };

    private static ProblemDetails Problem(int status, string title, string detail, string type) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = type
    };
}
