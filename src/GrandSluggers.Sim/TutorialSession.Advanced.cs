namespace GrandSluggers.Sim;

/// <summary>Advanced field lessons observe the same live events and typed play outcomes as Exhibition.</summary>
public sealed partial class TutorialSession
{
    bool _wallCaromSeen;
    double _dashLastTime;
    double _dashLastX;
    double _dashLastZ;
    bool _relayHumanFeed;
    bool _relayHumanOnward;

    partial void ResetAdvancedEvidence()
    {
        _wallCaromSeen = false;
        _dashLastTime = 0;
        _dashLastX = 0;
        _dashLastZ = 0;
        _relayHumanFeed = false;
        _relayHumanOnward = false;
    }

    partial void EvaluateAdvancedFieldObjective(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (Lesson.Objective is "human-relay" or "human-snap-relay")
        {
            var input = _inputs[^1];
            var owned = input.Source == LivePlayCommandSource.Human && !Demonstration;
            var pad = input.Field;
            if (owned && pad?.Cutoff == true && live.Events.Contains(LiveEvent.ThrowPop) && live.ThrowBag == 0)
                _relayHumanFeed = true;
            if (live.Events.Contains(LiveEvent.ThrowQueueCleared)) _relayHumanOnward = false;
            if (owned && pad?.SouthDown == true &&
                (live.Events.Contains(LiveEvent.ThrowQueued) && live.QueuedThrowBag == 4
                 || live.Events.Contains(LiveEvent.ThrowPop) && live.ThrowBag == 4))
                _relayHumanOnward = true;
            if (result.CompletedPlay is not { } relayPlay) return;
            var marks = live.TakeTrace(relayPlay).Marks ?? [];
            var feed = marks.FirstOrDefault(m => m.Kind == PlayTraceMarkKind.ThrowRelease
                && m.Flight is { FromPos: "CF", Bag: 0 });
            var cutter = feed?.Flight?.ReceiverPos;
            var handoff = cutter is not null && marks.Any(m => m.Kind == PlayTraceMarkKind.Reception
                && m.Fielder == cutter && m.T >= feed!.T);
            var onward = handoff ? marks.FirstOrDefault(m => m.Kind == PlayTraceMarkKind.ThrowRelease
                && m.Flight is { Bag: 4 } && m.Flight.FromPos == cutter && m.T >= feed!.T) : null;
            var playerOwned = _relayHumanFeed && (Match.Rules.Fielding.Throw.RelayAutoContinue > 0 || _relayHumanOnward);
            var success = playerOwned && handoff && onward?.Flight is not null;
            if (Lesson.Objective == "human-snap-relay" && success)
            {
                var flight = onward!.Flight!;
                var snap = live.TutorialFielderAt(flight.FromPos);
                var receiver = live.TutorialFielderAt(flight.ReceiverPos);
                var at = Diamond.Bag(4);
                var distance = Diamond.Dist(flight.FromX, flight.FromZ, at.X, at.Z);
                var abilities = Match.Rules.Fielding.Abilities;
                success = snap?.FieldAbility == "snap-throw" && receiver is not null;
                if (success && abilities.SnapThrowMul > 1)
                {
                    var pair = _content.Chemistry.Between(snap!, receiver!) switch
                    {
                        Chemistry.Good => Match.Rules.Fielding.Chem.GoodSpeedMul,
                        Chemistry.Bad => Match.Rules.Fielding.Chem.BadSpeedMul,
                        _ => 1.0
                    };
                    success = Math.Abs(flight.SpeedMul - InPlay.ArmMul(snap!, Match.Rules) * pair * abilities.SnapThrowMul) <= 1e-5;
                }
                else if (success)
                {
                    var expected = InPlay.ThrowSec(distance,
                        new ThrowResult(Chemistry.Neutral, flight.SpeedMul, false, Arm: snap!.Stats.Arm,
                            ReleaseSec: abilities.SnapReleaseSec), Match.Rules);
                    success = Math.Abs(flight.DurationSec - expected) <= 1e-5;
                }
            }
            Finish(success, success ? Lesson.Objective == "human-snap-relay" ? "snap-relay" : "relay-handoff" : "relay-not-completed",
                success ? "Your cutoff feed was received and the ball went on toward home."
                    : "Catch the fly, send it through the cutoff, and complete the onward throw toward home.");
            return;
        }
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

        if (Lesson.Objective == "human-ball-dash")
        {
            var previousTime = _dashLastTime;
            var previousX = _dashLastX;
            var previousZ = _dashLastZ;
            _dashLastTime = Elapsed;
            _dashLastX = live.GloveX;
            _dashLastZ = live.GloveZ;
            var input = _inputs[^1];
            var pad = input.Field;
            if (live.HoldsBall && !live.Throwing && input.Source == LivePlayCommandSource.Human && !Demonstration
                && pad is { StickX: var sx, StickY: var sz } && sx * sx + sz * sz >= .95 * .95
                && previousTime > 0 && Elapsed > previousTime)
            {
                var carrier = _content.Must(live.TutorialGloveId);
                var ordinary = FieldingResolver.ChaseSpeedFt(carrier, live.GlovePos, live.Preview, Match.Rules);
                var actual = Diamond.Dist(previousX, previousZ, live.GloveX, live.GloveZ) / (Elapsed - previousTime);
                if (FieldAbilities.HasBallDash(carrier) && actual >= ordinary * 1.15)
                {
                    Finish(true, "ball-dash-carried", "You carried the live ball at Ball Dash speed.");
                    return;
                }
            }
            if (result.CompletedPlay is not null)
                Finish(false, "ball-dash-not-carried", "Secure the ball with a Ball Dash holder, then steer that glove at full speed.");
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

public sealed partial class LivePlaySystem
{
    internal Character? TutorialFielderAt(string position) => Assigned().GetValueOrDefault(position);
}
