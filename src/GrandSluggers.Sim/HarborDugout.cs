namespace GrandSluggers.Sim;

/// <summary>
/// Harbor dugouts: the hip wall <b>is</b> the rail. Pit behind it toward the
/// stands, roof over the bench, stairs at the home end. HarborKit dresses these.
/// </summary>
public static class HarborDugout
{
    const float Inv = 0.70710678f;

    /// <summary>Midpoint along the 90-ft baseline (home → bag).</summary>
    public const float Along0 = 52f;

    /// <summary>Center X. 1B is +X, 3B is −X. HalfDeep behind the hip wall.</summary>
    public const float X = 64.7f;

    /// <summary>Center Z. Spans just after home to just before the bag.</summary>
    public const float Z = 8.84f;

    /// <summary>Two-thirds of the first home-to-bag span.</summary>
    public const float HalfAlong = 21.3f;
    public const float HalfDeep = 3.5f;
    public const float AlongHome = Along0 - HalfAlong;
    public const float AlongBag = Along0 + HalfAlong;

    /// <summary>Floor below field grade. Half-underground like a big-league pit.</summary>
    public const float PitDepth = 3.2f;

    /// <summary>Gold fascia = hip wall height so the rail continues the short wall.</summary>
    public const float FasciaY = 4.2f;

    public const float StarSpacing = 2.55f;

    public const int StairCount = 4;
    public const float StairDepth = 0.7f;

    /// <summary>Steps stay in the home-end opening, not a runway onto the grass.</summary>
    public const float FieldStairRun = 1.2f;

    /// <summary>
    /// 1B yaw 45°: local +Z runs home→bag, local −X is the field rail.
    /// 3B is 135°. A 180° flip puts the mesh in the stands and opens the wall gap.
    /// </summary>
    public static float YawDeg(int sign) => sign > 0 ? 45f : 135f;

    public static float StarZ0 => Z - HalfAlong + 1.7f;

    /// <summary>World X of the field-side rail (the hip wall).</summary>
    public static float FieldX(float x) => x > 0f ? RailX(1) : RailX(-1);

    public static float RailX(int sign) => sign * Inv * (Along0 + HarborWall.FoulOffset);

    public static float RailZ() => Inv * (Along0 - HarborWall.FoulOffset);

    /// <summary>Hip-wall point at this distance along the baseline.</summary>
    public static (float X, float Z) RailAt(int sign, float along)
    {
        var off = HarborWall.FoulOffset;
        return (sign * (Inv * along + Inv * off), Inv * along - Inv * off);
    }

    public static float PitFloorY => -PitDepth;

    /// <summary>Lawn must not cover this box (pit + field stairs). Pad so the lip reads.</summary>
    public const float HolePad = 1.2f;

    public static float HoleMinX => FieldX(X) - FieldStairRun - HolePad;
    public static float HoleMaxX => X + HalfDeep + HolePad;
    public static float HoleMinZ => Z - HalfAlong - HolePad;
    public static float HoleMaxZ => Z + HalfAlong + HolePad;

    public static bool InPitHole(double x, double z)
    {
        var ax = Math.Abs(x);
        var along = (ax + z) * Inv;
        var into = (ax - z) * Inv;
        return Math.Abs(along - Along0) <= HalfAlong + HolePad
            && Math.Abs(into - (HarborWall.FoulOffset + HalfDeep)) <= HalfDeep + FieldStairRun + HolePad;
    }

    /// <summary>Z span of the pit at this X, for punching a lawn hole on a 45° dugout.</summary>
    public static bool TryHoleZ(double x, out float z0, out float z1)
    {
        z0 = z1 = 0;
        const double s2 = 1.41421356237;
        var ax = Math.Abs(x);
        var ha = (HalfAlong + HolePad) * s2;
        var hd = (HalfDeep + FieldStairRun + HolePad) * s2;
        var a0 = X + Z - ax - ha;
        var a1 = X + Z - ax + ha;
        var b0 = ax - X + Z - hd;
        var b1 = ax - X + Z + hd;
        z0 = (float)Math.Max(a0, b0);
        z1 = (float)Math.Min(a1, b1);
        return z1 > z0 + 1f;
    }

    public static bool LawnCovers(double x, double z) => !InPitHole(x, z);

    /// <summary>
    /// The short wall opens here: DressWall skips the hip boxes so the
    /// padded rail is the wall along home-to-bag. The rail <b>ends</b> stay
    /// closed so the loop can pin a vertex on each end and resume flush.
    /// </summary>
    public static bool WallOpensHere(double x, double z)
    {
        var ax = Math.Abs(x);
        var along = (ax + z) * Inv;
        var into = (ax - z) * Inv;
        return along > AlongHome + 0.5f
            && along < AlongBag - 0.5f
            && Math.Abs(into - HarborWall.FoulOffset) < 5f
            && z < 95;
    }

    /// <summary>
    /// Unity Y-yaw of local −X: x′ = −cos(yaw), z′ = sin(yaw). That vector
    /// must point at the origin from the 1B rail — not into the stands.
    /// </summary>
    public static bool RailFacesTheDiamond()
    {
        var yaw = YawDeg(1) * Math.PI / 180.0;
        var lx = -Math.Cos(yaw);
        var lz = Math.Sin(yaw);
        var rail = RailAt(1, Along0);
        return lx * (-rail.X) + lz * (-rail.Z) > 0;
    }

    /// <summary>Local +Z (Unity forward) runs home→bag along the 45° line.</summary>
    public static bool YawFollowsTheFoulLine()
    {
        var yaw = YawDeg(1) * Math.PI / 180.0;
        var fx = Math.Sin(yaw);
        var fz = Math.Cos(yaw);
        return Math.Abs(fx - Inv) < 0.05 && Math.Abs(fz - Inv) < 0.05
            && Math.Abs(YawDeg(-1) - 135f) < 0.1;
    }

    /// <summary>Front rail sits on the hip wall, pit behind it into foul.</summary>
    public static bool RailIsTheHipWall()
    {
        var into = Math.Abs(X - Z) / 1.41421356f;
        var expectX = Inv * (Along0 + HarborWall.FoulOffset + HalfDeep);
        var expectZ = Inv * (Along0 - HarborWall.FoulOffset - HalfDeep);
        return Math.Abs(into - (HarborWall.FoulOffset + HalfDeep)) < 0.8f
            && Math.Abs(FasciaY - HarborWall.HipHeight) < 0.15f
            && Math.Abs(X - expectX) < 0.15f
            && Math.Abs(Z - expectZ) < 0.15f
            && RailFacesTheDiamond()
            && YawFollowsTheFoulLine();
    }

    /// <summary>
    /// Each rail end is a wall-loop vertex, and the wall resumes on one side
    /// of it. Skipping a 16-ft segment that merely <i>touches</i> the opening
    /// is what left the Play gap.
    /// </summary>
    public static bool WallMeetsTheRail(Park park)
    {
        var loop = HarborWall.Loop(park);
        foreach (var sign in new[] { 1, -1 })
        {
            foreach (var along in new[] { AlongHome, AlongBag })
            {
                var r = RailAt(sign, along);
                var pinned = false;
                for (var i = 0; i < loop.Length; i++)
                {
                    var p = loop[i];
                    if (Diamond.Dist(p.X, p.Z, r.X, r.Z) > 1.2) continue;
                    var prev = loop[(i - 1 + loop.Length) % loop.Length];
                    var next = loop[(i + 1) % loop.Length];
                    if (WallOpensHere(prev.X, prev.Z) != WallOpensHere(next.X, next.Z))
                        pinned = true;
                }
                if (!pinned) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Kit local along (the larger XZ extent) must cover the hip-wall opening.
    /// A 12-ft shed in a 42-ft hole is the same Play gap as a skipped segment.
    /// </summary>
    public static bool KitSpansTheOpening(float localAlongFt) =>
        localAlongFt >= HalfAlong * 1.6f;

    /// <summary>Field-side lip is the hip wall, past the 11-ft dirt path.</summary>
    public static bool IsSetBackFromTheDirt() =>
        HarborWall.FoulOffset > 12f;

    /// <summary>Just past the plate dirt, not on the chalk.</summary>
    public static bool StartsAfterHome() =>
        Along0 - HalfAlong > 12f;

    /// <summary>Stops short of the 90-ft bag.</summary>
    public static bool EndsBeforeTheBag() =>
        Along0 + HalfAlong < Diamond.Baseline - 4;

    public static bool IsSunken() => PitDepth >= 2f;

    public static bool HasStairs() => StairCount >= 4 && StairDepth > 0.4f;

    /// <summary>MLB pit: padded rail and mesh front, not a wooden shed.</summary>
    public static bool HasMeshFront() => FasciaY > 3.2f && FasciaY < 5.0f && PitDepth >= 2f;

    /// <summary>
    /// Scoop / plate cameras must not sit inside either dugout box (roof included).
    /// </summary>
    public static bool CameraClears(double camX, double camZ)
    {
        return !InPitHole(camX, camZ);
    }
}
