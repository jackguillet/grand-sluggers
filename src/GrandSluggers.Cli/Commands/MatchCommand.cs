using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;

namespace GrandSluggers.Cli;

sealed class MatchCommand : Command
{
    public override string Name => "match";

    public override IReadOnlyList<string> Usage =>
    [
        "match [--home rio] [--away ashlord] [--park harbor-diamond] [--seed N] [--night] [--hazards on|off] [--stars on|off] [--difficulty easy|normal|hard] [--trace [file]]",
        "match --cohort s29|harbor-calibration|harbor-validation",
        "match --cohort park-factors [--table] [--hazards on|off]",
    ];

    public override IReadOnlyList<Option> Options =>
    [
        new("--home", Arity.Value),
        new("--away", Arity.Value),
        new("--park", Arity.Value, "-p"),
        new("--seed", Arity.Value, "-s"),
        new("--night", Arity.Switch),
        new("--hazards", Arity.Value),
        new("--stars", Arity.Value),
        new("--difficulty", Arity.Value, "-d"),
        new("--trace", Arity.OptionalValue),
        new("--cohort", Arity.Value),
        new("--table", Arity.Switch),
    ];

    /// <summary>A cohort fixes its own seeds, parks and matchups; only the park-factors report narrows.</summary>
    static readonly string[] CohortFlags = ["--cohort", "--table", "--hazards"];

    public override int Run(ContentCatalog content, CommandLine line) =>
        line.Has("--cohort") ? RunCohort(content, line) : RunGame(content, line);

    static int RunCohort(ContentCatalog content, CommandLine line)
    {
        // --table is the park-factors report read by a person instead of filed as JSON, and --hazards on|off
        // narrows it to one of its two hazard states.
        var names = RaceCohort.Names.Append(ParkFactorCohort.Name).ToArray();
        var cohort = line.OneOf("--cohort", names)!;
        if (line.Given.FirstOrDefault(f => !CohortFlags.Contains(f)) is { } fixedFlag)
            throw new UsageException($"{fixedFlag} does not go with --cohort; each cohort fixes its seeds, parks and matchups");
        var narrows = line.Has("--table") || line.Has("--hazards");
        if (narrows && cohort != ParkFactorCohort.Name)
            throw new UsageException($"--table and --hazards narrow only --cohort {ParkFactorCohort.Name}");
        if (cohort != ParkFactorCohort.Name)
        {
            Console.WriteLine(RaceCohort.Run(content, cohort).ToJson());
            return 0;
        }
        bool? hazards = line.Has("--hazards") ? line.OnOff("--hazards", true) : null;
        var report = ParkFactorCohort.Run(content, hazards: hazards);
        // The table always reaches a person: on stdout when asked for, beside the provenance line on stderr
        // otherwise, so the filed JSON stays byte-comparable between runs.
        if (line.Has("--table")) Console.Write(report.Table());
        else { Console.Error.Write(report.Table()); Console.WriteLine(report.ToJson()); }
        return 0;
    }

    static int RunGame(ContentCatalog content, CommandLine line)
    {
        // A park, a captain or a difficulty the catalog does not have is a stop, not a silent fall back to
        // Harbor or normal (#820): a run that prints a Final under the wrong game is worse than no run at all.
        var seed = line.Int("--seed", 1);
        var parkId = line.Text("--park", "")!;
        var home = line.Text("--home", "rio")!;
        var away = line.Text("--away", "ashlord")!;
        var difficulty = line.OneOf("--difficulty", CpuRules.Levels);
        var night = line.Has("--night");
        var hazards = line.OnOff("--hazards", true);
        var stars = line.OnOff("--stars", true);
        var trace = line.Has("--trace") ? line.Text("--trace") ?? "-" : null;

        var match = string.IsNullOrEmpty(parkId)
            ? Match.Exhibition(content, home, away, innings: 3, seed: seed, night: night, difficulty: difficulty, hazards: hazards, stars: stars)
            : Match.Exhibition(content, home, away, innings: 3, seed: seed, parkId: parkId, night: night, difficulty: difficulty, hazards: hazards, stars: stars);
        if (trace is not null) match.Tracing = true;
        var log = Console.Out;
        if (trace == "-") Console.SetOut(Console.Error);
        try
        {
            Console.WriteLine($"{match.Away.Name} at {match.Home.Name}  {match.Park.Name}  seed {seed}  {(match.Night ? "night" : "day")}  {match.Difficulty}  hazards {(match.Hazards ? "on" : "off")}");
            Console.WriteLine($"stars {(match.StarsEnabled ? "on" : "off")}  away {match.AwayStars:0.#}  home {match.HomeStars:0.#}");
            while (!match.Over)
            {
                var half = $"{(match.Top ? "T" : "B")}{match.Inning}";
                var ev = match.AutoPlay();
                Console.WriteLine($"{half,-3} {match.AwayScore}-{match.HomeScore}  {ev.Kind,-11}  {ev.Caption}");
            }
            var mvp = match.Scorebook.Mvp();
            Console.WriteLine($"Final  {match.Away.Name} {match.AwayScore}  {match.Home.Name} {match.HomeScore}");
            Console.WriteLine($"MVP  {mvp.Who.Name} ({mvp.Points}) — {mvp.Why}");
        }
        finally
        {
            if (trace == "-") Console.SetOut(log);
        }
        if (trace is null) return 0;
        var json = match.TraceLog().ToJson();
        if (trace == "-") log.WriteLine(json);
        else File.WriteAllText(trace, json);
        return 0;
    }
}
