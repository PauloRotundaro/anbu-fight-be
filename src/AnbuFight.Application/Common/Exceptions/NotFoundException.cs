namespace AnbuFight.Application.Common.Exceptions;

/// <summary>Maps to HTTP 404 in the global exception handler.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"\"{name}\" ({key}) was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
