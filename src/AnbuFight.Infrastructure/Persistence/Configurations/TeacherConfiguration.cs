using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class TeacherConfiguration : BaseEntityConfiguration<Teacher>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        builder.Property(teacher => teacher.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(teacher => teacher.LastName).HasMaxLength(100).IsRequired();
        builder.Property(teacher => teacher.Email).HasMaxLength(256).IsRequired();
        builder.Property(teacher => teacher.PhoneNumber).HasMaxLength(20).IsRequired();

        builder.Property(teacher => teacher.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(teacher => teacher.FullName);

        builder.HasIndex(teacher => teacher.Email)
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        builder.HasIndex(teacher => new { teacher.FirstName, teacher.LastName })
            .HasFilter(NotDeletedFilter);
    }
}
