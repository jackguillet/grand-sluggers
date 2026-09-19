namespace GrandSluggers.Sim;

/// <summary>Choice lessons pair a player's bag command with the live out and runner identity.</summary>
public sealed partial class TutorialSession
{
    void EvaluateOutObjective(LivePlayCommandResult result)
    {
        if (result.CompletedPlay is not { } play) return;
        if (Lesson.Objective == "human-choice-second")
        {
            var outs = play.Outcome?.OutsMade;
            var chosen = _throws.Count > 0 && _throws[0] == 2;
            var forced = outs?.Any(o => o.Type == OutType.Force && o.Bag == 2
                && o.Runner.Id == _firstRunner) == true;
            var batterSafe = outs?.All(o => o.Runner.Id != _batter) == true
                && play.Outcome?.BatterToBag >= 1;
            var good = chosen && forced && batterSafe;
            Finish(good, good ? "choice-at-second" : "choice-missed",
                good ? "Your throw forced the lead runner at second while the batter reached first."
                    : "Choose second for the force; the named lead runner must be retired there.");
        }
    }
}
