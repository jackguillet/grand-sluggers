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
    PitchVanish? Vanish = null,
    /// <summary>A path that skips twice on the dirt in front of the plate and pops up onto the unchanged crossing, or null (§13).</summary>
    PitchSkips? Skips = null,
    /// <summary>
    /// The pitch flies true to its aim (§13, Star Dot): the family's own drop and sweep are off and the CPU arm lays no scatter
    /// on its intent, so it crosses exactly on the aimed spot, the rubber walk and the stick's held break included.
    /// </summary>
    bool Dot = false,
    /// <summary>A ball put in play off this pitch leaves this many degrees lower (§13, Star Sinker); 0 is the ordinary launch.</summary>
    double SinkDeg = 0,
    /// <summary>A slow start on a high arc that falls through the zone on the ordinary instant at the ordinary crossing, or null (§13).</summary>
    PitchLob? Lob = null,
    /// <summary>The ball leaves the hand this many feet wider, out on the hand's side (§13, Star Sidearm); the crossing is unchanged.</summary>
    double SidearmFt = 0)
{
    /// <summary>The most a sinker may lower the launch: a grounder more often, not a ball driven into the plate.</summary>
    public const double MaxSinkDeg = 15;

    /// <summary>The widest a sidearm release may name: a wider slot, not a throw from the dugout.</summary>
    public const double MaxSidearmFt = 3;
}

/// <summary>
/// A star pitch's lob (spec §13, Star Lob): the ball leaves the hand at <see cref="PaceMul"/> of its pace along its path and
/// speeds up evenly as it falls, so it covers the whole path on the ordinary clock; over it rides a high arc, <see cref="ArcFt"/>
/// above the ordinary path at mid-flight and nothing at the release or the plate. The path's end, the crossing and the arrival
/// instant are the ordinary pitch's, so the umpire, the bat, the CPU and the timing window read the ordinary pitch.
/// </summary>
public sealed record PitchLob(double PaceMul, double ArcFt)
{
    /// <summary>The slowest start a row may name: a lob, not a pitch that stops in the air.</summary>
    public const double MinPaceMul = 0.5;
    /// <summary>The highest arc a row may name, in feet over the ordinary path at mid-flight.</summary>
    public const double MaxArcFt = 8;

    /// <summary>
    /// How far along its path the ball is at time fraction <paramref name="u"/>: <see cref="PaceMul"/>·u + (1 − <see cref="PaceMul"/>)·u²,
    /// a pace of <see cref="PaceMul"/> at the release that grows evenly to 2 − <see cref="PaceMul"/> at the plate; 0 at 0 and 1 at 1.
    /// </summary>
    public double Progress(double u)
    {
        u = Math.Clamp(u, 0, 1);
        return PaceMul * u + (1 - PaceMul) * u * u;
    }

    /// <summary>The arc's height over the ordinary path at time fraction <paramref name="u"/>: 4·<see cref="ArcFt"/>·u·(1 − u), the whole arc at mid-flight, 0 at both ends.</summary>
    public double Lift(double u)
    {
        u = Math.Clamp(u, 0, 1);
        return 4 * ArcFt * u * (1 - u);
    }
}

/// <summary>
/// A star pitch's skips (spec §13): the ball comes down onto the dirt at <see cref="FirstAt"/> of the flight, skips up
/// <see cref="HopFt"/> and back down onto the dirt at <see cref="SecondAt"/>, then pops up off the second skip onto the aimed
/// crossing, highest at the plate. Only the height changes: every point of the flight is over the ordinary ball's ground point
/// at the ordinary instant, and at the plate the ball is on the ordinary crossing, so the umpire, the bat, the timing window
/// and the CPU read the ordinary pitch. The dirt touches are part of the pitch's path, not a bounce the umpire calls: a
/// Skipping Stone is a strike or a ball by its crossing alone. Nothing is rolled: the skips are a fixed curve.
/// </summary>
public sealed record PitchSkips(double FirstAt, double SecondAt, double HopFt)
{
    /// <summary>The highest skip a row may name, in reference-zone feet: a skip along the dirt, not a hop over the batter.</summary>
    public const double MaxHopFt = 2;

    /// <summary>
    /// The ball's height at <paramref name="u"/> of the flight, from the ordinary ball's height <paramref name="ordinaryY"/> at
    /// the same instant and its crossing height <paramref name="crossingY"/>: down onto the dirt by <see cref="FirstAt"/> (the
    /// ordinary height × (1 − (u / firstAt)²)), one skip of <see cref="HopFt"/> × <paramref name="scale"/> to
    /// <see cref="SecondAt"/>, then up onto the crossing along (1 − (1 − s)²), s the share of the last stretch, so the ball
    /// climbs the whole way into the zone and is exactly the ordinary ball at the plate.
    /// </summary>
    public double Height(double u, double ordinaryY, double crossingY, double scale)
    {
        u = Math.Clamp(u, 0, 1);
        if (u >= 1) return ordinaryY;
        if (u <= FirstAt)
        {
            var a = u / FirstAt;
            return ordinaryY * (1 - a * a);
        }
        if (u <= SecondAt)
        {
            var b = (u - FirstAt) / (SecondAt - FirstAt);
            return 4 * HopFt * scale * b * (1 - b);
        }
        var c = 1 - (u - SecondAt) / (1 - SecondAt);
        return crossingY * (1 - c * c);
    }
}


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
    SwingDustBowl? DustBowl = null,
    /// <summary>A ball off this swing that hops over the first infield glove it reaches, then drops back onto its line, or null (§13).</summary>
    BallHop? Hop = null,
    /// <summary>
    /// The ball off this swing goes this many degrees toward the batter's pull line (§13, Star Pull); a negative number goes toward
    /// the opposite field (Star Opposite); 0 is the ordinary spray.
    /// </summary>
    double PullDeg = 0,
    /// <summary>The swing lays down a bunt that rolls along the batter's pull line and stops just fair, or null (§13, Star Drag Bunt).</summary>
    SwingDragBunt? DragBunt = null,
    /// <summary>A fair ball off this swing leaves its first hop this many times as fast upward (§13, Star Chopper); 1 is the ordinary hop.</summary>
    double FirstHopBounceMul = 1)
{
    /// <summary>The highest first-hop bounce a row may name: a chopper, not a moon shot.</summary>
    public const double MaxBounceMul = 3;

    /// <summary>The largest pull a row may name: a lean toward a line, not a ball sent foul.</summary>
    public const double MaxPullDeg = 20;

    /// <summary>The lowest launch a star swing may name: a chopper driven into the dirt, not into the plate.</summary>
    public const double MinLaunchDeg = -15;

    /// <summary>
    /// The pull in degrees of spray for a batter who bats <paramref name="bats"/> (§5.3 signs: a right-handed batter pulls toward
    /// third, negative spray; a left-handed batter toward first).
    /// </summary>
    public double PullSprayDeg(Hand bats) => PullDeg == 0 ? 0 : -SweetSpot.TipSign(bats) * PullDeg;

    /// <summary>The tallest contact oval a row may name: twice the batter's zone, never more.</summary>
    public const double MaxOvalHeightMul = 2;
    /// <summary>The longest stall a row may name: the ball spins, then baseball resumes inside the two-second rule.</summary>
    public const double MaxStallSec = 1.2;

    /// <summary>The highest launch a stalling swing may name: the stall is a grounder's, so its first hop comes early.</summary>
    public const double MaxStallLaunchDeg = 4;

    /// <summary>The largest Perfect ring a row may name: twice the ordinary heart, and never past the drawn oval (<see cref="SweetSpot.Zone"/>).</summary>
    public const double MaxPerfectRingMul = 2;

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
/// A star swing's drag bunt (spec §13, Star Drag Bunt): the timed star swing squares at contact and lays the ball down the
/// batter's pull line (third for a right-handed batter, first for a left-handed one). It leaves at the bunt's own exit for the
/// contact (<c>batting.bunt.response</c>) and the row's launch, and its bearing is solved on the ball's own path in the park —
/// its roll, the ground and the wind — so the ball comes to rest <see cref="InsetFt"/> inside the foul line. The ball runs out
/// from home along a line that ends inside the chalk, so it rolls along the line and stays fair all the way. Geometry decides
/// it; nothing is rolled. A glove that reaches it first fields a bunt.
/// </summary>
public sealed record SwingDragBunt(double InsetFt)
{
    /// <summary>The farthest inside the line a row may stop the ball: a drag bunt hugs the chalk.</summary>
    public const double MaxInsetFt = 1;

    /// <summary>The highest launch a drag bunt may name: it rolls, it does not pop.</summary>
    public const double MaxLaunchDeg = 10;

    /// <summary>The side of the field the bunt goes to for a batter who bats <paramref name="bats"/>: −1 the third-base line, +1 the first-base line.</summary>
    public static int Side(Hand bats) => -(int)SweetSpot.TipSign(bats);

    /// <summary>How far inside the foul line on <paramref name="side"/> the point (<paramref name="x"/>, <paramref name="z"/>) is, in feet; negative is foul.</summary>
    public static double InsideLineFt(double x, double z, int side) => (z - side * x) * Math.Sqrt(0.5);

    /// <summary>
    /// The spray (rounded to a tenth of a degree, as every contact is) that brings a ball of <paramref name="exitMph"/> and
    /// <paramref name="launchDeg"/> to rest <see cref="InsetFt"/> inside the <paramref name="side"/> line in <paramref name="park"/>:
    /// fly it, read where it stops, turn the bearing by what is off, and fly it again.
    /// </summary>
    public double SprayDeg(double exitMph, double launchDeg, int side, Park park, RulesTable rules)
    {
        var line = side * AtBatResolver.FoulLineDeg;
        var spray = line - side;
        for (var i = 0; i < 6; i++)
        {
            var rest = Rest(exitMph, launchDeg, spray, park, rules);
            var dist = Math.Sqrt(rest.X * rest.X + rest.Z * rest.Z);
            if (dist < 1e-6) break;
            var want = line - side * Math.Asin(Math.Min(1, InsetFt / dist)) * 180 / Math.PI;
            var off = want - FieldBounds.SprayDeg(rest.X, rest.Z);
            spray += off;
            if (Math.Abs(off) < 1e-6) break;
        }
        return Math.Round(spray, 1);
    }

    /// <summary>Where the ball of this contact comes to rest (the last point of its path) in <paramref name="park"/>.</summary>
    public static (double X, double Z) Rest(double exitMph, double launchDeg, double sprayDeg, Park park, RulesTable rules)
    {
        var path = BallFlight.Trajectory(exitMph, launchDeg, sprayDeg, park, rules);
        var last = path[^1];
        return (last.X, last.Z);
    }
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
/// A star swing's hop over a glove (spec §13): when the ball's path in the air enters the reach of an infield glove (the pitcher
/// and the four infielders, not the catcher) at glove height — at or under the standing catch height — the ball hops over that
/// glove and drops back onto its line. The hop is laid on the path by the ball's distance, across the ground, from the glove's
/// spot: <see cref="HeightFt"/> up while within the glove's reach plus <see cref="PadFt"/>, rising into that and dropping back
/// out over <see cref="RampFt"/> on either side, 0 beyond. Only the height of that one pass changes: the ball's ground track,
/// its clock, its landing and everything after it are the straight ball's. The hop is decided live, against where the bodies
/// stand as the ball comes, and it must end before the ball lands and within <see cref="WithinSec"/> of contact, or there is
/// none. The hopped ball is the one real ball for every glove, the pursuit and the CPU.
/// </summary>
public sealed record BallHop(double HeightFt, double PadFt, double RampFt)
{
    /// <summary>The highest hop a row may name: over a glove, not over the infield.</summary>
    public const double MaxHeightFt = 8;

    /// <summary>The widest pad or ramp a row may name.</summary>
    public const double MaxEdgeFt = 4;

    /// <summary>Every bend ends within two seconds of contact (§13).</summary>
    public const double WithinSec = 2;

    /// <summary>How far from the glove the hop reaches: the glove's reach, the pad and the ramp.</summary>
    public double OuterFt(double reachFt) => reachFt + PadFt + RampFt;

    /// <summary>
    /// The lift at <paramref name="d"/> feet across the ground from the glove's spot: <see cref="HeightFt"/> inside the reach and
    /// the pad, a half-cosine down to 0 across the ramp, 0 past it.
    /// </summary>
    public double Lift(double d, double reachFt)
    {
        var top = reachFt + PadFt;
        if (d <= top) return HeightFt;
        if (d >= top + RampFt) return 0;
        return HeightFt * 0.5 * (1 + Math.Cos(Math.PI * (d - top) / RampFt));
    }

    /// <summary>
    /// When the path from <paramref name="now"/> first enters the glove's reach at glove height: the first sample, before the
    /// landing mark and not after <paramref name="untilT"/>, within <paramref name="reachFt"/> of (<paramref name="gx"/>,
    /// <paramref name="gz"/>) across the ground with a height from 0 to <paramref name="gloveHeightFt"/>. Null when it never does.
    /// </summary>
    public static double? Entry(IReadOnlyList<Sample> path, double now, double gx, double gz, double reachFt,
        double gloveHeightFt, double untilT)
    {
        for (var i = 0; i < path.Count; i++)
        {
            var s = path[i];
            if (i > 0 && s.Event != SampleEvent.None) return null;
            if (s.T < now) continue;
            if (s.T > untilT) return null;
            if (s.Height >= 0 && s.Height <= gloveHeightFt && Diamond.Dist(s.X, s.Z, gx, gz) <= reachFt) return s.T;
        }
        return null;
    }

    /// <summary>Does the path between <paramref name="from"/> and <paramref name="to"/> come within <paramref name="radiusFt"/> of the spot across the ground?</summary>
    public static bool Nears(IReadOnlyList<Sample> path, double from, double to, double gx, double gz, double radiusFt, RulesTable rules)
    {
        var a = BallFlight.PointAt(path, from, rules);
        var b = BallFlight.PointAt(path, to, rules);
        if (Diamond.Dist(a.X, a.Z, gx, gz) < radiusFt || Diamond.Dist(b.X, b.Z, gx, gz) < radiusFt) return true;
        foreach (var s in path)
        {
            if (s.T <= from) continue;
            if (s.T >= to) break;
            if (Diamond.Dist(s.X, s.Z, gx, gz) < radiusFt) return true;
        }
        return false;
    }

    /// <summary>
    /// The path with the hop over the glove at (<paramref name="gx"/>, <paramref name="gz"/>) laid on it: the pass through the
    /// hop's ring (<see cref="OuterFt"/>) that the ball is in or comes to next at <paramref name="now"/> is lifted by
    /// <see cref="Lift"/>, and the points where the path crosses the ring are added as samples, so the ball leaves its line and
    /// rejoins it exactly there. Every other sample is untouched. Null when the path has no such pass, or the pass would not be
    /// over before the landing mark or before <see cref="WithinSec"/>: then there is no hop.
    /// </summary>
    public List<Sample>? Apply(IReadOnlyList<Sample> path, double now, double gx, double gz, double reachFt)
    {
        var outer = OuterFt(reachFt);
        bool Inside(int i) => Diamond.Dist(path[i].X, path[i].Z, gx, gz) < outer;
        var (i0, i1) = (-1, -1);
        for (var i = 1; i < path.Count; i++)
        {
            if (Inside(i))
            {
                if (i0 < 0) i0 = i;
                continue;
            }
            if (i0 < 0) continue;
            if (path[i].T >= now)
            {
                i1 = i;
                break;
            }
            i0 = -1;
        }
        if (i0 < 0 || i1 < 0) return null;
        var land = BallFlight.LandingIndex(path);
        if ((land >= 0 && i1 >= land) || path[i1].T > WithinSec) return null;
        var list = new List<Sample>(path.Count + 2);
        for (var i = 0; i < i0; i++) list.Add(path[i]);
        list.Add(OnRing(path[i0 - 1], path[i0], gx, gz, outer));
        for (var i = i0; i < i1; i++)
        {
            var s = path[i];
            list.Add(s with { Height = s.Height + Lift(Diamond.Dist(s.X, s.Z, gx, gz), reachFt) });
        }
        list.Add(OnRing(path[i1 - 1], path[i1], gx, gz, outer));
        for (var i = i1; i < path.Count; i++) list.Add(path[i]);
        return list;
    }

    /// <summary>The point on the straight segment from <paramref name="a"/> to <paramref name="b"/> where it crosses the ring, found by halving; its height is the line's.</summary>
    static Sample OnRing(Sample a, Sample b, double gx, double gz, double radiusFt)
    {
        var inA = Diamond.Dist(a.X, a.Z, gx, gz) < radiusFt;
        var (lo, hi) = (0.0, 1.0);
        for (var n = 0; n < 40; n++)
        {
            var mid = (lo + hi) / 2;
            var inside = Diamond.Dist(a.X + (b.X - a.X) * mid, a.Z + (b.Z - a.Z) * mid, gx, gz) < radiusFt;
            if (inside == inA) lo = mid; else hi = mid;
        }
        var u = (lo + hi) / 2;
        var (x, z) = (a.X + (b.X - a.X) * u, a.Z + (b.Z - a.Z) * u);
        return new Sample(a.T + (b.T - a.T) * u, Math.Sqrt(x * x + z * z), a.Height + (b.Height - a.Height) * u, x, z);
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

    /// <summary>
    /// The name a player reads for a star pitch (§13): its row's <c>name</c>, the one copy. An id with no row reads as the id
    /// itself, so a missing row shows up on screen instead of hiding behind a made-up name.
    /// </summary>
    public static string PitchName(string? id, StarSkillTable? table = null) =>
        string.IsNullOrEmpty(id) ? "" : StarSkillTable.Or(table).Pitch(id)?.Name is { Length: > 0 } name ? name : id;

    /// <summary>The name a player reads for a star swing: its row's <c>name</c>, else the id (<see cref="PitchName"/>).</summary>
    public static string SwingName(string? id, StarSkillTable? table = null) =>
        string.IsNullOrEmpty(id) ? "" : StarSkillTable.Or(table).Swing(id)?.Name is { Length: > 0 } name ? name : id;

    /// <summary>The kind a sidekick's special names: the generic pool (§13). A captain's specials name any other kind.</summary>
    public const string GenericKind = "generic";

    /// <summary>How much farther a star swing's fly carries from its apex (§13); 1 for a swing whose row names none.</summary>
    public static double SwingApexCarryMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.ApexCarryMul ?? 1.0;

    /// <summary>How much larger a star swing's Perfect ring is (§13); 1 for a swing whose row names none.</summary>
    public static double SwingPerfectRingMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.PerfectRingMul ?? 1.0;

    /// <summary>The hop over a glove a star swing's ball makes (§13); null for a swing whose row names none.</summary>
    public static BallHop? SwingHop(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.Hop;

    /// <summary>How much taller a star swing's contact oval is (§13); 1 for a swing whose row names none.</summary>
    public static double SwingOvalHeightMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.OvalHeightMul ?? 1.0;

    /// <summary>The jagged flight a star swing's ball flies (§13); null for a swing whose row names none.</summary>
    public static BallJag? SwingJag(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.Jag;

    /// <summary>How many degrees lower a ball put in play off this star pitch leaves (§13, Star Sinker); 0 for a pitch whose row names none.</summary>
    public static double PitchSinkDeg(string? starPitch, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(starPitch)?.SinkDeg ?? 0;

    /// <summary>The CPU arm's scatter is off for this star pitch (§13, Star Dot): it crosses where it was aimed.</summary>
    public static bool PitchIsDot(string? starPitch, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(starPitch)?.Dot == true;

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
