namespace GrandSluggers.Sim;

/// <summary>
/// The park-neutral bowl of bleachers (<see cref="ParkKitSlots.KitBowl"/>): the stands a park draws when its stands slot
/// names the kit bowl. It is built from the park's own wall loop (<see cref="HarborWall.Loop(Park)"/>, the edge the flight
/// clips against), so it hugs every park's wall, lopsided and polyline fences included, and no park id or literal position
/// chooses where a seat is.
///
/// <para>
/// Three pieces: a horseshoe in foul territory, from just past one pole along the rail and around behind the plate to just
/// past the other, and two corner banks behind the outfield wall from each pole in to <see cref="OpenSprayDeg"/>, so centre
/// field stays open. Each piece is <see cref="Rows"/> treads of <see cref="HarborStands.RowRun"/> by
/// <see cref="HarborStands.RowRise"/>, the Harbor bowl's step. The horseshoe's first tread stands <see cref="FoulGapFt"/>
/// behind the rail (room for a dugout); a corner bank's stands <see cref="WallGapFt"/> behind the fence and starts just under
/// its top, so the wall hides the base of the bank.
/// </para>
/// </summary>
public static class KitBowl
{
    public const int Rows = 7;
    /// <summary>The horseshoe's first tread stands this far behind the foul rail and the backstop.</summary>
    public const double FoulGapFt = 14;
    /// <summary>A corner bank's first tread stands this far behind the outfield fence.</summary>
    public const double WallGapFt = 5;
    /// <summary>No bank inside this spray from centre: the batter's eye and the backdrop stay open.</summary>
    public const double OpenSprayDeg = 20;
    /// <summary>The horseshoe stops this far into foul of each line, so the pole stands clear between it and the corner bank.</summary>
    public const double PoleGapDeg = 3;
    /// <summary>A corner bank's first tread sits this far under the fence's top.</summary>
    public const double UnderCapFt = 1.5;
    /// <summary>A seat section's length along the front row, and the aisle between two sections.</summary>
    public const double SectionFt = 26;
    public const double AisleFt = 3;

    /// <summary>One piece of the bowl: its kind, and for each tread its front edge, walked in loop order.</summary>
    public sealed record Piece(string Kind, IReadOnlyList<Tread> Treads);

    /// <summary>
    /// One tread: its row (0 is the front), its height, and its front edge as points on the ground plane with the outward
    /// direction at each (the tread runs <see cref="HarborStands.RowRun"/> that way) and the section parameter at each,
    /// which is the front row's length walked to that point, so aisles line up from the front row to the back.
    /// </summary>
    public sealed record Tread(int Row, double Y, IReadOnlyList<(double X, double Z)> Front, IReadOnlyList<(double X, double Z)> Out, IReadOnlyList<double> S);

    public const string Horseshoe = "horseshoe";
    public const string CornerLeft = "corner-left";
    public const string CornerRight = "corner-right";

    public static IReadOnlyList<Piece> Of(Park park, RulesTable rules)
    {
        var loop = HarborWall.Loop(park, rules);
        var n = loop.Length;
        var spray = new double[n];
        for (var i = 0; i < n; i++) spray[i] = SprayDeg(loop[i]);
        var fair = AtBatResolver.FoulLineDeg;

        // Loop order: CF (0) → RF pole → the rail → behind the plate → the rail → LF pole → back toward CF.
        var right = new List<int>();
        var shoe = new List<int>();
        var left = new List<int>();
        for (var i = 0; i < n; i++)
        {
            var a = Math.Abs(spray[i]);
            if (a < OpenSprayDeg) continue;
            if (a <= fair + 1e-9) (spray[i] > 0 ? right : left).Add(i);
            else if (a >= fair + PoleGapDeg) shoe.Add(i);
        }

        var pieces = new List<Piece>(3);
        if (right.Count >= 2) pieces.Add(Build(park, CornerRight, right, WallGapFt, CornerBase(park, right, rules), rules));
        if (shoe.Count >= 2) pieces.Add(Build(park, Horseshoe, shoe, FoulGapFt, HarborStands.RowY(0), rules));
        if (left.Count >= 2) pieces.Add(Build(park, CornerLeft, left, WallGapFt, CornerBase(park, left, rules), rules));
        return pieces;
    }

    /// <summary>A corner bank's first tread: just under the highest fence top it stands behind.</summary>
    static double CornerBase(Park park, List<int> idx, RulesTable rules) =>
        idx.Max(i => (double)HarborWall.Height(park, i, rules)) - UnderCapFt;

    /// <summary>The section a point at parameter <paramref name="s"/> is in, or -1 in an aisle.</summary>
    public static int SectionAt(double s)
    {
        var period = SectionFt + AisleFt;
        var k = (int)Math.Floor(s / period);
        return s - k * period < SectionFt ? k : -1;
    }

    /// <summary>Spray from home in degrees: 0 is centre field, +45 the right-field line, ±180 straight behind the plate.</summary>
    public static double SprayDeg((double X, double Z) p) => Math.Atan2(p.X, p.Z) * (180.0 / Math.PI);

    static Piece Build(Park park, string kind, List<int> idx, double gap, double baseY, RulesTable rules)
    {
        var loop = HarborWall.Loop(park, rules);
        var m = idx.Count;
        var outs = new (double X, double Z)[m];
        for (var j = 0; j < m; j++)
        {
            // The vertex normal: the mean of the spans either side that belong to this piece.
            var i = idx[j];
            (double X, double Z) sum = (0, 0);
            if (j > 0) sum = Add(sum, HarborWall.Outward(park, idx[j - 1], rules));
            if (j < m - 1) sum = Add(sum, HarborWall.Outward(park, i, rules));
            var len = Math.Sqrt(sum.X * sum.X + sum.Z * sum.Z);
            outs[j] = len < 1e-9 ? (0, 1) : (sum.X / len, sum.Z / len);
        }
        var s = new double[m];
        for (var j = 1; j < m; j++)
        {
            var a = Offset(loop[idx[j - 1]], outs[j - 1], gap);
            var b = Offset(loop[idx[j]], outs[j], gap);
            s[j] = s[j - 1] + Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z));
        }
        var treads = new List<Tread>(Rows);
        for (var row = 0; row < Rows; row++)
        {
            var d = gap + row * HarborStands.RowRun;
            var front = new (double X, double Z)[m];
            for (var j = 0; j < m; j++) front[j] = Offset(loop[idx[j]], outs[j], d);
            treads.Add(new Tread(row, baseY + row * HarborStands.RowRise, front, outs, s));
        }
        return new Piece(kind, treads);
    }

    static (double X, double Z) Add((double X, double Z) a, (double X, double Z) b) => (a.X + b.X, a.Z + b.Z);

    static (double X, double Z) Offset((double X, double Z) p, (double X, double Z) dir, double d) => (p.X + dir.X * d, p.Z + dir.Z * d);
}
