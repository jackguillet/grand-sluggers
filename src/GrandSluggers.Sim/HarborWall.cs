namespace GrandSluggers.Sim;

/// <summary>
/// Harbor padded wall as a ground loop: round outfield fence, then along
/// foul territory around the dugouts and plate dirt. Not a polar cut
/// through the infield. Fair flies still use <see cref="AtBatResolver.FenceAt"/>.
/// </summary>
public static class HarborWall
{
    /// <summary>
    /// Fence samples pole to pole — the same count and the same spray grid
    /// <see cref="FieldBounds.FenceSegs"/> samples, so a drawn outfield vertex is a vertex of the
    /// polygon the flight clips against and not a point near one.
    /// </summary>
    public const int OutfieldSegs = 48;
    public const int FoulSegs = 20;
    public const int HomeSegs = 16;
    /// <summary>Home and bag rail ends, pinned on each foul wrap so the wall butts the dugout.</summary>
    public const int DugoutEnds = 2;
    /// <summary>
    /// Both sides (CF → pole → home), each walked on its own half of the park, sharing the CF point
    /// and the point behind the plate. Must stay even: the two halves are the same length, so the
    /// count does not change with the park (#845).
    /// </summary>
    public const int WrapSegs = 2 * (OutfieldSegs / 2 + 1 + FoulSegs + DugoutEnds + HomeSegs / 2) - 2;
    /// <summary>
    /// The edge this kit dresses: the one <see cref="FieldBounds"/> clips against, from
    /// <c>data/rules/boundary.json</c> (#826). The drawn rail and the ball's rail are the same
    /// number because they are read from the same place — there is no second constant.
    /// </summary>
    static ParkBoundary Bounds => ParkBoundary.Default;

    /// <summary>
    /// Hip wall offset from the foul line along the infield. The dugout rail
    /// <b>is</b> this line. Flares to the pole. Home backstop stays at <see cref="HomeZ"/>.
    /// </summary>
    public static float FoulOffset => (float)Bounds.FoulOffsetFt;
    /// <summary>Round wrap behind the plate. Radius is the offset line’s closest point, not a V to a farther apex.</summary>
    public static float HomeZ => (float)Bounds.BackstopZFt;
    public static float DugoutPad => (float)Bounds.DugoutPadFt;
    /// <summary>
    /// The padded outfield wall's top is the park's own fence (spec D15): the same number the flight
    /// clips against (<see cref="FieldBounds"/>), so a ball that meets the padding you see caroms and a
    /// homer clears it. No second constant. For a park with a polyline fence (F2-c) this is the park's
    /// nominal top, which the stands and the dress are sized against; each span of the drawn wall stands
    /// at its own top (<see cref="OutfieldHeight(Park, double)"/>).
    /// </summary>
    public static float OutfieldHeight(Park park) => (float)park.FenceHeightFt;

    /// <summary>
    /// The drawn wall's top at one bearing between the poles: the top the flight's fence has there
    /// (<see cref="AtBatResolver.FenceSpotAt"/>, D15 as amended by D21). <see cref="OutfieldHeight(Park)"/>
    /// everywhere for a park with no polyline; a polyline's point heights, straight between them, for a
    /// park that names one (F2-c).
    /// </summary>
    public static float OutfieldHeight(Park park, double sprayDeg) => (float)AtBatResolver.FenceSpotAt(park, sprayDeg).TopFt;
    /// <summary>
    /// Hip-high rail around the infield, dugouts, and home, all the way out to each pole. The top the
    /// flight clips against (<see cref="ParkBoundary.RailTopFt"/>).
    /// </summary>
    public static float HipHeight => (float)Bounds.RailHeightFt;
    public const bool HasNet = false;
    /// <summary>Authored ring sat on its side in the sky. Boxes follow the loop until the FBX lies in XZ.</summary>
    public const bool DropAuthoredRing = false;

    public static float DugoutClearX => HarborDugout.X + HarborDugout.HalfDeep + DugoutPad;

    /// <summary>
    /// The loop per (park, edge), built once. The key is <see cref="FieldBounds"/>'s key, deliberately —
    /// the same <c>FieldBounds.EdgeKey</c> type, not a copy of its fields: the drawn wall and the clip
    /// polygon are the same edge (D15 as amended by D21, FD-06), so whatever moves one moves the other and
    /// the two caches grow together. The polyline fence (F2-c) is in it by value; a park's own foul area
    /// (F2-d) lands in both at once.
    ///
    /// <para>
    /// Until #845 this was one slot keyed by the three posts under a lock. A second
    /// <see cref="ParkBoundary"/> would have been served the first one's loop and nothing would have
    /// failed, because the posts matched; and the flight asks for every park from every thread, so
    /// two parks in one process rebuilt the slot on every call.
    /// </para>
    /// </summary>
    static readonly System.Collections.Concurrent.ConcurrentDictionary<FieldBounds.EdgeKey, (double X, double Z)[]> Loops = new();

    public static (double X, double Z)[] Loop(Park park) => Loop(park, Bounds);

    /// <summary>The loop this park draws on a given edge. <see cref="FieldBounds.Of(Park, ParkBoundary)"/>'s sibling.</summary>
    public static (double X, double Z)[] Loop(Park park, ParkBoundary bounds) =>
        Loops.GetOrAdd(FieldBounds.EdgeKey.Of(park, bounds), k => BuildLoop(park, k.Bounds));

    public static (double X, double Z) LoopPoint(Park park, int i)
    {
        var loop = Loop(park);
        var n = loop.Length;
        var i0 = ((i % n) + n) % n;
        return loop[i0];
    }

    /// <summary>
    /// The whole boundary, once around: left pole to right pole through the park's own
    /// <see cref="AtBatResolver.FenceAt"/> on each side, then each side's foul wrap and the round
    /// backstop from the <see cref="ParkBoundary"/>. Every vertex is a vertex of
    /// <see cref="FieldBounds.Of(Park)"/>'s polygon, so the wall drawn in left field is the wall a
    /// ball hit to left meets (<c>SF-05</c>, D15 as amended by D21, FD-06).
    ///
    /// <para>
    /// Until #845 this built CF → RF pole → behind home and mirrored that half, so Funfair
    /// (315 / 340), Rooftop (318 / 322) and Canopy (312 / 318) drew right field's wall in left. A
    /// symmetric park's halves are the same shape in the same order, so Harbor's loop is the loop it
    /// always was — same count, same winding, same vertices — and nothing changes on screen.
    /// </para>
    /// </summary>
    static (double X, double Z)[] BuildLoop(Park park, ParkBoundary bounds)
    {
        var right = HalfLoop(park, 1, bounds);
        var left = HalfLoop(park, -1, bounds);
        var pts = new List<(double X, double Z)>(WrapSegs);
        pts.AddRange(right);
        // Both halves start on the CF post and end behind the plate. Walk the left one back from
        // home to CF without repeating either shared end.
        for (var i = left.Count - 2; i >= 1; i--)
            pts.Add(left[i]);
        return pts.ToArray();
    }

    /// <summary>
    /// One side: CF → this side's pole along the park's own fence → in along the foul rail → behind
    /// the plate. <paramref name="sign"/> is +1 for the first-base side.
    ///
    /// <para>
    /// The fence is walked on <see cref="FieldBounds.FenceBearings"/>, the bearings the clip polygon is
    /// built on: the spray grid, and for a park with a polyline fence (F2-c) every point merged in, so
    /// every fence point is a drawn vertex and each span is drawn between the flight's own vertices. The
    /// grid's bearings are exact multiples of 1.875°, so a park with no points walks exactly the
    /// bearings it always did and its loop is unchanged to the bit (<c>SF-06</c>). Centre field (bearing
    /// 0) is on the grid, so it is always the vertex both halves share.
    /// </para>
    /// </summary>
    static List<(double X, double Z)> HalfLoop(Park park, int sign, ParkBoundary bounds)
    {
        var bearings = FieldBounds.FenceBearings(park);
        var half = new List<(double X, double Z)>(WrapSegs / 2 + 2 + bearings.Count - (OutfieldSegs + 1));
        var cf = 0;
        while (bearings[cf] < 0) cf++;
        for (var i = cf; i >= 0 && i < bearings.Count; i += sign)
            half.Add(FencePoint(park, bearings[i]));
        var poleR = AtBatResolver.FenceAt(park, sign * AtBatResolver.FoulLineDeg);
        var alongs = new List<double>(FoulSegs + DugoutEnds);
        for (var i = 1; i <= FoulSegs; i++)
            alongs.Add(poleR * (1 - i / (double)FoulSegs));
        alongs.Add(HarborDugout.AlongBag);
        alongs.Add(HarborDugout.AlongHome);
        alongs.Sort((a, b) => b.CompareTo(a));
        foreach (var s in alongs)
            half.Add(bounds.RailPoint(sign, s, poleR));
        var home = half[^1];
        var r = Math.Sqrt(home.X * home.X + home.Z * home.Z);
        var a0 = Math.Atan2(home.X, home.Z);
        var a1 = sign * Math.PI;
        for (var i = 1; i <= HomeSegs / 2; i++)
        {
            var t = i / (double)(HomeSegs / 2);
            var a = a0 + (a1 - a0) * t;
            half.Add((Math.Sin(a) * r, Math.Cos(a) * r));
        }
        return half;
    }

    /// <summary>
    /// Along the foul line toward home, offset into foul so the wall stays
    /// off the dirt and outside the dugout. Offset is 0 at the pole.
    ///
    /// <para>
    /// The math is <see cref="ParkBoundary.RailPoint"/>: the drawn rail is the rail the ball meets,
    /// not a copy of it (#826).
    /// </para>
    /// </summary>
    public static (double X, double Z) FoulWall(int sign, double alongFt, double poleFt) =>
        Bounds.RailPoint(sign, alongFt, poleFt);

    static (double X, double Z) FencePoint(Park park, double sprayDeg)
    {
        var r = AtBatResolver.FenceAt(park, sprayDeg);
        var rad = sprayDeg * Math.PI / 180.0;
        return (Math.Sin(rad) * r, Math.Cos(rad) * r);
    }

    static (double X, double Z) Lerp2((double X, double Z) a, (double X, double Z) b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        t = t * t * (3 - 2 * t);
        return (a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t);
    }

    /// <summary>
    /// The park's two poles are the same distance, so its wall has a mirror line down the middle.
    /// Harbor, Crystal and Ember are; Funfair, Rooftop and Canopy are not (FD-06).
    /// </summary>
    public static bool ParkIsSymmetric(Park park) => park.LeftFenceFt == park.RightFenceFt;

    /// <summary>
    /// Every loop vertex has a partner across the Z axis.
    ///
    /// <para>
    /// Re-authored by #845 (FD-06): the loop is no longer a mirror of the right-field half, so this
    /// is now a statement about the <i>park</i> and not about the builder. A park whose poles match
    /// draws the same wall on both sides; a lopsided park draws its own left field, and this is
    /// false for it — which is the fix, not a regression. <see cref="WrapsTheDiamond"/> asks for it
    /// only of a symmetric park (<see cref="ParkIsSymmetric"/>).
    /// </para>
    /// </summary>
    public static bool LoopIsSymmetric(Park park)
    {
        var loop = Loop(park);
        if (loop.Length != WrapSegs) return false;
        foreach (var p in loop)
        {
            var ok = false;
            foreach (var q in loop)
            {
                if (Math.Abs(q.X + p.X) < 1.2 && Math.Abs(q.Z - p.Z) < 1.2)
                {
                    ok = true;
                    break;
                }
            }
            if (!ok) return false;
        }
        return true;
    }

    public static (double X, double Z) TrackInner(Park park, int i)
    {
        var p = LoopPoint(park, i);
        var o = Outward(park, i);
        var w = ParkDiamond.TrackWidth;
        return (p.X - o.X * w, p.Z - o.Z * w);
    }

    public static (double X, double Z) TrackOuter(Park park, int i)
    {
        var p = LoopPoint(park, i);
        var o = Outward(park, i);
        var w = ParkDiamond.TrackWallInset;
        return (p.X - o.X * w, p.Z - o.Z * w);
    }

    /// <summary>
    /// One drawn span of the loop, from vertex <c>i</c> to <c>i + 1</c>: the flight segment it lies on,
    /// and that segment's top at each of the span's two ends.
    /// </summary>
    readonly record struct DrawnSpan(FieldBounds.WallSegment Segment, float Start, float End);

    /// <summary>
    /// The drawn spans per (park, edge), built once, on <see cref="Loops"/>' key — the one
    /// <c>FieldBounds.EdgeKey</c> the clip polygon's cache uses, not a second key — so whatever grows
    /// that key (the polyline fence, F2-c; a park's own foul area, F2-d) reaches the spans with the loop
    /// and the polygon they are read from.
    /// </summary>
    static readonly System.Collections.Concurrent.ConcurrentDictionary<FieldBounds.EdgeKey, DrawnSpan[]> Spans = new();

    static DrawnSpan Span(Park park, int i)
    {
        var spans = Spans.GetOrAdd(FieldBounds.EdgeKey.Of(park, Bounds),
            k => BuildSpans(Loop(park, k.Bounds), FieldBounds.Of(park, k.Bounds)));
        var n = spans.Length;
        return spans[((i % n) + n) % n];
    }

    /// <summary>
    /// Each drawn span's flight segment — the one its midpoint lies on — and the segment's top where
    /// each end of the span stands on it (<see cref="FieldBounds.WallSegment.HeightAt"/>).
    /// </summary>
    static DrawnSpan[] BuildSpans((double X, double Z)[] loop, FieldBounds.Boundary bounds)
    {
        var n = loop.Length;
        var spans = new DrawnSpan[n];
        for (var i = 0; i < n; i++)
        {
            var a = loop[i];
            var b = loop[(i + 1) % n];
            var mid = ((a.X + b.X) * 0.5, (a.Z + b.Z) * 0.5);
            FieldBounds.WallSegment? under = null;
            var best = double.MaxValue;
            foreach (var s in bounds.Segments)
            {
                var (d, _) = OnSegment(s, mid);
                if (d >= best) continue;
                best = d;
                under = s;
            }
            spans[i] = new DrawnSpan(under!, TopOn(under!, a), TopOn(under!, b));
        }
        return spans;
    }

    /// <summary>How far a point is from a segment, and how far along it (0 at A, 1 at B) its nearest point is.</summary>
    static (double DistSq, double Along) OnSegment(FieldBounds.WallSegment s, (double X, double Z) p)
    {
        var ex = s.Bx - s.Ax;
        var ez = s.Bz - s.Az;
        var len2 = ex * ex + ez * ez;
        var t = len2 < 1e-12 ? 0 : Math.Clamp(((p.X - s.Ax) * ex + (p.Z - s.Az) * ez) / len2, 0, 1);
        var dx = p.X - (s.Ax + ex * t);
        var dz = p.Z - (s.Az + ez * t);
        return (dx * dx + dz * dz, t);
    }

    /// <summary>
    /// The segment's top at a drawn vertex on it. A vertex at one of the segment's ends reads that end's
    /// top exactly, not a projection a rounding error short of it; a level segment reads its one top.
    /// </summary>
    static float TopOn(FieldBounds.WallSegment s, (double X, double Z) p)
    {
        var t = OnSegment(s, p).Along;
        if (t < 1e-9) t = 0;
        else if (t > 1 - 1e-9) t = 1;
        return (float)s.HeightAt(t);
    }

    /// <summary>
    /// The segment of the clip polygon (<see cref="FieldBounds.Of(Park)"/>) that the drawn span from
    /// loop vertex <paramref name="i"/> to <paramref name="i"/> + 1 lies on. Every drawn vertex is a
    /// polygon vertex or a point on one of its straight rail segments (<c>SF-05</c>), so each drawn
    /// span is a piece of exactly one flight segment: its top, its kind (fence or rail) and its
    /// material are read from here, not recomputed (FD-06-R2).
    /// </summary>
    public static FieldBounds.WallSegment FlightSpan(Park park, int i) => Span(park, i).Segment;

    /// <summary>
    /// The top of the drawn span from loop vertex <paramref name="i"/> to <paramref name="i"/> + 1 at
    /// each of its ends: the top of the flight's wall under it (<see cref="FlightSpan"/>) where that end
    /// stands. Between the poles that is the fence — the park's <c>fenceHeightFt</c> end to end, or a
    /// polyline span's two point heights, straight between them (F2-c) — and on the foul wrap and the
    /// backstop it is the rail's top, level all the way to each pole (FD-06-R2). This is what
    /// <c>FieldKit.Wall</c> draws each span from and to, so the span that leaves a pole is rail from its
    /// first foot and the wall steps from the fence to the rail at the pole itself, where the ball's
    /// wall steps. Every span of a park with no polyline is level (<c>Start == End</c>).
    /// </summary>
    public static (float Start, float End) SpanTops(Park park, int i)
    {
        var span = Span(park, i);
        return (span.Start, span.End);
    }

    /// <summary>
    /// The top of the wall at loop vertex <paramref name="i"/>: the taller of the two drawn spans that
    /// meet there (<see cref="SpanTops"/>, each read at this vertex), so the flight's top and not a second
    /// rule. The fence's top between the poles and at each pole, where the fence ends; the rail's top at
    /// every other vertex of the foul wrap and the backstop.
    ///
    /// <para>
    /// Until F2-b2 (#873) this was its own rule: the fence within half a degree of the foul line,
    /// then a smoothstep from the rail's top up to the fence past a literal 95 ft out
    /// (<c>RampStartZ</c>), while the flight's rail stayed hip-high to the pole — 150–177 ft of drawn
    /// rail per side on the 90-ft field and 64–82 ft on the 80-ft one stood over a ball that
    /// went through it (map finding 19). Jack chose the flight's rail (FD-06-R2): the drawn rail stays
    /// hip-high to the pole and the wall steps up at the pole. No flight number moved.
    /// </para>
    /// </summary>
    public static float Height(Park park, int i) => Math.Max(SpanTops(park, i - 1).End, SpanTops(park, i).Start);

    /// <summary>
    /// A loop vertex on the fence between the poles, the poles included: a vertex of a fair span of
    /// the flight's wall (<see cref="FlightSpan"/>), where the drawn wall is the park's fence. Before
    /// F2-b2 this was "within half a degree of the foul line", which also took in the first one or
    /// two rail vertices past each pole, where the rail flares into the line.
    /// </summary>
    public static bool IsOutfield(Park park, int i) =>
        FlightSpan(park, i - 1).Kind == FieldBounds.WallKind.FairFence
        || FlightSpan(park, i).Kind == FieldBounds.WallKind.FairFence;

    /// <summary>A loop vertex at a foul pole: the point on that side's fence where the foul line meets it (<see cref="ParkDiamond.FoulPole"/>).</summary>
    public static bool IsPole(Park park, int i)
    {
        var p = LoopPoint(park, i);
        for (var sign = -1; sign <= 1; sign += 2)
        {
            var pole = ParkDiamond.FoulPole(park, sign);
            if (Diamond.Dist(p.X, p.Z, pole.X, pole.Z) < 1e-6) return true;
        }
        return false;
    }

    /// <summary>
    /// D15: every outfield vertex of the drawn wall stands at the flight fence's top at that vertex —
    /// the park's <c>fenceHeightFt</c>, or for a polyline fence (F2-c) the points' heights, straight
    /// between them — the rail stays hip-high, and the fence is taller than the rail so the wall steps
    /// up to it at each pole (FD-06-R2).
    /// </summary>
    public static bool OutfieldIsTheFence(Park park)
    {
        if (!(HipHeight >= 3.2f && HipHeight <= 5.5f && park.FenceHeightFt > HipHeight)) return false;
        var loop = Loop(park);
        for (var i = 0; i < loop.Length; i++)
        {
            if (!IsOutfield(park, i)) continue;
            var top = AtBatResolver.FenceSpotAt(park, FieldBounds.SprayDeg(loop[i].X, loop[i].Z)).TopFt;
            if (!(top > HipHeight) || Math.Abs(Height(park, i) - top) > 1e-4) return false;
        }
        return true;
    }

    /// <summary>
    /// FD-06-R2: the only change of height on the foul side is the step at each pole. Every span of
    /// the foul wrap and the backstop stands at the rail's top (<see cref="HipHeight"/>), level to the
    /// pole; a fair span meets a foul one exactly twice around the loop, each time at a foul pole
    /// (<see cref="IsPole"/>), and the top steps there. Heights between two fair spans are the fence's
    /// own business (a polyline fence, F2-c) and are not asked about here.
    ///
    /// <para>
    /// Replaces <c>TaperIsARamp</c>, which asked the opposite: that the rail climb to the fence over
    /// at least six vertices past 95 ft out, while the ball's rail stayed hip-high to the pole.
    /// </para>
    /// </summary>
    public static bool StepsOnlyAtThePoles(Park park)
    {
        var n = Loop(park).Length;
        var steps = 0;
        for (var i = 0; i < n; i++)
        {
            var before = FlightSpan(park, i - 1).Kind;
            var here = FlightSpan(park, i).Kind;
            var tops = SpanTops(park, i);
            if (here == FieldBounds.WallKind.FoulWall && (tops.Start != HipHeight || tops.End != HipHeight)) return false;
            if (before == here) continue;
            if (!IsPole(park, i) || SpanTops(park, i - 1).End == tops.Start) return false;
            steps++;
        }
        return steps == 2;
    }

    public static (double X, double Z) Outward(Park park, int i)
    {
        var a = LoopPoint(park, i);
        var b = LoopPoint(park, i + 1);
        var tx = b.X - a.X;
        var tz = b.Z - a.Z;
        var len = Math.Sqrt(tx * tx + tz * tz);
        if (len < 1e-6) return (0, 1);
        tx /= len;
        tz /= len;
        // Cross(up, tangent): (-tz, tx) in XZ.
        var ox = -tz;
        var oz = tx;
        var mx = (a.X + b.X) * 0.5;
        var mz = (a.Z + b.Z) * 0.5;
        if (ox * mx + oz * mz < 0)
        {
            ox = -ox;
            oz = -oz;
        }
        return (ox, oz);
    }

    /// <summary>Behind home is a circular arc, not two lines to a point.</summary>
    public static bool HomeWrapIsRound(Park park)
    {
        var loop = Loop(park);
        var home = loop.OrderBy(p => p.Z).First();
        if (Math.Abs(home.X) > 4) return false;
        (double X, double Z)? left = null, right = null;
        foreach (var p in loop)
        {
            if (p.Z >= -8) continue;
            if (p.X < -8 && (left == null || p.Z < left.Value.Z)) left = p;
            if (p.X > 8 && (right == null || p.Z < right.Value.Z)) right = p;
        }
        if (left == null || right == null) return false;
        double R((double X, double Z) p) => Math.Sqrt(p.X * p.X + p.Z * p.Z);
        return Math.Abs(R(home) - R(left.Value)) < 6
            && Math.Abs(R(home) - R(right.Value)) < 6
            && home.Z > -FoulOffset - 8;
    }

    public static bool WrapStaysInFoul(Park park)
    {
        foreach (var p in Loop(park))
        {
            if (p.Z < 8) continue;
            var spray = Math.Atan2(p.X, p.Z) * (180.0 / Math.PI);
            if (Math.Abs(spray) <= AtBatResolver.FoulLineDeg + 1) continue;
            if (Math.Abs(p.X) < p.Z - 2) return false;
            if (ParkDiamond.OnDirt(p.X, p.Z)) return false;
        }
        return true;
    }

    public static bool WrapsTheDiamond(Park park)
    {
        var loop = Loop(park);
        if (loop.Length < 40) return false;
        var home = loop.OrderBy(p => p.Z).First();
        if (home.Z > HomeSet.CatcherZ - 8) return false;
        if (home.Z < HarborStands.HomeZ0 - 8) return false;
        var dug = (HarborDugout.X, HarborDugout.Z);
        var minDug = loop.Min(p => Diamond.Dist(p.X, p.Z, dug.X, dug.Z));
        // Center sits HalfDeep behind the rail. The hip wall IS that rail.
        if (Math.Abs(minDug - HarborDugout.HalfDeep) > 4) return false;
        var cf = FencePoint(park, 0);
        if (loop.Min(p => Diamond.Dist(p.X, p.Z, cf.X, cf.Z)) > 4) return false;
        return WrapStaysInFoul(park)
            // A symmetric park draws one wall on both sides. A lopsided park draws two, which is
            // the point of #845 — asking every park for a mirror is what hid the bug.
            && (!ParkIsSymmetric(park) || LoopIsSymmetric(park))
            && HomeWrapIsRound(park)
            && !HasNet && !DropAuthoredRing
            && OutfieldIsTheFence(park);
    }
}
