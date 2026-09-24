namespace GrandSluggers.Sim;

public sealed partial class Match
{
    internal PlayEvent? RecordBalk()
    {
        // No runner, no base award; the pitcher remains committed to deliver.
        if (!_runners.Any(r => r.Live && !r.IsBatter)) return null;
        BeginPlay();
        var scorers = new List<string>();
        foreach (var runner in _runners.Where(r => r.Live && !r.IsBatter).OrderByDescending(r => r.Progress).ToList())
        {
            var from = runner.Bag;
            var to = Math.Min(4, from + 1);
            RecordMove(runner.Who, from, to);
            if (to == 4)
            {
                Score(runner.Who);
                scorers.Add(runner.Who.Name);
                runner.Score(0);
            }
            else runner.Seat(to);
        }
        PruneRunners();
        PitchSetup.Reset();
        EndIfWalkOff();
        return FinishEvent(Emit(PlayKind.Balk, new PitchCommand(PitchFamily.Fastball, 0, false),
            new SwingCommand(false, 0, 0, false), EmptyHit(true),
            PlayCall.Of(new CallPart(CallBeat.Balk)), scorers.Count, scorers));
    }

    /// <summary>A runner completed a steal while the pitcher still held the ball. No plate appearance completes.</summary>
    internal PlayEvent? SettleSetupArrivals()
    {
        // A body on a bag another runner holds has not stolen it (§9.1): nobody is forced before the pitch, so the runner
        // already there keeps it and the arrival must go back or be tagged.
        var arrived = _runners.Where(r => !r.IsBatter && r.Broke &&
            (r.Scored || r.Live && r.Bag > r.FromBag && r.IsOn(r.Bag)
                && !RunnerSystem.Unentitled(_runners, r, _ => false, FlyState.None))).ToList();
        if (arrived.Count == 0) return null;
        BeginPlay();
        var scorers = new List<string>();
        foreach (var runner in arrived)
        {
            RecordMove(runner.Who, runner.FromBag, runner.Bag);
            Scorebook.Credit(runner.Who.Id, Rules.Stars.Mvp.StolenBase);
            AddStars(defense: false, Rules.Stars.Gains.StolenBase);
            if (runner.Scored)
            {
                Score(runner.Who);
                scorers.Add(runner.Who.Name);
            }
            else runner.Seat(runner.Bag);
        }
        PruneRunners();
        EndIfWalkOff();
        return FinishEvent(Emit(PlayKind.StolenBase, new PitchCommand(PitchFamily.Fastball, 0, false),
            new SwingCommand(false, 0, 0, false), EmptyHit(true), PlayCall.Of(new CallPart(CallBeat.StolenBase)), scorers.Count, scorers,
            outcome: new PlayOutcome(RunnerResult: RunnerPlayResult.StolenBase)));
    }
}
