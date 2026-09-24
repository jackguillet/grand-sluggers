namespace GrandSluggers.Sim;

public enum TutorialPhase { Brief, Attempt, Feedback, Exited }
public sealed record TutorialFeedback(bool Success, string Code, string Detail);
public sealed record TutorialInput(double Time, LivePlayCommandSource Source, PitchCommand? Pitch = null,
    SwingCommand? Swing = null, LivePadInput? Field = null, int PickoffBag = 0, string? SwapPitcherId = null,
    int StealBag = 0, string? ItemId = null, string? ItemTargetId = null, TutorialPlateTick? Plate = null,
    bool BeginPitchCharge = false, string[]? SwapPositions = null);
public sealed record TutorialRecording(int Version, string Lesson, int Revision, string Profile, string InputsHash, bool Demonstration, TutorialInput[] Inputs);
public sealed record TutorialCompletion(string Lesson, int Revision, string Profile);

public sealed record TutorialPracticeProgress(string Lesson, int Revision, string Profile, int Successes);

/// <summary>Three distinct successful attempts earn mastery. Failures/reset do not erase practice already earned.</summary>
public sealed class TutorialProgress
{
    public const int RequiredSuccesses = 3;
    readonly Dictionary<TutorialCompletion, int> _successes = [];
    public IReadOnlyCollection<TutorialCompletion> Completed => _successes
        .Where(p => p.Value == RequiredSuccesses).Select(p => p.Key).ToArray();
    public IReadOnlyCollection<TutorialPracticeProgress> Saved => _successes
        .Select(p => new TutorialPracticeProgress(p.Key.Lesson, p.Key.Revision, p.Key.Profile, p.Value)).ToArray();
    public int Count(TutorialLesson lesson, string profile) => _successes.GetValueOrDefault(new(lesson.Id, lesson.Revision, profile));
    public bool Has(TutorialLesson lesson, string profile) => Count(lesson, profile) == RequiredSuccesses;
    internal void Record(TutorialLesson lesson, string profile) =>
        _successes[new(lesson.Id, lesson.Revision, profile)] = Math.Min(RequiredSuccesses, Count(lesson, profile) + 1);

    // Compatibility for callers holding explicit completion records. Old single-success lessons have a retired revision.
    public void Restore(IEnumerable<TutorialCompletion> completions, TutorialCatalog catalog) =>
        RestorePractice(completions.Where(c => c is not null)
            .Select(c => new TutorialPracticeProgress(c.Lesson, c.Revision, c.Profile, RequiredSuccesses)), catalog);

    public void RestorePractice(IEnumerable<TutorialPracticeProgress> saved, TutorialCatalog catalog)
    {
        foreach (var row in saved)
        {
            if (row is null || row.Successes < 1 || row.Successes > RequiredSuccesses) continue;
            var lesson = catalog.Lessons.FirstOrDefault(l => l.Status == "implemented" && l.Id == row.Lesson
                && l.Revision == row.Revision && l.Profiles.Contains(row.Profile));
            if (lesson is null) continue;
            var key = new TutorialCompletion(row.Lesson, row.Revision, row.Profile);
            _successes[key] = Math.Max(_successes.GetValueOrDefault(key), row.Successes);
        }
    }
}

/// <summary>Owns a fresh match per attempt. The opponent/setup is scripted; player actions and verdicts use Exhibition's sim.</summary>
public sealed partial class TutorialSession
{
    readonly ContentCatalog _content;
    readonly TutorialCatalog _catalog;
    readonly TutorialSetup _setup;
    readonly List<TutorialInput> _inputs = [];
    readonly HashSet<string> _manualGloves = [];
    readonly HashSet<string> _assistedSinceManual = [];
    readonly Dictionary<string, double> _divers = [];
    readonly List<int> _throws = [];
    readonly HashSet<string> _humanJumpPresses = [];
    int _queuedHumanThrowBag;
    bool _manualTakeoverMoved;
    string _humanAerialCatcher = "";
    string _firstRunner = "";
    string _thirdRunner = "";
    string _secondRunner = "";
    string _batter = "";
    public TutorialLesson Lesson { get; }
    public TutorialProgress Progress { get; }
    public int Successes => Progress.Count(Lesson, _catalog.Profile);
    public bool Passed => Progress.Has(Lesson, _catalog.Profile);
    public Match Match { get; private set; } = null!;
    public TutorialPhase Phase { get; private set; } = TutorialPhase.Brief;
    public TutorialFeedback? Feedback { get; private set; }
    public PlayEvent? LastPlay { get; private set; }
    public AtBatResult? LastHit { get; private set; }
    public LivePlayCommandResult? LastTickResult { get; private set; }
    public bool Paused { get; private set; }
    public bool Demonstration { get; private set; }
    public double Elapsed { get; private set; }
    public string InputsHash { get; private set; } = "";
    public IReadOnlyList<TutorialInput> Inputs => _inputs;
    public IReadOnlyList<int> HumanThrows => _throws;
    public bool IsFieldLesson => _setup.Policy is "grounder" or "liner" or "airborne" or "cpu-special-ground";
    public bool IsItemLesson => _setup.Policy == "cpu-item";
    public bool IsGameContactLesson => _setup.Policy == "game-contact";
    public bool IsRunningLesson => Lesson.Category == "running";
    public bool IsOffenseLesson => _setup.Seat == "offense";
    public bool IsDefenseLesson => _setup.Policy == "steal-defense" || IsFieldLesson && !IsOffenseLesson;
    public bool IsStealLesson => _setup.Policy is "steal-offense" or "steal-defense";
    public double StealWindupStartsAt => _content.Feel.PitcherReadySeconds;
    public bool IsStealWindup => _setup.Policy == "steal-offense" && Phase == TutorialPhase.Attempt
        && !Match.LivePlay.Active && Elapsed >= StealWindupStartsAt;
    public PitchCommand CpuPitch => _setup.Pitch ?? new("fastball", 0, false);
    static readonly SwingCommand Take = new(false, 0, 0, false);
    static readonly SwingCommand Contact = new(true, 0, 0, false);

    public TutorialSession(ContentCatalog content, TutorialCatalog catalog, string lesson, TutorialProgress? progress = null)
    {
        _content = content; _catalog = catalog; Lesson = catalog.Lesson(lesson); Progress = progress ?? new();
        if (Lesson.Status != "implemented" || !Lesson.Profiles.Contains(catalog.Profile))
            throw new InvalidOperationException("This lesson is not available on " + catalog.Profile + ": " + Lesson.Id);
        _setup = catalog.Setups.Single(s => s.Id == Lesson.Setup);
        ResetMatch();
    }

    void ResetMatch()
    {
        var home = _content.Team("Tutorial defense", _setup.Home[0], _setup.Home.Skip(1).ToArray());
        var away = _content.Team("Tutorial offense", _setup.Away[0], _setup.Away.Skip(1).ToArray());
        if (_setup.PitcherId.Length > 0) home = home with { Starter = home.Roster.Single(c => c.Id == _setup.PitcherId) };
        if (_setup.BatterId.Length > 0)
            away = away with { Order = [away.Roster.Single(c => c.Id == _setup.BatterId),
                .. away.Roster.Where(c => c.Id == _setup.OnDeckId),
                .. away.Roster.Where(c => c.Id != _setup.BatterId && c.Id != _setup.OnDeckId)] };
        // A field lesson plays at the park its setup names (F8-c); every other lesson at the training park.
        Match = Match.Exhibition(_content, home, away, 3, _setup.Seed,
            parkId: _setup.Park.Length > 0 ? _setup.Park : Training.ParkId, night: _setup.Night);
        if (_setup.StartingStars > 0)
        {
            if (_setup.Seat == "offense") Match.GiveOffenseStars(_setup.StartingStars);
            else Match.GiveDefenseStars(_setup.StartingStars);
        }
        if (_setup.OpponentStars > 0) Match.GiveOffenseStars(_setup.OpponentStars);
        if (_setup.PoolStars is { } pool) Match.SetDefenseStarsForLesson(pool);
        if (_setup.Bottom) PrepareBottomHalf();
        if (!Match.SetOuts(_setup.Outs)) throw new InvalidDataException("Cannot stage tutorial outs.");
        if (_setup.Policy == "game-half") PrepareGameHalf();
        var namedRunners = _setup.RunnerIdsByProfile?.GetValueOrDefault(_catalog.Profile);
        for (var i = 0; i < _setup.Runners.Length; i++)
        {
            var bag = _setup.Runners[i];
            var who = namedRunners is null ? Match.Away.Roster[bag + 1] : _content.Must(namedRunners[i]);
            if (!Match.StationRunner(bag, who))
                throw new InvalidDataException("Cannot station tutorial runner.");
        }
        for (var strike = 0; strike < _setup.Strikes; strike++)
            Match.BeginAtBat(new PitchCommand(PitchFamily.Fastball, 0, false), Take, out _, out _);
        PrepareSetOpportunity();
        InputsHash = PlayTraceIdentity.Capture(Match).Sha256;
        _firstRunner = Match.First?.Id ?? ""; _secondRunner = Match.Second?.Id ?? "";
        _thirdRunner = Match.Third?.Id ?? ""; _batter = Match.Batter.Id;
        _inputs.Clear(); _manualGloves.Clear(); _assistedSinceManual.Clear(); _divers.Clear(); _throws.Clear(); _humanJumpPresses.Clear();
        _manualTakeoverMoved = false;
        _humanAerialCatcher = "";
        _queuedHumanThrowBag = 0;
        ResetRunningEvidence();
        ResetOutEvidence();
        ResetStealEvidence();
        ResetAdvancedEvidence();
        ResetSpecialEvidence();
        ResetAbilityEvidence();
        ResetGameEvidence();
        ResetPlateEvidence();
        Elapsed = 0; LastPlay = null; LastHit = null; LastTickResult = null; Feedback = null; Paused = false;
    }

    public void Begin(bool demonstration = false)
    {
        if (Phase != TutorialPhase.Brief) throw new InvalidOperationException("Begin requires the lesson brief.");
        Demonstration = demonstration; Phase = TutorialPhase.Attempt;
        if (_setup.Policy == "steal-defense") { BeginStealDefense(); return; }
        if (!IsFieldLesson) return;
        if (_setup.Policy == "cpu-special-ground")
        {
            BeginSpecialGround();
            return;
        }
        var ball = _setup.Balls[_catalog.Profile];
        var hit = TutorialContact.Create(Match.Park, ball, Match.Rules);
        var preview = Match.PreviewHit(hit);
        if ((_setup.Policy == "grounder") != preview.Grounder || hit.Foul
            || (hit.HomeRun && Lesson.Objective is not ("human-wall-rob" or "human-buddy-rob" or "human-super-rob")))
            throw new InvalidDataException("Tutorial setup no longer produces its intended ball class: " + Lesson.Id);
        LastHit = hit;
        Match.LivePlay.Recording = true;
        var seats = Lesson.Objective is "human-double-off" or "human-triple-off" or "human-close-defense"
            or "human-third-force-cancels-run" or "human-third-tag-counts-run" ? new LiveSeats(true, true, true, true)
            : IsOffenseLesson ? new LiveSeats(true, false, false, false) : new LiveSeats(false, true, _setup.Seat != "assisted-defense", false);
        var result = Match.LivePlay.Apply(LivePlayCommand.BeginLive(CpuPitch, Contact, hit, preview, null, seats, 0, LivePlayCommandSource.System));
        if (!result.Snapshot.Active) throw new InvalidDataException("Tutorial setup did not start a live play.");
    }

    public void Retry()
    {
        if (Phase == TutorialPhase.Exited) throw new InvalidOperationException("Exited lesson.");
        ResetMatch(); Phase = TutorialPhase.Brief; Begin();
    }
    public void Exit() { Phase = TutorialPhase.Exited; Paused = false; }
    public void Pause(bool paused) { if (Phase == TutorialPhase.Attempt) Paused = paused; }

    public bool Pitch(PitchCommand command, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy is not ("cpu-take" or "game-count" or "game-half")) return false;
        _inputs.Add(new(Elapsed, source, Pitch: command));
        if (_setup.Policy is "game-count" or "game-half") { PitchGame(command); return true; }
        var beforeStars = Match.DefenseStars;
        var beforeCost = Match.PitchStarCost;
        var hadMeter = Match.CanStarPitch;
        Match.BeginAtBat(command, Take, out var hit, out var play);
        LastHit = hit; LastPlay = play;
        var verdict = Lesson.Objective == "star-resource"
            ? EvaluateStarResource(command, play, beforeStars, beforeCost, hadMeter)
            : Lesson.Objective == "star-pitch"
            ? EvaluateStarPitch(command, play, beforeStars, beforeCost, hadMeter)
            : Lesson.Objective == "star-unavailable"
            ? EvaluateStarUnavailable(command, play, beforeStars)
            : TutorialPlateObjectives.Pitch(Lesson.Objective, _setup, command, play);
        if (verdict is not null) Finish(verdict.Success, verdict.Code, verdict.Detail);
        return true;
    }

    public bool Swing(SwingCommand command, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy is not ("cpu-strike" or "cpu-ball" or "cpu-item" or "game-contact")) return false;
        command = command with { Human = true };
        _inputs.Add(new(Elapsed, source, Swing: command));
        if (_setup.Policy == "game-contact") { SwingGame(command); return true; }
        var bats = Match.Batter.Bats;
        var beforeStars = Match.OffenseStars;
        var beforeCost = Match.SwingStarCost;
        var hadMeter = Match.CanStarSwing;
        Match.BeginAtBat(CpuPitch, command, out var hit, out var play);
        LastHit = hit; LastPlay = play;
        if (IsItemLesson)
        {
            if (!hit.InPlay || hit.Foul || !hit.ChemistryItemOffered)
                Finish(false, "no-item-offer", "Make fair contact with a good-chemistry teammate on deck to earn an item.");
            else
            {
                var preview = Match.PreviewHit(hit, command);
                Match.LivePlay.Recording = true;
                Match.LivePlay.Apply(LivePlayCommand.BeginLive(CpuPitch, command, hit, preview, null,
                    new LiveSeats(true, false, false, false), 0, LivePlayCommandSource.System));
            }
            return true;
        }
        var verdict = Lesson.Objective == "star-swing"
            ? EvaluateStarSwing(command, hit, beforeStars, beforeCost, hadMeter)
            : Lesson.Objective == "cancel-take"
            ? TutorialPlateObjectives.CancelTake(_plateCancelledLoad, command, play)
            : TutorialPlateObjectives.Swing(Lesson.Objective, _setup, bats, command, hit, play);
        Finish(verdict.Success, verdict.Code, verdict.Detail);
        return true;
    }

    bool Accepts(LivePlayCommandSource source) => Phase == TutorialPhase.Attempt && !Paused
        && (source == LivePlayCommandSource.Human || Demonstration);

    /// <summary>Only ordinary seat inputs cross this boundary; callers cannot submit a synthetic out or completion.</summary>
    public void Tick(double seconds, LivePadInput? input = null, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > 0.1) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (Phase != TutorialPhase.Attempt || Paused) return;
        Elapsed += seconds;
        var pad = Accepts(source) ? input ?? LivePadInput.Dead : LivePadInput.Dead;
        _inputs.Add(new(Elapsed, source, Field: pad));
        var plateArrival = StealWindupStartsAt + Motion.PitchRelease
            + PitchFlight.AirSeconds(Match.PitchSpeedMph(CpuPitch), Match.Rules);
        if (_setup.Policy == "steal-offense" && !Match.LivePlay.Active)
        {
            var beforeSteals = Match.Runners.Where(r => r.Broke).Select(r => r.Who.Id).ToHashSet();
            Match.PitchSetup.RunnerInput(pad);
            if (pad.Orders is not null && source == LivePlayCommandSource.Human && !Demonstration)
                foreach (var r in Match.Runners.Where(r => r.Broke && !beforeSteals.Contains(r.Who.Id)))
                {
                    _humanStealArms.Add(r.FromBag);
                    if (r.FromBag == 1) _stealRunner = r.Who.Id;
                    if (r.FromBag == 3) _homeStealRunner = r.Who.Id;
                }
            if (Elapsed >= StealWindupStartsAt) Match.PitchSetup.BeginCharge();
            if (Elapsed >= StealWindupStartsAt + Motion.PitchRelease) Match.PitchSetup.ReleaseBall();
            Match.PitchSetup.Advance(seconds);
        }
        if (_setup.Policy == "steal-offense" && !Match.LivePlay.Active && Elapsed >= plateArrival)
        {
            if (Match.StealOn) BeginStealPitch(Lesson.Objective == "human-double-steal"
                ? new LiveSeats(true, true, true, true) : new LiveSeats(true, false, false, false));
            else
            {
                Match.BeginAtBat(CpuPitch, Take, out _, out var pitchPlay);
                LastPlay = pitchPlay;
                Finish(false, "steal-not-armed", "The pitch was released before you armed a runner.");
            }
        }
        if (!IsFieldLesson && !Match.LivePlay.Active)
        {
            if (Elapsed >= _setup.TimeoutSec) Finish(false, "timeout", "No completed attempt. Retry when ready.");
            return;
        }
        var live = Match.LivePlay;
        ObserveRundown();
        TickCloseOpponent();
        TickScoringOpponent();
        ObserveScoringRunner();
        var closeIconBefore = live.CloseIcon;
        TickDoubledOffOpponent();
        TickDelayedStealOpponent(seconds);
        var pos = live.GlovePos; var who = live.GloveId; var x = live.GloveX; var z = live.GloveZ;
        var couldDive = live.CanDiveNow;
        var previousDive = live.DiveT;
        var runnerBefore = IsOffenseLesson ? CaptureRunnerBefore() : null;
        var thirdWasOnBag = Lesson.Objective == "human-double-steal" && Match.Runners.Any(r => r.Bag == 3 && r.Phase == RunnerPhase.OnBag);
        var result = live.Apply(LivePlayCommand.Tick(seconds,
            IsOffenseLesson ? LivePadInput.Dead : pad,
            IsOffenseLesson ? pad : LivePadInput.Dead, false, source));
        LastTickResult = result;
        if (IsGameContactLesson) { ObserveGameContact(result); return; }
        ObserveScoringRunner();
        var owned = source == LivePlayCommandSource.Human && !Demonstration;
        pad = RunnerEvidence(pad);
        ObserveCloseOffense(closeIconBefore, pad, owned, result);
        ObserveCloseDefense(closeIconBefore, pad, owned, result);
        ObserveDelayedHomeSend(pad, owned, thirdWasOnBag);
        if (IsOffenseLesson && (IsFieldLesson || IsItemLesson))
        {
            if (IsItemLesson)
            {
                ObserveSpecialItem(live, result);
                LastPlay = result.CompletedPlay;
                if (Phase == TutorialPhase.Attempt && (result.CompletedPlay is not null || Elapsed >= _setup.TimeoutSec))
                    Finish(false, "item-opportunity-ended", "The live ball ended before the chosen item took effect.");
                return;
            }
            ObserveRunning(pad, owned, runnerBefore, result);
            LastPlay = result.CompletedPlay;
            if (Phase == TutorialPhase.Attempt && (result.CompletedPlay is not null || Elapsed >= _setup.TimeoutSec))
                Finish(false, "running-opportunity-ended", "The run ended before the requested runner action. Retry the same play.");
            return;
        }
        // West belongs to the defense seat even with a neutral pursuit stick. Shipped rules arm the jump;
        // The jump arc raises JumpTakeoff. A same-tick completed catch may reset both live flags, so use its typed feat.
        var acceptedJump = live.JumpT > 0 || live.Events.Contains(LiveEvent.JumpTakeoff)
            || result.CompletedPlay?.Fielder?.Id == who && result.CompletedPlay?.Outcome?.DefensiveFeat is
                DefensiveFeat.Jump or DefensiveFeat.BuddyJump or DefensiveFeat.SuperJump or DefensiveFeat.Clamber;
        if (owned && pad.WestDown && acceptedJump)
            _humanJumpPresses.Add(who);
        var moved = Math.Abs(live.GloveX - x) + Math.Abs(live.GloveZ - z) > 1e-6;
        foreach (var step in live.Facts.OfType<AssistedRouteStep>())
            _assistedSinceManual.Add(step.GloveId);
        if (owned && live.GlovePos == pos && moved && live.PursuitManual)
        {
            _manualTakeoverMoved = true;
            _assistedSinceManual.Remove(who);
        }
        if (owned && live.PursuitManual && live.GlovePos == pos && (Math.Abs(live.GloveX - x) + Math.Abs(live.GloveZ - z) > 1e-6))
            _manualGloves.Add(who);
        var divingOut = result.CompletedPlay?.Outcome?.DefensiveFeat == DefensiveFeat.Dive;
        if (owned && pad.EastDown && couldDive
            && ((live.LungingGlovePos == pos && live.DiveT > previousDive)
                || divingOut && result.CompletedPlay?.Fielder?.Id == who))
            _divers[who] = Elapsed + Match.Rules.Fielding.Catch.DiveArmSec;
        // Human-owned defense never supplies its own throw. ThrowCommitted is emitted once when the throw command is accepted,
        // including an earlier accepted buffer; CPU submissions above are replaced with dead input.
        if (live.Events.Contains(LiveEvent.ThrowQueueCleared)) _queuedHumanThrowBag = 0;
        if (owned && live.Events.Contains(LiveEvent.ThrowQueued))
            _queuedHumanThrowBag = live.QueuedThrowBag is >= 1 and <= 4
                ? live.QueuedThrowBag : live.CommitBagFor(pad); // The recovery buffer has no onward-throw queue.
        if (live.Events.Contains(LiveEvent.ThrowCommitted))
        {
            if ((owned && !IsOffenseLesson && (pad.SouthDown || pad.Cutoff))
                || _queuedHumanThrowBag == live.ThrowBag) _throws.Add(live.ThrowBag);
            _queuedHumanThrowBag = 0;
        }
        if (owned && live.Events.Contains(LiveEvent.Glove) && live.Caught && live.PursuitManual)
        {
            if (!live.CatchJump && !live.CatchDive && _manualGloves.Contains(live.FirstGloveId)
                && !_assistedSinceManual.Contains(live.FirstGloveId))
                _humanAerialCatcher = live.FirstGloveId;
        }
        LastPlay = result.CompletedPlay;
        if (Lesson.Objective == "manual-ground-possession" && live.HoldsBall && live.Call != FairFoulCall.Caught)
        {
            var manual = _manualGloves.Contains(live.FirstGloveId)
                && !_assistedSinceManual.Contains(live.FirstGloveId);
            Finish(manual, manual ? "ground-possession" : "assisted-pickup", manual ? "You moved to the ground ball and secured it." : "The assistance collected that ball. Retry and move the glove yourself.");
        }
        else if (Lesson.Objective is "hazard-redirect-take" or "hazard-carom-take") EvaluateHazardTake(live, result);
        else if (Lesson.Objective == "hazard-dodge-catch") EvaluateHazardDodge(live, result);
        else if (Lesson.Objective == "human-special-ground") EvaluateSpecialGround(live, result);
        else if (Lesson.Objective == "human-ability-reach") EvaluateAbilityReach(live, result);
        else if (Lesson.Objective == "human-dive-out")
        {
            var caughtOut = result.CompletedPlay?.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true
                || live.StampsThisPlay.Any(s => PlayStamp.IsOutWord(s.Word)) && live.CatchDive;
            if (caughtOut && (live.CatchDive || divingOut))
            {
                var catcher = result.CompletedPlay?.Fielder?.Id ?? live.FirstGloveId;
                var humanDive = _divers.TryGetValue(catcher, out var until) && Elapsed <= until;
                Finish(humanDive, humanDive ? "diving-out" : "assisted-catch", humanDive ? "Your dive caught the ball in the air for an out." : "That catch was assisted. Retry and command the dive.");
            }
            else if (result.CompletedPlay is not null)
                Finish(false, "no-diving-out", "The play ended without your diving catch. Retry and dive as the ball enters reach.");
        }
        else if (Lesson.Objective == "human-double-play" && result.CompletedPlay is { } play)
        {
            var outs = play.Outcome?.OutsMade;
            var correct = outs is { Count: 2 } && outs[0].Type == OutType.Force && outs[0].Bag == 2
                && outs[0].Runner.Id == _firstRunner && outs[1].Type == OutType.ThrowOutAtFirst && outs[1].Runner.Id == _batter
                && _throws.SequenceEqual(new[] { 2, 1 });
            Finish(correct, correct ? "turned-two" : "double-play-missed", correct ? "Your two throws beat both runners: second, then first." : "Make the force at second, then command the throw to first before the batter arrives.");
        }
        else if (_setup.Policy is "steal-offense" or "steal-defense") EvaluateStealPlay(result);
        else if (Lesson.Objective is "human-pickoff" or "human-rundown-tag") EvaluateSetPlay(result);
        else if (Lesson.Objective is "human-choice-second" or "human-double-off" or "human-triple-off" or "human-force-home"
            or "human-third-force-zero-run" or "human-third-force-cancels-run" or "human-third-tag-counts-run") EvaluateOutObjective(result);
        else EvaluateExpandedFieldObjective(live, result);
        if (Phase == TutorialPhase.Attempt && (result.CompletedPlay is not null || Elapsed >= _setup.TimeoutSec))
            Finish(false, "timeout", "The opportunity ended. Retry the same setup.");
    }

    /// <summary>Expansion field lessons read accepted human commands and the resulting live glove/throw/out state.</summary>
    /// <summary>
    /// The field's take lessons (F8-c): the ball goes through a redirect, or off a solid body, and the player's own glove takes
    /// it after — moved there by the player, not the assistance. A take before the hazard acted, or a play that ends first, fails.
    /// </summary>
    void EvaluateHazardTake(LivePlaySystem live, LivePlayCommandResult result)
    {
        var acted = Lesson.Objective == "hazard-redirect-take" ? live.RedirectsThisPlay.Count > 0 : live.CaromsThisPlay.Count > 0;
        var what = Lesson.Objective == "hazard-redirect-take" ? "came out of the other mouth" : "bounced off the body";
        if (live.HoldsBall)
        {
            var manual = _manualGloves.Contains(live.FirstGloveId) && !_assistedSinceManual.Contains(live.FirstGloveId);
            var success = acted && manual;
            Finish(success, success ? "hazard-take" : acted ? "assisted-pickup" : "before-hazard",
                success ? $"You read where the ball {what} and took it yourself."
                    : acted ? "The assistance collected that ball. Retry and move the glove yourself."
                    : $"You took it before it {what}. Let the hazard act, then go get it.");
        }
        else if (result.CompletedPlay is not null)
            Finish(false, "no-take", "The play ended before your glove took the ball.");
    }

    /// <summary>
    /// The status volume's lesson (F8-c): catch the fly with your own press, and never let the catching glove touch a volume on
    /// the way — the slow would have cost the catch. A slowed catcher, or no catch, fails.
    /// </summary>
    void EvaluateHazardDodge(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (result.CompletedPlay is not { } play) return;
        var catchOut = play.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true;
        var caught = catchOut && _humanAerialCatcher.Length > 0 && play.Fielder?.Id == _humanAerialCatcher;
        var slowed = live.SlowsThisPlay.Any(s => !s.IsRunner && s.Who.Id == _humanAerialCatcher);
        var success = caught && !slowed;
        Finish(success, success ? "dodged-catch" : slowed ? "slowed" : "no-catch",
            success ? "You went around the volume and made the catch."
                : slowed ? "You ran through the volume and it slowed you. Go around it."
                : "Catch the fly yourself with a South press.");
    }

    void EvaluateExpandedFieldObjective(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (Lesson.Objective == "manual-takeover")
        {
            if (!live.HoldsBall || live.Preview?.Grounder != true) return;
            var playerTookOver = _manualTakeoverMoved
                && _manualGloves.Contains(live.FirstGloveId)
                && !_assistedSinceManual.Contains(live.FirstGloveId);
            Finish(playerTookOver, playerTookOver ? "manual-takeover" : "assisted-pickup",
                playerTookOver ? "You took the glove and secured the ground ball."
                    : "The helper kept the glove, or you did not move it to the ball after taking control.");
            return;
        }
        var namedBag = Lesson.Objective switch
        {
            "throw-bag-1" => 1, "throw-bag-2" => 2, "throw-bag-3" => 3, "throw-bag-4" => 4, _ => 0
        };
        if (namedBag > 0)
        {
            if (_throws.Count > 0 && _throws[0] != namedBag)
            {
                Finish(false, "wrong-bag", "That throw went to a different bag. Arm the named bag, then throw.");
                return;
            }
            var at = Diamond.Bag(namedBag);
            var received = _throws.Count == 1 && live.FirstThrowBag == namedBag && live.ThrowBag == namedBag
                && !live.Throwing && live.HoldsBall && live.GlovePos == live.CoverPos
                && Diamond.Dist(live.GloveX, live.GloveZ, at.X, at.Z) <= Match.Rules.Fielding.Cover.RadiusFt;
            // A force at first can complete on the reception frame, clearing the live glove before it can be inspected.
            received |= namedBag == 1 && _throws.SequenceEqual(new[] { 1 })
                && result.CompletedPlay?.Outcome?.OutsMade.Any(o => o.Type == OutType.ThrowOutAtFirst && o.Bag == 1) == true;
            if (received)
            {
                Finish(true, "throw-" + new[] { "", "first", "second", "third", "home" }[namedBag],
                    "Your throw reached the named bag and its receiver secured the ball.");
                return;
            }
            if (result.CompletedPlay is not null)
                Finish(false, _throws.Count == 0 ? "no-throw" : "throw-not-received",
                    "The play ended before your throw was received at the named bag.");
            return;
        }
        if (Lesson.Objective is not ("human-aerial-out" or "human-jump-out") || result.CompletedPlay is not { } play)
        {
            EvaluateAdvancedFieldObjective(live, result);
            return;
        }
        var catchOut = play.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true;
        if (Lesson.Objective == "human-aerial-out")
        {
            var catcher = play.Fielder?.Id ?? "";
            var success = catchOut && _manualGloves.Contains(catcher)
                && !_assistedSinceManual.Contains(catcher) && play.Outcome?.DefensiveFeat == DefensiveFeat.None;
            Finish(success, success ? "aerial-out" : "no-aerial-out",
                success ? "Your positioning secured the airborne ball for an out."
                    : "Move the glove into position to catch the ball in the air. No catch button.");
        }
        else
        {
            var success = catchOut && play.Fielder is { } jumper && _humanJumpPresses.Contains(jumper.Id)
                && play.Outcome?.DefensiveFeat == DefensiveFeat.Jump;
            Finish(success, success ? "jumping-out" : "no-jumping-out",
                success ? "Your West press timed a jumping catch for an out."
                    : "Take the glove and press West in the jump window to catch the airborne ball.");
        }
    }

    partial void EvaluateAdvancedFieldObjective(LivePlaySystem live, LivePlayCommandResult result);
    partial void ResetAdvancedEvidence();


    public TutorialRecording Recording() => new(1, Lesson.Id, Lesson.Revision, _catalog.Profile, InputsHash, Demonstration, _inputs.ToArray());

    public static TutorialSession Replay(ContentCatalog content, TutorialCatalog catalog, TutorialRecording recording)
    {
        if (recording.Version != 1 || recording.Profile != catalog.Profile || recording.Inputs is null)
            throw new InvalidDataException("Tutorial recording has an unsupported version/profile or missing inputs.");
        var run = new TutorialSession(content, catalog, recording.Lesson);
        if (run.Lesson.Revision != recording.Revision || run.InputsHash != recording.InputsHash)
            throw new InvalidDataException("Tutorial recording uses a different lesson revision or gameplay inputs.");
        run.Begin(recording.Demonstration);
        foreach (var input in recording.Inputs)
        {
            if (input is null || !double.IsFinite(input.Time) || input.Time < run.Elapsed || run.Phase != TutorialPhase.Attempt
                || new[] { input.Pitch is not null, input.Swing is not null, input.Field is not null,
                    input.PickoffBag > 0, input.SwapPitcherId is not null, input.StealBag > 0, input.ItemId is not null,
                    input.Plate is not null, input.BeginPitchCharge, input.SwapPositions is not null }.Count(b => b) != 1)
                throw new InvalidDataException("Invalid tutorial input timeline.");
            if (input.Field is not null) run.Tick(input.Time - run.Elapsed, input.Field, input.Source);
            else
            {
                if (input.Time != run.Elapsed) throw new InvalidDataException("Tutorial input omitted clock frames.");
                var accepted = input.Pitch is not null ? run.Pitch(input.Pitch, input.Source)
                    : input.BeginPitchCharge ? run.BeginPitchCharge(input.Source)
                    : input.Swing is not null ? run.Swing(input.Swing, input.Source)
                    : input.PickoffBag > 0 ? run.Pickoff(input.PickoffBag, input.Source)
                    : input.StealBag > 0 ? run.ArmSteal(input.StealBag, input.Source)
                    : input.ItemId is not null ? run.Item(input.ItemId, input.ItemTargetId!, input.Source)
                    : input.Plate is not null ? run.Plate(input.Plate, input.Source)
                    : input.SwapPositions is { Length: 2 } pair ? run.SwapPositions(pair[0], pair[1], input.Source)
                    : run.SwapPitcher(input.SwapPitcherId!, input.Source);
                if (!accepted) throw new InvalidDataException("Tutorial input does not belong to the teaching role.");
            }
        }
        return run;
    }

    void Finish(bool success, string code, string detail)
    {
        if (Phase != TutorialPhase.Attempt) return;
        if (Demonstration) { success = false; code = "demonstration"; detail = "Demonstration finished. Retry to perform the skill yourself."; }
        Feedback = new(success, code, detail); Phase = TutorialPhase.Feedback;
        if (success) Progress.Record(Lesson, _catalog.Profile);
    }
}

/// <summary>Authored contact inputs use the production ball flight, not a parallel tutorial flight.</summary>
public static class TutorialContact
{
    public static AtBatResult Create(Park park, TutorialBall fixture, RulesTable rules)
    {
        var exit = fixture.ExitMph;
        if (fixture.CarryFt > 0)
        {
            var lo = 20.0; var hi = 160.0;
            if (fixture.CarryFt < BallFlight.CarryFeet(lo, fixture.LaunchDeg, 0, rules)
                || fixture.CarryFt > BallFlight.CarryFeet(hi, fixture.LaunchDeg, 0, rules))
                throw new InvalidDataException("Tutorial carry is outside the supported contact range.");
            for (var i = 0; i < 40; i++)
            {
                var mid = (lo + hi) / 2;
                if (BallFlight.CarryFeet(mid, fixture.LaunchDeg, 0, rules) < fixture.CarryFt) lo = mid; else hi = mid;
            }
            exit = Math.Round((lo + hi) / 2, 1);
        }
        var ball = BattedBall.Of(exit, fixture.LaunchDeg, fixture.SprayDeg, false, park, rules);
        return new(ContactQuality.Nice, !ball.Foul, false, exit, fixture.LaunchDeg, Math.Round(ball.LandingDist, 1),
            ball.HomeRun, false, null, null, SprayDeg: fixture.SprayDeg, Foul: ball.Foul, Class: ball.Shape);
    }
}
