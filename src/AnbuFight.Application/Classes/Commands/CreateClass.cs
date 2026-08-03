namespace AnbuFight.Application.Classes.Commands;

/// <param name="DaysOfWeek">0 = domingo … 6 = sábado. Uma aula que ocorre em vários dias é um só registro.</param>
public sealed record CreateClassCommand(
    string Name,
    Modality Modality,
    IReadOnlyList<int> DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid TeacherId,
    int? Capacity = null,
    bool IsActive = true) : IRequest<Guid>, IClassSchedule;

public sealed class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassCommandValidator() => ClassScheduleRules.ValidateSchedule(this);
}

public sealed class CreateClassCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateClassCommand, Guid>
{
    public async Task<Guid> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        if (!await context.Teachers.AnyAsync(teacher => teacher.Id == request.TeacherId, cancellationToken))
        {
            throw new NotFoundException(nameof(Teacher), request.TeacherId);
        }

        await ClassScheduleRules.EnsureTeacherIsFreeAsync(context, request, null, cancellationToken);

        var gymClass = new Class
        {
            Name = request.Name.Trim(),
            Modality = request.Modality,
            DaysOfWeek = [.. request.DaysOfWeek.Order()],
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TeacherId = request.TeacherId,
            Capacity = request.Capacity,
            IsActive = request.IsActive
        };

        context.Classes.Add(gymClass);
        await context.SaveChangesAsync(cancellationToken);

        return gymClass.Id;
    }
}
