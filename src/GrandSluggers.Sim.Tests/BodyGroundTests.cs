using System.Security.Cryptography;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class BodyGroundTests : IClassFixture<BodyGroundTests.Roots>
{
    const double Frame = 1.0 / 60.0;
    readonly Roots _roots;

    public BodyGroundTests(Roots roots) => _roots = roots;

    static readonly ContentCatalog Shipped = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Shipped.Root.Shipped,
        Path.GetFullPath(Path.Combine(Shipped.Root.Shipped, "..", "trials", "c80"))));

    static readonly (int Frames, double X, double Y)[] Script = [(100, 1, 0), (60, 0, -1), (45, 0, 1)];

    [Fact]
    public void AtOneTheStickGlovesStepIsThePreChangeBits()
    {
        Assert.Equal("176a9d3491fbc74419efabe9e6418aaae3bfa7cd885b5f2dc1375ceb27eaf8fb",
            Hash(StickRun(Shipped, Shipped.Parks["harbor-diamond"], Script).SelectMany(p => new[] { p.X, p.Z })));
        Assert.Equal("09668725c6b0a072150fa406dc7ee4de10ac721eae029dadd8a798c4603d0713",
            Hash(StickRun(_roots.LawOn, _roots.LawOn.Parks["harbor-diamond"], Script).SelectMany(p => new[] { p.X, p.Z })));
    }

    [Fact]
    public void AtOneThePlannersRouteIsThePreChangeRoute()
    {
        Assert.Equal(new FieldingPursuit.Route(-25.1953125, 118.7890625, 1.25, 16.823202477577286, 18, 1, true, false, 0),
            PlanRoute(Shipped.Rules, Shipped.Parks["harbor-diamond"]));
        var ramped = new FieldingPursuit.Route(-26.081249999999997, 120.85624999999999, 1.3, 16.172964033379905, 18, 1.05, true, false, 0.1);
        Assert.Equal(ramped, PlanRoute(_roots.LawOn.Rules, _roots.LawOn.Parks["harbor-diamond"]));
        Assert.Equal(ramped, PlanRoute(Trial.Rules, Trial.Parks["harbor-diamond"]));
    }

    [Fact]
    public void AtOneTheRunnersPathIsThePreChangePath()
    {
        const string expected = "a4085fb291cabed947d5711852099c27c3a7e5a8073577a274f22e927ff24203";
        Assert.Equal(expected, Hash(RunnerPath(Shipped.Rules)));
        Assert.Equal(expected, Hash(RunnerPath(Trial.Rules)));
    }

    // ---------------------------------------------------------------------------------

    static List<(double X, double Z)> StickRun(ContentCatalog catalog, Park park, (int Frames, double X, double Y)[] script)
    {
        var home = catalog.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = catalog.Team("Offense", "zig", "boom", "jester", "grit", "soot", "nugget", "pip", "gull", "marlow");
        var match = new Match(catalog, away, home, park, innings: 3, seed: 1);
        var hit = FlightFixtures.Landing(match.Park, 300, 34, -8, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var seats = new LiveSeats(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, seats, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        var track = new List<(double X, double Z)> { live.Fielders["CF"] };
        foreach (var (frames, x, y) in script)
            for (var i = 0; i < frames; i++)
            {
                live.Apply(LivePlayCommand.Tick(Frame, new LivePadInput(StickX: x, StickY: y), LivePadInput.Dead, false, LivePlayCommandSource.Human));
                Assert.True(live.Active);
                Assert.Equal("CF", live.GlovePos);
                track.Add(live.Fielders["CF"]);
            }
        return track;
    }

    static FieldingPursuit.Route PlanRoute(RulesTable rules, Park park)
    {
        var path = new List<Sample>();
        for (var i = 0; i <= 80; i++)
        {
            var t = i * 0.05;
            var d = 60 * t - 5 * t * t;
            path.Add(new Sample(t, d, 0, -0.375 * d, 60 + 0.875 * d));
        }
        var who = Shipped.Must("ashlord");
        var preview = FlightFixtures.Preview(who, "SS", BattedBallClass.Grounder, 0, path[^1].X, path[^1].Z);
        return FieldingPursuit.Plan(preview, park, path, 0.25, -42, 118, 18, rules, readySec: 0.25);
    }

    static List<double> RunnerPath(RulesTable rules)
    {
        var values = new List<double>();
        var batter = Runner.BatterRunner(Shipped.Must("rio"), HomeSet.BatterX, HomeSet.BatterZ);
        var t = 0.0;
        for (var i = 0; i < 400; i++)
        {
            RunnerSystem.Tick([batter], Frame, new RunnerTickContext(t += Frame, 0, FlyState.None, 0, _ => false, _ => false), rules);
            values.Add(batter.Feet);
            values.Add(batter.OverrunFt);
            values.Add((double)batter.Phase);
        }
        var runner = new Runner(Shipped.Must("vale"), 1);
        runner.BeginPlay(forced: false, tagAndGo: false);
        runner.Send(2);
        t = 0;
        for (var i = 0; i < 400; i++)
        {
            RunnerSystem.Tick([runner], Frame, new RunnerTickContext(t += Frame, 0, FlyState.None, 0, _ => false, bag => bag == 2), rules);
            values.Add(runner.Feet);
            values.Add((double)runner.Phase);
            values.Add(runner.Bag);
        }
        return values;
    }

    static string Hash(IEnumerable<double> values) =>
        Convert.ToHexString(SHA256.HashData(values.SelectMany(v => BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(v))).ToArray())).ToLowerInvariant();

    public sealed class Roots : IDisposable
    {
        readonly List<string> _dirs = [];

        public Roots()
        {
            LawOn = Load(json => { }, fielding => { fielding["chase"]!["accelSec"] = 0.2; fielding["chase"]!["brakeSec"] = 0.1; });
        }

        public ContentCatalog LawOn { get; }

        ContentCatalog Load(Action<JsonObject> grounds, Action<JsonObject> fielding)
        {
            var root = Path.Combine(Path.GetTempPath(), "grand-sluggers-body-ground-" + Guid.NewGuid().ToString("N"));
            _dirs.Add(root);
            Copy(ContentCatalog.Load().Root.Shipped, root);
            Change(Path.Combine(root, RulesTable.Directory, "grounds.json"), grounds);
            Change(Path.Combine(root, RulesTable.Directory, "fielding.json"), fielding);
            return ContentCatalog.Load(new DataRoot(root));
        }

        static void Change(string path, Action<JsonObject> change)
        {
            var json = JsonNode.Parse(File.ReadAllText(path), null, new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        static void Copy(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }

        public void Dispose()
        {
            foreach (var dir in _dirs)
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
