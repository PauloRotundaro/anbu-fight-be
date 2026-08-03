namespace AnbuFight.Application.Classes.Commands;

public sealed record UpdateClassCommand(
    Guid Id,
    string Name,
    Modality Modality,
    IReadOnlyList<int> DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid TeacherId,
    int? Capacity,
    bool IsActive) : IRequest, IClassSchedule;

public sealed class UpdateClassCommandValidator : AbstractValidator<UpdateClassCommand>
{
    public UpdateClassCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        ClassScheduleRules.ValidateSchedule(this);
    }
}

public sealed class UpdateClassCommandHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<UpdateClassCommand>
{
    public async Task Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var gymClass = await context.Classes
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Class), request.Id);

        if (!await context.Teachers.AnyAsync(teacher => teacher.Id == request.TeacherId, cancellationToken))
        {
            throw new NotFoundException(nameof(Teacher), request.TeacherId);
        }

        await ClassScheduleRules.EnsureTeacherIsFreeAsync(context, request, gymClass.Id, cancellationToken);

        var scheduleChanged =
            gymClass.StartTime != request.StartTime ||
            gymClass.EndTime != request.EndTime ||
            !gymClass.DaysOfWeek.SequenceEqual(request.DaysOfWeek.Order());

        gymClass.Name = request.Name.Trim();
        gymClass.Modality = request.Modality;
        gymClass.DaysOfWeek = [.. request.DaysOfWeek.Order()];
        gymClass.StartTime = request.StartTime;
        gymClass.EndTime = request.EndTime;
        gymClass.TeacherId = request.TeacherId;
        gymClass.Capacity = request.Capacity;
        gymClass.IsActive = request.IsActive;

        if (scheduleChanged)
        {
            await RescheduleFutureSessionsAsync(gymClass, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sessões futuras ainda sem presença são descartadas para serem regeradas com o novo horário.
    /// Sessões passadas e com presença ficam como estão: são histórico.
    /// </summary>
    private async Task RescheduleFutureSessionsAsync(Class gymClass, CancellationToken cancellationToken)
    {
        var today = clock.Today;

        var futureSessions = await context.ClassSessions
            .Where(session =>
                session.ClassId == gymClass.Id &&
                session.Date >= today &&
                !session.Attendances.Any())
            .ToListAsync(cancellationToken);

        context.ClassSessions.RemoveRange(futureSessions);
    }
}
