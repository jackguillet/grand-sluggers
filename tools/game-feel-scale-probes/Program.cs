using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;

// #730 research only: what scales when the diamond shrinks, and by what.
// Every figure here comes from the production classifier with counterfactual rule tables.
// It is not a simulation, it is not a home-run rate, and it passes no human gate.
var root = FindRoot();
var mode = args.SingleOrDefault() ?? "--check";
if (mode is not ("--check" or "--write")) throw new ArgumentException("Use --check or --write.");

var json = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var outputPath = Path.Combine(root, "docs/research/game-feel-730-scale-derived.json");

// On demand, not in CI. It compared the full-size data/ against trials/c80; 3e (2026-09-22) promoted that overlay into
// data/ and deleted it, so the probe needs a full-size root rebuilt (the pre-promotion data/) before it can run again.
if (!Directory.Exists(Path.Combine(root, "trials/c80")))
    throw new InvalidOperationException("#730 scale probes compare the full-size root against trials/c80, which 3e promoted into data/; rebuild a full-size root first.");
var shipped = ContentCatalog.Load(Path.Combine(root, "data"));
var trial = ContentCatalog.Load(new DataRoot(Path.Combine(root, "data"), Path.Combine(root, "trials/c80")));

string[] parkIds = ["canopy-yard", "crystal-rink", "ember-keep", "funfair-park", "harbor-diamond", "rooftop-city"];
double[] sprays = [-44.9, -35, -22, -10, 0, 10, 22, 35, 44.9];

const double Basepath = 80.0 / 90.0;
const double Fence = 0.70;

// ---------------------------------------------------------------- the lip
var shippedLip = shipped.Rules.Flight.Classes.InfieldLipFt;
var lipOptions = new (string Id, double Ft)[]
{
    ("unchanged", shippedLip),
    ("basepath", Math.Round(shippedLip * Basepath, 2)),
    ("fence", Math.Round(shippedLip * Fence, 2))
};

var lipRows = new List<object>();
foreach (var (id, ft) in lipOptions)
{
    var rules = WithLip(trial.Rules, ft);
    var (pop, fly) = Shapes(trial, rules);
    lipRows.Add(new
    {
        option = id,
        lipFt = ft,
        popInCompact = pop,
        flyInCompact = fly,
        // Where the lip sits relative to a migrated Harbor.
        fractionOfCompactCentre = Math.Round(ft / trial.Parks["harbor-diamond"].CenterFenceFt, 4),
        fractionOfCompactPole = Math.Round(ft / trial.Parks["harbor-diamond"].LeftFenceFt, 4),
        // Which of the nine start spots the sim would call outfielders.
        outfieldStarts = Diamond.Order
            .Where(p => FieldingResolver.OutfieldGrass(Diamond.Positions[p].X, Diamond.Positions[p].Z, rules))
            .ToArray(),
        // Whether each hazard still sits in the zone its own migration used.
        hazardsInAForeignZone = ForeignZone(ft)
    });
}

var shippedShapes = Shapes(shipped, shipped.Rules);

// The lip is what tells the sim who is an infielder. A lip below a middle infielder's own
// radius reclassifies him, so the start spots put a floor under the lip decision.
var startRadii = new[] { "1B", "2B", "3B", "SS" }.Select(pos =>
{
    var (x, z) = Diamond.Positions[pos];
    return new
    {
        position = pos,
        radiusShippedFt = Math.Round(Math.Sqrt(x * x + z * z), 2),
        radiusIfScaledByBasepathFt = Math.Round(Math.Sqrt(x * x + z * z) * Basepath, 2)
    };
}).ToArray();

var lipFloorToday = startRadii.Max(r => r.radiusShippedFt);
var lipFloorIfStartsScale = startRadii.Max(r => r.radiusIfScaledByBasepathFt);

// The lip that leaves the compact game with the shipped game's pop count. Pops rise with the lip,
// so bisect rather than sweep: the full grid is 17,280 classifications per probe.
double lo = 100, hi = 200;
for (var i = 0; i < 12; i++)
{
    var mid = (lo + hi) / 2;
    if (Shapes(trial, WithLip(trial.Rules, mid)).Pop >= shippedShapes.Pop) hi = mid; else lo = mid;
}
var matching = Math.Round(hi, 1);

// ---------------------------------------------------------------- the foul wrap
var foulRows = new List<object>();
foreach (var (label, cat) in new[] { ("control", shipped), ("compact", trial) })
{
    int downTheLine = 0, foul = 0;
    foreach (var id in parkIds)
        foreach (var (exit, launch) in Grid(cat.Rules.Batting))
            foreach (var spray in sprays.Where(s => Math.Abs(s) > 40))
            {
                downTheLine++;
                if (BattedBall.Of(exit, launch, spray, cat.Parks[id], cat.Rules).Foul) foul++;
            }
    foulRows.Add(new { profile = label, downTheLine, foul, share = Math.Round((double)foul / downTheLine, 4) });
}

// ---------------------------------------------------------------- distances that scale with nothing
var absolutes = new List<object>();
foreach (var id in parkIds)
{
    var s = shipped.Parks[id];
    var t = trial.Parks[id];
    absolutes.Add(new
    {
        park = id,
        centreShippedFt = s.CenterFenceFt,
        centreCompactFt = t.CenterFenceFt,
        poleCompactFt = t.LeftFenceFt,
        // cpu.tagSecondMinCarryFt 250: past every compact pole makes the branch unreachable.
        tagSecondVsCompactPole = Math.Round(shipped.Rules.Running.Cpu.TagSecondMinCarryFt / t.LeftFenceFt, 4),
        tagThirdVsCompactCentre = Math.Round(shipped.Rules.Running.Cpu.TagThirdMinCarryFt / t.CenterFenceFt, 4),
        // fielding.throw.onTheFlyFt 200: the share of fair ground inside it, where no cutoff exists.
        insideCutoffShipped = Math.Round(FairShareWithin(s, shipped.Rules.Fielding.Throw.OnTheFlyFt), 4),
        insideCutoffCompact = Math.Round(FairShareWithin(t, shipped.Rules.Fielding.Throw.OnTheFlyFt), 4)
    });
}

// ---------------------------------------------------------------- hazard radii
var radiusRows = new List<object>();
foreach (var id in parkIds)
{
    var s = shipped.Parks[id];
    var t = trial.Parks[id];
    var fairShipped = FairArea(s);
    var fairCompact = FairArea(t);
    for (var i = 0; i < s.Hazards.Count; i++)
    {
        var h = s.Hazards[i];
        if (h.Radius <= 0) continue;
        radiusRows.Add(new
        {
            park = id,
            type = h.Type,
            radiusFt = h.Radius,
            shareShipped = Math.Round(Math.PI * h.Radius * h.Radius / fairShipped, 5),
            shareCompactUnscaled = Math.Round(Math.PI * h.Radius * h.Radius / fairCompact, 5),
            shareCompactFenceScaled = Math.Round(Math.PI * Math.Pow(h.Radius * Fence, 2) / fairCompact, 5),
            // Barrels and pipes are caught by radius + pad, so the pad decides how much scaling matters.
            // Each type reads the pad on its own hazards.json row.
            captureUnscaledFt = h.Type is HazardType.WarpPipe or HazardType.Barrel
                ? Math.Round(h.Radius + shipped.Rules.Hazards.Of(h.Type).ReachPadFt, 2) : h.Radius,
            captureFenceScaledFt = h.Type is HazardType.WarpPipe or HazardType.Barrel
                ? Math.Round(h.Radius * Fence + shipped.Rules.Hazards.Of(h.Type).ReachPadFt, 2)
                : Math.Round(h.Radius * Fence, 2)
        });
    }
}

// ---------------------------------------------------------------- fielder start spots
var startRows = new List<object>();
foreach (var (pos, bag) in new[] { ("1B", 1), ("2B", 2), ("3B", 3), ("SS", 2) })
{
    var (fx, fz) = Diamond.Positions[pos];
    startRows.Add(new
    {
        position = pos,
        coversBag = bag,
        gapShippedFt = Math.Round(BagGap(fx, fz, bag, shipped.Rules), 2),
        gapUnmigratedOnCompactFt = Math.Round(BagGap(fx, fz, bag, trial.Rules), 2),
        gapIfScaledByBasepathFt = Math.Round(BagGap(fx * Basepath, fz * Basepath, bag, trial.Rules), 2)
    });
}

var outfieldRows = new List<object>();
foreach (var pos in new[] { "LF", "CF", "RF" })
{
    var (fx, fz) = Diamond.Positions[pos];
    var radius = Math.Sqrt(fx * fx + fz * fz);
    var bearing = Math.Atan2(fx, fz) * 180.0 / Math.PI;
    var harborShipped = AtBatResolver.FenceAt(shipped.Parks["harbor-diamond"], bearing);
    var harborCompact = AtBatResolver.FenceAt(trial.Parks["harbor-diamond"], bearing);
    outfieldRows.Add(new
    {
        position = pos,
        startRadiusFt = Math.Round(radius, 2),
        bearingDeg = Math.Round(bearing, 2),
        fenceAtBearingShippedFt = Math.Round(harborShipped, 2),
        fenceAtBearingCompactFt = Math.Round(harborCompact, 2),
        fractionShipped = Math.Round(radius / harborShipped, 4),
        // Preserve bearing and the fraction of the fence at that bearing.
        radiusIfFractionPreservedFt = Math.Round(radius / harborShipped * harborCompact, 2),
        // What the un-migrated literal does today: it stands beyond the wall and gets clamped.
        feetBeyondCompactWall = Math.Round(radius - harborCompact, 2)
    });
}

var report = new
{
    schemaVersion = 1,
    issue = 730,
    status = "research-probes-not-simulation",
    note = "Counterfactual rule tables through the production classifier. No rate, no gate, no runtime change.",
    basepathScale = Math.Round(Basepath, 6),
    fenceScale = Fence,
    shippedLipFt = shippedLip,
    shippedShapes = new { pop = shippedShapes.Pop, fly = shippedShapes.Fly },
    infielderStartRadii = startRadii,
    lipFloorFt = new
    {
        ifStartsStayWhereTheyAre = lipFloorToday,
        ifStartsScaleByBasepath = lipFloorIfStartsScale,
        why = "below this the sim calls a middle infielder an outfielder, which moves him onto the outfield air multiplier"
    },
    lipThatKeepsTheShippedPopCountFt = matching,
    lipOptions = lipRows,
    foulWrap = foulRows,
    absoluteDistances = absolutes,
    hazardRadii = radiusRows,
    infieldStarts = startRows,
    outfieldStarts = outfieldRows,
    sourceSha256 = Sha(root,
        "data/rules/flight.json", "data/rules/fielding.json", "data/rules/running.json",
        "trials/c80/rules/infield.json", "trials/c80/rules/flight.json",
        "src/GrandSluggers.Sim/BattedBall.cs", "src/GrandSluggers.Sim/Fielding.cs",
        "src/GrandSluggers.Sim/Diamond.cs", "src/GrandSluggers.Sim/HarborWall.cs",
        "src/GrandSluggers.Sim/AtBatResolver.cs", "tools/game-feel-scale-probes/Program.cs")
};

var text = JsonSerializer.Serialize(report, json) + "\n";
if (mode == "--write") File.WriteAllText(outputPath, text);
else if (!File.Exists(outputPath) || File.ReadAllText(outputPath) != text)
    throw new InvalidOperationException("#730 scale evidence is stale. Review, then regenerate with --write.");
Console.WriteLine($"#730: {lipRows.Count} lip options, {radiusRows.Count} hazards, {startRows.Count + outfieldRows.Count} start spots "
    + $"{(mode == "--write" ? "written" : "verified")}; no simulation and no human gate.");

// ---------------------------------------------------------------- helpers

static RulesTable WithLip(RulesTable rules, double lipFt)
{
    var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var node = JsonSerializer.SerializeToNode(rules, opts)!;
    node["flight"]!["classes"]!["infieldLipFt"] = lipFt;
    return node.Deserialize<RulesTable>(opts)!;
}

static IEnumerable<(double Exit, double Launch)> Grid(BattingRules b)
{
    double[] qualities = [b.Quality.Slap.Nice, b.Quality.Slap.Perfect, b.Quality.Charge.Nice, b.Quality.Charge.Perfect];
    for (var power = 1; power <= 10; power++)
        foreach (var charged in new[] { false, true })
            foreach (var quality in qualities)
                foreach (var lift in new[] { 0.0, b.Launch.StickDeg })
                    yield return (
                        Math.Round((b.Exit.BaseMph + power * b.Exit.MphPerPower) * quality, 1),
                        Math.Round(b.Launch.LoftBaseDeg + (power - 5) * b.Launch.LoftPerPower
                            + (charged ? b.Charge.LoftDeg : 0) + lift, 1));
}

(int Pop, int Fly) Shapes(ContentCatalog cat, RulesTable rules)
{
    int pop = 0, fly = 0;
    foreach (var id in parkIds)
        foreach (var (exit, launch) in Grid(rules.Batting))
            foreach (var spray in sprays)
            {
                var shape = BattedBall.Of(exit, launch, spray, cat.Parks[id], rules).Shape;
                if (shape == BattedBallClass.Pop) pop++;
                else if (shape == BattedBallClass.Fly) fly++;
            }
    return (pop, fly);
}

string[] ForeignZone(double lipFt)
{
    var odd = new List<string>();
    foreach (var id in parkIds)
    {
        var s = shipped.Parks[id];
        var t = trial.Parks[id];
        for (var i = 0; i < s.Hazards.Count; i++)
        {
            var was = Diamond.Dist(0, 0, s.Hazards[i].X, s.Hazards[i].Z) >= lipFt;
            var now = Diamond.Dist(0, 0, t.Hazards[i].X, t.Hazards[i].Z) >= lipFt;
            if (was != now) odd.Add($"{id} {s.Hazards[i].Type}");
        }
    }
    return odd.ToArray();
}

static double BagGap(double fx, double fz, int bag, RulesTable rules)
{
    var i = rules.Infield;
    var (bx, bz) = bag switch
    {
        1 => (i.CornerFt, i.CornerFt),
        2 => (0.0, i.SecondFt),
        3 => (-i.CornerFt, i.CornerFt),
        _ => (0.0, 0.0)
    };
    return Diamond.Dist(fx, fz, bx, bz);
}

/// <summary>Fair ground inside the fence, integrated over the foul-line wedge.</summary>
static double FairArea(Park park)
{
    double area = 0;
    const int steps = 900;
    var span = 2 * AtBatResolver.FoulLineDeg;
    for (var i = 0; i < steps; i++)
    {
        var deg = -AtBatResolver.FoulLineDeg + span * (i + 0.5) / steps;
        var r = AtBatResolver.FenceAt(park, deg);
        area += 0.5 * r * r * (span / steps) * Math.PI / 180.0;
    }
    return area;
}

static double FairShareWithin(Park park, double ft)
{
    double inside = 0, all = 0;
    const int steps = 900;
    var span = 2 * AtBatResolver.FoulLineDeg;
    for (var i = 0; i < steps; i++)
    {
        var deg = -AtBatResolver.FoulLineDeg + span * (i + 0.5) / steps;
        var r = AtBatResolver.FenceAt(park, deg);
        var cut = Math.Min(r, ft);
        inside += 0.5 * cut * cut;
        all += 0.5 * r * r;
    }
    return inside / all;
}

static Dictionary<string, string> Sha(string root, params string[] names) =>
    names.ToDictionary(n => n, n => Convert.ToHexString(
        SHA256.HashData(File.ReadAllBytes(Path.Combine(root, n)))).ToLowerInvariant());

static string FindRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "data", "characters"))) return dir.FullName;
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("no data root");
}
