namespace AnbuFight.Application.Common.Exceptions;

/// <summary>
/// Wrong credentials, disabled account or an unusable refresh token. Maps to HTTP 401 and always
/// carries the same message, so it cannot be used to probe which e-mails exist.
/// </summary>
public sealed class AuthenticationFailedException(string message = "Invalid credentials.") : Exception(message);
