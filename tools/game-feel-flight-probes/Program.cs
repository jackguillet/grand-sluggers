using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;

// Research only: call the production integrator with in-memory counterfactual rules.
// This is neither a second flight model nor a complete compact-game simulation.
var root = FindRoot();
var mode = args.SingleOrDefault() ?? "--check";
if (mode is not ("--check" or "--write"))
    throw new ArgumentException("Use --check or --write.");
var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var fixturePath = "docs/research/game-feel-708-flight-inputs.json";
var outputPath = Path.Combine(root, "docs/research/game-feel-708-flight-derived.json");
var input = JsonNode.Parse(File.ReadAllText(Path.Combine(root, fixturePath)))!;
var candidates = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "docs/research/game-feel-708-candidates.json")))!;
var compact = candidates["profiles"]!.AsArray().Single(p => p!["id"]!.GetValue<string>() == "C80")!;
var fences = compact["fencesFt"]!.AsArray().Select(v => v!.GetValue<int>()).ToArray();
var rules = RulesTable.Load(Path.Combine(root, "data"));
var trialDrag = candidates["flightBudgetResearch"]!["acceptedDirection"]!["dragTrialTo"]!.GetValue<double>();
// The control is the research's own full-size drag, not the shipped table's: the game shipped the trial's 0.0040, and
// this evidence compares the two numbers the decision was made between.
var controlDrag = candidates["flightBudgetResearch"]!["acceptedDirection"]!["dragFrom"]!.GetValue<double>();
var rows = new List<object>();
var regression = new Dictionary<string, (double Carry, double Time100, string? FenceOutcome)>();

foreach (var (label, drag) in new[] { ("control-drag", controlDrag), ("trial-drag", trialDrag) })
{
    var node = JsonSerializer.SerializeToNode(rules, options)!;
    node["flight"]!["drag"] = drag;
    var counterfactual = node.Deserialize<RulesTable>(options)!;
    foreach (var probe in input["probes"]!.AsArray())
    {
        var id = probe!["id"]!.GetValue<string>();
        var exit = probe["exitMph"]!.GetValue<double>();
        var launch = probe["launchDeg"]!.GetValue<double>();
        var open = BallFlight.Trajectory(exit, launch, 0, counterfactual);
        var stations = input["groundStationsFt"]!.AsArray().Select(v => v!.GetValue<double>())
            .Select(d => new { distanceFt = d, arrival = AtDistance(open, d) }).ToArray();
        var impacts = Enumerable.Range(1, open.Count - 1)
            .Where(i => open[i].Event == SampleEvent.Ground && open[i - 1].Height > 0)
            .Take(3).Select(i => new { timeSec = R(open[i].T), distanceFt = R(open[i].Dist) }).ToArray();
        var sprays = probe["kind"]!.GetValue<string>() == "ground"
            ? new[] { 0.0 } : input["sprayDegrees"]!.AsArray().Select(v => v!.GetValue<double>()).ToArray();
        foreach (var spray in sprays)
        {
            var park = new Park("research-c80", "Research C80", "", "grass", fences[0], fences[1], fences[2],
                0, Array.Empty<Hazard>(), FenceHeightFt: candidates["spatialPolicy"]!["wallHeightFt"]!.GetValue<double>());
            var clipped = BallFlight.Trajectory(exit, launch, spray, park, counterfactual);
            var boundary = FieldBounds.Of(park, Rules.Default);
            object? crossing = null;
            string? outcome = null;
            var grounded = false;
            for (var i = 1; i < clipped.Count; i++)
            {
                var current = clipped[i];
                if (current.Event is SampleEvent.Wall or SampleEvent.Fence or SampleEvent.FoulWall or SampleEvent.Stands)
                {
                    var previous = clipped[i - 1];
                    // For walls, the integrator stores exact crossing height after resolving the carom.
                    // For an above-wall crossing, interpolate on its unmodified segment.
                    var hit = boundary.Cross(previous.X, previous.Z, current.X, current.Z);
                    var height = current.Event is SampleEvent.Wall or SampleEvent.FoulWall
                        ? current.Height
                        : hit is { } h ? previous.Height + (current.Height - previous.Height) * h.U
                        : throw new InvalidOperationException("Above-wall event lacks a boundary crossing.");
                    outcome = current.Event == SampleEvent.Fence
                        ? grounded ? "ground-rule-double" : "home-run"
                        : current.Event.ToString().ToLowerInvariant();
                    crossing = new { outcome, heightFt = R(height), wallHeightFt = park.FenceHeightFt,
                        groundedBeforeCrossing = grounded, eventSampleTimeSec = R(current.T) };
                    if (outcome == "home-run" && height <= park.FenceHeightFt)
                        throw new InvalidOperationException("A homer must cross above the wall before bouncing.");
                    break;
                }
                grounded |= current.Event == SampleEvent.Ground;
            }
            rows.Add(new { id, dragProfile = label, drag, exitMph = exit, launchDeg = launch, sprayDeg = spray,
                windMph = 0, firstLandingDistanceFt = R(BallFlight.FirstLandingDist(open, rules: rules)),
                firstLandingTimeSec = R(BallFlight.HangTime(open, rules: rules)),
                fenceDistanceAlongSprayFt = R(boundary.RadiusAt(spray)), fenceCrossing = crossing,
                groundStations = probe["kind"]!.GetValue<string>() == "ground" ? stations : null,
                groundImpacts = probe["kind"]!.GetValue<string>() == "ground" ? impacts : null });
            if (spray == 0)
                regression[$"{label}/{id}"] = (BallFlight.FirstLandingDist(open, rules: rules),
                    AtDistance(open, 100)?.TimeSec ?? double.NaN, outcome);
        }
    }
}

// Regression cases for the reviewed false claims, not a balance or human acceptance gate.
var oldGround = regression["control-drag/hard-infield-80-8"];
var newGround = regression["trial-drag/hard-infield-80-8"];
if (!(newGround.Time100 > oldGround.Time100 + .1 && newGround.Carry < oldGround.Carry - 10))
    throw new InvalidOperationException("Revisit the documented grounder sensitivity; its representative counterexample changed.");
var heat = regression["trial-drag/p5-heat-perfect-charge-lift"];
if (!(heat.Carry > fences[1] && heat.FenceOutcome == "wall"))
    throw new InvalidOperationException("Revisit the Heat Swing correction: carry alone is not a homer verdict.");
if (regression["trial-drag/p10-perfect-charge-lift"].FenceOutcome != "home-run")
    throw new InvalidOperationException("The documented strongest-bat center homer probe changed.");

var sources = new[] { fixturePath, "docs/research/game-feel-708-candidates.json", "data/rules/flight.json",
    "data/rules/batting.json", "data/abilities/star-skills.json", "src/GrandSluggers.Sim/BallFlight.cs",
    "src/GrandSluggers.Sim/FieldBounds.cs", "src/GrandSluggers.Sim/Rules.cs",
    "src/GrandSluggers.Sim/BattedBall.cs", "src/GrandSluggers.Sim/AtBatResolver.cs",
    "src/GrandSluggers.Sim/HarborWall.cs", "src/GrandSluggers.Sim/Models.cs",
    "tools/game-feel-flight-probes/GameFeelFlightProbes.csproj", "tools/game-feel-flight-probes/Program.cs" };
var hashes = sources.ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, p)))).ToLowerInvariant());
var report = new { schemaVersion = 1, status = "production-flight-model-probes-not-whole-game-validation",
    inputs = fixturePath, candidate = "C80", trialDrag, sourceSha256 = hashes,
    limitations = input["limitations"], rows };
var json = JsonSerializer.Serialize(report, options) + "\n";
if (mode == "--write") File.WriteAllText(outputPath, json);
else if (!File.Exists(outputPath) || File.ReadAllText(outputPath) != json)
    throw new InvalidOperationException("Flight evidence is stale. Review inputs/source changes, then regenerate with --write.");
Console.WriteLine($"#708: {rows.Count} production flight probes {(mode == "--write" ? "written" : "verified")}; no whole-game or human gate passed.");

static double R(double value) => Math.Round(value, 6);
static Arrival? AtDistance(IReadOnlyList<Sample> samples, double distance)
{
    for (var i = 1; i < samples.Count; i++)
    {
        var a = samples[i - 1]; var b = samples[i];
        if (a.Dist >= distance || b.Dist < distance || b.Dist <= a.Dist) continue;
        var fraction = (distance - a.Dist) / (b.Dist - a.Dist);
        var dt = b.T - a.T;
        var horizontal = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Z - a.Z, 2)) / dt;
        var vertical = (b.Height - a.Height) / dt;
        return new Arrival(R(a.T + fraction * dt), R(a.Height + fraction * (b.Height - a.Height)),
            R(horizontal), R(vertical), b.Event.ToString(),
            "Segment velocity in play-clock units; an impact-spanning segment includes the collision.");
    }
    return null;
}
static string FindRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "docs/research/game-feel-708-candidates.json"))) return dir.FullName;
    throw new DirectoryNotFoundException("Run from the repository's probe project.");
}
record Arrival(double TimeSec, double HeightFt, double HorizontalSpeedFtPerPlaySecond,
    double VerticalSpeedFtPerPlaySecond, string SegmentEndEvent, string VelocityMethod);
