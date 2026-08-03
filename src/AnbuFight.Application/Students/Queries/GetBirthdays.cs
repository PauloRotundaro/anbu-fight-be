namespace AnbuFight.Application.Students.Queries;

/// <param name="Age">Idade que o aluno vai completar.</param>
/// <param name="DaysUntilBirthday">0 significa hoje.</param>
public sealed record BirthdayStudentDto(
    Guid Id,
    string FullName,
    DateOnly Birthdate,
    int Age,
    int DaysUntilBirthday,
    string PhoneNumber,
    StudentStatus Status,
    bool IsActive);

/// <summary>
/// Aniversariantes dos próximos dias. Usa uma janela em dias em vez de um intervalo <c>MM-DD</c>
/// justamente para que a virada de ano não seja um caso especial.
/// </summary>
public sealed record GetBirthdaysQuery(int DaysAhead = 30, bool OnlyActive = true)
    : IRequest<IReadOnlyList<BirthdayStudentDto>>;

public sealed class GetBirthdaysQueryValidator : AbstractValidator<GetBirthdaysQuery>
{
    public GetBirthdaysQueryValidator() =>
        RuleFor(query => query.DaysAhead).InclusiveBetween(0, 365);
}

public sealed class GetBirthdaysQueryHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<GetBirthdaysQuery, IReadOnlyList<BirthdayStudentDto>>
{
    public async Task<IReadOnlyList<BirthdayStudentDto>> Handle(
        GetBirthdaysQuery request,
        CancellationToken cancellationToken)
    {
        var today = clock.Today;

        // Filtrar por (mês, dia) no banco evita trazer a base inteira para contar aniversários,
        // e resolve a virada de ano sem tratamento especial.
        var window = BuildWindow(today, request.DaysAhead);

        var query = context.Students.AsNoTracking();

        if (request.OnlyActive)
        {
            query = query.Where(student => student.Status == StudentStatus.Active);
        }

        var candidates = await query
            .Where(student => window.Keys.Contains(student.Birthdate.Month * 100 + student.Birthdate.Day))
            .Select(student => new
            {
                student.Id,
                student.FirstName,
                student.LastName,
                student.Birthdate,
                student.PhoneNumber,
                student.Status
            })
            .ToListAsync(cancellationToken);

        return [.. candidates
            .Select(student =>
            {
                var daysUntil = window.DaysUntil[student.Birthdate.Month * 100 + student.Birthdate.Day];
                var celebrationDate = today.AddDays(daysUntil);

                return new BirthdayStudentDto(
                    student.Id,
                    $"{student.FirstName} {student.LastName}",
                    student.Birthdate,
                    celebrationDate.Year - student.Birthdate.Year,
                    daysUntil,
                    student.PhoneNumber,
                    student.Status,
                    student.Status == StudentStatus.Active);
            })
            .OrderBy(student => student.DaysUntilBirthday)
                .ThenBy(student => student.FullName)];
    }

    private static (List<int> Keys, Dictionary<int, int> DaysUntil) BuildWindow(DateOnly today, int daysAhead)
    {
        var daysUntil = new Dictionary<int, int>(daysAhead + 1);

        for (var offset = 0; offset <= daysAhead; offset++)
        {
            var date = today.AddDays(offset);
            var key = (date.Month * 100) + date.Day;

            daysUntil.TryAdd(key, offset);

            // Quem nasceu em 29/02 comemora em 01/03 nos anos que não são bissextos.
            if (date is { Month: 3, Day: 1 } && !DateTime.IsLeapYear(date.Year))
            {
                daysUntil.TryAdd(229, offset);
            }
        }

        return ([.. daysUntil.Keys], daysUntil);
    }
}
