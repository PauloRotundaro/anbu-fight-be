namespace AnbuFight.Domain.Common;

/// <summary>
/// Base class for every persisted entity. Concentrates identity, auditing and soft delete
/// so those concerns are handled once, in the persistence interceptor and query filters.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Sequential (v7) GUID: keeps B-tree index inserts append-only, unlike random v4 GUIDs.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// When set, the row is logically deleted and filtered out of every query.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
