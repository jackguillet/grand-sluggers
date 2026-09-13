namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dress that reads from the field postcard and from live SET.
/// One kit, one scoreboard. Sizes are couch-feet so a 1280 Linux player
/// still shows people, ads, and digits — not a green plane and four boxes.
/// </summary>
public static class HarborPostcard
{
    public const string ParkId = "harbor-diamond";

    public const float WallThickFt = 3.4f;
    /// <summary>Full loop: outfield plus the wrap behind dugouts and home.</summary>
    public const int WallSegs = HarborWall.WrapSegs;
    public const float WallOverlapFt = 1.2f;
    public const float AdHeightFt = 12f;
    public const float AdWidthFt = 16f;
    /// <summary>Padding left above and below an ad or the CF mark so it sits inside the wall face.</summary>
    public const float FaceMarginFt = 1f;
    public const float CrowdPersonFt = HarborStands.PersonFt;
    public const float CrowdInsideFt = 18f;
    /// <summary>CF decks sit on the wall and flatten the postcard. Home and 1B/3B stands stay.</summary>
    public const bool CenterFieldHasBleachers = false;
    public const float TownPastFenceFt = 48f;
    public const float TownHeightFt = 48f;
    public const float DigitHeightFt = 12f;
    public const float ScoreboardPastFenceFt = 22f;

    /// <summary>Seven-seg bits A..G. Same masks HarborKit paints on the park board.</summary>
    public static readonly int[] DigitMask = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

    public static bool Owns(string? parkId) =>
        parkId != null && parkId.Equals(ParkId, StringComparison.OrdinalIgnoreCase);

    public static (double X, double Z) WallPoint(Park park, double sprayDeg)
    {
        var fence = AtBatResolver.FenceAt(park, sprayDeg);
        var rad = sprayDeg * Math.PI / 180.0;
        return (Math.Sin(rad) * fence, Math.Cos(rad) * fence);
    }

    /// <summary>
    /// One piece of the ground loop (outfield + foul wrap). Chord plus overlap.
    /// </summary>
    public static (double X, double Z, double Width, int I0, int I1) WallPiece(Park park, int i)
    {
        var n = HarborWall.Loop(park).Length;
        var i0 = ((i % n) + n) % n;
        var i1 = (i0 + 1) % n;
        var p0 = HarborWall.LoopPoint(park, i0);
        var p1 = HarborWall.LoopPoint(park, i1);
        var dx = p1.X - p0.X;
        var dz = p1.Z - p0.Z;
        var chord = Math.Sqrt(dx * dx + dz * dz);
        return ((p0.X + p1.X) * 0.5, (p0.Z + p1.Z) * 0.5, chord + WallOverlapFt, i0, i1);
    }

    public static bool WallPiecesConnect(Park park)
    {
        var n = HarborWall.Loop(park).Length;
        if (n != HarborWall.WrapSegs) return false;
        for (var i = 0; i < n; i++)
        {
            var a = WallPiece(park, i);
            var b = WallPiece(park, i + 1);
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;
            var gap = Math.Sqrt(dx * dx + dz * dz);
            if (gap > (a.Width + b.Width) * 0.5 - 0.2) return false;
        }
        var cf = WallPoint(park, 0);
        var dist = Math.Sqrt(cf.X * cf.X + cf.Z * cf.Z);
        return Math.Abs(dist - park.CenterFenceFt) < WallThickFt * 2
            && HarborWall.WrapsTheDiamond(park);
    }

    public static bool SegOn(int value, int bit)
    {
        value = Math.Clamp(value, 0, 9);
        return (DigitMask[value] & bit) != 0;
    }

    public static double SubtendDeg(double camZ, double objZ, double heightFt)
    {
        var dist = Math.Abs(objZ - camZ);
        if (dist < 1) return 90;
        return Math.Atan(heightFt / dist) * (180.0 / Math.PI);
    }

    /// <summary>
    /// A panel of <paramref name="wantFt"/> centered on a wall face <paramref name="wallFt"/> tall: its
    /// center height and the height it may have, shrunk so it never pokes over the cap (D15: the wall
    /// is the park's fence, whatever number that is).
    /// </summary>
    public static (float Y, float Height) OnWallFace(float wallFt, float wantFt) =>
        (wallFt * 0.5f, Math.Max(0f, Math.Min(wantFt, wallFt - 2f * FaceMarginFt)));

    /// <summary>The smallest angle a thing on the field postcard may subtend and still read: the board digits' floor.</summary>
    public const double ReadsDeg = 1.5;

    /// <summary>
    /// Field pick looks at CF. The wall (at the park's own fence height, D15), track crowd, and digits
    /// must subtend enough angle that they are not specks on a 1280 player.
    /// </summary>
    public static bool ReadsFromField(CameraShot field, Park park)
    {
        if (field.Pos.Z < 20 || field.Target.Z < 250) return false;
        var centerFenceFt = park.CenterFenceFt;
        var wallZ = centerFenceFt;
        var boardZ = centerFenceFt + ScoreboardPastFenceFt;
        if (SubtendDeg(field.Pos.Z, wallZ, HarborWall.OutfieldHeight(park)) < ReadsDeg) return false;
        if (SubtendDeg(field.Pos.Z, boardZ, DigitHeightFt) < ReadsDeg) return false;
        if (centerFenceFt + TownPastFenceFt <= wallZ) return false;
        return true;
    }
}
