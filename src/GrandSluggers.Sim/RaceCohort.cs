using System.Text;

namespace GrandSluggers.Sim;

/// <summary>Predeclared sampling plans for #693. Runs the existing match, without changing any coefficient.</summary>
public static class RaceCohort
{
    public static readonly IReadOnlyList<string> Names = new[] { "s29", "harbor-calibration", "harbor-validation" };
    public static readonly IReadOnlyList<int> CalibrationSeeds = new[] { 1, 2, 3, 4, 5 };
    public static readonly IReadOnlyList<int> ValidationSeeds = new[] { 1001, 1002, 1003, 1004, 1005 };
    /// <summary>The five predeclared matchups. S-29 and <see cref="ParkFactorCohort"/> share them, so the two reports can be read together.</summary>
    public static readonly IReadOnlyList<(string Home, string Away)> Pairs =
        new[] { ("rio", "ashlord"), ("zig", "konga"), ("fenn", "brondo"), ("konga", "rio"), ("vale", "brondo") };

    public static RaceCohortReport Run(ContentCatalog content, string name)
    {
        if (!Names.Contains(name)) throw new ArgumentException($"Unknown cohort '{name}'; choose {string.Join(", ", Names)}");
        var seeds = name == "harbor-validation" ? ValidationSeeds : CalibrationSeeds;
        var games = new List<RaceCohortGame>();
        foreach (var (x, y) in Pairs)
        foreach (var (home, away) in new[] { (x, y), (y, x) })
        foreach (var seed in seeds)
        {
            var match = name == "s29" ? Match.Exhibition(content, home, away, innings: 3, seed: seed)
                : Match.Exhibition(content, home, away, innings: 3, seed: seed, parkId: ExhibitionPick.DefaultPark);
            match.AutoPlayGame();
            if (!match.Over) throw new InvalidOperationException("Cohort game did not finish");
            games.Add(new(seed, home, away, match.Park.Id, PlayTraceIdentity.Capture(match), match.HomeScore, match.AwayScore,
                match.Log.GroupBy(p => p.Kind.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                match.Log.Count(p => (p.Outcome?.OutsMade.Count ?? 0) >= 2)));
        }
        return new(1, name, seeds.ToArray(), games, games.Average(g => g.HomeRuns), games.Average(g => g.AwayRuns),
            name == "s29" ? "accepted F693-06: home and away means separately 1.8–5" : "accepted F693-06-H: home and away means separately 1.8–5; baseline misses remain calibration work",
            "Outcome counts are not opportunity rates. Relay opportunities require the separate live-play traces. Human feel gate remains open.");
    }
}
public sealed record RaceCohortGame(int Seed, string Home, string Away, string Park, PlayTraceIdentity Identity,
    int HomeRuns, int AwayRuns, IReadOnlyDictionary<string, int> Outcomes, int MultipleOutPlays);
public sealed record RaceCohortReport(int SchemaVersion, string Cohort, IReadOnlyList<int> Seeds,
    IReadOnlyList<RaceCohortGame> Games, double MeanHomeRuns, double MeanAwayRuns, string Acceptance, string Limitations)
{
    public string ToJson() => JsonSerializer.Serialize(this, PlayTrace.IndentedJson);
}

/// <summary>
/// FR-10 / SF-30: the same predeclared plan at every park in the catalog, day and night, with park
/// hazards on and off, reported against Harbor. A <b>report, never a gate</b> (FD-13): Harbor is the
/// only calibrated park, every other park is measured and filed, and no test asserts a factor. A
/// factor outside the FD-02 direction is a finding in a PR body, never an expectation to edit.
/// <para>
/// The plan lives here rather than in <c>tools/park-factors.py</c> so the rows are typed, the seeds
/// are declared in code, and night is measured at all — the CLI could not play a night game before
/// #828. The park list is the catalog's: a park added to <c>data/parks/</c> appears with no code
/// change. The row set says which data root and overlay produced it, so a shipped run and a
/// <c>trials/example</c> run can be filed side by side.
/// </para>
/// <para>
/// Four conditions, as FD-10's acceptance asks (F4-h, #858): day and night, each with hazards on and
/// off. The hazards-off games are <c>Match.Exhibition(..., hazards: false)</c>, the switch a couch game
/// uses, so the report measures what the players would play. A row with hazards on is the row this
/// report filed before the switch existed, cell for cell; the hazards-off rows are added beside it,
/// never mixed into it.
/// </para>
/// </summary>
public static class ParkFactorCohort
{
    public const string Name = "park-factors";

    /// <summary>
    /// The control park (FD-13: "Harbor is the only calibrated park"). Its row is the unit: its two
    /// factors are 1.00 by definition, not by measurement. It reads the one default (#820, SF-04)
    /// rather than spelling an id: today's control park and today's default park are the same field,
    /// and a second spelling would be a second source of truth.
    /// </summary>
    public const string ControlPark = ExhibitionPick.DefaultPark;

    public const int Innings = 3;

    /// <summary>
    /// The predeclared seeds, restated here so a change to a Harbor cohort cannot silently move this
    /// report. Five seeds x ten matchups is fifty games per park per condition — the sample size of
    /// <c>docs/research/fields-park-baseline.json</c>, so the two can be read side by side.
    /// </summary>
    public static readonly IReadOnlyList<int> Seeds = new[] { 1, 2, 3, 4, 5 };

    /// <summary>
    /// The predeclared matchups: the five S-29 pairs, each played both ways round. Who bats last is
    /// part of a park's story, and the lineups differ.
    /// </summary>
    public static readonly IReadOnlyList<(string Home, string Away)> Matchups =
        RaceCohort.Pairs.SelectMany(p => new[] { (p.Home, p.Away), (p.Away, p.Home) }).ToArray();

    /// <summary>
    /// A condition's name in the table and the JSON: <c>day</c> or <c>night</c>, and
    /// <c>, hazards off</c> after it when the switch is off. Hazards on is the default and is not
    /// spelled, so a hazards-on row reads exactly as the report read before the switch existed.
    /// </summary>
    public static string ConditionName(bool night, bool hazards) =>
        (night ? "night" : "day") + (hazards ? "" : ", hazards off");

    /// <summary>
    /// Runs the plan. <paramref name="seeds"/> and <paramref name="matchups"/> exist so a test row
    /// can ask the same question of a smaller plan; the CLI always runs the predeclared one.
    /// <paramref name="hazards"/> null runs both states (the filed report); true or false narrows the
    /// report to that one state (<c>cli match --cohort park-factors --hazards on|off</c>).
    /// </summary>
    public static ParkFactorReport Run(ContentCatalog content, IReadOnlyList<int>? seeds = null,
        IReadOnlyList<(string Home, string Away)>? matchups = null, bool? hazards = null)
    {
        seeds = seeds is { Count: > 0 } ? seeds : Seeds;
        matchups = matchups is { Count: > 0 } ? matchups : Matchups;
        // Hazards on first: the rows the report has always filed, then the switch's rows beside them.
        var states = hazards is { } only ? new[] { only } : new[] { true, false };
        if (!content.Parks.ContainsKey(ControlPark))
            throw new InvalidOperationException(
                $"The park-factors cohort reports against '{ControlPark}' (FD-13); this catalog has no such park.");

        // The catalog, never a literal list: a park added to data is measured without a code change.
        // Control park first, then the rest by id, so the rows are the same order on every run.
        var parks = content.Parks.Keys
            .OrderBy(id => id == ControlPark ? 0 : 1)
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var tallies = new Dictionary<(string Park, bool Hazards, bool Night), Tally>();
        foreach (var parkId in parks)
        foreach (var on in states)
        foreach (var night in new[] { false, true })
        {
            var tally = new Tally();
            foreach (var (home, away) in matchups)
            foreach (var seed in seeds)
            {
                var match = Match.Exhibition(content, home, away, innings: Innings, seed: seed, parkId: parkId, night: night, hazards: on);
                if (match.Park.Id != parkId)
                    throw new InvalidOperationException($"Park '{parkId}' did not load; the match fell back to {match.Park.Id}.");
                if (match.Hazards != on)
                    throw new InvalidOperationException($"The match at {parkId} played hazards {(match.Hazards ? "on" : "off")}; the cell asked for {(on ? "on" : "off")}.");
                match.AutoPlayGame();
                if (!match.Over) throw new InvalidOperationException($"Cohort game did not finish at {parkId}");
                tally.Add(match);
            }
            tallies[(parkId, on, night)] = tally;
        }

        var rows = new List<ParkFactorRow>();
        foreach (var parkId in parks)
        foreach (var on in states)
        foreach (var night in new[] { false, true })
        {
            var t = tallies[(parkId, on, night)];
            // Against the control park in the same condition: hazards off is read against Harbor with
            // hazards off, so the control stays the unit in all four.
            var control = tallies[(ControlPark, on, night)];
            var isControl = parkId == ControlPark;
            rows.Add(new ParkFactorRow(
                parkId, content.MustPark(parkId).Name, ConditionName(night, on), t.Games,
                Per(t.Runs, t.Games), Per(t.HomeTeamRuns, t.Games), Per(t.AwayTeamRuns, t.Games),
                Per(t.Singles, t.Games), Per(t.Doubles, t.Games), Per(t.Triples, t.Games),
                Per(t.HomeRuns, t.Games), Per(t.GroundRuleDoubles, t.Games),
                Per(t.FlyOuts, t.Games), Per(t.GroundOuts, t.Games),
                Per(t.Strikeouts, t.Games), Per(t.Walks, t.Games),
                Factor(isControl, t.Runs, t.Games, control.Runs, control.Games),
                Factor(isControl, t.HomeRuns, t.Games, control.HomeRuns, control.Games),
                Night: night, Hazards: on));
        }

        return new ParkFactorReport(
            SchemaVersion: 2,
            Cohort: Name,
            Root: content.Root.Provenance,
            ControlPark: ControlPark,
            Innings: Innings,
            Seats: "CPU both sides",
            Seeds: seeds.ToArray(),
            Matchups: matchups.Select(m => $"{m.Away} at {m.Home}").ToArray(),
            Conditions: rows.Select(r => r.Condition).Distinct().ToArray(),
            GamesPerParkPerCondition: matchups.Count * seeds.Count,
            Games: rows.Sum(r => r.Games),
            Rows: rows,
            Acceptance: "None. This is a report, not a gate (FD-13): Harbor is the only calibrated park, every other park "
                + "is measured against it and filed, and no test asserts a factor. A factor outside the FD-02 direction is a "
                + "finding for a PR body, never an expectation to edit.",
            Limitations: "The seed is shared across parks but the games diverge after the first ball in play, so a cell is N "
                + "independent samples, not N paired plays. Read a run or home-run factor inside about 0.15 of 1.00 as noise, "
                + "and rare kinds (triples, ground-rule doubles) as noisier still; the cohort size that would read a 10 % "
                + "effect is open (implementation map section 5 Q10). Three-inning games, CPU on both sides, difficulty "
                + "normal. Every condition is compared within itself, so the control park is 1.00 in each: a hazards-off "
                + "row is read against the control park with hazards off. Hazards off removes the instances whose pattern "
                + "counts as a hazard (FD-10) and nothing else, so a park keeps its size, fence, walls, air, ground and night "
                + "window, and a park with no such instance plays the same games in both states.");

        static double Per(int n, int games) => games == 0 ? 0 : Math.Round((double)n / games, 2);

        // The control park is the unit, so its factors are 1.00 by definition. Elsewhere a factor the
        // control never measured (no home run in the whole cell) is null, not a fabricated zero.
        static double? Factor(bool isControl, int mine, int games, int control, int controlGames)
        {
            if (isControl) return 1.0;
            if (control == 0 || games == 0 || controlGames == 0) return null;
            return Math.Round(((double)mine / games) / ((double)control / controlGames), 2);
        }
    }

    sealed class Tally
    {
        public int Games, HomeTeamRuns, AwayTeamRuns, Singles, Doubles, Triples, HomeRuns,
            GroundRuleDoubles, FlyOuts, GroundOuts, Strikeouts, Walks;

        public int Runs => HomeTeamRuns + AwayTeamRuns;

        public void Add(Match match)
        {
            Games++;
            HomeTeamRuns += match.HomeScore;
            AwayTeamRuns += match.AwayScore;
            foreach (var play in match.Log)
            {
                switch (play.Kind)
                {
                    case PlayKind.Single: Singles++; break;
                    case PlayKind.Double: Doubles++; break;
                    case PlayKind.Triple: Triples++; break;
                    case PlayKind.HomeRun: HomeRuns++; break;
                    case PlayKind.FlyOut: FlyOuts++; break;
                    case PlayKind.GroundOut: GroundOuts++; break;
                    case PlayKind.Strikeout: Strikeouts++; break;
                    case PlayKind.Walk: Walks++; break;
                }
                // A typed outcome, not the caption: the ground-rule double is a Double that left on a hop.
                if (play.Outcome?.GroundRuleDouble == true) GroundRuleDoubles++;
            }
        }
    }
}

/// <summary>
/// One park in one condition. <see cref="HomeTeamRunsPerGame"/> is runs scored by the home side;
/// <see cref="HomeRunsPerGame"/> is the ball over the fence. A factor is this row against the
/// control park's row in the <em>same</em> condition, so the control is 1.00 in each.
/// <see cref="Condition"/> is the name (<see cref="ParkFactorCohort.ConditionName"/>);
/// <see cref="Night"/> and <see cref="Hazards"/> are the same condition as typed facts, last so a
/// hazards-on row serialises as it did before the switch with one key added.
/// </summary>
public sealed record ParkFactorRow(string Park, string Name, string Condition, int Games,
    double RunsPerGame, double HomeTeamRunsPerGame, double AwayTeamRunsPerGame,
    double SinglesPerGame, double DoublesPerGame, double TriplesPerGame, double HomeRunsPerGame,
    double GroundRuleDoublesPerGame, double FlyOutsPerGame, double GroundOutsPerGame,
    double StrikeoutsPerGame, double WalksPerGame, double? RunFactor, double? HomeRunFactor,
    bool Night, bool Hazards);

/// <summary>
/// Schema 2 (F4-h, #858): the rows carry a <c>hazards</c> fact, <see cref="Conditions"/> names the
/// conditions in row order, and a full run has four of them, not two.
/// </summary>
public sealed record ParkFactorReport(int SchemaVersion, string Cohort, string Root, string ControlPark,
    int Innings, string Seats, IReadOnlyList<int> Seeds, IReadOnlyList<string> Matchups,
    IReadOnlyList<string> Conditions,
    int GamesPerParkPerCondition, int Games, IReadOnlyList<ParkFactorRow> Rows,
    string Acceptance, string Limitations)
{
    public string ToJson() => JsonSerializer.Serialize(this, PlayTrace.IndentedJson);

    /// <summary>The same rows a person can read: one table per condition, in row order, control park first.</summary>
    public string Table()
    {
        var text = new StringBuilder();
        text.AppendLine(Root);
        text.AppendLine($"{Cohort}  seeds {string.Join(",", Seeds)}  {Matchups.Count} matchups  {Innings} innings  {Seats}"
            + $"  {GamesPerParkPerCondition} games per park per condition  {Games} games  control {ControlPark}");
        foreach (var condition in Conditions)
        {
            text.AppendLine();
            text.AppendLine(condition.ToUpperInvariant());
            text.AppendLine($"{"park",-16} {"games",5} {"runs/g",7} {"xHarbor",8} {"HR/g",6} {"xHarbor",8} "
                + $"{"1B/g",6} {"2B/g",6} {"3B/g",6} {"GRD/g",6} {"FO/g",6} {"GO/g",6} {"K/g",6} {"BB/g",6}");
            foreach (var r in Rows.Where(r => r.Condition == condition))
                text.AppendLine($"{r.Park,-16} {r.Games,5} {r.RunsPerGame,7:0.00} {Show(r.RunFactor),8} "
                    + $"{r.HomeRunsPerGame,6:0.00} {Show(r.HomeRunFactor),8} {r.SinglesPerGame,6:0.00} "
                    + $"{r.DoublesPerGame,6:0.00} {r.TriplesPerGame,6:0.00} {r.GroundRuleDoublesPerGame,6:0.00} "
                    + $"{r.FlyOutsPerGame,6:0.00} {r.GroundOutsPerGame,6:0.00} {r.StrikeoutsPerGame,6:0.00} "
                    + $"{r.WalksPerGame,6:0.00}");
        }
        text.AppendLine();
        text.AppendLine("A report, not a gate (FD-13). Nothing here was tuned.");
        return text.ToString();

        static string Show(double? factor) => factor is { } f ? f.ToString("0.00") : "-";
    }
}
