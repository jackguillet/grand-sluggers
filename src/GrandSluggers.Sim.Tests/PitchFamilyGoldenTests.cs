using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The bit-identity rail for the pitch family library (#810, PH-02-R2).
///
/// Moving the fastball and the changeup off a <c>bool</c> and onto authored rows in
/// <c>pitching.json</c> must not move one flight. So the delivered ball — <see cref="PitchFlight.Point"/>
/// at 23 points of the flight, <see cref="PitchFlight.Crossing"/>, and
/// <see cref="AtBatResolver.PitchSpeedMph"/> — is sampled over a grid of charge, stick break, rubber
/// walk, aim, Pitch stat, Nice! release and every star pitch that has a shape branch, and the
/// doubles are stored <b>exactly</b> (IEEE-754 bits, <see cref="BitConverter.DoubleToInt64Bits"/>)
/// in <c>fixtures/pitch-family-golden.json</c>.
///
/// The fixture was captured on the pre-#810 code, where a changeup was spelled three ways
/// (<c>Type: "changeup"</c>, the <c>Changeup: true</c> modifier, and both). #810 deletes the bool,
/// so the generator's call sites move to the family id — <b>the expected numbers do not</b>, and the
/// committed file stays byte-for-byte what the old code produced. That is the whole point: the next
/// family (P1-d: curveball / slider / sinker) is a row, and this file is what says it did not move
/// the two that already fly.
///
/// The fixture is regenerated only on purpose:
/// <c>GRAND_SLUGGERS_WRITE_PITCH_GOLDEN=1 dotnet test --filter "FullyQualifiedName~PitchFamilyGolden"</c>.
/// A regeneration that changes a single byte is a behaviour change and has to be argued, not merged.
/// </summary>
public sealed class PitchFamilyGoldenTests
{
    /// <summary>Set to 1 to rewrite the fixture from the current code. Never set in CI.</summary>
    public const string WriteVariable = "GRAND_SLUGGERS_WRITE_PITCH_GOLDEN";

    // The grid is written as literals, not read from the rules table, on purpose: a sample point
    // that moved with a tuned number would quietly re-aim the whole fixture. 0.62 is the shipped
    // changeup hangUntil, so the three rows around it straddle the seam of the hang/dump split.
    const double HangSeam = 0.62;

    static readonly double[] UGrid =
    [
        0, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6,
        HangSeam - 1e-9, HangSeam, HangSeam + 1e-9, 0.65, 0.7, 0.8, 0.9, 0.95, 0.99, 1
    ];
    static readonly double[] ShapeU = [0, 0.3, HangSeam - 1e-9, HangSeam, 0.7, 1];
    static readonly double[] StarU = [0, 0.3, HangSeam, 0.8, 1];
    static readonly double[] Charges = [0, 0.3, 0.55, 1];
    static readonly double[] Breaks = [-1, -0.4, 0, 0.7, 1];
    static readonly double[] CrossBreaks = [-1, 0, 1];
    static readonly double[] Rubbers = [-1, 0, 0.6];
    static readonly (double X, double Y)[] Aims = [(0, 0), (0.35, -0.6), (-0.5, 0.4)];
    static readonly int[] Stats = [1, 5, 10];
    static readonly string?[] StarIds = [null, "heatball", "prismball", "charmball", "phonyball", "caskball"];

    /// <summary>
    /// The three spellings of the two shipped deliveries as the pre-#810 API took them, and the
    /// family each one actually flew as. #810 keeps the three call sites and the three keys; the
    /// third and second now say the same family out loud instead of through a bool.
    /// </summary>
    static readonly string[] SpellingKeys = ["f#0", "c#1", "c#2"];

    static PitchCommand Command(int spelling, double charge = 0, bool star = false,
        double aimX = 0, double aimY = 0, double breakX = 0, double rubberX = 0, bool nice = false) =>
        spelling switch
        {
            0 => new PitchCommand(PitchFamily.Fastball, charge, star, aimX, aimY, breakX,
                RubberX: rubberX, Nice: nice),
            1 => new PitchCommand(PitchFamily.Changeup, charge, star, aimX, aimY, breakX,
                RubberX: rubberX, Nice: nice),
            _ => new PitchCommand(PitchFamily.Fastball, charge, star, aimX, aimY, breakX,
                Changeup: true, RubberX: rubberX, Nice: nice)
        };

    [Fact]
    public void TheShippedFastballAndChangeupFlyTheStoredBitsExactly()
    {
        // The golden pins world feet, and Z rides on Diamond.Mound, which is process-wide: the
        // compact copy is a different diamond and would be a different (equally correct) fixture.
        if (TestRoot.Compact) return;

        var stored = Stored();
        var now = Capture();

        var drift = new List<string>();
        foreach (var (key, value) in now)
        {
            if (!stored.TryGetValue(key, out var was)) drift.Add($"{key}: not in the fixture");
            else if (was != value) drift.Add($"{key}: fixture {was} vs now {value} ({Readable(was)} vs {Readable(value)})");
        }
        foreach (var key in stored.Keys)
            if (!now.ContainsKey(key)) drift.Add($"{key}: the fixture has it, this build does not sample it");

        Assert.Equal(stored.Count, now.Count);
        Assert.Empty(drift);
    }

    /// <summary>
    /// The library's two authored rows are exactly the numbers the old <c>speed</c> / <c>shapes</c>
    /// sections carried, so the golden above is an argument and not a coincidence.
    /// </summary>
    [Fact]
    public void TheGoldenCoversEveryFlightTheTwoAuthoredFamiliesCanProduce()
    {
        var keys = Capture().Keys.ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        foreach (var spelling in SpellingKeys)
            Assert.Contains(keys, k => k.Contains("|" + spelling + "|", StringComparison.Ordinal));
        Assert.Contains(keys, k => k.StartsWith("mph|", StringComparison.Ordinal));
        Assert.Contains(keys, k => k.StartsWith("cross|", StringComparison.Ordinal));
        Assert.Contains(keys, k => k.StartsWith("star|", StringComparison.Ordinal));
    }

    [Fact]
    public void RegenerateOnlyWhenTheEnvironmentAsksForIt()
    {
        if (Environment.GetEnvironmentVariable(WriteVariable) != "1") return;
        if (TestRoot.Compact) throw new InvalidOperationException("regenerate on the shipped diamond, not the compact copy");

        var document = new JsonObject
        {
            ["what"] = "Bit-identical pitch flights for the authored families (#810, PH-02-R2). Captured before the "
                       + "family library replaced PitchCommand.Changeup; every value must survive the refactor unchanged.",
            ["encoding"] = "IEEE-754 doubles as 16 lower-case hex digits (BitConverter.DoubleToInt64Bits), space separated.",
            ["regenerate"] = WriteVariable + "=1 dotnet test --filter \"FullyQualifiedName~PitchFamilyGolden\"",
            ["rows"] = new JsonObject(Capture().Select(r => new KeyValuePair<string, JsonNode?>(r.Key, r.Value)))
        };
        File.WriteAllText(FixturePath,
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    // ---- the grid ------------------------------------------------------------------------------

    static Dictionary<string, string> Capture()
    {
        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        var rules = Shipped;

        // The shape itself: every point of the flight, at each charge and each stick break.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var charge in Charges)
                foreach (var breakX in Breaks)
                    foreach (var u in UGrid)
                    {
                        var pitch = Command(s, charge, breakX: breakX);
                        var p = PitchFlight.Point(pitch, u, rules: rules);
                        rows[$"pt|{SpellingKeys[s]}|c{Name(charge)}|b{Name(breakX)}|u{Name(u)}"] = Bits(p.X, p.Y, p.Z);
                    }

        // Where the pitcher stands and where the pitch is aimed, at the seam of the hang.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var rubberX in Rubbers)
                foreach (var (aimX, aimY) in Aims)
                    foreach (var u in ShapeU)
                    {
                        var pitch = Command(s, aimX: aimX, aimY: aimY, rubberX: rubberX);
                        var p = PitchFlight.Point(pitch, u, rules: rules);
                        rows[$"aim|{SpellingKeys[s]}|r{Name(rubberX)}|x{Name(aimX)}|y{Name(aimY)}|u{Name(u)}"] = Bits(p.X, p.Y, p.Z);
                    }

        // The star pitches that bend the shown ball, plus an armed id on an unstarred command.
        for (var s = 0; s < SpellingKeys.Length; s++)
        {
            foreach (var star in StarIds)
                foreach (var u in StarU)
                {
                    var pitch = Command(s, star: star is not null);
                    var p = PitchFlight.Point(pitch, u, star, rules: rules);
                    rows[$"star|{SpellingKeys[s]}|{star ?? "none"}|u{Name(u)}"] = Bits(p.X, p.Y, p.Z);
                }
            var unarmed = Command(s);
            rows[$"star|{SpellingKeys[s]}|heatball-unarmed|u{Name(0.8)}"] =
                Bits(PitchFlight.Point(unarmed, 0.8, "heatball", rules: rules));
        }

        // Speed: base, the Pitch stat, the charge, the Nice! band, the star multiplier.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var charge in Charges)
                foreach (var stat in Stats)
                    foreach (var nice in new[] { false, true })
                        foreach (var star in StarIds)
                        {
                            var pitch = Command(s, charge, star: star is not null, nice: nice);
                            var mph = AtBatResolver.PitchSpeedMph(pitch, stat, rules, star);
                            rows[$"mph|{SpellingKeys[s]}|c{Name(charge)}|p{stat}|{(nice ? "nice" : "plain")}|{star ?? "none"}"] = Bits(mph);
                        }

        // The crossing the umpire, the tell and the CPU batter all read.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var charge in Charges)
                foreach (var breakX in CrossBreaks)
                    foreach (var rubberX in Rubbers)
                        foreach (var (aimX, aimY) in Aims.Take(2))
                            foreach (var star in new string?[] { null, "prismball", "caskball" })
                            {
                                var pitch = Command(s, charge, star is not null, aimX, aimY, breakX, rubberX);
                                var c = PitchFlight.Crossing(pitch, star, rules);
                                rows[$"cross|{SpellingKeys[s]}|c{Name(charge)}|b{Name(breakX)}|r{Name(rubberX)}|x{Name(aimX)}|{star ?? "none"}"] =
                                    Bits(c.X, c.Y);
                            }

        return rows;
    }

    // ---- exact doubles -------------------------------------------------------------------------

    static string Bits(params double[] values) =>
        string.Join(' ', values.Select(v => BitConverter.DoubleToInt64Bits(v).ToString("x16")));

    static string Bits((double X, double Y, double Z) p) => Bits(p.X, p.Y, p.Z);

    static string Readable(string bits) =>
        string.Join(' ', bits.Split(' ')
            .Select(b => BitConverter.Int64BitsToDouble(Convert.ToInt64(b, 16)).ToString("R")));

    /// <summary>A grid coordinate in the key: round-trip exact, so no sample point is ambiguous.</summary>
    static string Name(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    // ---- the file ------------------------------------------------------------------------------

    static readonly ContentCatalog ShippedContent = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    /// <summary>Named explicitly: the golden is the shipped library, whatever overlay a process carries.</summary>
    static RulesTable Shipped => ShippedContent.Rules;

    static string FixturePath => Path.Combine(
        Path.GetFullPath(Path.Combine(ShippedContent.Root.Shipped, "..")),
        "src", "GrandSluggers.Sim.Tests", "fixtures", "pitch-family-golden.json");

    static Dictionary<string, string> Stored()
    {
        var json = JsonNode.Parse(File.ReadAllText(FixturePath))!.AsObject();
        var rows = json["rows"]!.AsObject();
        return rows.ToDictionary(r => r.Key, r => r.Value!.GetValue<string>(), StringComparer.Ordinal);
    }
}
