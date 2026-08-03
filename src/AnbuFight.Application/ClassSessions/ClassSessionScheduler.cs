namespace AnbuFight.Application.ClassSessions;

/// <summary>
/// Materializa as ocorrências das aulas sob demanda: ao consultar um período, as sessões que
/// faltam são criadas e persistidas.
///
/// A alternativa seria calcular as ocorrências a cada leitura, mas aí presença e cancelamento
/// não teriam um registro real para apontar. Materializar dá id estável e integridade referencial,
/// sem depender de um job agendado.
/// </summary>
public sealed class ClassSessionScheduler(IApplicationDbContext context, IGymClock clock)
{
    /// <summary>Teto de segurança: uma consulta não materializa mais do que um trimestre.</summary>
    public const int MaxRangeInDays = 92;

    public async Task EnsureRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var classes = await context.Classes
            .AsNoTracking()
            .Where(gymClass => gymClass.IsActive)
            .Select(gymClass => new
            {
                gymClass.Id,
                gymClass.DaysOfWeek,
                gymClass.StartTime,
                gymClass.EndTime
            })
            .ToListAsync(cancellationToken);

        if (classes.Count == 0)
        {
            return;
        }

        var existing = await context.ClassSessions
            .AsNoTracking()
            .Where(session => session.Date >= from && session.Date <= to)
            .Select(session => new { session.ClassId, session.Date })
            .ToListAsync(cancellationToken);

        var known = existing.Select(session => (session.ClassId, session.Date)).ToHashSet();
        var missing = new List<ClassSession>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var dayOfWeek = (int)date.DayOfWeek;

            foreach (var gymClass in classes.Where(gymClass => gymClass.DaysOfWeek.Contains(dayOfWeek)))
            {
                if (known.Add((gymClass.Id, date)))
                {
                    missing.Add(new ClassSession
                    {
                        ClassId = gymClass.Id,
                        Date = date,
                        StartsAt = clock.ToInstant(date, gymClass.StartTime),
                        EndsAt = clock.ToInstant(date, gymClass.EndTime)
                    });
                }
            }
        }

        if (missing.Count == 0)
        {
            return;
        }

        context.ClassSessions.AddRange(missing);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Duas requisições simultâneas podem tentar materializar o mesmo período; o índice
            // único (aula, data) garante que só uma vence. A outra segue com o que já existe —
            // mas só se as sessões realmente estiverem lá: qualquer outra falha precisa subir.
            foreach (var session in missing)
            {
                context.ClassSessions.Entry(session).State = EntityState.Detached;
            }

            var persisted = await context.ClassSessions
                .AsNoTracking()
                .Where(session => session.Date >= from && session.Date <= to)
                .Select(session => new { session.ClassId, session.Date })
                .ToListAsync(cancellationToken);

            var persistedKeys = persisted.Select(session => (session.ClassId, session.Date)).ToHashSet();

            if (missing.Exists(session => !persistedKeys.Contains((session.ClassId, session.Date))))
            {
                throw;
            }
        }
    }
}
