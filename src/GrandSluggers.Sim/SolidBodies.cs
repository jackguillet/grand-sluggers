namespace GrandSluggers.Sim;

/// <summary>
/// One <see cref="HazardPattern.SolidBody"/> or <see cref="HazardPattern.TimedMover"/> instance of the park a play is on (F4-f):
/// the instance's index in <see cref="Park.Hazards"/>, its type, where it stands (a mover's authored spot), its radius and the
/// type's row. A mover runs along the fence — at right angles to its bearing from home — <c>travelFt</c> to either side of its
/// spot, once out and back per <c>periodSec</c> of the play clock (<see cref="At"/>).
/// </summary>
public sealed record SolidBody(int Hazard, string Type, double X, double Z, double RadiusFt, HazardTypeRules Row)
{
    public bool Moves => Row.Pattern == HazardPattern.TimedMover;

    /// <summary>Where the body stands at play second <paramref name="t"/>: its spot, or a mover's place on its run.</summary>
    public (double X, double Z) At(double t)
    {
        if (!Moves || Row.PeriodSec is not { } period || Row.TravelFt is not { } travel) return (X, Z);
        var d = Math.Sqrt(X * X + Z * Z);
        var (tx, tz) = d > 1e-9 ? (Z / d, -X / d) : (1.0, 0.0);
        var along = travel * Math.Sin(2 * Math.PI * t / period);
        return (X + tx * along, Z + tz * along);
    }

    /// <summary>The body at play second <paramref name="t"/> as a disc the route keeps off (a volume nobody can pass through).</summary>
    public StatusVolume AsVolume(double t)
    {
        var (x, z) = At(t);
        return new StatusVolume(Hazard, Type, x, z, RadiusFt, double.PositiveInfinity);
    }
}

/// <summary>The ball met a solid body this play (F4-f): the typed fact presentation reads (F8-b).</summary>
public sealed record BodyCarom(int Hazard, string Type, double T, double X, double Z);

/// <summary>
/// The park's solid bodies and timed movers, live (§14; F4-f, FD-09, SF-28). A ball inside a body's disc below its
/// <c>heightFt</c>, moving into it, caroms off it: the part of its horizontal speed into the body turns around at the row's
/// <c>restitution</c>, the part along it is kept, and the path continues from the rim. A body standing where a fielder would
/// step is pushed to the rim; every route to a goal goes around a body (<see cref="VolumeRoute"/>, with a slow nothing can pay).
/// A mover is at its place on the play clock, a function of the clock alone, so the same second is the same place.
/// </summary>
public static class SolidBodies
{
    /// <summary>The played park's solid bodies and movers, in park order.</summary>
    public static IReadOnlyList<SolidBody> Of(Park park, RulesTable rules)
    {
        List<SolidBody>? list = null;
        for (var i = 0; i < park.Hazards.Count; i++)
        {
            var h = park.Hazards[i];
            var row = rules.Hazards.Of(h.Type);
            if (row.Pattern is HazardPattern.SolidBody or HazardPattern.TimedMover)
                (list ??= []).Add(new SolidBody(i, h.Type, h.X, h.Z, h.Radius, row));
        }
        return list is null ? [] : list;
    }

    /// <summary>A point inside a body's disc, pushed out to its rim along the line from its centre; any other point as it is.</summary>
    public static (double X, double Z) PushOut(IReadOnlyList<SolidBody> bodies, double t, double x, double z)
    {
        foreach (var b in bodies)
        {
            var (cx, cz) = b.At(t);
            var dx = x - cx;
            var dz = z - cz;
            var d = Math.Sqrt(dx * dx + dz * dz);
            if (d >= b.RadiusFt) continue;
            var (nx, nz) = d > 1e-9 ? (dx / d, dz / d) : (0.0, -1.0);
            (x, z) = (cx + nx * b.RadiusFt, cz + nz * b.RadiusFt);
        }
        return (x, z);
    }

    /// <summary>
    /// The carom off a body the ball at (x, y, z) moving at (vx, vz) is in, or null: the body, the rim point and the new
    /// horizontal velocity (the part into the body reversed at the row's restitution, the part along it kept).
    /// </summary>
    public static (SolidBody Body, double X, double Z, double Vx, double Vz)? Carom(
        IReadOnlyList<SolidBody> bodies, double t, double x, double y, double z, double vx, double vz)
    {
        foreach (var b in bodies)
        {
            if (y > (b.Row.HeightFt ?? 0)) continue;
            var (cx, cz) = b.At(t);
            var dx = x - cx;
            var dz = z - cz;
            var d = Math.Sqrt(dx * dx + dz * dz);
            if (d > b.RadiusFt) continue;
            var (nx, nz) = d > 1e-9 ? (dx / d, dz / d) : (0.0, -1.0);
            var into = vx * nx + vz * nz;
            if (into >= 0) continue; // already leaving
            var e = b.Row.Restitution ?? 0.5;
            var (ox, oz) = (vx - (1 + e) * into * nx, vz - (1 + e) * into * nz);
            return (b, cx + nx * b.RadiusFt, cz + nz * b.RadiusFt, ox, oz);
        }
        return null;
    }
}
