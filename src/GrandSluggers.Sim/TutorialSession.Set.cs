namespace GrandSluggers.Sim;

/// <summary>SET lessons use the same pitcher swap and pickoff commands as Exhibition.</summary>
public sealed partial class TutorialSession
{
    string _startingPitcher = "";
    int _staminaBeforeSwap;
    int _humanPickoffBag;

    void PrepareSetOpportunity()
    {
        _startingPitcher = Match.Pitcher.Id;
        _staminaBeforeSwap = Match.PitcherStamina;
        _humanPickoffBag = 0;
        if (_setup.Policy == "pickoff" && !Match.StartStealAt(1))
            throw new InvalidDataException("Tutorial pickoff runner could not arm in SET.");
        if (_setup.Policy != "pitcher-swap") return;
        // Repeated ordinary taken balls spend the starter's arm without recording a user pitch.
        var outside = new PitchCommand("fastball", 0, false, AimX: 4);
        for (var i = 0; i < 160 && !Match.PitcherTired && !Match.Over; i++)
            Match.Play(outside, Take);
        if (!Match.PitcherTired || !Match.CanSwapPitcher)
            throw new InvalidDataException("Tutorial pitcher-swap setup could not expose a tired starter.");
        _staminaBeforeSwap = Match.PitcherStamina;
    }

    public bool SwapPitcher(string fielderId, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy != "pitcher-swap" || string.IsNullOrWhiteSpace(fielderId)) return false;
        var next = Match.DefenseRoster.FirstOrDefault(c => c.Id == fielderId);
        if (next is null || next.Id == _startingPitcher) return false;
        _inputs.Add(new(Elapsed, source, SwapPitcherId: fielderId));
        var changed = Match.SwapPitcher(next);
        var earned = source == LivePlayCommandSource.Human && !Demonstration && changed
            && Match.Pitcher.Id == fielderId && Match.PitcherStamina > _staminaBeforeSwap
            && !Match.PitcherTired;
        Finish(earned, earned ? "fresh-pitcher" : "swap-missed",
            earned ? "You replaced the tired pitcher with a fresh arm."
                : "Choose an eligible fielder whose fresh arm clears the tired state.");
        return true;
    }

    public bool Pickoff(int bag, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy != "pickoff" || bag is < 1 or > 3) return false;
        _inputs.Add(new(Elapsed, source, PickoffBag: bag));
        _humanPickoffBag = source == LivePlayCommandSource.Human && !Demonstration ? bag : 0;
        var started = Match.BeginPickoff(bag, LiveSeats.CpuOnly, out var dead, source);
        if (!started)
        {
            LastPlay = dead;
            Finish(false, "pickoff-no-runner", "That bag had no exposed runner to catch.");
        }
        return true;
    }

    void EvaluateSetPlay(LivePlayCommandResult result)
    {
        if (result.CompletedPlay is not { } play) return;
        var target = play.Outcome?.OutsMade.Any(o => o.Runner.Id == _firstRunner
            && o.Type == OutType.Tag && o.Bag is 1 or 2) == true;
        var picked = _humanPickoffBag == 1 && play.Outcome?.RunnerResult == RunnerPlayResult.PickedOff
            && target && play.Outcome?.ThrowEndpoint == new ThrowEndpoint(ThrowOrigin.PitcherRubber, 1);
        Finish(picked, picked ? "picked-off" : "pickoff-safe",
            picked ? "Your pickoff caught the runner between bags and the defense made the tag."
                : "The runner escaped the pickoff; choose the occupied bag while the runner is exposed.");
    }
}
