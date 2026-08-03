namespace AnbuFight.Application.Attendances.Commands;

/// <summary>
/// Check-in do próprio aluno numa sessão. Toda regra é validada aqui, no servidor: a tela pode
/// esconder o botão, mas quem impede é este handler.
/// </summary>
public sealed record CheckInCommand(Guid ClassSessionId) : IRequest<Guid>;

public sealed class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator() => RuleFor(command => command.ClassSessionId).NotEmpty();
}

public sealed class CheckInCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    CheckInPolicy checkInPolicy,
    IGymClock clock)
    : IRequestHandler<CheckInCommand, Guid>
{
    public async Task<Guid> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var studentId = currentUser.StudentId
            ?? throw new ForbiddenAccessException("Esta conta não está vinculada a um cadastro de aluno.");

        var session = await context.ClassSessions
            .AsNoTracking()
            .Include(entity => entity.Class)
            .FirstOrDefaultAsync(entity => entity.Id == request.ClassSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ClassSession), request.ClassSessionId);

        if (session.IsCancelled)
        {
            throw new ConflictException("Esta aula foi cancelada.");
        }

        var now = clock.Now;

        if (!session.IsWithinCheckInWindow(now, checkInPolicy.CheckInWindowMinutesBefore))
        {
            throw new ConflictException(
                $"Fora da janela de check-in: ela abre {checkInPolicy.CheckInWindowMinutesBefore} minutos " +
                "antes do início e fecha no fim da aula.");
        }

        var alreadyCheckedIn = await context.Attendances.AnyAsync(
            attendance =>
                attendance.StudentId == studentId &&
                attendance.ClassSessionId == session.Id,
            cancellationToken);

        if (alreadyCheckedIn)
        {
            throw new ConflictException("Você já registrou presença nesta aula.");
        }

        if (session.Class.Capacity is int capacity)
        {
            var taken = await context.Attendances
                .CountAsync(attendance => attendance.ClassSessionId == session.Id, cancellationToken);

            if (taken >= capacity)
            {
                throw new ConflictException("Turma lotada.");
            }
        }

        var eligibility = await checkInPolicy.EvaluateAsync(studentId, cancellationToken);

        if (!eligibility.CanCheckIn)
        {
            throw new ConflictException(CheckInPolicy.Explain(eligibility));
        }

        var attendance = new Attendance
        {
            StudentId = studentId,
            ClassSessionId = session.Id,
            CheckedInAt = now,
            RegisteredBy = AttendanceOrigin.Student
        };

        context.Attendances.Add(attendance);
        await context.SaveChangesAsync(cancellationToken);

        return attendance.Id;
    }
}
