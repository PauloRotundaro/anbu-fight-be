namespace AnbuFight.Application.Common.Exceptions;

/// <summary>
/// The caller is authenticated but may not touch this particular record
/// (e.g. a student reading another student's payments). Maps to HTTP 403.
/// </summary>
public sealed class ForbiddenAccessException(string message = "You are not allowed to access this resource.")
    : Exception(message);
