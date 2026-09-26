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
    bool Freeze = false,
    /// <summary>RB on the defense pad (#723, F693-03-throw-cancel): cancels a queued onward throw. Read as a fresh press, never a held shoulder.</summary>
    bool Cancel = false,
    /// <summary>Which bound device this pad is (#718): the pursuit stick's calibration and arming are per device. 0 for a single seat.</summary>
    int Device = 0,
    RunnerOrderInput? Orders = null,
    bool ExplicitTarget = false,
    bool? CloseResponse = null)
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
    Bobble,
    /// <summary>A runner is caught between bags with the ball in range (§9.7): the rundown began.</summary>
    Rundown,
    /// <summary>An out was recorded this frame (§15, #690): OUT / DIVE / JUMP at the glove or bag.</summary>
    StampOut,
    /// <summary>A runner crossed the plate this frame (§15, #690): SCORE at home.</summary>
    StampScore,
    /// <summary>A human's onward-throw press was remembered while the ball flies to their receiver (#723): the queue tell.</summary>
    ThrowQueued,
    /// <summary>The remembered press was cancelled or expired (#723): the tell clears.</summary>
    ThrowQueueCleared,
    /// <summary>A dive was committed — East, or the CPU's deliberate choice — and its recovery is owed (#719).</summary>
    DiveCommit,
    /// <summary>The glove took off on a normal jump (#719): the airborne clock started this frame.</summary>
    JumpTakeoff,
    /// <summary>A hard ball's take cost the hands (#720): the ordinary impact recoil began this frame — the brace and the skid.</summary>
    ImpactRecoil,
    /// <summary>
    /// A body touched a park's status volume this frame and runs slowed (FR-07, FD-08-R2; F4-b): who, which instance and until
    /// when are <see cref="LivePlaySystem.Slows"/>, one <see cref="BodySlowed"/> per touch.
    /// </summary>
    BodySlowed,
    /// <summary>The ball went into a redirect and came out of another this frame (F4-c): <see cref="LivePlaySystem.RedirectsThisPlay"/>.</summary>
    BallRedirected,
    /// <summary>The ball hit a reward target this frame (F4-c): <see cref="LivePlaySystem.RewardThisPlay"/>.</summary>
    RewardHit,
    /// <summary>The ball caromed off a solid body or a mover this frame (F4-f): <see cref="LivePlaySystem.CaromsThisPlay"/>.</summary>
    BodyCarom,
    /// <summary>A throw command was accepted; transfer begins now, ThrowPop marks actual release.</summary>
    ThrowCommitted,
    /// <summary>A surge band's wave started carrying the rolling ball this frame (§14): <see cref="LivePlaySystem.CarriesThisPlay"/>.</summary>
    BallCarried,
    /// <summary>A drifting disc started pushing the ball in flight this frame (§14): <see cref="LivePlaySystem.PushesThisPlay"/>.</summary>
    BallPushed
}

/// <summary>
/// The live ball between contact and Time: gloves, catches, throws, the relay chain, the
/// close-play race, the bobble, and the runner play (the steal, the pickoff; spec §0.1, §11, A.7). Unity supplies the
/// pads once per frame and draws the state; every decision is made here from positions,
/// clocks, and <see cref="Match"/>'s seeded random stream. Nothing here is a roll on the
/// outcome: a glove reaches the ball or it does not, a throw lands inside the cover's reach or
/// it does not, and the runner bodies race the one throw clock (§8.3, §8.5, §8.8).
/// </summary>
public sealed partial class LivePlaySystem
{
    readonly Dictionary<string, (double X, double Z)> _fielders = new();
    readonly Dictionary<string, double> _readyAt = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>The human glove's lockouts (§8.2): the reference numbers at every difficulty rung.</summary>
    readonly Dictionary<string, double> _readyHuman = new(StringComparer.OrdinalIgnoreCase);
    readonly List<LiveEvent> _events = [];
    readonly List<LiveStamp> _stamps = [];
    readonly List<LiveStamp> _stampsThisPlay = [];
    /// <summary>The park's status volumes on the bodies that touch them (F4-b, #896): the touch, the per-body time, the slow.</summary>
    readonly BodySlows _bodySlows = new();

    /// <summary>Where this park's fielders start (FD-07, F2-d): the global infield, the park's outfield (<see cref="OutfieldStarts"/>).</summary>
    IReadOnlyDictionary<string, (double X, double Z)> Starts => OutfieldStarts.Of(Park, R);
    readonly BallHazards _ballHazards = new();
    IReadOnlyList<SolidBody> _solids = [];
    readonly List<BodyCarom> _caromsThisPlay = [];
    int? _caromLock;
    readonly List<BallRedirected> _redirectsThisPlay = [];
    RewardHit? _reward;
    /// <summary>The fielding positions whose body a volume never slows (Burrow), read with the touches each frame: their route goes straight (F4-g).</summary>
    readonly HashSet<string> _routeImmune = new(StringComparer.OrdinalIgnoreCase);
    readonly List<BodySlowed> _slows = [];
    readonly List<BodySlowed> _slowsThisPlay = [];
    readonly List<LiveFact> _facts = [];
    readonly List<LiveFact> _factsThisPlay = [];
    readonly HashSet<Runner> _scoreTold = [];
    bool _gloved;
    readonly GloveRecoil _recoil = new();
    bool _wallCued;
    FairFoulCall _call;
    readonly CloseContest _close = new();

    // The facts of this play the result carries (§8.6, §8.5).
    bool _bobbled;
    bool _sailed;
    Character? _firstGlove;

    // The hand-off coast (§8.9): the body the ring left keeps the glove's last velocity for chase.handoffCoastSec, then brakes.
    readonly HandoffCoast _coast = new();
    readonly GloveDive _dive = new();
    /// <summary>Each body's velocity under the response law (#718) and this frame's step record.</summary>
    readonly BodyResponse _response = new();

    // A ball on the ground in nobody's glove and off its batted path: a fumble, an overthrow, a drop at an uncovered bag.
    readonly LooseBallMotion _looseMotion = new();

    // The throw in flight: who threw it, who receives it, who backs it up.
    string _throwerPos = "";
    readonly ThrowSupport _support = new();
    // The human seat's pursuit stick (#718): one model per bound device, read once a frame; the arming outlives the play.
    readonly Dictionary<int, PursuitStick> _sticks = new();
    StickRead _stick = StickRead.Assist;
    int _stickDevice;
    (int Inning, bool Top)? _stickHalf;
    /// <summary>The glove holds a ball it received cleanly from a teammate's throw (#723): Snap Throw's eligibility. A pickup, a bobble, a sail or a hand-off clears it.</summary>
    bool _receivedClean;
    /// <summary>The human's remembered throw presses (#723): the approach, the relay and the recovery.</summary>
    readonly ThrowCommands _commands = new();

    // The CPU glove's decision clock (§8.8): the throw waits for the reaction, then the table runs once per possession.
    readonly CpuThrowClock _cpuClock = new();

    // Items (§12): one body kept off the ball for a beat, or every ball on the dirt hopping.
    readonly LiveItems _items = new();
    /// <summary>A thrown item landed this play and took effect (<see cref="LiveItems.Landed"/>).</summary>
    public bool ItemLanded => _items.Landed;
    /// <summary>
    /// The named item's field effect is in force now (§12): the peel is down, the rocket's target is still dazed, the
    /// dirt is hopping. <paramref name="targetId"/> matters only for the item that targets a body.
    /// </summary>
    public bool ItemEffectActive(string item, string targetId) => item switch
    {
        "banana" => _items.PeelDown,
        "rocket" => Field?.ItemTarget is { } target && target.Id == targetId
            && _items.OffLeft(PosOf(Assigned(), target)) > 0,
        "pow" => _items.Hopping,
        _ => false
    };

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
    /// <summary>The batted ball has cleared the bat this play (§7.11, <see cref="FlyCatch.OffTheBat"/>): a glove may take it.</summary>
    public bool OffTheBat { get; private set; }
    /// <summary>The fair / foul call so far (§5.6): the untouched path's verdict until a glove touches the ball.</summary>
    public FairFoulCall Call => _call;
    /// <summary>Foul as things stand: called foul on a touch, or untouched on a path the flight says is foul.</summary>
    public bool FoulNow => _call == FairFoulCall.Foul || (_call == FairFoulCall.Undecided && Ball is { Foul: true });
    /// <summary>A dead-ball runner play (§11.3, §11.4): the catcher's throw after a take or a miss, or the pickoff from the rubber. No batted ball, no batter body.</summary>
    public bool RunnerPlay { get; private set; }
    /// <summary>The bag the pickoff was thrown to on a pickoff play; 0 on the catcher's play.</summary>
    public int PickoffBag { get; private set; }
    /// <summary>The first bag a throw went to this play: the endpoint the result names (§15).</summary>
    public int FirstThrowBag { get; private set; }

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
    /// <summary>The throw's clock and line (§8.5).</summary>
    readonly ThrowFlight _flight = new();
    /// <summary>Seconds since the throw's command (<see cref="ThrowFlight.T"/>).</summary>
    public double ThrowT => _flight.T;
    /// <summary>Command to landing (<see cref="ThrowFlight.Duration"/>).</summary>
    public double ThrowDur => _flight.Duration;
    /// <summary>Command-to-release preparation, part of ThrowDur, never part of ball flight (<see cref="ThrowFlight.ReleaseSec"/>).</summary>
    public double ThrowReleaseSec => _flight.ReleaseSec;
    public bool ThrowPreparing => Throwing && !_flight.Released;
    public bool ThrowInFlight => Throwing && _flight.Released;
    public double ThrowFlight01 => _flight.Flight01;
    public (double X, double Y, double Z) ThrowFrom => _flight.From;
    public (double X, double Y, double Z) ThrowTo => _flight.To;
    public int ThrowBag { get; private set; }
    public ThrowResult? ArmedThrow { get; private set; }
    public Character? ArmedCut { get; private set; }
    public string CoverPos { get; private set; } = "";
    /// <summary>
    /// The ball X the cover map is read at for the whole play (§8.7): the landing X of a batted ball,
    /// the glove's spot when a runner play forms (the rubber on a pickoff, the plate on the catcher's
    /// throw). Fixed once so the formation, the throw's receiver, the cover walk, the CPU table, and the
    /// rundown all name the same body for a bag; only the glove exclusion moves with the ball (#640).
    /// </summary>
    public double CoverBallX { get; private set; }
    public string ThrowFromPos { get; private set; } = "";
    public string SwitchPos { get; private set; } = "";
    public string BuddyPos { get; private set; } = "";
    public bool BuddyWindow { get; private set; }
    /// <summary>The dive's arm window left (<see cref="GloveDive.ArmT"/>).</summary>
    public double DiveT => _dive.ArmT;
    public double JumpT { get; private set; }
    public double SwapLock { get; private set; }
    /// <summary>The glove's impact recovery left (<see cref="GloveRecoil.T"/>).</summary>
    public double RecoilT => _recoil.T;
    /// <summary>The ordinary impact recoil (#720) is running (<see cref="GloveRecoil.Active"/>).</summary>
    public bool ImpactRecoil => _recoil.Active;
    /// <summary>The full length the impact recovery started from (<see cref="GloveRecoil.Duration"/>).</summary>
    public double RecoilDur => _recoil.Duration;
    /// <summary>The ball's speed the frame before the glove took it (<see cref="GloveRecoil.IncomingFtPerSec"/>).</summary>
    public double IncomingFtPerSec => _recoil.IncomingFtPerSec;
    /// <summary>The bobble's stun (#721, F693-02-bobble-stun-duration): this body neither steers nor takes until it runs out; the ball and every other body stay live.</summary>
    public double StunT { get; private set; }
    /// <summary>The body paying <see cref="StunT"/>, held across selection.</summary>
    public string StunPos { get; private set; } = "";
    /// <summary>The normalized difficulty of the last ground-ball take (F693-02-awkward-hop-difficulty-source): 0 for a routine one. Sampled on both tables.</summary>
    public double HopDifficulty { get; private set; }
    /// <summary>The chance the last take rolled against (F693-02-ordinary-handling-chance-curve); 0 = no roll at all.</summary>
    public double HandlingChance { get; private set; }
    /// <summary>The last failed take got past the body (#721 slice 2): the ball carried on as a batted ball, not a local bobble.</summary>
    public bool Deflected { get; private set; }
    /// <summary>How squarely the ring met the ball at the last failed take, 1 at the body, 0 at the edge; the branch's fact.</summary>
    public double ErrorObstruction { get; private set; }
    public bool Bobbling { get; private set; }
    public bool PlayerBobble { get; private set; }
    public bool CatchDive { get; private set; }
    /// <summary>The dive's recovery still owed by <see cref="DivingPos"/> (<see cref="GloveDive.RecoveryT"/>): no move, no throw until it is 0.</summary>
    public double DiveRecoveryT => _dive.RecoveryT;
    /// <summary>The body paying <see cref="DiveRecoveryT"/> (<see cref="GloveDive.DivingPos"/>).</summary>
    public string DivingPos => _dive.DivingPos;
    // The live ball as the CPU reads it for a dive (#719): last frame's position and the velocity between frames, no resolved path.
    (double X, double Y, double Z)? _ballPrev;
    (double X, double Y, double Z) _ballVel;
    readonly NormalJump _jump = new();
    /// <summary>The glove is in the air on a normal jump (<see cref="NormalJump.Airborne"/>).</summary>
    public bool Airborne => _jump.Airborne;
    /// <summary>Seconds since takeoff while <see cref="Airborne"/> (<see cref="NormalJump.AirT"/>).</summary>
    public double JumpAirT => _jump.AirT;
    /// <summary>The body's root rise this frame (<see cref="NormalJump.HeightFt"/>).</summary>
    public double JumpHeightFt => _jump.HeightFt;
    /// <summary>The current jump's peak rise (Lily Leap's, or <c>catch.jumpRiseFt</c>); 0 on the ground.</summary>
    public double JumpRiseFt => _jump.RiseFt;
    /// <summary>A grounded West press waiting on its first eligible instant (<see cref="NormalJump.Pending"/>).</summary>
    public bool JumpPending => _jump.Pending;
    public bool CatchJump { get; private set; }
    /// <summary>A close play's mash contest is running (<see cref="CloseContest.Active"/>).</summary>
    public bool InClosePlay => _close.Active;
    /// <summary>The bag of the play's close play (<see cref="CloseContest.Bag"/>).</summary>
    public int CloseBag => _close.Bag;
    /// <summary>The close play's icon is up (<see cref="CloseContest.Icon"/>).</summary>
    public bool CloseIcon => _close.Icon;
    /// <summary>A runner is caught between bags with a glove holding the ball in range (§9.7).</summary>
    public bool InRundown => RundownRunner is not null;
    /// <summary>The body in the rundown this frame, or null.</summary>
    public Runner? RundownRunner { get; private set; }
    /// <summary>The CPU glove is walking to this force bag to step on it (§10.4, S-41); 0 when throwing instead.</summary>
    int _cpuWalkBag;
    bool _rundownCued;
    public bool AwaitingRelay { get; private set; }
    public double Dash01 { get; private set; }
    /// <summary>The pitch (or the pickoff beat) this runner play completes.</summary>
    public PlayEvent? StealPitch { get; private set; }
    /// <summary>The ball is on the ground in nobody's glove, off its batted path (a fumble, an overthrow, a drop).</summary>
    public bool LooseBall => _looseMotion.Active;
    /// <summary>A throw is hanging at an uncovered bag, waiting for the cover (§8.5).</summary>
    public bool Lobbing => Throwing && _flight.LobT > 0;
    /// <summary>A throw missed its cover this play (§8.5): the ERROR.</summary>
    public bool ThrowSailed => _sailed;
    /// <summary>HUD sub-caption for the last live decision. Narrated, never read back.</summary>
    public string Sub { get; private set; } = "";
    /// <summary>Cues raised by the most recent command.</summary>
    public IReadOnlyList<LiveEvent> Events => _events;

    /// <summary>Live tells raised by the most recent command (§15, #690).</summary>
    public IReadOnlyList<LiveStamp> Stamps => _stamps;

    /// <summary>Every live tell this play, in order. Cleared on the next Begin, not at Time.</summary>
    public IReadOnlyList<LiveStamp> StampsThisPlay => _stampsThisPlay;

    /// <summary>The status-volume touches of the most recent command (F4-b, #896): one <see cref="BodySlowed"/> per body per volume it entered.</summary>
    public IReadOnlyList<BodySlowed> Slows => _slows;

    /// <summary>Every status-volume touch this play, in order. Cleared on the next Begin, not at Time.</summary>
    public IReadOnlyList<BodySlowed> SlowsThisPlay => _slowsThisPlay;
    /// <summary>What bodies actually did this frame, as typed facts (<see cref="LiveFact"/>).</summary>
    public IReadOnlyList<LiveFact> Facts => _facts;
    /// <summary>Every fact of this play that lasts the play (<see cref="ReachBonusTake"/>), kept until the next live ball.</summary>
    public IReadOnlyList<LiveFact> FactsThisPlay => _factsThisPlay;

    void RecordFact(LiveFact fact)
    {
        _facts.Add(fact);
        _factsThisPlay.Add(fact);
    }

    /// <summary>The volumes this play's bodies are tested against (F4-b): the park's status volumes, the night disc at night; none with hazards off.</summary>
    public IReadOnlyList<StatusVolume> StatusVolumes => _bodySlows.Volumes;

    /// <summary>The fielder at <paramref name="pos"/> runs slowed by a status volume this frame (F4-b): inside one, or inside its time.</summary>
    public bool IsSlowed(string pos) => _bodySlows.Slowed(pos);

    /// <summary>This runner runs slowed by a status volume this frame (F4-b).</summary>
    public bool IsSlowed(Runner runner) => _bodySlows.Slowed(runner);

    /// <summary>The glove owns the ball (a catch or a buddy jump).</summary>
    public bool HoldsBall => Caught || Buddy;

    /// <summary>Play seconds the batter had been squared at the plate time (§7.3); 0 with no square.</summary>
    public double SquareSec => Swing?.SquareSec ?? 0;
    /// <summary>The bunt alignment is on (§7.3): the batter squared, or the ball is a bunt; the middle covers first and second.</summary>
    public bool BuntAlignment => BuntDefense.AlignmentOn(Swing, Ball?.Shape);

    /// <summary>
    /// Every body on the field where it stands (§10.6, #574): each glove from the live map (the glove
    /// on the ball at its own spot), each live runner on the path. Complete captures this before the
    /// field resets so the result beat has one position source and nobody snaps to a table spot.
    /// </summary>
    public IReadOnlyList<FieldBody> BodiesNow()
    {
        var list = new List<FieldBody>();
        foreach (var kv in Assigned())
        {
            (double X, double Z) at = kv.Key == GlovePos && Active ? (GloveX, GloveZ)
                : _fielders.TryGetValue(kv.Key, out var p) ? p
                : Starts[kv.Key];
            list.Add(new FieldBody(kv.Key, kv.Value, at.X, at.Z));
        }
        foreach (var r in Runners)
        {
            if (!r.Live) continue;
            var (x, z) = r.Position;
            list.Add(new FieldBody(FieldBody.Runner, r.Who, x, z));
        }
        return list;
    }

    /// <summary>
    /// The bag a runner play is about (§11.3, §11.4; the camera sits on it, §15): the throw's bag while
    /// the ball is in the air, the pickoff bag, else the lead bag a body that broke is bound for.
    /// </summary>
    public int RunnerPlayBag
    {
        get
        {
            if (Throwing && ThrowBag > 0) return ThrowBag;
            if (PickoffBag > 0) return PickoffBag;
            var lead = 0;
            foreach (var r in Runners)
                if (r.Live && (r.Broke || r.Phase == RunnerPhase.Stealing) && r.DestBag > lead) lead = r.DestBag;
            return lead;
        }
    }
    /// <summary>Human on the glove, or a human seat that owns the throw: their pad is the source.</summary>
    public LivePlayCommandSource Source =>
        PlayerFielding || Seats.HumanOwnsThrow ? LivePlayCommandSource.Human : LivePlayCommandSource.Cpu;

    RulesTable R => _match.Rules;
    /// <summary>The match table's diamond: its bags, rubber and starts (#1067).</summary>
    internal DiamondGeometry Geometry => DiamondGeometry.Of(R);
    Park Park => _match.Park;
    FeelTable Feel => _match.Content.Feel;
    /// <summary>The play's own defense map, read once per live ball: the gloves that started it stay its gloves through the third out and the flip.</summary>
    Dictionary<string, Character> Assigned() => _assigned ??= FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
    Dictionary<string, Character>? _assigned;
    double Hang => Path is null ? (Preview?.HangTimeSec ?? 0) : BallFlight.HangTime(Path, R);
    double Rest => Path is null ? 0 : BallFlight.RestTime(Path);
    /// <summary>The instant a dead ball is decided: a foul at its verdict, anything else at the landing mark.</summary>
    double DeadAt => Ball is not null && LiveKind() == PlayKind.Foul ? Ball.DecidedT : Hang;
    /// <summary>
    /// Play seconds before the body at <paramref name="pos"/> may move (§8.2): the human seat's glove waits the
    /// reference lockout, every CPU-driven body waits it × <c>cpu.reactionMul</c>; a ball in the air caps both at its hang.
    /// </summary>
    double ReadyAt(string pos) => (HumanGlove(pos) ? _readyHuman : _readyAt).TryGetValue(pos, out var t) ? t : 0;
    /// <summary>The body a human defense steers: the play glove (a dead stick lets the CPU run it, and it is still theirs).</summary>
    bool HumanGlove(string pos) => Seats.HumanFields && string.Equals(pos, GlovePos, StringComparison.OrdinalIgnoreCase);
    /// <summary>The body on the ball: the thrower while a throw is in the air (the YOU ring is already on the receiver), else the glove.</summary>
    string OnBallPos => Throwing && !string.IsNullOrEmpty(_throwerPos) ? _throwerPos : GlovePos;
    bool CanMove(string pos) => ElapsedSeconds + 1e-9 >= ReadyAt(pos) && !_dive.Recovering(pos)
                                && !(ImpactRecoil && RecoilT > 0 && pos == GlovePos) && !Stunned(pos);
    /// <summary>The fumbler inside the bobble's stun (#721): no steering, no jump, no dive, no take.</summary>
    bool Stunned(string pos) => StunT > 0 && pos == StunPos;

    // ---------------------------------------------------------------------------------
    // Begin
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult BeginLive(LivePlayCommand command)
    {
        if (command.Hit is null || command.Pitch is null || command.Swing is null)
            return new LivePlayCommandResult(Snapshot);
        ResetField();
        // The play's facts survive a same-tick completed catch; the next live ball clears them.
        _facts.Clear();
        _factsThisPlay.Clear();
        _events.Clear();
        _stamps.Clear();
        _stampsThisPlay.Clear();
        _slows.Clear();
        _slowsThisPlay.Clear();
        _redirectsThisPlay.Clear();
        _reward = null;
        _caromsThisPlay.Clear();
        BeginSurges();
        BeginDrifts();
        _caromLock = null;
        _scoreTold.Clear();
        Pitch = command.Pitch;
        Swing = command.Swing;
        Hit = command.Hit;
        Preview = command.Preview;
        Field = command.Field;
        Seats = command.Seats ?? LiveSeats.CpuOnly;
        // Defensive-role entry (#718, F693-02-pursuit-arming): a new half owes every seat's pursuit stick one neutral observation before it steers.
        var half = (_match.Inning, _match.Top);
        if (_stickHalf != half)
        {
            foreach (var s in _sticks.Values) s.EnterDefense();
            _stickHalf = half;
        }
        Ball = Preview?.Ball ?? BattedBall.Of(Hit, Park, R);
        Path = Ball.Samples;
        BeginFirstHopKick();
        BeginHotBall();
        CoverBallX = Preview?.LandingX ?? Ball.LandingX;
        PlayerFielding = FieldAssist.PlayerStartsOnGlove(Seats.PlayerMustField);
        var airHang = Ball.Shape.OnTheDirt() ? (double?)null : Hang;
        foreach (var kv in FieldingResolver.CpuReactionLockouts(R, airHang, Hit.Class == BattedBallClass.Bunt)) _readyAt[kv.Key] = kv.Value;
        foreach (var kv in FieldingResolver.ReactionLockouts(R, 1, airHang, Hit.Class == BattedBallClass.Bunt)) _readyHuman[kv.Key] = kv.Value;
        InitGloves();
        // The park's status volumes, as this play reads them (F4-b): every body starts outside them, unslowed.
        _bodySlows.Begin(ParkHazards.StatusVolumes(Park, R, _match.Night));
        // The ball's redirects and reward targets (F4-c): read live off the ball, never off where it lands.
        _ballHazards.Begin(Park, _match.Night, R);
        // The solid bodies and movers (F4-f): the ball caroms off them and nobody stands in one.
        _solids = SolidBodies.Of(Park, R);
        var kind = LiveKind();
        var result = Begin(command with { PlayKind = kind });
        if (!Active) return result;
        Dash01 = command.Dash01;
        _match.Dash01 = Dash01;
        BallX = 0;
        BallY = R.Flight.PlateHeightFt;
        BallZ = 0;
        // An item thrown before the ball was live (the headless game) lands on its body after its flight (§12).
        if (Field is { ItemHit: true }) _items.LandAt(R.Batting.Items.FlySec);
        return new LivePlayCommandResult(Snapshot);
    }

    void InitGloves()
    {
        _fielders.Clear();
        foreach (var kv in Assigned())
            _fielders[kv.Key] = Starts[kv.Key];
        // The square (§7.3): the corners crashed and the middle walked to the bags while the pitch was thrown; the
        // live ball starts from those bodies — the same function the presenter drew them from (BuntDefense.Spots).
        if (BuntDefense.Squared(Swing))
            foreach (var kv in BuntDefense.Spots(Assigned(), SquareSec, R))
                _fielders[kv.Key] = kv.Value;
        SwapLock = 0;
        if (Preview is null)
        {
            GlovePos = "P";
            GloveX = Geometry.Rubber.X;
            GloveZ = Geometry.Rubber.Z;
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
        _assigned = null;
        Pitch = null;
        Swing = null;
        Hit = null;
        Preview = null;
        Field = null;
        Path = null;
        Ball = null;
        _kickAt = null;
        _hot = null;
        FlightDone = false;
        OffTheBat = false;
        _call = FairFoulCall.Undecided;
        RunnerPlay = false;
        PickoffBag = 0;
        FirstThrowBag = 0;
        _fielders.Clear();
        _readyAt.Clear();
        _readyHuman.Clear();
        GlovePos = "P";
        GloveX = Geometry.Rubber.X;
        GloveZ = Geometry.Rubber.Z;
        PlayerFielding = false;
        Caught = false;
        Buddy = false;
        _flight.Reset();
        ThrowBag = 0;
        ArmedThrow = null;
        ArmedCut = null;
        CoverPos = "";
        CoverBallX = 0;
        ThrowFromPos = "";
        SwitchPos = "";
        BuddyPos = "";
        BuddyWindow = false;
        JumpT = SwapLock = 0;
        _dive.Reset();
        _recoil.Reset();
        StunT = 0;
        StunPos = "";
        HopDifficulty = 0;
        HandlingChance = 0;
        Deflected = false;
        ErrorObstruction = 0;
        _looseMotion.Reset();
        _ballPrev = null;
        _ballVel = (0, 0, 0);
        _commands.Reset();
        _jump.Reset();
        Bobbling = false;
        PlayerBobble = false;
        CatchDive = CatchJump = false;
        _gloved = false;
        _wallCued = false;
        _bobbled = false;
        _sailed = false;
        _firstGlove = null;
        _coast.Reset();
        _response.Reset();
        _receivedClean = false;
        _throwerPos = "";
        _support.Reset();
        _cpuClock.Restart();
        _items.Reset();
        _bodySlows.Begin([]);
        _ballHazards.Clear();
        _solids = [];
        AwaitingRelay = false;
        _close.Reset();
        RundownRunner = null;
        _cpuWalkBag = 0;
        _rundownCued = false;
        Dash01 = 0;
        StealPitch = null;
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
        _stamps.Clear();
        _slows.Clear();
        _facts.Clear();
        var dt = command.DeltaSeconds;
        // Ownership of a press is decided here, once, from the seats: the offense pad never
        // reaches the gloves and the defense pad never reaches the runners (spec §0.4, #579).
        var field = Seats.OwnedFieldPad(command.FieldPad);
        var run = Seats.OwnedRunPad(command.RunPad);
        if (dt <= 0) return new LivePlayCommandResult(Snapshot);

        if (!Active || Paused) return new LivePlayCommandResult(Snapshot);
        if (!RunnerPlay && (Path is null || Path.Count == 0))
            return new LivePlayCommandResult(Snapshot, FlightDone: true);

        // The park's status volumes (F4-b, FR-07): every body where the last frame left it, before anybody moves this one.
        ReadStatusVolumes();

        // The glove's own velocity over the last frame: what the body keeps for chase.handoffCoastSec when the ring leaves it (§8.9).
        _coast.Sample(GlovePos, GloveX, GloveZ, dt);
        // The response law (#718): a body nobody stepped last frame brakes to a stop; then this frame's steps begin.
        TickIdleBrakes(dt);
        // The pursuit stick (#718): one read a frame — the owner and the asked velocity every stick site below shares.
        _stick = ReadPursuitStick(field);
        // A human's onward-throw press while the ball is in flight to their receiver (#723): remembered, retargeted or cancelled.
        TickThrowQueue(field, dt);

        // Dash: mash South on the offense pad (running.dash).
        if (run.SouthDown) Dash01 = Math.Min(R.Running.Dash.MaxDash, Dash01 + R.Running.Dash.PerPress);
        _match.Dash01 = Dash01;
        // The offense pad's runner verbs (§9.3): the seat that owns the runners is the only one that reaches them.
        if (Seats.HumanRuns) ApplyRunPad(run);
        // The rundown (§9.7) is read off last frame's bodies so the runner AI sees it as it decides.
        ReadRundown();

        Advance(LivePlayCommand.Advance(dt, LiveKind(), HoldsBall, Throwing, HoldsBall, Dash01, command.Source, PlayFielder()));

        if (LooseBall)
            TickLooseBall(dt);
        else if (HoldsBall && !Throwing)
        {
            BallX = GloveX;
            BallY = Buddy ? R.Fielding.Catch.BuddyHeldBallY : R.Fielding.Catch.HeldBallY;
            BallZ = GloveZ;
        }
        else if (!Throwing && Path is not null)
        {
            var p = BallFlight.PointAt(Path, ElapsedSeconds, R);
            (BallX, BallY, BallZ) = p;
            OffTheBat |= FlyCatch.OffTheBat(Path, BallX, BallY, BallZ, R);
            ReadBallHazards(dt);
            ReadFirstHopKick();
            ReadSurges(dt);
            ReadDrifts(dt);
        }

        _dive.Tick(dt);
        if (JumpT > 0) JumpT -= dt;
        if (SwapLock > 0) SwapLock -= dt;
        _jump.Tick(dt, R.Fielding.Catch);
        // The live ball between frames (#719): what the CPU's dive reads, position and motion, never the resolved path.
        _ballVel = _ballPrev is { } prev ? ((BallX - prev.X) / dt, (BallY - prev.Y) / dt, (BallZ - prev.Z) / dt) : (0, 0, 0);
        _ballPrev = (BallX, BallY, BallZ);
        if (StunT > 0)
        {
            StunT -= dt;
            if (StunT <= 1e-9)
            {
                StunT = 0;
                StunPos = "";
            }
        }
        foreach (var pos in _items.Tick(dt, _fielders, R))
            Foil(pos, R.Batting.Items.SlipSec);
        // The hot ball (§13, Hot Iron): a glove that has held it past its hold while it is molten drops it at its feet.
        ReadHotBall();
        if (_items.Due(ElapsedSeconds))
            LandItem();

        TickBuddyPartner(dt);
        TickCoverBags(dt);
        TickCutoffAndBackup(dt);
        ChargeOutfield(dt);
        TickHandoffCoast(dt);
        ChargeBunt(dt);
        TickCoastBrake(dt);
        // While the throw is in the air the YOU ring rides the receiver: the cursor follows that body's walk to the bag.
        if (Throwing && _fielders.TryGetValue(GlovePos, out var receiverAt))
            (GloveX, GloveZ) = receiverAt;
        TryTakeGlove(field);
        ClampField();

        if (InClosePlay)
            return TickClosePlay(dt, field, run);

        // The impact recoil (#720) is no stop: the world goes on, the body skids and brakes, and only its own steering and throw
        // start wait (F693-02-ordinary-recoil-actions); the contact at the bag still counts in the branches below.
        if (_recoil.Active && !Throwing)
            TickImpactRecoil(dt);

        if (Throwing)
        {
            var reached = _flight.Advance(dt);
            if (_flight.ReleaseDue) ReleaseThrow();
            (BallX, BallY, BallZ) = _flight.Ball;
            if (reached)
                _trace?.Mark(PlayTraceMarkKind.ThrowTargetReached, ElapsedSeconds, CoverPos, ThrowBag);
            if (_flight.Landed && !command.EffectInFlight)
            {
                if (OnThrowLanded(dt, out var arrived)) return arrived;
                if (!Throwing && !LooseBall && IsTime()) return Commit();
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
        if (Hit is not null && Ball is { GroundRule: true, LeavesT: { } leftAt } && !HoldsBall && !Throwing && !LooseBall
            && ElapsedSeconds >= leftAt + R.Flight.DeadBall.RestHoldSec && !command.EffectInFlight)
            return Commit();

        if (RunnerPlay)
        {
            var done = TickRunnerField(dt, field, command.EffectInFlight);
            ClampField();
            return done ?? new LivePlayCommandResult(Snapshot);
        }

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
        var onTheGround = LooseBall || ElapsedSeconds >= hang;
        var buddyOn = FieldingResolver.BuddyJumpOffered(pre);
        var needsJump = FlyCatch.NeedsJump(pre);
        var plant = FlyCatch.ChaseTarget(pre, R, Park);
        var who = map.TryGetValue(GlovePos, out var gloveNow) ? gloveNow : pre.Fielder;
        BuddyWindow = BuddyReady();
        var catchRules = R.Fielding.Catch;

        NoteSwitchHint(map, pre, pad);
        if (SelectTakes(pad, buddyOn)) TakeSelect(map, pad);

        // West: a takeoff, read before the frame's step so the airborne steering begins at the press (#719,
        // F693-02-normal-jump-startup-trial).
        TickJumpPress(pad, dt);

        var steering = (chasing || HoldsBall) && !Throwing;
        var dead = !_stick.Manual;
        if (SwapLock <= 0 && FieldAssist.CpuChases(HoldsBall, Throwing, dead))
            ChaseGlove(dt, pre);

        // One glove speed for human and CPU (§8.1); the body moves once its reaction lockout is over (§8.2).
        if (steering && map.TryGetValue(GlovePos, out var glove) && _stick.Manual && CanMove(GlovePos))
        {
            var speed = CarrySpeed(glove, FieldingResolver.ChaseSpeedFt(glove, GlovePos, pre, R, pad.EastHeld));
            var feet = StepStick(GlovePos, (GloveX, GloveZ), _stick.WantX, _stick.WantY, speed, dt, specialSlowed: pre.Frozen);
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
        if (pad.EastDown && CanMove(GlovePos) && LungeToward(pre, plant))
        {
            CommitDive(GlovePos, catchRules.DiveArmSec);
        }

        var radius = CatchRadius(map);
        var standUp = FieldingResolver.StandUpCatchFt(radius);
        var diveWin = FieldingResolver.DiveCatchFt(radius, R);
        var scoopStand = FieldingResolver.CatchWindowFt(radius, dive: false, jump: false, R);
        var d = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        if (!HoldsBall && GloveMayTake(GlovePos))
        {
            if (onTheGround)
            {
                // A loose ball (a fumble, an overthrow) is picked up by touching it (fielding.chase.looseScoopFt), never by the catch radius.
                var dirtStand = LooseBall ? R.Fielding.Chase.LooseScoopFt : scoopStand;
                var dirtDive = LooseBall ? R.Fielding.Chase.LooseScoopFt : FieldingResolver.CatchWindowFt(radius, dive: true, jump: false, R);
                if (LooseBall ? FlyCatch.TouchScoop(d, dirtStand, BallY, R)
                    : FlyCatch.TouchScoop(pre, Park, BallX, BallZ, BallY, ElapsedSeconds, hang, d, dirtStand, R))
                    TakeBattedBall();
                var pickupInPlay = LooseBall || FlyCatch.PickupInPlay(pre, Park, BallX, BallZ, ElapsedSeconds, hang, R);
                if (pickupInPlay && FlyCatch.PlayerDiveCatch(DiveT > 0, d, dirtStand, dirtDive, BallY, R))
                {
                    CatchDive = true;
                    TakeBattedBall();
                }
            }
            else
            {
                var inWin = FlyCatch.JumpWindow(ElapsedSeconds, hang, R, who, Park);
                var linerInAir = !needsJump && ElapsedSeconds < hang;
                var underStand = FlyCatch.InPosition(pre, GloveX, GloveZ, BallX, BallZ, BallY, plant.X, plant.Z, standUp,
                    ElapsedSeconds, hang, needsJump, R, JumpHeightFt);
                var underDive = FlyCatch.InPosition(pre, GloveX, GloveZ, BallX, BallZ, BallY, plant.X, plant.Z, diveWin,
                    ElapsedSeconds, hang, needsJump, R, JumpHeightFt);
                // The leap: the body actually in the air (#719).
                var leaping = Airborne;
                var jumpTry = leaping && FlyCatch.HighEnough(BallY, needsJump || buddyOn, R);
                var buddyRob = BuddyReady();
                BuddyWindow = buddyRob;
                var canRob = !needsJump || FlyCatch.CanRob(pre.Ball?.FenceClearFt ?? double.NaN, who, Park, R, buddyRob);
                // Dead stick runs the glove and may take a standing catch, never a dive.
                if (dead && FlyCatch.AutoCatch(underStand, inWin, needsJump, canRob: false, linerInAir: linerInAir))
                    TakeBattedBall();
                if (FlyCatch.PlayerCaught(jumpTry, false, underStand, inWin, needsJump, canRob, linerInAir))
                {
                    if (jumpTry) CatchJump = true;
                    // A leap's catch over a height only its higher rise reaches (§8.4): the ordinary jump at the same instant would not.
                    if (jumpTry && !needsJump && _jump.RiseFt > catchRules.JumpRiseFt
                        && BallY > catchRules.StandingHeightFt + NormalJump.HeightAt(JumpAirT, catchRules.JumpAirSec, catchRules.JumpRiseFt))
                        RecordFact(new ReachBonusTake(who.Id, who.FieldAbility));
                    if (buddyRob && jumpTry)
                    {
                        Buddy = true;
                        _events.Add(LiveEvent.BuddyJump);
                    }
                    TakeBattedBall();
                }
                if (!needsJump && underDive && DiveT > 0 && BallY < catchRules.DiveMaxBallY)
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
            if (ThrowPress(pad) is { } press)
                return BeginPlayerThrowOrCommit(map, press);
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
        // The take is manual pursuit's entry (#718): the calibrated radial stick past enterMag; Select takes regardless.
        if (!(pad.Swap || _stick.Manual))
            return;
        PlayerFielding = true;
        // The take is not a re-pick (D16, D18): the stick takes the body wearing the ring; Select is the switch, with its lock (§8.9).
        if (SelectTakes(pad)) TakeSelect(Assigned(), pad);
    }

    // ---------------------------------------------------------------------------------
    // CPU glove (dead stick)
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult? TickCpuField(double dt, LivePadInput pad, bool effectInFlight)
    {
        var pre = Preview!;
        var hang = Hang;
        var catchRules = R.Fielding.Catch;
        if (Seats.HumanOwnsThrow && !HoldsBall)
            NoteSwitchHint(Assigned(), pre, pad);
        if (!HoldsBall && Path is not null)
            ChaseGlove(dt, pre);
        _fielders[GlovePos] = (GloveX, GloveZ);
        var cpuMap = Assigned();
        var cpuRadius = CatchRadius(cpuMap);
        var cpuStandUp = FieldingResolver.StandUpCatchFt(cpuRadius);
        var cpuScoop = FieldingResolver.CatchWindowFt(cpuRadius, dive: false, jump: false, R);
        var cpuDist = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        // CPU catches use standing reach. Only a human command may initiate a dive.
        // Nothing is force-fed at hang. Dirt scoops keep windowPadFt (the double-play rows).
        if (!HoldsBall && GloveMayTake(GlovePos))
        {
            if (LooseBall ? FlyCatch.TouchScoop(cpuDist, R.Fielding.Chase.LooseScoopFt, BallY, R)
                : FlyCatch.TouchScoop(pre, Park, BallX, BallZ, BallY, ElapsedSeconds, hang, cpuDist, cpuScoop, R))
                TakeBattedBall();
            else if (ElapsedSeconds < hang && !LooseBall)
            {
                var plant = FlyCatch.ChaseTarget(pre, R, Park);
                var needsJump = FlyCatch.NeedsJump(pre);
                var who = PlayFielder();
                var inWin = FlyCatch.JumpWindow(ElapsedSeconds, hang, R, who, Park);
                var linerInAir = !needsJump && ElapsedSeconds < hang;
                var underStand = FlyCatch.InPosition(pre, GloveX, GloveZ, BallX, BallZ, BallY, plant.X, plant.Z, cpuStandUp,
                    ElapsedSeconds, hang, needsJump, R, JumpHeightFt);
                var buddyAt = BuddyReady();
                BuddyWindow = buddyAt;
                var canRob = needsJump && FlyCatch.CanRob(pre.Ball?.FenceClearFt ?? double.NaN, who, Park, R, buddyAt);
                var autoStand = FlyCatch.AutoCatch(underStand, inWin, needsJump, canRob, linerInAir: linerInAir);
                if (autoStand)
                {
                    // No roll decides the catch (§8.6): the glove and the ball do.
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
            if (ThrowPress(pad) is { } press)
                return BeginPlayerThrowOrCommit(owned, press);
            if (IsTime())
                return Commit();
            return null;
        }
        return TickCpuHeld(dt, effectInFlight);
    }

    /// <summary>The CPU glove holding the ball, batted or dead (§8.8, §9.7, §10.4): the tag or force this frame, Time, the walk to a bag, the rundown, then the table once per possession.</summary>
    LivePlayCommandResult? TickCpuHeld(double dt, bool effectInFlight)
    {
        if (!Throwing && TickLiveContact(out var done))
        {
            // An unassisted force or a tag at the bag landed (§10.4): the glove decides again from there, as a receiver would.
            if (_cpuWalkBag > 0)
            {
                _cpuWalkBag = 0;
                _cpuClock.Restart();
            }
            return done;
        }
        if (effectInFlight) return null;
        if (IsTime())
            return Commit();
        if (RecoilT > 0) return null;
        if (_dive.Recovering(GlovePos)) return null;   // the dive's recovery (#719): the table waits with the body
        // Walking to the bag to step on it or to wait for the body bound there (§10.3, §10.4, S-41).
        if (_cpuWalkBag > 0)
        {
            WalkGloveTo(Geometry.Bag(_cpuWalkBag), dt);
            if (!PlayStandsAt(_cpuWalkBag))
            {
                _cpuWalkBag = 0;
                _cpuClock.Restart();
            }
            return null;
        }
        // The rundown (§9.7): run the runner down, throw once they are near a covered bag.
        if (TickCpuRundown(dt)) return null;
        // The decision table (§8.8) runs once per possession, after the fielder's reaction.
        if (CpuMayThrow())
            CpuDecide();
        return null;
    }

    /// <summary>A play still stands at <paramref name="bag"/>: a force there, a retouch owed there, or a body bound there and short of it.</summary>
    bool PlayStandsAt(int bag) =>
        Forces.At(bag) && _match.RunnerAt(bag - 1) is { Live: true } forced && forced.Bag < bag
        || Runners.Any(r => r.Live && r.LeftEarly && r.FromBag == bag)
        || RunnerSystem.HeadingTo(Runners, bag) is not null;

    // ---------------------------------------------------------------------------------
    // The runner play (§11.3, §11.4): the ball is dead in a glove, the bodies who broke are on the path
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// One frame of the glove on a runner play. The human seat walks the glove with the stick
    /// (the tag is a walk into the body, §10.3), arms a bag and presses South for the one throw
    /// (§8.5), or the cutoff verb (§8.7); the CPU runs the held-ball table. A loose ball is chased
    /// by the nearest body and scooped by touch; Time ends it (§10.6).
    /// </summary>
    LivePlayCommandResult? TickRunnerField(double dt, LivePadInput pad, bool effectInFlight)
    {
        var map = Assigned();
        var human = PlayerFielding || Seats.HumanOwnsThrow;
        if (human) PlayerFielding = true;
        if (Throwing) return null;
        // Select / R is the one §8.9 rule here as on a batted ball (#637): live on a loose ball, refused with the ball in the
        // glove, so the ring and the ball never leave the catcher (or the receiver) on a press; the HUD pulses the body it would take.
        if (human)
        {
            NoteSwitchHint(map, Preview, pad);
            if (SelectTakes(pad)) TakeSelect(map, pad);
        }
        if (LooseBall)
        {
            ChaseLooseBall(dt, map, human ? pad : LivePadInput.Dead);
            if (IsTime()) return Commit();
            return null;
        }
        if (!HoldsBall) return null;
        if (!human) return TickCpuHeld(dt, effectInFlight);

        WalkGloveWithStick(dt, map, pad);
        if (TickLiveContact(out var contactDone))
            return contactDone;
        ReadThrowBag(pad, stickOk: false);
        if (ThrowPress(pad) is { } press)
            return BeginPlayerThrowOrCommit(map, press);
        if (IsTime()) return Commit();
        return null;
    }

    /// <summary>The human glove walks with the stick at the one chase speed (§8.1) once its lockout is over (§8.2).</summary>
    void WalkGloveWithStick(double dt, Dictionary<string, Character> map, LivePadInput pad)
    {
        if (Throwing || !_stick.Manual || !CanMove(GlovePos)) return;
        if (!map.TryGetValue(GlovePos, out var glove)) return;
        var asked = LooseBall ? null : Preview;
        var speed = CarrySpeed(glove, FieldingResolver.ChaseSpeedFt(glove, GlovePos, asked, R, pad.EastHeld));
        var feet = StepStick(GlovePos, (GloveX, GloveZ), _stick.WantX, _stick.WantY, speed, dt, specialSlowed: asked?.Frozen == true);
        GloveX = feet.X;
        GloveZ = feet.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
    }

    /// <summary>
    /// A loose ball on a runner play (a sailed pickoff, an overthrow): the nearest body runs it down and touching it is the scoop (§8.6).
    /// A live stick steers; the nearest-body hand-off and the CPU's walk run on a dead stick outside the Select lock, the batted-ball
    /// chase's rule (§8.9), so a Select holds the body it took for <c>chase.swapLockSec</c>.
    /// </summary>
    void ChaseLooseBall(double dt, Dictionary<string, Character> map, LivePadInput pad)
    {
        var dead = !_stick.Manual;
        if (!dead)
            WalkGloveWithStick(dt, map, pad);
        else if (SwapLock <= 0 && FieldAssist.CpuChases(HoldsBall, Throwing, dead))
        {
            TryHandoffLoose();
            if (CanMove(GlovePos)) WalkGloveTo((BallX, BallZ), dt);
        }
        var d = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
        if (GloveMayTake(GlovePos) && FlyCatch.TouchScoop(d, R.Fielding.Chase.LooseScoopFt, BallY, R))
            TakeBall();
    }

    /// <summary>The glove with the ball walks toward a spot at the one chase speed (§8.1).</summary>
    void WalkGloveTo((double X, double Z) goal, double dt)
    {
        var who = GloveChar();
        var speed = CarrySpeed(who, FieldingResolver.ChaseSpeedFt(who, Preview?.Frozen ?? false, R));
        var next = StepTo(GlovePos, (GloveX, GloveZ), goal, speed, R.Fielding.Chase.StepStopFt, dt, flat: false);
        GloveX = next.X;
        GloveZ = next.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
    }

    // ---------------------------------------------------------------------------------
    // Rundown (§9.7): a body off the bags with the ball held in range
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The runner in a rundown this frame (§9.7): live, off every bag, not the batter short of first,
    /// not a forced runner whose force still stands (that is a throw to the bag), with a glove
    /// holding the ball inside running.rundown.rangeFt of their body. Flags every body for the AI.
    /// </summary>
    void ReadRundown()
    {
        Runner? caught = null;
        var range = R.Running.Rundown.RangeFt;
        foreach (var r in Runners)
        {
            r.MarkRundown(false);
            if (!r.Live || !HoldsBall || Throwing || LooseBall) continue;
            if (r.IsBatter && r.Bag == 0) continue;
            if (r.OverrunProtected || (r.OnBag || r.IsOn(r.Bag)) && !UnentitledNow(r)) continue;
            if (RunnerSystem.ForcedOff(r, Forces.At, Fly) && r.DestBag > r.Bag) continue;
            var (x, z) = r.Position;
            if (Diamond.Dist(GloveX, GloveZ, x, z) > range) continue;
            // A glove standing on the bag a body is still closing on waits there: that is the tag at the bag (§10.3),
            // not a chase. A body that stops or turns away is caught between the bags.
            var heading = r.DestBag > r.Bag ? r.NextBag : r.Bag;
            var closing = r.Moving && !r.Held && (r.DestBag > r.Bag ? r.Velocity > 0 : r.Velocity < 0);
            if (closing && InPlay.OnThisBag(heading, GloveX, GloveZ, R, R.Running.Bags.OccupyRadiusFt)) continue;
            r.MarkRundown(true);
            caught ??= r;
        }
        if (caught is not null && RundownRunner is null)
        {
            _events.Add(LiveEvent.Rundown);
            if (!_rundownCued)
            {
                _rundownCued = true;
                Sub = $"{caught.Who.Name} is caught in a rundown!";
            }
        }
        RundownRunner = caught;
    }

    /// <summary>
    /// The nearest live body off every bag — or on a bag another runner is entitled to (§9.1) — that no force or waiting glove
    /// accounts for; the rundown read without the range.
    /// </summary>
    Runner? StrayRunner()
    {
        Runner? best = null;
        var bestD = double.MaxValue;
        foreach (var r in Runners)
        {
            if (!r.Live || (r.IsBatter && r.Bag == 0)) continue;
            if (r.OverrunProtected || (r.OnBag || r.IsOn(r.Bag)) && !UnentitledNow(r)) continue;
            if (RunnerSystem.ForcedOff(r, Forces.At, Fly) && r.DestBag > r.Bag) continue;
            var heading = r.DestBag > r.Bag ? r.NextBag : r.Bag;
            var closing = r.Moving && !r.Held && (r.DestBag > r.Bag ? r.Velocity > 0 : r.Velocity < 0);
            if (closing && InPlay.OnThisBag(heading, GloveX, GloveZ, R, R.Running.Bags.OccupyRadiusFt)) continue;
            var (x, z) = r.Position;
            var d = Diamond.Dist(GloveX, GloveZ, x, z);
            if (d < bestD) { bestD = d; best = r; }
        }
        return best;
    }

    /// <summary>
    /// The CPU glove in a rundown (§9.7): the chase keeps the body trapped while the ball is behind them
    /// (a body that turns runs into the glove), so the glove runs at them until the throw ahead is at its
    /// last makeable moment — the body's arrival at the covered bag ahead is inside the §8.8 margin of the
    /// throw's, and the throw still lands inside the close margin — and throws then, at full speed. A body not
    /// closing on a bag (frozen, §10.6), or one the throw could not beat, is run at until tagged. True when the
    /// rundown owned this frame.
    /// </summary>
    bool TickCpuRundown(double dt)
    {
        // In range it is the rundown; with nothing makeable on the table a stray body anywhere off the bags is
        // run at the same way (a frozen runner cannot be left standing on the path, §9.7, §10.6).
        var inRange = RundownRunner is not null;
        var target = RundownRunner ?? (_cpuClock.Decided ? StrayRunner() : null);
        if (target is null || !target.Live || !HoldsBall || Throwing) return false;
        var bag = target.DestBag > target.Bag ? target.NextBag : target.Bag;
        if (bag is < 1 or > 4) return false;
        var (x, z) = target.Position;
        var closing = target.Moving && !target.Held && (target.DestBag > target.Bag ? target.Velocity > 0 : target.Velocity < 0);
        var ready = CpuThrowReadySec(bag); // the flight, or the cover's walk to the bag when that is longer; infinite with no cover
        // The body's feet to the bag it is closing on: ahead by the runner clock, back along the segment on a return.
        var arrival = target.DestBag > target.Bag
            ? RunnerSystem.ArrivalSec(target, bag, ElapsedSeconds, R, Dash01)
            : target.Feet / RunnerSystem.SpeedFtPerSec(target.Who, R, Dash01);
        var margin = arrival - ready;
        // The throw ahead is the rundown's (the glove inside running.rundown.rangeFt, §9.7): it goes once the margin is
        // down to the table's band and while it can still land inside the close margin of the body (§8.8's "worth
        // it"). A body the throw cannot beat, or a stray body out of range (the table already held), is run at.
        if (inRange && closing && !double.IsPositiveInfinity(ready) && margin <= R.Cpu.Active.MakeableMarginSec && margin > -R.Running.Close.MarginSec)
        {
            BeginThrowToBag(bag, LazyLob(bag) ? R.Running.Rundown.LazyLobSpeedMul : 1);
            return true;
        }
        WalkGloveTo((x, z), dt);
        return true;
    }

    /// <summary>
    /// The lazy lob (§9.7, reference): every moving body is at least running.rundown.lazyLobFraction of the way to
    /// a bag and none of them is bound for <paramref name="bag"/>. A throw that races a body to the bag it is
    /// thrown to is a throw, whatever the fraction: a runner 8 ft short of second is past 80 % of the segment.
    /// </summary>
    bool LazyLob(int bag)
    {
        var rd = R.Running.Rundown;
        var moving = Runners.Where(r => r.Live && r.Moving).ToList();
        if (moving.Any(r => (r.DestBag > r.Bag ? r.NextBag : r.Bag) == bag)) return false;
        return moving.All(r => r.SegmentFt <= 0 || Math.Max(r.Feet, r.SegmentFt - r.Feet) / r.SegmentFt >= rd.LazyLobFraction);
    }

    /// <summary>The CPU glove holds the ball: the throw waits for the reaction delay (§8.8), then the table runs once.</summary>
    bool CpuMayThrow()
    {
        return _cpuClock.MayThrow(ElapsedSeconds, () => InPlay.ThrowReactionSec(GloveChar(), R));
    }

    /// <summary>
    /// The CPU fielder's decision table (§8.8) runs once for this possession (<see cref="CpuFieldDecider"/>), and the glove does
    /// what it decided: walks the ball to a bag, throws to a bag (straight or through the cutoff), throws it in, or holds.
    /// </summary>
    void CpuDecide()
    {
        _cpuClock.Decide();
        var decision = CpuFieldDecider.Decide(this);
        switch (decision.Action)
        {
            case CpuFieldAction.WalkTo:
                _cpuWalkBag = decision.Bag;
                break;
            case CpuFieldAction.ThrowTo:
                CpuThrowTo(decision.Bag);
                break;
            case CpuFieldAction.ThrowIn:
                CpuThrowIn();
                break;
        }
    }

    /// <summary>Seconds for this glove to carry the ball to <paramref name="bag"/> at its carry speed (§8.1; Ball Dash's boost included, #718).</summary>
    double CpuWalkSec(int bag)
    {
        var at = Geometry.Bag(bag);
        var speed = CarrySpeed(GloveChar(), FieldingResolver.ChaseSpeedFt(GloveChar(), Preview?.Frozen ?? false, R));
        return Diamond.Dist(GloveX, GloveZ, at.X, at.Z) / Math.Max(1, speed);
    }

    /// <summary>Seconds until a throw to <paramref name="bag"/> is in a glove on it: the flight, or the cover's walk there if that is longer (a lob waits, §8.5).</summary>
    double CpuThrowReadySec(int bag)
    {
        var coverPos = CoverOf(bag);
        if (string.IsNullOrEmpty(coverPos) || coverPos == GlovePos || !_fielders.TryGetValue(coverPos, out var coverAt))
            return double.PositiveInfinity;
        var cover = R.Fielding.Cover;
        var at = Geometry.Bag(bag);
        var walk = Math.Max(0, Diamond.Dist(coverAt.X, coverAt.Z, at.X, at.Z) - cover.RadiusFt) / Math.Max(1, CoverSpeed(coverPos, Assigned()));
        return Math.Max(CpuThrowArrivalSec(bag), walk);
    }

    /// <summary>
    /// The CPU's read of a throw to a bag (§8.7, §8.8, #722): straight, or through the cutoff on the line, each on the
    /// one clock with the real arms and the rung's read of the pair chemistry — and which leg it takes. Beyond
    /// <c>fielding.throw.onTheFlyFt</c> the relay is forced, whatever the clock says; inside it the relay is taken when it
    /// beats the direct throw by more than the rung's <c>cpu.*.relayBiasSec</c>. The shipped table's bias is a value no
    /// relay can save, so there the ceiling alone decides, which is the rule the game shipped with.
    /// </summary>
    readonly record struct ThrowPlan(double DirectSec, double RelaySec, (string Pos, double X, double Z)? Cut, bool Forced, bool UseRelay)
    {
        /// <summary>Seconds until the ball is at the bag by the leg the CPU takes.</summary>
        public double Sec => UseRelay ? RelaySec : DirectSec;
    }

    ThrowPlan PlanThrow(double fromX, double fromZ, Character thrower, string throwerPos, int bag)
    {
        var level = R.Cpu.Active;
        var to = Geometry.Bag(bag);
        var map = Assigned();
        var coverPos = CoverOf(bag);
        var cover = !string.IsNullOrEmpty(coverPos) && map.TryGetValue(coverPos, out var c) ? c : null;
        var dist = Diamond.Dist(fromX, fromZ, to.X, to.Z);
        var holding = throwerPos == GlovePos && _receivedClean;
        var direct = InPlay.ThrowSec(dist, Forecast(thrower, cover, level.ReadsChemistry, bag, holding), R);
        var forced = dist > R.Fielding.Throw.OnTheFlyFt;
        var cut = InPlay.CutoffFor(fromX, fromZ, to.X, to.Z, _fielders, throwerPos, coverPos, R);
        if (cut is null || !map.TryGetValue(cut.Value.Pos, out var cutter))
            return new ThrowPlan(direct, direct, null, forced, false);
        var (_, cx, cz) = cut.Value;
        // The cutter will hold a received ball, so its leg gets Snap Throw's release if it carries the ability.
        var relay = InPlay.ThrowSec(Diamond.Dist(fromX, fromZ, cx, cz), Forecast(thrower, cutter, level.ReadsChemistry, 0, holding), R)
                    + InPlay.ThrowReactionSec(cutter, R)
                    + InPlay.ThrowSec(Diamond.Dist(cx, cz, to.X, to.Z), Forecast(cutter, cover, level.ReadsChemistry, bag, true), R);
        return new ThrowPlan(direct, relay, cut, forced, InPlay.RelayWins(direct, relay, forced, level.RelayBiasSec));
    }

    /// <summary>Seconds from now until a throw from this glove would land at <paramref name="bag"/>, by the leg the CPU would take (§8.7).</summary>
    double CpuThrowArrivalSec(int bag) => PlanThrow(GloveX, GloveZ, GloveChar(), GlovePos, bag).Sec;

    /// <summary>The arm alone (no chemistry roll): the fielder's own estimate of a throw to <paramref name="bag"/> (0 for a cutoff feed), with the ability the command allows there and Snap Throw's release when the thrower holds a received ball (#723).</summary>
    ThrowResult ArmOnly(Character who, int bag, bool receivedClean) =>
        new(Chemistry.Neutral, InPlay.ArmMul(who, R) * AbilityMul(who, bag), false, Arm: who.Stats.Arm, ReleaseSec: SnapRelease(who, receivedClean),
            RangeBonusFt: FieldAbilities.RangeBonusFt(who, R));

    /// <summary>
    /// The CPU's forecast of a throw from <paramref name="from"/> to <paramref name="to"/>: the arm and ability exactly, and
    /// the pair chemistry as far as the rung reads it (<c>cpu.*.readsChemistry</c>) — the deterministic pair factor, never a
    /// sampled roll (F693-03-good-chemistry). At 0 it is the arm alone, which is what the shipped CPU forecasts.
    /// </summary>
    ThrowResult Forecast(Character from, Character? to, double chemistryRead, int bag, bool receivedClean)
    {
        var thr = ArmOnly(from, bag, receivedClean);
        if (to is null || chemistryRead <= 0) return thr;
        var chem = R.Fielding.Chem;
        var pair = _match.Chemistry.Between(from, to) switch
        {
            Chemistry.Good => chem.GoodSpeedMul,
            Chemistry.Bad => chem.BadSpeedMul,
            _ => 1.0
        };
        return thr with { SpeedMul = thr.SpeedMul * (1 + chemistryRead * (pair - 1)) };
    }

    /// <summary>
    /// The CPU runner's read of a throw released at (<paramref name="fromX"/>, <paramref name="fromZ"/>) by
    /// <paramref name="thrower"/> to <paramref name="bag"/> (§9.9, #722): the fielder's own plan, with the rung's read of
    /// the arm (<c>cpu.*.runnerReadsArm</c>), of the relay (<c>runnerReadsRelay</c>) and of the chemistry
    /// (<c>readsChemistry</c>). At the shipped rungs' zeros it is the neutral flat throw the runner always read: a speed
    /// multiplier of exactly 1 and no relay.
    /// </summary>
    double RunnerThrowSec(double fromX, double fromZ, Character? thrower, string throwerPos, int bag)
    {
        if (thrower is null) return InPlay.ThrowArrivalSec(fromX, fromZ, bag, null, R);
        var level = R.Cpu.Active;
        var to = Geometry.Bag(bag);
        var map = Assigned();
        var coverPos = CoverOf(bag);
        var cover = !string.IsNullOrEmpty(coverPos) && map.TryGetValue(coverPos, out var c) ? c : null;
        var direct = InPlay.ThrowSec(Diamond.Dist(fromX, fromZ, to.X, to.Z), Read(thrower, cover, level, bag), R);
        if (level.RunnerReadsRelay <= 0) return direct;
        var plan = PlanThrow(fromX, fromZ, thrower, throwerPos, bag);
        if (!plan.UseRelay || plan.Cut is not { } cut || !map.TryGetValue(cut.Pos, out var cutter)) return direct;
        var relay = InPlay.ThrowSec(Diamond.Dist(fromX, fromZ, cut.X, cut.Z), Read(thrower, cutter, level, 0), R)
                    + InPlay.ThrowReactionSec(cutter, R)
                    + InPlay.ThrowSec(Diamond.Dist(cut.X, cut.Z, to.X, to.Z), Read(cutter, cover, level, bag), R);
        return direct + level.RunnerReadsRelay * (relay - direct);
    }

    /// <summary>A throw as the CPU runner reads it: the neutral arm at <c>runnerReadsArm</c> 0, the real arm and ability at 1, the range rounded to the arm read; the chemistry as <see cref="Forecast"/> reads it.</summary>
    ThrowResult Read(Character who, Character? to, CpuLevelRules level, int bag)
    {
        var real = Forecast(who, to, level.ReadsChemistry, bag, false);
        var armPart = InPlay.ArmMul(who, R) * AbilityMul(who, bag);
        var pairPart = real.SpeedMul / armPart;
        var speed = (1 + level.RunnerReadsArm * (armPart - 1)) * pairPart;
        var arm = (int)Math.Round(InPlay.NeutralArm + level.RunnerReadsArm * (who.Stats.Arm - InPlay.NeutralArm));
        return new ThrowResult(Chemistry.Neutral, speed, false, Arm: arm,
            RangeBonusFt: level.RunnerReadsArm * FieldAbilities.RangeBonusFt(who, R));
    }

    /// <summary>The runner's clock (§9.9): <see cref="RunnerThrowSec"/> with the body at <paramref name="pos"/> as the thrower — whoever holds the ball next.</summary>
    Func<double, double, int, double> RunnerClock(string pos)
    {
        var who = Assigned().TryGetValue(pos, out var body) ? body : null;
        return (x, z, bag) => RunnerThrowSec(x, z, who, pos, bag);
    }

    /// <summary>The CPU throws to <paramref name="bag"/>: straight, or through the cutoff on the line when the relay is the leg the plan takes (§8.7).</summary>
    void CpuThrowTo(int bag)
    {
        var plan = PlanThrow(GloveX, GloveZ, GloveChar(), GlovePos, bag);
        if (plan.UseRelay && plan.Cut is { } cut)
        {
            _support.ArmRelay(bag);
            BeginThrowToCutoff(cut.Pos, cut.X, cut.Z);
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
        var plan = PlanThrow(GloveX, GloveZ, GloveChar(), GlovePos, bag);
        if (plan.UseRelay && plan.Cut is { } cut)
        {
            _support.ArmRelay(0); // the cutoff decides again from the infield
            BeginThrowToCutoff(cut.Pos, cut.X, cut.Z);
            return;
        }
        BeginThrowToBag(bag);
    }

    /// <summary>
    /// The runner's read of the ball this frame (§9.9), exactly as <see cref="RunnerAi"/> would be handed it now — the
    /// clock the ball carries included — for traces and tests. Not the catch event itself; that flag is the tick's.
    /// </summary>
    public RunnerAiContext RunnerRead(double dash01) => AiContext(dash01);

    /// <summary>The runner AI's read of the ball this frame (§9.9): who has it or will, and when.</summary>
    RunnerAiContext AiContext(double dash01, bool atCatch = false)
    {
        BallSituation ball;
        var carry = Hit?.CarryFt ?? 0;
        if (Throwing && ThrowBag is >= 1 and <= 4)
        {
            var to = Geometry.Bag(ThrowBag);
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
        else if (LooseBall)
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
            var speed = FieldingResolver.ChaseSpeedFt(who, GlovePos, Preview, R);
            var route = FieldingPursuit.Plan(Preview, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R, ReadyAt(GlovePos),
                !FieldingResolver.IsOutfield(GlovePos), GloveChar());
            var meetAt = route.Reachable ? route.MeetTimeSec : Math.Max(Rest, ElapsedSeconds + route.TravelTimeSec);
            ball = new BallSituation(false, false, 0, 0, route.X, route.Z, meetAt,
                FieldingResolver.OutfieldGrass(route.X, route.Z, R), Preview.LandingX, Preview.LandingZ, carry);
        }
        // A bunt (§7.3): the runner from third holds at contact unless the offense sent them. The clock the runner reads
        // (#722) is the defense's own plan from whoever holds the ball next: the receiver of a throw in the air, else the glove.
        var nextHolder = Throwing ? (ThrowBag is >= 1 and <= 4 ? CoverPos : _support.CutoffPos) : GlovePos;
        ball = ball with { Bunt = Ball is { Shape: BattedBallClass.Bunt }, ThrowClock = RunnerClock(nextHolder), Fielder = nextHolder };
        var trailing = _match.Inning >= _match.Innings
            ? (_match.Top ? _match.HomeScore - _match.AwayScore : _match.AwayScore - _match.HomeScore)
            : int.MinValue;
        // The outs the runner reads are the count at contact: the two-out contact play does not begin when the batter is retired mid-play.
        return new RunnerAiContext(ElapsedSeconds, OutsAtOpen, trailing, Fly, ball, dash01, atCatch);
    }

    /// <summary>
    /// The offense pad on the bodies (§9.3): D-pad selects, LB / RB / both send, return, halt every
    /// runner, the stick sends or returns the selected one, a tap of the opposite shoulder halts.
    /// </summary>
    void ApplyRunPad(LivePadInput run) => RunnerCommands.Apply(_match, run, ref _prevRun);

    /// <summary>The CPU-driven glove runs its route to the ball (or the loose ball) once its reaction lockout is over (§8.2).</summary>
    void ChaseGlove(double dt, FieldingPreview pre)
    {
        if (Path is null) return;
        var map = Assigned();
        if (LooseBall)
        {
            TryHandoffLoose();
            if (!CanMove(GlovePos)) return;
            var chaser = map.TryGetValue(GlovePos, out var lc) ? lc : pre.Fielder;
            var run = FieldingResolver.ChaseSpeedFt(chaser, pre.Frozen, R);
            var step = StepTo(GlovePos, (GloveX, GloveZ), (BallX, BallZ), run, R.Fielding.Chase.StepStopFt, dt, flat: false);
            if (Diamond.Dist(GloveX, GloveZ, step.X, step.Z) > 1e-6)
                _facts.Add(new AssistedRouteStep(chaser.Id));
            GloveX = step.X;
            GloveZ = step.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
            return;
        }
        var live = BallFlight.PointAt(Path, ElapsedSeconds, R);
        var hang = Hang;
        var airborne = FieldingResolver.InAir(pre, live.Y, ElapsedSeconds, R, hang);
        var airTarget = FlyCatch.ChaseTarget(pre, R, Park);
        TryHandoffOutfield(airborne ? airTarget.X : live.X, airborne ? airTarget.Z : live.Z, airborne);
        if (!CanMove(GlovePos)) return;
        var who = map.TryGetValue(GlovePos, out var c) ? c : pre.Fielder;
        var speed = FieldingResolver.ChaseSpeedFt(who, GlovePos, pre, R);
        var route = FieldingPursuit.Plan(pre, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R, ReadyAt(GlovePos),
            !FieldingResolver.IsOutfield(GlovePos), GloveChar());
        var next = StepTo(GlovePos, (GloveX, GloveZ), (route.X, route.Z), speed, R.Fielding.Chase.StepStopFt, dt, flat: false);
        if (Diamond.Dist(GloveX, GloveZ, next.X, next.Z) > 1e-6)
            _facts.Add(new AssistedRouteStep(who.Id));
        GloveX = next.X;
        GloveZ = next.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
    }

    /// <summary>The infield → outfield hand-off (§8.9, D16, D17): the glove goes where <see cref="PursuitDecider.OutfieldHandoff"/> says, if anywhere.</summary>
    void TryHandoffOutfield(double ballX, double ballZ, bool airborne)
    {
        if (PursuitDecider.OutfieldHandoff(this, ballX, ballZ, airborne) is { } to) HandGloveTo(to);
    }

    /// <summary>A loose ball is the nearest body's (§8.6, §8.7): the glove goes where <see cref="PursuitDecider.LooseHandoff"/> says, if anywhere.</summary>
    void TryHandoffLoose()
    {
        if (PursuitDecider.LooseHandoff(this) is { } to) HandGloveTo(to);
    }

    /// <summary>
    /// The play glove (and the YOU ring) moves to another body; every body stays where it stands. The body the ring left keeps its
    /// velocity for <c>chase.handoffCoastSec</c> (§8.9) unless <paramref name="coast"/> is off: a throw's release, where the thrower stays put (§8.5).
    /// A body in its dive (<see cref="DiveT"/>), or still paying the dive's recovery, never coasts.
    /// </summary>
    void HandGloveTo(string pos, bool coast = true)
    {
        if (pos == GlovePos) return;
        _receivedClean = false;
        _fielders[GlovePos] = (GloveX, GloveZ);
        // A body in its dive is on the ground: it does not coast. Its last frame can be the lunge — a displacement, not a
        // run — and read as a velocity it slid the diver a hundred feet. The dive's arm window on every table, and the
        // recovery the diver still owes after it (#719).
        var down = _dive.Down(GlovePos);
        if (coast && !down) _coast.Leave(GlovePos, R.Fielding.Chase.HandoffCoastSec);
        GlovePos = pos;
        if (_fielders.TryGetValue(GlovePos, out var at))
        {
            GloveX = at.X;
            GloveZ = at.Z;
        }
        else if (Starts.TryGetValue(GlovePos, out var home))
        {
            GloveX = home.X;
            GloveZ = home.Z;
            _fielders[GlovePos] = (GloveX, GloveZ);
        }
    }

    /// <summary>
    /// After a hand-off the previous body keeps its velocity for <c>chase.handoffCoastSec</c>, then stops (§8.9): the ring moves, the body
    /// does not jerk. The cover, cutoff, backup, and charge walks wait for it.
    /// </summary>
    void TickHandoffCoast(double dt)
    {
        if (_coast.Step(dt, GlovePos, _fielders.ContainsKey) is not { } coast) return;
        var at = _fielders[coast.Pos];
        _fielders[coast.Pos] = FieldBounds.ClampFielder(Park, at.X + coast.Vel.X * coast.Step, at.Z + coast.Vel.Z * coast.Step, R);
        // The body's velocity is the coast's, so when the coast ends it brakes rather than stopping dead (#718).
        _response.Carry(coast.Pos, coast.Vel);
    }

    /// <summary>
    /// The brake after a hand-off coast (#718): from the coast's last step the body brakes on every frame no walk has taken it, so
    /// the coast runs straight into the brake — no standing frame between them, and the walks that waited for the coast take the
    /// body at the velocity it has. Runs after the walks; nothing to do on the shipped table, where the coast ends in a dead stop.
    /// </summary>
    void TickCoastBrake(double dt)
    {
        if (_coast.Brake(dt) is not { } brake) return;
        var pos = brake.Pos;
        // On the coast's last frame the step record is the coast's own mark, and the brake has only what the coast left of the frame.
        if (pos == GlovePos || !brake.Ending && _response.Stepped(pos) || !_response.TryVelocity(pos, out var v)
            || Math.Abs(v.X) < 1e-9 && Math.Abs(v.Z) < 1e-9 || !_fielders.TryGetValue(pos, out var at))
        {
            _coast.EndBrake();
            return;
        }
        if (brake.Left > 1e-9) BrakeStep(pos, at, v, brake.Left);
    }

    bool Coasting(string pos) => _coast.Coasting(pos);

    /// <summary>The outfielder <see cref="PursuitDecider.Charger"/> names charges the ball on its route, one step this frame.</summary>
    void ChargeOutfield(double dt)
    {
        if (PursuitDecider.Charger(this) is not { } of || Preview is null) return;
        var at = _fielders[of.Position];
        var speed = FieldingResolver.ChaseSpeedFt(of.Fielder, of.Position, Preview, R);
        _fielders[of.Position] = StepTo(of.Position, at, (of.Route.X, of.Route.Z), speed, R.Fielding.Chase.StepStopFt, dt, flat: false);
    }

    /// <summary>
    /// The one cover map of this play (§8.7): the bunt's while the alignment is on (the middle behind the crash, §7.3),
    /// else <see cref="InPlay.CoverMap"/> at <see cref="CoverBallX"/>, the body on the ball excluded either way. Every
    /// read of who covers a bag — the formation, the throw's receiver, the cover walk, the CPU table, the rundown —
    /// goes through here so they cannot disagree (#640).
    /// </summary>
    Dictionary<int, string> CoverMapNow() =>
        !RunnerPlay && BuntAlignment ? BuntDefense.CoverMap(OnBallPos, R.Fielding.Bunt) : InPlay.CoverMap(OnBallPos, CoverBallX);

    /// <summary>Who covers <paramref name="bag"/> now (§8.7): the play's map with the glove excluded.</summary>
    string CoverOf(int bag)
    {
        if (bag is < 1 or > 4) return "";
        return CoverMapNow().TryGetValue(bag, out var pos) ? pos : FieldAssist.CoverKey(bag);
    }

    /// <summary>
    /// A charge body on a bunt (§7.3) that is free to converge on the ball: in the bunt table, not the glove, and not
    /// the cover of a bag a play still stands at (the catcher stays home on a squeeze, §10.3).
    /// </summary>
    bool BuntChargeBody(string pos, Dictionary<int, string> covers)
    {
        if (Ball is not { Shape: BattedBallClass.Bunt } || pos == GlovePos || !BuntDefense.Charges(pos, R.Fielding.Bunt)) return false;
        foreach (var kv in covers)
            if (kv.Value == pos && PlayStandsAt(kv.Key)) return false;
        return true;
    }

    /// <summary>
    /// The speed the body at <paramref name="pos"/> walks to a bag, the throw line or a backup spot (§8.7): the flat cover
    /// speed at chaseSpeedWeight 0, the body's own pursuit speed as far as the table reads it (#718).
    /// </summary>
    double CoverSpeed(string pos, IReadOnlyDictionary<string, Character> bodies) =>
        bodies.TryGetValue(pos, out var who) ? FieldingResolver.CoverSpeedFt(who, R) : R.Fielding.Cover.FtPerSec;

    /// <summary>
    /// Cover bodies walk to their bags (§8.7) at the body's own pursuit speed from contact, with no read (the old rule, at
    /// lockoutMul 1: the flat cover speed after the start delay and the body's reaction lockout, D11)
    /// (F693-02-coverage-budget, #718) — <c>cover.lockoutMul</c> 0 and <c>cover.startSec</c> 0.
    /// </summary>
    void TickCoverBags(double dt)
    {
        if (Preview is null && !RunnerPlay) return;
        var cover = R.Fielding.Cover;
        var bodies = Assigned();
        var onBall = OnBallPos;
        var map = CoverMapNow();
        var squared = !RunnerPlay && BuntDefense.Squared(Swing);
        foreach (var kv in map)
        {
            var pos = kv.Value;
            if (string.IsNullOrEmpty(pos) || pos == onBall || _support.Supports(pos) || Coasting(pos)) continue;
            // A body already walking on the square keeps walking through the crack (§7.3); the rest wait the cover start.
            var onSquare = squared && BuntDefense.CoverBag(pos, R.Fielding.Bunt) == kv.Key;
            if (!RunnerPlay && !onSquare && ElapsedSeconds < cover.StartSec) continue;
            // A charge body converges on the bunt instead of covering an idle bag (ChargeBunt).
            if (!HoldsBall && !Throwing && !LooseBall && BuntChargeBody(pos, map)) continue;
            if (!_fielders.TryGetValue(pos, out var at)) continue;
            var goal = Geometry.Bag(kv.Key);
            _fielders[pos] = StepTo(pos, at, goal, CoverSpeed(pos, bodies), cover.StopFt, dt, flat: true);
        }
    }

    /// <summary>
    /// The triangle on a bunt (§7.3): every charge body that is not the glove converges on the ball to
    /// fielding.bunt.chargeStopFt once its lockout is over (§8.2), unless a play stands at the bag it covers.
    /// They do not take the ball: the glove is the earliest route (§8.2) and the touch is the glove's; a human
    /// who takes one of them with the stick keeps it (§8.9).
    /// </summary>
    void ChargeBunt(double dt)
    {
        if (Preview is null || Hit is null || Path is null || RunnerPlay) return;
        if (Ball is not { Shape: BattedBallClass.Bunt } || HoldsBall || Throwing || LooseBall) return;
        var b = R.Fielding.Bunt;
        var map = Assigned();
        var covers = CoverMapNow();
        foreach (var pos in b.Charge)
        {
            if (!map.TryGetValue(pos, out var who) || !BuntChargeBody(pos, covers) || !CanMove(pos)) continue;
            if (_support.Supports(pos)) continue;
            if (!_fielders.TryGetValue(pos, out var at)) continue;
            if (Diamond.Dist(at.X, at.Z, BallX, BallZ) <= b.ChargeStopFt) continue;
            var speed = FieldingResolver.ChaseSpeedFt(who, pos, Preview, R);
            _fielders[pos] = StepTo(pos, at, (BallX, BallZ), speed, R.Fielding.Chase.StepStopFt, dt, flat: false);
        }
    }

    /// <summary>The cutoff walks to the throw line and the backup to its spot behind the target (§8.7), at the cover speed the table gives their bodies (#718).</summary>
    void TickCutoffAndBackup(double dt)
    {
        var cover = R.Fielding.Cover;
        var bodies = Assigned();
        // The cutoff walks to the line while the ball is in the air, YOU ring or not (the ring is handed to
        // the receiver at release, §8.5); once they hold it the spot is cleared.
        if (!string.IsNullOrEmpty(_support.CutoffPos) && _support.CutoffSpot is { } spot && (Throwing || _support.CutoffPos != GlovePos)
            && _fielders.TryGetValue(_support.CutoffPos, out var cutAt) && CanMove(_support.CutoffPos) && !Coasting(_support.CutoffPos))
            _fielders[_support.CutoffPos] = StepTo(_support.CutoffPos, cutAt, spot, CoverSpeed(_support.CutoffPos, bodies), cover.StopFt, dt, flat: true);
        if (!string.IsNullOrEmpty(_support.BackupPos) && _support.BackupPos != GlovePos
            && _fielders.TryGetValue(_support.BackupPos, out var backAt) && CanMove(_support.BackupPos) && !Coasting(_support.BackupPos))
            _fielders[_support.BackupPos] = StepTo(_support.BackupPos, backAt, _support.BackupSpot, CoverSpeed(_support.BackupPos, bodies), cover.StopFt, dt, flat: true);
    }

    // ---------------------------------------------------------------------------------
    // The response law (#718, F693-02-carry-movement-response)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The rated speed the response rates are measured against: the body's own pursuit top speed (§8.1), never the speed it
    /// happens to be asked for this frame — a Ball Dash carry raises the cap and not the rates (F693-02-carry-movement-response,
    /// #718), so the boosted body takes 0.24 s to its 1.20 V and not 0.20. The asked speed stands in only for a body the
    /// formation does not name.
    /// </summary>
    double RatedSpeed(string pos, double asked)
    {
        var top = Assigned().TryGetValue(pos, out var who) ? FieldingResolver.ChaseSpeedFt(who, false, R) : asked;
        return Math.Max(1, top);
    }

    /// <summary>
    /// The glove's speed with the ball in its hand (F693-02-ordinary-carry-speed, -ball-dash-carrier, #718): the pursuit speed it
    /// was asked for, × <c>abilities.ballDashMul</c> for a Ball Dash holder in secure possession — the ball caught or handed,
    /// not in flight. Without the ball, or for any other body, it is the asked speed itself.
    /// </summary>
    double CarrySpeed(Character who, double asked) =>
        HoldsBall && !Throwing ? FieldingResolver.CarrySpeedFt(who, asked, R) : asked;

    // ---------------------------------------------------------------------------------
    // The status volume, live (F4-b, #896; FR-07, FD-08-R1, FD-08-R2)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Once a frame, before anybody moves (FR-07): every fielder and every live runner, where the last frame left them, against
    /// the park's status volumes (<see cref="BodySlows"/>). A body that entered one runs slowed from this frame: a
    /// <see cref="LiveEvent.BodySlowed"/> cue, one <see cref="BodySlowed"/> on <see cref="Slows"/> and <see cref="SlowsThisPlay"/>
    /// per volume entered, and a <see cref="PlayTraceMarkKind.BodySlowed"/> mark in the trace. A Burrow body touches nothing.
    /// No draw, and nothing read from where the ball lands.
    /// </summary>
    void ReadStatusVolumes()
    {
        if (_bodySlows.Volumes.Count == 0) return;
        var t = ElapsedSeconds;
        var map = Assigned();
        _routeImmune.Clear();
        foreach (var pos in Diamond.Order)
        {
            if (!map.TryGetValue(pos, out var who)) continue;
            if (FieldAbilities.IgnoresParkSlow(who)) _routeImmune.Add(pos);
            var at = pos == GlovePos ? (GloveX, GloveZ) : _fielders.TryGetValue(pos, out var feet) ? feet : Starts[pos];
            foreach (var (v, until) in _bodySlows.Read(pos, at.Item1, at.Item2, t, FieldAbilities.IgnoresParkSlow(who)))
                Slowed(new BodySlowed(pos, who, v.Hazard, v.Type, t, until), v, null);
        }
        foreach (var r in Runners)
        {
            if (!r.Live) continue;
            var (x, z) = r.Position;
            foreach (var (v, until) in _bodySlows.Read(r, x, z, t, FieldAbilities.IgnoresParkSlow(r.Who)))
                Slowed(new BodySlowed(FieldBody.Runner, r.Who, v.Hazard, v.Type, t, until), v, r);
        }
    }

    void Slowed(BodySlowed touch, StatusVolume volume, Runner? runner)
    {
        _slows.Add(touch);
        _slowsThisPlay.Add(touch);
        if (!_events.Contains(LiveEvent.BodySlowed)) _events.Add(LiveEvent.BodySlowed);
        _trace?.Mark(PlayTraceMarkKind.BodySlowed, touch.T, touch.IsRunner ? null : touch.Pos,
            runner: runner is null ? null : PlayTraceRunner.Of(runner, slowed: true),
            hazard: new PlayTraceHazard(volume.Hazard, volume.Type, volume.X, volume.Z, volume.RadiusFt, touch.UntilT));
    }

    /// <summary>
    /// What a step of the body at <paramref name="pos"/> is multiplied by this frame (F4-b): <c>fielding.chase.frozenMul</c> while a
    /// status volume slows it, else exactly 1, so an unslowed step is the double it always was. A speed that already carries the
    /// heart swing's slow (<paramref name="specialSlowed"/>; a special, outside this phase) is not slowed again: the special and
    /// the volume are the one slow, never two stacked — stacking stays with the specials (the 3e boundary).
    /// </summary>
    /// <summary>
    /// The route cost of a status volume (FD-14, SF-26; F4-g): every step to a goal — the CPU's chase, cover, cutoff and backup
    /// walk and the assistance's — heads around a volume in the way when that costs less time than its slow
    /// (<see cref="VolumeRoute"/>), at the body's own asked speed. A park with no volume, a Burrow body and a straight line
    /// that meets no volume leave the goal exactly as it was. The stick is never steered.
    /// </summary>
    (double X, double Z) RouteAround(string pos, (double X, double Z) at, (double X, double Z) goal, double speed)
    {
        // A solid body first (F4-f): nobody passes through one, so the route always goes around it, at the mover's place now.
        if (_solids.Count > 0)
            goal = VolumeRoute.Waypoint(at, goal, _solids.Select(b => b.AsVolume(ElapsedSeconds)).ToList(), speed, 1e-6,
                R.Fielding.Chase.VolumeClearFt);
        return _bodySlows.Volumes.Count == 0 || _routeImmune.Contains(pos)
            ? goal
            : VolumeRoute.Waypoint(at, goal, _bodySlows.Volumes, speed, R.Fielding.Chase.FrozenMul, R.Fielding.Chase.VolumeClearFt);
    }

    /// <summary>The redirects the ball went through this play, in order (F4-c).</summary>
    public IReadOnlyList<BallRedirected> RedirectsThisPlay => _redirectsThisPlay;

    /// <summary>The solid bodies the ball caromed off this play, in order (F4-f).</summary>
    public IReadOnlyList<BodyCarom> CaromsThisPlay => _caromsThisPlay;

    /// <summary>Where a park's solid bodies stand this frame (a mover's place on the play clock), for presentation (F4-f).</summary>
    public IReadOnlyList<(int Hazard, double X, double Z)> SolidsNow =>
        _solids.Select(b => { var (x, z) = b.At(ElapsedSeconds); return (b.Hazard, x, z); }).ToList();

    /// <summary>
    /// The ball off a solid body (F4-f; SF-28): a ball in a body's disc below its top, moving into it, turns the part of its
    /// speed into the body around at the row's restitution and keeps the rest; the path continues from the rim, the ball is
    /// re-read and every chaser re-plans. The same body does not take the ball again until it is clear of the disc.
    /// </summary>
    void ReadSolidCarom(double dt)
    {
        if (_solids.Count == 0 || Path is null || Hit is null || Ball is null || Preview is null) return;
        var t = ElapsedSeconds;
        if (_caromLock is { } locked)
        {
            var b = _solids.First(s => s.Hazard == locked);
            var (bx, bz) = b.At(t);
            if (Diamond.Dist(bx, bz, BallX, BallZ) <= b.RadiusFt + 0.5) return;
            _caromLock = null;
        }
        var before = BallFlight.PointAt(Path, Math.Max(0, t - dt), R);
        var (vx, vy, vz) = ((BallX - before.X) / dt, (BallY - before.Y) / dt, (BallZ - before.Z) / dt);
        if (SolidBodies.Carom(_solids, t, BallX, BallY, BallZ, vx, vz) is not { } hit) return;
        Path = BallFlight.Continue(Path, t, hit.X, BallY, hit.Z, hit.Vx, vy, hit.Vz, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R, Hit.WindMul);
        Ball = BattedBall.Reread(Path, Hit.ExitVeloMph, Hit.LaunchDeg, Ball.Shape == BattedBallClass.Bunt, Park, R);
        (BallX, BallZ) = (hit.X, hit.Z);
        _ballPrev = null;
        Preview = Preview with { LandingX = Ball.LandingX, LandingZ = Ball.LandingZ };
        CoverBallX = Ball.LandingX;
        _caromLock = hit.Body.Hazard;
        _caromsThisPlay.Add(new BodyCarom(hit.Body.Hazard, hit.Body.Type, t, hit.X, hit.Z));
        if (!_events.Contains(LiveEvent.BodyCarom)) _events.Add(LiveEvent.BodyCarom);
        _trace?.Mark(PlayTraceMarkKind.BodyCarom, t,
            hazard: new PlayTraceHazard(hit.Body.Hazard, hit.Body.Type, hit.X, hit.Z, hit.Body.RadiusFt, t));
    }

    /// <summary>The reward target the live ball hit this play, or null (F4-c).</summary>
    public RewardHit? RewardThisPlay => _reward;

    /// <summary>
    /// Once a frame while the ball follows its path (F4-c; FR-07, FD-08-R1): a reward target the ball is under pays once a play,
    /// and a redirect's mouth the ball is in sends it out of another instance of its type, drawn from the match's seeded stream.
    /// The ball leaves the exit with the row's share of its horizontal speed on the same heading and the row's lift, the path is
    /// continued from there on the shared flight and ground physics and the ball is re-read — the way a continuing deflection is
    /// — and every chaser re-plans from the new path the next frame. The fair / foul call and every glove still decide the play.
    /// </summary>
    void ReadBallHazards(double dt)
    {
        if (Path is null || Hit is null || Ball is null || Preview is null || dt <= 0) return;
        var t = ElapsedSeconds;
        if (_reward is null && _ballHazards.Reward(BallX, BallY, BallZ) is { } sign)
        {
            _reward = new RewardHit(sign.Hazard, sign.Type, t);
            if (!_events.Contains(LiveEvent.RewardHit)) _events.Add(LiveEvent.RewardHit);
            var h = Park.Hazards[sign.Hazard];
            _trace?.Mark(PlayTraceMarkKind.RewardHit, t, hazard: new PlayTraceHazard(sign.Hazard, sign.Type, h.X, h.Z, h.Radius, t));
        }
        if (_ballHazards.Entered(BallX, BallY, BallZ) is not { } mouth)
        {
            ReadSolidCarom(dt);
            return;
        }
        var exits = _ballHazards.ExitsFor(mouth);
        var exit = exits[_match.DrawIndex(exits.Count)];
        var before = BallFlight.PointAt(Path, Math.Max(0, t - dt), R);
        var (vx, vz) = ((BallX - before.X) / dt, (BallZ - before.Z) / dt);
        var (x, y, z, ox, oy, oz) = BallHazards.Launch(exit, vx, vz);
        var entry = (X: BallX, Z: BallZ);
        Path = BallFlight.Continue(Path, t, x, y, z, ox, oy, oz, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R, Hit.WindMul);
        Ball = BattedBall.Reread(Path, Hit.ExitVeloMph, Hit.LaunchDeg, Ball.Shape == BattedBallClass.Bunt, Park, R);
        (BallX, BallY, BallZ) = (x, y, z);
        _ballPrev = null;
        Preview = Preview with { LandingX = Ball.LandingX, LandingZ = Ball.LandingZ };
        CoverBallX = Ball.LandingX;
        _ballHazards.Exited(exit);
        var fact = new BallRedirected(mouth.Hazard, mouth.Type, exit.Hazard, t, entry.X, entry.Z, x, z);
        _redirectsThisPlay.Add(fact);
        if (!_events.Contains(LiveEvent.BallRedirected)) _events.Add(LiveEvent.BallRedirected);
        _trace?.Mark(PlayTraceMarkKind.BallRedirected, t,
            hazard: new PlayTraceHazard(mouth.Hazard, mouth.Type, mouth.X, mouth.Z, mouth.DiscFt, t, exit.Hazard));
        Sub = $"Into the {PlayNarrator.RedirectName(mouth.Type)}!";
    }

    double VolumeMul(string pos, bool specialSlowed) => BodySlows.Mul(!specialSlowed && _bodySlows.Slowed(pos), R);

    // ---------------------------------------------------------------------------------
    // The pursuit stick (#718, F693-02-pursuit-neutral-boundary, -analog-response, -arming)
    // ---------------------------------------------------------------------------------

    /// <summary>The pursuit stick of a bound device; created armed-off, on the identity profile, the first time it is asked for.</summary>
    public PursuitStick FieldStick(int device = 0)
    {
        if (!_sticks.TryGetValue(device, out var stick)) _sticks[device] = stick = new PursuitStick();
        return stick;
    }

    /// <summary>The seat steers the body this frame (the stick past the gate); false is the assistance.</summary>
    public bool PursuitManual => _stick.Manual;

    /// <summary>The calibrated radial stick is on and the fielding seat has not armed: the body is the assistance's until the stick is seen neutral once, and the client should say so.</summary>
    public bool PursuitUnready => Seats.HumanFields && !FieldStick(_stickDevice).Armed;

    /// <summary>
    /// One read a frame of the defense pad's stick: the device's calibrated radial stick with its hysteresis, its arming and
    /// the linear remap.
    /// </summary>
    StickRead ReadPursuitStick(LivePadInput pad)
    {
        _stickDevice = pad.Device;
        var s = R.Fielding.Stick;
        return FieldStick(pad.Device).Read(pad.StickX, pad.StickY, s);
    }

    /// <summary>
    /// One frame of a body's velocity toward what it wants (#718): the component along its heading builds at the ramp rate
    /// and dies at the brake rate; the component across it builds at the ramp rate. So a reversal is the brake and then the
    /// ramp, a stop is the brake, and an angled turn is continuous correction through the same two rates. Rest to the rated
    /// speed takes the body class's <c>accelSec</c>; the rated speed to rest takes its <c>brakeSec</c> (§8.1, <see cref="BodyClasses.Ramp"/>).
    /// <para>
    /// The ground under the body (FD-04 B, FD-05, F3-d) scales those times: the row of the zone at <paramref name="at"/>, read
    /// every step, multiplies the ramp by <c>body.startMul</c>, the brake by <c>body.brakeMul</c> and the across-heading
    /// correction's ramp by <c>body.cutMul</c>. Each multiplies its time once, so 1.0 is the old arithmetic bit for bit. The
    /// rated speed, the asked velocity and the heading the body settles on are never scaled: the body always goes where it is
    /// asked to, it only answers slower on a slick ground.
    /// </para>
    /// </summary>
    (double X, double Z) Respond(string pos, (double X, double Z) at, (double X, double Z) want, double asked, double dt)
    {
        var ground = GroundZones.Of(Park, R).RowAt(at.X, at.Z, R.Grounds).Body;
        var top = RatedSpeed(pos, asked);
        // Airborne on a normal jump the body answers at a fraction of its ground rates (#719, F693-02-normal-jump-air-response-trial).
        var rate = Airborne && pos == GlovePos ? R.Fielding.Catch.JumpAirResponseMul : 1.0;
        // The ramp is the body's class's (§8.1, CH-11): a light body gets to speed and stops sooner than a heavy one.
        var ramp = BodyClasses.Ramp(Assigned().TryGetValue(pos, out var who) ? who : null, R);
        return _response.Respond(pos, want, top, rate, ground, ramp, dt);
    }

    /// <summary>
    /// A body's step toward a goal this frame (§8.1): on the shipped table the flat cover step or <see cref="FieldingResolver.StepToward"/>,
    /// exactly as before; under the response law the body wants the goal at <paramref name="speed"/> (rest inside the stop radius),
    /// its velocity answers through <see cref="Respond"/>, and it moves on that velocity.
    /// </summary>
    (double X, double Z) StepTo(string pos, (double X, double Z) at, (double X, double Z) goal, double speed, double stopFt, double dt, bool flat)
    {
        // A status volume slows the body that touched it (F4-b): every step it takes. The flat step is the cover, cutoff and
        // backup walk, whose speed never carries the heart swing's slow; every other step is a chase over the preview.
        goal = RouteAround(pos, at, goal, speed);
        speed *= VolumeMul(pos, specialSlowed: !flat && Preview?.Frozen == true);
        var dx = goal.X - at.X;
        var dz = goal.Z - at.Z;
        var dist = Math.Sqrt(dx * dx + dz * dz);
        var want = dist <= stopFt || dist < 1e-9
            ? (X: 0.0, Z: 0.0)
            : (X: dx / dist * Math.Min(speed, dist / dt), Z: dz / dist * Math.Min(speed, dist / dt));
        var v = Respond(pos, at, want, speed, dt);
        return FieldBounds.ClampFielder(Park, at.X + v.X * dt, at.Z + v.Z * dt, R);
    }

    /// <summary>
    /// The stick's step (§8.1): today's proportional step on the shipped table; under the response law the same want, answered through the body's velocity.
    /// <paramref name="specialSlowed"/> says the asked speed already carries the heart swing's slow (<see cref="VolumeMul"/>).
    /// </summary>
    (double X, double Z) StepStick(string pos, (double X, double Z) at, double stickX, double stickY, double speed, double dt, bool specialSlowed)
    {
        speed *= VolumeMul(pos, specialSlowed);
        var v = Respond(pos, at, (stickX * speed, stickY * speed), speed, dt);
        return FieldBounds.ClampFielder(Park, at.X + v.X * dt, at.Z + v.Z * dt, R);
    }

    /// <summary>
    /// Bodies nobody stepped last frame brake to a stop (#718), and the frame's step record is cleared. A coasting body is not idle:
    /// the ring left it on a frame nobody stepped it, and its coast (then <see cref="TickCoastBrake"/>) is this frame's step.
    /// Nothing to do on the shipped table.
    /// </summary>
    void TickIdleBrakes(double dt)
    {
        foreach (var pos in _response.Idle(Coasting))
        {
            _response.TryVelocity(pos, out var v);
            if (Math.Abs(v.X) < 1e-9 && Math.Abs(v.Z) < 1e-9 || !_fielders.TryGetValue(pos, out var at))
            {
                _response.Forget(pos);
                continue;
            }
            BrakeStep(pos, at, v, dt);
        }
        _response.EndIdle();
    }

    /// <summary>One braking step of a body nobody steers (#718): its velocity dies at the brake rate and it moves on what is left.</summary>
    void BrakeStep(string pos, (double X, double Z) at, (double X, double Z) v, double dt)
    {
        // Nobody's intent in the air is a coast, not a brake (#719): the airborne glove keeps its velocity.
        var nv = Airborne && pos == GlovePos ? v : Respond(pos, at, (0, 0), 0, dt);
        var next = FieldBounds.ClampFielder(Park, at.X + nv.X * dt, at.Z + nv.Z * dt, R);
        _fielders[pos] = next;
        if (pos == GlovePos && !Throwing)
        {
            GloveX = next.X;
            GloveZ = next.Z;
        }
    }

    // ---------------------------------------------------------------------------------
    // Throw commands (#723): the relay is player-owned, a press is remembered, a cancel is a verb
    // ---------------------------------------------------------------------------------

    /// <summary>Whether a queued onward throw waits for the receiver's catch (#723): the HUD's queue tell.</summary>
    public bool ThrowQueued => _commands.Queued;
    public bool CanCancelThrow => ThrowQueued || AwaitingRelay;

    /// <summary>The bag a queued onward throw would go to — the armed bag, home by default — or 0 when nothing is queued.</summary>
    public int QueuedThrowBag => _commands.RelayQueued ? _support.RelayTarget : 0;

    /// <summary>
    /// While a human's throw flies to the cutoff (F693-03-relay-ownership, -input-buffer, -throw-cancel): a fresh South press is
    /// remembered for <c>fielding.throw.relayBufferSec</c> of active play and fires at the catch; the bag selectors retarget the
    /// armed bag without refreshing the press's age; a fresh RB clears it. Nothing here on the shipped table, whose
    /// buffer is 0 and whose cutoff throws the armed leg for the player.
    /// </summary>
    void TickThrowQueue(LivePadInput field, double dt)
    {
        var t = R.Fielding.Throw;
        var relayInFlight = Throwing && ThrowBag is not (>= 1 and <= 4);
        _commands.Tick(field, dt, t, mayAim: !HoldsBall && !Throwing && !RunnerPlay, Seats.HumanOwnsThrow, relayInFlight, _events);
        // The relay's armed bag follows the selectors while the throw flies to the cutter.
        if (t.RelayBufferSec <= 0 || !Seats.HumanOwnsThrow || !relayInFlight) return;
        var stick = field.StickBag > 0 ? field.StickBag : field.ArrowBag;
        var armed = InPlay.ArmedBag(field.KeysBag, stick, false);
        if (armed > 0) _support.ArmRelay(armed);
    }

    /// <summary>Whether a throw to <paramref name="bag"/> is the one Laser is for (F693-03-laser-throw): home, with a live runner on third or on the third–home segment.</summary>
    bool LaserEligible(int bag) => bag == 4 && Runners.Any(r => r.Live && !r.IsBatter && r.Bag == 3);

    /// <summary>
    /// The thrower's ability on a throw to <paramref name="bag"/> (§8.5, #723): the table's multiplier, except that when
    /// <c>abilities.laserHomeOnly</c> is on, a Laser holder's boost is taken back off any throw that is not home with a runner —
    /// a cutoff feed (bag 0) first of all. On the shipped table nothing is taken off.
    /// </summary>
    double AbilityMul(Character who, int bag)
    {
        var mul = FieldAbilities.ThrowMul(who, R);
        var a = R.Fielding.Abilities;
        if (HasAbility(who, FieldAbilityId.Laser) && !LaserEligible(bag)) mul /= a.LaserMul;
        return mul;
    }

    static bool HasAbility(Character who, string id) => who.FieldAbility == id;

    /// <summary>Snap Throw's release for <paramref name="who"/> when they hold a clean received throw (F693-03-snap-throw); null is the ordinary release.</summary>
    double? SnapRelease(Character who, bool receivedClean) =>
        receivedClean && HasAbility(who, FieldAbilityId.SnapThrow) ? R.Fielding.Abilities.SnapReleaseSec : null;

    /// <summary>A built throw with the command's rules on it (#723): the Laser boost confined, the Snap release when it applies.</summary>
    ThrowResult WithCommand(ThrowResult thr, Character from, int bag)
    {
        var a = R.Fielding.Abilities;
        if (HasAbility(from, FieldAbilityId.Laser) && !LaserEligible(bag))
            thr = thr with { SpeedMul = thr.SpeedMul / a.LaserMul };
        var release = SnapRelease(from, _receivedClean);
        if (release is { } sec) thr = thr with { ReleaseSec = sec };
        return thr;
    }

    void ClampField()
    {
        var feet = FieldBounds.ClampFielder(Park, GloveX, GloveZ, R);
        GloveX = feet.X;
        GloveZ = feet.Z;
        if (_fielders.Count == 0)
        {
            _fielders[GlovePos] = feet;
            return;
        }
        foreach (var k in _fielders.Keys.ToList())
        {
            var f = FieldBounds.ClampFielder(Park, _fielders[k].X, _fielders[k].Z, R);
            // Nobody stands in a solid body (F4-f): a body stepped into one is on its rim.
            _fielders[k] = _solids.Count == 0 ? f : SolidBodies.PushOut(_solids, ElapsedSeconds, f.X, f.Z);
        }
        if (_solids.Count > 0)
        {
            feet = SolidBodies.PushOut(_solids, ElapsedSeconds, feet.X, feet.Z);
            (GloveX, GloveZ) = feet;
        }
        _fielders[GlovePos] = feet;
    }

    (double X, double Z) SwitchAim(FieldingPreview? pre)
    {
        if (LooseBall) return (BallX, BallZ);
        if (pre is not null && Path is not null)
        {
            var map = Assigned();
            var who = map.TryGetValue(GlovePos, out var fielder) ? fielder : pre.Fielder;
            var speed = FieldingResolver.ChaseSpeedFt(who, GlovePos, pre, R);
            var route = FieldingPursuit.Plan(pre, Park, Path, ElapsedSeconds, GloveX, GloveZ, speed, R, ReadyAt(GlovePos),
                !FieldingResolver.IsOutfield(GlovePos), GloveChar());
            return (route.X, route.Z);
        }
        return (BallX, BallZ);
    }

    void NoteSwitchHint(Dictionary<string, Character> map, FieldingPreview? pre, LivePadInput pad)
    {
        if (HoldsBall || Throwing)
        {
            SwitchPos = "";
            return;
        }
        var spots = LiveSpots(map);
        var aim = SwitchAim(pre);
        SwitchPos = FieldAssist.SwitchHint(GlovePos, spots, aim.X, aim.Z, pad.StickX, pad.StickY, Feel.FieldAssistStick, R);
        if (!map.ContainsKey(SwitchPos) || SwitchPos == GlovePos)
            SwitchPos = "";
    }

    Dictionary<string, (double X, double Z)> LiveSpots(Dictionary<string, Character> map)
    {
        var spots = new Dictionary<string, (double X, double Z)>();
        foreach (var kv in map)
            spots[kv.Key] = _fielders.TryGetValue(kv.Key, out var live) ? live : Starts[kv.Key];
        return spots;
    }

    /// <summary>
    /// Select / R lands this frame (§8.9): the human's switch, one rule for the batted-ball play and the runner play (§11.3, §11.4):
    /// never while holding the ball or throwing (the ring and the ball stay in the glove, S-99, #637), never inside
    /// <c>chase.swapLockSec</c> of the last switch, never while a buddy jump is offered (the ring is the jump's).
    /// </summary>
    bool SelectTakes(LivePadInput pad, bool buddyOn = false) =>
        pad.Swap && SwapLock <= 0 && !buddyOn && !HoldsBall && !Throwing;

    /// <summary>The switch itself: the ring to the body Select names (<see cref="FieldAssist.SwapGlove"/>) and the lock.</summary>
    void TakeSelect(Dictionary<string, Character> map, LivePadInput pad)
    {
        CycleGlove(map, pad);
        SwapLock = R.Fielding.Chase.SwapLockSec;
    }

    void CycleGlove(Dictionary<string, Character> map, LivePadInput pad)
    {
        var spots = LiveSpots(map);
        var aim = SwitchAim(Preview);
        var next = FieldAssist.SwapGlove(GlovePos, spots, aim.X, aim.Z, pad.StickX, pad.StickY, Feel.FieldAssistStick, R);
        if (!map.ContainsKey(next)) next = "P";
        HandGloveTo(next);
    }

    double CatchRadius(Dictionary<string, Character> map)
    {
        var who = map.TryGetValue(GlovePos, out var c) ? c : Preview!.Fielder;
        // The body class's reach for this ball (§8.1): the ground reach on a ball hit on the ground, the fly reach on one hit in the air.
        var radius = FieldingResolver.CatchRadiusFt(who, Preview is not null ? Park : null, R, air: Preview is not { Grounder: true });
        // Abilities widen the reach for their ball (§8.4): Super Jump on a fly, Dive / Burrow on the dirt, Sand Scoop on a low one.
        if (Preview is not null && FlyCatch.IsFly(Preview))
            radius += FieldAbilities.FlyRangeBonus(who, R);
        if (Preview is { Grounder: true })
            radius += FieldAbilities.GroundRangeBonus(who, R, BallY);
        return radius;
    }

    double CatchWindow(Dictionary<string, Character> map) =>
        FieldingResolver.CatchWindowFt(CatchRadius(map), DiveT > 0, JumpT > 0, R);

    /// <summary>The glove's catch window now (§8.3): how far from the ball a take can happen this frame, dive and jump included.</summary>
    public double CatchWindowFt => CatchWindow(Assigned());

    /// <summary>A human East press lunges sideways toward the ball or its landing point.</summary>
    bool LungeToward(FieldingPreview pre, (double X, double Z) plant)
    {
        var toX = pre.Grounder || pre.Line || LooseBall ? BallX : plant.X;
        var toZ = pre.Grounder || pre.Line || LooseBall ? BallZ : plant.Z;
        return LungeTo(toX, toZ);
    }

    bool LungeTo(double toX, double toZ)
    {
        var lunged = FieldDash.Lunge(GloveX, GloveZ, toX, toZ, R, R.Fielding.Dash.DiveLungeFt);
        if (Diamond.Dist(GloveX, GloveZ, lunged.X, lunged.Z) < 0.01) return false;
        GloveX = lunged.X;
        GloveZ = lunged.Z;
        _dive.Lunged(GlovePos);
        _fielders[GlovePos] = (GloveX, GloveZ);
        return true;
    }

    // ---------------------------------------------------------------------------------
    // The dive is deliberate and costs (#719: F693-02-dive-jump-scoop-reach, -dive-recovery-cost, -cpu-dive-intent, -cpu-dive-intent-policy)
    // ---------------------------------------------------------------------------------

    /// <summary>East commits a dive (<see cref="GloveDive.Commit"/>): its cost is owed from the commitment whether the ball comes or not, and a remembered throw press is dropped.</summary>
    void CommitDive(string pos, double armSec)
    {
        if (!_dive.Commit(pos, armSec, FieldingResolver.DiveRecoverySec(GloveChar(), R))) return;
        _commands.DropRecovery();
        _events.Add(LiveEvent.DiveCommit);
    }

    /// <summary>
    /// The human's throw press through the dive's recovery (F693-02-ordinary-recoil-actions): nothing releases while the body
    /// recovers; a South / cutoff press inside the last <c>throw.relayBufferSec</c> of it is remembered and fires at readiness;
    /// an earlier one is dropped. With no recovery owed the press is the press.
    /// </summary>
    LivePadInput? ThrowPress(LivePadInput pad)
    {
        if (pad.Cancel)
        {
            _commands.DropRecovery();
            return null;
        }
        if (_commands.TakeApproach(pad) is { } approach)
        {
            pad = approach;
            if (pad.KeysBag > 0) ThrowBag = pad.KeysBag;
        }
        var pressed = (pad.SouthDown && (!pad.ExplicitTarget || pad.KeysBag > 0 || ThrowBag > 0)) || pad.Cutoff;
        // Nothing releases while the body recovers from a dive, or while it is still in the air after a jumping catch
        // (F693-02-jump-catch-throw-readiness): the landing comes first, and a press inside the buffer waits for it.
        var recovering = _dive.Recovering(GlovePos);
        var bracing = ImpactRecoil && RecoilT > 0;   // the ordinary impact recoil (#720) is the same wait
        if (recovering || bracing || Airborne)
        {
            var remaining = Math.Max(Math.Max(recovering ? DiveRecoveryT : 0, bracing ? RecoilT : 0),
                Airborne ? R.Fielding.Catch.JumpAirSec - JumpAirT : 0);
            if (pressed && R.Fielding.Throw.RelayBufferSec > 0 && remaining <= R.Fielding.Throw.RelayBufferSec + 1e-9)
            {
                _commands.RememberRecovery(pad);
                _events.Add(LiveEvent.ThrowQueued);
            }
            return null;
        }
        if (_commands.TakeRecovery() is { } remembered)
            return remembered;
        return pressed ? pad : null;
    }

    // ---------------------------------------------------------------------------------
    // The normal jump (#719: F693-02-normal-jump-*, -jump-catch-throw-readiness)
    // ---------------------------------------------------------------------------------

    /// <summary>A takeoff is eligible when the body may move, holds nothing, throws nothing and is not committed to a dive.</summary>
    bool JumpEligible() => !Airborne && !HoldsBall && !Throwing && DiveT <= 0 && RecoilT <= 0 && CanMove(GlovePos);

    /// <summary>West this frame (<see cref="NormalJump.Press"/>): a takeoff starts the arm window for the client's pose and the buddy leap.</summary>
    void TickJumpPress(LivePadInput pad, double dt)
    {
        if (!_jump.Press(pad.WestDown, GlovePos, HoldsBall || Throwing || DiveT > 0, JumpEligible, dt, R.Fielding.Catch,
                FieldAbilities.JumpRiseFt(GloveChar(), R))) return;
        JumpT = R.Fielding.Catch.JumpAirSec;
        _events.Add(LiveEvent.JumpTakeoff);
    }

    /// <summary>
    /// The world moves the ball (#719): the rest of its path, its landing ring and its cover mark slide by (dx, dz) from now — a
    /// carom, a gust, a deflection off another glove. The sim's own effects will drive this as they arrive; a test uses it to
    /// move a ball after a dive was committed on it, which is how a deliberate dive misses.
    /// </summary>
    public void NudgeBall(double dx, double dz)
    {
        if (Path is null || Preview is null) return;
        Path = Path.Select(s => s.T >= ElapsedSeconds - 1e-9 ? s with { X = s.X + dx, Z = s.Z + dz } : s).ToList();
        Preview = Preview with { LandingX = Preview.LandingX + dx, LandingZ = Preview.LandingZ + dz };
        CoverBallX += dx;
        _ballPrev = null;
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
        // Off the bat (§7.11): the batted ball in its flight is no glove's until it has cleared the bat.
        if (!OffTheBat && !LooseBall && Path is not null) return false;
        if (pos == "P" && ElapsedSeconds + 1e-9 < ReadyAt(pos)) return false;
        if (_items.IsOff(pos)) return false;
        if (Stunned(pos)) return false;   // the fumbler waits out the stun (#721); a helper may take it first
        if (_items.Hopping && BallY < R.Fielding.Catch.TouchScoopY) return false;
        return true;
    }

    // The offer plans a pair; only the live bodies and ball can activate their leap.
    bool BuddyReady()
    {
        if (Preview is not { } pre || !FieldingResolver.BuddyJumpOffered(pre)
            || HoldsBall || LooseBall || ElapsedSeconds >= Hang || GlovePos != pre.Position
            || BuddyPos == GlovePos || !FieldingResolver.IsOutfield(BuddyPos)
            || !CanMove(GlovePos) || !CanMove(BuddyPos)
            || !GloveMayTake(GlovePos) || !GloveMayTake(BuddyPos)
            || !_fielders.TryGetValue(BuddyPos, out var partner)) return false;
        return FlyCatch.BuddyInPosition(pre, Park, GloveX, GloveZ, partner.X, partner.Z,
            BallX, BallY, BallZ, ElapsedSeconds, Hang, R);
    }

    void TickBuddyPartner(double dt)
    {
        BuddyWindow = false;
        if (Preview is null || Path is null || HoldsBall || LooseBall || ElapsedSeconds >= Hang
            || !FieldingResolver.BuddyJumpOffered(Preview)) return;
        var map = Assigned();
        BuddyPos = PosOf(map, Preview.Buddy!);
        if (BuddyPos == GlovePos || !map.TryGetValue(BuddyPos, out var partner)
            || !_fielders.TryGetValue(BuddyPos, out var at) || !CanMove(BuddyPos)) return;
        var plant = FlyCatch.ChaseTarget(Preview, R, Park);
        var speed = FieldingResolver.ChaseSpeedFt(partner, BuddyPos, Preview, R);
        _fielders[BuddyPos] = StepTo(BuddyPos, at, plant, speed, R.Fielding.Chase.StepStopFt, dt, flat: false);
        BuddyWindow = BuddyReady();
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

    int DefaultBag()
    {
        if (RunnerPlay)
        {
            // The bag the lead body is bound for (§11.3): the steal bag on a South press with nothing armed.
            var lead = Runners.Where(r => r.Live && r.Advancing && r.Bag < 4).OrderByDescending(r => r.Progress).FirstOrDefault();
            return lead?.DestBag is >= 1 and <= 4 ? lead.DestBag : 0;
        }
        return ForceRecorded ? 1 : InPlay.DefaultGroundBag(_match.First is not null, _match.RunnerAt(2)?.Advancing == true, _match.RunnerAt(3)?.Advancing == true);
    }

    // ---------------------------------------------------------------------------------
    // Throws (§8.5): one clock, to a bag or the cutoff; the thrower stays put, the YOU ring hands to the receiver.
    // ---------------------------------------------------------------------------------

    LivePlayCommandResult? BeginPlayerThrowOrCommit(Dictionary<string, Character> map, LivePadInput pad)
    {
        if (pad.ExplicitTarget && !pad.Cutoff && pad.KeysBag <= 0 && ThrowBag <= 0) return null;
        var hopperCaught = Preview is not null && Preview.Grounder && HoldsBall;
        var def = DefaultBag();
        if (!HoldsBall)
        {
            // Dead balls finish through the shared deadline before ownership dispatch.
            if (InPlay.HasDeadBallResult(LiveKind())) return null;
            return Commit();
        }
        // LB / X with no bag: the relay to the cutoff on the line to the armed bag (home by default; on a
        // runner play the bag the lead body is bound for, §11.3) (§8.7).
        if (pad.Cutoff && (ThrowBag <= 0 || !hopperCaught))
        {
            var toward = ThrowBag is >= 1 and <= 4 ? ThrowBag : RunnerPlay && def > 0 ? def : 4;
            var to = Geometry.Bag(toward);
            var cut = InPlay.CutoffFor(GloveX, GloveZ, to.X, to.Z, _fielders, GlovePos, CoverOf(toward), R);
            if (cut is not null)
            {
                _support.ArmRelay(ThrowBag is >= 1 and <= 4 ? ThrowBag : 0);
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

    /// <summary>A throw to <paramref name="bag"/> from the glove, with the pair's chemistry and the thrower's arm; <paramref name="speedMul"/> under 1 is the lazy lob (§9.7).</summary>
    void BeginThrowToBag(int bag, double speedMul = 1)
    {
        var map = Assigned();
        var coverPos = CoverOf(bag);
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : PlayFielder();
        var cut = !string.IsNullOrEmpty(coverPos) && map.TryGetValue(coverPos, out var c) ? c : null;
        var thr = WithCommand(cut is not null ? _match.ThrowBetween(from, cut) : ArmOnly(from, bag, _receivedClean), from, bag);
        if (speedMul < 1) thr = thr with { SpeedMul = thr.SpeedMul * speedMul };
        ArmedThrow = thr;
        ArmedCut = cut;
        var to = Geometry.Bag(bag);
        BeginThrow(thr, bag, to.X, to.Z, coverPos);
    }

    /// <summary>A throw to the cutoff's body on the line (§8.7); the relay continues with the cutoff's arm.</summary>
    void BeginThrowToCutoff(string cutPos, double lineX, double lineZ)
    {
        var map = Assigned();
        var from = map.TryGetValue(GlovePos, out var glove) ? glove : PlayFielder();
        if (!map.TryGetValue(cutPos, out var cutter))
        {
            BeginThrowToBag(Math.Max(1, _support.RelayBag));
            return;
        }
        var thr = WithCommand(_match.ThrowBetween(from, cutter), from, 0);
        ArmedThrow = thr;
        ArmedCut = cutter;
        _support.Cutoff(cutPos, (lineX, lineZ));
        // The ball goes to where the cutoff will stand: on the line, or where they are if already there.
        var at = _fielders.TryGetValue(cutPos, out var spot) ? spot : Starts[cutPos];
        var onTheLine = Diamond.Dist(at.X, at.Z, lineX, lineZ) < R.Fielding.Cover.RadiusFt;
        BeginThrow(thr, 0, onTheLine ? at.X : lineX, onTheLine ? at.Z : lineZ, cutPos);
    }

    void BeginThrow(ThrowResult thr, int bag, double targetX, double targetZ, string receiverPos)
    {
        Throwing = true;
        _flight.EndLob();
        ThrowBag = bag;
        CoverPos = receiverPos;
        _throwerPos = GlovePos;
        ThrowFromPos = GlovePos;
        var throwRules = R.Fielding.Throw;
        var from = (X: GloveX, Y: throwRules.HandHeightFt, Z: GloveZ);
        var landing = InPlay.ThrowLanding(GloveX, GloveZ, targetX, targetZ, thr.LateralFt);
        // One clock (§8.5): the ball flies on the same seconds the bag is judged on, the catcher's gun included (§11.3).
        var dist = Diamond.Dist(from.X, from.Z, targetX, targetZ);
        _flight.Begin(from, (landing.X, throwRules.BagHeightFt, landing.Z), InPlay.ThrowSec(dist, thr, R),
            thr.ReleaseSec ?? throwRules.ReleaseSec);
        BallX = ThrowFrom.X;
        BallY = ThrowFrom.Y;
        BallZ = ThrowFrom.Z;
        if (bag is >= 1 and <= 4 && FirstThrowBag == 0) FirstThrowBag = bag;
        if (bag is >= 1 and <= 4)
        {
            var cover = R.Fielding.Cover;
            var behind = InPlay.BackupSpot(GloveX, GloveZ, targetX, targetZ, cover.BackupFt);
            var backupSpot = FieldBounds.ClampFielder(Park, behind.X, behind.Z, R);
            _support.Backup(InPlay.BackupPos(bag, backupSpot.X, backupSpot.Z, _fielders, GlovePos, receiverPos), backupSpot);
        }
        _cpuClock.Restart();
        _cpuWalkBag = 0;
        _heldSince = -1;
        // Preparation holds possession at the thrower. The ring, sound and trace move on release.
        _fielders[_throwerPos] = (GloveX, GloveZ);
        _events.Add(LiveEvent.ThrowCommitted);
        if (_flight.ReleaseDue) ReleaseThrow();
    }

    void ReleaseThrow()
    {
        _flight.Release();
        if (!string.IsNullOrEmpty(CoverPos) && CoverPos != GlovePos)
            HandGloveTo(CoverPos, coast: false);
        _trace?.Mark(PlayTraceMarkKind.ThrowRelease, ElapsedSeconds - Math.Max(0, ThrowT - ThrowReleaseSec), _throwerPos, ThrowBag,
            flight: new PlayTraceThrow(_throwerPos, CoverPos, ThrowBag, ThrowFrom.X, ThrowFrom.Y, ThrowFrom.Z,
                ThrowTo.X, ThrowTo.Y, ThrowTo.Z, ThrowDur - ThrowReleaseSec, ArmedThrow!.SpeedMul, ArmedThrow.Relation.ToString()));
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
        var receiverPos = ThrowBag is >= 1 and <= 4 ? CoverPos : _support.CutoffPos;
        (double X, double Z) target;
        if (ThrowBag is >= 1 and <= 4) target = Geometry.Bag(ThrowBag);
        else if (_support.CutoffSpot is { } cutSpot) target = cutSpot;
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
            var lob = _flight.Hang(dt, R.Fielding.Throw.LobMaxSec);
            if (lob.First) _trace?.Mark(PlayTraceMarkKind.UncoveredWait, ElapsedSeconds, receiverPos, ThrowBag);
            (BallX, BallY, BallZ) = ThrowTo;
            if (!lob.Drops) return false;
            DropThrowAtBag();
            return false;
        }
        _trace?.Mark(PlayTraceMarkKind.Reception, ElapsedSeconds, receiverPos, ThrowBag);
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
        _flight.EndLob();
        _support.ClearCutoff();
        CatchGlove();
        _receivedClean = true;
        if (PlayerFielding || Seats.HumanOwnsThrow)
        {
            // The relay is player-owned (F693-03-relay-ownership, #723): the cutoff holds until commanded. A press remembered
            // inside fielding.throw.relayBufferSec fires now; otherwise the armed bag stays armed for the next press.
            if (_commands.TakeRelay())
            {
                var bag = _support.RelayTarget;
                _support.ArmRelay(0);
                ThrowBag = bag;
                BeginThrowToBag(bag);
                return true;
            }
            if (_support.RelayBag is >= 1 and <= 4) ThrowBag = _support.RelayBag;
            _support.ArmRelay(0);
            return false;
        }
        _cpuClock.Restart();
        return false;
    }

    /// <summary>The throw skips past its receiver (§8.5, §8.6): the ERROR. The ball rolls on, loose; the nearest body chases it.</summary>
    void SailThrow(string receiverPos)
    {
        _sailed = true;
        _receivedClean = false;
        Throwing = false;
        Caught = false;
        Buddy = false;
        _flight.EndLob();
        _support.ClearCutoff();
        var map = Assigned();
        var who = map.TryGetValue(receiverPos, out var r) ? r.Name : "the cover";
        Sub = $"It sails past {who}!";
        RaiseStamp(PlayStamp.ErrorTell());
        var dx = ThrowTo.X - ThrowFrom.X;
        var dz = ThrowTo.Z - ThrowFrom.Z;
        var len = Math.Sqrt(dx * dx + dz * dz);
        var flight = Math.Max(1, ThrowDur - R.Fielding.Throw.ReleaseSec);
        var speed = len / flight * R.Fielding.Overthrow.CarryMul;
        SetLoose(ThrowTo.X, ThrowTo.Z, len > 1e-6 ? dx / len * speed : 0, len > 1e-6 ? dz / len * speed : 0);
        TryHandoffLoose();
    }

    /// <summary>The lob nobody came for drops at the bag, live.</summary>
    void DropThrowAtBag()
    {
        Throwing = false;
        Caught = false;
        Buddy = false;
        _flight.EndLob();
        _support.ClearCutoff();
        SetLoose(ThrowTo.X, ThrowTo.Z, 0, 0);
        TryHandoffLoose();
    }

    void SetLoose(double x, double z, double vx, double vz)
    {
        _heldSince = -1;
        BallX = x;
        BallZ = z;
        BallY = 0;
        _looseMotion.Roll(vx, vz, ElapsedSeconds);
        _trace?.Mark(PlayTraceMarkKind.LooseBall, ElapsedSeconds, GlovePos);
    }

    // ---------------------------------------------------------------------------------
    // Handling errors (#721: F693-02-handling-error-opportunities, -ordinary-handling-error-chance, -ordinary-handling-error-cap,
    // -ordinary-handling-chance-curve, -awkward-hop-difficulty-source, -ordinary-bobble-outcome, -bobble-stun, -bobble-stun-duration,
    // -bobble-recovery-reliability, -bobble-direction-spread, -uniform-error-direction, -local-bobble-*)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The one roll an ordinary ground pickup can have (F693-02-ordinary-handling-error-chance): none at all unless the take is an
    /// awkward in-between hop, then <c>chanceCap × D × (1 − handsCut × H)</c> once, on the seeded stream. True when the take fails
    /// and the ball is loose as a local bobble.
    /// </summary>
    bool TryFumble(Character who)
    {
        var h = R.Fielding.Handling;
        var quality = FieldingResolver.HandlingQuality(who, R, _match.DefenseGlove);
        HandlingChance = FieldAbilities.SureScoop(who, R, BallY) ? 0 : FieldingResolver.HandlingErrorChance(HopDifficulty, quality, R);
        if (!_match.RollHandling(HandlingChance)) return false;
        // The outcome is the contact's, never a second roll (F693-02-error-outcome-selection): how squarely the ring met the ball and how
        // much speed the ball keeps. A glancing touch on a ball with pace gets past; anything else drops at the feet.
        var (vx, vy, vz) = _ballVel;
        var speed = Math.Sqrt(vx * vx + vz * vz);
        var window = FieldingResolver.CatchWindowFt(CatchRadius(Assigned()), dive: CatchDive, jump: false, R);
        ErrorObstruction = FieldingResolver.Obstruction(Diamond.Dist(GloveX, GloveZ, BallX, BallZ), window);
        var retention = FieldingResolver.DeflectionRetention(ErrorObstruction, R);
        if (FieldingResolver.DeflectionContinues(ErrorObstruction, speed, R))
            ContinuingDeflection(who, h, retention, vx, vy, vz);
        else
            LocalBobble(who, h);
        return true;
    }

    /// <summary>
    /// The ball gets past (F693-02-expanded-ordinary-error-outcomes, -continuing-error-direction, -speed-retention, -vertical-retention,
    /// -ground-response, -reaction, -recovery): it keeps <paramref name="retention"/> of its horizontal and signed vertical speed, turns a
    /// uniform ±<c>deflectSpreadDeg</c> off its travel, and is a batted ball from there on the shared flight and ground physics — the
    /// path continued from the contact, the ball reread for what it decides next. The fumbler is stunned the same 0.40 s; whoever
    /// reaches it takes it without a roll (the take's arming is spent).
    /// </summary>
    void ContinuingDeflection(Character who, HandlingRules h, double retention, double vx, double vy, double vz)
    {
        _bobbled = true;
        _receivedClean = false;
        Caught = false;
        PlayerBobble = true;
        Deflected = true;
        StunT = h.StunSec;
        StunPos = GlovePos;
        var speed = Math.Sqrt(vx * vx + vz * vz);
        var (dx, dz) = (vx / speed, vz / speed);
        var turn = _match.RollSpreadDeg(h.DeflectSpreadDeg) * Math.PI / 180.0;
        var (cx, cz) = (Math.Cos(turn), Math.Sin(turn));
        var (ox, oz) = (dx * cx - dz * cz, dx * cz + dz * cx);
        var s = retention * speed;
        if (Path is not null && Hit is not null)
        {
            Path = BallFlight.Continue(Path, ElapsedSeconds, BallX, BallY, BallZ, ox * s, retention * vy, oz * s, Hit.LaunchDeg, Hit.ExitVeloMph, Park, R, Hit.WindMul);
            if (Ball is not null) Ball = BattedBall.Reread(Path, Hit.ExitVeloMph, Hit.LaunchDeg, Ball.Shape == BattedBallClass.Bunt, Park, R);
            _ballPrev = null;
        }
        _events.Add(LiveEvent.Bobble);
        Sub = $"{who.Name} can't handle it!";
    }

    /// <summary>
    /// The failed take (F693-02-ordinary-bobble-outcome, -local-bobble-glove-release, -local-bobble-horizontal-retention,
    /// -bobble-direction-spread): the ball spills down from where it met the glove with no vertical speed, a fifth of its horizontal
    /// speed (six ft/s at most) along its travel turned a uniform ±30° — a ball with no horizontal travel spills away from the body —
    /// and the fumbler is stunned <c>stunSec</c>. The fair / foul call was made by the touch; the ball stays live for everyone else.
    /// </summary>
    void LocalBobble(Character who, HandlingRules h)
    {
        _bobbled = true;
        _receivedClean = false;
        Caught = false;
        PlayerBobble = true;
        StunT = h.StunSec;
        StunPos = GlovePos;
        var (vx, _, vz) = _ballVel;
        var speed = Math.Sqrt(vx * vx + vz * vz);
        double dx, dz;
        if (speed > 1e-9) (dx, dz) = (vx / speed, vz / speed);
        else
        {
            var ax = BallX - GloveX;
            var az = BallZ - GloveZ;
            var len = Math.Sqrt(ax * ax + az * az);
            (dx, dz) = len > 1e-9 ? (ax / len, az / len) : (0, 1);
        }
        var turn = _match.RollSpreadDeg(h.BobbleSpreadDeg) * Math.PI / 180.0;
        var (cx, cz) = (Math.Cos(turn), Math.Sin(turn));
        var (ox, oz) = (dx * cx - dz * cz, dx * cz + dz * cx);
        var s = Math.Min(h.BobbleRetain * speed, h.BobbleCapFtPerSec);
        _looseMotion.Bobble(ox * s, oz * s, inTheAir: BallY > 1e-9);
        _heldSince = -1;
        _trace?.Mark(PlayTraceMarkKind.LooseBall, ElapsedSeconds, GlovePos);
        _events.Add(LiveEvent.Bobble);
        Sub = $"{who.Name} bobbles it!";
    }

    /// <summary>
    /// One frame of the loose ball (<see cref="LooseBallMotion.Tick"/>): the local bobble falls from the contact, rebounds at 35 % of
    /// its downward speed under a six-inch ceiling, settles and rolls (F693-02-local-bobble-*); any other loose ball rolls to a stop
    /// at the <c>overthrow</c> deceleration. The ground numbers are the row of the zone the ball is in, each tick (FD-05, F3-c).
    /// </summary>
    void TickLooseBall(double dt)
    {
        if (_looseMotion.Tick(BallX, BallY, BallZ, dt, ElapsedSeconds, GroundZones.Of(Park, R), R) is { } at)
            (BallX, BallY, BallZ) = at;
    }

    /// <summary>The ball lands at the bag. True when the play is decided or waiting on the next press.</summary>
    bool OnThrowArrived(out LivePlayCommandResult result)
    {
        var bag = ThrowBag;
        Throwing = false;
        _flight.EndLob();
        _support.ClearCutoff();
        CatchGlove();
        _receivedClean = true;

        InPlay.GroundThrowStep? step = null;
        if (bag is >= 1 and <= 4)
        {
            var heading = RunnerSystem.HeadingTo(Runners, bag);
            // The close play (§9.6, D5): only when the ball is at a tag bag ahead of the body by no more than the margin.
            if (TryBeginClosePlay(bag))
            {
                result = new LivePlayCommandResult(Snapshot);
                return true;
            }
            var verdict = ArrivalVerdict(bag);
            if (verdict.Decided)
            {
                var landed = ApplyThrow(bag, verdict.RunnerBeats, ThrowerChar());
                step = landed.Throw;
                if (step is { } s && !string.IsNullOrEmpty(s.Caption))
                    Sub = s.Caption;
                MaybeStampCloseSafe(bag);
            }
            else if (heading is null && RunnerForBag(bag) is null && !Runners.Any(r => r.Live && r.IsOn(bag))
                     && (PlayerFielding || Seats.HumanOwnsThrow))
            {
                // Nobody to play on at that bag (S-34): a human's throw is named, nothing is decided. The CPU's
                // throw-in to the bag ahead of the lead runner (§8.8 rule 5) is the hold, not a wasted throw.
                LastMoment = new LiveMoment(InPlay.ThrowVerdict.Wasted, bag, ThrowerChar(), null);
                Sub = Caption;
            }
            // Otherwise the body is still coming, outside the margin: the glove holds on the bag and the tag rule runs (§10.3).
        }

        if (step is { } decided && WaitForNextThrow(decided))
        {
            result = new LivePlayCommandResult(Snapshot, step);
            return true;
        }
        // The receiver holds the ball: the CPU decides again from here after its reaction (the chain, §8.8 rule 1).
        _cpuClock.Restart();
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
        var at = Geometry.Bag(bag);
        var (x, z) = target.Position;
        var bags = R.Running.Bags;
        // On the bag is safe only when the bag is theirs (§9.1): a body arriving on a bag another runner holds is tagged there.
        if (Diamond.Dist(x, z, at.X, at.Z) <= bags.TagSafeRadiusFt && ProtectsOn(target, bag)) return (true, true);
        var reach = InPlay.TagReachFt(GloveChar(), R, target.Sliding);
        if (Diamond.Dist(x, z, at.X, at.Z) < reach) return (true, false);
        return (false, false);
    }

    /// <summary>A live tell this frame and this play (§15, #690). Geometry already decided; this is the sticker.</summary>
    void RaiseStamp(LiveStamp stamp)
    {
        if (string.IsNullOrEmpty(stamp.Word)) return;
        _stamps.Add(stamp);
        _stampsThisPlay.Add(stamp);
        _events.Add(PlayStamp.Cue(stamp));
    }

    /// <summary>A body that just crossed home: SCORE at the plate, once per runner.</summary>
    void StampNewScores()
    {
        foreach (var runner in Runners)
        {
            if (!runner.Scored || !_scoreTold.Add(runner)) continue;
            RaiseStamp(PlayStamp.ScoreTell());
        }
    }

    /// <summary>The runner in ahead of the throw by no more than the margin (§9.6): the small SAFE, at a bag or across the plate.</summary>
    void MaybeStampCloseSafe(int bag)
    {
        var runner = bag == 4
            ? Runners.FirstOrDefault(r => r.Scored && !double.IsNaN(r.ScoredAt))
            : EntitledOn(bag);
        if (runner is null || double.IsNaN(runner.LastTouchAt)) return;
        if (InPlay.CloseSafe(ElapsedSeconds, runner.LastTouchAt, R) && bag != 4)
            RaiseStamp(PlayStamp.SafeTell(bag));
    }

    /// <summary>The body on the ball: the glove that holds or chases it.</summary>
    Character PlayFielder()
    {
        if (Preview is null && !RunnerPlay) return Field?.Fielder ?? _match.Pitcher;
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
        if (RunnerPlay) return CommitRunnerPlay();
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
        var knock = pre.Grounder && hit is not null
            ? RecoilDur
            : 0;
        // The catch feat is typed on the outcome (§8.4, §15): the stamp reads it, never the client's mirror.
        var feat = kind is PlayKind.FlyOut or PlayKind.GroundOut && _gloved
            ? FieldingResolver.PlayerCatchFeat(pre, Park, R, Buddy, CatchJump, CatchDive)
            : DefensiveFeat.None;
        return new FieldingResult(kind, from, ArmedCut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace,
            ArmedThrow, pre.Buddy, Warped: (Field?.Warped ?? pre.Warped) || _redirectsThisPlay.Count > 0, Item: Field?.Item,
            Bobble: _bobbled, KnockbackSec: knock, Feat: feat, GroundRule: pre.Ball is { GroundRule: true },
            Caught: _gloved, ThrowSailed: _sailed, ItemHit: Field?.ItemHit ?? false, ItemTarget: Field?.ItemTarget,
            RedirectType: _redirectsThisPlay.Count > 0 ? _redirectsThisPlay[^1].Type : null);
    }

    // ---------------------------------------------------------------------------------
    // Catch, bobble, tag
    // ---------------------------------------------------------------------------------

    void CatchGlove()
    {
        if (!Caught && !_gloved) _events.Add(LiveEvent.Glove);
        _heldSince = ElapsedSeconds;
        Caught = true;
        _gloved = true;
        _looseMotion.Held();
        _firstGlove ??= GloveChar();
        _trace?.Mark(PlayTraceMarkKind.Possession, ElapsedSeconds, GlovePos);
    }

    /// <summary>A glove takes a thrown or loose ball: no fair / foul call, no bobble roll.</summary>
    void TakeBall()
    {
        _receivedClean = false;
        CatchGlove();
        _cpuClock.Restart();
    }

    /// <summary>
    /// The glove takes the batted ball, and that touch makes the fair / foul call (§5.6): in the
    /// air it is a catch (an out wherever the ball was); on the ground it is fair or foul by
    /// where the ball is. A loose ball picked up again was called already. The scoop or catch
    /// may bobble (§8.6): a fumble with time and scatter, never a caption.
    /// </summary>
    void TakeBattedBall()
    {
        _receivedClean = false;
        var first = !Caught;
        var wasLoose = LooseBall;
        if (first && !wasLoose && Preview is { } preview)
        {
            var who = GloveChar();
            var bonus = FieldAbilities.CatchBonus(who, R)
                + (preview.Grounder ? FieldAbilities.GroundRangeBonus(who, R, BallY) : 0);
            if (bonus > 0)
            {
                var ordinary = CatchRadius(Assigned()) - bonus;
                var d = Diamond.Dist(GloveX, GloveZ, BallX, BallZ);
                var ordinaryCouldTake = preview.Grounder
                    ? d < FieldingResolver.CatchWindowFt(ordinary, CatchDive, CatchJump, R)
                    : FlyCatch.InPosition(preview, GloveX, GloveZ, BallX, BallZ, BallY,
                        FlyCatch.ChaseTarget(preview, R, Park).X, FlyCatch.ChaseTarget(preview, R, Park).Z,
                        CatchDive ? FieldingResolver.DiveCatchFt(ordinary, R) : ordinary,
                        ElapsedSeconds, Hang, FlyCatch.NeedsJump(preview), R);
                if (!ordinaryCouldTake) RecordFact(new ReachBonusTake(who.Id, who.FieldAbility));
            }
        }
        // The ball's speed the frame before the take (F693-02-ground-pickup-recoil-basis): the one input the recoil reads,
        // sampled on both tables before possession attaches the ball to the glove.
        if (first) _recoil.SampleIncoming(_ballVel);
        CatchGlove();
        _cpuClock.Restart();
        if (!first || Preview is null) return;
        if (_call == FairFoulCall.Undecided && !wasLoose)
        {
            // In the air = before the ball's first ground contact (the landing mark), read off the path, not a height.
            var inTheAir = ElapsedSeconds < Hang;
            _call = inTheAir ? FairFoulCall.Caught
                : FieldBounds.IsFair(BallX, BallZ) ? FairFoulCall.Fair
                : FairFoulCall.Foul;
        }
        // On the ground = a grounder, or any ball past its landing: what the impact recoil (#720) charges for a pickup.
        var landed = ElapsedSeconds >= Hang;
        // The take's difficulty (F693-02-awkward-hop-difficulty-source, #721): the hop the ball is in, read off its height and rise. Sampled on both tables.
        HopDifficulty = landed && !wasLoose ? FieldingResolver.HopDifficulty(BallY, _ballVel.Y * R.Flight.TimeScale, R) : 0;
        if (!wasLoose) ArmRecoil(landed);
    }

    void ArmRecoil(bool landed)
    {
        if (_recoil.Charged || Preview is null || Buddy) return;
        // #721: a legal routine pickup never rolls; an awkward in-between hop rolls once, and a failed take is a local bobble.
        // A clean take then pays the impact recoil (#720) as any other.
        if (landed && Hit is not null)
        {
            _recoil.MarkCharged();
            var taker = GloveChar();
            if (TryFumble(taker)) return;
            ArmImpact(taker);
            return;
        }
        if (!landed || !Preview.Grounder)
        {
            // A landed liner or fly picked up off the grass: no bobble roll, and none here. The take still
            // costs what its speed says (F693-02-ground-pickup-recoil-basis).
            if (landed && Hit is not null)
            {
                _recoil.MarkCharged();
                ArmImpact(GloveChar());
            }
            // A hard ball caught in the air by a body on its feet (F693-02-grounded-air-catch-recoil, #720 slice 2): the same
            // response off the airborne anchors. A dive, a jump, a buddy leap or a body still in the air is not this take.
            else if (!landed && Hit is not null && Grounded)
            {
                _recoil.MarkCharged();
                ArmImpact(GloveChar(), airborne: true);
            }
            return;
        }
        _recoil.MarkCharged();
    }

    /// <summary>The take's impact recoil (<see cref="GloveRecoil.Arm"/>), announced as a live event when it costs anything.</summary>
    void ArmImpact(Character who, bool airborne = false)
    {
        if (_recoil.Arm(who, airborne, _ballVel, R)) _events.Add(LiveEvent.ImpactRecoil);
    }

    /// <summary>On its feet at the take: not airborne on a jump, not in a jump or dive window, not a diving or jumping catch.</summary>
    bool Grounded => !Airborne && JumpT <= 0 && DiveT <= 0 && !CatchDive && !CatchJump;

    /// <summary>One frame of the impact recoil: the glove (and the ball in it) moves by the kick's share of the frame (<see cref="GloveRecoil.Step"/>).</summary>
    void TickImpactRecoil(double dt)
    {
        if (_recoil.Step(dt) is not { } move) return;
        var next = FieldBounds.ClampFielder(Park, GloveX + move.X, GloveZ + move.Z, R);
        GloveX = next.X;
        GloveZ = next.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
        if (HoldsBall)
        {
            BallX = GloveX;
            BallZ = GloveZ;
        }
    }

    /// <summary>
    /// The thrown item lands after its flight (§12), by geometry: a banana is a peel on the grass at the
    /// aimed body's feet that stays and slips whoever steps on it; a rocket dazes the body it was aimed at
    /// (North smashes it in the air); a POW keeps every ball on the dirt hopping. No roll.
    /// </summary>
    void LandItem()
    {
        if (Field is not { ItemHit: true, Item: { } item }) return;
        var pos = Field.ItemTarget is not null ? PosOf(Assigned(), Field.ItemTarget) : GlovePos;
        if (string.IsNullOrEmpty(pos)) pos = GlovePos;
        var feet = _fielders.TryGetValue(pos, out var at) ? at : (GloveX, GloveZ);
        if (_items.Land(item, feet, R) is { } sec)
            Foil(pos, sec);
    }

    /// <summary>A body on a peel or dazed by a rocket is off the ball for <paramref name="sec"/>; holding the ball, it drops it where it stands.</summary>
    void Foil(string pos, double sec)
    {
        _items.KeepOff(pos, sec);
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
        if (Hit is null && !RunnerPlay) return false;
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
        return FlyCatch.PlayerKind(false, Preview, inAir: true, foul: FoulNow);
    }

    // ---------------------------------------------------------------------------------
    // Close play (spec §9.6, D5): the mash only when the ball is at third or home ahead of the body by no more than the margin
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The throw just landed in the cover's glove on <paramref name="bag"/>. A tag bag, an unforced
    /// body bound there and short of it, and its arrival inside running.close.marginSec from now: the
    /// mash contest. Anything else is decided by the geometry, silently.
    /// </summary>
    bool TryBeginClosePlay(int bag)
    {
        var heading = RunnerSystem.HeadingTo(Runners, bag);
        if (heading is null || heading.IsOn(bag) || !ClosePlay.Offered(bag, Forces, true))
            return false;
        // A bag another runner holds is no race (§9.1): arriving there is not safe, so there is no close play to mash.
        if (!ProtectsOn(heading, bag)) return false;
        var runnerAt = RunnerSystem.ArrivalSec(heading, bag, ElapsedSeconds, R, Dash01);
        if (!ClosePlay.WithinMargin(runnerAt, R)) return false;
        // The body in the play waits for the verdict (the mash is the slide): safe puts them on the bag, out retires them.
        _close.Begin(heading, bag);
        return true;
    }

    LivePlayCommandResult TickClosePlay(double dt, LivePadInput field, LivePadInput run)
    {
        if (!_close.Clock(dt, R))
        {
            if (_close.Icon) _events.Add(LiveEvent.CloseIcon);
            return new LivePlayCommandResult(Snapshot);
        }

        var runner = _close.Runner?.Who;
        var fielder = PlayFielder();
        var offenseHuman = Seats.Versus ? Seats.HumanBats : Seats.HumanBats && !Seats.PlayerMustField && !PlayerFielding;
        var defenseHuman = PlayerFielding || Seats.HumanPitches || Seats.PlayerMustField;
        if (_close.Decide(offenseHuman, run.SouthDown, runner?.Stats.Run ?? 5,
                defenseHuman, field.CloseResponse ?? field.SouthDown, fielder.Stats.Hands, R) is not { } safe)
            return new LivePlayCommandResult(Snapshot);
        if (_close.Runner is { Live: true } body)
        {
            // The verdict is written once (§9.6): the body is on the bag, or the out is recorded; the caption follows the record.
            if (safe)
            {
                body.Arrive(CloseBag, ElapsedSeconds);
                _trace?.Mark(PlayTraceMarkKind.RunnerAward, ElapsedSeconds, GlovePos, CloseBag, PlayTraceRunner.Of(body));
            }
            else if (!Retire(body.FromBag, CloseBag, OutType.Tag, PlayFielder())) safe = true;
            if (!safe) LastMoment = new LiveMoment(InPlay.ThrowVerdict.TagOut, CloseBag, PlayFielder(), body.Who);
            Throws++;
        }
        else
            ApplyThrow(CloseBag, safe, PlayFielder());
        Sub = ClosePlay.Caption(CloseBag, safe);
        StampNewScores();
        if (safe && CloseBag != 4) RaiseStamp(PlayStamp.SafeTell(CloseBag));
        _match.CreditClosePlay(safe ? runner : fielder);
        _close.End();
        _cpuClock.Restart();
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
        // The item is in the air for its flight (batting.items.flySec); it lands by geometry when the clock gets there.
        _items.LandAt(Field is { ItemHit: true } ? ElapsedSeconds + R.Batting.Items.FlySec : -1);
        return new LivePlayCommandResult(Snapshot);
    }

    LivePlayCommandResult SmashItem()
    {
        if (Field is not null)
            Field = ErrorItems.Smash(Field, Preview is not null && Preview.Grounder);
        _items.Cancel();
        _events.Add(LiveEvent.ItemSmashed);
        Sub = "Item smashed.";
        return new LivePlayCommandResult(Snapshot);
    }

    // ---------------------------------------------------------------------------------
    // The runner play (§11.3, §11.4): the catcher's throw after a take or a miss, the pickoff from the rubber
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Begin a dead-ball runner play: the ball is in the catcher's glove at the crossing (the pitcher's
    /// on the rubber for a pickoff), every cover stands on the bag a body is bound for, and the bodies
    /// who broke are on the path with their head start. The CPU catcher's release is its own clock
    /// (fielding.catcher, one seeded stream, S-92); the pickoff throw goes at once.
    /// </summary>
    LivePlayCommandResult BeginRunnerPlay(PlayEvent? pitch, int pickoffBag, LiveSeats seats)
    {
        if (pitch is null) return new LivePlayCommandResult(Snapshot);
        Reset();
        _events.Clear();
        _stamps.Clear();
        _stampsThisPlay.Clear();
        _slows.Clear();
        _slowsThisPlay.Clear();
        _facts.Clear();
        _factsThisPlay.Clear();
        _scoreTold.Clear();
        RunnerPlay = true;
        PickoffBag = pickoffBag;
        StealPitch = pitch;
        Seats = seats;
        PlayerFielding = FieldAssist.PlayerStartsOnGlove(Seats.PlayerMustField);
        var result = Begin(LivePlayCommand.Begin(PlayKind.InPlay));
        if (!Active) return result;
        if (pickoffBag == 0 && !Runners.Any(r => (r.Live || r.Scored) && r.Broke))
        {
            // Nobody broke: there is no play (a walk entitled every armed runner).
            Reset();
            return new LivePlayCommandResult(Snapshot);
        }
        InitRunnerGloves(pickoffBag);
        _bodySlows.Begin(ParkHazards.StatusVolumes(Park, R, _match.Night));
        CatchGlove();
        HasBall = true;
        BallX = GloveX;
        BallY = R.Fielding.Catch.HeldBallY;
        BallZ = GloveZ;
        if (pickoffBag > 0)
        {
            BeginThrowToBag(pickoffBag);
            return new LivePlayCommandResult(Snapshot);
        }
        var map = Assigned();
        var catcher = map.TryGetValue("C", out var c) ? c : _match.Pitcher;
        _cpuClock.ArmAt(_match.RollCatcherRelease(catcher));
        var buffered = _match.PitchSetup.TakeCatcherInput();
        if (seats.HumanOwnsThrow && buffered is { } pad)
        {
            ReadThrowBag(pad, true);
            if (pad.SouthDown) BeginPlayerThrowOrCommit(map, pad);
        }
        return new LivePlayCommandResult(Snapshot);
    }

    /// <summary>
    /// The formation of a runner play: every body at its position, the glove on the ball at the plate
    /// (the rubber on a pickoff), the cover of every bag a body is bound for standing on it (the middle
    /// infielder breaks to the bag on the pitch; the first baseman holds the runner on), and, with a
    /// runner on third watching a steal of second, the free middle infielder cutting in front of the
    /// bag (running.steal.cutInFrontFt) for the delayed double steal (S-66).
    /// </summary>
    void InitRunnerGloves(int pickoffBag)
    {
        _fielders.Clear();
        foreach (var kv in Assigned())
            _fielders[kv.Key] = Starts[kv.Key];
        SwapLock = 0;
        GlovePos = pickoffBag > 0 ? "P" : "C";
        var spot = pickoffBag > 0 ? Geometry.Rubber : StealThrow.CatcherSpot(R);
        GloveX = spot.X;
        GloveZ = spot.Z;
        _fielders[GlovePos] = (GloveX, GloveZ);
        // The cover map of a runner play is read where the ball is when it forms; the throw from a bag later reads the same one.
        CoverBallX = GloveX;
        var bags = new HashSet<int>();
        if (pickoffBag > 0) bags.Add(pickoffBag);
        foreach (var r in Runners)
            if (r.Live && r.Broke && r.DestBag is >= 1 and <= 4) bags.Add(r.DestBag);
        foreach (var bag in bags)
        {
            // The glove's own bag is the glove's (the catcher at the plate on a steal of home): nobody else stands on it.
            if (FieldAssist.CoverKey(bag) == GlovePos) continue;
            var pos = CoverOf(bag);
            if (string.IsNullOrEmpty(pos) || pos == GlovePos) continue;
            _fielders[pos] = Geometry.Bag(bag);
        }
        var runnerOnThird = Runners.Any(r => r.Live && !r.Broke && r.IsOn(3));
        if (pickoffBag == 0 && runnerOnThird && bags.Contains(2))
        {
            var cover = CoverOf(2);
            var free = cover == "SS" ? "2B" : cover == "2B" ? "SS" : "";
            if (!string.IsNullOrEmpty(free) && free != GlovePos && _fielders.ContainsKey(free))
            {
                var second = Geometry.Bag(2);
                var len = Math.Max(1, Diamond.Dist(Diamond.Home.X, Diamond.Home.Z, second.X, second.Z));
                var cut = R.Running.Steal.CutInFrontFt;
                _fielders[free] = (second.X - (second.X - Diamond.Home.X) / len * cut, second.Z - (second.Z - Diamond.Home.Z) / len * cut);
            }
        }
    }

    LivePlayCommandResult CommitRunnerPlay()
    {
        if (StealPitch is null)
        {
            Reset();
            return new LivePlayCommandResult(Snapshot, FlightDone: true);
        }
        var play = _match.FinishRunnerPlay(StealPitch, PickoffBag, FirstThrowBag);
        Reset();
        return new LivePlayCommandResult(Snapshot, CompletedPlay: play);
    }
}
