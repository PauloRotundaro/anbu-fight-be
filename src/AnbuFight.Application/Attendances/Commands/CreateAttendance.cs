namespace AnbuFight.Application.Attendances.Commands;

/// <summary>
/// Lançamento manual pela gestão — a chamada feita pelo professor. Não passa pela janela de
/// horário nem pela regra de inadimplência: é um registro do que aconteceu, não uma liberação.
/// </summary>
public sealed record CreateAttendanceCommand(
    Guid StudentId,
    Guid ClassSessionId,
    DateTimeOffset? CheckedInAt = null) : IRequest<Guid>;

public sealed class CreateAttendanceCommandValidator : AbstractValidator<CreateAttendanceCommand>
{
    public CreateAttendanceCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.ClassSessionId).NotEmpty();
    }
}

public sealed class CreateAttendanceCommandHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<CreateAttendanceCommand, Guid>
{
    public async Task<Guid> Handle(CreateAttendanceCommand request, CancellationToken cancellationToken)
    {
        if (!await context.Students.AnyAsync(student => student.Id == request.StudentId, cancellationToken))
        {
            throw new NotFoundException(nameof(Student), request.StudentId);
        }

        var session = await context.ClassSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.ClassSessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ClassSession), request.ClassSessionId);

        var alreadyCheckedIn = await context.Attendances.AnyAsync(
            attendance =>
                attendance.StudentId == request.StudentId &&
                attendance.ClassSessionId == session.Id,
            cancellationToken);

        if (alreadyCheckedIn)
        {
            throw new ConflictException("Este aluno já tem presença registrada nesta aula.");
        }

        var attendance = new Attendance
        {
            StudentId = request.StudentId,
            ClassSessionId = session.Id,
            CheckedInAt = request.CheckedInAt ?? clock.Now,
            RegisteredBy = AttendanceOrigin.Teacher
        };

        context.Attendances.Add(attendance);
        await context.SaveChangesAsync(cancellationToken);

        return attendance.Id;
    }
}
