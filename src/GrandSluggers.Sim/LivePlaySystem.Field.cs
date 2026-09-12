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
    int ArrowBag = 0)
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
    WallCarom
}

/// <summary>
/// The live ball between contact and Time: gloves, catches, throws, the relay chain, the
/// close-play race, the bobble, and the steal phase (spec §0.1, A.7). Unity supplies the
/// pads once per frame and draws the state; every decision is made here from positions,
/// clocks, and <see cref="Match"/>'s seeded random stream.
/// </summary>
public sealed partial class LivePlaySystem
{
    readonly Dictionary<string, (double X, double Z)> _fielders = new();
    readonly List<LiveEvent> _events = [];
    bool _gloved;
    bool _recoilArmed;
    bool _wallCued;
    FairFoulCall _call;
    int[]? _relayBags;
    int _relayI;
    double _closePlayT;
    double _closeOffAt = -1;
    double _closeDefAt = -1;
    double _cpuGunAt;

    // ---- What Unity draws. Read after every Apply; never written from outside. ----

    public PitchCommand? Pitch { get; private set; }
    public SwingCommand? Swing { get; private set; }
    public AtBatResult? Hit { get; private set; }
    public FieldingPreview? Preview { get; private set; }
    /// <summary>The resolver's call for a CPU glove; null while a human runs the glove from the start.</summary>
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
    Dictionary<string, Character> Assigned() => FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
    double Hang => Path is null ? (Preview?.HangTimeSec ?? 0) : BallFlight.HangTime(Path, R);
    double Rest => Path is null ? 0 : BallFlight.RestTime(Path);
    /// <summary>The instant a dead ball is decided: a foul at its verdict, anything else at the landing mark.</summary>
    double DeadAt => Ball is not null && LiveKind() == PlayKind.Foul ? Ball.DecidedT : Hang;

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
        InitGloves();
        var kind = LiveKind();
        var result = Begin(command with { PlayKind = kind });
        if (!Active) return result;
        Dash01 = command.Dash01;
        _match.Dash01 = Dash01;
        BallX = 0;
        BallY = R.Flight.PlateHeightFt;
        BallZ = 0;
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
        _relayBags = null;
        _relayI = 0;
        AwaitingRelay = false;
        InClosePlay = false;
        CloseIcon = false;
        CloseBag = 0;
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

        Advance(LivePlayCommand.Advance(dt, LiveKind(), HoldsBall, Throwing, HoldsBall, Dash01, command.Source));

        var bobble = R.Fielding.Bobble;
        if (Bobbling)
        {
            var dx = BallX - GloveX;
            var dz = BallZ - GloveZ;
            if (dx * dx + dz * dz < 0.4) { dx = 0; dz = 1; }
            var len = Math.Sqrt(dx * dx + dz * dz);
            BallX += dx / len * bobble.ScatterFtPerSec * dt;
            BallZ += dz / len * bobble.ScatterFtPerSec * dt;
            BallY = Math.Max(0, BallY - bobble.ScatterDropFtPerSec * dt);
        }
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

        TickBuddyPartner();
        TickCoverBags(dt);
        ChargeOutfield(dt);
        TryTakeGlove(field);
        ClampField();

        if (InClosePlay)
            return TickClosePlay(dt, field, run);

        if (RecoilT > 0 && !Throwing)
        {
            if (!Bobbling && Caught)
            {
                var ax = GloveX - BallX;
                var az = GloveZ - BallZ;
                if (ax * ax + az * az > 0.2)
                {
                    var len = Math.Sqrt(ax * ax + az * az);
                    GloveX += ax / len * bobble.RecoilFtPerSec * dt;
                    GloveZ += az / len * bobble.RecoilFtPerSec * dt;
                    _fielders[GlovePos] = (GloveX, GloveZ);
                }
            }
            ClampField();
            RecoilT -= dt;
            if (RecoilT <= 0 && Bobbling)
                return Commit();
            if (!Bobbling && HoldsBall && TickLiveContact(out var contactDone))
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
                if (OnThrowArrived(out var arrived)) return arrived;
                if (TryBeginClosePlay()) return new LivePlayCommandResult(Snapshot);
                if (IsTime()) return Commit();
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
        if (Hit is not null && Ball is { GroundRule: true, LeavesT: { } leftAt } && !HoldsBall && !Throwing
            && ElapsedSeconds >= leftAt + R.Flight.DeadBall.RestHoldSec && !command.EffectInFlight)
            return Commit();

        if (PlayerFielding && Preview is not null && Hit is not null)
        {
            var done = TickPlayerField(dt, field);
            ClampField();
            return done ?? new LivePlayCommandResult(Snapshot);
        }

        if (Preview is not null && Field is not null && Hit is not null)
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
        var rest = Rest;
        var chasing = !HoldsBall && (pre.Grounder || pre.Line ? ElapsedSeconds < rest : ElapsedSeconds < hang);
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

        if (steering && map.TryGetValue(GlovePos, out var glove) && stick >= stickTake)
        {
            var speed = FieldingResolver.StickSpeedFt(glove, pre.Frozen, pad.EastHeld, R);
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
                CatchGlove();
                var thr = _match.ThrowBetween(tossFrom, partner);
                Field = FieldDash.ApplyBuddyToss(
                    Field ?? new FieldingResult(PlayKind.GroundOut, tossFrom, partner, 0.4, GloveX, GloveZ, false, false),
                    partner, thr);
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
        if (pad.EastDown)
        {
            var toX = pre.Grounder || pre.Line ? BallX : plant.X;
            var toZ = pre.Grounder || pre.Line ? BallZ : plant.Z;
            var lunged = FieldDash.Lunge(GloveX, GloveZ, toX, toZ, R.Fielding.Dash.DiveLungeFt);
            GloveX = lunged.X;
            GloveZ = lunged.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
            DiveT = catchRules.DiveArmSec;
        }

        var window = CatchWindow(map);
        var d = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        if (pre.Grounder || pre.Line)
        {
            if (FlyCatch.TouchScoop(pre, Park, BallX, BallZ, BallY, ElapsedSeconds, hang, d, window, R))
            {
                TakeBattedBall();
                ArmRecoil();
            }
            var pickupInPlay = FlyCatch.PickupInPlay(pre, Park, BallX, BallZ, ElapsedSeconds, hang, R);
            if (pickupInPlay && pad.SouthDown && d < window)
            {
                TakeBattedBall();
                ArmRecoil();
            }
            if (pickupInPlay && FlyCatch.PlayerDiveCatch(DiveT > 0, d, window, BallY, R))
            {
                CatchDive = true;
                TakeBattedBall();
                ArmRecoil();
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
            {
                TakeBattedBall();
                ArmRecoil();
            }
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
                ArmRecoil();
            }
            if (!needsJump && FlyCatch.PlayerDiveCatch(DiveT > 0, d, window, BallY, R))
            {
                CatchDive = true;
                TakeBattedBall();
                ArmRecoil();
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
            var batterIn = ElapsedSeconds >= InPlay.HomeToFirstSec(_match.Batter, Dash01, R);
            if (!pad.SouthDown && !batterIn)
                return null;
            AwaitingRelay = false;
            if (batterIn && !pad.SouthDown)
                return Commit();
            return BeginPlayerThrowOrCommit(map, pad);
        }

        if (buddyOn && !Buddy && ElapsedSeconds < hang + catchRules.BuddyJumpHoldSec) return null;
        if (pre.Grounder || pre.Line)
        {
            if (!Caught && ElapsedSeconds < rest) return null;
        }
        else if (!HoldsBall && ElapsedSeconds < hang) return null;
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
        return BeginPlayerThrowOrCommit(map, pad);
    }

    void TryTakeGlove(LivePadInput pad)
    {
        if (Seats.PlayerMustField) return;
        if (PlayerFielding || HoldsBall || Throwing) return;
        if (Hit is null || Preview is null) return;
        if (!FieldAssist.StickTakesGlove(pad.StickX, pad.StickY, Feel.FieldAssistStick, pad.Swap))
            return;
        PlayerFielding = true;
        Field = null;
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
        var cpu = Field!;
        var hang = Hang;
        var rest = Rest;
        var grounder = pre.Grounder;
        var line = pre.Line;
        var catchRules = R.Fielding.Catch;
        if (Seats.HumanOwnsThrow && !HoldsBall)
            NoteSwitchHint(Assigned(), pre, pad);
        if (!Caught && Path is not null)
            ChaseGlove(dt, pre);
        _fielders[GlovePos] = (GloveX, GloveZ);
        var outPlay = cpu.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
        var reached = outPlay || cpu.Bobble;
        var cpuMap = Assigned();
        var cpuWindow = CatchWindow(cpuMap);
        var cpuDist = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        if (!Caught && FlyCatch.TouchScoop(pre, Park, BallX, BallZ, BallY, ElapsedSeconds, hang, cpuDist, cpuWindow, R))
        {
            TakeBattedBall();
            ArmRecoil();
        }
        if (!Caught && !grounder)
        {
            var plant = FlyCatch.ChaseTarget(pre, Park, R);
            var needsJump = FlyCatch.NeedsJump(pre);
            var inWin = FlyCatch.JumpWindow(ElapsedSeconds, hang, PlayFielder(), Park, R);
            var under = FlyCatch.Under(GloveX, GloveZ, BallX, BallZ, plant.X, plant.Z, cpuWindow, needsJump, R);
            if (FlyCatch.AutoCatch(under, inWin, needsJump))
            {
                TakeBattedBall();
                if (cpu.Kind is not PlayKind.FlyOut)
                    Field = cpu = cpu with { Kind = PlayKind.FlyOut };
                ArmRecoil();
            }
        }
        // Spec A.4 #38: the resolver's out is still force-fed to the glove at the window here. P4 makes it a radius check.
        if (!Caught && reached && (grounder
                ? ElapsedSeconds >= hang && BallY < catchRules.GrounderScoopMaxBallY
                : line
                    ? ElapsedSeconds >= hang - catchRules.LineCatchLeadSec && BallY < catchRules.LineCatchMaxBallY
                    : ElapsedSeconds >= hang - catchRules.FlyCatchLeadSec))
        {
            TakeBattedBall();
            ArmRecoil();
        }
        // The touch made the call: a foul-territory pickup is dead in the glove; a fair pickup of a
        // ball the flight had rolling foul is a live hop the resolver never scored — play it as one.
        if (HoldsBall && !Throwing && LiveKind() == PlayKind.Foul)
            return Commit();
        if (HoldsBall && _call == FairFoulCall.Fair && cpu.Kind == PlayKind.Foul)
            Field = cpu = cpu with { Kind = FlyCatch.PlayerKind(true, pre, Hit, inAir: false, R, foul: false) };
        if (!grounder && !line && !HoldsBall && ElapsedSeconds < hang) return null;
        if ((grounder || line) && !Caught && ElapsedSeconds < rest) return null;
        if (cpu.Bobble && Caught)
            return null;
        if (Seats.HumanOwnsThrow && HoldsBall)
        {
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
        if (HoldsBall && !Throwing && TickLiveContact(out var done))
            return done;
        if (outPlay && grounder)
        {
            if (StartGroundRelays()) return null;
        }
        else if (outPlay && cpu.Throw is not null)
        {
            ArmedThrow = cpu.Throw;
            ArmedCut = cpu.Cutoff;
            BeginThrow(cpu.Throw, cpu.Cutoff, 0);
            return null;
        }
        if (!grounder && ElapsedSeconds < hang + catchRules.FlyThrowDelaySec) return null;
        if (effectInFlight) return null;
        if (IsTime())
            return Commit();
        return null;
    }

    void ChaseGlove(double dt, FieldingPreview pre)
    {
        if (Path is null) return;
        var live = BallFlight.PointAt(Path, ElapsedSeconds, R);
        var map = Assigned();
        var hang = Hang;
        var airborne = FieldingResolver.InAir(pre, live.Y, ElapsedSeconds, hang);
        var airTarget = FlyCatch.ChaseTarget(pre, Park, R);
        TryHandoffOutfield(map, airborne ? airTarget.X : live.X, airborne ? airTarget.Z : live.Z);
        var who = map.TryGetValue(GlovePos, out var c) ? c : pre.Fielder;
        var run = FieldingResolver.ChaseSpeedFt(who, pre.Frozen, R);
        var route = FieldingPursuit.Plan(pre, Park, Path, ElapsedSeconds, GloveX, GloveZ, run, R);
        var next = FieldingResolver.StepToward(GloveX, GloveZ, route.X, route.Z, run, dt, Park, R);
        GloveX = next.X;
        GloveZ = next.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
    }

    void TryHandoffOutfield(Dictionary<string, Character> map, double ballX, double ballZ)
    {
        var pick = FieldingResolver.PlayGlove(map, ballX, ballZ, _fielders, R);
        if (!FieldingResolver.HandoffToOutfield(GlovePos, pick.Pos)) return;
        _fielders[GlovePos] = (GloveX, GloveZ);
        GlovePos = pick.Pos;
        if (_fielders.TryGetValue(GlovePos, out var at))
        {
            GloveX = at.X;
            GloveZ = at.Z;
        }
    }

    void ChargeOutfield(double dt)
    {
        if (Preview is null || Hit is null || Path is null) return;
        if (HoldsBall || Throwing) return;
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
            map, FieldingResolver.OutfieldPursuitPositions, Preview, Park, Path, _fielders, ElapsedSeconds, R);
        if (of.Position == GlovePos) return;
        if (!_fielders.TryGetValue(of.Position, out var at)) return;
        var speed = FieldingResolver.ChaseSpeedFt(of.Fielder, Preview.Frozen, R);
        _fielders[of.Position] = FieldingResolver.StepToward(at.X, at.Z, of.Route.X, of.Route.Z, speed, dt, Park, R);
    }

    void TickCoverBags(double dt)
    {
        if (Preview is null && !StealPhase) return;
        var chase = R.Fielding.Chase;
        foreach (var pos in new[] { "1B", "2B", "3B", "C" })
        {
            if (pos == GlovePos) continue;
            if (!_fielders.TryGetValue(pos, out var at)) continue;
            var goal = FieldAssist.CoverSpot(pos);
            var dx = goal.X - at.X;
            var dz = goal.Z - at.Z;
            var dist = Math.Sqrt(dx * dx + dz * dz);
            if (dist < chase.CoverStopFt) continue;
            var step = Math.Min(dist, chase.CoverFtPerSec * dt);
            _fielders[pos] = (at.X + dx / dist * step, at.Z + dz / dist * step);
        }
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
        if (Preview is not null && Path is not null)
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
            var choice = FieldingPursuit.Choose(map, positions, Preview, Park, Path, _fielders, ElapsedSeconds, R);
            pick = (choice.Fielder, choice.Position);
        }
        else
            pick = FieldingResolver.PlayGlove(map, BallX, BallZ, _fielders, R);
        if (pick.Pos == GlovePos) return;
        _fielders[GlovePos] = (GloveX, GloveZ);
        GlovePos = pick.Pos;
        var at = _fielders[GlovePos];
        GloveX = at.X;
        GloveZ = at.Z;
    }

    (double X, double Z) SwitchAim(FieldingPreview? pre)
    {
        if (pre is not null && Path is not null)
        {
            var map = Assigned();
            var who = map.TryGetValue(GlovePos, out var fielder) ? fielder : pre.Fielder;
            var speed = FieldingResolver.ChaseSpeedFt(who, pre.Frozen, R);
            var route = FieldingPursuit.Plan(pre, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R);
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
        _fielders[GlovePos] = (GloveX, GloveZ);
        GlovePos = next;
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

    double CatchWindow(Dictionary<string, Character> map)
    {
        var who = map.TryGetValue(GlovePos, out var c) ? c : Preview!.Fielder;
        var radius = FieldingResolver.CatchRadiusFt(who, Preview is not null ? Park : null, R);
        if (Preview is not null && FlyCatch.IsFly(Preview))
            radius += FieldAbilities.FlyRangeBonus(who, R);
        return FieldingResolver.CatchWindowFt(radius, DiveT > 0, JumpT > 0, R);
    }

    static string PosOf(Dictionary<string, Character> map, Character who)
    {
        foreach (var kv in map)
            if (kv.Value.Id == who.Id) return kv.Key;
        return "";
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
        ForceRecorded ? 1 : InPlay.DefaultGroundBag(_match.First is not null, _match.Second is not null, _match.Third is not null);

    LivePlayCommandResult? BeginPlayerThrowOrCommit(Dictionary<string, Character> map, LivePadInput pad)
    {
        var hopperCaught = Preview is not null && Preview.Grounder && HoldsBall;
        var def = DefaultBag();
        ThrowBag = InPlay.CommitBag(ThrowBag, hopperCaught, pad.Cutoff, def);
        if (!HoldsBall)
        {
            // Dead balls finish through the shared deadline before ownership dispatch.
            if (InPlay.HasDeadBallResult(LiveKind())) return null;
            return Commit();
        }
        if (ThrowBag <= 0)
        {
            if (pad.Cutoff && hopperCaught)
            {
                if (PlayerFielding)
                    ThrowBag = def is >= 1 and <= 4 ? def : 1;
                else if (StartGroundRelays())
                    return null;
            }
            if (ThrowBag <= 0)
                return null;
        }
        var key = FieldAssist.CoverKey(ThrowBag);
        map.TryGetValue(key, out var cut);
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : Preview!.Fielder;
        ThrowResult? thr = null;
        if (cut is not null) thr = _match.ThrowBetween(from, cut);
        ArmedThrow = thr;
        ArmedCut = cut;
        if (thr is not null)
        {
            _relayBags = [ThrowBag];
            _relayI = 0;
            BeginThrow(thr, cut, ThrowBag);
            return null;
        }
        return Commit();
    }

    void BeginThrow(ThrowResult thr, Character? cut, int bag)
    {
        Throwing = true;
        ThrowT = 0;
        ThrowBag = bag;
        CoverPos = FieldAssist.CoverKey(bag);
        var throwRules = R.Fielding.Throw;
        ThrowFrom = (GloveX, throwRules.HandHeightFt, GloveZ);
        TakeCoverAfterThrow(bag);
        var to = cut is not null && _fielders.TryGetValue(PosOf(Assigned(), cut), out var at) && !string.IsNullOrEmpty(PosOf(Assigned(), cut))
            ? (at.X, at.Z)
            : BagXZ(bag);
        ThrowTo = (to.X, throwRules.BagHeightFt, to.Z);
        ThrowDur = InPlay.ThrowFlightSec(thr, R);
        _events.Add(LiveEvent.ThrowPop);
    }

    void TakeCoverAfterThrow(int bag)
    {
        ThrowFromPos = GlovePos;
        // Spec A.4 #45: the human glove still hands its body to the cover here. P4 keeps the thrower put.
        var cover = FieldAssist.AfterThrowPos(GlovePos, bag);
        if (string.IsNullOrEmpty(cover) || cover == GlovePos) return;
        _fielders[GlovePos] = (GloveX, GloveZ);
        GlovePos = cover;
        if (_fielders.TryGetValue(GlovePos, out var at))
        {
            GloveX = at.X;
            GloveZ = at.Z;
        }
        else
        {
            var spot = FieldAssist.CoverSpot(GlovePos);
            GloveX = spot.X;
            GloveZ = spot.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
        }
    }

    bool StartGroundRelays()
    {
        if (_relayBags is not null) return false;
        if (Hit is null || Field is null) return false;
        var beats = InPlay.BatterBeatsThrow(_match.Batter, Hit, Field, Dash01, R);
        _relayBags = InPlay.GroundThrowBags(
            _match.First is not null, _match.Second is not null, _match.Third is not null, beats);
        _relayI = 0;
        if (_relayBags.Length == 0) return false;
        return FireRelay();
    }

    bool FireRelay()
    {
        if (_relayBags is null || _relayI >= _relayBags.Length) return false;
        var map = Assigned();
        var bag = _relayBags[_relayI];
        map.TryGetValue(FieldAssist.CoverKey(bag), out var cut);
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : Preview?.Fielder;
        var thr = Field?.Throw;
        if (thr is null && from is not null && cut is not null)
            thr = _match.ThrowBetween(from, cut);
        if (cut is null && thr is null) return false;
        ArmedThrow = thr;
        ArmedCut = cut;
        BeginThrow(thr ?? new ThrowResult(Chemistry.Neutral, 1.0, false), cut, bag);
        return true;
    }

    /// <summary>The ball lands at the bag. True when the play is decided or waiting on the next press.</summary>
    bool OnThrowArrived(out LivePlayCommandResult result)
    {
        var bag = ThrowBag;
        var dest = BagXZ(bag);
        GloveX = dest.X;
        GloveZ = dest.Z;
        var cover = FieldAssist.CoverKey(bag);
        if (!string.IsNullOrEmpty(cover))
        {
            GlovePos = cover;
            _fielders[GlovePos] = (GloveX, GloveZ);
        }
        Throwing = false;
        CatchGlove();

        InPlay.GroundThrowStep? step = null;
        if (bag is >= 1 and <= 4)
        {
            var close = ClosePlay.Offered(bag, Forces, _match.Second is not null, _match.Third is not null);
            if (!close)
            {
                var landed = ApplyThrow(bag, RelayBeats(bag), PlayFielder());
                step = landed.Throw;
                if (step is { } s && !string.IsNullOrEmpty(s.Caption))
                    Sub = s.Caption;
                MaybeStampCloseSafe(bag);
            }
        }

        if (step is { } decided && WaitForNextThrow(decided))
        {
            result = new LivePlayCommandResult(Snapshot, step);
            return true;
        }
        if (!PlayerFielding && AdvanceRelay())
        {
            result = new LivePlayCommandResult(Snapshot, step);
            return true;
        }
        result = new LivePlayCommandResult(Snapshot, step);
        return false;
    }

    void MaybeStampCloseSafe(int bag)
    {
        double needed;
        if (bag == 1) needed = InPlay.HomeToFirstSec(_match.Batter, Dash01, R);
        else if (bag == 2 && _match.First is not null) needed = InPlay.BagToBagSec(_match.First, R);
        else if (bag == 3 && _match.Second is not null) needed = InPlay.BagToBagSec(_match.Second, R);
        else if (bag == 4 && _match.Third is not null) needed = InPlay.BagToBagSec(_match.Third, R);
        else return;
        if (InPlay.CloseSafe(ElapsedSeconds, needed, R))
            _events.Add(LiveEvent.StampSafe);
    }

    /// <summary>
    /// Whether the runner beat this throw. Spec A.5 #52 / §9.1: the human seat reads the live clock,
    /// the CPU seat the resolver's closed form. P3/P5 replace both with runner positions.
    /// </summary>
    bool RelayBeats(int bag)
    {
        if (PlayerFielding)
        {
            if (bag == 1)
                return ElapsedSeconds >= InPlay.HomeToFirstSec(_match.Batter, Dash01, R);
            if (bag == 2 && _match.First is not null)
                return ElapsedSeconds >= InPlay.BagToBagSec(_match.First, R);
            if (bag == 3 && _match.Second is not null)
                return ElapsedSeconds >= InPlay.BagToBagSec(_match.Second, R);
            if (bag == 4 && _match.Third is not null)
                return ElapsedSeconds >= InPlay.BagToBagSec(_match.Third, R);
            return false;
        }
        if (Hit is null || Field is null) return false;
        if (bag == 1)
            return InPlay.BatterBeatsThrow(_match.Batter, Hit, Field, Dash01, R);
        if (bag == 3 && _match.Second is not null)
            return InPlay.RunnerBeatsTag(_match.Second, Hit, Field, 3, R);
        if (bag == 4 && _match.Third is not null)
            return InPlay.RunnerBeatsTag(_match.Third, Hit, Field, 4, R);
        if (bag == 2 && _match.First is not null)
            return InPlay.RunnerBeatsTag(_match.First, Hit, Field, 2, R);
        return false;
    }

    Character PlayFielder()
    {
        if (Field?.Fielder is not null) return Field.Fielder;
        return Preview is not null ? Preview.Fielder : _match.Pitcher;
    }

    bool WaitForNextThrow(InPlay.GroundThrowStep step)
    {
        if (!PlayerFielding) return false;
        if (step.PlayOver || step.NextDefaultBag <= 0) return false;
        AwaitingRelay = true;
        ThrowBag = step.NextDefaultBag;
        _relayBags = null;
        return true;
    }

    bool AdvanceRelay()
    {
        if (_relayBags is null || _relayI + 1 >= _relayBags.Length) return false;
        _relayI++;
        return FireRelay();
    }

    static (double X, double Z) BagXZ(int bag) => Diamond.Bag(bag);

    // ---------------------------------------------------------------------------------
    // Commit
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult Commit()
    {
        if (Pitch is null || Swing is null || Hit is null)
            return new LivePlayCommandResult(Snapshot, FlightDone: true);
        FieldingResult? result = null;
        if (PlayerFielding && Preview is not null)
            result = BuildPlayerResult();
        else if (Field is not null)
            result = Field;
        if (result is null)
            return new LivePlayCommandResult(Snapshot, FlightDone: true);
        LastFieldResult = result;
        var play = _match.FinishAtBat(Pitch, Swing, Hit, result);
        return new LivePlayCommandResult(Snapshot, CompletedPlay: play);
    }

    FieldingResult BuildPlayerResult()
    {
        var pre = Preview!;
        var hit = Hit;
        var map = Assigned();
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : pre.Fielder;
        var cut = ArmedCut;
        var thr = ArmedThrow;
        var bag = ThrowBag;
        if (thr is null && HoldsBall && bag > 0)
        {
            map.TryGetValue(FieldAssist.CoverKey(bag), out cut);
            if (cut is not null) thr = _match.ThrowBetween(from, cut);
        }
        if (HoldsBall)
        {
            if (LiveKind() == PlayKind.Foul)
                return new FieldingResult(PlayKind.Foul, from, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace);
            if (Bobbling || PlayerBobble)
                return new FieldingResult(PlayKind.Single, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy, Bobble: true);
            // The catch can be committed a frame after hang. Once the glove owns an aerial ball
            // it is still a fly out; do not reclassify the catch from carry distance.
            var kind = FlyCatch.PlayerKind(true, pre, hit, inAir: true, R, foul: FoulNow);
            var knock = pre.Grounder && hit is not null ? InPlay.KnockbackSec(InPlay.Energy(hit, R), from, R) : 0;
            var feat = kind == PlayKind.FlyOut && hit is not null
                ? FieldingResolver.PlayerCatchFeat(pre, Park, Buddy, CatchJump)
                : DefensiveFeat.None;
            return new FieldingResult(kind, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy,
                KnockbackSec: knock, Feat: feat);
        }
        var miss = FlyCatch.PlayerKind(false, pre, hit, rules: R, foul: FoulNow);
        return new FieldingResult(miss, from, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, Buddy: pre.Buddy,
            GroundRule: pre.Ball is { GroundRule: true });
    }

    // ---------------------------------------------------------------------------------
    // Catch, bobble, tag
    // ---------------------------------------------------------------------------------

    void CatchGlove()
    {
        if (!Caught && !_gloved) _events.Add(LiveEvent.Glove);
        Caught = true;
        _gloved = true;
    }

    /// <summary>
    /// The glove takes the batted ball, and that touch makes the fair / foul call (§5.6): in the
    /// air it is a catch (an out wherever the ball was); on the ground it is fair or foul by
    /// where the ball is. Thrown balls and the steal phase go through <see cref="CatchGlove"/>.
    /// </summary>
    void TakeBattedBall()
    {
        var first = !Caught;
        CatchGlove();
        if (!first || _call != FairFoulCall.Undecided || Preview is null) return;
        // In the air = before the ball's first ground contact (the landing mark), read off the path, not a height.
        var inTheAir = !Preview.Grounder && ElapsedSeconds <= Hang + 1e-6;
        _call = inTheAir ? FairFoulCall.Caught
            : FieldBounds.IsFair(BallX, BallZ) ? FairFoulCall.Fair
            : FairFoulCall.Foul;
    }

    void ArmRecoil()
    {
        if (_recoilArmed || Preview is null || !Preview.Grounder || Buddy) return;
        _recoilArmed = true;
        var bobble = false;
        var knock = 0.0;
        if (Field is not null)
        {
            bobble = Field.Bobble;
            knock = Field.KnockbackSec;
        }
        else if (Hit is not null)
        {
            var map = Assigned();
            var who = map.TryGetValue(GlovePos, out var g) ? g : Preview.Fielder;
            var energy = InPlay.Energy(Hit, R);
            // The one seeded stream (S-92). The client used to re-roll this with an ad-hoc Random.
            bobble = _match.RollBobble(energy, who);
            knock = InPlay.KnockbackSec(energy, who, R);
            PlayerBobble = bobble;
        }
        var rules = R.Fielding.Bobble;
        if (bobble)
        {
            Bobbling = true;
            RecoilT = rules.FumbleSec;
            var ax = BallX - GloveX;
            var az = BallZ - GloveZ;
            if (ax * ax + az * az < 0.4) { ax = 0; az = 1; }
            var len = Math.Sqrt(ax * ax + az * az);
            BallX = GloveX + ax / len * rules.ScatterFt;
            BallY = rules.ScatterBallY;
            BallZ = GloveZ + az / len * rules.ScatterFt;
        }
        else if (knock > R.Fielding.Knockback.MinSec)
            RecoilT = knock;
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

    PlayKind LiveKind()
    {
        // A touch that called it foul is dead whatever the resolver said; a catch of a foul fly is an out.
        if (_call == FairFoulCall.Foul) return PlayKind.Foul;
        if (Field is not null)
            return _call == FairFoulCall.Caught && Field.Kind == PlayKind.Foul ? PlayKind.FlyOut : Field.Kind;
        if (Preview is null || Hit is null) return PlayKind.Single;
        var inAir = HoldsBall || ElapsedSeconds < Hang;
        return FlyCatch.PlayerKind(HoldsBall, Preview, Hit, inAir, R, foul: FoulNow);
    }

    // ---------------------------------------------------------------------------------
    // Close play (spec §9.6 as shipped: the mash runs on every unforced 3B/home throw; P5 adds the margin)
    // ---------------------------------------------------------------------------------

    bool TryBeginClosePlay()
    {
        if (!ClosePlay.Offered(ThrowBag, Forces, _match.Second is not null, _match.Third is not null))
            return false;
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

        var runner = CloseBag == 4 ? _match.Third : _match.Second;
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
        _match.ClosePlaySafe = safe;
        Sub = ClosePlay.Caption(CloseBag, safe);
        if (safe) _events.Add(LiveEvent.StampSafe);
        ApplyThrow(CloseBag, safe, PlayFielder());
        InClosePlay = false;
        CloseIcon = false;
        return Commit();
    }

    // ---------------------------------------------------------------------------------
    // Items (offense verbs decided in the client; the ball's result lives here)
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult ApplyItem(LivePlayCommand command)
    {
        if (Field is null || string.IsNullOrEmpty(command.ItemId)) return new LivePlayCommandResult(Snapshot);
        if (FoulNow) return new LivePlayCommandResult(Snapshot); // a foul cannot become a hit (§7.11)
        Field = _match.ThrowItem(Field, command.ItemId, command.Fielder);
        if (Field.Kind is not (PlayKind.FlyOut or PlayKind.GroundOut))
            Caught = false;
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult SmashItem()
    {
        if (Field is not null)
            Field = ErrorItems.Smash(Field, Preview is not null && Preview.Grounder);
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
            ? StealThrow.RunnerRemainSec(state.Who, state.Lead01, R)
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
        map.TryGetValue(FieldAssist.CoverKey(ThrowBag), out var cut);
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : (map.TryGetValue("C", out var catcher) ? catcher : null);
        ThrowResult? thr = null;
        if (from is not null && cut is not null) thr = _match.ThrowBetween(from, cut);
        ArmedThrow = thr ?? new ThrowResult(Chemistry.Neutral, 1.0, false);
        ArmedCut = cut;
        BeginThrow(ArmedThrow, cut, ThrowBag);
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
