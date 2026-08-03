using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

/// <summary>
/// Everything every table shares: primary key, audit columns and the soft delete query filter.
/// Configuring it once is what guarantees no entity can accidentally skip the filter.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>Raw column name: index filters are provider SQL, not LINQ.</summary>
    protected const string NotDeletedFilter = "deleted_at IS NULL";

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);

        // Ids are generated in the application (sequential GUIDs), never by the database.
        builder.Property(entity => entity.Id).ValueGeneratedNever();

        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt).IsRequired();

        builder.HasQueryFilter(entity => entity.DeletedAt == null);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
