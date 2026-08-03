namespace AnbuFight.Application.UnitTests.Common;

/// <summary>Deterministic clock, so date-dependent rules are tested against a known "now".</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    public static FixedTimeProvider Default => new(DefaultNow);

    public override DateTimeOffset GetUtcNow() => now;
}
