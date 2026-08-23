namespace GrandSluggers.Sim;

public sealed class Match
{
    public const int DefaultInnings = 3;

    readonly AtBatResolver _atBat;
    readonly FieldingResolver _fielding;
    readonly Random _rng;
    readonly Dictionary<string, int> _mvp = new(StringComparer.OrdinalIgnoreCase);
    readonly List<PlayEvent> _log = [];

    public ContentCatalog Content { get; }
    public Park Park { get; }
    public Team Away { get; }
    public Team Home { get; }
    public IReadOnlyList<Character> AwayOrder { get; }
    public IReadOnlyList<Character> HomeOrder { get; }
    public int Innings { get; }
    public int Inning { get; private set; } = 1;
    public bool Top { get; private set; } = true;
    public int Outs { get; private set; }
    public int Balls { get; private set; }
    public int Strikes { get; private set; }
    public int AwayScore { get; private set; }
    public int HomeScore { get; private set; }
    public Character? First { get; private set; }
    public Character? Second { get; private set; }
    public Character? Third { get; private set; }
    public int AwayBatter { get; private set; }
    public int HomeBatter { get; private set; }
    public double AwayStars { get; private set; }
    public double HomeStars { get; private set; }
    public int AwayStamina { get; private set; } = 100;
    public int HomeStamina { get; private set; } = 100;
    public bool Over { get; private set; }
    public IReadOnlyList<PlayEvent> Log => _log;
    public ChemistryTable Chemistry => Content.Chemistry;

    public Match(ContentCatalog content, Team away, Team home, Park park, int innings = DefaultInnings, int seed = 1)
    {
        Content = content;
        Away = away;
        Home = home;
        Park = park;
        Innings = innings;
        _rng = new Random(seed);
        _atBat = new AtBatResolver(content.Chemistry);
        _fielding = new FieldingResolver(content.Chemistry);
        AwayOrder = BattingOrder(away);
        HomeOrder = BattingOrder(home);
        AwayStars = content.Chemistry.StartingStars(away);
        HomeStars = content.Chemistry.StartingStars(home);
    }

    public static Match Slice(ContentCatalog content, int innings = DefaultInnings, int seed = 1)
    {
        var park = content.Parks["harbor-diamond"];
        return new Match(content, PresetTeams.EmberCourt(content), PresetTeams.SparkAllStars(content), park, innings, seed);
    }

    public Team Offense => Top ? Away : Home;
    public Team Defense => Top ? Home : Away;
    public Character Batter => (Top ? AwayOrder : HomeOrder)[Top ? AwayBatter : HomeBatter];
    public Character Pitcher => Defense.Captain;
    public Character? OnDeck
    {
        get
        {
            var order = Top ? AwayOrder : HomeOrder;
            var i = ((Top ? AwayBatter : HomeBatter) + 1) % order.Count;
            return order[i];
        }
    }

    public IEnumerable<Character> RunnersOn()
    {
        if (First is not null) yield return First;
        if (Second is not null) yield return Second;
        if (Third is not null) yield return Third;
    }

    public double OffenseStars => Top ? AwayStars : HomeStars;
    public bool CanStarPitch => (Top ? HomeStars : AwayStars) >= 1;
    public bool CanStarSwing => OffenseStars >= 1;

    public PlayEvent Play(PitchCommand pitch, SwingCommand swing)
    {
        if (Over) throw new InvalidOperationException("game over");

        var inZone = AtBatResolver.PitchInZone(pitch, Pitcher.Stats.Pitch);
        SpendPitch(pitch);

        if (!swing.Swing)
        {
            var ev = FinishTake(pitch, swing, inZone);
            EndIfWalkOff();
            return ev;
        }

        SpendSwing(swing);

        var bat = Content.Bats.GetValueOrDefault(Top ? "furnace-club" : "harbor-lumber");
        var input = new AtBatInput(
            Pitcher, Batter, OnDeck, RunnersOn().ToList(),
            pitch.Type, pitch.Charge01 > 0.55, swing.Charge01 > 0.55,
            swing.TimingErrorFrames, pitch.Star, swing.Star, bat,
            Top ? HomeStamina : AwayStamina,
            swing.SprayAimDeg, inZone);

        var hit = _atBat.Resolve(input, Park, _rng);
        if (hit.Foul)
            return FinishFoul(pitch, swing, hit);
        if (!hit.InPlay)
            return FinishStrike(pitch, swing, hit, swinging: true);

        var field = _fielding.Resolve(hit, Park, Defense.Roster, Pitcher, _rng);
        var played = FinishInPlay(pitch, swing, hit, field);
        EndIfWalkOff();
        return played;
    }

    public PitchCommand CpuPitch()
    {
        var star = CanStarPitch && Pitcher.Captain && _rng.NextDouble() < 0.12;
        var type = _rng.NextDouble() < 0.28 ? "changeup" : _rng.NextDouble() < 0.5 ? "curve" : "fastball";
        var charge = _rng.NextDouble() < 0.3 ? 0.75 + _rng.NextDouble() * 0.25 : 0.1 + _rng.NextDouble() * 0.35;
        var err = Gauss() * (11 - Pitcher.Stats.Pitch) * 0.42;
        if ((Top ? HomeStamina : AwayStamina) < 25) err *= 1.6;
        return new PitchCommand(type, charge, err, star);
    }

    public SwingCommand CpuSwing(PitchCommand pitch, bool inZone)
    {
        var chase = !inZone && _rng.NextDouble() < 0.12;
        if (!inZone && !chase)
            return new SwingCommand(false, 0, 0, false);
        var star = CanStarSwing && Batter.Captain && inZone && _rng.NextDouble() < 0.12;
        var charge = _rng.NextDouble() < 0.35 ? 0.7 + _rng.NextDouble() * 0.3 : _rng.NextDouble() * 0.4;
        var err = Gauss() * (11 - Batter.Stats.Bat) * 0.62;
        if (!inZone) err += 4 * Math.Sign(err == 0 ? 1 : err);
        var spray = Gauss() * 12;
        return new SwingCommand(true, charge, err, star, spray);
    }

    public PlayEvent AutoPlay()
    {
        var pitch = CpuPitch();
        var inZone = AtBatResolver.PitchInZone(pitch, Pitcher.Stats.Pitch);
        var swing = CpuSwing(pitch, inZone);
        return Play(pitch, swing);
    }

    public void AutoPlayGame()
    {
        var guard = 0;
        while (!Over && guard++ < 2000)
            AutoPlay();
    }

    public (Character Who, int Points, string Why) Mvp()
    {
        if (_mvp.Count == 0)
            return (Home.Captain, 0, "showed up");
        var id = _mvp.OrderByDescending(kv => kv.Value).First().Key;
        var who = Away.Roster.Concat(Home.Roster).First(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        var pts = _mvp[id];
        var why = pts >= 8 ? "took over the diamond" : pts >= 4 ? "kept the line moving" : "did the little things";
        return (who, pts, why);
    }

    public string BoxLine() =>
        $"G{Away.Name} {AwayScore}  {Home.Name} {HomeScore}  {(Over ? "F" : (Top ? "T" : "B") + Inning)}  {Outs} out";

    PlayEvent FinishTake(PitchCommand pitch, SwingCommand swing, bool inZone)
    {
        var empty = EmptyHit(inZone);
        if (inZone)
        {
            if (Strikes >= 2)
                return FinishStrike(pitch, swing, empty, swinging: false);
            Strikes++;
            return Emit(PlayKind.TakeStrike, pitch, swing, empty, $"Strike {Strikes} looking.", 0, []);
        }
        Balls++;
        if (Balls >= 4)
            return FinishWalk(pitch, swing, empty);
        return Emit(PlayKind.TakeBall, pitch, swing, empty, $"Ball {Balls}.", 0, []);
    }

    PlayEvent FinishFoul(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        if (Strikes < 2) Strikes++;
        AddMvp(Batter.Id, 0);
        return Emit(PlayKind.Foul, pitch, swing, hit, "Foul.", 0, [], furnace: hit.StarSwingUsed == "furnace", heat: hit.StarPitchUsed == "heatball");
    }

    PlayEvent FinishStrike(PitchCommand pitch, SwingCommand swing, AtBatResult hit, bool swinging)
    {
        Strikes++;
        if (Strikes < 3)
        {
            var cap = swinging ? $"Strike {Strikes}." : $"Strike {Strikes} looking.";
            return Emit(swinging ? PlayKind.SwingMiss : PlayKind.TakeStrike, pitch, swing, hit, cap, 0, []);
        }
        AddMvp(Pitcher.Id, 2);
        AddStars(defense: true, 0.8);
        Outs++;
        var how = swinging ? "goes down swinging." : "is caught looking.";
        var ev = Emit(PlayKind.Strikeout, pitch, swing, hit, $"{Batter.Name} {how}", 0, []);
        NextBatter();
        CheckInning();
        return ev with { OutsAfter = Outs };
    }

    PlayEvent FinishWalk(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        var (runs, scorers) = Advance(Batter, walk: true);
        AddMvp(Batter.Id, 1 + runs);
        var ev = Emit(PlayKind.Walk, pitch, swing, hit, $"{Batter.Name} walks.", runs, scorers);
        NextBatter();
        return ev;
    }

    PlayEvent FinishInPlay(PitchCommand pitch, SwingCommand swing, AtBatResult hit, FieldingResult field)
    {
        var kind = field.Kind;
        var caption = "";
        var runs = 0;
        IReadOnlyList<string> scorers = [];

        switch (kind)
        {
            case PlayKind.HomeRun:
                (runs, scorers) = ClearTheBases(Batter);
                AddMvp(Batter.Id, 5 + runs);
                AddStars(defense: false, 1.0);
                caption = hit.StarSwingUsed == "furnace"
                    ? $"{Batter.Name} FURNACE - it's gone."
                    : $"{Batter.Name} goes deep.";
                NextBatter();
                break;
            case PlayKind.Triple:
                (runs, scorers) = AdvanceHit(Batter, 3);
                AddMvp(Batter.Id, 3 + runs);
                AddStars(defense: false, 0.8);
                caption = $"{Batter.Name} triples.";
                NextBatter();
                break;
            case PlayKind.Double:
                (runs, scorers) = AdvanceHit(Batter, 2);
                AddMvp(Batter.Id, 2 + runs);
                AddStars(defense: false, 0.8);
                caption = $"{Batter.Name} doubles.";
                NextBatter();
                break;
            case PlayKind.Single:
                (runs, scorers) = AdvanceHit(Batter, 1);
                AddMvp(Batter.Id, 2 + runs);
                AddStars(defense: false, 0.4);
                caption = field.Heatball ? $"{Batter.Name} - it drops! Heatball." : $"{Batter.Name} singles.";
                NextBatter();
                break;
            case PlayKind.FlyOut:
            case PlayKind.GroundOut:
                Outs++;
                AddMvp(field.Fielder?.Id ?? Pitcher.Id, 2);
                AddStars(defense: true, 0.35);
                if (kind == PlayKind.FlyOut && Third is not null && Outs < 3 && hit.CarryFt > 230)
                {
                    var tag = Third;
                    Third = null;
                    Score(tag);
                    runs = 1;
                    scorers = [tag.Name];
                    caption = $"{field.Fielder?.Name} reels it in. Sac fly.";
                }
                else
                    caption = kind == PlayKind.FlyOut
                        ? $"{field.Fielder?.Name} puts it away."
                        : $"{field.Fielder?.Name} to {field.Cutoff?.Name ?? "first"}.";
                NextBatter();
                CheckInning();
                break;
        }

        return Emit(kind, pitch, swing, hit, caption, runs, scorers,
            field.Fielder, field.Throw, field.HangTimeSec, field.LandingX, field.LandingZ,
            field.Heatball, field.Furnace);
    }

    PlayEvent Emit(
        PlayKind kind, PitchCommand pitch, SwingCommand swing, AtBatResult hit, string caption,
        int runs, IReadOnlyList<string> scorers,
        Character? fielder = null, ThrowResult? throwRes = null,
        double hang = 0, double lx = 0, double lz = 0,
        bool heat = false, bool furnace = false)
    {
        var ev = new PlayEvent(
            kind, hit, pitch, swing, Batter, Pitcher, fielder, throwRes, runs, scorers, caption,
            heat, furnace, hang, lx, lz, Outs, AwayScore, HomeScore);
        _log.Add(ev);
        return ev;
    }

    void NextBatter()
    {
        Balls = 0;
        Strikes = 0;
        if (Top) AwayBatter = (AwayBatter + 1) % AwayOrder.Count;
        else HomeBatter = (HomeBatter + 1) % HomeOrder.Count;
    }

    void EndIfWalkOff()
    {
        if (!Top && Inning >= Innings && HomeScore > AwayScore)
            Over = true;
    }

    void CheckInning()
    {
        if (Outs < 3) return;
        Outs = 0;
        Balls = 0;
        Strikes = 0;
        First = Second = Third = null;
        if (Top)
        {
            if (Inning >= Innings && HomeScore > AwayScore)
            {
                Over = true;
                return;
            }
            Top = false;
            return;
        }

        if (Inning >= Innings)
        {
            if (HomeScore != AwayScore || Inning >= Innings + 1)
            {
                Over = true;
                return;
            }
        }
        Inning++;
        Top = true;
        if (Inning > Innings + 1)
            Over = true;
    }

    (int Runs, IReadOnlyList<string> Scorers) Advance(Character batter, bool walk)
    {
        var scorers = new List<string>();
        if (walk)
        {
            if (First is not null && Second is not null && Third is not null)
            {
                Score(Third); scorers.Add(Third.Name); Third = Second; Second = First; First = batter;
            }
            else if (First is not null && Second is not null)
            { Third = Second; Second = First; First = batter; }
            else if (First is not null)
            { Second = First; First = batter; }
            else First = batter;
            return (scorers.Count, scorers);
        }
        return AdvanceHit(batter, 1);
    }

    (int Runs, IReadOnlyList<string> Scorers) AdvanceHit(Character batter, int bases)
    {
        var scorers = new List<string>();
        var bag = new[] { First, Second, Third };
        Character? n1 = null, n2 = null, n3 = null;
        void Place(Character c, int fromPlus)
        {
            var dest = fromPlus;
            if (dest >= 4) { Score(c); scorers.Add(c.Name); }
            else if (dest == 3) n3 = c;
            else if (dest == 2) n2 = c;
            else n1 = c;
        }
        if (Third is not null) Place(Third, 3 + bases);
        if (Second is not null) Place(Second, 2 + bases);
        if (First is not null) Place(First, 1 + bases);
        Place(batter, bases);
        First = n1; Second = n2; Third = n3;
        return (scorers.Count, scorers);
    }

    (int Runs, IReadOnlyList<string> Scorers) ClearTheBases(Character batter)
    {
        var scorers = new List<string>();
        if (Third is not null) { Score(Third); scorers.Add(Third.Name); }
        if (Second is not null) { Score(Second); scorers.Add(Second.Name); }
        if (First is not null) { Score(First); scorers.Add(First.Name); }
        Score(batter); scorers.Add(batter.Name);
        First = Second = Third = null;
        return (scorers.Count, scorers);
    }

    void Score(Character who)
    {
        if (Top) AwayScore++;
        else HomeScore++;
        AddMvp(who.Id, 1);
    }

    void SpendPitch(PitchCommand pitch)
    {
        var cost = 6 + (int)(pitch.Charge01 * 4) + (pitch.Star ? 12 : 0);
        if (Top) HomeStamina = Math.Max(0, HomeStamina - cost);
        else AwayStamina = Math.Max(0, AwayStamina - cost);
        if (pitch.Star)
        {
            if (Top) HomeStars = Math.Max(0, HomeStars - 1);
            else AwayStars = Math.Max(0, AwayStars - 1);
        }
    }

    void SpendSwing(SwingCommand swing)
    {
        if (!swing.Star) return;
        if (Top) AwayStars = Math.Max(0, AwayStars - 1);
        else HomeStars = Math.Max(0, HomeStars - 1);
    }

    void AddStars(bool defense, double amount)
    {
        if (defense)
        {
            if (Top) HomeStars = Math.Min(5, HomeStars + amount);
            else AwayStars = Math.Min(5, AwayStars + amount);
        }
        else
        {
            if (Top) AwayStars = Math.Min(5, AwayStars + amount);
            else HomeStars = Math.Min(5, HomeStars + amount);
        }
    }

    void AddMvp(string? id, int pts)
    {
        if (string.IsNullOrEmpty(id) || pts <= 0) return;
        _mvp[id] = _mvp.GetValueOrDefault(id) + pts;
    }

    AtBatResult EmptyHit(bool inZone) => new(
        ContactQuality.Miss, false, inZone, 0, 0, 0, false, false, null, null, 0, false, inZone);

    static IReadOnlyList<Character> BattingOrder(Team team)
    {
        var rest = team.Roster.Where(c => c.Id != team.Captain.Id).ToList();
        var order = new List<Character>();
        order.AddRange(rest.Take(3));
        order.Add(team.Captain);
        order.AddRange(rest.Skip(3));
        return order;
    }

    double Gauss()
    {
        var u1 = 1.0 - _rng.NextDouble();
        var u2 = _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
