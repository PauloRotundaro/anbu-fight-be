namespace AnbuFight.Application.UnitTests.Domain;

public class ClassScheduleTests
{
    [Fact]
    public void OccursOn_looks_at_the_configured_days()
    {
        var muayThai = ClassOn([1, 3], "19:00", "20:00");

        muayThai.OccursOn(DayOfWeek.Monday).ShouldBeTrue();
        muayThai.OccursOn(DayOfWeek.Wednesday).ShouldBeTrue();
        muayThai.OccursOn(DayOfWeek.Tuesday).ShouldBeFalse();
    }

    [Fact]
    public void OverlapsWith_detects_a_clash_on_a_shared_day()
    {
        var existing = ClassOn([1, 3], "19:00", "20:00");

        existing.OverlapsWith(Time("19:30"), Time("20:30"), [3]).ShouldBeTrue();
    }

    [Fact]
    public void OverlapsWith_ignores_the_same_time_on_a_different_day()
    {
        var existing = ClassOn([1, 3], "19:00", "20:00");

        existing.OverlapsWith(Time("19:00"), Time("20:00"), [2, 4]).ShouldBeFalse();
    }

    [Fact]
    public void OverlapsWith_allows_classes_that_only_touch_at_the_boundary()
    {
        var existing = ClassOn([1], "19:00", "20:00");

        // A aula seguinte começa exatamente quando a anterior termina.
        existing.OverlapsWith(Time("20:00"), Time("21:00"), [1]).ShouldBeFalse();
    }

    [Theory]
    [InlineData("17:59", false)]
    [InlineData("18:00", true)]
    [InlineData("19:30", true)]
    [InlineData("20:00", true)]
    [InlineData("20:01", false)]
    public void IsWithinCheckInWindow_opens_sixty_minutes_before_and_closes_at_the_end(
        string momentOfTheDay,
        bool expected)
    {
        var session = new ClassSession
        {
            ClassId = Guid.CreateVersion7(),
            Date = new DateOnly(2026, 3, 10),
            StartsAt = Instant("19:00"),
            EndsAt = Instant("20:00")
        };

        session.IsWithinCheckInWindow(Instant(momentOfTheDay), minutesBefore: 60).ShouldBe(expected);
    }

    private static Class ClassOn(List<int> days, string start, string end) => new()
    {
        Name = "Muay Thai",
        Modality = Modality.MuayThai,
        DaysOfWeek = days,
        StartTime = Time(start),
        EndTime = Time(end),
        TeacherId = Guid.CreateVersion7()
    };

    private static TimeOnly Time(string value) => TimeOnly.Parse(value, CultureInfo.InvariantCulture);

    private static DateTimeOffset Instant(string time) =>
        new(new DateOnly(2026, 3, 10).ToDateTime(Time(time)), TimeSpan.FromHours(-3));
}
