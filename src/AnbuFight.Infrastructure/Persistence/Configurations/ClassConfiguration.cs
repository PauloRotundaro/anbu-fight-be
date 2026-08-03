using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class ClassConfiguration : BaseEntityConfiguration<Class>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("classes");

        builder.Property(gymClass => gymClass.Name).HasMaxLength(120).IsRequired();

        builder.Property(gymClass => gymClass.Modality)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Postgres tem array nativo: os dias da semana cabem numa coluna só, sem tabela auxiliar.
        builder.Property(gymClass => gymClass.DaysOfWeek)
            .HasColumnType("integer[]")
            .IsRequired();

        builder.Property(gymClass => gymClass.StartTime).IsRequired();
        builder.Property(gymClass => gymClass.EndTime).IsRequired();
        builder.Property(gymClass => gymClass.IsActive).IsRequired();

        builder.HasOne(gymClass => gymClass.Teacher)
            .WithMany(teacher => teacher.Classes)
            .HasForeignKey(gymClass => gymClass.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(gymClass => gymClass.TeacherId);

        builder.HasIndex(gymClass => new { gymClass.IsActive, gymClass.StartTime })
            .HasFilter(NotDeletedFilter);
    }
}
