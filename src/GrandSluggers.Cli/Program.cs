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
    default:
        Console.WriteLine("""
            Grand Sluggers sim
              roster
              team [spark-allstars|ember-court|mixed-rivals]
              chem <character-id>
              at-bat [ember|spark] [--seed N]
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

static void PrintTeam(ContentCatalog content, string id)
{
    var team = id.ToLowerInvariant() switch
    {
        "ember" or "ember-court" => PresetTeams.EmberCourt(content),
        "mixed" or "mixed-rivals" => PresetTeams.MixedRivals(content),
        _ => PresetTeams.SparkAllStars(content)
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
