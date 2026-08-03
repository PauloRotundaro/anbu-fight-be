namespace AnbuFight.Application.Plans.Commands;

public sealed record UpdatePlanCommand(Guid Id, string Name, PlanType Type, decimal? DefaultValue = null)
    : IRequest;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Type).IsInEnum();

        RuleFor(command => command.DefaultValue)
            .GreaterThan(0).LessThan(1_000_000)
            .When(command => command.DefaultValue is not null);
    }
}

public sealed class UpdatePlanCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdatePlanCommand>
{
    public async Task Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.Plans
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.Id);

        var name = request.Name.Trim();
        var normalizedName = name.ToLowerInvariant();

        if (await context.Plans.AnyAsync(
                other => other.Id != plan.Id && other.Name.ToLower() == normalizedName, cancellationToken))
        {
            throw new ConflictException($"A plan named \"{name}\" already exists.");
        }

        plan.Name = name;
        plan.Type = request.Type;
        plan.DefaultValue = request.DefaultValue;

        await context.SaveChangesAsync(cancellationToken);
    }
}
