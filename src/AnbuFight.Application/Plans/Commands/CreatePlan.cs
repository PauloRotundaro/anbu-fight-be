namespace AnbuFight.Application.Plans.Commands;

/// <param name="DefaultValue">Opcional. Valor de tabela sugerido ao criar uma matrícula.</param>
public sealed record CreatePlanCommand(string Name, PlanType Type, decimal? DefaultValue = null)
    : IRequest<Guid>;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Type).IsInEnum();

        RuleFor(command => command.DefaultValue)
            .GreaterThan(0).LessThan(1_000_000)
            .When(command => command.DefaultValue is not null);
    }
}

public sealed class CreatePlanCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreatePlanCommand, Guid>
{
    public async Task<Guid> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalizedName = name.ToLowerInvariant();

        if (await context.Plans.AnyAsync(plan => plan.Name.ToLower() == normalizedName, cancellationToken))
        {
            throw new ConflictException($"A plan named \"{name}\" already exists.");
        }

        var plan = new Plan { Name = name, Type = request.Type, DefaultValue = request.DefaultValue };

        context.Plans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
