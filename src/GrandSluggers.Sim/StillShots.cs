namespace GrandSluggers.Sim;

/// <summary>
/// One named park shot (<c>data/feel/shots.json</c> <c>parkShots</c>; F7-b, FD-17): a still whose camera is computed
/// from the park being captured, not authored as a <see cref="Vec3"/>. A fixed shot in <c>shots</c> is one pose for
/// every park; a park shot names what it frames and how, and <see cref="StillShots.Frame"/> places it on that park's
/// geometry, so the same row frames Harbor, a lopsided park and the compact copy (FR-04, FR-05).
/// </summary>
/// <param name="Id">The still's name, as <see cref="StillRequest"/> accepts it and the PNG is called.</param>
/// <param name="Frame">What the shot frames. <see cref="StillShots.PoleFrame"/> is the one kind today.</param>
/// <param name="Side">−1 the left-field (third-base) side, +1 the right-field (first-base) side.</param>
/// <param name="BackFt">How far the camera stands back from the pole along the foul line, toward home.</param>
/// <param name="FairFt">How far the camera stands off the foul line into fair, square to the line.</param>
/// <param name="EyeFt">The camera's height over the grass: about a head, so the rail and the fence read at their size.</param>
/// <param name="Fov">The vertical field of view in degrees, the number the rig hands the camera.</param>
/// <param name="HoldFt">
/// How much of the fence, and how much of the foul rail, the frame must hold on either side of the pole. The pose does
/// not read it; it is the framing contract a change to the other numbers is tested against at every park.
/// </param>
public sealed record ParkShot(
    string Id,
    string Frame,
    int Side,
    double BackFt,
    double FairFt,
    double EyeFt,
    double Fov,
    double HoldFt);

/// <summary>
/// The named park shots' poses (F7-b1, #882; FD-17 C, FD-16, FR-14, FR-05, FR-04). The camera is placed from the one
/// geometry owner for the park being captured — the pole from <see cref="ParkDiamond.FoulPole"/>, the fence's top from
/// <see cref="AtBatResolver.FenceSpotAt"/>, the rail's top from <see cref="ParkBoundary"/> — and the framing numbers come
/// from the row. No pose is a literal and none names a park.
/// </summary>
public static class StillShots
{
    /// <summary>The <see cref="ParkShot.Frame"/> of a shot that looks at a foul pole.</summary>
    public const string PoleFrame = "pole";

    /// <summary>The look class a park shot carries on its <see cref="CameraShot"/>, as the <c>field</c> shot does.</summary>
    public const string ParkLook = "park";

    /// <summary>
    /// The row's pose on <paramref name="park"/>, cut (blend 0). <paramref name="bounds"/> is the edge the kit draws the
    /// rail on — the client passes the match table's <see cref="ParkBoundary.From"/>, the edge <see cref="HarborWall"/> dresses.
    /// </summary>
    public static CameraShot Frame(ParkShot row, Park park, ParkBoundary bounds)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        if (park is null) throw new ArgumentNullException(nameof(park));
        if (row.Frame == PoleFrame) return Pole(row, park, bounds);
        throw new InvalidDataException($"park shot '{row.Id}' frames '{row.Frame}', which no pose knows");
    }

    /// <summary>
    /// A foul pole with the last span of the fence on its fair side and the last stretch of the rail on its foul side.
    /// The camera stands <see cref="ParkShot.BackFt"/> back from the pole along the foul line and
    /// <see cref="ParkShot.FairFt"/> into fair, <see cref="ParkShot.EyeFt"/> over the grass. It looks at the pole, at the
    /// middle of the step from the rail's top to the fence's top there, so the step is the centre of the frame whatever
    /// the park's fence stands at.
    /// </summary>
    public static CameraShot Pole(ParkShot row, Park park, ParkBoundary bounds)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        if (park is null) throw new ArgumentNullException(nameof(park));
        var sign = Math.Sign(row.Side);
        if (sign == 0)
            throw new InvalidDataException($"park shot '{row.Id}' names no side of the field");
        var pole = ParkDiamond.FoulPole(park, sign);
        var poleFt = Math.Sqrt(pole.X * pole.X + pole.Z * pole.Z);
        if (!(row.BackFt < poleFt))
            throw new InvalidDataException(
                $"park shot '{row.Id}' stands {row.BackFt} ft back from a pole {poleFt:0.#} ft from home at '{park.Id}'; it would stand behind the plate");
        // Home to the pole is the foul line; the pole is on it (ParkDiamond.PoleIsOnTheFoulLine).
        var alongX = pole.X / poleFt;
        var alongZ = pole.Z / poleFt;
        var fair = ParkDiamond.FairInward(sign);
        var fenceTop = AtBatResolver.FenceSpotAt(park, sign * AtBatResolver.FoulLineDeg).TopFt;
        var lookY = (bounds.RailTopFt + fenceTop) * 0.5;
        var pos = new Vec3(
            pole.X - alongX * row.BackFt + fair.X * row.FairFt,
            row.EyeFt,
            pole.Z - alongZ * row.BackFt + fair.Z * row.FairFt);
        return new CameraShot(row.Id, ParkLook, pos, new Vec3(pole.X, lookY, pole.Z), row.Fov, 0);
    }

    /// <summary>
    /// Where <paramref name="point"/> lands in <paramref name="shot"/>'s frame, as the rig's camera sees it (look-at with
    /// the world up, <see cref="CameraShot.Fov"/> vertical): X and Y from −1 to 1 across the frame's width and height at
    /// <paramref name="aspect"/>, and the depth in front of the lens. A point is in the frame when both are within ±1 and
    /// the depth is positive.
    /// </summary>
    public static (double X, double Y, double Depth) Project(CameraShot shot, Vec3 point, double aspect)
    {
        var fx = shot.Target.X - shot.Pos.X;
        var fy = shot.Target.Y - shot.Pos.Y;
        var fz = shot.Target.Z - shot.Pos.Z;
        var fl = Math.Sqrt(fx * fx + fy * fy + fz * fz);
        fx /= fl;
        fy /= fl;
        fz /= fl;
        // right = up × forward (Unity's left-handed LookAt), up' = forward × right.
        var rx = fz;
        var rz = -fx;
        var rl = Math.Sqrt(rx * rx + rz * rz);
        rx /= rl;
        rz /= rl;
        var ux = fy * rz;
        var uy = fz * rx - fx * rz;
        var uz = -fy * rx;
        var dx = point.X - shot.Pos.X;
        var dy = point.Y - shot.Pos.Y;
        var dz = point.Z - shot.Pos.Z;
        var depth = dx * fx + dy * fy + dz * fz;
        var tanV = Math.Tan(shot.Fov * Math.PI / 360.0);
        var x = (dx * rx + dz * rz) / depth / (tanV * aspect);
        var y = (dx * ux + dy * uy + dz * uz) / depth / tanV;
        return (x, y, depth);
    }
}

/// <summary>
/// A <c>parkShots</c> row as authored. The numbers are nullable so a missing one is refused by name instead of reading
/// as 0 (the house rule for park data, <c>FencePointDto</c>).
/// </summary>
internal sealed class ParkShotDto
{
    public string? Id { get; set; }
    public string? Frame { get; set; }
    public string? Side { get; set; }
    public double? BackFt { get; set; }
    public double? FairFt { get; set; }
    public double? EyeFt { get; set; }
    public double? Fov { get; set; }
    public double? HoldFt { get; set; }

    /// <summary>The narrowest and widest vertical field of view a still may ask for (the rig's own floor is 10).</summary>
    internal const double MinFov = 10;
    internal const double MaxFov = 90;

    /// <summary>A camera over this is not "about head height" any more; it is a crane, and a different shot.</summary>
    internal const double MaxEyeFt = 20;

    /// <summary>The validator: every refusal names the file, the row and the reason.</summary>
    public ParkShot ToShot(string path, int index)
    {
        var where = $"{path}: parkShots[{index}]";
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidDataException($"{where} needs an id");
        where += $" '{Id}'";
        if (Frame != StillShots.PoleFrame)
            throw new InvalidDataException($"{where} frame must be '{StillShots.PoleFrame}'; got '{Frame}'");
        var side = Side switch
        {
            "left" => -1,
            "right" => 1,
            _ => throw new InvalidDataException($"{where} side must be 'left' or 'right'; got '{Side}'")
        };
        var back = Positive(where, "backFt", BackFt);
        var fair = Positive(where, "fairFt", FairFt);
        var eye = Positive(where, "eyeFt", EyeFt);
        if (eye > MaxEyeFt)
            throw new InvalidDataException($"{where} eyeFt must be at most {MaxEyeFt} (about head height); got {eye}");
        var fov = Positive(where, "fov", Fov);
        if (fov < MinFov || fov > MaxFov)
            throw new InvalidDataException($"{where} fov must be in [{MinFov}, {MaxFov}] degrees; got {fov}");
        var hold = Positive(where, "holdFt", HoldFt);
        return new ParkShot(Id!.Trim(), Frame!, side, back, fair, eye, fov, hold);
    }

    static double Positive(string where, string key, double? value)
    {
        if (value is not { } v)
            throw new InvalidDataException($"{where} needs {key}");
        if (!double.IsFinite(v) || v <= 0)
            throw new InvalidDataException($"{where} {key} must be a finite number over 0; got {v}");
        return v;
    }
}
