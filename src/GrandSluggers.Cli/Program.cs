using GrandSluggers.Sim;

var content = ContentCatalog.Load();
var cmd = args.Length > 0 ? args[0] : "help";

switch (cmd)
{
    case "team":
        PrintTeam(content, args.ElementAtOrDefault(1) ?? "spark-allstars");
        break;
    case "at-bat":
        SimAtBat(content, args.ElementAtOrDefault(1) ?? "ember", Seed(args));
        break;
    case "match":
        RunMatch(content, Seed(args), ParkId(args), HomeId(args), AwayId(args));
        break;
    case "challenge":
        RunChallenge(content, CaptainId(args), Seed(args));
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
    default:
        Console.WriteLine("""
            Grand Sluggers sim
              roster
              team [spark-allstars|ember-court|mixed-rivals|rio|vale|zig|brondo|konga|ashlord]
              chem <character-id>
              at-bat [ember|spark] [--seed N]
              match [--home rio] [--away ashlord] [--park harbor-diamond] [--seed N]
              challenge [--captain rio] [--seed N]
              art
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

static string ParkId(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] is "--park" or "-p")
            return args[i + 1];
    return "";
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
    Console.WriteLine($"AUDIO  {art.Audio.Count} events");
    Console.WriteLine($"PARKS  {art.Parks.Count} kit slots ({art.Parks.Count(p => p.Placed)} placed)");
    Console.WriteLine($"FOLDERS {art.Folders.Count}");
    var errors = art.Validate(content);
    if (errors.Count == 0)
        Console.WriteLine("OK     catalog matches roster, clips, parks");
    else
    {
        Console.WriteLine("FAIL   " + errors.Count + " errors");
        foreach (var e in errors) Console.WriteLine("  - " + e);
        Environment.ExitCode = 1;
    }
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

    var stars = content.Chemistry.StartingStars(team);
    var avg = content.Chemistry.AverageWithCaptain(team);
    Console.WriteLine($"{team.Name}  captain {team.Captain.Name}  chemistry avg {avg:0}  starting stars {stars}/5");
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

static void RunMatch(ContentCatalog content, int seed, string parkId, string home, string away)
{
    var match = string.IsNullOrEmpty(parkId)
        ? Match.Exhibition(content, home, away, innings: 3, seed: seed)
        : Match.Exhibition(content, home, away, innings: 3, seed: seed, parkId: parkId);
    Console.WriteLine($"{match.Away.Name} at {match.Home.Name}  {match.Park.Name}  seed {seed}");
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
        var timing = rng.NextDouble() * 10 - 4; // -4..6 frames
        var input = new AtBatInput(
            Pitcher: pitcher,
            Batter: batter,
            OnDeck: onDeck,
            RunnersOn: [],
            PitchType: "fastball",
            ChargePitch: false,
            ChargeSwing: i % 3 == 0,
            TimingErrorFrames: timing,
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
