namespace AnbuFight.Application.ClassSessions.Commands;

/// <summary>
/// Cancela uma ocorrência específica (feriado, professor doente) sem alterar a grade semanal.
/// </summary>
public sealed record CancelClassSessionCommand(Guid Id, string? Reason = null) : IRequest;

public sealed class CancelClassSessionCommandValidator : AbstractValidator<CancelClassSessionCommand>
{
    public CancelClassSessionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Reason).MaximumLength(200);
    }
}

public sealed class CancelClassSessionCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CancelClassSessionCommand>
{
    public async Task Handle(CancelClassSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await context.ClassSessions
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ClassSession), request.Id);

        if (session.IsCancelled)
        {
            throw new ConflictException("Esta aula já está cancelada.");
        }

        session.Cancel(request.Reason?.Trim());

        await context.SaveChangesAsync(cancellationToken);
    }
}
