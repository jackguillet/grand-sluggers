namespace GrandSluggers.Sim;

/// <summary>
/// Harbor drills. Pitch, swing, fly, hopper, chem throw.
/// </summary>
public sealed class Training
{
    public const string ParkId = "harbor-diamond";
    public const int DrillCount = 5;
    public static readonly string[] CorePitches = ["fastball", "changeup", "curve", "slider"];

    readonly HashSet<string> _inZone = new(StringComparer.OrdinalIgnoreCase);
    bool _needStar;
    bool _starInZone;
    bool _timedContact;
    bool _chargedContact;
    bool _caughtAndThrew;
    bool _scoopedHopper;

    Training(Park park) => Park = park;

    public Park Park { get; }
    public int CurrentDrill { get; private set; } = 1;
    public bool Finished => CurrentDrill > DrillCount;
    public IReadOnlyCollection<string> InZonePitchTypes => _inZone;
    public bool StarPitchInZone => _starInZone;
    public bool NeedStar => _needStar;
    public bool TimedContact => _timedContact;
    public bool ChargedContact => _chargedContact;
    public bool CaughtAndThrew => _caughtAndThrew;
    public bool ScoopedHopper => _scoopedHopper;
    public ThrowResult? LastGoodThrow { get; private set; }
    public ThrowResult? LastBadThrow { get; private set; }

    public static Training Start(ContentCatalog content)
    {
        if (!content.Parks.TryGetValue(ParkId, out var park))
            throw new InvalidDataException("harbor-diamond is missing");
        return new Training(park);
    }

    public Match MakeMatch(ContentCatalog content, int seed = 1, int innings = 9) =>
        Match.Exhibition(content, "rio", "ashlord", innings, seed, ParkId);

    public bool RecordPitch(PitchCommand pitch, Match match) =>
        RecordPitch(pitch, match.Pitcher.Stats.Pitch, match.CanStarPitch);

    public bool RecordPitch(PitchCommand pitch, int pitchStat, bool canStar)
    {
        if (Finished || CurrentDrill != 1) return false;
        if (canStar) _needStar = true;
        if (!AtBatResolver.PitchInZone(pitch, pitchStat)) return false;

        var type = CoreType(pitch.Type);
        if (type is not null) _inZone.Add(type);
        if (pitch.Star) _starInZone = true;
        if (PitchDrillDone()) Advance();
        return CurrentDrill != 1;
    }

    public bool RecordSwing(SwingCommand swing, AtBatResult hit)
    {
        if (Finished || CurrentDrill != 2) return false;
        if (!swing.Swing || hit.Quality == ContactQuality.Miss) return false;

        _timedContact = true;
        if (swing.Charge01 > 0.5) _chargedContact = true;
        // |spray| is optional — time + charge is the bar.
        if (_timedContact && _chargedContact) Advance();
        return CurrentDrill != 2;
    }

    public bool RecordFielding(FieldingResult field)
    {
        if (Finished || CurrentDrill != 3) return false;
        var caught = field.Fielder is not null &&
                     field.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
        var threw = field.Throw is not null && field.Cutoff is not null;
        if (!caught || !threw) return false;
        _caughtAndThrew = true;
        Advance();
        return true;
    }

    public bool RecordGrounder(FieldingResult field)
    {
        if (Finished || CurrentDrill != 4) return false;
        var hopper = field.Kind is PlayKind.GroundOut or PlayKind.Single && field.HangTimeSec < 1.9;
        var scooped = field.Fielder is not null && field.Throw is not null && field.Cutoff is not null;
        if (!hopper || !scooped) return false;
        _scoopedHopper = true;
        Advance();
        return true;
    }

    /// <summary>
    /// Record one real throw from a play. Drill 5 advances only after a Good throw
    /// and a Bad throw have both been seen (good must be faster).
    /// </summary>
    public bool RecordChemThrow(ThrowResult? thr)
    {
        if (Finished || CurrentDrill != 5 || thr is null) return false;
        if (thr.Relation == Chemistry.Good) LastGoodThrow = thr;
        else if (thr.Relation == Chemistry.Bad) LastBadThrow = thr;
        else return false;
        if (LastGoodThrow is null || LastBadThrow is null) return false;
        if (LastGoodThrow.SpeedMul <= LastBadThrow.SpeedMul) return false;
        Advance();
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
        : CurrentDrill switch
        {
            1 => "Paint the zone",
            2 => "Time it and charge",
            3 => "Catch it, throw a bag",
            4 => "Grab a grounder, throw to first",
            5 => "Throw to a buddy, then a rival",
            _ => ""
        };

    public string Verb => Finished
        ? "South  title"
        : CurrentDrill switch
        {
            1 => "South pitch   RB cycle   LT charge   North star",
            2 => "LT charge   South swing   LS spray",
            3 => "South catch   East dive   D-pad throw",
            4 => "Charge it   scoop   1 throw",
            5 => "D-pad throw  ·  buddy then rival",
            _ => ""
        };

    public string Progress
    {
        get
        {
            if (Finished) return "5 / 5";
            if (CurrentDrill == 5)
                return (LastGoodThrow is null ? "buddy ·" : "buddy ✓") + "  " +
                       (LastBadThrow is null ? "rival ·" : "rival ✓");
            if (CurrentDrill != 1)
                return $"{CurrentDrill} / {DrillCount}";
            var bits = CorePitches.Select(p => _inZone.Contains(p) ? p : "·");
            var star = !_needStar || _starInZone ? "*" : "star";
            return string.Join("  ", bits) + "  " + star;
        }
    }

    bool PitchDrillDone()
    {
        foreach (var p in CorePitches)
            if (!_inZone.Contains(p)) return false;
        return !_needStar || _starInZone;
    }

    void Advance()
    {
        if (CurrentDrill <= DrillCount)
            CurrentDrill++;
    }

    static string? CoreType(string type)
    {
        var t = type.Trim().ToLowerInvariant();
        foreach (var p in CorePitches)
            if (p == t) return p;
        return null;
    }
}
