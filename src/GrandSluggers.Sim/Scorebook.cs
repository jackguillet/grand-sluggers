namespace GrandSluggers.Sim;

/// <summary>
/// The scorebook (spec §12): the MVP points each play credits, the arm that last took the lead, and the box line. The
/// match raises the credits as plays finish; the scorebook keeps the tally and names the MVP.
/// </summary>
public sealed class Scorebook
{
    readonly Match _match;
    readonly Dictionary<string, int> _points = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The pitcher of the side that last took the lead (the winning pitcher if it holds, §12).</summary>
    string? _leadPitcherId;

    internal Scorebook(Match match) => _match = match;

    RulesTable Rules => _match.Rules;
    bool Over => _match.Over;
    bool Top => _match.Top;
    int Inning => _match.Inning;
    int Outs => _match.Outs;
    int HomeScore => _match.HomeScore;
    int AwayScore => _match.AwayScore;
    Team Home => _match.Home;
    Team Away => _match.Away;
    Character Batter => _match.Batter;

    /// <summary>The offense just took the lead; its pitcher is the winning pitcher if it holds.</summary>
    internal void LeadTakenBy(string pitcherId) => _leadPitcherId = pitcherId;

    /// <summary>MVP points to one character (§12); an empty id or no points is nothing.</summary>
    internal void Credit(string? id, int pts)
    {
        if (string.IsNullOrEmpty(id) || pts <= 0) return;
        _points[id] = _points.GetValueOrDefault(id) + pts;
    }

    /// <summary>
    /// The batter's MVP credit for the play (§12, stars.json mvp): the base points of the hit / walk /
    /// HBP, an RBI per run driven in, and the go-ahead RBI on top when the play put the offense ahead.
    /// </summary>
    internal void CreditBatter(int basePoints, int runs, int ledBefore)
    {
        var m = Rules.Stars.Mvp;
        var goAhead = runs > 0 && ledBefore <= 0 && _match.OffenseLead > 0;
        Credit(Batter.Id, basePoints + runs * m.Rbi + (goAhead ? m.GoAheadRbi : 0));
    }

    /// <summary>
    /// The MVP (§12, stars.json mvp): a walk-off hit names its hitter; otherwise the most points on
    /// either roster, the winning pitcher's points included once the game is over.
    /// </summary>
    public (Character Who, int Points, string Why) Mvp()
    {
        var m = Rules.Stars.Mvp;
        var points = new Dictionary<string, int>(_points, StringComparer.OrdinalIgnoreCase);
        if (Over && HomeScore != AwayScore && _leadPitcherId is { } arm)
            points[arm] = points.GetValueOrDefault(arm) + m.WinningPitcher;
        var walkOff = WalkOffHitter();
        string id;
        if (walkOff is not null) id = walkOff.Id;
        else if (points.Count == 0) return (Home.Captain, 0, "showed up");
        else id = points.OrderByDescending(kv => kv.Value).First().Key;
        var who = Away.Roster.Concat(Home.Roster).First(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var pts = points.GetValueOrDefault(id);
        var why = pts >= m.TookOverAt ? "took over the diamond" : pts >= m.KeptMovingAt ? "kept the line moving" : "did the little things";
        return (who, pts, why);
    }

    /// <summary>The hitter whose hit ended the game in the home half with the winning run (§12), or null.</summary>
    Character? WalkOffHitter()
    {
        if (!Over || _match.Log.Count == 0) return null;
        var last = _match.Log[^1];
        if (last.Context is not { Top: false } ctx || last.RunsScored <= 0) return null;
        if (last.Kind is not (PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)) return null;
        return last.HomeScoreAfter > last.AwayScoreAfter && ctx.HomeScoreBefore <= ctx.AwayScoreBefore ? last.Batter : null;
    }

    public string BoxLine() =>
        $"G{Away.Name} {AwayScore}  {Home.Name} {HomeScore}  {(Over ? "F" : (Top ? "T" : "B") + Inning)}  {Outs} out";
}
