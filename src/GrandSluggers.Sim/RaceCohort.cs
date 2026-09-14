namespace GrandSluggers.Sim;

/// <summary>Predeclared sampling plans for #693. Runs the existing match, without changing any coefficient.</summary>
public static class RaceCohort
{
    public static readonly IReadOnlyList<string> Names = new[] { "s29", "harbor-calibration", "harbor-validation" };
    public static readonly IReadOnlyList<int> CalibrationSeeds = new[] { 1, 2, 3, 4, 5 };
    public static readonly IReadOnlyList<int> ValidationSeeds = new[] { 1001, 1002, 1003, 1004, 1005 };
    static readonly (string, string)[] Pairs = { ("rio", "ashlord"), ("zig", "konga"), ("fenn", "brondo"), ("konga", "rio"), ("vale", "brondo") };

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
                : Match.Exhibition(content, home, away, innings: 3, seed: seed, parkId: "harbor-diamond");
            match.AutoPlayGame();
            if (!match.Over) throw new InvalidOperationException("Cohort game did not finish");
            games.Add(new(seed, home, away, match.Park.Id, PlayTraceIdentity.Capture(match), match.HomeScore, match.AwayScore,
                match.Log.GroupBy(p => p.Kind.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                match.Log.Count(p => (p.Outcome?.OutsMade.Count ?? 0) >= 2)));
        }
        return new(1, name, seeds.ToArray(), games, games.Average(g => g.HomeRuns), games.Average(g => g.AwayRuns),
            name == "s29" ? "accepted F693-06: home and away means separately 1.8–5" : "observation only; no Harbor scoring band accepted",
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
