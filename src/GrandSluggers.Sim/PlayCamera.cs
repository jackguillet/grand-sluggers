namespace GrandSluggers.Sim;

/// <summary>
/// One named shot per play. SET and the throw: 1P follows the role
/// (mound when pitching, plate when batting). 1v1 stays behind home.
/// Flight stays on that SET shot (no <c>pitch</c> cut). In-play is one
/// top-down follow on the dirt under the ball (CF at the top) for 1P and 1v1.
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
    }

    public const string Wall = "wall";

    /// <summary>
    /// Live batted ball. Top-down offset: high Y, small −Z so CF is the top of the frame.
    /// <see cref="FollowGround"/> puts the look on the dirt under the ball.
    /// </summary>
    public const string InPlay = "diamond";

    public readonly record struct Viewport(double X, double Y, double Depth);

    /// <summary>
    /// SET / throw. Two pads: always <see cref="AtBatShots.Plate"/>.
    /// One pad: <paramref name="pitchingSet"/> is the role — mound on the
    /// rubber, plate in the box.
    /// </summary>
    public static string Shot(Beat beat, int seats = 1, bool pitchingSet = false)
    {
        var set = seats >= 2 || !pitchingSet ? AtBatShots.Plate : AtBatShots.Mound;
        return beat switch
        {
            Beat.Set or Beat.PitchFlight => set,
            _ => InPlay
        };
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

    public static Beat BeatFrom(AtBatResult hit)
    {
        if (!string.IsNullOrEmpty(hit.StarSwingUsed)) return Beat.Smash;
        if (hit.HomeRun) return Beat.Homer;
        if (FieldingResolver.IsGrounder(hit))
            return hit.SprayDeg < -8 ? Beat.GrounderPull : Beat.Grounder;
        if (FieldingResolver.IsLine(hit)) return Beat.Line;
        return Beat.Fly;
    }

    public static string FromHit(AtBatResult hit) => Shot(BeatFrom(hit));

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
}
