using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class StudentPlanConfiguration : BaseEntityConfiguration<StudentPlan>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StudentPlan> builder)
    {
        builder.ToTable("student_plans");

        builder.Property(enrollment => enrollment.PlanValue).HasPrecision(10, 2).IsRequired();
        builder.Property(enrollment => enrollment.DueDate).IsRequired();

        builder.HasOne(enrollment => enrollment.Student)
            .WithMany(student => student.StudentPlans)
            .HasForeignKey(enrollment => enrollment.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(enrollment => enrollment.Plan)
            .WithMany(plan => plan.StudentPlans)
            .HasForeignKey(enrollment => enrollment.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // A student may hold several plans at once, but never the same plan twice.
        // Filtered on deleted_at so a cancelled enrollment can be recreated later.
        builder.HasIndex(enrollment => new { enrollment.StudentId, enrollment.PlanId })
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        builder.HasIndex(enrollment => enrollment.PlanId);

        builder.HasIndex(enrollment => enrollment.DueDate).HasFilter(NotDeletedFilter);
    }
}
