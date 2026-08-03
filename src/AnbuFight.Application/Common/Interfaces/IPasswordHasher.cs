namespace AnbuFight.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Constant-time verification; returns false for malformed hashes instead of throwing.</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// A well-formed hash that no password matches. Verifying against it on a failed lookup keeps the
    /// login response time flat, so timing cannot reveal which e-mails are registered.
    /// </summary>
    string DecoyHash { get; }
}
