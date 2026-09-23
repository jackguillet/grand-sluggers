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
/// doubles are held <b>exactly</b> (IEEE-754 bits, <see cref="BitConverter.DoubleToInt64Bits"/>).
/// <c>fixtures/pitch-family-golden.json</c> was captured on the pre-#810 code, where a changeup was
/// spelled three ways (<c>Type: "changeup"</c>, the <c>Changeup: true</c> modifier, and both). #810
/// deletes the bool, so the generator's call sites move to the family id — <b>the expected numbers do
/// not</b>. That is the whole point: the next family (P1-d: curveball / slider / sinker) is a row,
/// and this file is what says it did not move the two that already fly.
///
/// <b>What is stored, and what is composed.</b> A fixture of raw doubles is only honest where the
/// arithmetic is. Y, Z and mph are pure add/multiply/clamp/table-lookup, so they are stored and
/// compared bit for bit. X is not always: <see cref="PitchFlight.BreakShiftFt"/> calls
/// <c>Math.Sin(u * π)</c>, and heatball / prismball / charmball add <c>Math.Sin(u * hz)</c> — and
/// <c>Math.Sin</c> differs by one ULP between macOS libm and glibc (this repository already met that
/// in #736). A stored X under break would pin the platform, not the pitch.
///
/// So for every sample the fixture stores the <b>sin-free</b> X — the identical delivery with no
/// break and no star, which is exactly what <see cref="PitchFlight.Shape"/> produced before this
/// refactor — together with the <b>damping decision</b> that delivery was given on the old code
/// (<c>charged || changeup</c>). The test then rebuilds X on the running platform out of those two
/// pinned facts plus the live <see cref="PitchFlight.BreakShiftFt"/> and star terms, in the same
/// operation order <see cref="PitchFlight.Point"/> writes them, and asserts the result equals the
/// real X <b>exactly</b> — no tolerance. Both sides call the same platform's <c>Math.Sin</c>, so the
/// equality is exact everywhere, while everything this refactor actually touched — the shape, the
/// drop, the damping flag that moved from a bool to <c>row.BreakDamped</c> — stays pinned to the
/// numbers the old code produced. A flipped <c>breakDamped</c> changes the shift tenfold and fails.
///
/// The fixture is regenerated only on purpose, and only from the pre-#810 sources:
/// <c>GRAND_SLUGGERS_WRITE_PITCH_GOLDEN=1 dotnet test --filter "FullyQualifiedName~PitchFamilyGolden"</c>.
/// A regeneration that changes a single byte is a behaviour change and has to be argued, not merged.
/// </summary>
[Trait("Kind", "Balance")]
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
            // Was `Type: "fastball", Changeup: true` — the modifier spelling #810 deleted. The
            // command it produced is this one, and the stored numbers say so.
            _ => new PitchCommand(PitchFamily.Changeup, charge, star, aimX, aimY, breakX,
                RubberX: rubberX, Nice: nice)
        };

    [Fact]
    public void TheShippedFastballAndChangeupFlyTheStoredBitsExactly()
    {
        // The golden pins world feet, and Z rides on Diamond.Mound, which is process-wide: the
        // compact copy is a different diamond and would be a different (equally correct) fixture.
        if (TestRoot.Compact) return;

        var stored = Stored();
        var drift = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sample in Samples())
        {
            seen.Add(sample.Key);
            if (!stored.TryGetValue(sample.Key, out var row))
            {
                drift.Add($"{sample.Key}: not in the fixture");
                continue;
            }
            foreach (var complaint in sample.Check(row))
                drift.Add($"{sample.Key}: {complaint}");
        }
        foreach (var key in stored.Keys)
            if (!seen.Contains(key)) drift.Add($"{key}: the fixture has it, this build does not sample it");

        Assert.Equal(stored.Count, seen.Count);
        Assert.Empty(drift);
    }

    /// <summary>
    /// The split the fixture rests on, stated as a test rather than only as a comment: every sample
    /// whose X is stored is one the platform's libm cannot reach, and every sample whose X is
    /// composed is one it can.
    /// </summary>
    [Fact]
    public void XIsStoredExactlyWhereverNoSineTouchesItAndComposedWhereverOneDoes()
    {
        if (TestRoot.Compact) return;

        var rules = Shipped;
        var stored = 0;
        var composed = 0;
        var moved = 0;
        foreach (var sample in Samples())
        {
            if (sample.Mph is not null) continue;
            var u = Math.Clamp(sample.Crossing ? 1 : sample.U, 0, 1);
            if (!sample.TouchesSine)
            {
                // No sine anywhere on the way to this X, so the fixture alone pins it: the stored
                // double plus, at most, phonyball's table constant. Nothing here can move with libm.
                Assert.Equal(
                    Bits(sample.StarX(sample.StoredXBase(rules), u, rules.Pitching.StarShapes)),
                    Bits(sample.ActualX(rules)));
                stored++;
            }
            else
            {
                // A sine does touch it. The stored double is the flight without the break and
                // without the wobble; the rest is rebuilt on the running platform, and the two are
                // only equal where the sine itself is zero (u = 0 and the seams).
                composed++;
                if (Bits(sample.StoredXBase(rules)) != Bits(sample.ActualX(rules))) moved++;
            }
        }
        Assert.True(stored > 500, $"samples whose X the fixture pins outright: {stored}");
        Assert.True(composed > 500, $"samples whose X is rebuilt: {composed}");
        Assert.True(moved > 500, $"rebuilt samples the sine actually moves: {moved}");
    }

    [Fact]
    public void TheGoldenCoversEveryFlightTheTwoAuthoredFamiliesCanProduce()
    {
        var keys = Samples().Select(s => s.Key).ToList();
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

        var rules = Shipped;
        var document = new JsonObject
        {
            ["what"] = What,
            ["encoding"] = Encoding,
            ["regenerate"] = WriteVariable + "=1 dotnet test --filter \"FullyQualifiedName~PitchFamilyGolden\"",
            ["rows"] = new JsonObject(Samples().Select(s =>
                new KeyValuePair<string, JsonNode?>(s.Key, s.Store(rules))))
        };
        File.WriteAllText(FixturePath,
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    internal const string What =
        "Bit-identical pitch flights for the authored families (#810, PH-02-R2). Captured before the family library "
        + "replaced PitchCommand.Changeup; every value must survive the refactor unchanged.";

    internal const string Encoding =
        "IEEE-754 doubles as 16 lower-case hex digits (BitConverter.DoubleToInt64Bits), space separated. "
        + "mph rows are one double. Flight rows are 'y z xBase damped' (crossing rows drop z): xBase is the same "
        + "delivery with no break and no star, which is the only part of X no Math.Sin touches, and damped is the "
        + "break damping (charged || changeup) the old code gave that delivery. The test rebuilds the real X from "
        + "those on the running platform, because Math.Sin differs by one ULP between macOS libm and glibc (#736).";

    // ---- the grid ------------------------------------------------------------------------------

    /// <summary>One row of the fixture: what to sample, what to store, and how to check it back.</summary>
    internal sealed record Sample(string Key, PitchCommand? Pitch = null, double U = 0, string? Star = null,
        bool Crossing = false, (int Stat, bool Nice)? Mph = null)
    {
        /// <summary>
        /// True when a <c>Math.Sin</c> stands between the rules table and this sample's X: a stick
        /// break (<see cref="PitchFlight.BreakShiftFt"/>) or one of the three wobbling star pitches.
        /// Phonyball shifts X too, but by a table constant, so it stays reproducible from the
        /// fixture alone; caskball lifts Y and never touches X.
        /// </summary>
        public bool TouchesSine =>
            Pitch is not null
            && (Pitch.BreakX * Pitch.BreakMul != 0 || (Pitch.Star && Star is "heatball" or "prismball" or "charmball"));

        PitchCommand Delivery => Pitch!;

        /// <summary>The delivery stripped of everything a sine touches: the shape alone.</summary>
        PitchCommand Bare => Delivery with { BreakX = 0, Star = false };

        public double ActualX(RulesTable r) => Crossing
            ? PitchFlight.Crossing(Delivery, Star, r).X
            : PitchFlight.Point(Delivery, U, Star, rules: r).X;

        public double StoredXBase(RulesTable r) => Crossing
            ? PitchFlight.Crossing(Bare, Star, r).X
            : PitchFlight.Point(Bare, U, Star, rules: r).X;

        /// <summary>
        /// The damping <see cref="PitchFlight.Point"/> hands <see cref="PitchFlight.BreakShiftFt"/>.
        /// On the pre-#810 code this read <c>charged || changeup</c>; the fixture holds that answer,
        /// and this is what the refactor has to keep agreeing with.
        /// </summary>
        public bool Damped(RulesTable r) =>
            ChargeFeel.IsCharge(Delivery.Charge01) || r.Pitching.Families.Of(Delivery.Type).BreakDamped;

        public string Store(RulesTable r)
        {
            if (Mph is { } m)
                return Bits(AtBatResolver.PitchSpeedMph(Delivery, m.Stat, r, Star));
            var parts = new List<string>();
            if (Crossing)
                parts.Add(Bits(PitchFlight.Crossing(Delivery, Star, r).Y));
            else
            {
                var p = PitchFlight.Point(Delivery, U, Star, rules: r);
                parts.Add(Bits(p.Y));
                parts.Add(Bits(p.Z));
            }
            parts.Add(Bits(StoredXBase(r)));
            parts.Add(Damped(r) ? "1" : "0");
            return string.Join(' ', parts);
        }

        /// <summary>Every way this sample disagrees with its stored row; empty is the pass.</summary>
        public IEnumerable<string> Check(string row)
        {
            var r = Shipped;
            var token = row.Split(' ');
            if (Mph is { } m)
            {
                var mph = Bits(AtBatResolver.PitchSpeedMph(Delivery, m.Stat, r, Star));
                if (token.Length != 1) yield return $"fixture row has {token.Length} values, expected 1";
                else if (token[0] != mph) yield return $"mph fixture {token[0]} vs now {mph} ({Readable(token[0])} vs {Readable(mph)})";
                yield break;
            }

            var want = Crossing ? 3 : 4;
            if (token.Length != want)
            {
                yield return $"fixture row has {token.Length} values, expected {want}";
                yield break;
            }

            var actualY = Crossing ? PitchFlight.Crossing(Delivery, Star, r).Y : PitchFlight.Point(Delivery, U, Star, rules: r).Y;
            if (Bits(actualY) != token[0])
                yield return $"Y fixture {token[0]} vs now {Bits(actualY)} ({Readable(token[0])} vs {actualY:R})";
            if (!Crossing)
            {
                var actualZ = PitchFlight.Point(Delivery, U, Star, rules: r).Z;
                if (Bits(actualZ) != token[1])
                    yield return $"Z fixture {token[1]} vs now {Bits(actualZ)} ({Readable(token[1])} vs {actualZ:R})";
            }

            var baseBits = token[Crossing ? 1 : 2];
            var actualBase = StoredXBase(r);
            if (Bits(actualBase) != baseBits)
                yield return $"sin-free X fixture {baseBits} vs now {Bits(actualBase)} ({Readable(baseBits)} vs {actualBase:R})";

            var dampedBits = token[Crossing ? 2 : 3];
            if (dampedBits is not ("0" or "1")) { yield return $"damped must be 0 or 1; got '{dampedBits}'"; yield break; }
            var damped = dampedBits == "1";
            if (Damped(r) != damped)
                yield return $"break damping fixture {damped} vs now {Damped(r)} — the family's breakDamped or the charge rule moved";

            // Rebuild X on this platform, in the order PitchFlight.Point writes it: the shape, then
            // the stick's shift, then the star's wobble. The stored pieces are the sin-free ones;
            // Math.Sin is called here exactly as production calls it, so the equality is exact on
            // macOS and on glibc alike (#736).
            var u = Math.Clamp(Crossing ? 1 : U, 0, 1);
            var composed = BitConverter.Int64BitsToDouble(Convert.ToInt64(baseBits, 16));
            composed += PitchFlight.BreakShiftFt(u, Delivery.BreakX * Delivery.BreakMul, damped, r.Pitching.Flight);
            composed = StarX(composed, u, r.Pitching.StarShapes);
            var actualX = ActualX(r);
            if (Bits(composed) != Bits(actualX))
                yield return $"X composed {Bits(composed)} vs flown {Bits(actualX)} ({composed:R} vs {actualX:R}) "
                             + "— the rebuild no longer matches how Point composes X";
        }

        /// <summary>The star's contribution to X, spelled exactly as <see cref="PitchFlight.Point"/> spells it.</summary>
        internal double StarX(double x, double u, StarPitchShapeRules st) =>
            !Delivery.Star ? x : Star switch
            {
                "heatball" => x + Math.Sin(u * st.HeatballWobbleHz) * st.HeatballWobbleFt,
                "prismball" => x + Math.Sin(u * st.PrismballWobbleHz) * st.PrismballWobbleFt,
                "charmball" => x + Math.Sin(u * st.CharmballWobbleHz) * st.CharmballWobbleFt,
                "phonyball" => x + (u > st.PhonyballSwitchAt ? st.PhonyballLateX : st.PhonyballEarlyX),
                // caskball lifts Y, not X; every other id falls through untouched.
                _ => x
            };
    }

    internal static IEnumerable<Sample> Samples()
    {
        // The shape itself: every point of the flight, at each charge and each stick break.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var charge in Charges)
                foreach (var breakX in Breaks)
                    foreach (var u in UGrid)
                        yield return new Sample($"pt|{SpellingKeys[s]}|c{Name(charge)}|b{Name(breakX)}|u{Name(u)}",
                            Command(s, charge, breakX: breakX), u);

        // Where the pitcher stands and where the pitch is aimed, at the seam of the hang.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var rubberX in Rubbers)
                foreach (var (aimX, aimY) in Aims)
                    foreach (var u in ShapeU)
                        yield return new Sample(
                            $"aim|{SpellingKeys[s]}|r{Name(rubberX)}|x{Name(aimX)}|y{Name(aimY)}|u{Name(u)}",
                            Command(s, aimX: aimX, aimY: aimY, rubberX: rubberX), u);

        // The star pitches that bend the shown ball, plus an armed id on an unstarred command.
        for (var s = 0; s < SpellingKeys.Length; s++)
        {
            foreach (var star in StarIds)
                foreach (var u in StarU)
                    yield return new Sample($"star|{SpellingKeys[s]}|{star ?? "none"}|u{Name(u)}",
                        Command(s, star: star is not null), u, star);
            yield return new Sample($"star|{SpellingKeys[s]}|heatball-unarmed|u{Name(0.8)}",
                Command(s), 0.8, "heatball");
        }

        // Speed: base, the Pitch stat, the charge, the Nice! band, the star multiplier.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var charge in Charges)
                foreach (var stat in Stats)
                    foreach (var nice in new[] { false, true })
                        foreach (var star in StarIds)
                            yield return new Sample(
                                $"mph|{SpellingKeys[s]}|c{Name(charge)}|p{stat}|{(nice ? "nice" : "plain")}|{star ?? "none"}",
                                Command(s, charge, star: star is not null, nice: nice), Star: star, Mph: (stat, nice));

        // The crossing the umpire, the tell and the CPU batter all read.
        for (var s = 0; s < SpellingKeys.Length; s++)
            foreach (var charge in Charges)
                foreach (var breakX in CrossBreaks)
                    foreach (var rubberX in Rubbers)
                        foreach (var (aimX, aimY) in Aims.Take(2))
                            foreach (var star in new string?[] { null, "prismball", "caskball" })
                                yield return new Sample(
                                    $"cross|{SpellingKeys[s]}|c{Name(charge)}|b{Name(breakX)}|r{Name(rubberX)}|x{Name(aimX)}|{star ?? "none"}",
                                    Command(s, charge, star is not null, aimX, aimY, breakX, rubberX), 1, star, Crossing: true);
    }

    // ---- exact doubles -------------------------------------------------------------------------

    internal static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("x16");

    static string Readable(string bits) => BitConverter.Int64BitsToDouble(Convert.ToInt64(bits, 16)).ToString("R");

    /// <summary>A grid coordinate in the key: round-trip exact, so no sample point is ambiguous.</summary>
    static string Name(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    // ---- the file ------------------------------------------------------------------------------

    static readonly ContentCatalog ShippedContent = ContentCatalog.Load(new DataRoot(ContentCatalog.Load().Root.Shipped));

    /// <summary>Named explicitly: the golden is the shipped library, whatever overlay a process carries.</summary>
    internal static RulesTable Shipped => ShippedContent.Rules;

    static string FixturePath => Path.Combine(
        Path.GetFullPath(Path.Combine(ShippedContent.Root.Shipped, "..")),
        "src", "GrandSluggers.Sim.Tests", "fixtures", "pitch-family-golden.json");

    static Dictionary<string, string> Stored()
    {
        var json = JsonNode.Parse(File.ReadAllText(FixturePath))!.AsObject();
        Assert.Equal(What, json["what"]!.GetValue<string>());
        Assert.Equal(Encoding, json["encoding"]!.GetValue<string>());
        var rows = json["rows"]!.AsObject();
        return rows.ToDictionary(r => r.Key, r => r.Value!.GetValue<string>(), StringComparer.Ordinal);
    }
}
