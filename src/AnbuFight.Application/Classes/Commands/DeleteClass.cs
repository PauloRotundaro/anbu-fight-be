namespace AnbuFight.Application.Classes.Commands;

public sealed record DeleteClassCommand(Guid Id) : IRequest;

public sealed class DeleteClassCommandValidator : AbstractValidator<DeleteClassCommand>
{
    public DeleteClassCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteClassCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteClassCommand>
{
    public async Task Handle(DeleteClassCommand request, CancellationToken cancellationToken)
    {
        var gymClass = await context.Classes
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Class), request.Id);

        var hasHistory = await context.Attendances
            .AnyAsync(attendance => attendance.ClassSession.ClassId == gymClass.Id, cancellationToken);

        if (hasHistory)
        {
            throw new ConflictException(
                "Esta aula já tem presenças registradas. Desative-a em vez de excluir, para preservar o histórico.");
        }

        var sessions = await context.ClassSessions
            .Where(session => session.ClassId == gymClass.Id)
            .ToListAsync(cancellationToken);

        context.ClassSessions.RemoveRange(sessions);
        context.Classes.Remove(gymClass);

        await context.SaveChangesAsync(cancellationToken);
    }
}
