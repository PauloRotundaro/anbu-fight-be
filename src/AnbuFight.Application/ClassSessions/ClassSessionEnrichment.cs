using AnbuFight.Application.Attendances;

namespace AnbuFight.Application.ClassSessions;

/// <summary>
/// Resolve, para cada sessão, se o aluno que consultou pode bater ponto agora — e, quando não pode,
/// com que texto a tela explica. Assim a lista de aulas de hoje já chega pronta para renderizar.
/// </summary>
internal static class ClassSessionEnrichment
{
    public static List<ClassSessionDto> WithCheckInState(
        this List<ClassSessionDto> sessions,
        CheckInEligibilityDto? eligibility,
        DateTimeOffset now,
        int windowMinutesBefore)
    {
        if (eligibility is null)
        {
            return sessions;
        }

        for (var index = 0; index < sessions.Count; index++)
        {
            var session = sessions[index];
            var unavailable = Evaluate(session, eligibility, now, windowMinutesBefore);

            sessions[index] = session with
            {
                CanCheckIn = unavailable is null,
                CheckInUnavailableReason = unavailable
            };
        }

        return sessions;
    }

    private static string? Evaluate(
        ClassSessionDto session,
        CheckInEligibilityDto eligibility,
        DateTimeOffset now,
        int windowMinutesBefore)
    {
        if (session.AlreadyCheckedIn)
        {
            return "Presença já registrada nesta aula.";
        }

        if (session.IsCancelled)
        {
            return "Esta aula foi cancelada.";
        }

        if (session.Capacity is int capacity && session.AttendanceCount >= capacity)
        {
            return "Turma lotada.";
        }

        if (now < session.StartsAt.AddMinutes(-windowMinutesBefore))
        {
            return $"O check-in abre {windowMinutesBefore} minutos antes do início da aula.";
        }

        if (now > session.EndsAt)
        {
            return "Esta aula já terminou.";
        }

        return eligibility.CanCheckIn ? null : CheckInPolicy.Explain(eligibility);
    }
}
