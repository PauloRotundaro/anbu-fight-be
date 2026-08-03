namespace AnbuFight.Application.Classes;

/// <summary>
/// Regras compartilhadas por criação e edição de aula: validação da grade e detecção de choque
/// na agenda do professor.
/// </summary>
internal static class ClassScheduleRules
{
    public static void ValidateSchedule<T>(AbstractValidator<T> validator)
        where T : IClassSchedule
    {
        validator.RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        validator.RuleFor(command => command.Modality).IsInEnum();
        validator.RuleFor(command => command.TeacherId).NotEmpty();

        validator.RuleFor(command => command.DaysOfWeek)
            .NotEmpty().WithMessage("Informe pelo menos um dia da semana.")
            .Must(days => days.All(day => day is >= 0 and <= 6))
            .WithMessage("Os dias da semana devem estar entre 0 (domingo) e 6 (sábado).")
            .Must(days => days.Distinct().Count() == days.Count)
            .WithMessage("Há dias da semana repetidos.");

        validator.RuleFor(command => command.EndTime)
            .GreaterThan(command => command.StartTime)
            .WithMessage("O horário de término deve ser depois do início.");

        validator.RuleFor(command => command.Capacity)
            .GreaterThan(0).WithMessage("A capacidade deve ser maior que zero.")
            .When(command => command.Capacity is not null);
    }

    /// <summary>
    /// Impede duas aulas do mesmo professor no mesmo horário. O filtro por horário roda no banco;
    /// a interseção de dias é resolvida em memória, sobre o punhado de candidatos que sobra.
    /// </summary>
    public static async Task EnsureTeacherIsFreeAsync(
        IApplicationDbContext context,
        IClassSchedule schedule,
        Guid? excludedClassId,
        CancellationToken cancellationToken)
    {
        var candidates = await context.Classes
            .AsNoTracking()
            .Where(other =>
                other.TeacherId == schedule.TeacherId &&
                other.IsActive &&
                other.Id != excludedClassId &&
                other.StartTime < schedule.EndTime &&
                schedule.StartTime < other.EndTime)
            .Select(other => new { other.Name, other.DaysOfWeek })
            .ToListAsync(cancellationToken);

        var conflict = candidates
            .FirstOrDefault(other => other.DaysOfWeek.Intersect(schedule.DaysOfWeek).Any());

        if (conflict is not null)
        {
            throw new ConflictException(
                $"O professor já tem a aula \"{conflict.Name}\" neste horário.");
        }
    }
}

/// <summary>Forma comum aos comandos de criação e edição de aula.</summary>
internal interface IClassSchedule
{
    string Name { get; }

    Modality Modality { get; }

    IReadOnlyList<int> DaysOfWeek { get; }

    TimeOnly StartTime { get; }

    TimeOnly EndTime { get; }

    Guid TeacherId { get; }

    int? Capacity { get; }
}
