using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Attendances.Queries;

/// <param name="CurrentStreakDays">
/// Dias consecutivos com pelo menos um treino, contados a partir do último check-in.
/// Zera se o último treino não foi hoje nem ontem.
/// </param>
/// <param name="WeeklyAverage">Média de treinos por semana no período consultado.</param>
public sealed record AttendanceSummaryDto(
    Guid StudentId,
    DateOnly From,
    DateOnly To,
    int TotalCheckIns,
    IReadOnlyList<ModalityCount> ByModality,
    int CurrentStreakDays,
    DateTimeOffset? LastCheckInAt,
    double WeeklyAverage);

public sealed record ModalityCount(Modality Modality, int Count);

public sealed record GetAttendanceSummaryQuery(
    Guid? StudentId = null,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<AttendanceSummaryDto>;

public sealed class GetAttendanceSummaryQueryValidator : AbstractValidator<GetAttendanceSummaryQuery>
{
    public GetAttendanceSummaryQueryValidator() =>
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From is not null && query.To is not null)
            .WithMessage("'To' deve ser igual ou posterior a 'From'.");
}

public sealed class GetAttendanceSummaryQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IGymClock clock)
    : IRequestHandler<GetAttendanceSummaryQuery, AttendanceSummaryDto>
{
    private const int DefaultWindowInDays = 90;

    public async Task<AttendanceSummaryDto> Handle(
        GetAttendanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = AccessGuard.RestrictStudentFilter(currentUser, request.StudentId)
            ?? throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(GetAttendanceSummaryQuery.StudentId),
                    "Informe o aluno: esta conta não está vinculada a um cadastro de aluno.")
            ]);

        var today = clock.Today;
        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-DefaultWindowInDays);

        var attendances = await context.Attendances
            .AsNoTracking()
            .Where(attendance =>
                attendance.StudentId == studentId &&
                attendance.ClassSession.Date >= from &&
                attendance.ClassSession.Date <= to)
            .Select(attendance => new
            {
                attendance.ClassSession.Date,
                attendance.ClassSession.Class.Modality,
                attendance.CheckedInAt
            })
            .ToListAsync(cancellationToken);

        var byModality = attendances
            .GroupBy(attendance => attendance.Modality)
            .Select(group => new ModalityCount(group.Key, group.Count()))
            .OrderByDescending(modality => modality.Count)
            .ToList();

        var trainedDays = attendances.Select(attendance => attendance.Date).ToHashSet();
        var weeks = Math.Max(1d, (to.DayNumber - from.DayNumber + 1) / 7d);

        return new AttendanceSummaryDto(
            studentId,
            from,
            to,
            attendances.Count,
            byModality,
            CountStreak(trainedDays, today),
            attendances.Count == 0 ? null : attendances.Max(attendance => attendance.CheckedInAt),
            Math.Round(attendances.Count / weeks, 1));
    }

    private static int CountStreak(HashSet<DateOnly> trainedDays, DateOnly today)
    {
        if (trainedDays.Count == 0)
        {
            return 0;
        }

        // Treinar ontem mantém a sequência viva: o dia de hoje ainda não acabou.
        var cursor = trainedDays.Contains(today) ? today
            : trainedDays.Contains(today.AddDays(-1)) ? today.AddDays(-1)
            : (DateOnly?)null;

        if (cursor is null)
        {
            return 0;
        }

        var streak = 0;

        while (trainedDays.Contains(cursor.Value))
        {
            streak++;
            cursor = cursor.Value.AddDays(-1);
        }

        return streak;
    }
}
