using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : BaseEntityConfiguration<Payment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.Property(payment => payment.PlanValue).HasPrecision(10, 2).IsRequired();
        builder.Property(payment => payment.Value).HasPrecision(10, 2).IsRequired();
        builder.Property(payment => payment.DueDate).IsRequired();

        builder.HasOne(payment => payment.Student)
            .WithMany(student => student.Payments)
            .HasForeignKey(payment => payment.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(payment => payment.StudentPlan)
            .WithMany(enrollment => enrollment.Payments)
            .HasForeignKey(payment => payment.StudentPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // The statement of a student, the most frequent read.
        builder.HasIndex(payment => new { payment.StudentId, payment.DueDate })
            .HasFilter(NotDeletedFilter);

        builder.HasIndex(payment => payment.StudentPlanId);

        // Partial index for the overdue/receivables report: only open charges are indexed.
        builder.HasIndex(payment => payment.DueDate)
            .HasDatabaseName("ix_payments_open_due_date")
            .HasFilter("payd_at IS NULL AND deleted_at IS NULL");
    }
}
