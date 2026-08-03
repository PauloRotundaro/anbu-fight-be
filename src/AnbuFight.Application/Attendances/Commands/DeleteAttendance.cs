namespace AnbuFight.Application.Attendances.Commands;

public sealed record DeleteAttendanceCommand(Guid Id) : IRequest;

public sealed class DeleteAttendanceCommandValidator : AbstractValidator<DeleteAttendanceCommand>
{
    public DeleteAttendanceCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteAttendanceCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteAttendanceCommand>
{
    public async Task Handle(DeleteAttendanceCommand request, CancellationToken cancellationToken)
    {
        var attendance = await context.Attendances
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Attendance), request.Id);

        context.Attendances.Remove(attendance);

        await context.SaveChangesAsync(cancellationToken);
    }
}
