namespace AnbuFight.Api.Infrastructure;

/// <summary>Body returned by every create endpoint.</summary>
public sealed record CreatedResponse(Guid Id);
