namespace GrandSluggers.Sim;

/// <summary>
/// Harbor stadium bowl. Super Sluggers weight: a horseshoe of small packed
/// crowd, roofs on the LF/RF wings, center field open to the wall and town.
/// HarborKit dresses these; CF decks stay off.
/// </summary>
public static class HarborStands
{
    /// <summary>A person, not a 12-ft giant. Mass of the bowl is the crowd texture.</summary>
    public const float PersonFt = 4f;
    public const float SeatFt = 3.4f;
    public const float RowRise = 1.45f;
    public const float RowRun = 2.55f;

    public const int HomeRows = 6;
    public const float HomeZ0 = -64f;
    public const bool HasRoofs = false;
    public const float HomeHalf0 = 72f;

    public const int WingRows = 7;
    public const float WingX0 = 102f;
    public const float WingZ0 = -12f;
    public const float WingZ1 = 205f;
    /// <summary>Stands sit this far into foul of the 45° line, or outside the dugout — whichever is farther.</summary>
    public const float FoulGapFt = 32f;

    public const int CornerRows = 7;
    public const int CornerSegs = 10;
    /// <summary>From CF. Below this spray the outfield stays open.</summary>
    public const float CornerSpray0 = 20f;
    public const float CornerSpray1 = 46f;
    public const float CornerBehind0 = 5f;

    public const float RoofThick = 1.3f;
    public const float RoofLift = 9f;

    public static float RowY(int row) => 2.5f + row * RowRise;

    public static float CornerRowY(int row) =>
        HarborPostcard.WallHeightFt - 2f + row * RowRise;

    public static float BankVisibleFt => CornerRows * RowRise + PersonFt;

    public static float WingX(double z, int row)
    {
        var foul = Math.Abs(z) + FoulGapFt + row * RowRun;
        var dug = WingX0 + row * RowRun;
        return (float)Math.Max(foul, dug);
    }

    public static bool CrowdIsPeople() =>
        PersonFt >= 3.2f && PersonFt <= 5.5f && PersonFt < 8f;

    public static bool CenterFieldIsOpen() =>
        CornerSpray0 >= 18f && !HarborPostcard.CenterFieldHasBleachers;

    public static bool WingsClearTheDugout() =>
        WingX0 > HarborDugout.X + HarborDugout.HalfDeep + 8f
        && WingX(HarborDugout.Z, 0) > HarborDugout.X + HarborDugout.HalfDeep + 6f;

    public static bool WingsSitInFoul() =>
        WingX(180, 0) > 180 && WingX(40, 0) >= WingX0 - 0.1f;

    public static bool CornersSitBehindTheWall() =>
        CornerBehind0 >= 3f && CornerSpray1 >= AtBatResolver.FoulLineDeg - 1;

    public static bool BowlReadsFromField(CameraShot field, Park park)
    {
        var p = HarborPostcard.WallPoint(park, CornerSpray0 + 10);
        return HarborPostcard.SubtendDeg(field.Pos.Z, p.Z, BankVisibleFt) >= 2;
    }
}
