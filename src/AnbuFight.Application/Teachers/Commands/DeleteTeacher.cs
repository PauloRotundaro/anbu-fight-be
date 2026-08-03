namespace AnbuFight.Application.Teachers.Commands;

public sealed record DeleteTeacherCommand(Guid Id) : IRequest;

public sealed class DeleteTeacherCommandValidator : AbstractValidator<DeleteTeacherCommand>
{
    public DeleteTeacherCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteTeacherCommandHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<DeleteTeacherCommand>
{
    public async Task Handle(DeleteTeacherCommand request, CancellationToken cancellationToken)
    {
        var teacher = await context.Teachers
            .Include(entity => entity.User)
                .ThenInclude(user => user!.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Teacher), request.Id);

        if (teacher.User is not null)
        {
            teacher.User.RevokeSessions(clock.Now);

            context.Users.Remove(teacher.User);
        }

        context.Teachers.Remove(teacher);

        await context.SaveChangesAsync(cancellationToken);
    }
}
