namespace GrandSluggers.Sim;

/// <summary>Evidence from accepted actions on the ordinary front-of-house screens.</summary>
public enum GuidedAction { LeftHandedRosterDrop, BattingOrderChanged, GlovePositionChanged,
    TwoPhysicalSeatsBound, CallTimeOpened, BookOpened, MatchRestarted, SeatLost, SeatRecovered, StickRecalibrated,
    StadiumChosen, StarsChanged, InningsChanged, MercyChanged, SettingsStarted }

public sealed class GuidedTutorialSession
{
    readonly string _profile;
    readonly HashSet<GuidedAction> _actions = [];
    LineupSeat _lostSeat = LineupSeat.Cpu;
    readonly HashSet<LineupSeat> _readied = [];
    public TutorialLesson Lesson { get; }
    public TutorialProgress Progress { get; }
    public TutorialPhase Phase { get; private set; } = TutorialPhase.Brief;
    public TutorialFeedback? Feedback { get; private set; }
    /// <summary>The last typed refusal or reset on the live screen. It explains; it never ends the attempt.</summary>
    public TutorialFeedback? Notice { get; private set; }
    public int Successes => Progress.Count(Lesson, _profile);
    public bool Passed => Progress.Has(Lesson, _profile);

    public GuidedTutorialSession(TutorialLesson lesson, string profile, TutorialProgress progress)
    {
        var objective = lesson.Id switch
        {
            "T-G01" => "guided-lineup", "T-G05" => "guided-seats", "T-G06" => "guided-pause",
            "T-G06-R" => "guided-recovery", "T-G06-C" => "guided-calibration", "T-G07" => "guided-settings", _ => ""
        };
        if (lesson.Status != "implemented" || lesson.Objective != objective || objective == ""
            || !lesson.Profiles.Contains(profile))
            throw new ArgumentException("Unsupported guided tutorial", nameof(lesson));
        Lesson = lesson; _profile = profile; Progress = progress;
    }

    public void Begin()
    {
        if (Phase != TutorialPhase.Brief) throw new InvalidOperationException("Begin requires a brief.");
        Clear(); Feedback = null; Phase = TutorialPhase.Attempt;
    }

    public void Retry()
    {
        if (Phase == TutorialPhase.Exited) throw new InvalidOperationException("Exited lesson.");
        Clear(); Feedback = null; Phase = TutorialPhase.Brief;
    }

    public void Exit() { Clear(); Phase = TutorialPhase.Exited; }

    void Clear() { _actions.Clear(); _readied.Clear(); _lostSeat = LineupSeat.Cpu; Notice = null; }

    /// <summary>Only the owner of an accepted screen transition calls this. Repeated edges do not count twice.</summary>
    public bool Observe(GuidedAction action)
    {
        var required = Required();
        if (Phase != TutorialPhase.Attempt || !required.Contains(action) || _actions.Contains(action)
            || action is GuidedAction.SeatLost or GuidedAction.SeatRecovered or GuidedAction.SettingsStarted
            || !Due(action)) return false;
        return Record(action);
    }

    /// <summary>Every earlier stage is done. Rule edits share one stage: the player picks their order.</summary>
    bool Due(GuidedAction action) => Required().All(a => Stage(a) >= Stage(action) || _actions.Contains(a));

    int Stage(GuidedAction action) => action switch
    {
        GuidedAction.StarsChanged or GuidedAction.InningsChanged or GuidedAction.MercyChanged => 1,
        GuidedAction.SettingsStarted => 2,
        _ => Math.Max(0, Required().ToList().IndexOf(action))
    };

    /// <summary>
    /// Player 1's edit on the Match settings screen, typed by <see cref="ExhibitionSettings.Refusal"/>.
    /// A refusal (Items unavailable, another seat) earns nothing. An accepted edit withdraws every
    /// ready this attempt has seen; <paramref name="clearedReady"/> says a human seat had readied.
    /// </summary>
    public bool ObserveRuleEdit(int row, LineupSeat seat, string? refusal, bool clearedReady)
    {
        if (Lesson.Id != "T-G07" || Phase != TutorialPhase.Attempt) return false;
        if (refusal != null)
        {
            Notice = new TutorialFeedback(false, refusal, "The rule did not change.");
            return true;
        }
        if (seat != LineupSeat.Pad1) return false;
        _readied.Clear();
        Notice = clearedReady ? new TutorialFeedback(false, "ready-reset", "A rule changed after a player readied.") : null;
        var action = row switch
        {
            0 => GuidedAction.StarsChanged, 2 => GuidedAction.InningsChanged, 3 => GuidedAction.MercyChanged,
            _ => (GuidedAction?)null
        };
        if (action is { } edit) Observe(edit);
        return true;
    }

    /// <summary>A human seat's own ready or withdrawal on Match settings. CPU readiness is never evidence.</summary>
    public bool ObserveReady(LineupSeat seat, bool ready)
    {
        if (Lesson.Id != "T-G07" || Phase != TutorialPhase.Attempt || seat == LineupSeat.Cpu) return false;
        return ready ? _readied.Add(seat) : _readied.Remove(seat);
    }

    /// <summary>
    /// The match starts from Match settings. Credit needs every rule edit and every human seat's own
    /// ready after the last rule change; otherwise the attempt fails with a typed reason.
    /// </summary>
    public bool ObserveSettingsStart(IReadOnlyCollection<LineupSeat> humans)
    {
        if (Lesson.Id != "T-G07" || Phase != TutorialPhase.Attempt) return false;
        if (!Due(GuidedAction.SettingsStarted))
            return Fail("settings-incomplete", "The match started before Stars, innings and mercy were each changed.");
        if (humans.Count == 0 || humans.Any(h => h == LineupSeat.Cpu || !_readied.Contains(h)))
            return Fail("ready-missing", "Every player readies after the last rule change.");
        return Record(GuidedAction.SettingsStarted);
    }

    bool Fail(string code, string detail)
    {
        Feedback = new TutorialFeedback(false, code, detail);
        Phase = TutorialPhase.Feedback;
        return true;
    }

    public bool ObserveSeatLost(LineupSeat seat)
    {
        if (Lesson.Id != "T-G06-R" || Phase != TutorialPhase.Attempt || seat == LineupSeat.Cpu
            || _actions.Contains(GuidedAction.SeatLost)) return false;
        _lostSeat = seat;
        return Record(GuidedAction.SeatLost);
    }

    public bool ObserveSeatRecovered(LineupSeat seat)
    {
        if (Lesson.Id != "T-G06-R" || Phase != TutorialPhase.Attempt || seat != _lostSeat
            || !_actions.Contains(GuidedAction.SeatLost)) return false;
        return Record(GuidedAction.SeatRecovered);
    }

    bool Record(GuidedAction action)
    {
        _actions.Add(action);
        if (!Required().All(_actions.Contains)) return true;
        Progress.Record(Lesson, _profile);
        Feedback = new TutorialFeedback(true, "guided-complete", "Completed the actions on the live screen.");
        Phase = TutorialPhase.Feedback;
        return true;
    }

    public IReadOnlyList<GuidedAction> Missing => Required().Where(a => !_actions.Contains(a)).ToArray();

    IReadOnlyList<GuidedAction> Required() => Lesson.Id switch
    {
        "T-G01" => [GuidedAction.LeftHandedRosterDrop, GuidedAction.BattingOrderChanged, GuidedAction.GlovePositionChanged],
        "T-G05" => [GuidedAction.TwoPhysicalSeatsBound],
        "T-G06" => [GuidedAction.CallTimeOpened, GuidedAction.BookOpened, GuidedAction.MatchRestarted],
        "T-G06-R" => [GuidedAction.SeatLost, GuidedAction.SeatRecovered],
        "T-G06-C" => [GuidedAction.StickRecalibrated],
        "T-G07" => [GuidedAction.StadiumChosen, GuidedAction.StarsChanged, GuidedAction.InningsChanged,
            GuidedAction.MercyChanged, GuidedAction.SettingsStarted],
        _ => []
    };
}
