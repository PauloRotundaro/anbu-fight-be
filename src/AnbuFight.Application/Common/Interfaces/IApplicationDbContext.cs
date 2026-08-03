using AnbuFight.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnbuFight.Application.Common.Interfaces;

/// <summary>
/// Persistence surface exposed to the Application layer: enough to write queries and commands,
/// without leaking the concrete <c>DbContext</c> or the database provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Student> Students { get; }

    DbSet<Teacher> Teachers { get; }

    DbSet<Plan> Plans { get; }

    DbSet<StudentPlan> StudentPlans { get; }

    DbSet<Payment> Payments { get; }

    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    DbSet<Class> Classes { get; }

    DbSet<ClassSession> ClassSessions { get; }

    DbSet<Attendance> Attendances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
