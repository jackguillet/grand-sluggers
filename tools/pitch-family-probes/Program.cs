using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GrandSluggers.Sim;

// Research only (#818, PH-20-R1). This measures the production flight function under the trial
// overlay trials/pitch5 and writes down what it did. Nothing here is a second flight model, a
// balance claim, or an acceptance: every number the overlay proposes is Jack's to judge in the
// trial window, and the shipped root is untouched by this tool and by the overlay it reads.
var root = FindRoot();
var mode = args.SingleOrDefault() ?? "--check";
if (mode is not ("--check" or "--write"))
    throw new ArgumentException("Use --check or --write.");

const string Overlay = "trials/pitch5";
const string OutputPath = "docs/research/pitch-families-p1d.json";
const string PlotFolder = "docs/research/pitch-families-p1d";

var dataRoot = new DataRoot(Path.Combine(root, "data"), Path.Combine(root, Overlay));
var rules = RulesTable.Load(dataRoot);
var families = rules.Pitching.Families;
var flight = rules.Pitching.Flight;

// ---- method constants, each one stated rather than assumed ------------------------------------

// The real ball's radius in game feet. Baseball.cs draws the ball at 0.62 ft across and says in its
// own summary that a real ball is "~0.25" — the drawn ball is a readability mesh, so it decides how
// big the ball looks, not where it is. Used twice: the margin a crossing must keep inside the zone,
// and the displacement at which a sweep has taken the ball off the line it was flying.
const double BallRadiusFt = 0.125;

// Feet per second a batter walks the box at full stick: the Unity at-bat seat moves the box by
// `box.StickX * dt * 1.6` box-units (unity/Assets/Scripts/Runtime/AtBatDirector.cs) and
// HomeSet.BatterWalk is feet per box-unit.
const double BoxWalkFtPerSec = 1.6 * HomeSet.BatterWalk;

// The batter the margin is computed for: an ordinary bat (Bat 5 = scale 1), no charge, no buddies,
// standing where the box starts them.
const int BatterContact = 5;

var hands = new[] { Hand.R, Hand.L };
var uGrid = Enumerable.Range(0, 201).Select(i => i / 200.0).ToArray();
var library = PitchFamily.All;

// ---- the rows ---------------------------------------------------------------------------------

var speed = new List<object>();
foreach (var family in library)
    foreach (var stat in new[] { 1, 5, 10 })
        foreach (var charge in new[] { 0.0, 1.0 })
            foreach (var nice in new[] { false, true })
            {
                var mph = AtBatResolver.PitchSpeedMph(new PitchCommand(family, charge, false, Nice: nice), stat, rules);
                speed.Add(new
                {
                    family,
                    pitchStat = stat,
                    charge,
                    nice,
                    mph = R(mph),
                    airSec = R(PitchFlight.AirSeconds(mph, rules)),
                    insideShippedEnvelope = Inside(family, stat, charge, nice)
                });
            }

var rows = new List<object>();
foreach (var family in library)
    foreach (var throws in hands)
        foreach (var charge in new[] { 0.0, 1.0 })
        {
            var row = families.Of(family);
            var pitch = new PitchCommand(family, charge, false, Throws: throws);
            var mph = AtBatResolver.PitchSpeedMph(pitch, 5, rules);
            var path = uGrid.Select(u => PitchFlight.Point(pitch, u, rules: rules)).ToArray();
            var (crossX, crossY) = (path[^1].X, path[^1].Y);
            var peak = path.Select((p, i) => (p.Y, U: uGrid[i])).MaxBy(p => p.Y);
            var fastball = PitchFlight.Crossing(new PitchCommand(PitchFamily.Fastball, charge, false, Throws: throws), rules: rules);

            rows.Add(new
            {
                family,
                throws = throws.ToString(),
                charge,
                pitchStat = 5,
                mph = R(mph),
                airSec = R(PitchFlight.AirSeconds(mph, rules)),
                crossingXFt = R(crossX),
                crossingYFt = R(crossY),
                peakHeightFt = R(peak.Y),
                peakAtU = R(peak.U),
                dropBelowFastballCrossingFt = R(fastball.Y - crossY),
                sweepAtPlateFt = R(PitchFlight.SweepShiftFt(1, row, throws)),
                sweepStartsAtU = R(row.SweepFrom),
                inZone = StrikeZoneGeometry.Contains(crossX, crossY),
                zoneMarginXFt = R(StrikeZoneGeometry.HalfWidth - Math.Abs(crossX)),
                zoneMarginYFt = R(Math.Min(crossY - StrikeZoneGeometry.Bottom, StrikeZoneGeometry.Top - crossY)),
                keepsABallInsideTheZone = Math.Abs(crossX) + BallRadiusFt <= StrikeZoneGeometry.HalfWidth
                    && crossY - BallRadiusFt >= StrikeZoneGeometry.Bottom
                    && crossY + BallRadiusFt <= StrikeZoneGeometry.Top,
                cursorReach = hands.Select(bats => new
                {
                    bats = bats.ToString(),
                    ovalDistanceAtCrossing = R(SweetSpot.Distance(0, bats, crossX, crossY, 1, rules))
                }).ToArray()
            });
        }

// The margin PH-04 has to survive, for the three families the trial proposes. Worst legal case:
// the family's whole sweep plus the player's whole stick the same way, at the highest Pitch stat
// with a Nice! release, against a batter who starts centred and does not move until the sweep
// alone has taken the ball a ball-radius off the line it was flying. The charge damps the stick
// and shortens the flight, so charged and uncharged are two different worst cases and both are here.
var coverage = new List<object>();
foreach (var family in library.Where(f => families.Of(f).SweepFt != 0))
    foreach (var throws in hands)
        foreach (var charged in new[] { false, true })
        {
            var row = families.Of(family);
            var side = Math.Sign(row.SweepFt * PitchFlight.GloveSideSign(throws));
            var pitch = new PitchCommand(family, charged ? 1 : 0, false, BreakX: side, Nice: true, Throws: throws);
            var mph = AtBatResolver.PitchSpeedMph(pitch, 10, rules);
            var air = PitchFlight.AirSeconds(mph, rules);
            var (crossX, crossY) = PitchFlight.Crossing(pitch, rules: rules);
            var shows = row.SweepFrom + (1 - row.SweepFrom) * Math.Sqrt(BallRadiusFt / Math.Abs(row.SweepFt));
            var window = Math.Max(0, 1 - shows) * air;
            var dy = crossY - StrikeZoneGeometry.CenterY;
            var squash = Math.Sqrt(Math.Max(0, 1 - dy * dy / (SweetSpot.HalfHeightFt * SweetSpot.HalfHeightFt)));

            coverage.Add(new
            {
                family,
                throws = throws.ToString(),
                charged,
                mph = R(mph),
                airSec = R(air),
                sweepFt = R(PitchFlight.SweepShiftFt(1, row, throws)),
                steerFt = R(PitchFlight.BreakShiftFt(1, side, charged, flight)),
                crossingXFt = R(crossX),
                crossingYFt = R(crossY),
                sweepShowsAtU = R(shows),
                reactionWindowSec = R(window),
                walkFt = R(window * BoxWalkFtPerSec),
                batters = hands.Select(bats =>
                {
                    var half = SweetSpot.NiceHalfWidthFt(bats, crossX, 1, rules) * squash;
                    return new
                    {
                        bats = bats.ToString(),
                        ovalHalfWidthAtCrossingFt = R(half),
                        reachFt = R(window * BoxWalkFtPerSec + half),
                        marginFt = R(window * BoxWalkFtPerSec + half - Math.Abs(crossX))
                    };
                }).ToArray()
            });
        }

// ---- the file ---------------------------------------------------------------------------------

var sources = new[]
{
    "src/GrandSluggers.Sim/PitchFlight.cs", "src/GrandSluggers.Sim/Rules.cs",
    "src/GrandSluggers.Sim/Models.cs", "src/GrandSluggers.Sim/AtBatFeel.cs",
    "data/rules/pitching.json", "data/rules/batting.json", Overlay + "/rules/pitching.json",
    "tools/pitch-family-probes/PitchFamilyProbes.csproj", "tools/pitch-family-probes/Program.cs"
};
var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var report = new
{
    schemaVersion = 1,
    what = "Curveball, slider and sinker as they fly under the trial overlay " + Overlay + " (#818, PH-20-R1). "
        + "Derived from the production flight function; proposals, not accepted numbers.",
    status = "trial-proposal-not-accepted",
    overlay = Overlay,
    generatedBy = "dotnet run --project tools/pitch-family-probes -- --write",
    verify = "dotnet run --project tools/pitch-family-probes -- --check",
    notClaimed = new[]
    {
        "No sitting has happened: the mound is not wired to a seat until P1-f, so nobody has thrown one of these.",
        "No feel judgment, no balance claim, and no human acceptance (PH-20-R1 keeps that with Jack).",
        "The shipped root is unchanged; without GRAND_SLUGGERS_TRIAL=" + Overlay + " these three families still stop by name.",
        "The CPU pitcher never selects them, so no whole-game rate here moved."
    },
    method = new
    {
        ballRadiusFt = BallRadiusFt,
        ballRadiusWhy = "The real ball's radius in game feet. Baseball.cs draws it at 0.62 ft across and calls that a "
            + "readability mesh over a ball ~0.25 ft across; the drawn radius would also refuse the shipped changeup, "
            + "which crosses 0.20 ft above the zone floor.",
        boxWalkFtPerSec = BoxWalkFtPerSec,
        boxWalkWhy = "AtBatDirector moves the box by StickX * dt * 1.6 box-units; HomeSet.BatterWalk is 2.4 ft per box-unit.",
        batterContact = BatterContact,
        batterNote = "Bat 5 (barrel scale 1), no charge, no buddies, starting in the middle of the box.",
        worstCase = "The family's whole sweep plus the player's whole stick the same way, at Pitch 10 with a Nice! "
            + "release. The reaction window opens when the sweep alone has moved the ball a ball-radius off its line; "
            + "the stick's own mid-flight bend shows far earlier, so counting it would only lengthen the window.",
        rounding = "Every number is rounded to 6 decimals. The sweep, the shape and the speed are pure arithmetic and "
            + "identical on macOS and glibc; the only libm on the way to these values is Math.Sin inside the stick's "
            + "shift, whose one-ULP platform difference is ~1e-16 and disappears at this rounding (#811, #736)."
    },
    zone = new
    {
        halfWidthFt = StrikeZoneGeometry.HalfWidth,
        bottomFt = StrikeZoneGeometry.Bottom,
        topFt = StrikeZoneGeometry.Top,
        centerFt = StrikeZoneGeometry.CenterY
    },
    gloveSideSign = hands.ToDictionary(h => h.ToString(), h => PitchFlight.GloveSideSign(h)),
    sourceSha256 = sources.ToDictionary(p => p, p => Sha(Path.Combine(root, p))),
    speed,
    rows,
    coverage
};

var json = JsonSerializer.Serialize(report, options) + "\n";
var plots = Plots();
var outputFile = Path.Combine(root, OutputPath);

if (mode == "--write")
{
    Directory.CreateDirectory(Path.Combine(root, PlotFolder));
    File.WriteAllText(outputFile, json);
    foreach (var (name, svg) in plots) File.WriteAllText(Path.Combine(root, PlotFolder, name), svg);
}
else
{
    if (!File.Exists(outputFile) || File.ReadAllText(outputFile) != json)
        throw new InvalidOperationException(
            "Pitch family evidence is stale. Review the sources named in sourceSha256, then regenerate with --write.");
    foreach (var (name, svg) in plots)
    {
        var path = Path.Combine(root, PlotFolder, name);
        if (!File.Exists(path) || File.ReadAllText(path) != svg)
            throw new InvalidOperationException($"{PlotFolder}/{name} is stale. Regenerate with --write.");
    }
}

Console.WriteLine($"#818: {rows.Count} flights, {speed.Count} speeds and {coverage.Count} coverage cases "
    + $"{(mode == "--write" ? "written" : "verified")} under {Overlay}; {plots.Count} plots; "
    + "proposals only — no sitting, no acceptance, shipped root unchanged.");

// ---- helpers ------------------------------------------------------------------------------------

double RMph(string family, int stat, double charge, bool nice) =>
    AtBatResolver.PitchSpeedMph(new PitchCommand(family, charge, false, Nice: nice), stat, rules);

bool Inside(string family, int stat, double charge, bool nice) =>
    RMph(family, stat, charge, nice) <= RMph(PitchFamily.Fastball, stat, charge, nice)
    && RMph(family, stat, charge, nice) >= RMph(PitchFamily.Changeup, stat, charge, nice);

// One SVG per view per hand: the five families on one axis, the zone drawn at the plate, an
// explicit light background so the plot reads the same on a white page and a dark one. Hand-written
// strings on purpose — an evidence plot must not need a plotting dependency to be regenerated.
Dictionary<string, string> Plots()
{
    var made = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var throws in hands)
    {
        made[$"side-{throws}.svg"] = Svg(throws, side: true);
        made[$"top-{throws}.svg"] = Svg(throws, side: false);
    }
    return made;
}

string Svg(Hand throws, bool side)
{
    const int W = 980, H = 430;
    const int L = 78, Rr = 262, T = 46, B = 56;
    var plotW = W - L - Rr;
    var plotH = H - T - B;
    var colours = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [PitchFamily.Fastball] = "#1f4e9c", [PitchFamily.Changeup] = "#8a5a00",
        [PitchFamily.Curveball] = "#9b1d6b", [PitchFamily.Slider] = "#0f7a63",
        [PitchFamily.Sinker] = "#a33208"
    };

    var release = PitchFlight.Release(0, rules);
    var far = release.Z;
    var (lo, hi) = side ? (0.0, 7.0) : (-2.2, 2.2);

    double Px(double distance) => L + plotW * (distance / far);
    double Py(double value) => T + plotH * (1 - (value - lo) / (hi - lo));

    var svg = new StringBuilder();
    svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{W}\" height=\"{H}\" viewBox=\"0 0 {W} {H}\" font-family=\"Helvetica, Arial, sans-serif\">\n");
    svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"0\" y=\"0\" width=\"{W}\" height=\"{H}\" fill=\"#ffffff\"/>\n");
    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L}\" y=\"24\" font-size=\"15\" font-weight=\"bold\" fill=\"#111111\">"
        + $"{(side ? "Side view — height" : "Top view — lateral")}, {(throws == Hand.R ? "right" : "left")}-handed pitcher, no steering, middle of the rubber</text>\n");
    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L}\" y=\"40\" font-size=\"11\" fill=\"#555555\">trial {Overlay} — proposed, not accepted (PH-20-R1). Pitch 5, no charge. Feet.</text>\n");

    // Grid and axes.
    for (var f = Math.Ceiling(lo); f <= hi; f += 1)
    {
        var y = Py(f);
        svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{L}\" y1=\"{y:F1}\" x2=\"{L + plotW}\" y2=\"{y:F1}\" stroke=\"#e6e6e6\" stroke-width=\"1\"/>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L - 8}\" y=\"{y + 4:F1}\" font-size=\"11\" fill=\"#444444\" text-anchor=\"end\">{f:F0}</text>\n");
    }
    for (var d = 0.0; d <= far + 0.001; d += 10)
    {
        var x = Px(d);
        svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{x:F1}\" y1=\"{T}\" x2=\"{x:F1}\" y2=\"{T + plotH}\" stroke=\"#f0f0f0\" stroke-width=\"1\"/>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x:F1}\" y=\"{T + plotH + 18}\" font-size=\"11\" fill=\"#444444\" text-anchor=\"middle\">{d:F0}</text>\n");
    }
    svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{L}\" y=\"{T}\" width=\"{plotW}\" height=\"{plotH}\" fill=\"none\" stroke=\"#999999\" stroke-width=\"1\"/>\n");
    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L + plotW / 2.0:F0}\" y=\"{H - 14}\" font-size=\"12\" fill=\"#222222\" text-anchor=\"middle\">feet from the release toward the plate</text>\n");
    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"16\" y=\"{T + plotH / 2.0:F0}\" font-size=\"12\" fill=\"#222222\" text-anchor=\"middle\" transform=\"rotate(-90 16 {T + plotH / 2.0:F0})\">"
        + $"{(side ? "height above the ground, feet" : "world X, feet (+ is toward first base)")}</text>\n");

    // The strike zone at the plate: its height in the side view, its width in the top view.
    var zoneLo = side ? StrikeZoneGeometry.Bottom : -StrikeZoneGeometry.HalfWidth;
    var zoneHi = side ? StrikeZoneGeometry.Top : StrikeZoneGeometry.HalfWidth;
    svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Px(far) - 9:F1}\" y=\"{Py(zoneHi):F1}\" width=\"9\" height=\"{Py(zoneLo) - Py(zoneHi):F1}\""
        + $" fill=\"#000000\" fill-opacity=\"0.06\" stroke=\"#333333\" stroke-width=\"1.5\"/>\n");
    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Px(far) - 12:F1}\" y=\"{Py(zoneHi) - 6:F1}\" font-size=\"10\" fill=\"#333333\" text-anchor=\"end\">strike zone at the plate</text>\n");

    // The flights.
    foreach (var family in library)
    {
        var pitch = new PitchCommand(family, 0, false, Throws: throws);
        var points = uGrid.Select(u =>
        {
            var p = PitchFlight.Point(pitch, u, rules: rules);
            return $"{Px(far - p.Z):F2},{Py(side ? p.Y : p.X):F2}";
        });
        svg.Append(CultureInfo.InvariantCulture, $"<polyline points=\"{string.Join(' ', points)}\" fill=\"none\" stroke=\"{colours[family]}\" stroke-width=\"2.4\" stroke-linejoin=\"round\"/>\n");
    }

    // Legend, outside the plot so no flight hides under it.
    var ly = T + 6;
    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L + plotW + 20}\" y=\"{ly}\" font-size=\"12\" font-weight=\"bold\" fill=\"#111111\">family</text>\n");
    foreach (var family in library)
    {
        ly += 22;
        var r = families.Of(family);
        var note = side
            ? $"drop {r.DropFt:0.##} ft, hump {r.Hump:0.##}"
            : $"sweep {r.SweepFt * PitchFlight.GloveSideSign(throws):+0.##;-0.##;0} ft";
        svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{L + plotW + 20}\" y1=\"{ly - 4}\" x2=\"{L + plotW + 44}\" y2=\"{ly - 4}\" stroke=\"{colours[family]}\" stroke-width=\"3\"/>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L + plotW + 50}\" y=\"{ly}\" font-size=\"12\" fill=\"#111111\">{family}</text>\n");
        ly += 14;
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{L + plotW + 50}\" y=\"{ly}\" font-size=\"10\" fill=\"#555555\">{note}</text>\n");
    }
    svg.Append("</svg>\n");
    return svg.ToString();
}

static double R(double value) => Math.Round(value, 6);

static string Sha(string path) =>
    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

static string FindRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, "trials", "pitch5")))
            return dir.FullName;
    throw new DirectoryNotFoundException("Run from the repository's probe project.");
}
