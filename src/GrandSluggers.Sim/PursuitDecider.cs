namespace GrandSluggers.Sim;

/// <summary>
/// The live play as the pursuit's decisions read it (§8.9, D16, D17): the ball and its path, the glove, every body and when it
/// may move, and the reaches that decide a hand-off. Read-only; the decisions change nothing.
/// </summary>
public interface IPursuitView
{
    RulesTable Rules { get; }
    Park Park { get; }
    double Elapsed { get; }
    FieldingPreview? Preview { get; }
    IReadOnlyList<Sample>? Path { get; }
    AtBatResult? Hit { get; }
    double Hang { get; }
    string GlovePos { get; }
    double GloveX { get; }
    double GloveZ { get; }
    double BallX { get; }
    double BallY { get; }
    double BallZ { get; }
    bool HoldsBall { get; }
    bool Throwing { get; }
    bool LooseBall { get; }
    string BuddyPos { get; }

    /// <summary>Every body's spot now.</summary>
    IReadOnlyDictionary<string, (double X, double Z)> Bodies { get; }

    /// <summary>When each CPU-driven body may move (§8.2).</summary>
    IReadOnlyDictionary<string, double> ReadyTable { get; }

    /// <summary>Who stands at each position on this play.</summary>
    IReadOnlyDictionary<string, Character> Assigned { get; }

    /// <summary>Play seconds before the body at <paramref name="pos"/> may move (§8.2).</summary>
    double ReadyAt(string pos);

    /// <summary>The body at <paramref name="pos"/> may move now.</summary>
    bool CanMove(string pos);

    /// <summary>The body at <paramref name="pos"/> is coasting after a hand-off.</summary>
    bool Coasting(string pos);

    /// <summary>The body at <paramref name="pos"/> is kept off the ball by an item (§12).</summary>
    bool KeptOff(string pos);

    /// <summary>The glove's body.</summary>
    Character GloveBody { get; }

    /// <summary>The glove's catch window this frame.</summary>
    double CatchWindow { get; }
}

/// <summary>
/// Who chases the ball (§8.9): the infield → outfield hand-off (D16, D17), the loose ball's nearest body (§8.6, §8.7), and the
/// outfielder who charges a ball still in the air. <see cref="LivePlaySystem"/> hands the glove on and steps the charger.
/// </summary>
public static class PursuitDecider
{
    /// <summary>
    /// The infield → outfield hand-off (§8.9): once the ball (its plant while in the air, or the first reachable point on a liner that
    /// will bounce, #667) is on the outfield grass, the play glove moves to the outfielder whose route meets it earliest (D16) — and
    /// never while the current glove still has a route to it (D17, S-96 on the ground, S-97 in the air, #636): the body keeps the
    /// ball as long as its own route reaches it no later than that outfielder's, or the ball is inside its reach. Never by the
    /// ball's position alone; one way only. The infield's reach on a ball in the air is <c>fielding.chase.infieldAirMul</c> (§8.1):
    /// the S-29 band is held there, not by giving the liner away. The position to hand the glove to, or null to keep it.
    /// </summary>
    public static string? OutfieldHandoff(IPursuitView v, double ballX, double ballZ, bool airborne)
    {
        var R = v.Rules;
        var map = v.Assigned;
        if (FieldingResolver.IsOutfield(v.GlovePos) || !FieldingResolver.OutfieldGrass(ballX, ballZ, R)) return null;
        if (v.Preview is not { } preview || v.Path is not { } path)
        {
            var pick = FieldingResolver.PlayGlove(map, ballX, ballZ, R, v.Bodies);
            return FieldingResolver.HandoffToOutfield(v.GlovePos, pick.Pos) ? pick.Pos : null;
        }
        var of = FieldingPursuit.Choose(
            map, FieldingResolver.OutfieldPursuitPositions, preview, v.Park, path, R, v.Bodies, v.Elapsed, v.ReadyTable);
        // D17, in the air and on the ground alike: the glove keeps the ball while its own route still meets it no later than the
        // outfielder's (the plant on a fly, the first reachable sample on a roller or a liner that will bounce).
        var who = map.TryGetValue(v.GlovePos, out var c) ? c : preview.Fielder;
        var speed = FieldingResolver.ChaseSpeedFt(who, v.GlovePos, preview, R);
        var mine = FieldingPursuit.Plan(preview, v.Park, path, v.Elapsed, v.GloveX, v.GloveZ, speed, R, v.ReadyAt(v.GlovePos),
            !FieldingResolver.IsOutfield(v.GlovePos), v.GloveBody);
        // A scoopable ball inside the glove's reach is a route of zero feet: the touch (§8.3) is this frame's play, whatever the planner says of the next sample.
        var inReach = !airborne && FlyCatch.TouchScoop(preview, v.Park, v.BallX, v.BallZ, v.BallY, v.Elapsed, v.Hang,
            Diamond.Dist(v.GloveX, v.GloveZ, v.BallX, v.BallZ), v.CatchWindow, R);
        if (inReach || mine.Reachable && !FieldingPursuit.Better(of.Route, mine)) return null;
        return of.Position;
    }

    /// <summary>A loose ball is the nearest body's (§8.6, §8.7): the backup behind an overthrow, the fielder beside a fumble. The position to hand the glove to, or null.</summary>
    public static string? LooseHandoff(IPursuitView v)
    {
        var pick = FieldingResolver.NearestGlove(v.Assigned, v.BallX, v.BallZ, v.Rules, v.Bodies);
        if (pick.Pos == v.GlovePos || string.IsNullOrEmpty(pick.Pos)) return null;
        if (v.KeptOff(pick.Pos)) return null;
        return pick.Pos;
    }

    /// <summary>
    /// The outfielder who charges a ball in the air that the glove is not on (§8.9): the one whose route meets the ball earliest,
    /// while the ball is short of where an outfielder should charge; free to move, not the glove, not the buddy and not coasting.
    /// Null when nobody charges this frame.
    /// </summary>
    public static FieldingPursuit.Choice? Charger(IPursuitView v)
    {
        var R = v.Rules;
        if (v.Preview is not { } preview || v.Hit is null || v.Path is not { } path) return null;
        if (v.HoldsBall || v.Throwing || v.LooseBall) return null;
        var live = BallFlight.PointAt(path, v.Elapsed, R);
        var hang = v.Hang;
        var inAir = FieldingResolver.InAir(preview, live.Y, v.Elapsed, R, hang);
        var plant = FlyCatch.ChaseTarget(preview, R, v.Park);
        if (!FieldingResolver.OutfieldShouldCharge(live.X, live.Z, plant.X, plant.Z, R))
            return null;
        // Once a grounded ball reaches the grass, the glove's chase owns the hand-off.
        if (!inAir && FieldingResolver.OutfieldGrass(live.X, live.Z, R))
            return null;
        var of = FieldingPursuit.Choose(
            v.Assigned, FieldingResolver.OutfieldPursuitPositions, preview, v.Park, path, R, v.Bodies, v.Elapsed, v.ReadyTable);
        if (of.Position == v.GlovePos || of.Position == v.BuddyPos || v.Coasting(of.Position)) return null;
        if (!v.CanMove(of.Position)) return null;
        if (!v.Bodies.ContainsKey(of.Position)) return null;
        return of;
    }
}
