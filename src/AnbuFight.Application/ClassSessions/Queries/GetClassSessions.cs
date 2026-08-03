using AnbuFight.Application.Attendances;

namespace AnbuFight.Application.ClassSessions.Queries;

/// <summary>
/// Ocorrências das aulas num período. As sessões que ainda não existiam são criadas nesta consulta,
/// então o id devolvido é estável e serve para check-in e cancelamento.
/// </summary>
public sealed record GetClassSessionsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    Modality? Modality = null,
    Guid? TeacherId = null) : IRequest<IReadOnlyList<ClassSessionDto>>;

public sealed class GetClassSessionsQueryValidator : AbstractValidator<GetClassSessionsQuery>
{
    public GetClassSessionsQueryValidator()
    {
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From is not null && query.To is not null)
            .WithMessage("'To' deve ser igual ou posterior a 'From'.");

        RuleFor(query => query)
            .Must(query =>
                query.From is null || query.To is null ||
                query.To.Value.DayNumber - query.From.Value.DayNumber < ClassSessionScheduler.MaxRangeInDays)
            .WithMessage($"O período não pode passar de {ClassSessionScheduler.MaxRangeInDays} dias.");

        RuleFor(query => query.Modality).IsInEnum().When(query => query.Modality is not null);
    }
}

public sealed class GetClassSessionsQueryHandler(
    IApplicationDbContext context,
    ClassSessionScheduler scheduler,
    ClassSessionReader reader)
    : IRequestHandler<GetClassSessionsQuery, IReadOnlyList<ClassSessionDto>>
{
    public async Task<IReadOnlyList<ClassSessionDto>> Handle(
        GetClassSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var from = request.From ?? reader.Today;
        var to = request.To ?? from.AddDays(6);

        await scheduler.EnsureRangeAsync(from, to, cancellationToken);

        return await reader.ReadAsync(
            query => query
                .Where(session => session.Date >= from && session.Date <= to)
                .Where(session => request.Modality == null || session.Class.Modality == request.Modality)
                .Where(session => request.TeacherId == null || session.Class.TeacherId == request.TeacherId),
            cancellationToken);
    }
}

/// <summary>
/// Leitura de sessões já enriquecida com o estado de check-in do usuário atual — usada por todas
/// as consultas de sessão para que a regra fique num lugar só.
/// </summary>
public sealed class ClassSessionReader(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IGymClock clock,
    CheckInPolicy checkInPolicy)
{
    public DateOnly Today => clock.Today;

    public async Task<List<ClassSessionDto>> ReadAsync(
        Func<IQueryable<ClassSession>, IQueryable<ClassSession>> filter,
        CancellationToken cancellationToken)
    {
        var studentId = currentUser.IsStudent ? currentUser.StudentId : null;

        var sessions = await filter(context.ClassSessions.AsNoTracking())
            .OrderBy(session => session.StartsAt)
            .Select(ClassSessionDto.Projection(studentId))
            .ToListAsync(cancellationToken);

        var eligibility = studentId is Guid student
            ? await checkInPolicy.EvaluateAsync(student, cancellationToken)
            : null;

        return sessions.WithCheckInState(eligibility, clock.Now, checkInPolicy.CheckInWindowMinutesBefore);
    }
}
