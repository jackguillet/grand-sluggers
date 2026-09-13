namespace GrandSluggers.Sim;

/// <summary>
/// One named shot per play beat (spec §15), every id from <c>data/feel/shots.json</c>. SET and
/// the pitch: 1P follows the role (mound when pitching, plate when batting), 1v1 stays behind
/// home. In play the beat is decided from typed live state, never from a caption: a grounder is
/// the 45° follow on the dirt under the ball (<c>diamond</c>), a liner or fly pulls back
/// (<c>diamond-fly</c>), a home run is the <c>smash</c> override at the crack, a throw sits on the
/// bag it is going to (<c>throw</c>), a close play is the bag cam (<c>tag</c>), a steal or pickoff is
/// <c>throw</c> on the play's bag, a rundown follows the ball between the bags. The same table for
/// 1P and 1v1; the client owns no Vector3 of its own.
/// </summary>
public static class PlayCamera
{
    public enum Beat
    {
        Set,
        PitchFlight,
        Grounder,
        GrounderPull,
        Line,
        Fly,
        Homer,
        Wall,
        Throw,
        Tag,
        Smash,
        StealThrow,
        Rundown,
    }

    /// <summary>
    /// Live batted ball. 45° down from the home side of the dirt under the ball
    /// so CF is the top of the frame and home sits under second.
    /// </summary>
    public const string InPlay = "diamond";

    /// <summary>Same 45°, farther back and a little more FOV so a fly has grass.</summary>
    public const string InPlayFly = "diamond-fly";

    /// <summary>The bag cam a throw lands on (§15): the ball and the body arrive in one frame.</summary>
    public const string ThrowShot = "throw";

    /// <summary>The close-play bag cam (§9.6, §15): tighter than the throw, on the tag.</summary>
    public const string TagShot = "tag";

    /// <summary>The home-run override at the crack (§15): the batter's body for <c>smashHold</c>.</summary>
    public const string SmashShot = "smash";

    public const double InPlayLookDownDeg = 45;

    public readonly record struct Viewport(double X, double Y, double Depth);

    /// <summary>
    /// SET / throw. Two pads: always <see cref="AtBatShots.Plate"/>.
    /// One pad: <paramref name="pitchingSet"/> is the role — mound on the
    /// rubber, plate in the box. Every in-play beat is the same for one pad and two.
    /// </summary>
    public static string Shot(Beat beat, int seats = 1, bool pitchingSet = false)
    {
        var set = seats >= 2 || !pitchingSet ? AtBatShots.Plate : AtBatShots.Mound;
        return beat switch
        {
            Beat.Set or Beat.PitchFlight => set,
            Beat.Fly or Beat.Line or Beat.Homer or Beat.Wall => InPlayFly,
            Beat.Smash => SmashShot,
            Beat.Throw or Beat.StealThrow => ThrowShot,
            Beat.Tag => TagShot,
            _ => InPlay
        };
    }

    /// <summary>
    /// The live ball as the camera reads it this frame: typed state only (spec §15). <paramref name="Hit"/>
    /// is null on a runner play (steal, pickoff). <paramref name="SmashLeft"/> is the client's
    /// <c>smashHold</c> clock; <paramref name="PlayBag"/> is <c>LivePlaySystem.RunnerPlayBag</c>.
    /// </summary>
    public readonly record struct LiveView(
        double ElapsedSeconds,
        AtBatResult? Hit,
        bool RunnerPlay,
        bool Throwing,
        int ThrowBag,
        bool ClosePlay,
        int CloseBag,
        bool Rundown,
        int PlayBag,
        double SmashLeft,
        Vec3 Ball,
        Vec3 Batter);

    /// <summary>
    /// Which beat the live ball is in. Priority: the runner play sits on its bag; a home run smashes at
    /// the crack; every other hit holds the SET shot for <see cref="FeelTable.ContactCutSeconds"/>; then
    /// the close play, the throw, the rundown, and finally the class read from the typed hit.
    /// </summary>
    public static Beat LiveBeat(LiveView v, FeelTable feel)
    {
        if (v.RunnerPlay) return Beat.StealThrow;
        var hit = v.Hit;
        if (hit != null && hit.HomeRun && v.SmashLeft > 0) return Beat.Smash;
        if (hit != null && !hit.HomeRun && v.ElapsedSeconds < feel.ContactCutSeconds) return Beat.Set;
        if (v.ClosePlay && v.CloseBag > 0) return Beat.Tag;
        if (v.Throwing && v.ThrowBag > 0) return Beat.Throw;
        if (v.Rundown) return Beat.Rundown;
        return hit != null ? BeatFrom(hit) : Beat.Grounder;
    }

    /// <summary>The bag a beat frames, or 0 when it frames the ball / the body.</summary>
    public static int BeatBag(Beat beat, LiveView v) => beat switch
    {
        Beat.Tag => v.CloseBag,
        Beat.Throw => v.ThrowBag,
        Beat.StealThrow => v.PlayBag,
        _ => 0
    };

    /// <summary>
    /// The frame for this beat, from the named shot in <paramref name="shots"/>: null while the SET shot
    /// still holds (the contact cut has not come). A bag beat translates the authored shot onto the bag;
    /// the smash sits on the batter; everything else looks at the dirt under the ball.
    /// </summary>
    public static Framing? LiveFraming(CameraShots shots, LiveView v, FeelTable feel)
    {
        var beat = LiveBeat(v, feel);
        if (beat == Beat.Set) return null;
        var shot = shots.Must(Shot(beat));
        var bag = BeatBag(beat, v);
        if (bag > 0)
        {
            var at = Diamond.Bag(bag);
            return FollowBag(shot, at.X, at.Z);
        }
        if (beat == Beat.Smash) return Smash(shot, v.Batter);
        return FollowGround(shot, v.Ball);
    }

    /// <summary>
    /// Unity-style vertical FOV projection. Rubber in the bottom of mound SET
    /// is a viewport Y, not a look-at-dirt target.
    /// </summary>
    public static Viewport? Project(CameraShot shot, Vec3 p, double aspect = 16.0 / 9.0)
    {
        var fx = shot.Target.X - shot.Pos.X;
        var fy = shot.Target.Y - shot.Pos.Y;
        var fz = shot.Target.Z - shot.Pos.Z;
        var fl = Math.Sqrt(fx * fx + fy * fy + fz * fz);
        if (fl < 1e-6) return null;
        fx /= fl;
        fy /= fl;
        fz /= fl;
        var rx = fz;
        var rz = -fx;
        var rl = Math.Sqrt(rx * rx + rz * rz);
        if (rl < 1e-6) return null;
        rx /= rl;
        rz /= rl;
        var ux = fy * rz;
        var uy = fz * rx - fx * rz;
        var uz = -fy * rx;
        var dx = p.X - shot.Pos.X;
        var dy = p.Y - shot.Pos.Y;
        var dz = p.Z - shot.Pos.Z;
        var z = fx * dx + fy * dy + fz * dz;
        if (z <= 0.01) return null;
        var x = rx * dx + rz * dz;
        var y = ux * dx + uy * dy + uz * dz;
        var vfov = shot.Fov * Math.PI / 180.0;
        var hfov = 2 * Math.Atan(Math.Tan(vfov / 2) * aspect);
        var ndcX = x / z / Math.Tan(hfov / 2);
        var ndcY = y / z / Math.Tan(vfov / 2);
        return new Viewport(0.5 + 0.5 * ndcX, 0.5 + 0.5 * ndcY, z);
    }

    public static bool InFrame(Viewport? v, double margin = 0.04) =>
        v is { } p && p.X > margin && p.X < 1 - margin && p.Y > margin && p.Y < 1 - margin;

    /// <summary>The follow beat for a batted ball, from its typed class (§6.2, §15). A star swing follows like its class.</summary>
    public static Beat BeatFrom(AtBatResult hit)
    {
        if (hit.HomeRun) return Beat.Homer;
        var shape = hit.Class;
        if (shape == BattedBallClass.Wall) return Beat.Wall;
        if (shape.OnTheDirt())
            return hit.SprayDeg < -8 ? Beat.GrounderPull : Beat.Grounder;
        if (shape == BattedBallClass.Liner) return Beat.Line;
        return Beat.Fly;
    }

    public static string FromHit(AtBatResult hit) => Shot(BeatFrom(hit));

    public static string FollowShot(bool fly) => fly ? InPlayFly : InPlay;

    /// <summary>
    /// Translate a named shot so its authored look sits on <paramref name="subject"/>.
    /// Wall and live fly/homer are follow-cams, not a second JSON park still.
    /// </summary>
    public readonly record struct Framing(string Shot, Vec3 Pos, Vec3 Look, double Fov);

    public static Framing Follow(CameraShot shot, Vec3 subject) =>
        new(
            shot.Id,
            new Vec3(
                shot.Pos.X + subject.X - shot.Target.X,
                shot.Pos.Y + subject.Y - shot.Target.Y,
                shot.Pos.Z + subject.Z - shot.Target.Z),
            subject,
            shot.Fov);

    /// <summary>Dirt under the ball. Looking at the airborne ball tilts the grass out of frame.</summary>
    public static Vec3 GroundUnder(double x, double y, double z)
    {
        _ = y;
        return new Vec3(x, 0, z);
    }

    public static Framing FollowGround(CameraShot shot, Vec3 at) =>
        Follow(shot, GroundUnder(at.X, at.Y, at.Z));

    /// <summary>A bag cam (<c>throw</c>, <c>tag</c>) translated onto the bag, keeping the authored look height.</summary>
    public static Framing FollowBag(CameraShot shot, double bagX, double bagZ) =>
        Follow(shot, new Vec3(bagX, shot.Target.Y, bagZ));

    /// <summary>The smash override on a body: the authored offsets ride the subject (the batter's chest).</summary>
    public static Framing Smash(CameraShot shot, Vec3 at) =>
        new(
            shot.Id,
            new Vec3(at.X + shot.Pos.X, at.Y + shot.Pos.Y, at.Z + shot.Pos.Z),
            new Vec3(at.X + shot.Target.X, at.Y + shot.Target.Y, at.Z + shot.Target.Z),
            shot.Fov);

    /// <summary>Degrees below horizontal. 90 is straight down, 45 is the in-play look.</summary>
    public static double LookDownDeg(CameraShot shot)
    {
        var dx = shot.Target.X - shot.Pos.X;
        var dy = shot.Target.Y - shot.Pos.Y;
        var dz = shot.Target.Z - shot.Pos.Z;
        var horiz = Math.Sqrt(dx * dx + dz * dz);
        if (horiz < 1e-6) return dy < 0 ? 90 : 0;
        return Math.Atan2(-dy, horiz) * (180.0 / Math.PI);
    }
}
