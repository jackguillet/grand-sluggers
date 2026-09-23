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
            "Balk: threw to a base after committing to pitch. Runners advance one base.", scorers.Count, scorers));
    }

    /// <summary>A runner completed a steal while the pitcher still held the ball. No plate appearance completes.</summary>
    internal PlayEvent? SettleSetupArrivals()
    {
        var arrived = _runners.Where(r => !r.IsBatter && r.Broke &&
            (r.Scored || r.Live && r.Bag > r.FromBag && r.IsOn(r.Bag))).ToList();
        if (arrived.Count == 0) return null;
        BeginPlay();
        var scorers = new List<string>();
        foreach (var runner in arrived)
        {
            RecordMove(runner.Who, runner.FromBag, runner.Bag);
            AddMvp(runner.Who.Id, Rules.Stars.Mvp.StolenBase);
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
            new SwingCommand(false, 0, 0, false), EmptyHit(true), "Stolen base.", scorers.Count, scorers,
            outcome: new PlayOutcome(RunnerResult: RunnerPlayResult.StolenBase)));
    }
}
