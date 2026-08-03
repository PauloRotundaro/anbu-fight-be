using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class ClassSessionConfiguration : BaseEntityConfiguration<ClassSession>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("class_sessions");

        builder.Property(session => session.Date).IsRequired();
        builder.Property(session => session.StartsAt).IsRequired();
        builder.Property(session => session.EndsAt).IsRequired();
        builder.Property(session => session.IsCancelled).IsRequired();
        builder.Property(session => session.CancellationReason).HasMaxLength(200);

        builder.HasOne(session => session.Class)
            .WithMany(gymClass => gymClass.Sessions)
            .HasForeignKey(session => session.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // Garante que a materialização sob demanda não duplique a mesma ocorrência quando duas
        // requisições chegam ao mesmo tempo.
        builder.HasIndex(session => new { session.ClassId, session.Date })
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        builder.HasIndex(session => session.Date).HasFilter(NotDeletedFilter);
    }
}
