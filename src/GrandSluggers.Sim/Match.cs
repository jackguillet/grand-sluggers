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
    public bool Night { get; }
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
    RunnerState? _firstRun, _secondRun, _thirdRun;
    public int AwayBatter { get; private set; }
    public int HomeBatter { get; private set; }
    public double AwayStars { get; private set; }
    public double HomeStars { get; private set; }
    public int AwayStamina { get; private set; } = 100;
    public int HomeStamina { get; private set; } = 100;
    Character _homePitcher;
    Character _awayPitcher;
    public bool Over { get; private set; }
    public IReadOnlyList<PlayEvent> Log => _log;
    public ChemistryTable Chemistry => Content.Chemistry;

    public Match(ContentCatalog content, Team away, Team home, Park park, int innings = DefaultInnings, int seed = 1, bool night = false)
    {
        Content = content;
        Away = away;
        Home = home;
        Park = park;
        Night = night;
        Innings = innings;
        _rng = new Random(seed);
        _atBat = new AtBatResolver(content.Chemistry);
        _fielding = new FieldingResolver(content.Chemistry);
        AwayOrder = away.BattingOrder;
        HomeOrder = home.BattingOrder;
        AwayStars = content.Chemistry.StartingStars(away);
        HomeStars = content.Chemistry.StartingStars(home);
        _homePitcher = home.Pitcher;
        _awayPitcher = away.Pitcher;
        HomeBat = GearMesh.SignatureBat(content, home.Captain.Id);
        AwayBat = GearMesh.SignatureBat(content, away.Captain.Id);
        HomeGlove = content.Gloves.GetValueOrDefault("web-back") ?? content.Gloves.Values.First();
        AwayGlove = content.Gloves.GetValueOrDefault("lucky-mitt") ?? content.Gloves.Values.First();
    }

    public BatItem HomeBat { get; private set; } = null!;
    public BatItem AwayBat { get; private set; } = null!;
    public GloveItem HomeGlove { get; private set; } = null!;
    public GloveItem AwayGlove { get; private set; } = null!;
    public BatItem OffenseBat => Top ? AwayBat : HomeBat;
    public GloveItem DefenseGlove => Top ? HomeGlove : AwayGlove;

    public void CycleBat(bool home)
    {
        var ids = Content.Bats.Keys.OrderBy(k => k).ToList();
        if (ids.Count == 0) return;
        if (home)
            HomeBat = Content.Bats[ids[(ids.IndexOf(HomeBat.Id) + 1) % ids.Count]];
        else
            AwayBat = Content.Bats[ids[(ids.IndexOf(AwayBat.Id) + 1) % ids.Count]];
    }

    public void CycleGlove(bool home)
    {
        var ids = Content.Gloves.Keys.OrderBy(k => k).ToList();
        if (ids.Count == 0) return;
        if (home)
            HomeGlove = Content.Gloves[ids[(ids.IndexOf(HomeGlove.Id) + 1) % ids.Count]];
        else
            AwayGlove = Content.Gloves[ids[(ids.IndexOf(AwayGlove.Id) + 1) % ids.Count]];
    }

    public static Match Slice(ContentCatalog content, int innings = DefaultInnings, int seed = 1, string parkId = "harbor-diamond", bool night = false)
    {
        if (!content.Parks.TryGetValue(parkId, out var park))
            park = content.Parks["harbor-diamond"];
        return new Match(content, PresetTeams.EmberCourt(content), PresetTeams.SparkAllStars(content), park, innings, seed, night);
    }

    public static Match Exhibition(
        ContentCatalog content,
        string homeCaptain = "rio",
        string awayCaptain = "ashlord",
        int innings = DefaultInnings,
        int seed = 1,
        string? parkId = null,
        bool night = false)
    {
        var (home, away) = PresetTeams.Pair(content, homeCaptain, awayCaptain);
        return Exhibition(content, home, away, innings, seed, parkId ?? PresetTeams.HomeParkId(homeCaptain), night);
    }

    public static Match Exhibition(
        ContentCatalog content,
        Team home,
        Team away,
        int innings = DefaultInnings,
        int seed = 1,
        string? parkId = null,
        bool night = false)
    {
        parkId ??= PresetTeams.HomeParkId(home.Captain.Id);
        if (!content.Parks.TryGetValue(parkId, out var park))
            park = content.Parks["harbor-diamond"];
        return new Match(content, away, home, park, innings, seed, night);
    }

    /// <summary>
    /// Still-gate only. Flip the top without playing three outs so Play can
    /// photograph batting SET / scoop / star without a pad grinding the half.
    /// </summary>
    public void SkipToHomeHalf()
    {
        if (Over || !Top) return;
        Outs = 0;
        Balls = 0;
        Strikes = 0;
        ClearBags();
        Top = false;
    }

    public void GiveOffenseStars(double n)
    {
        n = Math.Clamp(n, 0, 5);
        if (Top) AwayStars = Math.Max(AwayStars, n);
        else HomeStars = Math.Max(HomeStars, n);
    }

    public Team Offense => Top ? Away : Home;
    public Team Defense => Top ? Home : Away;
    public Character Batter => (Top ? AwayOrder : HomeOrder)[Top ? AwayBatter : HomeBatter];
    public Character Pitcher => Top ? _homePitcher : _awayPitcher;
    public int PitcherStamina => Top ? HomeStamina : AwayStamina;
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
    public double DefenseStars => Top ? HomeStars : AwayStars;
    public bool StealOn { get; private set; }
    /// <summary>Furthest occupied bag. Control and steal apply to this runner.</summary>
    public Character? LeadRunner => Third ?? Second ?? First;
    public int LeadBag => Third is not null ? 3 : Second is not null ? 2 : First is not null ? 1 : 0;
    public RunnerState? LeadState => RunnerAt(LeadBag);
    public double Lead01 => LeadState?.Lead01 ?? 0;
    public bool StealAttempt => LeadState?.StealAttempt ?? false;
    public bool Returning => LeadState?.Returning ?? false;
    public bool Sliding => LeadState?.Sliding ?? false;
    public bool CanSteal =>
        !Over && Outs < 3 &&
        ((LeadBag == 1 && Second is null) || (LeadBag == 2 && Third is null));

    public RunnerState? RunnerAt(int bag) => bag switch
    {
        1 => _firstRun,
        2 => _secondRun,
        3 => _thirdRun,
        _ => null
    };

    public bool TakeLead(double delta = 0.25)
    {
        var state = LeadState;
        if (state is null || Over || Outs >= 3) return false;
        state.TakeLead(delta);
        return true;
    }

    public bool ReturnToBag(double delta = 0.25)
    {
        var state = LeadState;
        if (state is null) return false;
        state.ReturnToBag(delta);
        StealOn = false;
        return true;
    }

    public bool StartSteal()
    {
        if (!CanSteal) return false;
        var state = LeadState;
        if (state is null) return false;
        state.StartSteal();
        StealOn = true;
        return true;
    }

    public bool Slide()
    {
        var state = LeadState;
        if (state is null) return false;
        state.Slide();
        return true;
    }

    public int StarCost(Character who, Character teamCaptain) =>
        who.Captain && !who.Id.Equals(teamCaptain.Id, StringComparison.OrdinalIgnoreCase) ? 2 : 1;

    public int PitchStarCost => StarCost(Pitcher, Defense.Captain);
    public int SwingStarCost => StarCost(Batter, Offense.Captain);
    public bool CanStarPitch => DefenseStars >= PitchStarCost;
    public bool CanStarSwing => OffenseStars >= SwingStarCost;

    public bool ToggleSteal()
    {
        if (!CanSteal)
        {
            StealOn = false;
            LeadState?.CancelSteal();
            return false;
        }
        if (StealOn)
        {
            StealOn = false;
            LeadState?.CancelSteal();
            return false;
        }
        return StartSteal();
    }

    public PlayEvent Play(PitchCommand pitch, SwingCommand swing, string? item = null)
    {
        if (!BeginAtBat(pitch, swing, out var hit, out var finished))
            return finished!;
        var field = _fielding.Resolve(hit, Park, Defense.Roster, Pitcher, _rng, DefenseGlove, night: Night);
        field = ApplyOffenseItem(hit, field, item);
        return FinishAtBat(pitch, swing, hit, field);
    }

    public bool BeginAtBat(PitchCommand pitch, SwingCommand swing, out AtBatResult hit, out PlayEvent? finished)
    {
        hit = EmptyHit(true);
        finished = null;
        if (Over) throw new InvalidOperationException("game over");

        var inZone = AtBatResolver.PitchInZone(pitch, Pitcher.Stats.Pitch);
        SpendPitch(pitch);

        if (!swing.Swing)
        {
            finished = FinishTake(pitch, swing, inZone);
            EndIfWalkOff();
            return false;
        }

        SpendSwing(swing);

        var bat = OffenseBat;
        var input = new AtBatInput(
            Pitcher, Batter, OnDeck, RunnersOn().ToList(),
            pitch.Type, pitch.Charge01 > 0.55, swing.Charge01 > 0.55,
            swing.TimingErrorFrames, pitch.Star, swing.Star, bat,
            Top ? HomeStamina : AwayStamina,
            swing.SprayAimDeg, inZone, swing.Bunt, swing.LaunchAim);

        hit = _atBat.Resolve(input, Park, _rng, Night);
        if (hit.Foul)
        {
            finished = FinishFoul(pitch, swing, hit);
            return false;
        }
        if (!hit.InPlay)
        {
            finished = FinishStrike(pitch, swing, hit, swinging: true);
            return false;
        }
        return true;
    }

    public PlayEvent FinishAtBat(PitchCommand pitch, SwingCommand swing, AtBatResult hit, FieldingResult field)
    {
        var played = FinishInPlay(pitch, swing, hit, field);
        EndIfWalkOff();
        return played;
    }

    public FieldingPreview PreviewHit(AtBatResult hit) =>
        _fielding.Preview(hit, Park, Defense.Roster, Pitcher, _rng, Night);

    public FieldingResult ResolveFielding(AtBatResult hit, FieldingPreview? preview = null) =>
        _fielding.Resolve(hit, Park, Defense.Roster, Pitcher, _rng, DefenseGlove, preview, Night);

    public bool SwapPitcher()
    {
        var team = Defense;
        var cur = Pitcher;
        var next = team.Roster
            .Where(c => !c.Id.Equals(cur.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Stats.Pitch)
            .FirstOrDefault();
        if (next is null) return false;
        if (Top)
        {
            _homePitcher = next;
            HomeStamina = Math.Min(100, HomeStamina + 35);
        }
        else
        {
            _awayPitcher = next;
            AwayStamina = Math.Min(100, AwayStamina + 35);
        }
        return true;
    }

    public ThrowResult ThrowBetween(Character from, Character to) =>
        FieldAbilities.ApplyThrow(from, Content.Chemistry.FieldingThrow(from, to, _rng));

    public FieldingResult ApplyOffenseItem(AtBatResult hit, FieldingResult field, string? playerItem, Character? target = null)
    {
        if (!hit.ChemistryItemOffered) return field;
        if (playerItem == "") return field;
        var who = target ?? field.Fielder;
        if (!string.IsNullOrEmpty(playerItem))
            return ThrowItem(field, playerItem, who);
        if (_rng.NextDouble() < 0.4)
            return ThrowItem(field, ErrorItems.Pick(_rng), who);
        return field;
    }

    /// <summary>
    /// After contact: throw a banana / rocket / POW at a fielder. Empty or unknown item is a no-op.
    /// </summary>
    public FieldingResult ThrowItem(FieldingResult field, string? item, Character? target)
    {
        if (string.IsNullOrEmpty(item)) return field;
        return ErrorItems.Apply(field, item, _rng, target);
    }

    public PitchCommand CpuPitch()
    {
        var star = CanStarPitch && _rng.NextDouble() < (Pitcher.Captain ? 0.14 : 0.08);
        var type = _rng.NextDouble() < 0.22 ? "changeup" : _rng.NextDouble() < 0.22 ? "slider" : _rng.NextDouble() < 0.5 ? "curve" : "fastball";
        var charge = _rng.NextDouble() < 0.3 ? 0.75 + _rng.NextDouble() * 0.25 : 0.1 + _rng.NextDouble() * 0.35;
        var err = Gauss() * (11 - Pitcher.Stats.Pitch) * 0.42;
        if ((Top ? HomeStamina : AwayStamina) < 25) err *= 1.6;
        var scatter = (11 - Pitcher.Stats.Pitch) * 0.055;
        var aimX = Gauss() * scatter;
        var aimY = Gauss() * scatter * 0.85;
        if ((Top ? HomeStamina : AwayStamina) < 25)
        {
            aimX *= 1.6;
            aimY *= 1.6;
        }
        return new PitchCommand(type, charge, err, star, aimX, aimY);
    }

    public SwingCommand CpuSwing(PitchCommand pitch, bool inZone)
    {
        if (CanSteal && Batter.Stats.Run >= 7 && _rng.NextDouble() < 0.16)
        {
            StartSteal();
            TakeLead(0.45 + _rng.NextDouble() * 0.35);
        }
        var chase = !inZone && _rng.NextDouble() < 0.12;
        if (!inZone && !chase)
            return new SwingCommand(false, 0, 0, false);
        var star = CanStarSwing && inZone && _rng.NextDouble() < (Batter.Captain ? 0.14 : 0.08);
        var charge = _rng.NextDouble() < 0.35 ? 0.7 + _rng.NextDouble() * 0.3 : _rng.NextDouble() * 0.4;
        var err = Gauss() * (11 - Batter.Stats.Bat) * 0.62;
        if (!inZone) err += 4 * Math.Sign(err == 0 ? 1 : err);
        var spray = Gauss() * 12;
        var launchAim = Gauss() * 0.45;
        return new SwingCommand(true, charge, err, star, spray, LaunchAim: launchAim);
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
            return AfterPitch(Emit(PlayKind.TakeStrike, pitch, swing, empty, $"Strike {Strikes} looking.", 0, []));
        }
        Balls++;
        if (Balls >= 4)
            return FinishWalk(pitch, swing, empty);
        return AfterPitch(Emit(PlayKind.TakeBall, pitch, swing, empty, $"Ball {Balls}.", 0, []));
    }

    PlayEvent FinishFoul(PitchCommand pitch, SwingCommand swing, AtBatResult hit)
    {
        if (Strikes < 2) Strikes++;
        AddMvp(Batter.Id, 0);
        StealOn = false;
        return Emit(PlayKind.Foul, pitch, swing, hit, "Foul.", 0, [], furnace: hit.StarSwingUsed is "furnace" or "heat-swing", heat: hit.StarPitchUsed == "heatball");
    }

    PlayEvent FinishStrike(PitchCommand pitch, SwingCommand swing, AtBatResult hit, bool swinging)
    {
        Strikes++;
        if (Strikes < 3)
        {
            var cap = swinging ? $"Strike {Strikes}." : $"Strike {Strikes} looking.";
            return AfterPitch(Emit(swinging ? PlayKind.SwingMiss : PlayKind.TakeStrike, pitch, swing, hit, cap, 0, []));
        }
        StealOn = false;
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
        StealOn = false;
        var (runs, scorers) = Advance(Batter, walk: true);
        AddMvp(Batter.Id, 1 + runs);
        var ev = Emit(PlayKind.Walk, pitch, swing, hit, $"{Batter.Name} walks.", runs, scorers);
        NextBatter();
        return ev;
    }

    PlayEvent FinishInPlay(PitchCommand pitch, SwingCommand swing, AtBatResult hit, FieldingResult field)
    {
        StealOn = false;
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
                caption = hit.StarSwingUsed is "furnace" or "heat-swing"
                    ? $"{Batter.Name} {hit.StarSwingUsed!.ToUpperInvariant()} - it's gone."
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
                caption = field.Warped ? $"{Batter.Name} - it hopped a {ParkHazards.WarpName(Park)}!"
                    : field.Heatball ? $"{Batter.Name} - it drops! Heatball."
                    : $"{Batter.Name} singles.";
                NextBatter();
                break;
            case PlayKind.FlyOut:
            case PlayKind.GroundOut:
                if (kind == PlayKind.GroundOut && First is not null)
                {
                    SetBag(1, null);
                    Outs++;
                    AddMvp(field.Fielder?.Id ?? Pitcher.Id, 2);
                    AddStars(defense: true, 0.4);
                    if (Outs < 3 && !InPlay.BatterBeatsThrow(Batter, hit, field))
                    {
                        Outs++;
                        caption = $"{field.Fielder?.Name} turns two.";
                        NextBatter();
                    }
                    else if (Outs < 3)
                    {
                        (runs, scorers) = AdvanceHit(Batter, 1);
                        caption = $"Force at second. {Batter.Name} in at first.";
                    }
                    else
                    {
                        caption = $"{field.Fielder?.Name} forces the runner.";
                        NextBatter();
                    }
                    CheckInning();
                    break;
                }
                if (kind == PlayKind.GroundOut && First is null && (Second is not null || Third is not null))
                {
                    var tagBag = InPlay.TagBag(Second is not null, Third is not null);
                    var fromBag = tagBag == 4 ? 3 : 2;
                    var runner = tagBag == 4 ? Third! : Second!;
                    var beats = InPlay.RunnerBeatsTag(runner, hit, field, tagBag);
                    if (!beats)
                    {
                        SetBag(fromBag, null);
                        Outs++;
                        AddMvp(field.Fielder?.Id ?? Pitcher.Id, 2);
                        AddStars(defense: true, 0.4);
                        caption = $"{field.Fielder?.Name} tags {runner.Name}.";
                        if (Outs < 3)
                            SetBag(1, Batter);
                        NextBatter();
                    }
                    else
                    {
                        if (tagBag == 4)
                        {
                            Score(runner);
                            SetBag(3, null);
                            runs = 1;
                            scorers = [runner.Name];
                            caption = $"{runner.Name} beats the tag. {Batter.Name} in at first.";
                        }
                        else
                        {
                            SetBag(3, runner);
                            SetBag(2, null);
                            caption = $"{runner.Name} in at third. {Batter.Name} in at first.";
                        }
                        SetBag(1, Batter);
                        NextBatter();
                    }
                    CheckInning();
                    break;
                }
                if (kind == PlayKind.GroundOut && InPlay.BatterBeatsThrow(Batter, hit, field))
                {
                    kind = PlayKind.Single;
                    goto case PlayKind.Single;
                }
                Outs++;
                AddMvp(field.Fielder?.Id ?? Pitcher.Id, 2);
                AddStars(defense: true, 0.35);
                if (kind == PlayKind.FlyOut && Third is not null && Outs < 3 && hit.CarryFt > 230)
                {
                    var tag = Third;
                    SetBag(3, null);
                    Score(tag);
                    runs = 1;
                    scorers = [tag.Name];
                    caption = $"{field.Fielder?.Name} reels it in. Sac fly.";
                }
                else
                    caption = kind == PlayKind.FlyOut && field.Buddy is not null && FieldingResolver.HomeRunLikely(hit, Park)
                        ? $"{field.Fielder?.Name} + {field.Buddy.Name} BUDDY JUMP!"
                        : kind == PlayKind.FlyOut && field.Fielder is { } wall
                          && ParkHazards.CanClamber(Park, wall) && hit.CarryFt > 260
                            ? $"{wall.Name} CLAMBERS the wall!"
                        : kind == PlayKind.FlyOut && field.Fielder?.FieldAbility == "super-jump" && hit.CarryFt > 250
                            ? $"{field.Fielder.Name} SUPER JUMP!"
                        : field.Chomped
                            ? "A chomper ate it!"
                        : kind == PlayKind.FlyOut
                            ? $"{field.Fielder?.Name} puts it away."
                            : field.Warped
                                ? $"{ParkHazards.WarpName(Park)}! {field.Fielder?.Name} is looking the wrong way."
                                : field.Throw is { SpeedMul: > 1.2 }
                                    ? $"{field.Fielder?.Name} lasers it to {field.Cutoff?.Name ?? "the bag"}."
                                    : $"{field.Fielder?.Name} to {field.Cutoff?.Name ?? "first"}.";
                NextBatter();
                CheckInning();
                break;
        }

        if (ParkHazards.HitStarSign(Park, field.LandingX, field.LandingZ) &&
            kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun or PlayKind.FlyOut)
        {
            AddStars(defense: false, 1);
            caption += "  Billboard STAR!";
        }

        if (field.Item is { } item)
        {
            caption += item switch
            {
                "banana" => "  Banana slip!",
                "rocket" => "  Rocket daze!",
                "pow" => "  POW!",
                _ => $"  {item}!"
            };
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
        ClearBags();
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
            var f = First;
            var s = Second;
            var t = Third;
            if (f is not null && s is not null && t is not null)
            {
                Score(t); scorers.Add(t.Name); SetBag(3, s); SetBag(2, f); SetBag(1, batter);
            }
            else if (f is not null && s is not null)
            { SetBag(3, s); SetBag(2, f); SetBag(1, batter); }
            else if (f is not null)
            { SetBag(2, f); SetBag(1, batter); }
            else SetBag(1, batter);
            return (scorers.Count, scorers);
        }
        return AdvanceHit(batter, 1);
    }

    (int Runs, IReadOnlyList<string> Scorers) AdvanceHit(Character batter, int bases)
    {
        var scorers = new List<string>();
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
        SetBag(1, n1); SetBag(2, n2); SetBag(3, n3);
        return (scorers.Count, scorers);
    }

    (int Runs, IReadOnlyList<string> Scorers) ClearTheBases(Character batter)
    {
        var scorers = new List<string>();
        if (Third is not null) { Score(Third); scorers.Add(Third.Name); }
        if (Second is not null) { Score(Second); scorers.Add(Second.Name); }
        if (First is not null) { Score(First); scorers.Add(First.Name); }
        Score(batter); scorers.Add(batter.Name);
        ClearBags();
        return (scorers.Count, scorers);
    }

    void SetBag(int bag, Character? who)
    {
        switch (bag)
        {
            case 1:
                First = who;
                _firstRun = who is null ? null : new RunnerState(who);
                break;
            case 2:
                Second = who;
                _secondRun = who is null ? null : new RunnerState(who);
                break;
            case 3:
                Third = who;
                _thirdRun = who is null ? null : new RunnerState(who);
                break;
        }
    }

    void ClearBags()
    {
        First = Second = Third = null;
        _firstRun = _secondRun = _thirdRun = null;
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
            var starsCost = PitchStarCost;
            if (Top) HomeStars = Math.Max(0, HomeStars - starsCost);
            else AwayStars = Math.Max(0, AwayStars - starsCost);
        }
    }

    void SpendSwing(SwingCommand swing)
    {
        if (!swing.Star) return;
        var cost = SwingStarCost;
        if (Top) AwayStars = Math.Max(0, AwayStars - cost);
        else HomeStars = Math.Max(0, HomeStars - cost);
    }

    PlayEvent AfterPitch(PlayEvent ev) => StealOn ? ResolveSteal(ev) : ResolvePickoff(ev);

    PlayEvent ResolveSteal(PlayEvent ev)
    {
        StealOn = false;
        if (Over || Outs >= 3)
            return ev;

        Character? runner = null;
        var fromBag = 0;
        if (LeadBag == 2 && Third is null)
        {
            runner = Second;
            fromBag = 2;
        }
        else if (LeadBag == 1 && Second is null)
        {
            runner = First;
            fromBag = 1;
        }
        if (runner is null)
            return ev;

        var map = FieldingResolver.Assign(Defense.Roster, Pitcher);
        var catcher = map.GetValueOrDefault("C") ?? Pitcher;
        var gun = catcher.Stats.Field + 2.0;
        var lead = RunnerAt(fromBag)?.Lead01 ?? 0;
        var jump = runner.Stats.Run + Gauss() * 1.6 + lead * 3.4;
        var thr = ThrowBetween(catcher, runner);
        if (thr.Error) gun -= 4;
        gun += (thr.SpeedMul - 1) * 4;

        PlayEvent result;
        var toThird = fromBag == 2;
        if (jump > gun)
        {
            if (toThird) { SetBag(3, runner); SetBag(2, null); }
            else { SetBag(2, runner); SetBag(1, null); }
            AddMvp(runner.Id, 2);
            AddStars(defense: false, 0.35);
            result = ev with
            {
                Kind = PlayKind.StolenBase,
                Caption = ev.Caption + $"  {runner.Name} steals {(toThird ? "third" : "second")}.",
                Fielder = catcher,
                Throw = thr
            };
        }
        else
        {
            SetBag(fromBag, null);
            Outs++;
            AddMvp(catcher.Id, 2);
            AddStars(defense: true, 0.4);
            result = ev with
            {
                Kind = PlayKind.CaughtStealing,
                Caption = $"{runner.Name} caught stealing.",
                Fielder = catcher,
                Throw = thr,
                OutsAfter = Outs
            };
            CheckInning();
            result = result with { OutsAfter = Outs };
        }

        if (_log.Count > 0)
            _log[^1] = result;
        return result;
    }

    PlayEvent ResolvePickoff(PlayEvent ev)
    {
        if (Over || Outs >= 3)
            return ev;
        var bag = LeadBag;
        var state = LeadState;
        var runner = LeadRunner;
        if (bag == 0 || state is null || runner is null || state.Lead01 < 0.2)
            return ev;

        var risk = state.Lead01 * 0.42;
        if (state.Returning) risk *= 0.18;
        var map = FieldingResolver.Assign(Defense.Roster, Pitcher);
        var catcher = map.GetValueOrDefault("C") ?? Pitcher;
        risk += (catcher.Stats.Field - runner.Stats.Run) * 0.02;
        risk = Math.Clamp(risk, 0, 0.72);
        if (_rng.NextDouble() >= risk)
            return ev;

        var thr = ThrowBetween(Pitcher, catcher);
        SetBag(bag, null);
        Outs++;
        AddMvp(catcher.Id, 2);
        AddStars(defense: true, 0.4);
        var result = ev with
        {
            Kind = PlayKind.CaughtStealing,
            Caption = $"{runner.Name} picked off.",
            Fielder = Pitcher,
            Throw = thr,
            OutsAfter = Outs
        };
        CheckInning();
        result = result with { OutsAfter = Outs };
        if (_log.Count > 0)
            _log[^1] = result;
        return result;
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

    double Gauss()
    {
        var u1 = 1.0 - _rng.NextDouble();
        var u2 = _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
