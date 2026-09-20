namespace GrandSluggers.Sim;

/// <summary>Advanced field lessons observe the same live events and typed play outcomes as Exhibition.</summary>
public sealed partial class TutorialSession
{
    bool _wallCaromSeen;
    bool _looseSeen;
    bool _looseHumanChase;
    double _looseLastX;
    double _looseLastZ;
    double _looseLastBallX;
    double _looseLastBallZ;
    bool _looseTracked;
    string _looseChaserId = "";
    double _dashLastTime;
    double _dashLastX;
    double _dashLastZ;
    string _dashLastHolder = "";
    bool _dashLastHeld;
    bool _relayHumanFeed;
    bool _relayHumanOnward;
    bool _laserHumanThrow;
    bool _laserWasThrowing;
    bool _bufferQueued;
    bool _bufferRetargeted;
    bool _bufferCancelled;
    double _bufferReceiverHeldAt;
    double _bufferQueuedAt;
    int _bufferInitialBag;

    partial void ResetAdvancedEvidence()
    {
        _wallCaromSeen = false;
        _looseSeen = false;
        _looseHumanChase = false;
        _looseLastX = _looseLastZ = _looseLastBallX = _looseLastBallZ = 0;
        _looseTracked = false;
        _looseChaserId = "";
        _dashLastTime = 0;
        _dashLastX = 0;
        _dashLastZ = 0;
        _dashLastHolder = "";
        _dashLastHeld = false;
        _relayHumanFeed = false;
        _relayHumanOnward = false;
        _laserHumanThrow = false;
        _laserWasThrowing = false;
        _bufferQueued = false;
        _bufferRetargeted = false;
        _bufferCancelled = false;
        _bufferReceiverHeldAt = 0;
        _bufferQueuedAt = 0;
        _bufferInitialBag = 0;
    }

    partial void EvaluateAdvancedFieldObjective(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (Lesson.Objective == "human-loose-recovery")
        {
            _looseSeen |= live.Events.Contains(LiveEvent.WallCarom);
            var input = _inputs[^1];
            if (_looseSeen && !live.HoldsBall && input.Source == LivePlayCommandSource.Human && !Demonstration
                && input.Field is { StickX: var sx, StickY: var sz } && sx * sx + sz * sz >= .25
                && live.PursuitManual && _looseTracked
                && (live.GloveX - _looseLastX) * (_looseLastBallX - _looseLastX)
                    + (live.GloveZ - _looseLastZ) * (_looseLastBallZ - _looseLastZ) > 1e-5)
            {
                _looseHumanChase = true;
                _looseChaserId = live.TutorialGloveId;
            }
            if (_looseSeen)
            {
                _looseLastX = live.GloveX; _looseLastZ = live.GloveZ;
                _looseLastBallX = live.BallX; _looseLastBallZ = live.BallZ;
                _looseTracked = true;
            }
            if (_looseSeen && _looseHumanChase && live.HoldsBall
                && live.Events.Contains(LiveEvent.Glove) && live.TutorialGloveId == _looseChaserId)
            {
                Finish(true, "loose-recovered", "You chased the wall carom and scooped the live loose ball.");
                return;
            }
            if (result.CompletedPlay is not null)
                Finish(false, "loose-not-recovered", "Chase the loose wall carom with the glove and secure it yourself.");
            return;
        }
        if (Lesson.Objective == "human-chemistry-throw")
        {
            var input = _inputs[^1];
            if (input.Source == LivePlayCommandSource.Human && !Demonstration
                && input.Field?.Cutoff == true && live.Events.Contains(LiveEvent.ThrowPop) && live.ThrowBag == 0)
                _relayHumanFeed = true;
            if (result.CompletedPlay is not { } chemistryPlay) return;
            var marks = live.TakeTrace(chemistryPlay).Marks ?? [];
            var feed = marks.FirstOrDefault(m => m.Kind == PlayTraceMarkKind.ThrowRelease
                && m.Flight is { FromPos: "CF", Bag: 0 });
            var flight = feed?.Flight;
            var from = flight is null ? null : live.TutorialFielderAt(flight.FromPos);
            var to = flight is null ? null : live.TutorialFielderAt(flight.ReceiverPos);
            var success = _relayHumanFeed && flight is not null && from is not null && to is not null
                && _content.Chemistry.Between(from, to) == Chemistry.Good
                && Math.Abs(flight.SpeedMul - InPlay.ArmMul(from, Match.Rules)
                    * Match.Rules.Fielding.Chem.GoodSpeedMul) <= 1e-5;
            Finish(success, success ? "chemistry-throw" : "chemistry-not-seen",
                success ? "Your cutoff feed used the good chemistry between its thrower and receiver."
                    : "Send the live fly ball through the good-chemistry cutoff pair.");
            return;
        }
        if (Lesson.Objective is "human-buffered-relay" or "human-retargeted-relay" or "human-cancelled-relay")
        {
            var input = _inputs[^1];
            var owned = input.Source == LivePlayCommandSource.Human && !Demonstration;
            var pad = input.Field;
            if (owned && pad?.Cutoff == true && live.Events.Contains(LiveEvent.ThrowPop) && live.ThrowBag == 0)
                _relayHumanFeed = true;
            if (owned && pad?.SouthDown == true && live.Events.Contains(LiveEvent.ThrowQueued))
            {
                _bufferQueued = true;
                _bufferQueuedAt = Elapsed;
                _bufferInitialBag = live.QueuedThrowBag;
            }
            if (_bufferQueued && _bufferInitialBag == 3 && owned && pad?.KeysBag == 4
                && live.ThrowQueued && live.QueuedThrowBag == 4)
                _bufferRetargeted = true;
            if (_bufferQueued && owned && pad?.Cancel == true && live.Events.Contains(LiveEvent.ThrowQueueCleared)
                && Elapsed - _bufferQueuedAt <= Match.Rules.Fielding.Throw.RelayBufferSec + 1e-9)
                _bufferCancelled = true;
            if (Lesson.Objective == "human-cancelled-relay" && _bufferCancelled)
            {
                if (live.HoldsBall && live.GlovePos != "CF" && !live.Throwing && !live.ThrowQueued)
                {
                    if (_bufferReceiverHeldAt <= 0) _bufferReceiverHeldAt = Elapsed;
                    if (Elapsed - _bufferReceiverHeldAt >= .30)
                        Finish(true, "relay-cancelled", "Your cancel cleared the queued throw; the receiver held the ball.");
                }
                if (result.CompletedPlay is not null)
                    Finish(false, "relay-cancel-missed", "Cancel the queued onward throw before the receiver releases it.");
                return;
            }
            if (result.CompletedPlay is not { } bufferedPlay) return;
            var marks = live.TakeTrace(bufferedPlay).Marks ?? [];
            var releases = marks.Where(m => m.Kind == PlayTraceMarkKind.ThrowRelease && m.Flight is not null).ToArray();
            var feed = releases.FirstOrDefault(m => m.Flight is { FromPos: "CF", Bag: 0 });
            var cutter = feed?.Flight?.ReceiverPos;
            var handoff = cutter is not null && marks.Any(m => m.Kind == PlayTraceMarkKind.Reception
                && m.Fielder == cutter && m.T >= feed!.T);
            var onward = handoff && releases.Any(m => m.Flight is { Bag: 4 }
                && m.Flight.FromPos == cutter && m.T >= feed!.T);
            var success = _relayHumanFeed && _bufferQueued && !_bufferCancelled && onward
                && (Lesson.Objective != "human-retargeted-relay" || _bufferRetargeted);
            Finish(success, success ? Lesson.Objective == "human-retargeted-relay" ? "relay-retargeted" : "relay-buffered"
                : "relay-buffer-missed", success ? "Your buffered throw was received and sent to the chosen bag."
                    : "Queue the onward throw while the feed flies; retarget or cancel as the lesson asks.");
            return;
        }
        if (Lesson.Objective == "human-laser-home")
        {
            var input = _inputs[^1];
            if (input.Source == LivePlayCommandSource.Human && !Demonstration
                && input.Field is { SouthDown: true } && !_laserWasThrowing
                && live.Events.Contains(LiveEvent.ThrowPop) && live.ThrowBag == 4
                && _throws.LastOrDefault() == 4)
                _laserHumanThrow = true;
            _laserWasThrowing = live.Throwing;
            if (result.CompletedPlay is not { } laserPlay) return;
            var marks = live.TakeTrace(laserPlay).Marks ?? [];
            var release = marks.FirstOrDefault(m => m.Kind == PlayTraceMarkKind.ThrowRelease
                && m.Flight is { FromPos: "CF" });
            var flight = release?.Flight;
            var thrower = flight is null ? null : live.TutorialFielderAt(flight.FromPos);
            var receiver = flight is null ? null : live.TutorialFielderAt(flight.ReceiverPos);
            var eligibleBag = Match.Rules.Fielding.Abilities.LaserHomeOnly == 0 || flight?.Bag == 4;
            var success = _laserHumanThrow && flight is not null && thrower?.FieldAbility == "laser"
                && receiver is not null && eligibleBag;
            if (success)
            {
                var chem = _content.Chemistry.Between(thrower!, receiver!) switch
                {
                    Chemistry.Good => Match.Rules.Fielding.Chem.GoodSpeedMul,
                    Chemistry.Bad => Match.Rules.Fielding.Chem.BadSpeedMul,
                    _ => 1.0
                };
                var expected = InPlay.ArmMul(thrower!, Match.Rules) * chem * Match.Rules.Fielding.Abilities.LaserMul;
                success = Math.Abs(flight!.SpeedMul - expected) <= 1e-5;
            }
            Finish(success, success ? "laser-home" : "laser-not-used",
                success ? "Your Laser throw carried its ability boost toward home."
                    : "Catch with a Laser glove, then command a throw home while the runner is on third.");
            return;
        }
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
            var previousHolder = _dashLastHolder;
            var previousHeld = _dashLastHeld;
            _dashLastTime = Elapsed;
            _dashLastX = live.GloveX;
            _dashLastZ = live.GloveZ;
            _dashLastHolder = live.TutorialGloveId;
            _dashLastHeld = live.HoldsBall && !live.Throwing;
            var input = _inputs[^1];
            var pad = input.Field;
            if (live.HoldsBall && !live.Throwing && input.Source == LivePlayCommandSource.Human && !Demonstration
                && pad is { StickX: var sx, StickY: var sz } && sx * sx + sz * sz >= .95 * .95
                && previousHeld && previousHolder == live.TutorialGloveId
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
