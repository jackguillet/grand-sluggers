using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// SF-30 (FR-10, FD-02, FD-13). The park-factors cohort is a <b>report, not a gate</b>: nothing here
/// asserts a factor value, a run rate or a park's character. The rows assert that the report covers
/// the catalog, names the root it ran on, measures day and night, keeps the control park at 1.00 by
/// definition, and gives the same bytes for the same seeds.
/// <para>
/// They run the smallest honest plan — one matchup, one seed, so one game a cell — because coverage,
/// shape and determinism do not need fifty games a park, and a cohort row that plays hundreds of
/// games would become the whole suite's long pole. <c>cli match --cohort park-factors</c> always runs
/// the predeclared plan. The first row is <c>Rows=compact</c>, so CI asks it of <c>trials/c80</c>.
/// </para>
/// </summary>
public class ParkFactorsCohortTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    static readonly IReadOnlyList<int> OneSeed = new[] { 1 };
    static IReadOnlyList<(string Home, string Away)> OneMatchup => ParkFactorCohort.Matchups.Take(1).ToArray();

    [Fact]
    [Trait("Rows", "compact")]
    public void SF30_CohortCoversEveryCatalogParkDayAndNight_NamesItsRoot_AndRepeatsByteForByte()
    {
        var report = ParkFactorCohort.Run(_content, OneSeed, OneMatchup);

        Assert.Equal(ParkFactorCohort.Name, report.Cohort);
        Assert.Equal(1, report.SchemaVersion);
        Assert.Equal(ParkFactorCohort.Innings, report.Innings);

        // The root is named, so a shipped run and a trials/c80 run can be filed side by side.
        Assert.Equal(_content.Root.Provenance, report.Root);
        Assert.Contains("data root", report.Root);

        // The catalog decides the park list: a park added to data/parks is measured with no code change.
        Assert.Equal(
            _content.Parks.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            report.Rows.Select(r => r.Park).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList());
        Assert.Equal(_content.Parks.Count * 2, report.Rows.Count);
        Assert.Equal(ParkFactorCohort.ControlPark, report.Rows[0].Park);

        foreach (var park in _content.Parks.Keys)
        {
            var rows = report.Rows.Where(r => r.Park == park).ToList();
            // Day and night, both present, both played.
            Assert.Equal(new[] { "day", "night" }, rows.Select(r => r.Condition).OrderBy(c => c, StringComparer.Ordinal));
            Assert.Equal(new[] { false, true }, rows.Select(r => r.Night).OrderBy(n => n));
            Assert.All(rows, r => Assert.Equal(report.GamesPerParkPerCondition, r.Games));
            Assert.All(rows, r => Assert.True(r.Games > 0, park));
            Assert.All(rows, r => Assert.Equal(_content.Parks[park].Name, r.Name));
        }

        // FD-13: Harbor is the control park, so its row is the unit in both conditions.
        Assert.Equal(ParkFactorCohort.ControlPark, report.ControlPark);
        foreach (var row in report.Rows.Where(r => r.Park == ParkFactorCohort.ControlPark))
        {
            Assert.Equal(1.0, row.RunFactor);
            Assert.Equal(1.0, row.HomeRunFactor);
        }

        Assert.Equal(OneSeed, report.Seeds);
        Assert.Equal(OneMatchup.Count, report.Matchups.Count);
        Assert.Equal(report.Rows.Sum(r => r.Games), report.Games);
        // A report, never an expectation (FD-13).
        Assert.Contains("report, not a gate", report.Acceptance);
        Assert.Contains("noise", report.Limitations);

        // The same seeds give the same bytes: the report can be filed and compared, not re-argued.
        var again = ParkFactorCohort.Run(_content, OneSeed, OneMatchup);
        Assert.Equal(report.ToJson(), again.ToJson());
        Assert.Equal(report.Table(), again.Table());
        // The table is the same rows a person can read, not a second measurement.
        Assert.StartsWith(report.Root, report.Table());
        Assert.Contains(ParkFactorCohort.ControlPark, report.Table());
        Assert.Contains("DAY", report.Table());
        Assert.Contains("NIGHT", report.Table());
    }

    [Fact]
    public void AnEmptyPlanFallsBackToThePredeclaredOne()
    {
        // A caller that passes nothing gets the filed plan, so a report can never be quietly smaller
        // than the one the CLI runs.
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, ParkFactorCohort.Seeds);
        Assert.Equal(RaceCohort.Pairs.Count * 2, ParkFactorCohort.Matchups.Count);
        // Every pair is played both ways round.
        foreach (var (h, a) in RaceCohort.Pairs)
        {
            Assert.Contains((h, a), ParkFactorCohort.Matchups);
            Assert.Contains((a, h), ParkFactorCohort.Matchups);
        }
        Assert.DoesNotContain(ParkFactorCohort.Name, RaceCohort.Names);
    }
}

/// <summary>
/// Its own class so xUnit plays these games beside the report's, not after them: a cohort row is the
/// slowest thing in the suite, and two of them in one collection would run back to back.
/// </summary>
public class ParkFactorsNightTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    /// <summary>
    /// The night half of the cohort, and <c>cli match --night</c>, reach the match the same way:
    /// <c>Match.Exhibition(..., night: true)</c>. <see cref="NightTests"/> pins why Crystal differs
    /// and Harbor does not; this row pins that a whole night game is played, not a day game with a
    /// flag set.
    /// </summary>
    [Fact]
    public void NightReachesTheMatchTheCohortPlays()
    {
        var (home, away) = ParkFactorCohort.Matchups[0];
        Assert.NotEqual(Game("crystal-rink", night: false), Game("crystal-rink", night: true));
        Assert.Equal(Game(ParkFactorCohort.ControlPark, false), Game(ParkFactorCohort.ControlPark, true));

        string Game(string parkId, bool night)
        {
            var match = Match.Exhibition(_content, home, away, innings: ParkFactorCohort.Innings,
                seed: 7, parkId: parkId, night: night);
            Assert.Equal(night, match.Night);
            Assert.Equal(parkId, match.Park.Id);
            match.AutoPlayGame();
            Assert.True(match.Over);
            return $"{match.AwayScore}-{match.HomeScore} " + string.Join(",", match.Log.Select(e => e.Kind));
        }
    }
}
