namespace GrandSluggers.Sim;

/// <summary>
/// Which way a body faces (spec §8.2, #611, #576). One rule for every body on the field:
/// <list type="bullet">
/// <item>moving faster than a walk (<see cref="CartoonJuice.WalkFtPerSec"/>): its own velocity — turn and run;</item>
/// <item>planted, or holding the ball: what it looks at (the ball for a glove, the next bag for a runner);</item>
/// <item>releasing a throw: the throw target (pinned, even while the feet still drift);</item>
/// <item>the <b>backpedal</b>, the one named short-range case: a glove under a fly still in the air, inside
/// <see cref="FeelTable.BackpedalFt"/> of the plant and moving away from the ball, faces the ball.</item>
/// </list>
/// The body turns toward that heading at <see cref="FeelTable.BodyTurnDegPerSec"/> × the frame's seconds, so a
/// 30 fps and a 60 fps body face the same way at the same play time. A ball within
/// <see cref="FeelTable.FaceBallMinFt"/> (overhead, or in the glove) is no heading: the body keeps the one it has,
/// so the ball passing over the plant never turns it around. Yaw is degrees about +Y: 0 = +Z, 90 = +X
/// (Unity's <c>Quaternion.LookRotation</c>). Presentation only — the sim's bodies have no facing.
/// </summary>
public static class BodyFacing
{
    public enum Source { Hold, Look, Run, Backpedal }

    /// <summary>The feel numbers the turn reads (data/feel/table.json), plus the walk threshold the locomotion takes share.</summary>
    public readonly record struct Rates(
        double TurnDegPerSec,
        double WalkFtPerSec,
        double SmoothSec,
        double BackpedalFt,
        double FaceBallMinFt,
        double TeleportFtPerSec)
    {
        public static Rates Of(FeelTable feel) => new(
            feel.BodyTurnDegPerSec, CartoonJuice.WalkFtPerSec, feel.HeadingSmoothSec,
            feel.BackpedalFt, feel.FaceBallMinFt, feel.HeadingTeleportFtPerSec);

        /// <summary>The table's shipped values, for a body placed before the content is bound.</summary>
        public static readonly Rates Default = new(720, CartoonJuice.WalkFtPerSec, 0.08, 12, 3, 90);
    }

    /// <param name="LookX">World vector from the body to what it faces when planted; zero keeps the heading.</param>
    /// <param name="Pinned">Face the look even while moving (a throw on release, the pitcher on the rubber).</param>
    /// <param name="Fly">This body is the glove under a ball still in the air, running to (<paramref name="PlantX"/>, <paramref name="PlantZ"/>).</param>
    public readonly record struct Facts(
        double LookX,
        double LookZ,
        bool Pinned = false,
        bool Fly = false,
        double PlantX = 0,
        double PlantZ = 0)
    {
        public bool HasLook => LookX * LookX + LookZ * LookZ > 1e-4;
    }

    public static double YawOf(double x, double z) => Math.Atan2(x, z) * 180.0 / Math.PI;

    /// <summary>Degrees into (−180, 180].</summary>
    public static double Wrap(double deg)
    {
        deg %= 360.0;
        if (deg > 180.0) deg -= 360.0;
        if (deg <= -180.0) deg += 360.0;
        return deg;
    }

    /// <summary>Unsigned angle between two headings, degrees.</summary>
    public static double Between(double aDeg, double bDeg) => Math.Abs(Wrap(aDeg - bDeg));

    /// <summary>The dt-scaled turn: at most <paramref name="degPerSec"/> × <paramref name="dt"/> toward the want, never past it.</summary>
    public static double Turn(double yawDeg, double wantDeg, double degPerSec, double dt)
    {
        var delta = Wrap(wantDeg - yawDeg);
        var max = Math.Max(0, degPerSec * dt);
        return Wrap(yawDeg + Math.Clamp(delta, -max, max));
    }

    /// <summary>
    /// A defensive body in a live play: the throw target while it releases, else the ball (none when the ball is
    /// overhead or in its glove). <paramref name="fly"/> marks the glove running to a fly's plant (the backpedal case).
    /// </summary>
    public static Facts Fielder(
        double x, double z,
        double ballX, double ballZ,
        bool releasing, double throwToX, double throwToZ,
        bool fly, double plantX, double plantZ,
        Rates rates)
    {
        if (releasing) return new Facts(throwToX - x, throwToZ - z, Pinned: true);
        var dx = ballX - x;
        var dz = ballZ - z;
        var overhead = dx * dx + dz * dz < rates.FaceBallMinFt * rates.FaceBallMinFt;
        return overhead
            ? new Facts(0, 0, Fly: fly, PlantX: plantX, PlantZ: plantZ)
            : new Facts(dx, dz, Fly: fly, PlantX: plantX, PlantZ: plantZ);
    }

    /// <summary>Which heading wins for a body at (x, z) moving at (velX, velZ).</summary>
    public static Source SourceOf(Facts facts, double x, double z, double velX, double velZ, Rates rates)
    {
        if (facts.Pinned && facts.HasLook) return Source.Look;
        if (velX * velX + velZ * velZ > rates.WalkFtPerSec * rates.WalkFtPerSec)
            return Backpedals(facts, x, z, velX, velZ, rates) ? Source.Backpedal : Source.Run;
        return facts.HasLook ? Source.Look : Source.Hold;
    }

    /// <summary>The last few feet to a fly's plant with the ball coming over the head: moving away from the ball.</summary>
    public static bool Backpedals(Facts facts, double x, double z, double velX, double velZ, Rates rates) =>
        facts.Fly && facts.HasLook
        && Diamond.Dist(x, z, facts.PlantX, facts.PlantZ) <= rates.BackpedalFt
        && velX * facts.LookX + velZ * facts.LookZ < 0;
}

/// <summary>
/// One body's heading over time (<see cref="BodyFacing"/>): measures its ground velocity from where it is placed
/// each frame, picks the source, and turns toward it at the feel table's rate. The Unity actor and the headless
/// scenarios run this same object, so a heading counted in a test is the heading drawn on screen.
/// </summary>
public sealed class BodyHeading
{
    double _x, _z, _vx, _vz;
    bool _placed;

    public BodyHeading(double yawDeg = 0)
    {
        YawDeg = BodyFacing.Wrap(yawDeg);
        WantDeg = YawDeg;
    }

    public double YawDeg { get; private set; }
    /// <summary>The heading the body is turning toward.</summary>
    public double WantDeg { get; private set; }
    public BodyFacing.Source Source { get; private set; } = BodyFacing.Source.Hold;
    public double VelX => _vx;
    public double VelZ => _vz;
    public double SpeedFtPerSec => Math.Sqrt(_vx * _vx + _vz * _vz);

    /// <summary>Stand here facing this way, at rest (a still, a spawn).</summary>
    public void Snap(double x, double z, double yawDeg)
    {
        _x = x;
        _z = z;
        _vx = _vz = 0;
        _placed = true;
        YawDeg = WantDeg = BodyFacing.Wrap(yawDeg);
        Source = BodyFacing.Source.Hold;
    }

    public double Tick(double x, double z, double dt, BodyFacing.Facts facts, BodyFacing.Rates rates)
    {
        if (!_placed)
        {
            _x = x;
            _z = z;
            _placed = true;
        }
        if (dt > 1e-6)
        {
            var ix = (x - _x) / dt;
            var iz = (z - _z) / dt;
            if (ix * ix + iz * iz > rates.TeleportFtPerSec * rates.TeleportFtPerSec)
                _vx = _vz = 0;
            else
            {
                var a = rates.SmoothSec > 0 ? 1 - Math.Exp(-dt / rates.SmoothSec) : 1;
                _vx += (ix - _vx) * a;
                _vz += (iz - _vz) * a;
            }
        }
        _x = x;
        _z = z;
        Source = BodyFacing.SourceOf(facts, x, z, _vx, _vz, rates);
        WantDeg = Source switch
        {
            BodyFacing.Source.Run => BodyFacing.YawOf(_vx, _vz),
            BodyFacing.Source.Look or BodyFacing.Source.Backpedal => BodyFacing.YawOf(facts.LookX, facts.LookZ),
            _ => WantDeg
        };
        YawDeg = BodyFacing.Turn(YawDeg, WantDeg, rates.TurnDegPerSec, dt);
        return YawDeg;
    }
}
