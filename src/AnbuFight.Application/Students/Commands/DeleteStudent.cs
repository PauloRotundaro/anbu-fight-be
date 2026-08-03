namespace AnbuFight.Application.Students.Commands;

/// <summary>
/// Soft deletes the student. History (enrollments and payments) is preserved, and the linked
/// credential is removed together with its refresh tokens so access stops immediately.
/// </summary>
public sealed record DeleteStudentCommand(Guid Id) : IRequest;

public sealed class DeleteStudentCommandValidator : AbstractValidator<DeleteStudentCommand>
{
    public DeleteStudentCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteStudentCommandHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<DeleteStudentCommand>
{
    public async Task Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .Include(entity => entity.User)
                .ThenInclude(user => user!.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);

        if (student.User is not null)
        {
            student.User.RevokeSessions(clock.Now);

            context.Users.Remove(student.User);
        }

        context.Students.Remove(student);

        await context.SaveChangesAsync(cancellationToken);
    }
}
