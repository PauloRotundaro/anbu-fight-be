using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(user => user.Student)
            .WithOne(student => student.User)
            .HasForeignKey<User>(user => user.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(user => user.Teacher)
            .WithOne(teacher => teacher.User)
            .HasForeignKey<User>(user => user.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        // One credential per person.
        builder.HasIndex(user => user.StudentId)
            .IsUnique()
            .HasFilter("student_id IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(user => user.TeacherId)
            .IsUnique()
            .HasFilter("teacher_id IS NOT NULL AND deleted_at IS NULL");
    }
}
