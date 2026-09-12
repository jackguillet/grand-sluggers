namespace GrandSluggers.Sim;

/// <summary>One frame of a seat's pad, as the live ball sees it. Unity translates its pad; the harness scripts it.</summary>
public sealed record LivePadInput(
    double StickX = 0,
    double StickY = 0,
    bool SouthDown = false,
    bool WestDown = false,
    bool EastDown = false,
    bool EastHeld = false,
    bool Cutoff = false,
    bool Swap = false,
    bool Item = false,
    bool Attack = false,
    int KeysBag = 0,
    int StickBag = 0,
    int ArrowBag = 0,
    /// <summary>LB held on the offense pad: every runner goes (§9.3); before the catch, tag and go (§9.5).</summary>
    bool AllAdvance = false,
    /// <summary>RB held on the offense pad: every runner comes back. A tap while they run halts them.</summary>
    bool AllReturn = false,
    /// <summary>Both shoulders: halt every runner; with the stick toward a bag, only that runner.</summary>
    bool Freeze = false)
{
    public static LivePadInput Dead { get; } = new();

    public double StickMag => Math.Abs(StickX) + Math.Abs(StickY);
}

/// <summary>Who holds which seat for this live ball. Baseball does not branch by seat; ownership of a press does.</summary>
public sealed record LiveSeats(bool HumanBats, bool HumanPitches, bool PlayerMustField, bool Versus)
{
    public static LiveSeats CpuOnly { get; } = new(false, false, false, false);

    /// <summary>
    /// Seat ownership for a half is a function of (half, home/away, seated controllers) and
    /// nothing else (spec §0.4). The batting human never owns a glove: in 1P vs CPU the whole
    /// defense is CPU that half; in 1v1 the other controller sits it. Training drills that
    /// force the player onto the glove say so through <see cref="PlayerMustField"/>, not here.
    /// </summary>
    public static LiveSeats For(Seats seats, bool top) =>
        new(seats.HumanBats(top), seats.HumanPitches(top), PlayerMustField: false, seats.BothHuman);

    /// <summary>A human sits the defense this half: their pad owns the glove and the throw.</summary>
    public bool HumanFields => PlayerMustField || Versus || HumanPitches;

    /// <summary>A human sits the offense this half: their pad owns the runners.</summary>
    public bool HumanRuns => HumanBats;

    /// <summary>Human is on defense: they throw. CPU may still run and catch on a dead stick.</summary>
    public bool HumanOwnsThrow => FieldAssist.HumanOwnsThrow(HumanFields);

    /// <summary>
    /// A pad reaches the gloves only through a seat that owns them. A press from any other
    /// controller is dead here, so no glove is ever taken, and no throw ever waits, on a pad
    /// that is not on defense.
    /// </summary>
    public LivePadInput OwnedFieldPad(LivePadInput? pad) => HumanFields ? pad ?? LivePadInput.Dead : LivePadInput.Dead;

    /// <summary>Same boundary for the runners: only the offense seat dashes, mashes, or sends.</summary>
    public LivePadInput OwnedRunPad(LivePadInput? pad) => HumanRuns ? pad ?? LivePadInput.Dead : LivePadInput.Dead;
}

/// <summary>The fair / foul call of a live batted ball (§5.6).</summary>
public enum FairFoulCall
{
    /// <summary>Nobody has touched it: the untouched path's verdict stands (<see cref="BattedBall.Foul"/>).</summary>
    Undecided,
    /// <summary>First touched on the ground in fair territory.</summary>
    Fair,
    /// <summary>First touched on the ground in foul territory: dead.</summary>
    Foul,
    /// <summary>Caught in the air: an out wherever it was (§7.11).</summary>
    Caught
}

/// <summary>A presentation cue the live ball raised this frame. Unity plays it; nothing decides by it.</summary>
public enum LiveEvent
{
    Glove,
    ThrowPop,
    StampSafe,
    CloseIcon,
    BuddyJump,
    ItemSmashed,
    /// <summary>The ball met the outfield fence below its top: the carom (§7.9).</summary>
    WallCarom,
    /// <summary>A throw missed its cover and skipped past (§8.5): the ERROR tell on the thrower.</summary>
    ThrowSailed,
    /// <summary>The glove fumbled the ball (§8.6): it is loose on the ground.</summary>
    Bobble
}

/// <summary>
/// The live ball between contact and Time: gloves, catches, throws, the relay chain, the
/// close-play race, the bobble, and the steal phase (spec §0.1, A.7). Unity supplies the
/// pads once per frame and draws the state; every decision is made here from positions,
/// clocks, and <see cref="Match"/>'s seeded random stream. Nothing here is a roll on the
/// outcome: a glove reaches the ball or it does not, a throw lands inside the cover's reach or
/// it does not, and the runner bodies race the one throw clock (§8.3, §8.5, §8.8).
/// </summary>
public sealed partial class LivePlaySystem
{
    readonly Dictionary<string, (double X, double Z)> _fielders = new();
    readonly Dictionary<string, double> _readyAt = new(StringComparer.OrdinalIgnoreCase);
    readonly List<LiveEvent> _events = [];
    bool _gloved;
    bool _recoilArmed;
    bool _wallCued;
    bool _chomped;
    FairFoulCall _call;
    double _closePlayT;
    double _closeOffAt = -1;
    double _closeDefAt = -1;
    double _cpuGunAt;

    // The facts of this play the result carries (§8.6, §8.5).
    bool _bobbled;
    bool _sailed;
    bool _dropRolled;
    bool _dropped;
    Character? _firstGlove;

    // A ball on the ground in nobody's glove and off its batted path: a fumble, an overthrow, a drop at an uncovered bag.
    bool _loose;
    double _looseVX;
    double _looseVZ;
    double _looseRestAt = -1;

    // The throw in flight: who threw it, who receives it, who backs it up.
    string _throwerPos = "";
    string _cutoffPos = "";
    (double X, double Z)? _cutoffSpot;
    string _backupPos = "";
    (double X, double Z) _backupSpot;
    double _lobT;
    int _relayBag;

    // The CPU glove's decision clock (§8.8): the throw waits for the reaction, then the table runs once per possession.
    double _cpuThrowAt = -1;
    bool _cpuDecided;

    // Items (§12): one body kept off the ball for a beat, or every ball on the dirt hopping.
    string _foilPos = "";
    double _foilT;
    double _powT;
    double _itemLandAt = -1;

    // ---- What Unity draws. Read after every Apply; never written from outside. ----

    public PitchCommand? Pitch { get; private set; }
    public SwingCommand? Swing { get; private set; }
    public AtBatResult? Hit { get; private set; }
    public FieldingPreview? Preview { get; private set; }
    /// <summary>What the flight decided alone (a homer, a foul, a chomp) or <see cref="PlayKind.InPlay"/>; the item thrown at the glove rides on it.</summary>
    public FieldingResult? Field { get; private set; }
    /// <summary>The fielding result the last completed play was scored from.</summary>
    public FieldingResult? LastFieldResult { get; private set; }
    public IReadOnlyList<Sample>? Path { get; private set; }
    /// <summary>The batted ball's facts (§6.2): class, landing, wall, fence, and the untouched fair / foul verdict.</summary>
    public BattedBall? Ball { get; private set; }
    public LiveSeats Seats { get; private set; } = LiveSeats.CpuOnly;
    public bool FlightDone { get; private set; }
    /// <summary>The fair / foul call so far (§5.6): the untouched path's verdict until a glove touches the ball.</summary>
    public FairFoulCall Call => _call;
    /// <summary>Foul as things stand: called foul on a touch, or untouched on a path the flight says is foul.</summary>
    public bool FoulNow => _call == FairFoulCall.Foul || (_call == FairFoulCall.Undecided && Ball is { Foul: true });
    public bool StealPhase { get; private set; }

    public double BallX { get; private set; }
    public double BallY { get; private set; }
    public double BallZ { get; private set; }
    public IReadOnlyDictionary<string, (double X, double Z)> Fielders => _fielders;
    public string GlovePos { get; private set; } = "P";
    public double GloveX { get; private set; }
    public double GloveZ { get; private set; }
    public bool PlayerFielding { get; private set; }
    public bool Caught { get; private set; }
    public bool Buddy { get; private set; }
    public double ThrowT { get; private set; }
    public double ThrowDur { get; private set; }
    public (double X, double Y, double Z) ThrowFrom { get; private set; }
    public (double X, double Y, double Z) ThrowTo { get; private set; }
    public int ThrowBag { get; private set; }
    public ThrowResult? ArmedThrow { get; private set; }
    public Character? ArmedCut { get; private set; }
    public string CoverPos { get; private set; } = "";
    public string ThrowFromPos { get; private set; } = "";
    public string SwitchPos { get; private set; } = "";
    public string BuddyPos { get; private set; } = "";
    public bool BuddyWindow { get; private set; }
    public double DiveT { get; private set; }
    public double JumpT { get; private set; }
    public double SwapLock { get; private set; }
    public double RecoilT { get; private set; }
    public bool Bobbling { get; private set; }
    public bool PlayerBobble { get; private set; }
    public bool CatchDive { get; private set; }
    public bool CatchJump { get; private set; }
    public bool InClosePlay { get; private set; }
    public int CloseBag { get; private set; }
    public bool CloseIcon { get; private set; }
    public bool AwaitingRelay { get; private set; }
    public double Dash01 { get; private set; }
    public double StealT { get; private set; }
    public double StealTagT { get; private set; } = -1;
    public double StealRelease { get; private set; }
    public PlayEvent? StealPitch { get; private set; }
    /// <summary>The ball is on the ground in nobody's glove, off its batted path (a fumble, an overthrow, a drop).</summary>
    public bool LooseBall => _loose;
    /// <summary>A throw is hanging at an uncovered bag, waiting for the cover (§8.5).</summary>
    public bool Lobbing => Throwing && _lobT > 0;
    /// <summary>A throw missed its cover this play (§8.5): the ERROR.</summary>
    public bool ThrowSailed => _sailed;
    /// <summary>HUD sub-caption for the last live decision. Narrated, never read back.</summary>
    public string Sub { get; private set; } = "";
    /// <summary>Cues raised by the most recent command.</summary>
    public IReadOnlyList<LiveEvent> Events => _events;

    /// <summary>The glove owns the ball (a catch or a buddy jump).</summary>
    public bool HoldsBall => Caught || Buddy;
    /// <summary>Human on the glove, or a human seat that owns the throw: their pad is the source.</summary>
    public LivePlayCommandSource Source =>
        PlayerFielding || Seats.HumanOwnsThrow ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;

    RulesTable R => _match.Rules;
    Park Park => _match.Park;
    FeelTable Feel => _match.Content.Feel;
    Dictionary<string, Character> Assigned() => FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
    double Hang => Path is null ? (Preview?.HangTimeSec ?? 0) : BallFlight.HangTime(Path, R);
    double Rest => Path is null ? 0 : BallFlight.RestTime(Path);
    /// <summary>The instant a dead ball is decided: a foul at its verdict, anything else at the landing mark.</summary>
    double DeadAt => Ball is not null && LiveKind() == PlayKind.Foul ? Ball.DecidedT : Hang;
    /// <summary>Play seconds before the body at <paramref name="pos"/> may move (§8.2).</summary>
    double ReadyAt(string pos) => _readyAt.TryGetValue(pos, out var t) ? t : 0;
    /// <summary>The body on the ball: the thrower while a throw is in the air (the YOU ring is already on the receiver), else the glove.</summary>
    string OnBallPos => Throwing && !string.IsNullOrEmpty(_throwerPos) ? _throwerPos : GlovePos;
    bool CanMove(string pos) => ElapsedSeconds + 1e-9 >= ReadyAt(pos);

    // ---------------------------------------------------------------------------------
    // Begin
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult BeginLive(LivePlayCommand command)
    {
        if (command.Hit is null || command.Pitch is null || command.Swing is null)
            return new LivePlayCommandResult(Snapshot);
        ResetField();
        _events.Clear();
        Pitch = command.Pitch;
        Swing = command.Swing;
        Hit = command.Hit;
        Preview = command.Preview;
        Field = command.Field;
        Seats = command.Seats ?? LiveSeats.CpuOnly;
        Ball = Preview?.Ball ?? BattedBall.Of(Hit, Park, R);
        Path = Ball.Samples;
        PlayerFielding = FieldAssist.PlayerStartsOnGlove(Seats.PlayerMustField);
        foreach (var kv in FieldingResolver.ReactionLockouts(R)) _readyAt[kv.Key] = kv.Value;
        InitGloves();
        var kind = LiveKind();
        var result = Begin(command with { PlayKind = kind });
        if (!Active) return result;
        Dash01 = command.Dash01;
        _match.Dash01 = Dash01;
        BallX = 0;
        BallY = R.Flight.PlateHeightFt;
        BallZ = 0;
        // An item thrown before the ball was live (the headless game) lands on its body after its flight (§12).
        if (Field is { ItemHit: true }) _itemLandAt = R.Batting.Items.FlySec;
        return new LivePlayCommandResult(Snapshot);
    }

    void InitGloves()
    {
        _fielders.Clear();
        foreach (var kv in Assigned())
            _fielders[kv.Key] = Diamond.Positions[kv.Key];
        SwapLock = 0;
        if (Preview is null)
        {
            GlovePos = "P";
            GloveX = Diamond.Rubber.X;
            GloveZ = Diamond.Rubber.Z;
            return;
        }
        // Preview owns the trajectory-planned first glove for CPU and dead-stick defense alike.
        GlovePos = Preview.Position;
        var at = _fielders[GlovePos];
        GloveX = at.X;
        GloveZ = at.Z;
    }

    void ResetField()
    {
        Pitch = null;
        Swing = null;
        Hit = null;
        Preview = null;
        Field = null;
        Path = null;
        Ball = null;
        FlightDone = false;
        _call = FairFoulCall.Undecided;
        StealPhase = false;
        _fielders.Clear();
        _readyAt.Clear();
        GlovePos = "P";
        GloveX = Diamond.Rubber.X;
        GloveZ = Diamond.Rubber.Z;
        PlayerFielding = false;
        Caught = false;
        Buddy = false;
        ThrowT = 0;
        ThrowDur = 0;
        ThrowBag = 0;
        ArmedThrow = null;
        ArmedCut = null;
        CoverPos = "";
        ThrowFromPos = "";
        SwitchPos = "";
        BuddyPos = "";
        BuddyWindow = false;
        DiveT = JumpT = SwapLock = RecoilT = 0;
        Bobbling = false;
        PlayerBobble = false;
        CatchDive = CatchJump = false;
        _gloved = false;
        _recoilArmed = false;
        _wallCued = false;
        _chomped = false;
        _bobbled = false;
        _sailed = false;
        _dropRolled = false;
        _dropped = false;
        _firstGlove = null;
        _loose = false;
        _looseVX = _looseVZ = 0;
        _looseRestAt = -1;
        _throwerPos = "";
        _cutoffPos = "";
        _cutoffSpot = null;
        _backupPos = "";
        _backupSpot = (0, 0);
        _lobT = 0;
        _relayBag = 0;
        _cpuThrowAt = -1;
        _cpuDecided = false;
        _foilPos = "";
        _foilT = 0;
        _powT = 0;
        _itemLandAt = -1;
        AwaitingRelay = false;
        InClosePlay = false;
        CloseIcon = false;
        CloseBag = 0;
        _closeRunner = null;
        _closePlayT = 0;
        _closeOffAt = _closeDefAt = -1;
        Dash01 = 0;
        StealT = 0;
        StealTagT = -1;
        StealRelease = 0;
        StealPitch = null;
        _cpuGunAt = 0;
        Sub = "";
        // Cues are per command: Tick clears them on entry. Clearing here would drop the cue raised on
        // the tick that also completes the play (a catch, the wall thump) before Unity reads it.
    }

    // ---------------------------------------------------------------------------------
    // Tick: one frame of the live ball
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult Tick(LivePlayCommand command)
    {
        _events.Clear();
        var dt = command.DeltaSeconds;
        // Ownership of a press is decided here, once, from the seats: the offense pad never
        // reaches the gloves and the defense pad never reaches the runners (spec §0.4, #579).
        var field = Seats.OwnedFieldPad(command.FieldPad);
        var run = Seats.OwnedRunPad(command.RunPad);
        if (dt <= 0) return new LivePlayCommandResult(Snapshot);

        if (StealPhase)
        {
            if (Paused) return new LivePlayCommandResult(Snapshot);
            return TickSteal(dt, field);
        }

        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        if (Path is null || Path.Count == 0)
            return new LivePlayCommandResult(Snapshot, FlightDone: true);

        // Dash: mash South on the offense pad (running.dash).
        if (run.SouthDown) Dash01 = Math.Min(R.Running.Dash.MaxDash, Dash01 + R.Running.Dash.PerPress);
        _match.Dash01 = Dash01;
        // The offense pad's runner verbs (§9.3): the seat that owns the runners is the only one that reaches them.
        if (Seats.HumanRuns) ApplyRunPad(run);

        Advance(LivePlayCommand.Advance(dt, LiveKind(), HoldsBall, Throwing, HoldsBall, Dash01, command.Source, PlayFielder()));

        if (_loose)
            TickLooseBall(dt);
        else if (HoldsBall && !Throwing)
        {
            BallX = GloveX;
            BallY = Buddy ? R.Fielding.Catch.BuddyHeldBallY : R.Fielding.Catch.HeldBallY;
            BallZ = GloveZ;
        }
        else if (!Throwing)
        {
            var p = BallFlight.PointAt(Path, ElapsedSeconds, R);
            (BallX, BallY, BallZ) = p;
        }

        if (DiveT > 0) DiveT -= dt;
        if (JumpT > 0) JumpT -= dt;
        if (SwapLock > 0) SwapLock -= dt;
        if (_foilT > 0 && (_foilT -= dt) <= 0) _foilPos = "";
        if (_powT > 0) _powT -= dt;
        if (_itemLandAt >= 0 && ElapsedSeconds >= _itemLandAt)
        {
            _itemLandAt = -1;
            LandItem();
        }

        TickBuddyPartner();
        TickCoverBags(dt);
        TickCutoffAndBackup(dt);
        ChargeOutfield(dt);
        // While the throw is in the air the YOU ring rides the receiver: the cursor follows that body's walk to the bag.
        if (Throwing && _fielders.TryGetValue(GlovePos, out var receiverAt))
            (GloveX, GloveZ) = receiverAt;
        TryTakeGlove(field);
        ClampField();

        if (InClosePlay)
            return TickClosePlay(dt, field, run);

        // The fumble (§8.6): the glove is out of the play for the fumble; the ball is loose on the ground.
        if (RecoilT > 0 && !Throwing)
        {
            RecoilT -= dt;
            if (RecoilT <= 0) Bobbling = false;
            ClampField();
            if (Bobbling) return new LivePlayCommandResult(Snapshot);
            if (HoldsBall && TickLiveContact(out var contactDone))
                return contactDone;
            if (RecoilT > 0) return new LivePlayCommandResult(Snapshot);
        }

        if (Throwing)
        {
            ThrowT += dt;
            var u = Math.Clamp(ThrowT / Math.Max(0.05, ThrowDur), 0, 1);
            BallX = ThrowFrom.X + (ThrowTo.X - ThrowFrom.X) * u;
            BallY = ThrowFrom.Y + (ThrowTo.Y - ThrowFrom.Y) * u;
            BallZ = ThrowFrom.Z + (ThrowTo.Z - ThrowFrom.Z) * u;
            if (ThrowT >= ThrowDur && !command.EffectInFlight)
            {
                if (OnThrowLanded(dt, out var arrived)) return arrived;
                if (!Throwing && !_loose)
                {
                    if (TryBeginClosePlay()) return new LivePlayCommandResult(Snapshot);
                    if (IsTime()) return Commit();
                }
            }
            return new LivePlayCommandResult(Snapshot);
        }

        // A dead ball does not wait for a fielder to possess it. Keep this before
        // ownership dispatch so CPU, assisted defense and two-pad play agree. A homer is dead
        // at the crossing; a foul is dead when the untouched path's call is in (§7.11).
        if (Hit is not null && InPlay.DeadBallResultReady(LiveKind(), ElapsedSeconds, DeadAt, HoldsBall, Throwing,
                command.EffectInFlight, R))
            return Commit();

        if (Ball is not null && !_wallCued && Ball.WallT is { } wallT && ElapsedSeconds >= wallT && !HoldsBall)
        {
            _wallCued = true;
            _events.Add(LiveEvent.WallCarom);
        }
        // Bounced then over the fence (§1): nobody can play it once it is gone; the ground-rule double is committed here.
        if (Hit is not null && Ball is { GroundRule: true, LeavesT: { } leftAt } && !HoldsBall && !Throwing && !_loose
            && ElapsedSeconds >= leftAt + R.Flight.DeadBall.RestHoldSec && !command.EffectInFlight)
            return Commit();
        // A chomper (§14): the fly into the mouth is an out at the landing, nobody's glove.
        if (Preview is { Chomped: true } && !_chomped && !HoldsBall && ElapsedSeconds >= Hang)
        {
            _chomped = true;
            _call = FairFoulCall.Caught;
            RecordCatchOut(PlayFielder());
            return IsTime() ? Commit() : new LivePlayCommandResult(Snapshot);
        }
        if (_chomped) return IsTime() ? Commit() : new LivePlayCommandResult(Snapshot);

        if (PlayerFielding && Preview is not null && Hit is not null)
        {
            var done = TickPlayerField(dt, field);
            ClampField();
            return done ?? new LivePlayCommandResult(Snapshot);
        }

        if (Preview is not null && Hit is not null)
        {
            var done = TickCpuField(dt, field, command.EffectInFlight);
            ClampField();
            return done ?? new LivePlayCommandResult(Snapshot);
        }

        var finished = ElapsedSeconds >= Rest + R.Flight.DeadBall.RestHoldSec;
        return new LivePlayCommandResult(Snapshot, FlightDone: finished && !command.EffectInFlight);
    }

    // ---------------------------------------------------------------------------------
    // Human glove
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult? TickPlayerField(double dt, LivePadInput pad)
    {
        var pre = Preview!;
        var map = Assigned();
        var hang = Hang;
        // A ball not yet in a glove is chased, on the grass after a drop or a carom as much as before the landing (§7.6, §7.8).
        var chasing = !HoldsBall;
        // A fly or a liner is in the air until it lands; after that (or once loose) it is a pickup.
        var onTheGround = _loose || (!pre.Grounder && ElapsedSeconds >= hang);
        var buddyOn = FieldingResolver.BuddyJumpOffered(pre);
        var needsJump = FlyCatch.NeedsJump(pre);
        var plant = FlyCatch.ChaseTarget(pre, Park, R);
        var who = map.TryGetValue(GlovePos, out var gloveNow) ? gloveNow : pre.Fielder;
        BuddyWindow = buddyOn && FlyCatch.JumpWindow(ElapsedSeconds, hang, who, Park, R);
        var stickTake = Feel.FieldAssistStick;
        var catchRules = R.Fielding.Catch;

        NoteSwitchHint(map, pre, pad);
        if (pad.Swap && !buddyOn && !HoldsBall)
        {
            CycleGlove(map, pad);
            SwapLock = R.Fielding.Chase.SwapLockSec;
        }

        var stick = pad.StickMag;
        var steering = (chasing || HoldsBall) && !Throwing;
        var dead = FieldAssist.StickDead(pad.StickX, pad.StickY, stickTake);
        if (SwapLock <= 0 && FieldAssist.CpuChases(HoldsBall, Throwing, dead))
            ChaseGlove(dt, pre);

        // One glove speed for human and CPU (§8.1); the body moves once its reaction lockout is over (§8.2).
        if (steering && map.TryGetValue(GlovePos, out var glove) && stick >= stickTake && CanMove(GlovePos))
        {
            var speed = FieldingResolver.ChaseSpeedFt(glove, pre.Frozen, R, pad.EastHeld);
            GloveX += pad.StickX * speed * dt;
            GloveZ += pad.StickY * speed * dt;
            var feet = FieldBounds.Clamp(Park, GloveX, GloveZ);
            GloveX = feet.X;
            GloveZ = feet.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
        }

        if ((pad.Item || pad.Attack) && chasing && map.TryGetValue(GlovePos, out var tossFrom))
        {
            Character? partner = null;
            foreach (var kv in map)
            {
                if (kv.Key == GlovePos) continue;
                var at = _fielders.TryGetValue(kv.Key, out var spot) ? spot : (0, 0);
                var dist = Diamond.Dist(GloveX, GloveZ, at.X, at.Z);
                if (FieldDash.BuddyTossOffered(_match.Chemistry.Between(tossFrom, kv.Value), dist, R))
                {
                    partner = kv.Value;
                    break;
                }
            }
            if (partner is null && pad.Attack)
            {
                var best = 99.0;
                foreach (var kv in map)
                {
                    if (kv.Key == GlovePos) continue;
                    var at = _fielders.TryGetValue(kv.Key, out var spot) ? spot : (0, 0);
                    var dist = Diamond.Dist(GloveX, GloveZ, at.X, at.Z);
                    if (FieldDash.KickOffered(dist, R) && dist < best)
                    {
                        best = dist;
                        partner = kv.Value;
                    }
                }
            }
            if (partner is not null)
            {
                TakeBall();
                var thr = _match.ThrowBetween(tossFrom, partner);
                ArmedThrow = thr;
                ArmedCut = partner;
                GlovePos = PosOf(map, partner);
                if (_fielders.TryGetValue(GlovePos, out var spot))
                {
                    GloveX = spot.X;
                    GloveZ = spot.Z;
                }
            }
        }
        if (pad.WestDown)
            JumpT = needsJump || buddyOn ? catchRules.WallJumpArmSec : catchRules.JumpArmSec;
        if (pad.EastDown && CanMove(GlovePos))
        {
            var toX = pre.Grounder || pre.Line || _loose ? BallX : plant.X;
            var toZ = pre.Grounder || pre.Line || _loose ? BallZ : plant.Z;
            var lunged = FieldDash.Lunge(GloveX, GloveZ, toX, toZ, R.Fielding.Dash.DiveLungeFt);
            GloveX = lunged.X;
            GloveZ = lunged.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
            DiveT = catchRules.DiveArmSec;
        }

        var window = CatchWindow(map);
        var d = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        if (!HoldsBall && GloveMayTake(GlovePos))
        {
            if (pre.Grounder || onTheGround)
            {
                // A loose ball (a fumble, an overthrow) is picked up by touching it (fielding.chase.looseScoopFt), never by the catch radius.
                var scoopReach = _loose ? R.Fielding.Chase.LooseScoopFt : window;
                if (_loose ? FlyCatch.TouchScoop(d, scoopReach, BallY, R)
                    : FlyCatch.TouchScoop(pre, Park, BallX, BallZ, BallY, ElapsedSeconds, hang, d, window, R))
                    TakeBattedBall();
                var pickupInPlay = _loose || FlyCatch.PickupInPlay(pre, Park, BallX, BallZ, ElapsedSeconds, hang, R);
                if (pickupInPlay && pad.SouthDown && d < scoopReach)
                    TakeBattedBall();
                if (pickupInPlay && FlyCatch.PlayerDiveCatch(DiveT > 0, d, scoopReach, BallY, R))
                {
                    CatchDive = true;
                    TakeBattedBall();
                }
            }
            else
            {
                var inWin = FlyCatch.JumpWindow(ElapsedSeconds, hang, who, Park, R);
                var under = FlyCatch.Under(GloveX, GloveZ, BallX, BallZ, plant.X, plant.Z, window, needsJump, R);
                var jumpTry = JumpT > 0 && FlyCatch.HighEnough(BallY, needsJump || buddyOn, R);
                var buddyRob = buddyOn && Diamond.Dist(GloveX, GloveZ, plant.X, plant.Z) < catchRules.BuddyPlantFt;
                var canRob = !needsJump || FlyCatch.CanRob(pre.Ball?.FenceClearFt ?? double.NaN, who, Park, buddyRob, R);
                if (stick < stickTake && FlyCatch.AutoCatch(under, inWin, needsJump))
                    TakeBattedBall();
                if (FlyCatch.PlayerCaught(jumpTry, pad.SouthDown, under, inWin, needsJump, canRob))
                {
                    if (jumpTry) CatchJump = true;
                    if (buddyOn && inWin && Diamond.Dist(GloveX, GloveZ, plant.X, plant.Z) < catchRules.BuddyPlantFt)
                    {
                        Buddy = true;
                        GloveX = plant.X;
                        GloveZ = plant.Z;
                        _fielders[GlovePos] = (GloveX, GloveZ);
                        _events.Add(LiveEvent.BuddyJump);
                    }
                    TakeBattedBall();
                }
                if (!needsJump && FlyCatch.PlayerDiveCatch(DiveT > 0, d, window, BallY, R))
                {
                    CatchDive = true;
                    TakeBattedBall();
                }
            }
        }

        var stickOk = InPlay.StickNamesBag(chasing, HoldsBall);
        ReadThrowBag(pad, stickOk);

        if (Buddy)
        {
            BallX = GloveX;
            BallY = catchRules.BuddyHeldBallY + (JumpT > 0 ? catchRules.BuddyLeapBallY : 0);
            BallZ = GloveZ;
        }

        if (AwaitingRelay)
        {
            if (ThrowBag <= 0) ThrowBag = 1;
            var batterIn = _match.BatterRunner is not { Live: true, Bag: 0 };
            if (!pad.SouthDown && !batterIn)
                return null;
            AwaitingRelay = false;
            if (batterIn && !pad.SouthDown)
                return Commit();
            return BeginPlayerThrowOrCommit(map, pad);
        }

        if (buddyOn && !Buddy && ElapsedSeconds < hang + catchRules.BuddyJumpHoldSec) return null;
        if (HoldsBall)
        {
            SwitchPos = "";
            // Touched foul: dead in the glove, no throw to make (§7.11).
            if (LiveKind() == PlayKind.Foul)
                return Commit();
            if (TickLiveContact(out var contactDone))
                return contactDone;
            if (pad.SouthDown || pad.Cutoff)
                return BeginPlayerThrowOrCommit(map, pad);
            if (IsTime())
                return Commit();
            return null;
        }
        // Loose: a drop, a miss, or a carom is live (§7.6, §7.8, §7.9); the runners run and the glove chases until Time.
        if (InPlay.HasDeadBallResult(LiveKind())) return null;
        if (IsTime()) return Commit();
        return null;
    }

    void TryTakeGlove(LivePadInput pad)
    {
        if (Seats.PlayerMustField) return;
        if (PlayerFielding || HoldsBall || Throwing) return;
        if (Hit is null || Preview is null) return;
        if (!FieldAssist.StickTakesGlove(pad.StickX, pad.StickY, Feel.FieldAssistStick, pad.Swap))
            return;
        PlayerFielding = true;
        var map = Assigned();
        if (pad.Swap)
            CycleGlove(map, pad);
        else
            AutoGlove(map);
    }

    // ---------------------------------------------------------------------------------
    // CPU glove (dead stick)
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult? TickCpuField(double dt, LivePadInput pad, bool effectInFlight)
    {
        var pre = Preview!;
        var hang = Hang;
        var grounder = pre.Grounder;
        var catchRules = R.Fielding.Catch;
        if (Seats.HumanOwnsThrow && !HoldsBall)
            NoteSwitchHint(Assigned(), pre, pad);
        if (!HoldsBall && Path is not null)
            ChaseGlove(dt, pre);
        _fielders[GlovePos] = (GloveX, GloveZ);
        var cpuMap = Assigned();
        var cpuWindow = CatchWindow(cpuMap);
        var cpuDist = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        // The CPU catch is geometric (§8.3): the glove touching a ball on the ground scoops it; under a
        // fly inside the radius in the window catches it. Nothing is force-fed at hang.
        if (!HoldsBall && GloveMayTake(GlovePos))
        {
            if (_loose ? FlyCatch.TouchScoop(cpuDist, R.Fielding.Chase.LooseScoopFt, BallY, R)
                : FlyCatch.TouchScoop(pre, Park, BallX, BallZ, BallY, ElapsedSeconds, hang, cpuDist, cpuWindow, R))
                TakeBattedBall();
            else if (!grounder && !_loose && !_dropped)
            {
                var plant = FlyCatch.ChaseTarget(pre, Park, R);
                var needsJump = FlyCatch.NeedsJump(pre);
                var who = PlayFielder();
                var inWin = FlyCatch.JumpWindow(ElapsedSeconds, hang, who, Park, R);
                var under = FlyCatch.Under(GloveX, GloveZ, BallX, BallZ, plant.X, plant.Z, cpuWindow, needsJump, R);
                var buddyOn = FieldingResolver.BuddyJumpOffered(pre);
                var buddyAt = buddyOn && !string.IsNullOrEmpty(BuddyPos) && _fielders.TryGetValue(BuddyPos, out var buddySpot)
                              && Diamond.Dist(buddySpot.X, buddySpot.Z, plant.X, plant.Z) < catchRules.BuddyPlantFt;
                var canRob = needsJump && FlyCatch.CanRob(pre.Ball?.FenceClearFt ?? double.NaN, who, Park, buddyAt, R);
                if (FlyCatch.AutoCatch(under, inWin, needsJump, canRob))
                {
                    // Drop chances belong to star effects only (§8.6): rolled once, on the one seeded stream.
                    if (!_dropRolled)
                    {
                        _dropRolled = true;
                        _dropped = _match.RollDrop(Hit!, pre.Frozen);
                    }
                    if (!_dropped)
                    {
                        if (needsJump) CatchJump = true;
                        if (needsJump && buddyAt)
                        {
                            Buddy = true;
                            _events.Add(LiveEvent.BuddyJump);
                        }
                        TakeBattedBall();
                    }
                }
            }
        }
        // The touch made the call: a foul-territory pickup is dead in the glove.
        if (HoldsBall && !Throwing && LiveKind() == PlayKind.Foul)
            return Commit();
        if (!HoldsBall)
        {
            if (InPlay.HasDeadBallResult(LiveKind())) return null;
            return IsTime() ? Commit() : null;
        }
        if (Seats.HumanOwnsThrow)
        {
            // The human seat owns the throw: from here the pad is theirs (the YOU ring is on this glove).
            PlayerFielding = true;
            var owned = Assigned();
            if (TickLiveContact(out var contactDone))
                return contactDone;
            ReadThrowBag(pad, InPlay.StickNamesBag(false, true));
            if (pad.SouthDown || pad.Cutoff)
                return BeginPlayerThrowOrCommit(owned, pad);
            if (IsTime())
                return Commit();
            return null;
        }
        if (!Throwing && TickLiveContact(out var done))
            return done;
        if (effectInFlight) return null;
        if (IsTime())
            return Commit();
        // The decision table (§8.8) runs once per possession, after the fielder's reaction.
        if (!Throwing && RecoilT <= 0 && CpuMayThrow())
            CpuDecide();
        return null;
    }

    /// <summary>The CPU glove holds the ball: the throw waits for the reaction delay (§8.8), then the table runs once.</summary>
    bool CpuMayThrow()
    {
        if (_cpuDecided) return false;
        if (_cpuThrowAt < 0)
        {
            _cpuThrowAt = ElapsedSeconds + InPlay.ThrowReactionSec(GloveChar(), R);
            return false;
        }
        return ElapsedSeconds >= _cpuThrowAt;
    }

    /// <summary>
    /// The CPU fielder's decision table (§8.8), computed from the live bodies the moment the throw
    /// may go: for each bag, <c>margin = runnerArrival − throwArrival</c>; a play is makeable above the
    /// difficulty's margin. Infielders: the lead force, home, third, first, else hold (or, on the grass,
    /// throw in ahead of the lead runner). Outfielders: home if a run is at stake, third, second, else
    /// the cutoff. A throw through the cutoff continues with the cutoff's arm.
    /// </summary>
    void CpuDecide()
    {
        _cpuDecided = true;
        var makeable = R.Cpu.Active.MakeableMarginSec;
        var onGrass = FieldingResolver.OutfieldGrass(GloveX, GloveZ, R);
        double Margin(int bag)
        {
            var runner = RunnerForBag(bag);
            if (runner is null || runner.Bag >= bag) return double.NegativeInfinity;
            if (!runner.Forced || runner.FromBag != bag - 1)
            {
                // An unforced runner is a candidate only when their body is bound for the bag.
                if (!(runner.Advancing && runner.DestBag >= bag && runner.Bag == bag - 1)) return double.NegativeInfinity;
            }
            return RunnerSystem.ArrivalSec(runner, bag, ElapsedSeconds, Dash01, R) - CpuThrowArrivalSec(bag);
        }
        bool Makeable(int bag) => Margin(bag) > makeable;

        if (!onGrass)
        {
            // 1. The lead forced bag ahead of a forced runner (second, third, home); the batter at first is rule 4.
            for (var bag = 4; bag >= 2; bag--)
            {
                if (!Forces.At(bag)) continue;
                var forced = _match.RunnerAt(bag - 1);
                if (forced is null || !forced.Live || forced.Bag >= bag) continue;
                if (Makeable(bag)) { CpuThrowTo(bag); return; }
                break;
            }
            // 2. Home, 3. third (tags on a runner going).
            if (!Forces.At(4) && Makeable(4)) { CpuThrowTo(4); return; }
            if (!Forces.At(3) && Makeable(3)) { CpuThrowTo(3); return; }
            // 4. First.
            if (Forces.At(1) && Makeable(1)) { CpuThrowTo(1); return; }
            // 5. Hold: nobody is out on a throw; Time comes when the bodies settle (§10.6).
            return;
        }

        // Outfielders: home if a run is at stake, third, second, else the cutoff.
        var runAtStake = _match.Outs < 2 || Math.Abs(_match.HomeScore - _match.AwayScore) <= 2;
        if (runAtStake && Makeable(4)) { CpuThrowTo(4); return; }
        if (Makeable(3)) { CpuThrowTo(3); return; }
        if (Makeable(2)) { CpuThrowTo(2); return; }
        CpuThrowIn();
    }

    /// <summary>Seconds from now until a throw from this glove would land at <paramref name="bag"/>, through the cutoff when the arm cannot reach on the fly.</summary>
    double CpuThrowArrivalSec(int bag)
    {
        var to = Diamond.Bag(bag);
        var thr = ArmOnly(GloveChar());
        var dist = Diamond.Dist(GloveX, GloveZ, to.X, to.Z);
        var cut = CutoffFor(bag);
        if (cut is null) return InPlay.ThrowSec(dist, thr, R);
        var (cutPos, cx, cz) = cut.Value;
        var cutter = Assigned()[cutPos];
        return InPlay.ThrowSec(Diamond.Dist(GloveX, GloveZ, cx, cz), thr, R)
               + InPlay.ThrowReactionSec(cutter, R)
               + InPlay.ThrowSec(Diamond.Dist(cx, cz, to.X, to.Z), ArmOnly(cutter), R);
    }

    /// <summary>The arm alone (no chemistry roll): the fielder's own estimate of a throw.</summary>
    ThrowResult ArmOnly(Character who) =>
        new(Chemistry.Neutral, InPlay.ArmMul(who, R) * FieldAbilities.ThrowMul(who, R), false);

    /// <summary>The CPU throws to <paramref name="bag"/>: straight when the arm reaches, through the cutoff on the line otherwise (§8.7).</summary>
    void CpuThrowTo(int bag)
    {
        var cut = CutoffFor(bag);
        if (cut is not null)
        {
            _relayBag = bag;
            BeginThrowToCutoff(cut.Value.Pos, cut.Value.X, cut.Value.Z);
            return;
        }
        BeginThrowToBag(bag);
    }

    /// <summary>
    /// The outfielder with nothing makeable (§8.8 rule 5): the ball comes in to the cutoff, or to the
    /// bag ahead of the lead runner's next advance, so an infielder holds it for Time (§10.6).
    /// </summary>
    void CpuThrowIn()
    {
        var lead = Runners.Where(r => r.Live).OrderByDescending(r => r.Progress).FirstOrDefault();
        var bag = lead is null ? 2 : Math.Min(4, lead.Advancing ? lead.DestBag : lead.Bag + 1);
        if (bag == 4 && lead is { Bag: 3, Advancing: false }) bag = 3;
        var cut = CutoffFor(bag);
        if (cut is not null)
        {
            _relayBag = 0; // the cutoff decides again from the infield
            BeginThrowToCutoff(cut.Value.Pos, cut.Value.X, cut.Value.Z);
            return;
        }
        BeginThrowToBag(bag);
    }

    /// <summary>The cutoff on the line from the glove to <paramref name="bag"/> when the throw is longer than the arm's fly reach (§8.7).</summary>
    (string Pos, double X, double Z)? CutoffFor(int bag)
    {
        var to = Diamond.Bag(bag);
        if (Diamond.Dist(GloveX, GloveZ, to.X, to.Z) <= R.Fielding.Throw.OnTheFlyFt) return null;
        var cover = CoverOf(bag);
        return InPlay.CutoffFor(GloveX, GloveZ, to.X, to.Z, _fielders, GlovePos, cover);
    }

    /// <summary>The runner AI's read of the ball this frame (§9.9): who has it or will, and when.</summary>
    RunnerAiContext AiContext(double dash01)
    {
        BallSituation ball;
        var carry = Hit?.CarryFt ?? 0;
        if (Throwing && ThrowBag is >= 1 and <= 4)
        {
            var to = Diamond.Bag(ThrowBag);
            ball = new BallSituation(false, true, ThrowBag, ElapsedSeconds + Math.Max(0, ThrowDur - ThrowT), to.X, to.Z,
                ElapsedSeconds, FieldingResolver.OutfieldGrass(GloveX, GloveZ, R), BallX, BallZ, carry);
        }
        else if (Throwing)
        {
            // To the cutoff: a free ball first in a glove where the throw lands.
            var lands = ElapsedSeconds + Math.Max(0, ThrowDur - ThrowT);
            ball = new BallSituation(false, false, 0, 0, ThrowTo.X, ThrowTo.Z, lands,
                FieldingResolver.OutfieldGrass(ThrowTo.X, ThrowTo.Z, R), BallX, BallZ, carry);
        }
        else if (HoldsBall || Path is null || Preview is null)
        {
            var onGrass = Path is null ? PlayKind is PlayKind.Double or PlayKind.Triple : FieldingResolver.OutfieldGrass(GloveX, GloveZ, R);
            ball = new BallSituation(HasBall, false, 0, 0, GloveX, GloveZ, ElapsedSeconds, onGrass, GloveX, GloveZ, carry);
        }
        else if (_loose)
        {
            var who = GloveChar();
            var speed = FieldingResolver.ChaseSpeedFt(who, Preview.Frozen, R);
            var meetAt = ElapsedSeconds + Diamond.Dist(GloveX, GloveZ, BallX, BallZ) / Math.Max(1, speed);
            ball = new BallSituation(false, false, 0, 0, BallX, BallZ, meetAt,
                FieldingResolver.OutfieldGrass(BallX, BallZ, R), BallX, BallZ, carry);
        }
        else
        {
            var who = GloveChar();
            var speed = FieldingResolver.ChaseSpeedFt(who, Preview.Frozen, R);
            var route = FieldingPursuit.Plan(Preview, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R, ReadyAt(GlovePos));
            var meetAt = route.Reachable ? route.MeetTimeSec : Math.Max(Rest, ElapsedSeconds + route.TravelTimeSec);
            ball = new BallSituation(false, false, 0, 0, route.X, route.Z, meetAt,
                FieldingResolver.OutfieldGrass(route.X, route.Z, R), Preview.LandingX, Preview.LandingZ, carry);
        }
        var trailing = _match.Inning >= _match.Innings
            ? (_match.Top ? _match.HomeScore - _match.AwayScore : _match.AwayScore - _match.HomeScore)
            : int.MinValue;
        // The outs the runner reads are the count at contact: the two-out contact play does not begin when the batter is retired mid-play.
        return new RunnerAiContext(ElapsedSeconds, OutsAtOpen, trailing, Fly, ball, dash01);
    }

    /// <summary>
    /// The offense pad on the bodies (§9.3): D-pad selects, LB / RB / both send, return, halt every
    /// runner, the stick sends or returns the selected one, a tap of the opposite shoulder halts.
    /// </summary>
    void ApplyRunPad(LivePadInput run)
    {
        var m = _match;
        if (run.KeysBag is >= 1 and <= 4) m.SelectRunner(run.KeysBag);
        var advDown = run.AllAdvance && !_prevRun.AllAdvance;
        var retDown = run.AllReturn && !_prevRun.AllReturn;
        if (run.Freeze)
        {
            var named = run.StickBag > 0 ? Runners.FirstOrDefault(r => r.Live && (r.NextBag == run.StickBag || r.Bag == run.StickBag)) : null;
            if (named is not null) m.HaltAt(named.FromBag);
            else m.FreezeRunners();
        }
        else if (run.AllAdvance)
        {
            if (advDown && Runners.Any(r => r.Live && r.Phase == RunnerPhase.Returning))
                foreach (var r in Runners.Where(r => r.Live && r.Phase == RunnerPhase.Returning)) r.Halt();
            else m.AdvanceAll();
        }
        else if (run.AllReturn)
        {
            if (retDown && Runners.Any(r => r.Live && r.Advancing && r.Feet > 0))
                foreach (var r in Runners.Where(r => r.Live && r.Advancing)) r.Halt();
            else m.ReturnAll();
        }
        var sel = m.SelectedState;
        if (sel is not null && run.StickBag > 0 && !run.Freeze)
        {
            if (run.StickBag == sel.NextBag && sel.Bag < 4) m.SendRunnerAt(sel.FromBag);
            else if (run.StickBag == sel.Bag || run.StickBag == Baserunning.PrevBag(Math.Max(1, sel.Bag))) m.ReturnToBagAt(sel.FromBag);
        }
        if ((run.WestDown || run.SouthDown) && sel is not null && sel.FeetTo(sel.NextBag) <= R.Running.Bags.SlideFt)
            sel.RequestSlide();
        _prevRun = run;
    }

    /// <summary>The CPU-driven glove runs its route to the ball (or the loose ball) once its reaction lockout is over (§8.2).</summary>
    void ChaseGlove(double dt, FieldingPreview pre)
    {
        if (Path is null) return;
        var map = Assigned();
        if (_loose)
        {
            TryHandoffLoose(map);
            if (!CanMove(GlovePos)) return;
            var chaser = map.TryGetValue(GlovePos, out var lc) ? lc : pre.Fielder;
            var run = FieldingResolver.ChaseSpeedFt(chaser, pre.Frozen, R);
            var step = FieldingResolver.StepToward(GloveX, GloveZ, BallX, BallZ, run, dt, Park, R);
            GloveX = step.X;
            GloveZ = step.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
            return;
        }
        var live = BallFlight.PointAt(Path, ElapsedSeconds, R);
        var hang = Hang;
        var airborne = FieldingResolver.InAir(pre, live.Y, ElapsedSeconds, hang);
        var airTarget = FlyCatch.ChaseTarget(pre, Park, R);
        TryHandoffOutfield(map, airborne ? airTarget.X : live.X, airborne ? airTarget.Z : live.Z);
        if (!CanMove(GlovePos)) return;
        var who = map.TryGetValue(GlovePos, out var c) ? c : pre.Fielder;
        var speed = FieldingResolver.ChaseSpeedFt(who, pre.Frozen, R);
        var route = FieldingPursuit.Plan(pre, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R, ReadyAt(GlovePos));
        var next = FieldingResolver.StepToward(GloveX, GloveZ, route.X, route.Z, speed, dt, Park, R);
        GloveX = next.X;
        GloveZ = next.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
    }

    void TryHandoffOutfield(Dictionary<string, Character> map, double ballX, double ballZ)
    {
        var pick = FieldingResolver.PlayGlove(map, ballX, ballZ, _fielders, R);
        if (!FieldingResolver.HandoffToOutfield(GlovePos, pick.Pos)) return;
        HandGloveTo(pick.Pos);
    }

    /// <summary>A loose ball is the nearest body's (§8.6, §8.7): the backup behind an overthrow, the fielder beside a fumble.</summary>
    void TryHandoffLoose(Dictionary<string, Character> map)
    {
        var pick = FieldingResolver.NearestGlove(map, BallX, BallZ, _fielders);
        if (pick.Pos == GlovePos || string.IsNullOrEmpty(pick.Pos)) return;
        if (pick.Pos == _foilPos) return;
        HandGloveTo(pick.Pos);
    }

    /// <summary>The play glove (and the YOU ring) moves to another body; every body stays where it stands.</summary>
    void HandGloveTo(string pos)
    {
        if (pos == GlovePos) return;
        _fielders[GlovePos] = (GloveX, GloveZ);
        GlovePos = pos;
        if (_fielders.TryGetValue(GlovePos, out var at))
        {
            GloveX = at.X;
            GloveZ = at.Z;
        }
        else if (Diamond.Positions.TryGetValue(GlovePos, out var home))
        {
            GloveX = home.X;
            GloveZ = home.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
        }
    }

    void ChargeOutfield(double dt)
    {
        if (Preview is null || Hit is null || Path is null) return;
        if (HoldsBall || Throwing || _loose) return;
        var live = BallFlight.PointAt(Path, ElapsedSeconds, R);
        var hang = Hang;
        var inAir = FieldingResolver.InAir(Preview, live.Y, ElapsedSeconds, hang);
        var plant = FlyCatch.ChaseTarget(Preview, Park, R);
        if (!FieldingResolver.OutfieldShouldCharge(live.X, live.Z, plant.X, plant.Z, R))
            return;
        // Once a grounded ball reaches the grass, ChaseGlove owns the handoff.
        if (!inAir && FieldingResolver.OutfieldGrass(live.X, live.Z, R))
            return;
        var map = Assigned();
        var of = FieldingPursuit.Choose(
            map, FieldingResolver.OutfieldPursuitPositions, Preview, Park, Path, _fielders, ElapsedSeconds, R, _readyAt);
        if (of.Position == GlovePos) return;
        if (!CanMove(of.Position)) return;
        if (!_fielders.TryGetValue(of.Position, out var at)) return;
        var speed = FieldingResolver.ChaseSpeedFt(of.Fielder, Preview.Frozen, R);
        _fielders[of.Position] = FieldingResolver.StepToward(at.X, at.Z, of.Route.X, of.Route.Z, speed, dt, Park, R);
    }

    /// <summary>Who covers <paramref name="bag"/> now (§8.7): the live map with the glove excluded.</summary>
    string CoverOf(int bag)
    {
        if (bag is < 1 or > 4) return "";
        var ballX = Preview?.LandingX ?? BallX;
        var map = InPlay.CoverMap(OnBallPos, ballX);
        return map.TryGetValue(bag, out var pos) ? pos : FieldAssist.CoverKey(bag);
    }

    /// <summary>Cover bodies walk to their bags at the flat cover speed after the start delay (§8.7, D11).</summary>
    void TickCoverBags(double dt)
    {
        if (Preview is null && !StealPhase) return;
        var cover = R.Fielding.Cover;
        var ballX = Preview?.LandingX ?? 0;
        var onBall = OnBallPos;
        var map = InPlay.CoverMap(onBall, ballX);
        foreach (var kv in map)
        {
            var pos = kv.Value;
            if (string.IsNullOrEmpty(pos) || pos == onBall || pos == _cutoffPos || pos == _backupPos) continue;
            if (!StealPhase && ElapsedSeconds < Math.Max(cover.StartSec, ReadyAt(pos))) continue;
            if (!_fielders.TryGetValue(pos, out var at)) continue;
            var goal = Diamond.Bag(kv.Key);
            _fielders[pos] = StepFlat(at, goal, cover.FtPerSec, cover.StopFt, dt);
        }
    }

    /// <summary>The cutoff walks to the throw line and the backup to its spot behind the target (§8.7).</summary>
    void TickCutoffAndBackup(double dt)
    {
        var cover = R.Fielding.Cover;
        if (!string.IsNullOrEmpty(_cutoffPos) && _cutoffSpot is { } spot && _cutoffPos != GlovePos
            && _fielders.TryGetValue(_cutoffPos, out var cutAt) && CanMove(_cutoffPos))
            _fielders[_cutoffPos] = StepFlat(cutAt, spot, cover.FtPerSec, cover.StopFt, dt);
        if (!string.IsNullOrEmpty(_backupPos) && _backupPos != GlovePos
            && _fielders.TryGetValue(_backupPos, out var backAt) && CanMove(_backupPos))
            _fielders[_backupPos] = StepFlat(backAt, _backupSpot, cover.FtPerSec, cover.StopFt, dt);
    }

    (double X, double Z) StepFlat((double X, double Z) at, (double X, double Z) goal, double speed, double stopFt, double dt)
    {
        var dx = goal.X - at.X;
        var dz = goal.Z - at.Z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist < stopFt) return at;
        var step = Math.Min(dist, speed * dt);
        return FieldBounds.Clamp(Park, at.X + dx / dist * step, at.Z + dz / dist * step);
    }

    void ClampField()
    {
        var feet = FieldBounds.Clamp(Park, GloveX, GloveZ);
        GloveX = feet.X;
        GloveZ = feet.Z;
        if (_fielders.Count == 0)
        {
            _fielders[GlovePos] = feet;
            return;
        }
        foreach (var k in _fielders.Keys.ToList())
            _fielders[k] = FieldBounds.Clamp(Park, _fielders[k].X, _fielders[k].Z);
        _fielders[GlovePos] = feet;
    }

    void AutoGlove(Dictionary<string, Character> map)
    {
        (Character Fielder, string Pos) pick;
        if (_loose)
            pick = FieldingResolver.NearestGlove(map, BallX, BallZ, _fielders);
        else if (Preview is not null && Path is not null)
        {
            var live = BallFlight.PointAt(Path, ElapsedSeconds, R);
            var airborne = FieldingResolver.InAir(Preview, live.Y, ElapsedSeconds, Hang);
            var positions = FoulNow
                ? FieldingResolver.FoulPursuitPositions
                : airborne
                    ? FieldingResolver.AirPursuitPositions
                    : FieldingResolver.OutfieldGrass(live.X, live.Z, R)
                        ? FieldingResolver.OutfieldPursuitPositions
                        : FieldingResolver.InfieldPursuitPositions;
            var choice = FieldingPursuit.Choose(map, positions, Preview, Park, Path, _fielders, ElapsedSeconds, R, _readyAt);
            pick = (choice.Fielder, choice.Position);
        }
        else
            pick = FieldingResolver.PlayGlove(map, BallX, BallZ, _fielders, R);
        HandGloveTo(pick.Pos);
    }

    (double X, double Z) SwitchAim(FieldingPreview? pre)
    {
        if (_loose) return (BallX, BallZ);
        if (pre is not null && Path is not null)
        {
            var map = Assigned();
            var who = map.TryGetValue(GlovePos, out var fielder) ? fielder : pre.Fielder;
            var speed = FieldingResolver.ChaseSpeedFt(who, pre.Frozen, R);
            var route = FieldingPursuit.Plan(pre, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R, ReadyAt(GlovePos));
            return (route.X, route.Z);
        }
        return (BallX, BallZ);
    }

    void NoteSwitchHint(Dictionary<string, Character> map, FieldingPreview pre, LivePadInput pad)
    {
        if (HoldsBall || Throwing)
        {
            SwitchPos = "";
            return;
        }
        var spots = LiveSpots(map);
        var aim = SwitchAim(pre);
        SwitchPos = FieldAssist.SwitchHint(GlovePos, spots, aim.X, aim.Z, pad.StickX, pad.StickY, Feel.FieldAssistStick);
        if (!map.ContainsKey(SwitchPos) || SwitchPos == GlovePos)
            SwitchPos = "";
    }

    Dictionary<string, (double X, double Z)> LiveSpots(Dictionary<string, Character> map)
    {
        var spots = new Dictionary<string, (double X, double Z)>();
        foreach (var kv in map)
            spots[kv.Key] = _fielders.TryGetValue(kv.Key, out var live) ? live : Diamond.Positions[kv.Key];
        return spots;
    }

    void CycleGlove(Dictionary<string, Character> map, LivePadInput pad)
    {
        var spots = LiveSpots(map);
        var aim = SwitchAim(Preview);
        var next = FieldAssist.SwapGlove(GlovePos, spots, aim.X, aim.Z, pad.StickX, pad.StickY, Feel.FieldAssistStick);
        if (!map.ContainsKey(next)) next = "P";
        HandGloveTo(next);
    }

    double CatchWindow(Dictionary<string, Character> map)
    {
        var who = map.TryGetValue(GlovePos, out var c) ? c : Preview!.Fielder;
        var radius = FieldingResolver.CatchRadiusFt(who, Preview is not null ? Park : null, R);
        // Abilities widen the reach for their ball (§8.4): Super Jump on a fly, Dive / Burrow on the dirt.
        if (Preview is not null && FlyCatch.IsFly(Preview))
            radius += FieldAbilities.FlyRangeBonus(who, R);
        if (Preview is { Grounder: true })
            radius += FieldAbilities.GroundRangeBonus(who, R);
        return FieldingResolver.CatchWindowFt(radius, DiveT > 0, JumpT > 0, R);
    }

    static string PosOf(Dictionary<string, Character> map, Character who)
    {
        foreach (var kv in map)
            if (kv.Value.Id == who.Id) return kv.Key;
        return "";
    }

    Character GloveChar()
    {
        var map = Assigned();
        return map.TryGetValue(GlovePos, out var c) ? c : Preview?.Fielder ?? _match.Pitcher;
    }

    /// <summary>A body on a peel or dazed by a rocket cannot take the ball; a POW keeps every ball on the dirt hopping (§12).</summary>
    bool GloveMayTake(string pos)
    {
        if (_foilT > 0 && pos == _foilPos) return false;
        if (_powT > 0 && BallY < R.Fielding.Catch.TouchScoopY) return false;
        return true;
    }

    void TickBuddyPartner()
    {
        if (Preview is null || Path is null || !FieldingResolver.BuddyJumpOffered(Preview))
        {
            BuddyWindow = false;
            return;
        }
        var map = Assigned();
        BuddyPos = PosOf(map, Preview.Buddy!);
        if (string.IsNullOrEmpty(BuddyPos)) return;
        var hang = Hang;
        var plant = FlyCatch.WallPlant(Preview, Park, R);
        var hover = R.Fielding.Catch;
        var u = Math.Clamp(ElapsedSeconds / Math.Max(hover.HoverMinSec, hang - hover.HoverLeadSec), 0, 1);
        var start = Diamond.Positions[BuddyPos];
        _fielders[BuddyPos] = (start.X + (plant.X - start.X) * u, start.Z + (plant.Z - start.Z) * u);
        if (!PlayerFielding)
            BuddyWindow = FlyCatch.JumpWindow(ElapsedSeconds, hang, Preview.Fielder, Park, R);
    }

    void ReadThrowBag(LivePadInput pad, bool stickOk)
    {
        var stick = pad.StickBag > 0 ? pad.StickBag : pad.ArrowBag;
        var armed = InPlay.ArmedBag(pad.KeysBag, stick, stickOk);
        if (armed > 0) ThrowBag = armed;
    }

    /// <summary>The bag a South press throws to right now (HUD tell).</summary>
    public int CommitBagFor(LivePadInput pad)
    {
        var hopper = Preview is not null && Preview.Grounder;
        var stick = pad.StickBag > 0 ? pad.StickBag : pad.ArrowBag;
        var armed = InPlay.ArmedBag(ThrowBag > 0 ? ThrowBag : pad.KeysBag, stick, false);
        return InPlay.CommitBag(armed, hopper, pad.Cutoff, DefaultBag());
    }

    int DefaultBag() =>
        ForceRecorded ? 1 : InPlay.DefaultGroundBag(_match.First is not null, _match.RunnerAt(2)?.Advancing == true, _match.RunnerAt(3)?.Advancing == true);

    // ---------------------------------------------------------------------------------
    // Throws (§8.5): one clock, to a bag or the cutoff; the thrower stays put, the YOU ring hands to the receiver.
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult? BeginPlayerThrowOrCommit(Dictionary<string, Character> map, LivePadInput pad)
    {
        var hopperCaught = Preview is not null && Preview.Grounder && HoldsBall;
        var def = DefaultBag();
        if (!HoldsBall)
        {
            // Dead balls finish through the shared deadline before ownership dispatch.
            if (InPlay.HasDeadBallResult(LiveKind())) return null;
            return Commit();
        }
        // LB / X with no bag: the relay to the cutoff on the line to the armed bag (home by default) (§8.7).
        if (pad.Cutoff && (ThrowBag <= 0 || !hopperCaught))
        {
            var toward = ThrowBag is >= 1 and <= 4 ? ThrowBag : 4;
            var to = Diamond.Bag(toward);
            var cut = InPlay.CutoffFor(GloveX, GloveZ, to.X, to.Z, _fielders, GlovePos, CoverOf(toward));
            if (cut is not null)
            {
                _relayBag = ThrowBag is >= 1 and <= 4 ? ThrowBag : 0;
                BeginThrowToCutoff(cut.Value.Pos, cut.Value.X, cut.Value.Z);
                return null;
            }
        }
        ThrowBag = InPlay.CommitBag(ThrowBag, hopperCaught, pad.Cutoff, def);
        if (ThrowBag <= 0)
        {
            if (pad.Cutoff && hopperCaught)
                ThrowBag = def is >= 1 and <= 4 ? def : 1;
            if (ThrowBag <= 0)
                return null;
        }
        _ = map;
        BeginThrowToBag(ThrowBag);
        return null;
    }

    /// <summary>A throw to <paramref name="bag"/> from the glove, with the pair's chemistry and the thrower's arm.</summary>
    void BeginThrowToBag(int bag)
    {
        var map = Assigned();
        var coverPos = CoverOf(bag);
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : PlayFielder();
        var cut = !string.IsNullOrEmpty(coverPos) && map.TryGetValue(coverPos, out var c) ? c : null;
        var thr = cut is not null ? _match.ThrowBetween(from, cut) : ArmOnly(from);
        ArmedThrow = thr;
        ArmedCut = cut;
        var to = Diamond.Bag(bag);
        BeginThrow(thr, bag, to.X, to.Z, coverPos);
    }

    /// <summary>A throw to the cutoff's body on the line (§8.7); the relay continues with the cutoff's arm.</summary>
    void BeginThrowToCutoff(string cutPos, double lineX, double lineZ)
    {
        var map = Assigned();
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : PlayFielder();
        if (!map.TryGetValue(cutPos, out var cutter))
        {
            BeginThrowToBag(Math.Max(1, _relayBag));
            return;
        }
        var thr = _match.ThrowBetween(from, cutter);
        ArmedThrow = thr;
        ArmedCut = cutter;
        _cutoffPos = cutPos;
        _cutoffSpot = (lineX, lineZ);
        // The ball goes to where the cutoff will stand: on the line, or where they are if already there.
        var at = _fielders.TryGetValue(cutPos, out var spot) ? spot : Diamond.Positions[cutPos];
        var onTheLine = Diamond.Dist(at.X, at.Z, lineX, lineZ) < R.Fielding.Cover.RadiusFt;
        BeginThrow(thr, 0, onTheLine ? at.X : lineX, onTheLine ? at.Z : lineZ, cutPos);
    }

    void BeginThrow(ThrowResult thr, int bag, double targetX, double targetZ, string receiverPos)
    {
        Throwing = true;
        ThrowT = 0;
        _lobT = 0;
        ThrowBag = bag;
        CoverPos = receiverPos;
        _throwerPos = GlovePos;
        ThrowFromPos = GlovePos;
        var throwRules = R.Fielding.Throw;
        ThrowFrom = (GloveX, throwRules.HandHeightFt, GloveZ);
        var landing = InPlay.ThrowLanding(GloveX, GloveZ, targetX, targetZ, thr.LateralFt);
        ThrowTo = (landing.X, throwRules.BagHeightFt, landing.Z);
        // One clock (§8.5): the ball flies on the same seconds the bag is judged on, the catcher's gun included (§11.3).
        var dist = Diamond.Dist(ThrowFrom.X, ThrowFrom.Z, targetX, targetZ);
        ThrowDur = InPlay.ThrowSec(dist, thr, R);
        if (bag is >= 1 and <= 4 && !StealPhase)
        {
            var cover = R.Fielding.Cover;
            var behind = InPlay.BackupSpot(GloveX, GloveZ, targetX, targetZ, cover.BackupFt);
            _backupSpot = FieldBounds.Clamp(Park, behind.X, behind.Z);
            _backupPos = InPlay.BackupPos(bag, _backupSpot.X, _backupSpot.Z, _fielders, GlovePos, receiverPos);
        }
        _cpuThrowAt = -1;
        _cpuDecided = false;
        // The thrower stays where they are; the YOU ring hands to the receiver (§8.5).
        _fielders[_throwerPos] = (GloveX, GloveZ);
        if (!string.IsNullOrEmpty(receiverPos) && receiverPos != GlovePos)
            HandGloveTo(receiverPos);
        _events.Add(LiveEvent.ThrowPop);
    }

    /// <summary>
    /// The throw reaches its target (§8.5): caught when it lands inside the receiver's reach and the
    /// receiver is there; hangs as a lob at an uncovered bag until the cover arrives or it drops; a
    /// lateral miss past the reach skips by, live — the ERROR. True when the play is decided or waiting.
    /// </summary>
    bool OnThrowLanded(double dt, out LivePlayCommandResult result)
    {
        result = new LivePlayCommandResult(Snapshot);
        var cover = R.Fielding.Cover;
        var receiverPos = ThrowBag is >= 1 and <= 4 ? CoverPos : _cutoffPos;
        (double X, double Z) target;
        if (ThrowBag is >= 1 and <= 4) target = Diamond.Bag(ThrowBag);
        else if (_cutoffSpot is { } cutSpot) target = cutSpot;
        else target = (ThrowTo.X, ThrowTo.Z);
        var receiverAt = !string.IsNullOrEmpty(receiverPos) && _fielders.TryGetValue(receiverPos, out var at) ? at : target;
        var missed = Diamond.Dist(ThrowTo.X, ThrowTo.Z, target.X, target.Z) > cover.RadiusFt;
        if (missed)
        {
            SailThrow(receiverPos);
            return false;
        }
        var covered = !string.IsNullOrEmpty(receiverPos) && receiverPos != _throwerPos
                      && Diamond.Dist(receiverAt.X, receiverAt.Z, target.X, target.Z) <= cover.RadiusFt;
        if (!covered)
        {
            // Nobody at the bag: the ball hangs as a lob for the cover, then drops there, live (§8.5).
            _lobT += dt;
            (BallX, BallY, BallZ) = ThrowTo;
            if (_lobT < R.Fielding.Throw.LobMaxSec) return false;
            DropThrowAtBag();
            return false;
        }
        (BallX, BallY, BallZ) = ThrowTo;
        if (ThrowBag is >= 1 and <= 4)
        {
            // The receiver takes it on the bag.
            HandGloveTo(receiverPos);
            GloveX = target.X;
            GloveZ = target.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
            return OnThrowArrived(out result);
        }
        // The cutoff has it: the relay continues with their arm (§8.7).
        HandGloveTo(receiverPos);
        GloveX = ThrowTo.X;
        GloveZ = ThrowTo.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
        Throwing = false;
        _lobT = 0;
        _cutoffPos = "";
        _cutoffSpot = null;
        CatchGlove();
        if (PlayerFielding || Seats.HumanOwnsThrow)
        {
            if (_relayBag is >= 1 and <= 4)
            {
                var bag = _relayBag;
                _relayBag = 0;
                BeginThrowToBag(bag);
                return true;
            }
            return false;
        }
        _cpuThrowAt = -1;
        _cpuDecided = false;
        return false;
    }

    /// <summary>The throw skips past its receiver (§8.5, §8.6): the ERROR. The ball rolls on, loose; the nearest body chases it.</summary>
    void SailThrow(string receiverPos)
    {
        _sailed = true;
        Throwing = false;
        Caught = false;
        Buddy = false;
        _lobT = 0;
        _cutoffPos = "";
        _cutoffSpot = null;
        var map = Assigned();
        var who = map.TryGetValue(receiverPos, out var r) ? r.Name : "the cover";
        Sub = $"It sails past {who}!";
        _events.Add(LiveEvent.ThrowSailed);
        var dx = ThrowTo.X - ThrowFrom.X;
        var dz = ThrowTo.Z - ThrowFrom.Z;
        var len = Math.Sqrt(dx * dx + dz * dz);
        var flight = Math.Max(1, ThrowDur - R.Fielding.Throw.ReleaseSec);
        var speed = len / flight * R.Fielding.Overthrow.CarryMul;
        SetLoose(ThrowTo.X, ThrowTo.Z, len > 1e-6 ? dx / len * speed : 0, len > 1e-6 ? dz / len * speed : 0);
        TryHandoffLoose(map);
    }

    /// <summary>The lob nobody came for drops at the bag, live.</summary>
    void DropThrowAtBag()
    {
        Throwing = false;
        Caught = false;
        Buddy = false;
        _lobT = 0;
        _cutoffPos = "";
        _cutoffSpot = null;
        SetLoose(ThrowTo.X, ThrowTo.Z, 0, 0);
        TryHandoffLoose(Assigned());
    }

    void SetLoose(double x, double z, double vx, double vz)
    {
        _loose = true;
        BallX = x;
        BallZ = z;
        BallY = 0;
        _looseVX = vx;
        _looseVZ = vz;
        _looseRestAt = vx == 0 && vz == 0 ? ElapsedSeconds : -1;
    }

    /// <summary>A loose ball rolls to a stop (fielding.overthrow) inside the park.</summary>
    void TickLooseBall(double dt)
    {
        var speed = Math.Sqrt(_looseVX * _looseVX + _looseVZ * _looseVZ);
        if (speed <= 0)
        {
            if (_looseRestAt < 0) _looseRestAt = ElapsedSeconds;
            return;
        }
        var decel = R.Fielding.Overthrow.DecelFtPerSec2 * dt;
        var next = Math.Max(0, speed - decel);
        var nx = BallX + _looseVX / speed * (speed + next) * 0.5 * dt;
        var nz = BallZ + _looseVZ / speed * (speed + next) * 0.5 * dt;
        var inside = FieldBounds.Clamp(Park, nx, nz);
        if (Math.Abs(inside.X - nx) > 1e-6 || Math.Abs(inside.Z - nz) > 1e-6) next = 0;
        BallX = inside.X;
        BallZ = inside.Z;
        BallY = 0;
        _looseVX = speed > 0 ? _looseVX / speed * next : 0;
        _looseVZ = speed > 0 ? _looseVZ / speed * next : 0;
        if (next <= 0) _looseRestAt = ElapsedSeconds;
    }

    /// <summary>The ball lands at the bag. True when the play is decided or waiting on the next press.</summary>
    bool OnThrowArrived(out LivePlayCommandResult result)
    {
        var bag = ThrowBag;
        Throwing = false;
        _lobT = 0;
        _cutoffPos = "";
        _cutoffSpot = null;
        CatchGlove();

        InPlay.GroundThrowStep? step = null;
        var stillComing = false;
        if (bag is >= 1 and <= 4)
        {
            var heading = RunnerSystem.HeadingTo(Runners, bag);
            var close = ClosePlay.Offered(bag, Forces, heading is not null && !heading.IsOn(bag));
            if (close) stillComing = true;
            else
            {
                var verdict = ArrivalVerdict(bag);
                if (verdict.Decided)
                {
                    var landed = ApplyThrow(bag, verdict.RunnerBeats, ThrowerChar());
                    step = landed.Throw;
                    if (step is { } s && !string.IsNullOrEmpty(s.Caption))
                        Sub = s.Caption;
                    MaybeStampCloseSafe(bag);
                }
                else if (heading is null && RunnerForBag(bag) is null && !Runners.Any(r => r.Live && r.IsOn(bag)))
                {
                    // Nobody to play on at that bag (S-34): the throw is named, nothing is decided.
                    LastMoment = new LiveMoment(InPlay.ThrowVerdict.Wasted, bag, ThrowerChar(), null);
                    Sub = Caption;
                }
                else
                    stillComing = heading is not null;
            }
        }
        _ = stillComing;

        if (step is { } decided && WaitForNextThrow(decided))
        {
            result = new LivePlayCommandResult(Snapshot, step);
            return true;
        }
        // The receiver holds the ball: the CPU decides again from here after its reaction (the chain, §8.8 rule 1).
        _cpuThrowAt = -1;
        _cpuDecided = false;
        result = new LivePlayCommandResult(Snapshot, step);
        return false;
    }

    /// <summary>
    /// The ball is on <paramref name="bag"/>: judged from the runner's body (§10.2). A forced runner
    /// short of the bag is out; a runner on it beat the throw; an unforced runner inside the tag
    /// reach is tagged; one further out is not decided yet — the glove holds and the tag rule runs.
    /// </summary>
    (bool Decided, bool RunnerBeats) ArrivalVerdict(int bag)
    {
        var target = RunnerForBag(bag);
        if (target is null)
            return (Runners.Any(r => r.Live && r.IsOn(bag)), true);
        if (target.Bag >= bag) return (true, true);
        var forced = Forces.At(bag) && target.FromBag == bag - 1;
        if (forced) return (true, false);
        var at = Diamond.Bag(bag);
        var (x, z) = target.Position;
        var bags = R.Running.Bags;
        if (Diamond.Dist(x, z, at.X, at.Z) <= bags.TagSafeRadiusFt) return (true, true);
        var reach = bags.TagReachFt - (target.Sliding ? bags.SlideReachCutFt : 0);
        if (Diamond.Dist(x, z, at.X, at.Z) < reach) return (true, false);
        return (false, false);
    }

    void MaybeStampCloseSafe(int bag)
    {
        var runner = Runners.FirstOrDefault(r => r.Live && r.IsOn(bag));
        if (runner is null || double.IsNaN(runner.LastTouchAt)) return;
        if (InPlay.CloseSafe(ElapsedSeconds, runner.LastTouchAt, R))
            _events.Add(LiveEvent.StampSafe);
    }

    /// <summary>The body on the ball: the glove that holds or chases it.</summary>
    Character PlayFielder()
    {
        if (Preview is null) return Field?.Fielder ?? _match.Pitcher;
        return GloveChar();
    }

    /// <summary>The body that released the throw in flight or just landed: the assist the caption names.</summary>
    Character ThrowerChar()
    {
        var map = Assigned();
        return !string.IsNullOrEmpty(_throwerPos) && map.TryGetValue(_throwerPos, out var c) ? c : PlayFielder();
    }

    bool WaitForNextThrow(InPlay.GroundThrowStep step)
    {
        if (!PlayerFielding) return false;
        if (step.PlayOver || step.NextDefaultBag <= 0) return false;
        AwaitingRelay = true;
        ThrowBag = step.NextDefaultBag;
        return true;
    }

    // ---------------------------------------------------------------------------------
    // Commit
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult Commit()
    {
        if (Pitch is null || Swing is null || Hit is null)
            return new LivePlayCommandResult(Snapshot, FlightDone: true);
        FieldingResult? result = null;
        if (Preview is not null)
            result = BuildLiveResult();
        else if (Field is not null)
            result = Field;
        if (result is null)
            return new LivePlayCommandResult(Snapshot, FlightDone: true);
        LastFieldResult = result;
        var play = _match.FinishAtBat(Pitch, Swing, Hit, result);
        return new LivePlayCommandResult(Snapshot, CompletedPlay: play);
    }

    /// <summary>What the gloves did this play, as facts (§8.5, §8.6): Complete names the play from the bodies (§10.6).</summary>
    FieldingResult BuildLiveResult()
    {
        var pre = Preview!;
        var hit = Hit;
        var kind = LiveKind();
        var from = _firstGlove ?? GloveChar();
        var knock = pre.Grounder && hit is not null ? InPlay.KnockbackSec(InPlay.Energy(hit, R), from, R) : 0;
        var feat = kind == PlayKind.FlyOut && _gloved
            ? FieldingResolver.PlayerCatchFeat(pre, Park, Buddy, CatchJump)
            : DefensiveFeat.None;
        return new FieldingResult(kind, from, ArmedCut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace,
            ArmedThrow, pre.Buddy, Warped: Field?.Warped ?? pre.Warped, Item: Field?.Item, Chomped: pre.Chomped,
            Bobble: _bobbled, KnockbackSec: knock, Feat: feat, GroundRule: pre.Ball is { GroundRule: true },
            Caught: _gloved, ThrowSailed: _sailed, ItemHit: Field?.ItemHit ?? false, ItemTarget: Field?.ItemTarget);
    }

    // ---------------------------------------------------------------------------------
    // Catch, bobble, tag
    // ---------------------------------------------------------------------------------

    void CatchGlove()
    {
        if (!Caught && !_gloved) _events.Add(LiveEvent.Glove);
        Caught = true;
        _gloved = true;
        _loose = false;
        _looseVX = _looseVZ = 0;
        _looseRestAt = -1;
        _firstGlove ??= GloveChar();
    }

    /// <summary>A glove takes a thrown or loose ball: no fair / foul call, no bobble roll.</summary>
    void TakeBall()
    {
        CatchGlove();
        _cpuThrowAt = -1;
        _cpuDecided = false;
    }

    /// <summary>
    /// The glove takes the batted ball, and that touch makes the fair / foul call (§5.6): in the
    /// air it is a catch (an out wherever the ball was); on the ground it is fair or foul by
    /// where the ball is. A loose ball picked up again was called already. The scoop or catch
    /// may bobble (§8.6): a fumble with time and scatter, never a caption.
    /// </summary>
    void TakeBattedBall()
    {
        var first = !Caught;
        var wasLoose = _loose;
        CatchGlove();
        _cpuThrowAt = -1;
        _cpuDecided = false;
        if (!first || Preview is null) return;
        if (_call == FairFoulCall.Undecided && !wasLoose)
        {
            // In the air = before the ball's first ground contact (the landing mark), read off the path, not a height.
            var inTheAir = !Preview.Grounder && ElapsedSeconds <= Hang + 1e-6;
            _call = inTheAir ? FairFoulCall.Caught
                : FieldBounds.IsFair(BallX, BallZ) ? FairFoulCall.Fair
                : FairFoulCall.Foul;
        }
        if (!wasLoose) ArmRecoil();
    }

    void ArmRecoil()
    {
        if (_recoilArmed || Preview is null || !Preview.Grounder || Buddy) return;
        _recoilArmed = true;
        if (Hit is null) return;
        var who = GloveChar();
        var energy = InPlay.Energy(Hit, R);
        // The one seeded stream (S-92). The client used to re-roll this with an ad-hoc Random.
        var bobble = _match.RollBobble(energy, who);
        var knock = InPlay.KnockbackSec(energy, who, R);
        PlayerBobble = bobble;
        var rules = R.Fielding.Bobble;
        if (bobble)
        {
            // The fumble (§8.6): the ball scatters loose; the glove is out of it for the fumble, then chases.
            _bobbled = true;
            Bobbling = true;
            Caught = false;
            RecoilT = rules.FumbleSec;
            var ax = BallX - GloveX;
            var az = BallZ - GloveZ;
            if (ax * ax + az * az < 0.4) { ax = 0; az = 1; }
            var len = Math.Sqrt(ax * ax + az * az);
            SetLoose(GloveX + ax / len * rules.ScatterFt, GloveZ + az / len * rules.ScatterFt,
                ax / len * rules.ScatterFtPerSec, az / len * rules.ScatterFtPerSec);
            BallY = rules.ScatterBallY;
            _events.Add(LiveEvent.Bobble);
            Sub = $"{who.Name} bobbles it!";
        }
        else if (knock > R.Fielding.Knockback.MinSec)
            RecoilT = knock;
    }

    /// <summary>The thrown item lands on its body (§12): a peel or a rocket keeps that glove off the ball; a POW hops every ball on the dirt.</summary>
    void LandItem()
    {
        if (Field is not { ItemHit: true, Item: { } item }) return;
        var sec = ErrorItems.EffectSec(item, R);
        if (ErrorItems.AffectsEveryGlove(item))
        {
            _powT = sec;
            return;
        }
        var map = Assigned();
        var pos = Field.ItemTarget is not null ? PosOf(map, Field.ItemTarget) : GlovePos;
        if (string.IsNullOrEmpty(pos)) pos = GlovePos;
        _foilPos = pos;
        _foilT = sec;
        // A body hit while holding the ball drops it where they stand.
        if (HoldsBall && !Throwing && pos == GlovePos)
        {
            Caught = false;
            Buddy = false;
            SetLoose(GloveX, GloveZ, 0, 0);
        }
    }

    /// <summary>Glove on a bag or a body: forces and tags from the geometry. True when a command landed.</summary>
    bool TickLiveContact(out LivePlayCommandResult result)
    {
        result = new LivePlayCommandResult(Snapshot);
        if (Hit is null) return false;
        if (!HoldsBall || Throwing) return false;
        var command = LivePlayCommand.Contact(LiveKind(), true, false, HoldsBall, GloveX, GloveZ, Dash01, PlayFielder(), Source);
        var contact = Contact(command);
        if (contact.Throw is null && contact.TaggedFromBag is null) return false;
        Sub = Caption;
        if (_match.Outs >= 3 || IsTime())
        {
            result = Commit();
            return true;
        }
        result = contact;
        return true;
    }

    /// <summary>
    /// What the ball has decided so far (§7): a touch that called it foul is dead; a catch in the
    /// air is an out; a chomped fly is an out; an untouched foul path is foul; an uncaught homer
    /// flight is a home run at the crossing; everything else is <see cref="PlayKind.InPlay"/>
    /// until Complete names it from the bodies. Never a hit label from carry.
    /// </summary>
    PlayKind LiveKind()
    {
        if (_call == FairFoulCall.Foul) return PlayKind.Foul;
        if (_call == FairFoulCall.Caught) return PlayKind.FlyOut;
        if (Preview is null) return Field?.Kind ?? PlayKind;
        if (Preview.Chomped) return PlayKind.FlyOut;
        return FlyCatch.PlayerKind(false, Preview, inAir: true, foul: FoulNow);
    }

    // ---------------------------------------------------------------------------------
    // Close play (spec §9.6 as shipped: the mash runs on every unforced 3B/home throw; P5 adds the margin)
    // ---------------------------------------------------------------------------------

    Runner? _closeRunner;

    bool TryBeginClosePlay()
    {
        var heading = RunnerSystem.HeadingTo(Runners, ThrowBag);
        if (heading is null || heading.IsOn(ThrowBag) || !ClosePlay.Offered(ThrowBag, Forces, true))
            return false;
        // The body in the play waits for the verdict (the mash is the slide): safe puts them on the bag, out retires them.
        _closeRunner = heading;
        heading.Halt();
        InClosePlay = true;
        _closePlayT = 0;
        CloseIcon = false;
        CloseBag = ThrowBag;
        _closeOffAt = _closeDefAt = -1;
        return true;
    }

    LivePlayCommandResult TickClosePlay(double dt, LivePadInput field, LivePadInput run)
    {
        _closePlayT += dt;
        if (!CloseIcon)
        {
            if (_closePlayT < ClosePlay.IconDelaySec(R)) return new LivePlayCommandResult(Snapshot);
            CloseIcon = true;
            _closePlayT = 0;
            _events.Add(LiveEvent.CloseIcon);
            return new LivePlayCommandResult(Snapshot);
        }

        var runner = _closeRunner?.Who;
        var fielder = PlayFielder();
        var offenseHuman = Seats.Versus ? Seats.HumanBats : Seats.HumanBats && !Seats.PlayerMustField && !PlayerFielding;
        var defenseHuman = PlayerFielding || Seats.HumanPitches || Seats.PlayerMustField;

        if (_closeOffAt < 0)
        {
            if (offenseHuman)
            {
                if (run.SouthDown) _closeOffAt = _closePlayT;
            }
            else
            {
                var cpu = ClosePlay.CpuReactionSec(runner?.Stats.Run ?? 5, R);
                if (_closePlayT >= cpu) _closeOffAt = cpu;
            }
        }
        if (_closeDefAt < 0)
        {
            if (defenseHuman)
            {
                if (field.SouthDown) _closeDefAt = _closePlayT;
            }
            else
            {
                var cpu = ClosePlay.CpuReactionSec(fielder.Stats.Field, R);
                if (_closePlayT >= cpu) _closeDefAt = cpu;
            }
        }

        if (_closeOffAt < 0 || _closeDefAt < 0) return new LivePlayCommandResult(Snapshot);
        var safe = ClosePlay.OffenseSafe(_closeOffAt, _closeDefAt);
        Sub = ClosePlay.Caption(CloseBag, safe);
        if (safe) _events.Add(LiveEvent.StampSafe);
        if (_closeRunner is { Live: true } body)
        {
            if (safe) body.Arrive(CloseBag, ElapsedSeconds);
            else Retire(body.FromBag, CloseBag, OutType.Tag, PlayFielder());
            if (!safe) LastMoment = new LiveMoment(InPlay.ThrowVerdict.TagOut, CloseBag, PlayFielder(), body.Who);
            Throws++;
        }
        else
            ApplyThrow(CloseBag, safe, PlayFielder());
        _closeRunner = null;
        InClosePlay = false;
        CloseIcon = false;
        _cpuThrowAt = -1;
        _cpuDecided = false;
        // The play goes on from the bag: the other bodies settle and Time ends it (§10.6).
        if (_match.Outs >= 3 || IsTime()) return Commit();
        return new LivePlayCommandResult(Snapshot);
    }

    // ---------------------------------------------------------------------------------
    // Items (offense verbs decided in the client; the ball's result lives here)
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult ApplyItem(LivePlayCommand command)
    {
        if (Preview is null || string.IsNullOrEmpty(command.ItemId)) return new LivePlayCommandResult(Snapshot);
        if (FoulNow) return new LivePlayCommandResult(Snapshot); // a foul cannot become a hit (§7.11)
        Field ??= new FieldingResult(LiveKind(), Preview.Fielder, null, Preview.HangTimeSec, Preview.LandingX, Preview.LandingZ, Preview.Heatball, Preview.Furnace);
        Field = _match.ThrowItem(Field, command.ItemId, command.Fielder ?? GloveChar());
        _itemLandAt = -1;
        LandItem();
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult SmashItem()
    {
        if (Field is not null)
            Field = ErrorItems.Smash(Field, Preview is not null && Preview.Grounder);
        _itemLandAt = -1;
        _events.Add(LiveEvent.ItemSmashed);
        Sub = "Item smashed.";
        return new LivePlayCommandResult(Snapshot);
    }

    // ---------------------------------------------------------------------------------
    // Steal phase (spec §11.3 as shipped)
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult BeginSteal(LivePlayCommand command)
    {
        if (command.StealPitch is null) return new LivePlayCommandResult(Snapshot);
        ResetField();
        _events.Clear();
        Reset();
        Active = true;
        Paused = _match.Paused;
        StealPhase = true;
        Seats = command.Seats ?? LiveSeats.CpuOnly;
        StealPitch = command.StealPitch;
        StealT = 0;
        StealTagT = -1;
        StealRelease = 0;
        Caught = true;
        Buddy = false;
        Throwing = false;
        ThrowBag = StealThrow.DefaultBag(_match.StealTargetBag);
        CoverPos = FieldAssist.CoverKey(ThrowBag);
        PlayerFielding = FieldAssist.PlayerStartsOnGlove(Seats.PlayerMustField);
        InitStealGloves();
        ArmedThrow = null;
        ArmedCut = null;
        CatchGlove();
        var map = Assigned();
        var catcher = map.TryGetValue("C", out var c) ? c : _match.Pitcher;
        // The one seeded stream (S-92). The client used to seed a Random from the catcher's stat.
        _cpuGunAt = _match.RollCatcherRelease(catcher);
        BallX = GloveX;
        BallY = R.Fielding.Catch.HeldBallY;
        BallZ = GloveZ;
        return new LivePlayCommandResult(Snapshot);
    }

    void InitStealGloves()
    {
        _fielders.Clear();
        foreach (var kv in Assigned())
            _fielders[kv.Key] = Diamond.Positions[kv.Key];
        GlovePos = "C";
        var spot = StealThrow.CatcherSpot;
        GloveX = spot.X;
        GloveZ = spot.Z;
        _fielders["C"] = (GloveX, GloveZ);
        if (!string.IsNullOrEmpty(CoverPos))
            _fielders[CoverPos] = FieldAssist.CoverSpot(CoverPos);
    }

    LivePlayCommandResult TickSteal(double dt, LivePadInput pad)
    {
        StealT += dt;
        ElapsedSeconds += dt;
        TickCoverBags(dt);
        var map = Assigned();

        if (StealTagT >= 0)
        {
            StealTagT += dt;
            (BallX, BallY, BallZ) = ThrowTo;
            if (StealTagT >= R.Fielding.Catcher.TagHoldSec)
                return CommitSteal();
            return new LivePlayCommandResult(Snapshot);
        }

        var fromBag = _match.ArmedStealBag;
        var state = _match.RunnerAt(fromBag);
        var remain = state is not null
            ? StealThrow.RunnerRemainSec(state.Who, R)
            : R.Running.Steal.NoThrowRemainSec;

        if (Throwing)
        {
            ThrowT += dt;
            var u = Math.Clamp(ThrowT / Math.Max(0.05, ThrowDur), 0, 1);
            BallX = ThrowFrom.X + (ThrowTo.X - ThrowFrom.X) * u;
            BallY = ThrowFrom.Y + (ThrowTo.Y - ThrowFrom.Y) * u;
            BallZ = ThrowFrom.Z + (ThrowTo.Z - ThrowFrom.Z) * u;
            if (ThrowT >= ThrowDur)
            {
                Throwing = false;
                StealTagT = 0;
                (BallX, BallY, BallZ) = ThrowTo;
            }
            return new LivePlayCommandResult(Snapshot);
        }

        BallX = GloveX;
        BallY = R.Fielding.Catch.HeldBallY;
        BallZ = GloveZ;
        var fieldSeat = Seats.PlayerMustField || Seats.HumanPitches;
        if (fieldSeat && pad.Swap)
        {
            CycleGlove(map, pad);
            PlayerFielding = true;
        }
        if (!PlayerFielding && fieldSeat &&
            FieldAssist.StickTakesGlove(pad.StickX, pad.StickY, Feel.FieldAssistStick, false))
        {
            PlayerFielding = true;
            GlovePos = "C";
            var spot = StealThrow.CatcherSpot;
            GloveX = spot.X;
            GloveZ = spot.Z;
            _fielders["C"] = (GloveX, GloveZ);
        }

        ReadThrowBag(pad, stickOk: true);

        if (PlayerFielding)
        {
            if (pad.SouthDown)
            {
                FireStealThrow(map);
                return new LivePlayCommandResult(Snapshot);
            }
        }
        else if (StealT >= _cpuGunAt)
        {
            ThrowBag = StealThrow.DefaultBag(_match.StealTargetBag);
            FireStealThrow(map);
            return new LivePlayCommandResult(Snapshot);
        }

        if (StealT >= remain)
        {
            ThrowBag = 0;
            StealRelease = remain;
            return CommitSteal();
        }
        return new LivePlayCommandResult(Snapshot);
    }

    void FireStealThrow(Dictionary<string, Character> map)
    {
        var target = _match.StealTargetBag;
        ThrowBag = StealThrow.CommitBag(ThrowBag, target);
        if (ThrowBag <= 0) ThrowBag = StealThrow.DefaultBag(target);
        StealRelease = StealT;
        var coverPos = FieldAssist.CoverKey(ThrowBag);
        map.TryGetValue(coverPos, out var cut);
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : (map.TryGetValue("C", out var catcher) ? catcher : null);
        ThrowResult? thr = null;
        if (from is not null && cut is not null) thr = _match.ThrowBetween(from, cut);
        ArmedThrow = thr ?? (from is not null ? ArmOnly(from) : new ThrowResult(Chemistry.Neutral, 1.0, false));
        ArmedCut = cut;
        var to = Diamond.Bag(ThrowBag);
        BeginThrow(ArmedThrow, ThrowBag, to.X, to.Z, coverPos);
    }

    LivePlayCommandResult CommitSteal()
    {
        if (StealPitch is null)
            return new LivePlayCommandResult(Snapshot, FlightDone: true);
        var play = _match.ResolveStealThrow(StealPitch, ThrowBag, StealRelease, ArmedThrow);
        Throwing = false;
        Caught = false;
        CoverPos = "";
        ThrowFromPos = "";
        StealPitch = null;
        PlayerFielding = false;
        StealPhase = false;
        Reset();
        return new LivePlayCommandResult(Snapshot, CompletedPlay: play);
    }

    /// <summary>The HUD tell for the steal phase: which bag a South press throws to.</summary>
    public int StealCommitBagFor(LivePadInput pad)
    {
        var stick = pad.StickBag > 0 ? pad.StickBag : pad.ArrowBag;
        var armed = InPlay.ArmedBag(ThrowBag > 0 ? ThrowBag : pad.KeysBag, stick, true);
        return StealThrow.CommitBag(armed, _match.StealTargetBag);
    }
}
