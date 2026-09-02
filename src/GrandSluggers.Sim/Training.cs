namespace GrandSluggers.Sim;

public enum PracticeLesson
{
    Pitching = 1,
    Batting = 2,
    Fielding = 3,
    Running = 4,
    Special = 5,
    Free = 6
}

/// <summary>
/// SMS-style Practice: choosable lessons with skip. Pitching part 2 is MAX charge, not four pitch enums.
/// </summary>
public sealed class Training
{
    public const string ParkId = "harbor-diamond";
    public const int DrillCount = 5;
    public static readonly string[] CorePitches = ["fastball", "changeup", "curve", "slider"];
    public static readonly PracticeLesson[] Lessons =
    [
        PracticeLesson.Pitching, PracticeLesson.Batting, PracticeLesson.Fielding,
        PracticeLesson.Running, PracticeLesson.Special, PracticeLesson.Free
    ];

    int _maxCharges;
    int _throws;
    bool _needStar;
    bool _starInZone;
    bool _timedContact;
    bool _chargedContact;
    bool _caughtAndThrew;
    bool _scoopedHopper;
    bool _turnedTwo;
    bool _ranLead;
    bool _ranSteal;
    bool _ranDash;
    bool _skipped;

    Training(Park park) => Park = park;

    public Park Park { get; }
    public PracticeLesson Lesson { get; private set; } = PracticeLesson.Pitching;
    public int LessonPart { get; private set; } = 1;
    public int CurrentDrill
    {
        get => (int)Lesson;
        private set => Lesson = (PracticeLesson)Math.Clamp(value, 1, 6);
    }
    public bool Finished => _skipped || (int)Lesson > DrillCount && Lesson != PracticeLesson.Free;
    public bool NeedStar => _needStar;
    public bool StarPitchInZone => _starInZone;
    public bool TimedContact => _timedContact;
    public bool ChargedContact => _chargedContact;
    public bool CaughtAndThrew => _caughtAndThrew;
    public bool ScoopedHopper => _scoopedHopper;
    public bool TurnedTwo => _turnedTwo;
    public int MaxCharges => _maxCharges;
    public ThrowResult? LastGoodThrow { get; private set; }
    public ThrowResult? LastBadThrow { get; private set; }
    public IReadOnlyCollection<string> InZonePitchTypes { get; } = new HashSet<string>();

    public static Training Start(ContentCatalog content)
    {
        if (!content.Parks.TryGetValue(ParkId, out var park))
            throw new InvalidDataException("harbor-diamond is missing");
        return new Training(park);
    }

    public Match MakeMatch(ContentCatalog content, int seed = 1, int innings = 9)
    {
        var match = Match.Exhibition(content, "rio", "ashlord", innings, seed, ParkId);
        if (Lesson == PracticeLesson.Running)
            SeedFirst(match);
        return match;
    }

    static void SeedFirst(Match match)
    {
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        var n = 0;
        while (match.First is null && !match.Over && n++ < 16)
            match.Play(wild, take);
    }

    /// <summary>From pitching (lesson 1), skip lands on Fielding so you can scoop. Elsewhere it ends the session.</summary>
    public bool Skip()
    {
        if (Lesson == PracticeLesson.Pitching)
            return Choose(PracticeLesson.Fielding);
        _skipped = true;
        return true;
    }

    public bool Choose(PracticeLesson lesson)
    {
        Lesson = lesson;
        LessonPart = 1;
        _throws = 0;
        _maxCharges = 0;
        _ranLead = false;
        _ranSteal = false;
        _ranDash = false;
        _skipped = false;
        return true;
    }

    public PracticeLesson Cycle(int dir)
    {
        Choose(Shift(Lesson, dir));
        return Lesson;
    }

    public static PracticeLesson Shift(PracticeLesson lesson, int dir)
    {
        var i = Array.IndexOf(Lessons, lesson);
        if (i < 0) i = 0;
        var n = Lessons.Length;
        return Lessons[(i + (dir % n) + n) % n];
    }

    public bool RecordPitch(PitchCommand pitch, Match match) =>
        RecordPitch(pitch, match.Pitcher.Stats.Pitch, match.CanStarPitch);

    public bool RecordPitch(PitchCommand pitch, int pitchStat, bool canStar)
    {
        if (Finished || Lesson != PracticeLesson.Pitching) return false;
        if (canStar) _needStar = true;
        if (!AtBatResolver.PitchInZone(pitch, pitchStat)) return false;
        _throws++;
        if (ChargeFeel.AtMax(pitch.Charge01, 0, 0.5) || pitch.Charge01 >= 1)
            _maxCharges++;
        if (pitch.Star) _starInZone = true;
        if (LessonPart == 1 && _throws >= 3)
            LessonPart = 2;
        if (LessonPart >= 2 && _maxCharges >= 3)
        {
            CurrentDrill = 2;
            LessonPart = 1;
            return true;
        }
        return false;
    }

    public bool RecordSwing(SwingCommand swing, AtBatResult hit)
    {
        if (Finished || Lesson != PracticeLesson.Batting) return false;
        if (!swing.Swing || hit.Quality == ContactQuality.Miss) return false;
        _timedContact = true;
        if (ChargeFeel.IsCharge(swing.Charge01) || swing.Charge01 > 0.5)
            _chargedContact = true;
        if (_timedContact && _chargedContact)
        {
            CurrentDrill = 3;
            return true;
        }
        return false;
    }

    public bool RecordFielding(FieldingResult field)
    {
        if (Finished || Lesson != PracticeLesson.Fielding) return false;
        var caught = field.Fielder is not null &&
                     field.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
        var threw = field.Throw is not null && field.Cutoff is not null;
        var toss = field.Throw is not null && field.Fielder is not null;
        if ((!caught || !threw) && !toss) return false;
        _caughtAndThrew = true;
        if (LessonPart < 2)
            LessonPart = 2;
        return true;
    }

    /// <summary>Fielding part 2: hopper with a runner on first, two throws, two outs.</summary>
    public bool RecordTurnTwo(string? caption)
    {
        if (Finished || Lesson != PracticeLesson.Fielding) return false;
        if (string.IsNullOrEmpty(caption)
            || caption.IndexOf("turns two", StringComparison.OrdinalIgnoreCase) < 0)
            return false;
        _turnedTwo = true;
        CurrentDrill = 4;
        return true;
    }

    /// <summary>Put the on-deck hitter on first so the next hopper can turn two.</summary>
    public bool SetupTurnTwo(Match match)
    {
        if (Lesson != PracticeLesson.Fielding) return false;
        if (match.First is not null) return true;
        var who = match.OnDeck;
        if (who is null || who.Id.Equals(match.Batter.Id, StringComparison.OrdinalIgnoreCase))
            who = match.Offense.Roster.FirstOrDefault(c =>
                !c.Id.Equals(match.Batter.Id, StringComparison.OrdinalIgnoreCase));
        if (who is null) return false;
        return match.StationRunner(1, who);
    }

    public bool RecordGrounder(FieldingResult field)
    {
        if (Finished || (Lesson != PracticeLesson.Fielding && Lesson != PracticeLesson.Running)) return false;
        var hopper = field.Kind is PlayKind.GroundOut or PlayKind.Single && field.HangTimeSec < 1.9 * BallFlight.TimeScale;
        var scooped = field.Fielder is not null && field.Throw is not null;
        if (!hopper || !scooped) return false;
        _scoopedHopper = true;
        if (Lesson == PracticeLesson.Fielding)
            CurrentDrill = 4;
        return true;
    }

    public bool RecordChemThrow(ThrowResult? thr)
    {
        if (Finished || (Lesson != PracticeLesson.Special && Lesson != PracticeLesson.Fielding) || thr is null)
            return false;
        if (thr.Relation == Chemistry.Good) LastGoodThrow = thr;
        else if (thr.Relation == Chemistry.Bad) LastBadThrow = thr;
        else return false;
        if (LastGoodThrow is null || LastBadThrow is null) return false;
        if (LastGoodThrow.SpeedMul <= LastBadThrow.SpeedMul) return false;
        CurrentDrill = 6;
        return true;
    }

    public bool RecordRun(Match? match = null)
    {
        if (Finished || Lesson != PracticeLesson.Running) return false;
        if (match != null)
        {
            if ((match.SelectedState?.Lead01 ?? 0) > 0.2 || match.Lead01 > 0.2)
                _ranLead = true;
            if (match.StealOn || match.StealAttempt || match.ArmedStealBag > 0)
                _ranSteal = true;
            if (match.Dash01 > 0.25)
                _ranDash = true;
        }
        if (!_ranLead || (!_ranSteal && !_ranDash)) return false;
        CurrentDrill = 5;
        return true;
    }

    public bool RecordSpecial(bool star)
    {
        if (Finished || Lesson != PracticeLesson.Special) return false;
        if (!star) return false;
        CurrentDrill = 6;
        return true;
    }

    public static bool TryFindChemPair(Match match, out Character from, out Character goodTo, out Character badTo)
    {
        var people = match.Home.Roster.Concat(match.Away.Roster).ToList();
        from = goodTo = badTo = people[0];
        foreach (var a in people)
        {
            Character? g = null, b = null;
            foreach (var o in people)
            {
                if (o.Id.Equals(a.Id, StringComparison.OrdinalIgnoreCase)) continue;
                var rel = match.Chemistry.Between(a, o);
                if (rel == Chemistry.Good) g ??= o;
                else if (rel == Chemistry.Bad) b ??= o;
                if (g is not null && b is not null)
                {
                    from = a;
                    goodTo = g;
                    badTo = b;
                    return true;
                }
            }
        }
        return false;
    }

    public string Caption => Finished
        ? "Ready."
        : Lesson switch
        {
            PracticeLesson.Pitching => LessonPart == 2 ? "Charge at MAX" : "Throw the ball",
            PracticeLesson.Batting => "Oval and charge",
            PracticeLesson.Fielding => LessonPart >= 2 ? "Turn two" : "Catch it, throw a bag",
            PracticeLesson.Running => "Pick a runner, lead, steal",
            PracticeLesson.Special => "Star pitch / star swing",
            PracticeLesson.Free => "Free practice",
            _ => ""
        };

    public string Verb => Finished
        ? "South  title"
        : Lesson switch
        {
            PracticeLesson.Pitching => "South pitch   LT charge   West changeup   stick break",
            PracticeLesson.Batting => "stick walk   LT MAX   South swing",
            PracticeLesson.Fielding => LessonPart >= 2
                ? "South to second    South to first"
                : "South catch   West jump   d-pad throw   East dash",
            PracticeLesson.Running => "D-pad pick   stick lead   L3 steal",
            PracticeLesson.Special => "North + South star",
            PracticeLesson.Free => "any verb  ·  East skip",
            _ => ""
        };

    public string Progress
    {
        get
        {
            if (Finished) return "done";
            if (Lesson == PracticeLesson.Pitching)
                return LessonPart == 2 ? $"MAX {_maxCharges} / 3" : $"throws {_throws} / 3";
            return Lesson + "  part " + LessonPart;
        }
    }
}
