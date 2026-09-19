namespace GrandSluggers.Sim;

public enum TutorialPhase { Brief, Attempt, Feedback, Exited }
public sealed record TutorialFeedback(bool Success, string Code, string Detail);
public sealed record TutorialInput(double Time, LivePlayCommandSource Source, PitchCommand? Pitch = null,
    SwingCommand? Swing = null, LivePadInput? Field = null, int PickoffBag = 0, string? SwapPitcherId = null);
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
    public bool IsFieldLesson => _setup.Policy is "grounder" or "liner" or "airborne";
    public bool IsRunningLesson => Lesson.Category == "running";
    public bool IsOffenseLesson => _setup.Seat == "offense";
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
        Match = Match.Exhibition(_content, home, away, 3, _setup.Seed, parkId: Training.ParkId);
        foreach (var bag in _setup.Runners)
            if (!Match.StationRunner(bag, Match.Away.Roster[bag + 1]))
                throw new InvalidDataException("Cannot station tutorial runner.");
        for (var strike = 0; strike < _setup.Strikes; strike++)
            Match.BeginAtBat(new PitchCommand("fastball", 0, false), Take, out _, out _);
        PrepareSetOpportunity();
        InputsHash = PlayTraceIdentity.Capture(Match).Sha256;
        _firstRunner = Match.First?.Id ?? ""; _batter = Match.Batter.Id;
        _inputs.Clear(); _manualGloves.Clear(); _assistedSinceManual.Clear(); _divers.Clear(); _throws.Clear(); _humanJumpPresses.Clear();
        _manualTakeoverMoved = false;
        _humanAerialCatcher = "";
        _queuedHumanThrowBag = 0;
        ResetRunningEvidence();
        Elapsed = 0; LastPlay = null; LastHit = null; LastTickResult = null; Feedback = null; Paused = false;
    }

    public void Begin(bool demonstration = false)
    {
        if (Phase != TutorialPhase.Brief) throw new InvalidOperationException("Begin requires the lesson brief.");
        Demonstration = demonstration; Phase = TutorialPhase.Attempt;
        if (!IsFieldLesson) return;
        var ball = _setup.Balls[_catalog.Profile];
        var hit = TutorialContact.Create(Match.Park, ball, Match.Rules);
        var preview = Match.PreviewHit(hit);
        if ((_setup.Policy == "grounder") != preview.Grounder || hit.Foul
            || (hit.HomeRun && Lesson.Objective is not ("human-wall-rob" or "human-buddy-rob" or "human-super-rob")))
            throw new InvalidDataException("Tutorial setup no longer produces its intended ball class: " + Lesson.Id);
        LastHit = hit;
        Match.LivePlay.Recording = true;
        var seats = IsOffenseLesson ? new LiveSeats(true, false, false, false) : new LiveSeats(false, true, true, false);
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
        if (!Accepts(source) || _setup.Policy != "cpu-take") return false;
        _inputs.Add(new(Elapsed, source, Pitch: command));
        Match.BeginAtBat(command, Take, out var hit, out var play);
        LastHit = hit; LastPlay = play;
        var verdict = TutorialPlateObjectives.Pitch(Lesson.Objective, _setup, command, play);
        Finish(verdict.Success, verdict.Code, verdict.Detail);
        return true;
    }

    public bool Swing(SwingCommand command, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || _setup.Policy is not ("cpu-strike" or "cpu-ball")) return false;
        command = command with { Human = true };
        _inputs.Add(new(Elapsed, source, Swing: command));
        var bats = Match.Batter.Bats;
        Match.BeginAtBat(CpuPitch, command, out var hit, out var play);
        LastHit = hit; LastPlay = play;
        var verdict = TutorialPlateObjectives.Swing(Lesson.Objective, _setup, bats, command, hit, play);
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
        if (!IsFieldLesson && !Match.LivePlay.Active)
        {
            if (Elapsed >= _setup.TimeoutSec) Finish(false, "timeout", "No completed attempt. Retry when ready.");
            return;
        }
        var live = Match.LivePlay;
        var pos = live.GlovePos; var who = live.TutorialGloveId; var x = live.GloveX; var z = live.GloveZ;
        var couldDive = live.TutorialCanDive;
        var previousDive = live.DiveT;
        var runnerBefore = IsOffenseLesson ? CaptureRunnerBefore() : null;
        var result = live.Apply(LivePlayCommand.Tick(seconds,
            IsOffenseLesson ? LivePadInput.Dead : pad,
            IsOffenseLesson ? pad : LivePadInput.Dead, false, source));
        LastTickResult = result;
        var owned = source == LivePlayCommandSource.Human && !Demonstration;
        if (IsOffenseLesson)
        {
            ObserveRunning(pad, owned, runnerBefore, result);
            LastPlay = result.CompletedPlay;
            if (Phase == TutorialPhase.Attempt && (result.CompletedPlay is not null || Elapsed >= _setup.TimeoutSec))
                Finish(false, "running-opportunity-ended", "The run ended before the requested runner action. Retry the same play.");
            return;
        }
        // West belongs to the defense seat even with a neutral pursuit stick. Shipped rules arm the jump;
        // C80 raises JumpTakeoff. A same-tick completed catch may reset both live flags, so use its typed feat.
        var acceptedJump = live.JumpT > 0 || live.Events.Contains(LiveEvent.JumpTakeoff)
            || result.CompletedPlay?.Fielder?.Id == who && result.CompletedPlay?.Outcome?.DefensiveFeat is
                DefensiveFeat.Jump or DefensiveFeat.BuddyJump or DefensiveFeat.SuperJump or DefensiveFeat.Clamber;
        if (owned && pad.WestDown && acceptedJump)
            _humanJumpPresses.Add(who);
        var moved = Math.Abs(live.GloveX - x) + Math.Abs(live.GloveZ - z) > 1e-6;
        if (live.TutorialAssistedPursuitGloveId.Length > 0)
            _assistedSinceManual.Add(live.TutorialAssistedPursuitGloveId);
        if (owned && live.GlovePos == pos && moved && live.PursuitManual)
        {
            _manualTakeoverMoved = true;
            _assistedSinceManual.Remove(who);
        }
        if (owned && live.PursuitManual && live.GlovePos == pos && (Math.Abs(live.GloveX - x) + Math.Abs(live.GloveZ - z) > 1e-6))
            _manualGloves.Add(who);
        var divingOut = result.CompletedPlay?.Outcome?.DefensiveFeat == DefensiveFeat.Dive;
        if (owned && pad.EastDown && couldDive
            && ((live.TutorialDiver == pos && live.DiveT > previousDive)
                || divingOut && result.CompletedPlay?.Fielder?.Id == who))
            _divers[who] = Elapsed + Match.Rules.Fielding.Catch.DiveArmSec;
        // Human-owned defense never supplies its own throw. ThrowPop is emitted once when the actual throw begins,
        // including an earlier accepted buffer; CPU submissions above are replaced with dead input.
        if (live.Events.Contains(LiveEvent.ThrowQueueCleared)) _queuedHumanThrowBag = 0;
        if (owned && live.Events.Contains(LiveEvent.ThrowQueued))
            _queuedHumanThrowBag = live.QueuedThrowBag is >= 1 and <= 4
                ? live.QueuedThrowBag : live.CommitBagFor(pad); // C80 recovery buffer has no onward-throw queue.
        if (live.Events.Contains(LiveEvent.ThrowPop))
        {
            if (owned || _queuedHumanThrowBag == live.ThrowBag) _throws.Add(live.ThrowBag);
            _queuedHumanThrowBag = 0;
        }
        if (owned && live.Events.Contains(LiveEvent.Glove) && live.Caught && live.PursuitManual)
        {
            if (pad.SouthDown && !live.CatchJump && !live.CatchDive) _humanAerialCatcher = live.TutorialFirstGloveId;
        }
        LastPlay = result.CompletedPlay;
        if (Lesson.Objective == "manual-ground-possession" && live.HoldsBall && live.Preview?.Grounder == true)
        {
            var manual = _manualGloves.Contains(live.TutorialFirstGloveId)
                && !_assistedSinceManual.Contains(live.TutorialFirstGloveId);
            Finish(manual, manual ? "ground-possession" : "assisted-pickup", manual ? "You moved to the ground ball and secured it." : "The assistance collected that ball. Retry and move the glove yourself.");
        }
        else if (Lesson.Objective == "human-dive-out")
        {
            var caughtOut = result.CompletedPlay?.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true
                || live.StampsThisPlay.Any(s => PlayStamp.IsOutWord(s.Word)) && live.CatchDive;
            if (caughtOut && (live.CatchDive || divingOut))
            {
                var catcher = result.CompletedPlay?.Fielder?.Id ?? live.TutorialFirstGloveId;
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
        else if (Lesson.Objective == "human-pickoff") EvaluateSetPlay(result);
        else if (Lesson.Objective == "human-choice-second") EvaluateOutObjective(result);
        else EvaluateExpandedFieldObjective(live, result);
        if (Phase == TutorialPhase.Attempt && (result.CompletedPlay is not null || Elapsed >= _setup.TimeoutSec))
            Finish(false, "timeout", "The opportunity ended. Retry the same setup.");
    }

    /// <summary>Expansion field lessons read accepted human commands and the resulting live glove/throw/out state.</summary>
    void EvaluateExpandedFieldObjective(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (Lesson.Objective == "manual-takeover")
        {
            if (!live.HoldsBall || live.Preview?.Grounder != true) return;
            var playerTookOver = _manualTakeoverMoved
                && _manualGloves.Contains(live.TutorialFirstGloveId)
                && !_assistedSinceManual.Contains(live.TutorialFirstGloveId);
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
            var success = catchOut && _humanAerialCatcher.Length > 0
                && play.Fielder?.Id == _humanAerialCatcher && play.Outcome?.DefensiveFeat == DefensiveFeat.None;
            Finish(success, success ? "aerial-out" : "no-aerial-out",
                success ? "Your South press secured the airborne ball for an out."
                    : "Catch the ball in the air with your own South press while controlling the glove.");
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
                    input.PickoffBag > 0, input.SwapPitcherId is not null }.Count(b => b) != 1)
                throw new InvalidDataException("Invalid tutorial input timeline.");
            if (input.Field is not null) run.Tick(input.Time - run.Elapsed, input.Field, input.Source);
            else
            {
                if (input.Time != run.Elapsed) throw new InvalidDataException("Tutorial input omitted clock frames.");
                var accepted = input.Pitch is not null ? run.Pitch(input.Pitch, input.Source)
                    : input.Swing is not null ? run.Swing(input.Swing, input.Source)
                    : input.PickoffBag > 0 ? run.Pickoff(input.PickoffBag, input.Source)
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

public sealed partial class LivePlaySystem
{
    // Observation only: the same action eligibility/owner that MovePlayer uses. No tutorial exceptions to baseball.
    internal bool TutorialCanDive => PlayerFielding && !HoldsBall && !Throwing && CanMove(GlovePos);
    internal string TutorialDiver => _lungePos;
    internal string TutorialGloveId => GloveChar().Id;
    internal string TutorialFirstGloveId => _firstGlove?.Id ?? "";
}
