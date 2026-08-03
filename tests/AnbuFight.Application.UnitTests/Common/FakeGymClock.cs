using AnbuFight.Application.Common.Interfaces;

namespace AnbuFight.Application.UnitTests.Common;

/// <summary>Relógio determinístico, fixo no fuso da academia (UTC-3).</summary>
public sealed class FakeGymClock(DateTimeOffset now) : IGymClock
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-3);

    public static FakeGymClock Default => new(FixedTimeProvider.DefaultNow);

    public DateTimeOffset Now => now;

    public DateOnly Today => ToLocalDate(now);

    public DateOnly ToLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(Offset).DateTime);

    public DateTimeOffset ToInstant(DateOnly date, TimeOnly time) =>
        new DateTimeOffset(date.ToDateTime(time, DateTimeKind.Unspecified), Offset).ToUniversalTime();
}
