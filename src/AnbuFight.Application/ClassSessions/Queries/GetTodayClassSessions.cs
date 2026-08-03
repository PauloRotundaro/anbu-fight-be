namespace AnbuFight.Application.ClassSessions.Queries;

/// <summary>
/// As aulas de hoje, já sinalizando para o aluno se ele pode bater ponto em cada uma.
/// É o que alimenta o botão de check-in do portal.
/// </summary>
public sealed record GetTodayClassSessionsQuery : IRequest<IReadOnlyList<ClassSessionDto>>;

public sealed class GetTodayClassSessionsQueryHandler(
    ClassSessionScheduler scheduler,
    ClassSessionReader reader)
    : IRequestHandler<GetTodayClassSessionsQuery, IReadOnlyList<ClassSessionDto>>
{
    public async Task<IReadOnlyList<ClassSessionDto>> Handle(
        GetTodayClassSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var today = reader.Today;

        await scheduler.EnsureRangeAsync(today, today, cancellationToken);

        return await reader.ReadAsync(
            query => query.Where(session => session.Date == today),
            cancellationToken);
    }
}
