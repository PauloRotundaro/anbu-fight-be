namespace AnbuFight.Application.Common.Exceptions;

/// <summary>Business rule violated by the current state of the data. Maps to HTTP 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
