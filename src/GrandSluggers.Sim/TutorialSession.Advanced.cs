namespace GrandSluggers.Sim;

/// <summary>Advanced field lessons observe the same live events and typed play outcomes as Exhibition.</summary>
public sealed partial class TutorialSession
{
    bool _wallCaromSeen;

    partial void EvaluateAdvancedFieldObjective(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (Lesson.Objective == "human-wall-carom")
        {
            _wallCaromSeen |= live.Events.Contains(LiveEvent.WallCarom);
            if (_wallCaromSeen && _throws.Count > 0 && _throws[0] != 3)
            {
                Finish(false, "wrong-carom-bag", "The wall bounce is live. Send the recovered ball to third.");
                return;
            }
            var third = Diamond.Bag(3);
            var received = _wallCaromSeen && _throws.SequenceEqual(new[] { 3 }) && live.FirstThrowBag == 3
                && !live.Throwing && live.HoldsBall && live.GlovePos == live.CoverPos
                && Diamond.Dist(live.GloveX, live.GloveZ, third.X, third.Z) <= Match.Rules.Fielding.Cover.RadiusFt;
            if (received)
                Finish(true, "carom-returned", "You read the wall bounce and your throw reached third.");
            else if (result.CompletedPlay is not null)
                Finish(false, _wallCaromSeen ? "carom-not-returned" : "no-carom",
                    "The ball must hit the wall before your throw reaches third.");
            return;
        }

        if (Lesson.Objective is not ("human-buddy-rob" or "human-super-rob") || result.CompletedPlay is not { } play)
            return;
        var feat = Lesson.Objective == "human-buddy-rob" ? DefensiveFeat.BuddyJump : DefensiveFeat.SuperJump;
        var succeeded = LastHit?.HomeRun == true
            && play.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true
            && play.Outcome.DefensiveFeat == feat
            && play.Fielder is { } fielder && _humanJumpPresses.Contains(fielder.Id);
        Finish(succeeded, succeeded ? "wall-rob" : "wall-rob-missed",
            succeeded ? "Your jump took a ball that would have cleared the wall for an out."
                : "Take the outfield glove and press West in the wall window. The ball must be caught for an out.");
    }
}
