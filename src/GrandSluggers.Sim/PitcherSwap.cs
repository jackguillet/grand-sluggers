namespace GrandSluggers.Sim;

/// <summary>
/// The SET swap pick (spec §4.7, #582): Select opens it, the stick or d-pad steps through every
/// fielder in glove order, Select again confirms, East closes. Any fielder can take the mound;
/// the match trades the gloves (<see cref="Match.SwapPitcher"/>). The pick starts on the best arm.
/// </summary>
public sealed class PitcherSwapPick
{
    public readonly record struct Candidate(string Pos, Character Who);

    public PitcherSwapPick(Match match)
    {
        Candidates = CandidatesOf(match);
        Index = 0;
        var best = -1;
        for (var i = 0; i < Candidates.Count; i++)
            if (Candidates[i].Who.Stats.Pitch > best)
            {
                best = Candidates[i].Who.Stats.Pitch;
                Index = i;
            }
    }

    public IReadOnlyList<Candidate> Candidates { get; }
    public int Index { get; private set; }
    public Candidate Current => Candidates[Index];

    /// <summary>Every glove but the mound, in <see cref="Diamond.Order"/>.</summary>
    public static IReadOnlyList<Candidate> CandidatesOf(Match match)
    {
        var map = FieldingResolver.Assign(match.DefenseRoster, match.Pitcher);
        var list = new List<Candidate>();
        foreach (var pos in Diamond.Order)
            if (pos != "P" && map.TryGetValue(pos, out var who))
                list.Add(new Candidate(pos, who));
        return list;
    }

    /// <summary>Step the pick; wraps. Returns the new candidate.</summary>
    public Candidate Step(int dir)
    {
        if (Candidates.Count == 0 || dir == 0) return Current;
        Index = ((Index + Math.Sign(dir)) % Candidates.Count + Candidates.Count) % Candidates.Count;
        return Current;
    }

    /// <summary>Put the pick on the mound. False when the half already used its swap.</summary>
    public bool Confirm(Match match) => Candidates.Count > 0 && match.SwapPitcher(Current.Who);

    /// <summary>The card line while picking (spec §4.7; the couch copy the book names).</summary>
    public string Tell => Candidates.Count == 0 ? "" : $"SWAP → {Current.Pos} {Current.Who.Name}";

    public static bool CanOpen(Match match) => match.CanSwapPitcher && CandidatesOf(match).Count > 0;
}
