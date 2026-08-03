using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class StudentConfiguration : BaseEntityConfiguration<Student>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.Property(student => student.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(student => student.LastName).HasMaxLength(100).IsRequired();
        builder.Property(student => student.Email).HasMaxLength(256).IsRequired();
        builder.Property(student => student.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(student => student.EmergencyContact).HasMaxLength(200);
        builder.Property(student => student.EmergencyPhoneNumber).HasMaxLength(20);
        // Stored as text: readable in the database and stable if the enum is ever reordered.
        builder.Property(student => student.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(student => student.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(student => student.FullName);
        builder.Ignore(student => student.IsActive);

        builder.HasIndex(student => student.Email)
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        // Covers the default listing, which is sorted by name and usually filtered by status.
        builder.HasIndex(student => new { student.Status, student.FirstName, student.LastName })
            .HasFilter(NotDeletedFilter);

        // Aniversariantes: consulta por (mês, dia) sem varrer a tabela.
        builder.HasIndex(student => student.Birthdate).HasFilter(NotDeletedFilter);
    }
}
