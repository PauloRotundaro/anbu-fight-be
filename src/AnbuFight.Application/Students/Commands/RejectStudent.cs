namespace AnbuFight.Application.Students.Commands;

/// <summary>
/// Recusa um auto-cadastro: exclusão lógica do aluno e da credencial. O e-mail volta a ficar
/// livre, então a pessoa pode se cadastrar de novo se tiver sido engano.
/// </summary>
public sealed record RejectStudentCommand(Guid Id) : IRequest;

public sealed class RejectStudentCommandValidator : AbstractValidator<RejectStudentCommand>
{
    public RejectStudentCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class RejectStudentCommandHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<RejectStudentCommand>
{
    public async Task Handle(RejectStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .Include(entity => entity.User)
                .ThenInclude(user => user!.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);

        if (student.Status != StudentStatus.PendingApproval)
        {
            throw new ConflictException(
                "Só é possível recusar um cadastro pendente. Para desligar um aluno ativo, use a exclusão.");
        }

        if (student.User is not null)
        {
            student.User.RevokeSessions(clock.Now);
            context.Users.Remove(student.User);
        }

        context.Students.Remove(student);

        await context.SaveChangesAsync(cancellationToken);
    }
}
