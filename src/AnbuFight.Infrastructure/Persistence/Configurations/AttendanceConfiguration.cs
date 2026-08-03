using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class AttendanceConfiguration : BaseEntityConfiguration<Attendance>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");

        builder.Property(attendance => attendance.CheckedInAt).IsRequired();

        builder.Property(attendance => attendance.RegisteredBy)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(attendance => attendance.Student)
            .WithMany(student => student.Attendances)
            .HasForeignKey(attendance => attendance.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attendance => attendance.ClassSession)
            .WithMany(session => session.Attendances)
            .HasForeignKey(attendance => attendance.ClassSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Um aluno só marca presença uma vez por aula.
        builder.HasIndex(attendance => new { attendance.StudentId, attendance.ClassSessionId })
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        // Histórico de frequência do aluno, a consulta mais frequente.
        builder.HasIndex(attendance => new { attendance.StudentId, attendance.CheckedInAt })
            .HasFilter(NotDeletedFilter);

        builder.HasIndex(attendance => attendance.ClassSessionId);
    }
}
