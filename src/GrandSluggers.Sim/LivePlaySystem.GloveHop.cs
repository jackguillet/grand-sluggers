namespace GrandSluggers.Sim;

/// <summary>
/// A star swing's hop over a glove (spec §13, <see cref="StarSwingSkill.Hop"/>): while the ball is in the air, the first infield
/// glove whose reach its path enters at glove height — read against where every body stands as the ball comes — is hopped over.
/// Just before the ball reaches the hop's ring the hop is laid on the path (<see cref="BallHop.Apply"/>), and every frame until
/// the ball is out of the ring it is laid again about where that glove stands now, so a glove that steps toward the ball, or
/// runs back with it, is hopped over all the same. The live ball, the pursuit and the CPU read the one hopped path; the ball
/// then drops back onto its line, and its landing and everything after are the straight ball's. One hop a ball; nothing is rolled.
/// </summary>
public sealed partial class LivePlaySystem
{
    /// <summary>The hop this play's ball still owes, or null once laid or when none is owed.</summary>
    BallHop? _hopOwed;
    string _hopSwing = "";

    /// <summary>The hop being laid: the glove it hops over, that glove's reach, the path without it, and whether the ball has entered the ring.</summary>
    (BallHop Hop, string Pos, double Reach, IReadOnlyList<Sample> Base, bool Entered)? _hopLive;

    /// <summary>Arms the hop for the ball just put in play: a fair ball (not a bunt) off a swing whose row names one.</summary>
    void BeginGloveHop()
    {
        _hopOwed = null;
        _hopLive = null;
        _hopSwing = "";
        if (Hit is null || Ball is null || Path is null || !Hit.InPlay || Hit.Foul || Ball.Foul) return;
        if (Ball.Shape == BattedBallClass.Bunt) return;
        if (StarSkills.SwingHop(Hit.StarSwingUsed, _match.Content?.StarSkills) is not { } hop) return;
        _hopOwed = hop;
        _hopSwing = Hit.StarSwingUsed ?? "";
    }

    /// <summary>Where the body at <paramref name="pos"/> stands this frame.</summary>
    (double X, double Z) FeetOf(string pos) =>
        pos == GlovePos ? (GloveX, GloveZ) : _fielders.TryGetValue(pos, out var feet) ? feet : Starts[pos];

    /// <summary>
    /// Each frame the ball follows its path, before it is placed: find the first infield glove whose reach the path from here
    /// enters at glove height, and once the ball would reach that glove's hop ring within the next two frames, lay the hop;
    /// while the ball is in the ring, lay it again about where the glove stands now. A ball that lands, or runs past two
    /// seconds, before any glove owes no hop.
    /// </summary>
    void ReadGloveHop(double dt)
    {
        if (Path is null || Hit is null) return;
        var t = ElapsedSeconds;
        if (_hopLive is { } live)
        {
            var at = FeetOf(live.Pos);
            var outer = live.Hop.OuterFt(live.Reach);
            var ball = BallFlight.PointAt(live.Base, t, R);
            var inside = Diamond.Dist(ball.X, ball.Z, at.X, at.Z) < outer;
            if (live.Entered && !inside)
            {
                _hopLive = null;
                return;
            }
            _hopLive = live with { Entered = live.Entered || inside };
            if (live.Hop.Apply(live.Base, t, at.X, at.Z, live.Reach) is { } again) Path = again;
            return;
        }
        if (_hopOwed is not { } hop) return;
        var until = Math.Min(Hang, BallHop.WithinSec);
        if (t >= until)
        {
            _hopOwed = null;
            return;
        }
        var map = Assigned();
        (string Pos, Character Who, double X, double Z, double Reach, double T)? first = null;
        foreach (var pos in Diamond.Order)
        {
            if (pos == "C" || FieldingResolver.IsOutfield(pos) || !map.TryGetValue(pos, out var who)) continue;
            var at = FeetOf(pos);
            var reach = CatchRadiusOf(who);
            if (BallHop.Entry(Path, t, at.X, at.Z, reach, R.Fielding.Catch.StandingHeightFt, until) is { } entry
                && (first is null || entry < first.Value.T))
                first = (pos, who, at.X, at.Z, reach, entry);
        }
        if (first is not { } glove) return;
        if (!BallHop.Nears(Path, t, t + 2 * dt, glove.X, glove.Z, hop.OuterFt(glove.Reach), R)) return;
        _hopOwed = null;
        if (hop.Apply(Path, t, glove.X, glove.Z, glove.Reach) is not { } hopped) return;
        _hopLive = (hop, glove.Pos, glove.Reach, Path, false);
        Path = hopped;
        RecordFact(new GloveHopped(_hopSwing, t, glove.X, glove.Z, glove.Pos, glove.Who.Id));
        if (StarSkillTable.Or(_match.Content?.StarSkills).Swing(_hopSwing) is { } row) Sub = $"{row.Name}!";
    }
}
