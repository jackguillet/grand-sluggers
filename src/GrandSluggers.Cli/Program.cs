using GrandSluggers.Sim;

ContentCatalog content;
try
{
    content = ContentCatalog.Load();
}
catch (Exception ex) when (ex is IOException or InvalidDataException)
{
    // A data root or trial overlay this run named and cannot have. The message names the offending
    // path, so report it as a failure rather than a crash — and non-zero, so nothing reads on.
    Console.Error.WriteLine("grand-sluggers: " + ex.Message);
    return 1;
}

// Provenance, on stderr so stdout stays exactly the game's own output and a control run of any
// command is still byte-comparable. A trace whose data root has to be reconstructed from memory
// is not evidence, so every run says which root and which trial overlay produced it (#716).
Console.Error.WriteLine(content.Root.Provenance);

var cmd = args.Length > 0 ? args[0] : "help";

switch (cmd)
{
    case "tutorials":
        try
        {
            var tutorials = TutorialCatalog.Load(content);
            if (args.Length == 3 && args[1] == "--replay")
            {
                var recording = System.Text.Json.JsonSerializer.Deserialize<TutorialRecording>(File.ReadAllText(args[2]));
                if (recording is null) throw new InvalidDataException("Empty tutorial recording.");
                var run = TutorialSession.Replay(content, tutorials, recording);
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { tutorials.Profile, run.Lesson.Id, run.Phase, run.Feedback, run.Successes, requiredSuccesses = TutorialProgress.RequiredSuccesses, run.Passed, run.HumanThrows }));
                Environment.ExitCode = run.Feedback?.Success == true ? 0 : 1;
            }
            else if (args.Length == 1)
            {
                Console.WriteLine($"TUTORIALS {tutorials.Profile} — {tutorials.Mechanics.Length} mechanics; {tutorials.Lessons.Count(l => l.Status == "implemented")} headless lessons (human learning gate separate)");
                foreach (var lesson in tutorials.Lessons)
                    Console.WriteLine($"{lesson.Id,-22} {lesson.Status,-12} #{lesson.Issue} {lesson.Title}");
            }
            else throw new InvalidDataException("Use tutorials [--replay recording.json].");
        }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException or ArgumentException or InvalidOperationException)
        { Console.Error.WriteLine("tutorials: " + e.Message); Environment.ExitCode = 1; }
        break;
    case "team":
        PrintTeam(content, args.ElementAtOrDefault(1) ?? "spark-allstars");
        break;
    case "at-bat":
        SimAtBat(content, args.ElementAtOrDefault(1) ?? "ember", Seed(args));
        break;
    case "match":
        var cohortAt = Array.IndexOf(args, "--cohort");
        if (cohortAt >= 0)
        {
            // --table is the park-factors report read by a person instead of filed as JSON, and
            // --hazards on|off narrows it to one of its two hazard states; every other argument is
            // refused, because a cohort fixes its own seeds, parks and matchups.
            var names = RaceCohort.Names.Append(ParkFactorCohort.Name).ToArray();
            var cohort = args.ElementAtOrDefault(2) ?? "";
            var extra = args.Skip(3).ToList();
            var table = extra.Remove("--table");
            bool? cohortHazards = null;
            var hazardsAt = extra.IndexOf("--hazards");
            if (hazardsAt >= 0)
            {
                cohortHazards = HazardsValue(extra.ElementAtOrDefault(hazardsAt + 1));
                extra.RemoveRange(hazardsAt, Math.Min(2, extra.Count - hazardsAt));
            }
            var narrows = table || hazardsAt >= 0;
            if (cohortAt != 1 || !names.Contains(cohort) || extra.Count > 0
                || (narrows && cohort != ParkFactorCohort.Name) || (hazardsAt >= 0 && cohortHazards is null))
            {
                Console.Error.WriteLine($"Use match --cohort {string.Join("|", names)} without overrides; each cohort fixes its seeds, parks and matchups. Add --table to read {ParkFactorCohort.Name} as a table instead of JSON, and --hazards on|off to report one hazard state instead of both.");
                Environment.ExitCode = 2;
                break;
            }
            if (cohort == ParkFactorCohort.Name)
            {
                var report = ParkFactorCohort.Run(content, hazards: cohortHazards);
                // The table always reaches a person: on stdout when asked for, beside the provenance
                // line on stderr otherwise, so the filed JSON stays byte-comparable between runs.
                if (table) Console.Write(report.Table());
                else { Console.Error.Write(report.Table()); Console.WriteLine(report.ToJson()); }
                break;
            }
            Console.WriteLine(RaceCohort.Run(content, args[cohortAt + 1]).ToJson());
            break;
        }
        // A park or a captain the catalog does not have is a stop, not a silent fall back to Harbor
        // (#820): a run that prints a Final under the wrong park is worse than no run at all. A
        // hazards value that is neither on nor off is the same stop, not a quiet default.
        var hazardsArg = Array.IndexOf(args, "--hazards");
        var hazards = hazardsArg < 0 ? true : HazardsValue(args.ElementAtOrDefault(hazardsArg + 1));
        if (hazards is null)
        {
            Console.Error.WriteLine("match: use --hazards on|off (default on).");
            Environment.ExitCode = 2;
            break;
        }
        try { RunMatch(content, Seed(args), ParkId(args), HomeId(args), AwayId(args), Difficulty(args), TraceArg(args), Night(args), hazards.Value); }
        catch (KeyNotFoundException e) { Console.Error.WriteLine("match: " + e.Message); Environment.ExitCode = 2; }
        break;
    case "challenge":
        try { RunChallenge(content, CaptainId(args), Seed(args)); }
        catch (KeyNotFoundException e) { Console.Error.WriteLine("challenge: " + e.Message); Environment.ExitCode = 2; }
        break;
    case "chem":
        DumpChem(content, args.ElementAtOrDefault(1) ?? "rio");
        break;
    case "roster":
        foreach (var c in content.Characters.Values.OrderBy(c => c.Faction).ThenByDescending(c => c.Captain).ThenBy(c => c.Name))
        {
            var cap = c.Captain ? "C" : " ";
            Console.WriteLine($"{cap} {c.Name,-14} {c.Faction,-10} P{c.Stats.Pitch} B{c.Stats.Bat} F{c.Stats.Field} R{c.Stats.Run}  {c.StarPitch}/{c.StarSwing}  {c.FieldAbility}");
        }
        break;
    case "art":
        PrintArt(content);
        break;
    case "protocol":
        PrintProtocol(content);
        break;
    case "stills":
        PrintStills(content);
        break;
    case "stages":
        PrintStages(content);
        break;
    default:
        Console.WriteLine("""
            Grand Sluggers sim
              tutorials [--replay recording.json]
              roster
              team [spark-allstars|ember-court|mixed-rivals|rio|vale|zig|brondo|konga|ashlord]
              chem <character-id>
              at-bat [ember|spark] [--seed N]
              match [--home rio] [--away ashlord] [--park harbor-diamond] [--seed N] [--night] [--hazards on|off] [--difficulty easy|normal|hard] [--trace [file]]
              match --cohort s29|harbor-calibration|harbor-validation
              match --cohort park-factors [--table] [--hazards on|off]
              challenge [--captain rio] [--seed N]
              art
              protocol
              stills
              stages
            """);
        break;
}

static int Seed(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] is "--seed" or "-s" && int.TryParse(args[i + 1], out var n))
            return n;
    return 1;
}

/// <summary>
/// A night game (§14). The park's night rules — Crystal's contact window, Ember's breath reach,
/// Funfair's chompers — could not be measured headless before this flag existed.
/// </summary>
static bool Night(string[] args) => args.Contains("--night");

/// <summary>
/// Park hazards on or off (FD-10, §14): <c>--hazards off</c> plays the park with its hazard instances
/// removed and nothing else changed. On is the default. Null for anything that is neither, so the
/// caller stops rather than guessing which game was meant.
/// </summary>
static bool? HazardsValue(string? value) => value switch
{
    "on" => true,
    "off" => false,
    _ => null
};

static string ParkId(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] is "--park" or "-p")
            return args[i + 1];
    return "";
}

static string? Difficulty(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] is "--difficulty" or "-d")
            return args[i + 1];
    return null;
}

static string HomeId(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == "--home")
            return args[i + 1];
    return "rio";
}

static string AwayId(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == "--away")
            return args[i + 1];
    return "ashlord";
}

static string? TraceArg(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] != "--trace") continue;
        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            return args[i + 1];
        return "-";
    }
    return null;
}

static string CaptainId(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] is "--captain" or "--home")
            return args[i + 1];
    return args.ElementAtOrDefault(1) is { Length: > 0 } a && !a.StartsWith('-') ? a : "rio";
}

static void PrintArt(ContentCatalog content)
{
    var art = content.Art;
    Console.WriteLine($"RIG    {art.Rig.Id}  bones {art.Rig.Bones.Count}  events {string.Join(",", art.Rig.Events)}");
    Console.WriteLine($"CLIPS  {art.Clips.Count}  {string.Join(" ", art.Clips.Select(c => c.Id))}");
    Console.WriteLine($"SKINS  {art.Skins.Count} captains authored, role players inherit body type");
    Console.WriteLine($"VFX    {art.Vfx.Count} events");
    Console.WriteLine($"AUDIO  {art.Audio.Count} events ({art.Audio.Count(e => e.Authored)} authored)");
    Console.WriteLine($"PARKS  {art.Parks.Count} kit slots ({art.Parks.Count(p => p.Placed)} placed)");
    foreach (var kit in art.Parks)
    {
        var empty = kit.Empty;
        var filled = ParkKitSlots.All.Count - empty.Count;
        Console.WriteLine($"  {kit.Id,-15} {filled,2}/{ParkKitSlots.All.Count} filled  empty: {(empty.Count == 0 ? "none" : string.Join(" ", empty))}");
    }
    Console.WriteLine($"FOLDERS {art.Folders.Count}");
    var errors = art.Validate(content)
        .Concat(DebugProtocol.Validate(content.Root))
        .Concat(DualStills.Validate(content.Root))
        .Concat(DccStages.Validate(content.Root))
        .ToList();
    if (errors.Count == 0)
        Console.WriteLine("OK     catalog matches roster, clips, parks, debug protocol, dual stills, dcc stages");
    else
    {
        Console.WriteLine("FAIL   " + errors.Count + " errors");
        foreach (var e in errors) Console.WriteLine("  - " + e);
        Environment.ExitCode = 1;
    }
}

static void PrintProtocol(ContentCatalog content)
{
    var protocol = DebugProtocol.Load(content.Root, new List<string>());
    Console.WriteLine($"PROTOCOL {protocol.Entries.Count} entries");
    foreach (var row in protocol.Entries)
    {
        var promoted = string.IsNullOrWhiteSpace(row.Promoted) ? "-" : row.Promoted;
        Console.WriteLine($"  {row.Id,-32} {row.Stage,-14} {row.Issue,-8} {promoted}");
    }
    var errors = DebugProtocol.Validate(content.Root);
    if (errors.Count == 0)
        Console.WriteLine("OK     debug protocol matches spec §2");
    else
    {
        Console.WriteLine("FAIL   " + errors.Count + " errors");
        foreach (var e in errors) Console.WriteLine("  - " + e);
        Environment.ExitCode = 1;
    }
}

static void PrintStills(ContentCatalog content)
{
    var catalog = DualStills.Load(content.Root, new List<string>());
    Console.WriteLine($"STILLS drop {catalog.Drop}  kinds {catalog.Kinds.Count}");
    foreach (var kind in catalog.Kinds)
        Console.WriteLine($"  {kind.Id,-12} dcc {kind.Dcc,-22} in-game {string.Join(" ", kind.InGame)}");
    Console.WriteLine($"  critic {catalog.Critic.Skill}  mayPassLook {catalog.Critic.MayPassLook}  mayClose188 {catalog.Critic.MayClose188}");
    var errors = DualStills.Validate(content.Root);
    if (errors.Count == 0)
        Console.WriteLine("OK     dual stills match spec §4; critic files, does not pass");
    else
    {
        Console.WriteLine("FAIL   " + errors.Count + " errors");
        foreach (var e in errors) Console.WriteLine("  - " + e);
        Environment.ExitCode = 1;
    }
}

static void PrintStages(ContentCatalog content)
{
    var catalog = DccStages.Load(content.Root, new List<string>());
    Console.WriteLine($"STAGES {catalog.Stages.Count}  one-shot {catalog.OneShot}");
    foreach (var stage in catalog.Stages)
    {
        Console.WriteLine($"  {stage.N} {stage.Id,-10} {stage.Kind,-6} character {Lane(stage.Character)}");
        Console.WriteLine($"                     harbor    {Lane(stage.Harbor)}");
    }
    var errors = DccStages.Validate(content.Root);
    if (errors.Count == 0)
        Console.WriteLine("OK     dcc stages match spec §6; one-shot banned");
    else
    {
        Console.WriteLine("FAIL   " + errors.Count + " errors");
        foreach (var e in errors) Console.WriteLine("  - " + e);
        Environment.ExitCode = 1;
    }

    static string Lane(DccStageLane lane) =>
        lane.Skip ? "—" : $"{lane.Checkpoint}  {lane.Flag}";
}

static void PrintTeam(ContentCatalog content, string id)
{
    var team = id.ToLowerInvariant() switch
    {
        "ember" or "ember-court" => PresetTeams.EmberCourt(content),
        "mixed" or "mixed-rivals" => PresetTeams.MixedRivals(content),
        "spark" or "spark-allstars" => PresetTeams.SparkAllStars(content),
        _ => PresetTeams.ForCaptain(content, id)
    };

    // Chemistry pays off in the field; every team starts on the same Stars (PH-16-R16).
    var buddies = team.Roster.Count(c => c.Id != team.Captain.Id && content.Chemistry.Between(team.Captain, c) == Chemistry.Good);
    var rivals = team.Roster.Count(c => c.Id != team.Captain.Id && content.Chemistry.Between(team.Captain, c) == Chemistry.Bad);
    Console.WriteLine($"{team.Name}  captain {team.Captain.Name}  good {buddies} bad {rivals} with the captain  "
        + $"starting stars {content.Rules.Stars.StartingReserve}/{content.Rules.Stars.MeterMax:0} (every team)");
    Console.WriteLine($"{"",2} {"Name",-14} {"Fac",-10} {"vs C",-8} P B F R");
    foreach (var c in team.Roster)
    {
        var rel = c.Id == team.Captain.Id ? "captain" : content.Chemistry.Between(team.Captain, c).ToString().ToLowerInvariant();
        Console.WriteLine($"  {c.Name,-14} {c.Faction,-10} {rel,-8} {c.Stats.Pitch} {c.Stats.Bat} {c.Stats.Field} {c.Stats.Run}");
    }
}

static void DumpChem(ContentCatalog content, string id)
{
    var me = content.Must(id);
    Console.WriteLine($"{me.Name} ({me.Faction})");
    foreach (var other in content.Characters.Values.OrderBy(c => c.Name))
    {
        if (other.Id == me.Id) continue;
        var rel = content.Chemistry.Between(me, other);
        if (rel == Chemistry.Neutral) continue;
        var mark = rel == Chemistry.Good ? "+" : "-";
        Console.WriteLine($"  {mark} {other.Name,-14} {other.Faction}");
    }
}

static void RunMatch(ContentCatalog content, int seed, string parkId, string home, string away, string? difficulty, string? trace, bool night, bool hazards)
{
    var match = string.IsNullOrEmpty(parkId)
        ? Match.Exhibition(content, home, away, innings: 3, seed: seed, night: night, difficulty: difficulty, hazards: hazards)
        : Match.Exhibition(content, home, away, innings: 3, seed: seed, parkId: parkId, night: night, difficulty: difficulty, hazards: hazards);
    if (trace is not null) match.Tracing = true;
    var log = Console.Out;
    if (trace == "-") Console.SetOut(Console.Error);
    try
    {
        Console.WriteLine($"{match.Away.Name} at {match.Home.Name}  {match.Park.Name}  seed {seed}  {(match.Night ? "night" : "day")}  {match.Difficulty}  hazards {(match.Hazards ? "on" : "off")}");
        Console.WriteLine($"stars  away {match.AwayStars:0.#}  home {match.HomeStars:0.#}");
        while (!match.Over)
        {
            var half = $"{(match.Top ? "T" : "B")}{match.Inning}";
            var ev = match.AutoPlay();
            Console.WriteLine($"{half,-3} {match.AwayScore}-{match.HomeScore}  {ev.Kind,-11}  {ev.Caption}");
        }
        var mvp = match.Mvp();
        Console.WriteLine($"Final  {match.Away.Name} {match.AwayScore}  {match.Home.Name} {match.HomeScore}");
        Console.WriteLine($"MVP  {mvp.Who.Name} ({mvp.Points}) — {mvp.Why}");
    }
    finally
    {
        if (trace == "-") Console.SetOut(log);
    }
    if (trace is null) return;
    var json = match.TraceLog().ToJson();
    if (trace == "-") log.WriteLine(json);
    else File.WriteAllText(trace, json);
}

static void RunChallenge(ContentCatalog content, string captainId, int seed)
{
    var run = Challenge.Start(content, captainId);
    var match = run.MakeMatch(content, innings: 3, seed: seed);
    Console.WriteLine($"Challenge  {match.Home.Name} vs {match.Away.Name}  at {match.Park.Name}  seed {seed}");
    Console.WriteLine($"owned {run.Owned.Count}  first rival {match.Away.Captain.Name}");
    match.AutoPlayGame();
    var recruit = run.Resolve(match);
    Console.WriteLine($"Final  {match.Away.Name} {match.AwayScore}  {match.Home.Name} {match.HomeScore}");
    if (recruit is not null)
        Console.WriteLine($"WIN  recruited {recruit.Name}  roster {run.Owned.Count}");
    else
        Console.WriteLine("LOSS  no recruit");
}

static void SimAtBat(ContentCatalog content, string matchup, int seed)
{
    var park = content.Parks["harbor-diamond"];
    var ember = matchup.StartsWith("ember", StringComparison.OrdinalIgnoreCase);
    var pitcher = content.Must(ember ? "ashlord" : "rio");
    var batter = content.Must(ember ? "rio" : "ashlord");
    var onDeck = content.Must(ember ? "nico" : "cinder");
    var resolver = new AtBatResolver(content.Chemistry);
    var rng = new Random(seed);

    Console.WriteLine($"{pitcher.Name} vs {batter.Name} at {park.Name}  (seed {seed})");
    Console.WriteLine($"chem pitcher-batter: {content.Chemistry.Between(pitcher, batter)}  batter-on-deck: {content.Chemistry.Between(batter, onDeck)}");

    for (var i = 0; i < 8; i++)
    {
        var timing = rng.NextDouble() * 10 - 5; // -5..5 frames around the 9-frame slap window
        var input = new AtBatInput(
            Pitcher: pitcher,
            Batter: batter,
            OnDeck: onDeck,
            RunnersOn: [],
            ChargePitch: false,
            ChangeupPitch: false,
            TimingErrorFrames: timing,
            Charge01: i % 3 == 0 ? 1 : 0,
            UseStarPitch: i == 6,
            UseStarSwing: i == 7,
            Bat: ember ? content.Bats.GetValueOrDefault("harbor-lumber") : content.Bats.GetValueOrDefault("furnace-club"),
            PitcherStamina: 80);
        var r = resolver.Resolve(input, park, rng);
        var extra = r.HomeRun ? "  HR" : r.InPlay ? $"  {r.CarryFt:0} ft" : "";
        var item = r.ChemistryItemOffered ? "  [item]" : "";
        var star = r.StarSwingUsed is not null ? $"  *{r.StarSwingUsed}" : r.StarPitchUsed is not null ? $"  *{r.StarPitchUsed}" : "";
        Console.WriteLine($"  t={timing,5:0.0}  {r.Quality,-8}  {r.ExitVeloMph,5:0} mph  {r.LaunchDeg,4:0}°{extra}{item}{star}");
    }
}

return Environment.ExitCode;
