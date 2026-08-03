using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : BaseEntityConfiguration<Plan>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.Property(plan => plan.Name).HasMaxLength(120).IsRequired();

        builder.Property(plan => plan.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(plan => plan.DefaultValue).HasPrecision(10, 2);

        builder.HasIndex(plan => plan.Name)
            .IsUnique()
            .HasFilter(NotDeletedFilter);
    }
}
