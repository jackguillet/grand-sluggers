using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// SF-30 (FR-10, FD-02, FD-13, FD-10). The park-factors cohort is a <b>report, not a gate</b>: nothing
/// here asserts a factor value, a run rate or a park's character. The rows assert that the report covers
/// the catalog, names the root it ran on, measures day and night with hazards on and off (FD-10's
/// acceptance, F4-h #858), keeps the control park at 1.00 by definition in every condition, and gives the
/// same bytes for the same seeds.
/// <para>
/// They run the smallest honest plan — one matchup, one seed, so one game a cell — because coverage,
/// shape and determinism do not need fifty games a park, and a cohort row that plays hundreds of
/// games would become the whole suite's long pole. <c>cli match --cohort park-factors</c> always runs
/// the predeclared plan.
/// </para>
/// </summary>
[Trait("Kind", "Balance")]
[Trait("Cost", "Heavy")]
public class ParkFactorsCohortTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    static readonly IReadOnlyList<int> OneSeed = new[] { 1 };
    static IReadOnlyList<(string Home, string Away)> OneMatchup => ParkFactorCohort.Matchups.Take(1).ToArray();

    [Fact]
    public void SF30_CohortCoversEveryCatalogParkDayAndNightHazardsOnAndOff_NamesItsRoot_AndRepeatsByteForByte()
    {
        var report = ParkFactorCohort.Run(_content, OneSeed, OneMatchup);

        Assert.Equal(ParkFactorCohort.Name, report.Cohort);
        // Schema 2 (#858): the rows carry the hazards fact and the report names its four conditions.
        Assert.Equal(2, report.SchemaVersion);
        Assert.Equal(ParkFactorCohort.Innings, report.Innings);

        // The root is named, so a run on the data root and a run on an overlay can be filed side by side.
        Assert.Equal(_content.Root.Provenance, report.Root);
        Assert.Contains("data root", report.Root);

        // The catalog decides the park list: a park added to data/parks is measured with no code change.
        Assert.Equal(
            _content.Parks.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            report.Rows.Select(r => r.Park).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList());
        // Four conditions (FD-10's acceptance): day and night, each with hazards on and then off.
        Assert.Equal(new[] { "day", "night", "day, hazards off", "night, hazards off" }, report.Conditions);
        Assert.Equal(_content.Parks.Count * 4, report.Rows.Count);
        Assert.Equal(ParkFactorCohort.ControlPark, report.Rows[0].Park);

        foreach (var park in _content.Parks.Keys)
        {
            var rows = report.Rows.Where(r => r.Park == park).ToList();
            // All four present, all four played, in the report's condition order; the name and the two
            // typed facts are the same condition.
            Assert.Equal(report.Conditions, rows.Select(r => r.Condition));
            Assert.Equal(new[] { (false, true), (true, true), (false, false), (true, false) }, rows.Select(r => (r.Night, r.Hazards)));
            Assert.All(rows, r => Assert.Equal(ParkFactorCohort.ConditionName(r.Night, r.Hazards), r.Condition));
            Assert.All(rows, r => Assert.Equal(report.GamesPerParkPerCondition, r.Games));
            Assert.All(rows, r => Assert.True(r.Games > 0, park));
            Assert.All(rows, r => Assert.Equal(_content.Parks[park].Name, r.Name));
        }

        // FD-13: Harbor is the control park, so its row is the unit in every condition.
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

        // The same seeds give the same bytes: the report can be filed and compared, not re-argued. The
        // second run is the hazards-off half alone (--hazards off), so the same games also prove that
        // narrowing the report moves no cell; ParkFactorsHazardsTests does the same for the other half.
        var again = ParkFactorCohort.Run(_content, OneSeed, OneMatchup, hazards: false);
        Assert.Equal(new[] { "day, hazards off", "night, hazards off" }, again.Conditions);
        Assert.Equal(report.Rows.Where(r => !r.Hazards), again.Rows);
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(report.Rows.Where(r => !r.Hazards).ToArray()),
            System.Text.Json.JsonSerializer.Serialize(again.Rows));
        // The table is the same rows a person can read, not a second measurement.
        Assert.StartsWith(report.Root, report.Table());
        Assert.Contains(ParkFactorCohort.ControlPark, report.Table());
        Assert.Contains("DAY", report.Table());
        Assert.Contains("NIGHT", report.Table());
        Assert.Contains("DAY, HAZARDS OFF", report.Table());
        Assert.Contains("NIGHT, HAZARDS OFF", report.Table());
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
[Trait("Kind", "Balance")]
[Trait("Cost", "Heavy")]
public class ParkFactorsNightTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    /// <summary>
    /// The night half of the cohort, and <c>cli match --night</c>, reach the match the same way:
    /// <c>Match.Exhibition(..., night: true)</c>. <see cref="NightTests"/> pins why Funfair differs
    /// and Harbor does not; this row pins that a whole night game is played, not a day game with a
    /// flag set.
    ///
    /// <para>
    /// Re-authored to FD-11-R2 (F4-d, #895): this row used Crystal, whose night differed from its day
    /// only by the contact window. Night keeps the stadium lights and the window is dropped on both
    /// roots, so a Crystal night is now its day, game for game. What night changes is the hazards, so the
    /// row shows that the night match plays Funfair's night block (its chompers) and the day match does
    /// not; a seed-7 cohort game need not meet a chomper, so the row reads the played park, not a score.
    /// </para>
    /// </summary>
    [Fact]
    public void NightReachesTheMatchTheCohortPlays()
    {
        var (home, away) = ParkFactorCohort.Matchups[0];
        Assert.True(Played("funfair-park", night: true).Hazards.Count > Played("funfair-park", night: false).Hazards.Count);
        Assert.Equal(Game("crystal-rink", night: false), Game("crystal-rink", night: true));
        Assert.Equal(Game(ParkFactorCohort.ControlPark, false), Game(ParkFactorCohort.ControlPark, true));

        Park Played(string parkId, bool night) =>
            Match.Exhibition(_content, home, away, innings: ParkFactorCohort.Innings, seed: 7, parkId: parkId, night: night).Park;

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

/// <summary>
/// The hazards half of <c>SF-30</c> (FD-10, F4-h #858), in its own class so its games run beside the
/// others'. The rows a report filed before the switch existed must be the rows it files now: the
/// hazards-on cells are computed here from the call the report made then —
/// <c>Match.Exhibition(content, home, away, innings, seed, parkId, night)</c>, no hazards argument — by a
/// tally written independently of <see cref="ParkFactorCohort"/>'s own, and compared with the report
/// narrowed to hazards on (<c>--hazards on</c>). The hazards-off half is narrowed and compared in
/// <see cref="ParkFactorsCohortTests"/>.
/// </summary>
[Trait("Kind", "Balance")]
[Trait("Cost", "Heavy")]
public class ParkFactorsHazardsTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    static readonly IReadOnlyList<int> OneSeed = new[] { 1 };
    static IReadOnlyList<(string Home, string Away)> OneMatchup => ParkFactorCohort.Matchups.Take(1).ToArray();

    [Fact]
    public void SF30_TheHazardsOnRowsAreTodaysRows()
    {
        // --hazards on: the report narrowed to the rows it filed before the switch existed.
        var on = ParkFactorCohort.Run(_content, OneSeed, OneMatchup, hazards: true);
        Assert.Equal(new[] { "day", "night" }, on.Conditions);
        Assert.Equal(_content.Parks.Count * 2, on.Rows.Count);

        // Today's rows, from today's call. One matchup and one seed is one game a cell, so a rate is
        // that game's count and a factor is the ratio of two games' counts.
        var (home, away) = OneMatchup[0];
        var seed = OneSeed[0];
        var games = new Dictionary<(string Park, bool Night), Match>();
        foreach (var park in _content.Parks.Keys)
        foreach (var night in new[] { false, true })
        {
            var match = Match.Exhibition(_content, home, away, innings: ParkFactorCohort.Innings, seed: seed, parkId: park, night: night);
            match.AutoPlayGame();
            Assert.True(match.Over);
            games[(park, night)] = match;
        }
        foreach (var row in on.Rows)
        {
            var game = games[(row.Park, row.Night)];
            var control = games[(ParkFactorCohort.ControlPark, row.Night)];
            Assert.Equal(row.Night ? "night" : "day", row.Condition);
            Assert.True(row.Hazards);
            Assert.Equal(1, row.Games);
            Assert.Equal(game.HomeScore + game.AwayScore, row.RunsPerGame);
            Assert.Equal(game.HomeScore, row.HomeTeamRunsPerGame);
            Assert.Equal(game.AwayScore, row.AwayTeamRunsPerGame);
            Assert.Equal(Count(game, PlayKind.Single), row.SinglesPerGame);
            Assert.Equal(Count(game, PlayKind.Double), row.DoublesPerGame);
            Assert.Equal(Count(game, PlayKind.Triple), row.TriplesPerGame);
            Assert.Equal(Count(game, PlayKind.HomeRun), row.HomeRunsPerGame);
            Assert.Equal(game.Log.Count(e => e.Outcome?.GroundRuleDouble == true), row.GroundRuleDoublesPerGame);
            Assert.Equal(Count(game, PlayKind.FlyOut), row.FlyOutsPerGame);
            Assert.Equal(Count(game, PlayKind.GroundOut), row.GroundOutsPerGame);
            Assert.Equal(Count(game, PlayKind.Strikeout), row.StrikeoutsPerGame);
            Assert.Equal(Count(game, PlayKind.Walk), row.WalksPerGame);
            if (row.Park == ParkFactorCohort.ControlPark)
            {
                Assert.Equal(1.0, row.RunFactor);
                Assert.Equal(1.0, row.HomeRunFactor);
            }
            else
            {
                Assert.Equal(Ratio(game.HomeScore + game.AwayScore, control.HomeScore + control.AwayScore), row.RunFactor);
                Assert.Equal(Ratio(Count(game, PlayKind.HomeRun), Count(control, PlayKind.HomeRun)), row.HomeRunFactor);
            }
        }

        static int Count(Match match, PlayKind kind) => match.Log.Count(e => e.Kind == kind);
        static double? Ratio(int mine, int control) => control == 0 ? null : Math.Round((double)mine / control, 2);
    }
}
