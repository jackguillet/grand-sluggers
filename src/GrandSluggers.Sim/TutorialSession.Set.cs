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
        var outside = new PitchCommand(PitchFamily.Fastball, 0, false, AimX: 4);
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

    public bool BeginPitchCharge(LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy != "pickoff" || !Match.PitchSetup.BeginCharge()) return false;
        _inputs.Add(new(Elapsed, source, BeginPitchCharge: true));
        return true;
    }

    public bool Pickoff(int bag, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy != "pickoff" || bag is < 1 or > 4) return false;
        _inputs.Add(new(Elapsed, source, PickoffBag: bag));
        _humanPickoffBag = source == LivePlayCommandSource.Human && !Demonstration ? bag : 0;
        var started = Match.BeginPickoff(bag, new LiveSeats(false, true, true, false), out var dead, source);
        if (!started)
        {
            LastPlay = dead;
            var balk = dead?.Kind == PlayKind.Balk;
            Finish(false, balk ? "balk-after-charge" : "pickoff-no-runner",
                balk ? "Charging commits the pitch. Throw to the base before you begin the windup."
                    : "That bag had no exposed runner to catch.");
        }
        return true;
    }

    void EvaluateSetPlay(LivePlayCommandResult result)
    {
        if (Lesson.Objective == "human-rundown-tag")
        {
            if (result.CompletedPlay is not { } rundown) return;
            var tagged = rundown.Outcome?.OutsMade.Any(o => o.Type == OutType.Tag
                && o.Runner.Id == _firstRunner && o.Bag == 2) == true;
            var success = _humanPickoffBag == 1 && _rundownSeen && _throws.Contains(2) && tagged
                && rundown.Outcome?.RunnerResult == RunnerPlayResult.PickedOff;
            Finish(success, success ? "rundown-tag" : "rundown-missed",
                success ? "Your pickoff trapped the runner; your follow-up throw made the tag at second."
                    : "Pick off the exposed runner, then send the ball ahead for the tag at second.");
            return;
        }
        var live = Match.LivePlay;
        var received = _humanPickoffBag == 1 && live.PickoffBag == 1
            && live.FirstThrowBag == 1 && live.PickoffReceivedAtFirst;
        if (received)
        {
            Finish(true, "pickoff-checked", "Your pickoff reached first while the runner had broken from the bag.");
            return;
        }
        if (result.CompletedPlay is not null)
            Finish(false, "pickoff-missed", "The pickoff did not reach the named receiver while the runner was exposed.");
    }
}
