using System.Text.RegularExpressions;

namespace GrandSluggers.Sim;

/// <summary>
/// A park's night rig (<c>data/art/night-rigs.json</c>; WD-10 A): the greybox shapes it stands at night, filling the
/// <see cref="ParkKitSlots.Night"/> slot through <see cref="ParkKitSlots.NightRig"/>. Glow is an unlit colour, never a light:
/// the play light is the same rig for every park at night (FD-11-R2).
/// </summary>
public sealed record NightRig(string Park, IReadOnlyList<NightRigPiece> Pieces);

/// <summary>One kind of rig piece, stood at every bearing it lists (0 out to centre, + toward right), this far from home.</summary>
public sealed record NightRigPiece(string Kind, double[] BearingsDeg, double DistanceFt, double HeightFt, double SizeFt, string Color, string Glow);

/// <summary>
/// A park's rough backdrop (<c>data/art/backdrops.json</c>; WD-03 A): the blockout behind its outfield, baked by
/// <c>tools/blender/backdrop_blockout.py</c> into <see cref="Slot"/> and its Resources copy, drawn through
/// <see cref="ParkKitSlots.BlockoutBackdrop"/>. Until it is <see cref="Placed"/> the builder stands the same shapes as greybox
/// primitives, so the rows are the one source either way.
/// </summary>
public sealed record ParkBackdrop(string Park, string Slot, string Resources, bool Placed, IReadOnlyList<BackdropShape> Shapes);

/// <summary>One backdrop shape: a box, cone, cylinder or sphere at a bearing and distance, base on the ground, facing home.</summary>
public sealed record BackdropShape(string Kind, double BearingDeg, double DistanceFt, double[] SizeFt, string Color, double PitchDeg = 0)
{
    /// <summary>Where the shape stands: the ground point at its bearing and distance from home (x toward right, z out).</summary>
    public (double X, double Z) At => (DistanceFt * Math.Sin(BearingDeg * Math.PI / 180), DistanceFt * Math.Cos(BearingDeg * Math.PI / 180));
}

public sealed record NightRigFile(IReadOnlyList<NightRig>? Rigs);
public sealed record BackdropFile(IReadOnlyList<ParkBackdrop>? Backdrops);

/// <summary>The rules a night rig and a backdrop keep: known kinds, colours, sizes, a row for every park that names the builder, and ground no ball plays.</summary>
public static class ParkDress
{
    public static readonly IReadOnlyList<string> RigKinds = ["tower", "torch", "neon", "lantern"];
    public static readonly IReadOnlyList<string> ShapeKinds = ["box", "cone", "cylinder", "sphere"];

    /// <summary>How far past the fence at its bearing a backdrop or rig piece must stand: out of play and clear of the wall.</summary>
    public const double ClearOfFenceFt = 20;

    static readonly Regex Hex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> Validate(ContentCatalog content, IReadOnlyList<ParkKitSlot> kits,
        IReadOnlyList<NightRig> rigs, IReadOnlyList<ParkBackdrop> backdrops, Func<string, bool> unityFileExists)
    {
        var errors = new List<string>();
        foreach (var kit in kits)
        {
            if (kit.Fills(ParkKitSlots.Night, ParkKitSlots.NightRig) && !rigs.Any(r => r.Park == kit.Id))
                errors.Add("park kit " + kit.Id + " names night-rig but data/art/night-rigs.json has no row for it");
            if (kit.Fills(ParkKitSlots.Backdrop, ParkKitSlots.BlockoutBackdrop) && !backdrops.Any(b => b.Park == kit.Id))
                errors.Add("park kit " + kit.Id + " names blockout-backdrop but data/art/backdrops.json has no row for it");
        }
        foreach (var rig in rigs)
        {
            var where = "night rig " + rig.Park;
            if (!content.Parks.TryGetValue(rig.Park, out var park)) { errors.Add(where + " names no park"); continue; }
            if (rig.Pieces.Count == 0) errors.Add(where + " stands nothing");
            foreach (var p in rig.Pieces)
            {
                if (!RigKinds.Contains(p.Kind)) errors.Add($"{where} piece kind '{p.Kind}' must be one of [{string.Join(", ", RigKinds)}]");
                if (!Hex.IsMatch(p.Color ?? "") || !Hex.IsMatch(p.Glow ?? "")) errors.Add($"{where} {p.Kind} colors must be #RRGGBB");
                if (!(p.HeightFt > 0 && p.SizeFt > 0)) errors.Add($"{where} {p.Kind} needs a height and a size");
                if (p.BearingsDeg is not { Length: > 0 }) errors.Add($"{where} {p.Kind} stands at no bearing");
                foreach (var b in p.BearingsDeg ?? [])
                    OutOfPlay(park, b, p.DistanceFt, p.SizeFt / 2, $"{where} {p.Kind} at {b}°", errors);
            }
        }
        foreach (var bd in backdrops)
        {
            var where = "backdrop " + bd.Park;
            if (!content.Parks.TryGetValue(bd.Park, out var park)) { errors.Add(where + " names no park"); continue; }
            if (!bd.Slot.StartsWith("Assets/", StringComparison.Ordinal) || !bd.Slot.EndsWith(".fbx", StringComparison.Ordinal))
                errors.Add(where + " slot must be an Assets/ .fbx path");
            if (!bd.Resources.StartsWith("Art/", StringComparison.Ordinal))
                errors.Add(where + " resources must be a Resources path under Art/");
            if (bd.Placed && !(unityFileExists(bd.Slot) && unityFileExists("Assets/Resources/" + bd.Resources + ".fbx")))
                errors.Add(where + " is placed but " + bd.Slot + " or its Resources copy is missing");
            if (bd.Shapes.Count == 0) errors.Add(where + " stands nothing");
            foreach (var s in bd.Shapes)
            {
                if (!ShapeKinds.Contains(s.Kind)) errors.Add($"{where} shape kind '{s.Kind}' must be one of [{string.Join(", ", ShapeKinds)}]");
                if (!Hex.IsMatch(s.Color ?? "")) errors.Add($"{where} {s.Kind} color must be #RRGGBB");
                if (s.SizeFt is not { Length: 3 } || s.SizeFt.Any(v => !(v > 0))) { errors.Add($"{where} {s.Kind} sizeFt must be three positive numbers"); continue; }
                // The shape faces home, so its depth runs toward the plate: that half is what could reach back into play.
                OutOfPlay(park, s.BearingDeg, s.DistanceFt, s.SizeFt[2] / 2, $"{where} {s.Kind} at {s.BearingDeg}°", errors);
            }
        }
        return errors;
    }

    /// <summary>A shape's near edge stands <see cref="ClearOfFenceFt"/> past the fence at its bearing (foul bearings read the pole's).</summary>
    static void OutOfPlay(Park park, double bearingDeg, double distanceFt, double halfFt, string what, List<string> errors)
    {
        var fence = AtBatResolver.FenceSpotAt(park, Math.Clamp(bearingDeg, -45, 45)).DistanceFt;
        if (distanceFt - halfFt < fence + ClearOfFenceFt)
            errors.Add($"{what} stands {distanceFt - halfFt:0} ft out, inside the fence at {fence:0} ft + {ClearOfFenceFt} ft");
    }
}
