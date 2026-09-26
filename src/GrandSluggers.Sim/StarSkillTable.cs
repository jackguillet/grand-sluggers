namespace GrandSluggers.Sim;

/// <summary>A captain's star pitch (data/abilities/star-skills.json, spec §13): the numbers the sim reads at runtime.</summary>
public sealed record StarPitchSkill(
    string Id,
    string Name,
    string Kind,
    double SpeedMul,
    int StaminaCost,
    bool LateBreak,
    bool Decoy,
    string? OnCatch,
    /// <summary>A pace that hangs the ball over one stretch of its path and leaps it to the plate on time, or null (§13).</summary>
    PitchLeap? Leap = null,
    /// <summary>A late rise that lifts the ball over the last stretch of its flight to a crossing above the aimed one, or null (§13).</summary>
    PitchRise? Rise = null,
    /// <summary>A ring on home plate, from contact, that slows the batter-runner inside it, or null (§13).</summary>
    PitchUndertow? Undertow = null,
    /// <summary>One full vertical loop mid-flight, then the ordinary crossing on the ordinary time, or null (§13).</summary>
    PitchLoop? Loop = null,
    /// <summary>A side-to-side sway that swells to its widest mid-flight and settles onto the ordinary path before the plate, or null (§13).</summary>
    PitchSway? Sway = null,
    /// <summary>A path that swings in on one pendulum arc from a pivot above the ball onto the unchanged crossing, or null (§13).</summary>
    PitchPendulum? Pendulum = null,
    /// <summary>A late drop that sinks the ball over the last stretch of its flight to a crossing below the aimed one, or null (§13).</summary>
    PitchDrop? Drop = null,
    /// <summary>A full stop at one point of the path for a fixed time, then a run on down the line to arrive on time, or null (§13).</summary>
    PitchHitch? Hitch = null,
    /// <summary>A stretch of the flight in which the ball is hidden and only its shadow shows, or null (§13).</summary>
    PitchVanish? Vanish = null);

/// <summary>
/// A star pitch's hitch (spec §13): at <see cref="At"/> of the flight the ball stops dead at one point of its path — a cable car
/// at its station — for <see cref="HoldSec"/> seconds, then runs on down the rest of its path fast enough to arrive at the
/// ordinary instant. The path, the crossing and the arrival time are the ordinary pitch's; only where the ball is along its
/// path, and when, changes. The stop is in seconds, so its share of the flight is the delivery's own: a slow pitch stops for
/// a smaller share of its longer flight. Nothing is rolled.
/// </summary>
public sealed record PitchHitch(double At, double HoldSec)
{
    /// <summary>The longest stop a row may name: a beat the eye catches, not a pitch that hangs until the batter gives up.</summary>
    public const double MaxHoldSec = 0.3;

    /// <summary>
    /// The share of a flight of <paramref name="airSec"/> seconds the stop takes: <see cref="HoldSec"/> over the air time, never
    /// more than half of what is left of the flight after the station, so the run down the line always has a stretch to make up
    /// the time in. A caller with no clock (<paramref name="airSec"/> ≤ 0) reads the slowest flight the table allows.
    /// </summary>
    public double HoldShare(double airSec, RulesTable rules)
    {
        var air = airSec > 0 ? airSec : rules.Pitching.Flight.AirMaxSec;
        return Math.Min(HoldSec / air, (1 - At) / 2);
    }

    /// <summary>
    /// How far along its path the ball is at time fraction <paramref name="u"/> of a flight of <paramref name="airSec"/> seconds:
    /// the identity up to the station, standing still there for the stop, then an even run that reaches the plate at 1 on 1.
    /// </summary>
    public double Progress(double u, double airSec, RulesTable rules)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= At) return u;
        var hold = HoldShare(airSec, rules);
        if (u <= At + hold) return At;
        if (u >= 1) return 1;
        return At + (1 - At) * (u - At - hold) / (1 - At - hold);
    }
}

/// <summary>
/// A star pitch's vanish (spec §13): from <see cref="From"/> to <see cref="To"/> of the flight the ball is hidden in heat
/// shimmer; its shadow keeps crossing the dirt, and it shows again for the rest of the flight. The path, the speed, the crossing
/// and the timing window are the ordinary pitch's: only what can be seen changes. The CPU batter pays the same read: while
/// the ball is hidden at its commit instant it reads the flight as it stood when the ball vanished (<see cref="CpuBatter.ReadPitch"/>).
/// </summary>
public sealed record PitchVanish(double From, double To)
{
    /// <summary>The ball is back by this share of the flight at the latest: the hitter always sees the last quarter.</summary>
    public const double BackBy = 0.75;

    /// <summary>The ball can be seen at time fraction <paramref name="u"/>: everywhere but the half-open stretch [From, To).</summary>
    public bool Visible(double u) => u < From || u >= To;
}

/// <summary>
/// A star pitch's undertow (spec §13): when a fair ball is put in play off it, a disc of radius <see cref="RadiusFt"/>
/// centred on home plate is live for <see cref="Sec"/> from contact, and the batter-runner's every step inside it is at
/// <see cref="RunnerMul"/> of its speed. It is a status volume (<see cref="BodySlows"/>) that touches one body: no fielder,
/// no other runner, and nothing after the body leaves it or the time runs out. Geometry decides it; nothing is rolled.
/// </summary>
public sealed record PitchUndertow(double RadiusFt, double Sec, double RunnerMul)
{
    /// <summary>The <see cref="StatusVolume.Type"/> the undertow's disc carries: presentation draws its ring by this name.</summary>
    public const string Type = "undertow";

    /// <summary>The widest ring a row may name: it may cover the batter's first steps, never the run to first.</summary>
    public const double MaxRadiusFt = 20;

    /// <summary>Every bend ends within 2 s of contact (§13).</summary>
    public const double MaxSec = 2;

    /// <summary>
    /// The disc as the live ball reads it, for a play whose clock starts at contact: centred on home, live until
    /// <see cref="Sec"/>, no time after the body leaves it (<c>slowSec</c> 0), the batter-runner's alone.
    /// </summary>
    public StatusVolume Volume() =>
        new(StatusVolume.StarHazard, Type, 0, 0, RadiusFt, 0, SlowMul: RunnerMul, UntilT: Sec, BatterRunnerOnly: true);
}

/// <summary>
/// A star pitch's loop (spec §13): from <see cref="At"/> of the flight the ball runs one full vertical loop,
/// <see cref="DiameterFt"/> across, over <see cref="Span"/> of the flight, standing still along its path while it loops;
/// then it runs on along the rest of its path to arrive at the ordinary instant. The loop sits on the path like a coaster
/// loop on its track: it leaves forward along the flight, climbs, turns over the top and comes back down to where it
/// started. The path, the crossing and the arrival time are the ordinary pitch's; only where the ball is, and when, changes.
/// </summary>
public sealed record PitchLoop(double At, double Span, double DiameterFt)
{
    /// <summary>The widest loop a row may name: a loop over the plate's width, not over the backstop.</summary>
    public const double MaxDiameterFt = 6;

    /// <summary>The time fraction the loop is done by: the ball runs its last stretch on its own path.</summary>
    public double Exit => At + Span;

    /// <summary>How far along its path the ball is at time fraction <paramref name="u"/>: the identity, held through the loop, then a catch-up; 1 at 1.</summary>
    public double Progress(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= At) return u;
        if (u <= Exit) return At;
        if (u >= 1) return 1;
        return At + (1 - At) * (u - Exit) / (1 - Exit);
    }

    /// <summary>
    /// The loop's offset from the path at time fraction <paramref name="u"/>: along the flight (toward the plate) and up, in feet.
    /// A circle of the loop's diameter standing on the path, entered and left at its foot; (0, 0) outside the loop.
    /// </summary>
    public (double Forward, double Up) Offset(double u)
    {
        if (u <= At || u >= Exit) return (0, 0);
        var turn = 2 * Math.PI * (u - At) / Span;
        var r = DiameterFt / 2;
        return (r * Math.Sin(turn), r * (1 - Math.Cos(turn)));
    }
}

/// <summary>
/// A star pitch's late rise (spec §13): nothing until <see cref="From"/> of the flight, then the ball climbs on a quadratic ease
/// to <see cref="RiseFt"/> above its ordinary path exactly at the plate. Unlike a shape that settles before the plate, the crossing moves: the umpire, the
/// bat and the CPU judge the risen ball, and the timing window is judged at that real crossing. The rise is always up.
/// </summary>
public sealed record PitchRise(double RiseFt, double From)
{
    /// <summary>The largest rise a row may name, in reference-zone feet: a jump out of the heart of the zone, not over the batter.</summary>
    public const double MaxRiseFt = 2;

    /// <summary>The height over the ordinary path at <paramref name="u"/>: 0 up to <see cref="From"/>, then (share of the stretch)² × <see cref="RiseFt"/>; the full rise at the plate.</summary>
    public double Lift(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= From) return 0;
        var t = (u - From) / (1 - From);
        return RiseFt * t * t;
    }
}

/// <summary>
/// A star pitch's late drop (spec §13, Anvil): the ball flies its ordinary path until <see cref="From"/> of the flight — the
/// clang, where it turns to cold iron — then sinks on a quadratic ease to <see cref="DropFt"/> below that path exactly at the
/// plate. The crossing moves: the umpire, the bat and the CPU judge the dropped ball, and the timing window is judged at that
/// real crossing. The drop is always straight down. <see cref="From"/> is the turn the client reads for the clang and the colour.
/// </summary>
public sealed record PitchDrop(double DropFt, double From)
{
    /// <summary>The largest drop a row may name, in reference-zone feet: out of the bottom of the zone, never into the dirt.</summary>
    public const double MaxDropFt = 2;

    /// <summary>How far below the ordinary path the ball is at <paramref name="u"/>: 0 up to <see cref="From"/>, then (share of the stretch)² × <see cref="DropFt"/>; the whole drop at the plate.</summary>
    public double Fall(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= From) return 0;
        var t = (u - From) / (1 - From);
        return DropFt * t * t;
    }

    /// <summary>The ball has clanged and turned to cold iron at <paramref name="u"/> of the flight (the tell's instant is <see cref="From"/>).</summary>
    public bool Turned(double u) => u >= From;
}

/// <summary>
/// A star pitch's sway (spec §13): the ball swings side to side across its ordinary path, <see cref="Cycles"/> full sways over
/// the flight, the swing swelling from nothing at the release to exactly <see cref="WidthFt"/> at <see cref="PeakAt"/> of the
/// flight and settling back to nothing by <see cref="SettleBy"/>. From there to the plate the ball is on its ordinary path, so
/// the crossing — what the umpire, the bat and the CPU judge — is the ordinary one. Nothing is rolled: the sway is a fixed curve.
/// </summary>
public sealed record PitchSway(double WidthFt, double Cycles, double PeakAt, double SettleBy)
{
    /// <summary>The widest sway a row may name: a ribbon across the zone, not a pitch thrown at another batter.</summary>
    public const double MaxWidthFt = 2;
    /// <summary>The most full sways a row may name over one flight: a sway the eye can follow, not a buzz.</summary>
    public const double MaxCycles = 4;

    /// <summary>
    /// The sideways offset from the ordinary path at <paramref name="u"/> of the flight, in feet of world X: the swell (a
    /// quarter sine up to <see cref="PeakAt"/>, a quarter cosine down to <see cref="SettleBy"/>) times a sway whose crest is at
    /// <see cref="PeakAt"/>. Its largest size is <see cref="WidthFt"/>, exactly at <see cref="PeakAt"/>; it is exactly 0 at the
    /// release and from <see cref="SettleBy"/> to the plate.
    /// </summary>
    public double OffsetFt(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= 0 || u >= SettleBy) return 0;
        var swell = u <= PeakAt
            ? Math.Sin(Math.PI / 2 * u / PeakAt)
            : Math.Cos(Math.PI / 2 * (u - PeakAt) / (SettleBy - PeakAt));
        return WidthFt * swell * Math.Cos(2 * Math.PI * Cycles * (u - PeakAt));
    }
}

/// <summary>
/// A star pitch's pendulum (spec §13): the ball hangs on a vine <see cref="LengthFt"/> long from a pivot that rides
/// <see cref="LengthFt"/> above the ordinary ball. The vine swings out from straight down to <see cref="SwingDeg"/> by
/// <see cref="WidestAt"/> of the flight, then swings back in on one pendulum arc to straight down at the plate. Every point
/// of the flight is on the circle about the pivot, so the ball is always exactly <see cref="LengthFt"/> from it; at the plate
/// the vine hangs straight and the ball is on the ordinary crossing at the ordinary instant, so the umpire, the bat and the
/// CPU read the ordinary pitch.
/// </summary>
public sealed record PitchPendulum(double LengthFt, double SwingDeg, double WidestAt)
{
    /// <summary>The longest vine a row may name: a swing into the zone, not across the infield.</summary>
    public const double MaxLengthFt = 12;

    /// <summary>The widest swing a row may name, from straight down: the ball never swings above the pivot.</summary>
    public const double MaxSwingDeg = 80;

    /// <summary>The vine's angle from straight down at <paramref name="u"/>, in degrees: out along a quarter sine, in along a pendulum's quarter swing, exactly 0 at the plate.</summary>
    public double AngleDeg(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u >= 1) return 0;
        if (u <= WidestAt) return SwingDeg * Math.Sin(Math.PI / 2 * u / WidestAt);
        return SwingDeg * Math.Cos(Math.PI / 2 * (u - WidestAt) / (1 - WidestAt));
    }

    /// <summary>
    /// The ball's offset from the ordinary path at <paramref name="u"/>, in world feet: sideways toward <paramref name="side"/>
    /// (+1 or −1 in X) and up, both exactly 0 at the plate.
    /// </summary>
    public (double X, double Y) Offset(double u, double side)
    {
        var a = AngleDeg(u) * Math.PI / 180;
        if (a == 0) return (0, 0);
        return (side * LengthFt * Math.Sin(a), LengthFt * (1 - Math.Cos(a)));
    }
}

/// <summary>
/// A star pitch's leap (spec §13): from <see cref="At"/> of the flight the ball crawls at <see cref="HoldPace"/> of its pace for
/// <see cref="Hold"/> of the flight, then leaps over the rest of its path to arrive at the ordinary instant. The path, the
/// crossing and the arrival time are the ordinary pitch's; only where the ball is along its path, and when, changes.
/// </summary>
public sealed record PitchLeap(double At, double Hold, double HoldPace)
{
    /// <summary>How far along its path the ball is at time fraction <paramref name="u"/>: the identity, a crawl, then a catch-up; 1 at 1.</summary>
    public double Progress(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u <= At) return u;
        var held = At + HoldPace * Hold;
        if (u <= At + Hold) return At + HoldPace * (u - At);
        if (u >= 1) return 1;
        return held + (1 - held) * (u - At - Hold) / (1 - At - Hold);
    }
}

/// <summary>A captain's star swing (data/abilities/star-skills.json, spec §13).</summary>
public sealed record StarSwingSkill(
    string Id,
    string Name,
    string Kind,
    double ExitVeloMul,
    double? LaunchDeg,
    string? Terrain,
    /// <summary>
    /// The nearest fielder — the body the play would send after the ball — stands still this many seconds from the contact
    /// (§13, <see cref="FieldingPreview.Dazzled"/>); 0 is none.
    /// </summary>
    double FielderPauseSec,
    bool Decoy,
    /// <summary>A fair ball off this swing leaves its first hop this many times as fast upward (§13); 1 is the ordinary hop.</summary>
    double FirstHopBounceMul = 1,
    /// <summary>A fair ball off this swing stands still at its first hop for this many seconds (§13); 0 is none.</summary>
    double FirstHopStallSec = 0,
    /// <summary>After a first-hop stall the ball runs on at this share of its speed (§13); 1 is its own.</summary>
    double FirstHopStallSpeedMul = 1,
    /// <summary>
    /// This swing's Perfect ring is this many times the ordinary one (§13, PH-16-R2: a swing's own contact area); 1 is the
    /// ordinary ring. The nice oval, the sour rim and the timing window are unchanged, so a miss is still a miss.
    /// </summary>
    double PerfectRingMul = 1,
    /// <summary>A ball off this swing that jags sideways twice in the air and lands where the straight ball would, or null (§13).</summary>
    BallJag? Jag = null,
    /// <summary>The ball off this swing stays molten after contact and burns a glove that holds it, or null (§13, Hot Iron).</summary>
    HotBall? HotBall = null,
    /// <summary>
    /// This swing's contact oval is this many times as tall (§13, PH-16-R2: a swing's own contact area); 1 is the ordinary
    /// oval. Only the height grows, so the width along the barrel and the timing window are unchanged: a wide pitch or a
    /// late bat is still a miss.
    /// </summary>
    double OvalHeightMul = 1,
    /// <summary>
    /// At its apex this swing's fly carries this many times as far along its own line as the plain ball would from there, to its
    /// first landing (§13, <see cref="AtBatResult.ApexCarryMul"/>); 1 is the ordinary ball. The wind and the park do not enter it.
    /// </summary>
    double ApexCarryMul = 1,
    /// <summary>A bowl of loose dust this swing's grounder raises where it first lands, slowing the fielders inside it, or null (§13).</summary>
    SwingDustBowl? DustBowl = null)
{
    /// <summary>The tallest contact oval a row may name: twice the batter's zone, never more.</summary>
    public const double MaxOvalHeightMul = 2;
    /// <summary>The longest stall a row may name: the ball spins, then baseball resumes inside the two-second rule.</summary>
    public const double MaxStallSec = 1.2;

    /// <summary>The highest launch a stalling swing may name: the stall is a grounder's, so its first hop comes early.</summary>
    public const double MaxStallLaunchDeg = 4;

    /// <summary>The largest Perfect ring a row may name: twice the ordinary heart, and never past the drawn oval (<see cref="SweetSpot.Zone"/>).</summary>
    public const double MaxPerfectRingMul = 2;

    /// <summary>The highest first-hop bounce a row may name: a chopper, not a moon shot.</summary>
    public const double MaxBounceMul = 3;

    /// <summary>The swing changes its ball's first hop.</summary>
    public bool ShapesFirstHop => FirstHopBounceMul != 1 || FirstHopStallSec > 0 || DustBowl is not null;

    /// <summary>The swing changes its ball's path at the first hop (a spring or a stall); a dust bowl leaves the path alone.</summary>
    public bool BendsFirstHop => FirstHopBounceMul != 1 || FirstHopStallSec > 0;

    /// <summary>The largest apex carry a row may name: a gust, not a launch; the fly's fall carries at most half again as far.</summary>
    public const double MaxApexCarryMul = 1.5;

    /// <summary>The longest pause a row may name: every special's bend ends within 2 s of the contact (§13).</summary>
    public const double MaxFielderPauseSec = 2;
}

/// <summary>
/// A star swing's dust bowl (spec §13): where the swing's fair grounder first meets the ground, a disc of loose dust of radius
/// <see cref="RadiusFt"/> (measured like a park's slow disc, from its centre) rises at that landing and settles
/// <see cref="Sec"/> after the contact — the two-second rule (§13): a later landing gives a shorter bowl, and a landing at or
/// past <see cref="Sec"/> none. Every step a fielder takes inside it is at <see cref="Mul"/> of its speed. It is a status volume on the park's rail
/// (<see cref="BodySlows"/>) that touches fielders only: no runner, and no time after the body leaves it or the dust settles.
/// Routes ignore it (<see cref="VolumeRoute"/> reads the park's discs only); going round it is the fielder's own verb.
/// </summary>
public sealed record SwingDustBowl(double RadiusFt, double Sec, double Mul)
{
    /// <summary>The <see cref="StatusVolume.Type"/> the bowl's disc carries: presentation draws the dust by this name.</summary>
    public const string Type = "dust-bowl";

    /// <summary>The widest bowl a row may name: a patch of the infield, not the infield.</summary>
    public const double MaxRadiusFt = 15;

    /// <summary>The latest a bowl may settle, in seconds after the contact: a bend ends within two seconds (§13).</summary>
    public const double MaxSec = 2;

    /// <summary>The highest launch a bowl-raising swing may name: the bowl is a grounder's, so its first landing comes early.</summary>
    public const double MaxLaunchDeg = 6;

    /// <summary>The <see cref="StatusVolume.Hazard"/> the bowl's disc carries: a star's, below every park index.</summary>
    public const int Hazard = -2;

    /// <summary>
    /// The bowl as the live ball reads it, for a play whose clock starts at contact: centred on the landing (<paramref name="x"/>,
    /// <paramref name="z"/>), raised when it is added (at the landing) and gone at play second <see cref="Sec"/>, <see cref="Mul"/>
    /// inside, no time after the body leaves it (<c>slowSec</c> 0), fielders' alone.
    /// </summary>
    public StatusVolume Volume(double x, double z) =>
        new(Hazard, Type, x, z, RadiusFt, 0, SlowMul: Mul, UntilT: Sec, FieldersOnly: true);

    /// <summary>A landing at play second <paramref name="landT"/> raises a bowl: only before the dust's window from contact closes.</summary>
    public bool RaisesAt(double landT) => landT < Sec;
}

/// <summary>
/// A star swing's hot ball (spec §13, Hot Iron): the ball stays molten for <see cref="MoltenSec"/> after contact. A glove that
/// holds it more than <see cref="HoldSec"/> of that time drops it at its feet, live, and cannot take it again until it cools.
/// Decided by the play clock and the possession, never a roll; the same rule for a CPU glove and a player's. A catch still
/// counts: a caught fly is an out before any drop.
/// </summary>
public sealed record HotBall(double MoltenSec, double HoldSec)
{
    /// <summary>The longest a ball may stay molten: every bend ends within two seconds of contact (§13).</summary>
    public const double MaxMoltenSec = 2;

    /// <summary>Seconds the ball stays molten from <paramref name="elapsed"/> (seconds since contact); 0 once it has cooled.</summary>
    public double MoltenLeft(double elapsed) => Math.Max(0, MoltenSec - elapsed);

    /// <summary>
    /// Seconds of the molten window a possession that began at <paramref name="heldSince"/> has spent in the glove by
    /// <paramref name="elapsed"/>: the hold, counted only while the ball is molten.
    /// </summary>
    public double HeldHot(double heldSince, double elapsed) =>
        heldSince < 0 ? 0 : Math.Max(0, Math.Min(elapsed, MoltenSec) - heldSince);

    /// <summary>The glove holding since <paramref name="heldSince"/> drops the ball now: it has held it more than <see cref="HoldSec"/> while molten.</summary>
    public bool Drops(double heldSince, double elapsed) => HeldHot(heldSince, elapsed) > HoldSec;

    /// <summary>
    /// Seconds a glove that takes the ball at <paramref name="elapsed"/> may hold it before it drops; infinite when it takes the
    /// ball late enough, or cool, that the hold cannot pass <see cref="HoldSec"/> inside the molten window.
    /// </summary>
    public double HoldLeft(double heldSince, double elapsed) =>
        heldSince < 0 || heldSince + HoldSec >= MoltenSec ? double.PositiveInfinity : Math.Max(0, heldSince + HoldSec - elapsed);
}

/// <summary>
/// A star swing's jagged flight (spec §13): the batted ball jags sideways <see cref="OffsetFt"/> at <see cref="FirstAt"/> of its
/// jag window, runs on beside its line, and jags back onto the line at <see cref="SecondAt"/>; each jag takes
/// <see cref="Span"/> of the window. The window is the ball's air time to its first landing (the ground, the wall, the fence or
/// the stands) and never longer than <see cref="WithinSec"/>, so the ball is back on its line before it lands: the landing, the
/// fair / foul verdict and the fence are the straight ball's. The jagged ball is the real ball — the gloves, the CPU's read and
/// the drawn ball all take its position from the one path.
/// </summary>
public sealed record BallJag(double OffsetFt, double FirstAt, double SecondAt, double Span)
{
    /// <summary>The largest jag a row may name.</summary>
    public const double MaxOffsetFt = 3;

    /// <summary>Every bend ends within two seconds of contact (§13).</summary>
    public const double WithinSec = 2;

    /// <summary>How far the ball is off its line at <paramref name="s"/> of the jag window: out, beside, back, 0 outside it.</summary>
    public double OffFt(double s)
    {
        if (s <= FirstAt || s >= SecondAt + Span) return 0;
        if (s < FirstAt + Span) return OffsetFt * (s - FirstAt) / Span;
        if (s <= SecondAt) return OffsetFt;
        return OffsetFt * (1 - (s - SecondAt) / Span);
    }

    /// <summary>
    /// The jag window for a path: the time of its first landing mark, capped at <see cref="WithinSec"/>; 0 when the path
    /// has none.
    /// </summary>
    public static double WindowSec(IReadOnlyList<Sample> samples)
    {
        var i = BallFlight.LandingIndex(samples);
        var land = i < 0 ? (samples.Count > 0 ? samples[^1].T : 0) : samples[i].T;
        return Math.Min(land, WithinSec);
    }

    /// <summary>
    /// The side the ball jags to, as a unit vector in (X, Z): square to its line from contact to its landing mark, toward the
    /// middle of the field, so the jag never carries a fair ball over a foul line. A ball straight up the middle jags to −X.
    /// </summary>
    public static (double X, double Z) Side(IReadOnlyList<Sample> samples)
    {
        if (samples.Count == 0) return (0, 0);
        var i = BallFlight.LandingIndex(samples);
        var end = i < 0 ? samples[^1] : samples[i];
        var (dx, dz) = (end.X - samples[0].X, end.Z - samples[0].Z);
        var len = Math.Sqrt(dx * dx + dz * dz);
        if (len < 1e-9) return (0, 0);
        var (nx, nz) = (-dz / len, dx / len);
        return nx * dx <= 0 ? (nx, nz) : (-nx, -nz);
    }

    /// <summary>The path with the jags laid on it: every sample before the window's end moved sideways, every one after it untouched.</summary>
    public IReadOnlyList<Sample> Apply(IReadOnlyList<Sample> samples)
    {
        var w = WindowSec(samples);
        if (w <= 0) return samples;
        var (sx, sz) = Side(samples);
        if (sx == 0 && sz == 0) return samples;
        var list = new List<Sample>(samples.Count);
        foreach (var s in samples)
        {
            var off = s.T < w ? OffFt(s.T / w) : 0;
            if (off == 0) { list.Add(s); continue; }
            var (x, z) = (s.X + sx * off, s.Z + sz * off);
            list.Add(s with { X = x, Z = z, Dist = Math.Sqrt(x * x + z * z) });
        }
        return list;
    }
}

/// <summary>
/// The star skills as loaded from JSON. The JSON is the only copy (spec §13): no C# switch may
/// re-type a multiplier. Callers with a <see cref="ContentCatalog"/> use its table; a caller
/// without one falls back to the table found from the data root, like <see cref="Rules"/>.
/// </summary>
public sealed class StarSkillTable
{
    public const string Path = "abilities/star-skills.json";

    public StarSkillTable(IReadOnlyDictionary<string, StarPitchSkill> pitches, IReadOnlyDictionary<string, StarSwingSkill> swings)
    {
        Pitches = pitches;
        Swings = swings;
    }

    public IReadOnlyDictionary<string, StarPitchSkill> Pitches { get; }
    public IReadOnlyDictionary<string, StarSwingSkill> Swings { get; }

    public StarPitchSkill? Pitch(string? id) =>
        !string.IsNullOrEmpty(id) && Pitches.TryGetValue(id, out var p) ? p : null;

    public StarSwingSkill? Swing(string? id) =>
        !string.IsNullOrEmpty(id) && Swings.TryGetValue(id, out var s) ? s : null;

    /// <summary>An empty table: every lookup is the identity. Only for callers that own no data at all.</summary>
    public static StarSkillTable Empty { get; } = new(
        new Dictionary<string, StarPitchSkill>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, StarSwingSkill>(StringComparer.OrdinalIgnoreCase));

    static StarSkillTable? _default;

    /// <summary>The table found from the data root, else empty.</summary>
    public static StarSkillTable Default => _default ??= LoadDefault();

    public static StarSkillTable Or(StarSkillTable? table) => table ?? Default;

    static StarSkillTable LoadDefault()
    {
        // Outside the catch on purpose: see RulesTable.LoadDefault. A named root or overlay that
        // cannot be honoured stops the run rather than quietly handing back the control.
        var root = ContentCatalog.TryFindDataRoot();
        if (root is null) return Empty;
        try
        {
            return ContentCatalog.Load(root).StarSkills;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Empty;
        }
    }
}

/// <summary>Star-skill lookups (spec §13). Every number comes from the table; an unknown id bends nothing.</summary>
public static class StarSkills
{
    public static double PitchSpeedMul(string? id, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(id)?.SpeedMul ?? 1.0;

    public static int StaminaCost(string? starPitch, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(starPitch)?.StaminaCost ?? 0;

    public static double SwingExitMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.ExitVeloMul ?? 1.0;

    /// <summary>The kind a sidekick's special names: the generic pool (§13). A captain's specials name any other kind.</summary>
    public const string GenericKind = "generic";

    /// <summary>How much farther a star swing's fly carries from its apex (§13); 1 for a swing whose row names none.</summary>
    public static double SwingApexCarryMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.ApexCarryMul ?? 1.0;

    /// <summary>How much larger a star swing's Perfect ring is (§13); 1 for a swing whose row names none.</summary>
    public static double SwingPerfectRingMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.PerfectRingMul ?? 1.0;

    /// <summary>How much taller a star swing's contact oval is (§13); 1 for a swing whose row names none.</summary>
    public static double SwingOvalHeightMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.OvalHeightMul ?? 1.0;

    /// <summary>The jagged flight a star swing's ball flies (§13); null for a swing whose row names none.</summary>
    public static BallJag? SwingJag(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.Jag;

    /// <summary>A role player's star swing names its launch; a captain's keeps the swing's own.</summary>
    public static double? SwingLaunchDeg(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.LaunchDeg;

    /// <summary>
    /// What <paramref name="who"/>'s Star Pitch costs their team (§12): the carrier's price (a captain's or a
    /// sidekick's), plus <c>costs.guestCaptainSurcharge</c> when a captain acts for a team <paramref name="teamCaptain"/> captains.
    /// </summary>
    public static int PitchCost(Character who, Character teamCaptain, RulesTable rules, StarSkillTable? table = null) =>
        Price(who, teamCaptain, rules);

    /// <summary>What <paramref name="who"/>'s Star Swing costs his team: the same rule as <see cref="PitchCost"/>.</summary>
    public static int SwingCost(Character who, Character teamCaptain, RulesTable rules, StarSkillTable? table = null) =>
        Price(who, teamCaptain, rules);

    static int Price(Character who, Character teamCaptain, RulesTable rules) =>
        rules.Stars.Prices.Of(who)
        + (who.Captain && !who.Id.Equals(teamCaptain.Id, StringComparison.OrdinalIgnoreCase)
            ? rules.Stars.Costs.GuestCaptainSurcharge
            : 0);

    /// <summary>
    /// How long a special owns the ball or the field before baseball resumes.
    /// Readable clip, not a full-screen blind.
    /// </summary>
    public static double SpectacleSeconds(string? id) =>
        string.IsNullOrEmpty(id) ? 0 : 2.0;
}
