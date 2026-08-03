using AnbuFight.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace AnbuFight.Infrastructure.Services;

/// <summary>
/// Resolve o tempo no fuso configurado para a academia. Instantes continuam sendo absolutos (UTC
/// no banco); o fuso só decide qual é o "dia" e a que instante corresponde um horário de aula.
/// </summary>
public sealed class GymClock : IGymClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public GymClock(TimeProvider timeProvider, IOptions<GymOptions> options)
    {
        _timeProvider = timeProvider;
        _timeZone = ResolveTimeZone(options.Value.TimeZone);
    }

    public DateTimeOffset Now => _timeProvider.GetUtcNow();

    public DateOnly Today => ToLocalDate(Now);

    public DateOnly ToLocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _timeZone).DateTime);

    public DateTimeOffset ToInstant(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Normalizado para UTC: é o mesmo instante, e o Postgres só aceita offset zero
        // em colunas timestamptz.
        return new DateTimeOffset(local, _timeZone.GetUtcOffset(local)).ToUniversalTime();
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            // Aceita tanto o id IANA ("America/Sao_Paulo") quanto o do Windows ("E. South America
            // Standard Time"), para que a mesma configuração sirva em contêiner e na máquina local.
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId) &&
                windowsId is not null)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }

            throw new InvalidOperationException(
                $"O fuso horário \"{id}\" configurado em \"{GymOptions.SectionName}:TimeZone\" não existe " +
                "neste sistema.");
        }
    }
}
