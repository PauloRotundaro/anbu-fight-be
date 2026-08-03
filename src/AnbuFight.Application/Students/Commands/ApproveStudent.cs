namespace AnbuFight.Application.Students.Commands;

/// <summary>Aprova um auto-cadastro, liberando o aluno para treinar e fazer check-in.</summary>
public sealed record ApproveStudentCommand(Guid Id) : IRequest;

public sealed class ApproveStudentCommandValidator : AbstractValidator<ApproveStudentCommand>
{
    public ApproveStudentCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class ApproveStudentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<ApproveStudentCommand>
{
    public async Task Handle(ApproveStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);

        if (student.Status != StudentStatus.PendingApproval)
        {
            throw new ConflictException("Este cadastro não está pendente de aprovação.");
        }

        student.Approve();

        await context.SaveChangesAsync(cancellationToken);
    }
}
