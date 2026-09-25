using System;
using System.Collections.Generic;
using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Set, pitch, swing, contact (a real director since #1042). It owns the at-bat's state — the mound's charge button and
    /// pitch selection, the CPU's delivery, the batter's charge and bunt side, the plate's step, the special's requests —
    /// SET's camera, and the sequence from SET to the batted ball it hands the live play. Out/safe stay in Sim. The coach,
    /// the banner, the result beat and the directors it calls on are the flow's (<see cref="IAtBatHost"/>).
    /// </summary>
    internal sealed class AtBatDirector : ISetCameraHost
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly LiveFieldState _live;
        readonly SeatPads _pads;
        readonly IAtBatHost _host;
        SetCamera _setCam;

        public AtBatDirector(MatchScene scene, PlayState play, LiveFieldState live, SeatPads pads, IAtBatHost host)
        {
            _scene = scene; _play = play; _live = live; _pads = pads; _host = host;
        }

        /// <summary>SET's camera and the pitcher's aim it leans toward.</summary>
        public SetCamera SetCam => _setCam ??= new SetCamera(_scene, _play, _pads, this);
        float ISetCameraHost.PitchCharge => PitchCharge;

        /// <summary>The mound's charge button, its fill and its seconds past full. Fields: the charge step writes them by ref.</summary>
        public ChargeButtonState PitchButton;
        public float PitchCharge;
        public float PitchPast;
        /// <summary>The batter's seconds past a full charge.</summary>
        public float ChargePast;

        /// <summary>This frame's plate input and step, read at the plate plane and at contact.</summary>
        public PlateInput PlateInput;
        public PlateButtonsStep PlateStep;

        /// <summary>
        /// The mound's pre-charge family selection (spec §3, PH-02-R3/R4/R5). The state is the
        /// client's; the step that moves it is the sim's (<see cref="Match.SelectPitch"/>).
        /// </summary>
        public PitchSelectionState PitchSelect;

        /// <summary>
        /// The CPU's delivery, decided at the top of SET (§4.8, PH-18-R1) so the body has SET to
        /// walk to the rubber it solved for. Null in a tutorial, whose pitch is scripted.
        /// </summary>
        public PitchCommand CpuPitch;

        /// <summary>The direction that CPU delivery holds the stick, drawn a frame at a time in flight.</summary>
        public int CpuSteer;

        /// <summary>The human batter's held bunt side on this tick (§5.8): the plate's side while squared, else none.</summary>
        public BuntSide BuntSide;

        /// <summary>The special's modifier, the requests and the unavailable tell (spec §12).</summary>
        public readonly StarRequests StarAsks = new StarRequests();

        /// <summary>
        /// A new pitch (SET): no charge on either button, the fastball unlocked (PH-02-R5: nothing on the shared screen
        /// marks the active family, so the player counts presses from a known start every time), no CPU delivery, no bunt
        /// side, no special asked.
        /// </summary>
        public void NewPitch()
        {
            PitchCharge = 0;
            ChargePast = 0;
            PitchButton = default;
            PitchSelect = PitchSelectionState.Reset;
            CpuPitch = null;
            CpuSteer = 0;
            BuntSide = BuntSide.None;
            StarAsks.NewPitch();
            PitchPast = 0;
        }

        /// <summary>
        /// The side the batter shows right now (§5.8, PH-14-R3): a human's held trigger, or the side the CPU batter
        /// drew with its square at SET. Public on the batter card for both seats.
        /// </summary>
        public BuntSide ShowingSide => _pads.HumanBats ? BuntSide : _play.Match != null ? _play.Match.CpuBatter.BuntSide : BuntSide.None;
        /// <summary>The batter is squared right now: a bunt trigger held (a human), or the CPU batter's square read at SET.</summary>
        public bool SquaredNow => _pads.HumanBats ? BuntSide != BuntSide.None : _play.Match != null && _play.Match.CpuBatter.Squared;

        public void TickAtBat(float dt)
        {
            if (_play.Phase == MatchDirector.Phase.Set) TickSet(dt);
            else if (_play.Phase == MatchDirector.Phase.Flight) TickFlight(dt);
            // Off the plate the triggers and East square nothing and cancel nothing, but a spent hold still has to be
            // seen coming up (PH-14-R6, PH-13-R1) before the live ball's readers (after this tick) take it again.
            else if (_pads.HumanBats) TickPlate(dt, accepting: false, commits: false);
        }

        /// <summary>
        /// One plate tick for the batting seat (spec §5.8's one-tick order, in the sim): the bunt, then the swing
        /// button with the square and East / G as its cancel. A tutorial plate lesson is handed the same input so its
        /// verdict rests on the player's own presses.
        /// </summary>
        public PlateButtonsStep TickPlate(float dt, bool accepting, bool commits)
        {
            var pad = _pads.BatPad;
            if (pad.Index != _play.PlateSeat)
            {
                _play.Plate = default;
                _play.PlateSeat = pad.Index;
            }
            PlateInput = pad.Plate;
            PlateStep = PlateButtons.Advance(_play.Plate, PlateInput, dt, _scene.Feel.SwingChargeSeconds, accepting, commits);
            _play.Plate = PlateStep.Next;
            if (accepting && _host.TutorialOn && _host.Coach.Tutorial.Phase == TutorialPhase.Attempt)
                _host.Coach.Tutorial.Plate(new TutorialPlateTick(PlateInput, dt, commits));
            return PlateStep;
        }

        public void BeginSet()
        {
            _host.BindSeats();
            if (_pads.TrainingOn && (_play.Match == null || _play.Match.Over))
                _play.Match = _host.NextTrainingMatch();
            _play.Phase = MatchDirector.Phase.Set;
            Controls.CatchPlay();
            Controls.ClearTargets();
            _play.Match?.ControllerRunners.Reset();
            _play.T = 0;
            _play.NewPitch();
            _live.NewPitch();
            NewPitch();
            _host.Steal.NewPitch();
            // The next pitch (§5.8): the must-release, the spent triggers and a spent cancel carry; a hold must come up.
            _play.Plate = _play.Plate.NextPitch();
            _host.Swap.Close();
            if (_play.Match != null) _play.Match.Dash01 = 0;
            _play.Match?.LivePlay.Apply(LivePlayCommand.Reset());
            _host.Toss.Reset();
            _scene.Items?.Hide();
            _host.BannerText = _host.Sub = "";
            // The body starts this SET where the last pitch left it; the rubber persists (§4.2).
            _play.MoundX = (float)_play.Match.PitcherOffsetX;
            var rel = PitchFlight.Release(_play.Match.Rules, _play.Match.PitcherOffsetX);
            _play.Ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            _scene.Park.Ball.Place(_play.Ball, "", PitchFamily.Fastball, false, false);
            HoldPitchInHand();
            SetCam.AimAt(0, 0);
            _host.Smash = 0;
            _scene.Audio?.CrowdBed(true);
            SetCam.Aim();
            SetCam.Log("begin");
            ShowCursor();
            if (_pads.TrainingOn && _host.Coach != null && _host.Coach.Session != null && _play.Match != null
                && _host.Coach.Session.Lesson == PracticeLesson.Fielding && _host.Coach.Session.LessonPart >= 2)
                _host.Coach.Session.SetupTurnTwo(_play.Match);
        }

        public void TickSet(float dt)
        {
            if (_play.T > 0.2f && _play.T < 0.28f) SetCam.Log("live");
            HoldPitchInHand();
            var mound = _pads.PitchPad;
            var box = _pads.BatPad;
            var pitchButton = default(ChargeButtonStep);
            var pitchFamily = PitchFamily.Fastball;
            var wasPicking = _host.Swap.Open;
            if (_pads.HumanPitches && !_play.Match.PitchSetup.Committed) _host.Swap.Tick(dt, mound);
            if (wasPicking || _host.Swap.Open)
            {
                // The window owns this frame, including its open/close edge. No pickoff,
                // rubber walk, steal or banked charge can leak through a menu action.
                TickChargeButton(dt, _scene.Feel.PitchChargeSeconds, mound,
                    ref PitchButton, ref PitchCharge, ref PitchPast, accepting: false);
                PitchSelect = PitchSelect with { Locked = false };
                if (_pads.HumanBats) TickPlate(dt, accepting: false, commits: false);
                _play.Charge = ChargePast = 0;
                BuntSide = BuntSide.None;
                return;
            }
            // A legal base throw is read before South can begin a pitch charge.
            if (_pads.HumanPitches && _host.Steal.ReadSetupThrow(mound, _play.T >= (float)_scene.Feel.PitcherReadySeconds && !_host.Swap.Open)) return;
            // The arm edge is read from the button as it stood *before* this tick's step (#813).
            var prevPitchButton = PitchButton;
            if (_pads.HumanPitches)
                pitchButton = TickChargeButton(dt, _scene.Feel.PitchChargeSeconds, mound,
                    ref PitchButton, ref PitchCharge, ref PitchPast,
                    _play.T >= (float)_scene.Feel.PitcherReadySeconds && !_host.Swap.Open);
            else
                PitchCharge = Mathf.Clamp01(_play.T / Mathf.Max(0.12f, (float)_scene.Feel.PitcherReadySeconds));
            if (_pads.HumanPitches && (pitchButton.Next.Armed || pitchButton.Committed) && !_play.Match.PitchSetup.Committed)
                _host.Steal.CommitCharge();
            if (_pads.HumanPitches)
            {
                // One SET tick of the cycle (spec §3, PH-02-R3/R4/R5). Cycling is legal before the
                // pitcher-ready beat — a selection is not a delivery — but not while the swap pick
                // owns the stick and the button (§4.7), so the gate is the seat, not `accepting`.
                var selection = _play.Match.SelectPitch(PitchSelect, mound.CyclePitch,
                    _pads.HumanPitches && !_host.Swap.Open, prevPitchButton, pitchButton);
                PitchSelect = selection.Next;
                // On the commit tick this is the locked family of the delivery leaving the hand;
                // `Next` has already reset to the fastball for the SET after it.
                pitchFamily = selection.Family;
            }
            if (_pads.HumanBats)
            {
                // A press during SET is not a swing (spec §3): the hold builds, the release drops. The triggers
                // square in SET too (§5.8), and East / G discards a load here as in the flight (§5.1).
                var plate = TickPlate(dt, accepting: true, commits: false);
                _play.Charge = (float)_play.Plate.Swing.Fill01;
                ChargePast = (float)_play.Plate.Swing.SecondsPastFull;
                BuntSide = plate.Bunt.Showing;
            }
            else
            {
                _play.PlateSeat = -1;
                _play.Plate = default;
            }
            _play.Pip += dt * (float)_scene.Feel.PipPulseHz;
            // The CPU seats' SET verbs (spec §4.7, §11.6): a tired arm swaps; the runner AI's steal table runs once per at-bat.
            if (!_pads.HumanPitches && _play.T < dt) _play.Match.CpuConsidersSwap();
            if (!_pads.HumanBats && _play.T < dt) _play.Match.CpuBatter.ArmSteal();
            // The CPU batter's square is read at SET (§5.9, §7.3) so a human pitcher sees it before the pitch.
            if (!_pads.HumanBats && _play.T < dt) _play.Match.CpuBatter.SquaresBunt();
            // The CPU decides its delivery at the top of SET, the way a hand decides before it
            // charges (§4.8, PH-18-R1): the rubber the model solves for is then somewhere to walk to
            // during SET instead of a place to appear at on the release frame.
            if (!_pads.HumanPitches && _play.T < dt && CpuPitch == null && !_host.TutorialOn)
            {
                CpuPitch = _play.Match.CpuPitcher.PitchByInputs(out var cpuPlan);
                CpuSteer = Math.Sign(cpuPlan.SteerDir);
            }
            // The held special modifier (PH-16-R11): no arming. The card reads STAR while it is down, free and paid for;
            // the release reads it.
            StarAsks.PitchShown = _pads.HumanPitches && StarAsks.Ready(mound) && _play.Match.CanStarPitch;
            StarAsks.SwingShown = _pads.HumanBats && StarAsks.Ready(box) && _play.Match.CanStarSwing;
            TickBaserunning(dt);
            if (_host.Steal.Advance(dt)) return;
            if (_pads.HumanBats)
            {
                // Down resets the box in SET only (§5.4); in flight the same axis aims launch.
                if (box.StickY < -(float)_scene.Feel.SetResetStick) _play.Match.ResetBatter();
                else _play.Match.WalkBatter(HomeSet.BoxWalkStep(box.StickX, dt));
            }
            // The square is a clock (§7.3): the defense crashes for as long as it has been held; released, it winds back.
            TickSquare(dt, SquaredNow);
            if (_pads.HumanPitches)
            {
                if (_host.Swap.Open || _play.Match.PitchSetup.Committed) { }
                else if (mound.StickY < -(float)_scene.Feel.SetResetStick) _play.Match.ResetPitcher();
                else _play.Match.WalkPitcher(HomeSet.RubberWalkStep(SetCam.WorldX(mound.StickX), dt));
                _play.MoundX = (float)_play.Match.PitcherOffsetX;
                SetCam.AimAt((float)_play.Match.PitcherOffsetX, 0);
                if (_play.T >= (float)_scene.Feel.PitcherReadySeconds)
                {
                    if (pitchButton.Committed)
                    {
                        Launch(PlayerPitch(pitchButton.CommitFill01, pitchButton.CommitSecondsPastFull, pitchFamily,
                            StarAsks.Release(mound)));
                        return;
                    }
                }
            }
            if (!_pads.HumanPitches)
                // The CPU's body walks to the rubber its delivery solved for, at the rate a hand
                // walks (§4.8). Presentation only: the delivery already carries that rubber, and
                // with no plan (a tutorial) the body sits on the match's own value.
                _play.MoundX = CpuPitch != null
                    ? Mathf.MoveTowards(_play.MoundX, (float)_play.Match.PitcherOffsetX, (float)HomeSet.RubberWalkStep(1f, dt))
                    : (float)_play.Match.PitcherOffsetX;
            ShowCursor();
            ShowAimTell(_pads.HumanPitches ? PreviewPitch(pitchFamily) : null);
            SetCam.Aim();
            if (!_pads.HumanPitches && _play.T > (float)_scene.Feel.PitcherReadySeconds)
            {
                // The CPU pitcher's pickoff read (§4.5, §4.8): a runner who armed in SET is between bags on the motion.
                var pickoffBag = _host.TutorialOn ? 0 : _play.Match.CpuPitcher.PickoffBag();
                if (pickoffBag > 0)
                {
                    _host.Steal.BeginPickoff(pickoffBag);
                    return;
                }
                Launch(_host.TutorialOn && _pads.HumanBats ? _host.Coach.Tutorial.CpuPitch : CpuPitch ?? _play.Match.CpuPitcher.Pitch());
            }
        }

        /// <summary>
        /// The pitcher card's verb tells: STAR and the swap pick (spec §4.1, §4.7). No family tell —
        /// the card is shared by both seats and the selection is made before the charge, so naming
        /// the held pitch would hand it to the batter (PH-02-R5). The repertoire row the card shows
        /// instead is <see cref="BroadcastHud.PitcherPitches(Match)"/> and depends on nothing held.
        /// </summary>
        public string PitcherExtra()
        {
            if (_play.Match == null) return "";
            var set = _play.Phase == MatchDirector.Phase.Set && _pads.HumanPitches;
            if (set && _play.Match.PitchSetup.Committed) return BroadcastHud.ShortFamily(_play.Match.FamilyAt(PitchSelect)) + " · " + BroadcastHud.PitchCommitted;
            return BroadcastHud.PitcherExtra(
                StarAsks.PitchShown && _pads.HumanPitches,
                set ? BroadcastHud.PitchCycle(BroadcastHud.ShortFamily(_play.Match.FamilyAt(PitchSelect))) : null,
                set && !_host.Swap.Open && _play.Match.CanArrangeDefense);
        }

        /// <summary>
        /// The pitcher's shape for the pose and the ball. Family-blind until the delivery exists
        /// (PH-02-R5): in SET the pose is the fastball's whatever is selected, and the real family
        /// arrives with <c>_play.Pitch</c> at the launch, for the throw itself.
        /// </summary>
        public string ShownPitchType => _play.Pitch != null ? _play.Pitch.Type : PitchFamily.Fastball;

        /// <summary>The pitch as it stands in SET: the selected family, the rubber, the charge so far. Not committed.</summary>
        PitchCommand PreviewPitch(string family) =>
            new(family, EffectiveCharge(PitchCharge, PitchPast),
                StarAsks.PitchShown && _play.Match.CanStarPitch, RubberX: _play.Match.PitcherOffsetX);

        /// <summary>
        /// The SET ring (spec §4.4, PH-06, PH-06-R1). Pitching seat only, and in ordinary play it is
        /// the <b>rubber</b> — where the pitcher stands, at the middle of the strike frame — for SET
        /// alone. The crossing now carries the family's drop and its natural sweep (§4.2, §4.3), so
        /// a ring drawn there would name the selected family on the shared screen before the ball
        /// left the hand, which PH-02-R5's family-blind SET forbids; and in flight the ball is the
        /// cue, so the ring hides at release.
        ///
        /// <para>
        /// Practice and the Tutorials keep the full crossing ring, through the flight (PH-19): there
        /// the shape <i>is</i> the lesson and there is no opponent to leak it to.
        /// </para>
        /// </summary>
        /// <param name="pitch">The pitch as it stands, for the teaching ring; null hides the tell.</param>
        void ShowAimTell(PitchCommand pitch)
        {
            var teaching = _pads.TrainingOn || _host.TutorialOn;
            if (pitch == null || _play.Match == null
                || !SetTells.AimTellOn(_pads.HumanPitches, _play.Phase == MatchDirector.Phase.Set, _play.Phase == MatchDirector.Phase.Flight, teaching))
            {
                _scene.Zone.AimTell(false, 0, 0);
                return;
            }
            var (x, y) = teaching
                ? SetTells.Locator(pitch, _play.Match.BatterZone, _play.Match.Rules, _play.Match.Pitcher.StarPitch)
                : SetTells.RubberRing(_play.Match.PitcherOffsetX, _play.Match.BatterZone);
            _scene.Zone.AimTell(true, (float)x, (float)y);
        }

        /// <summary>
        /// The gold oval follows the batter and shows this swing's barrel (contact, charge):
        /// the sim's own oval (<see cref="SweetSpot.Oval"/>), the one the resolver judges (S-134).
        /// </summary>
        public void ShowCursor()
        {
            if (_play.Match == null) return;
            _scene.Zone.Show(SweetSpot.Oval(_play.Match.Batter, _play.Match.OffenseBat, EffectiveCharge(_play.Charge, ChargePast),
                _play.Match.BatterOffsetX, _play.Match.Rules), _play.Match.BatterZone);
        }

        static ChargeButtonStep TickChargeButton(float dt, double seconds, Controls.Pad pad,
            ref ChargeButtonState state, ref float charge, ref float past, bool accepting = true,
            bool commits = true)
        {
            var step = ChargeButton.Advance(state, pad.BallDown, pad.BallHeld, pad.BallUp, dt, seconds,
                accepting, commits);
            state = step.Next;
            charge = (float)state.Fill01;
            past = (float)state.SecondsPastFull;
            return step;
        }

        float EffectiveCharge(float charge, float past) =>
            (float)ChargeFeel.Effective01(charge, past, _scene.Feel.ChargeMaxHoldSeconds, _scene.Feel.ChargeOverchargeDecay, _play.Match.Rules);

        /// <summary>
        /// The human's pitch (spec §4.1 – §4.2, §3): shape from the family the charge locked,
        /// charge from the release, location from the rubber walk alone (the crossing moves with the
        /// body, once), height from the shape (AimY is not a stick), Nice! from the release band.
        /// </summary>
        /// <param name="family">
        /// The committed step's locked family (PH-02-R4). Threaded in rather than re-read: the
        /// selection state has already reset to the fastball for the next SET.
        /// </param>
        /// <param name="starAsked">
        /// The modifier as the accepted release read it (PH-16-R11). Paid for, the delivery is the Star Pitch; not, it is
        /// the ordinary pitch of <paramref name="family"/> at the same timing, the tell flashes, and the match still
        /// records the request (PH-16-R12).
        /// </param>
        PitchCommand PlayerPitch(double fill01, double secondsPastFull, string family, bool starAsked)
        {
            var nice = ChargeFeel.NiceCopy(true, fill01, secondsPastFull, _scene.Feel.ChargeMaxHoldSeconds);
            if (!string.IsNullOrEmpty(nice)) _host.BannerText = nice;
            StarAsks.PitchAsked = starAsked;
            if (starAsked) StarAsks.Note(_play.Match.PitchStarRequest);
            StarAsks.PitchShown = starAsked && _play.Match.CanStarPitch;
            return new PitchCommand(family,
                EffectiveCharge((float)fill01, (float)secondsPastFull),
                StarAsks.PitchShown,
                RubberX: _play.Match.PitcherOffsetX,
                Nice: ChargeFeel.NiceRelease(fill01, secondsPastFull, _scene.Feel.ChargeMaxHoldSeconds, _play.Match.Rules));
        }

        public void Launch(PitchCommand pitch)
        {
            _host.Steal.CommitCharge();
            pitch = _play.Match.PreparePitch(pitch);
            _play.Pitch = pitch;
            var mph = _play.Match.PitchSpeedMph(pitch);
            _play.PitchDur = (float)PitchFlight.AirSeconds(mph, _play.Match.Rules);
            _play.Flight = -(float)Motion.PitchRelease;
            _play.PitchAir = false;
            _play.Swung = false;
            HoldPitchInHand();
            PitchCharge = 0;
            PitchPast = 0;
            PitchButton = default;
            if (!_pads.HumanBats)
            {
                _play.Charge = 0;
                ChargePast = 0;
                _play.Plate = default;
                _play.PlateSeat = -1;
                // The CPU batter decides at the plate plane from the final trajectory (spec §3, S-04): see TickFlight.
                _play.Swing = null;
            }
            _play.Phase = MatchDirector.Phase.Flight;
            _play.T = 0;
            var rel = PitchFlight.Release(_play.Match.Rules, pitch.RubberX);
            _play.Ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            SetCam.AimAt((float)pitch.AimX, (float)pitch.AimY);
            // A hand's bend starts at nothing and grows for as long as the stick is held (§4.1).
            // A steered CPU delivery carries the whole of that hold in one number
            // (PitchFlight.BreakReach, §4.8), so the drawn ball walks there with the same BreakStep
            // rather than snapping to it at release. Without a plan this is the command, as before.
            _play.BreakX = CpuSteer != 0 ? 0f : (float)pitch.BreakX;
            // Body and ball agree from here: both stand on the rubber this delivery was built from.
            _play.MoundX = (float)pitch.RubberX;
            ShowCursor();
            ShowAimTell(_pads.HumanPitches ? pitch : null);
            _scene.Rig.Punch(pitch.Star ? 8f : 4f);
            _scene.Fx.ResetDecoy();
            if (pitch.Star)
            {
                _scene.Audio?.CaptainVo(_play.Match.Pitcher.Id);
                Controls.RumbleStar();
            }
            var hero = PitcherHero();
            if (hero != null)
                hero.SetPose(Motion.Verb.ThrowPitch, (float)pitch.Charge01, pitch.Type);
            // Cut, do not blend. SET→flight blending looks at dirt while the
            // ball stays in the hand (#301).
            _scene.Cam.Cut(AtBatShots.Pitch);
            SetCam.Aim();
        }

        public void TickFlight(float dt)
        {
            SetCam.Aim();
            var previousFlight = _play.Flight;
            _play.Flight += dt;
            TickBaserunning(dt);
            // Split a render frame at release so the setup and airborne speed clocks agree.
            var heldSeconds = Mathf.Min(dt, Mathf.Max(0, -previousFlight));
            if (_host.Steal.Advance(heldSeconds)) return;
            if (_pads.HumanPitches && !_play.PitchAir && _host.Steal.ReadSetupThrow(_pads.PitchPad, true)) return;
            if (_pads.HumanBats && !(_host.TutorialOn && _host.Coach.Tutorial.IsStealLesson))
            {
                var box = _pads.BatPad;
                // Every flight tick, committed or not (§5.8): a committed swing follows through and no trigger squares
                // (the sim's SwingCommitted), and East stays the plate's until it comes up.
                var plate = TickPlate(dt, accepting: true, commits: true);
                BuntSide = plate.Bunt.Showing;
                if (!_play.Swung)
                {
                    _play.Charge = (float)_play.Plate.Swing.Fill01;
                    ChargePast = (float)_play.Plate.Swing.SecondsPastFull;
                    StarAsks.SwingShown = StarAsks.Ready(box) && _play.Match.CanStarSwing;
                    // Stick U/D never resets the box once the windup starts (§5.4); in flight it aims only a
                    // Star Swing's launch, because an ordinary swing reads no stick at contact (PH-12).
                    _play.Match.WalkBatter(HomeSet.BoxWalkStep(box.StickX, dt));
                    ShowCursor();
                    // The held bunt is not a swing (§5.8): a committed release is always the ordinary swing; the
                    // square cancels a load before it can commit (PlateButtons), so the two never share a tick.
                    if (plate.Swing.Committed)
                        CommitSwing(SwingInputIntent.Capture(
                            plate.Swing, box.StickX, box.StickY, false, _play.Match.BatterOffsetX, _play.Match.Rules), StarAsks.Release(box));
                }
            }
            // The held trigger through the pitch keeps the square (§5.8); the CPU's square holds from SET.
            TickSquare(dt, SquaredNow);
            if (!_play.PitchAir)
            {
                HoldPitchInHand();
                // Authored release even if ThrowPitch never plays. Waiting on
                // the clip left the ball in the glove while the count ticked.
                var due = (float)Motion.PitchRelease;
                if (_play.Flight < 0)
                {
                    return;
                }
                PitcherHero()?.SampleMotion(due);
                CaptureReleaseFromHand();
                _scene.Park.Ball.Release();
                _play.PitchAir = true;
                _play.Match.PitchSetup.ReleaseBall();
                dt = Mathf.Min(dt, _play.Flight);
            }
            StealDirector.BufferCatcherInput(_play.Match, _pads.HumanOwnsThrow, _pads.FieldInput());
            if (_host.Steal.Advance(Mathf.Min(dt, Mathf.Max(0, _play.PitchDur - Mathf.Max(0, previousFlight))))) return;
            var u = Mathf.Clamp01(_play.Flight / _play.PitchDur);
            // Break is a stick direction after release (spec §4.1): screen-relative from either camera.
            if (_pads.HumanPitches)
            {
                _play.BreakX = (float)PitchFlight.BreakStep(_play.BreakX, SetCam.WorldX(_pads.PitchPad.StickX), dt,
                    _play.Match.Pitcher.Stats.Control, _play.Match.Rules);
                // The stick *is* the human's break, so the command the umpire reads carries it.
                _play.Pitch = _play.Pitch with { BreakX = _play.BreakX };
            }
            else if (CpuSteer != 0)
                // Drawn only (§4.8, PH-18-R1): the command already carries the whole reach the sim
                // judged, and the same per-frame step arrives there over this delivery's air time.
                _play.BreakX = (float)PitchFlight.BreakStep(_play.BreakX, CpuSteer, dt,
                    _play.Match.Pitcher.Stats.Control, _play.Match.Rules);
            var from = ((double)_play.ReleaseFrom.x, (double)_play.ReleaseFrom.y, (double)_play.ReleaseFrom.z);
            var shown = CpuSteer != 0 ? _play.Pitch with { BreakX = _play.BreakX } : _play.Pitch;
            var p = PitchFlight.Point(shown, u, _play.Match.Rules, _play.Match.Pitcher.StarPitch, from, _play.Match.Content.StarSkills);
            _play.Ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
            ShowAimTell(_pads.HumanPitches ? _play.Pitch : null);
            // The CPU batter commits at the decision instant from the trajectory as it stands (spec §3, §5.9):
            // the break it can see is the one drawn so far, never the steer still to come (PH-18).
            if (!_pads.HumanBats && _play.Swing == null && _play.Flight >= AtBatMotion.CpuDecisionTime(_play.PitchDur, _play.Match.Rules))
                _play.Swing = WithSquare(AtBatMotion.CommitCpuSwing(
                    (_host.TutorialOn ? new SwingCommand(false, 0, 0, false) : _play.Match.CpuBatter.Swing(_play.Pitch, _play.BreakX)),
                    _play.PitchDur, _play.Match.Rules));
            if (!_pads.HumanBats && _play.Swing != null && _play.Swing.Swing && !_play.Swung
                && _play.Flight >= AtBatMotion.SwingStart(_play.PitchDur, _play.Swing.TimingErrorFrames, _play.Match.Rules, _play.Swing.Bunt))
            {
                _play.Swung = true;
                _play.SwingContactSec = SwingContactSec(_play.Swing);
            }
            if (u < 1 || _host.TutorialOn && _host.Coach.Tutorial.IsStealLesson) return;
            // At the plate a human's squared bat is the held bunt (§5.8, PH-14-R4): no timed press, its held side.
            // Nothing squared and nothing committed is a take.
            _play.Swing ??= WithSquare(_pads.HumanBats
                ? PlateButtons.HeldBuntAtPlate(PlateStep, _play.Match.BatterOffsetX, _play.SquareSec)
                  ?? new SwingCommand(false, _play.Charge, 12, false)
                : (_host.TutorialOn ? new SwingCommand(false, 0, 0, false) : _play.Match.CpuBatter.Swing(_play.Pitch)));
            Resolve();
        }

        /// <summary>
        /// The square clock (§7.3): held, it counts up and the corners crash; released, it counts back down so the
        /// bodies walk back along the same line instead of snapping to their spots (BuntDefense.Spots is a function of it).
        /// </summary>
        void TickSquare(float dt, bool squared) => _play.SquareSec = squared ? _play.SquareSec + dt : Mathf.Max(0f, _play.SquareSec - dt);

        /// <summary>The swing carries how long the batter had been squared (§7.3): this client's clock, for either seat.</summary>
        SwingCommand WithSquare(SwingCommand swing) => swing.SquareSec == _play.SquareSec ? swing : swing with { SquareSec = _play.SquareSec };

        /// <param name="starAsked">The modifier as the accepted release read it: the same rule as the pitch's (PH-16-R11, R12).</param>
        void CommitSwing(SwingInputIntent intent, bool starAsked)
        {
            if (!intent.Committed || _play.Swung) return;
            _play.Swung = true;
            StarAsks.SwingAsked = starAsked;
            if (starAsked) StarAsks.Note(_play.Match.SwingStarRequest);
            StarAsks.SwingShown = starAsked && _play.Match.CanStarSwing;
            var effective = EffectiveCharge((float)intent.Fill01, (float)intent.SecondsPastFull);
            _play.Charge = effective;
            var nice = ChargeFeel.NiceCopy(false, intent.Fill01,
                intent.SecondsPastFull, _scene.Feel.ChargeMaxHoldSeconds);
            if (!string.IsNullOrEmpty(nice)) _host.BannerText = nice;
            _play.Swing = WithSquare(intent.Resolve(
                _play.Flight, _play.PitchDur, effective, StarAsks.SwingShown, _play.Match.Rules));
            _play.SwingContactSec = SwingContactSec(_play.Swing);
        }

        /// <summary>
        /// When the committed take's Contact mark lands after the press (D13, #612): on the ball's
        /// plate time inside the window, the take's own mark outside. Read at the press, before the
        /// pitch resolves and the next batter steps in.
        /// </summary>
        public float SwingContactSec(SwingCommand swing) =>
            (float)AtBatMotion.SwingContactSec(swing.TimingErrorFrames,
                _play.Match.SwingWindowFrames(_play.Pitch), _play.Match.Rules);

        void Resolve()
        {
            var live = ResolveTutorialOrAtBat(out var hit, out var finished);
            // The held bat met the ball (PH-14-R3, PH-14-R6): the side is fixed and every trigger down now is spent,
            // so a held LT is not the item modifier, and squares nothing next pitch, until it comes up and is pressed.
            if (_pads.HumanBats && _play.Swing != null && _play.Swing.Bunt && hit != null && hit.Quality != ContactQuality.Miss)
                _play.Plate = PlateButtons.Contact(_play.Plate, PlateInput);
            if (!live)
            {
                _play.Last = finished;
                NoteTrainingPitch();
                NoteTrainingSwing();
                _host.Banner();
                if (finished != null && _play.Match.StealThrowPending)
                {
                    _host.InPlay.StartRunnerPlay(finished);
                    return;
                }
                _host.BeginResult();
                return;
            }
            NoteTrainingPitch();
            if (_pads.HumanBats) _host.Coach?.OnSwing(_play.Swing, hit);
            _play.Pending = hit;
            _play.Preview = _play.Match.PreviewHit(hit, _play.Swing);
            _live.CpuField = null;
            var playerStarts = FieldAssist.PlayerStartsOnGlove(_pads.PlayerMustField);
            _host.Toss.Reset(_play.Preview != null ? _play.Preview.Fielder : null);
            if (!playerStarts)
            {
                _live.CpuField = _play.Match.ResolveFielding(hit, _play.Preview);
                if (!_pads.HumanBats)
                    _live.CpuField = _play.Match.ApplyOffenseItem(hit, _live.CpuField, null);
            }
            _scene.Park.Ball.Release();
            StartFly(hit, alreadyLive: _host.TutorialOn && (_host.Coach.Tutorial.IsItemLesson || _host.Coach.Tutorial.IsGameContactLesson) && _play.Match.LivePlay.Active);
        }

        public void StartFly(AtBatResult hit, bool alreadyLive = false)
        {
            _play.Phase = MatchDirector.Phase.InPlay;
            _host.InPlay.Began();
            _play.T = 0;
            _play.Path = null;
            // Every batted ball — foul territory included (§7.11) — is one live ball the sim plays out.
            var seat = _play.Match.LivePlay.Source;
            if (!alreadyLive) _play.Match.LivePlay.Apply(LivePlayCommand.BeginLive(
                _play.Pitch, _play.Swing, hit, _play.Preview, _live.CpuField, _pads.LiveNow(), _live.Dash01, seat));
            _host.InPlay.SyncFromLive();
            // The contact word comes from the typed zone, never from the release (#578).
            _host.BannerText = PlayStamp.ContactTell(hit.Quality);
            if (hit.HomeRun && _play.Match.Night)
                _scene.Park.BurstFireworks(_play.Ball);
            if (hit.Quality != ContactQuality.Miss) _scene.Audio?.Bat(hit.Quality);
            if (hit.StarSwingUsed != null)
            {
                _scene.Audio?.CaptainVo(_play.Match.Batter.Id);
                Controls.RumbleStar();
            }
            else if (hit.Quality != ContactQuality.Miss)
                Controls.RumbleContact(hit.Quality);
            if (CartoonJuice.DirtPuff(hit.Quality))
                _scene.Park.Ball.ContactPuff(_play.Ball);
            // The batter's own contact (CH-13): the quality's freeze × its body class, and its settle.
            // The smash beat (§15): a perfect, a star swing, or a home run — smashFreeze + smashHold, the smash cam on the body.
            if (hit.Quality == ContactQuality.Perfect || hit.StarSwingUsed != null || hit.HomeRun)
            {
                _host.Juice.Contact(_play.Match.Batter, _scene.Feel.SmashFreeze, _scene.Feel);
                _host.Smash = (float)_scene.Feel.SmashHold;
                _scene.Rig.Punch(CartoonJuice.Punch(hit.Quality));
                _scene.Audio?.Swell();
            }
            else if (hit.Quality == ContactQuality.Nice)
            {
                _host.Juice.Contact(_play.Match.Batter, _scene.Feel.SolidFreeze, _scene.Feel);
                _scene.Rig.Punch(CartoonJuice.Punch(hit.Quality));
            }
            else if (hit.Quality == ContactQuality.Sour)
            {
                _host.Juice.Contact(_play.Match.Batter, CartoonJuice.SourFreeze, _scene.Feel);
                _scene.Rig.Punch(CartoonJuice.Punch(hit.Quality));
            }
            _host.InPlay.AimLive();
        }

        public Vector3 SmashLook()
        {
            if (_play.Match?.Batter != null && _scene.Heroes.TryGetValue(_play.Match.Batter.Id, out var b) && b != null)
                return b.transform.position + Vector3.up * 3.2f;
            return _play.Ball.sqrMagnitude > 0.4f
                ? _play.Ball
                : new Vector3((float)HomeSet.BatterBodyX(_play.Match.Batter.Bats, _play.Match.BatterOffsetX), (float)HomeSet.BatterChestY, (float)HomeSet.BatterZ);
        }

        bool ResolveTutorialOrAtBat(out AtBatResult hit, out PlayEvent finished)
        {
            // The match settles the special each side asked for at its release (PH-16-R12), so it is handed the request.
            if (!_host.TutorialOn) return _play.Match.BeginAtBat(StarAsks.AsReleased(_play.Pitch), StarAsks.AsReleased(_play.Swing), out hit, out finished);
            var run = _host.Coach.Tutorial;
            if (_host.Coach.PlayerPitches) run.Pitch(StarAsks.AsReleased(_play.Pitch));
            else run.Swing(StarAsks.AsReleased(_play.Swing));
            hit = run.LastHit; finished = run.LastPlay;
            return run.IsGameContactLesson ? run.Match.LivePlay.Active
                : hit != null && hit.InPlay && finished == null;
        }

        /// <summary>
        /// The offense pad before the pitch (spec §9.2, §11.1): D-pad selects, stick toward the next
        /// bag or L3 starts the steal, stick back returns, RB returns, LB + RB halts. LB is not
        /// all-advance here: during SET and the flight it is the held special modifier (PH-16-R17), and
        /// all-advance (tag-and-go on a fly included) is a live-ball verb, read through the sim's Tick
        /// (RunInput) once the ball is in play.
        /// </summary>
        void TickBaserunning(float dt)
        {
            if (_play.Match == null || _play.Match.LeadBag == 0) return;
            if (!_pads.HumanBats || _play.Phase is not (MatchDirector.Phase.Set or MatchDirector.Phase.Flight)) return;
            // Tutorial UI owns input and evidence before advancing its pre-contact clock.
            if (_host.TutorialOn && _host.Coach.Tutorial.IsStealLesson) return;
            _play.Match.PitchSetup.RunnerInput(_pads.RunInput());
            if (_pads.TrainingOn) _host.Coach.OnRun(_play.Match);
        }

        void NoteTrainingPitch()
        {
            if (_host.Coach == null || _play.Pitch == null) return;
            _host.Coach.OnPitch(_play.Pitch, _play.Match);
        }

        void NoteTrainingSwing()
        {
            if (_host.Coach == null || _play.Swing == null || _play.Last == null) return;
            _host.Coach.OnSwing(_play.Swing, _play.Last.AtBat);
        }

        public void HoldPitchInHand()
        {
            var hero = PitcherHero();
            var hand = hero != null ? hero.ThrowHand : null;
            if (hand != null) _scene.Park.Ball.Hold(hand);
        }

        public void CaptureReleaseFromHand()
        {
            var hero = PitcherHero();
            var hand = hero != null ? hero.ThrowHand : null;
            if (hand != null)
                _play.ReleaseFrom = hand.position;
            else
            {
                var rel = PitchFlight.Release(_play.Rules(_scene.Content), _play.Pitch != null ? _play.Pitch.RubberX : 0);
                _play.ReleaseFrom = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            }
        }

        public HeroActor PitcherHero()
        {
            if (_play.Match?.Pitcher == null) return null;
            _scene.Heroes.TryGetValue(_play.Match.Pitcher.Id, out var hero);
            return hero;
        }
    }

    /// <summary>What <see cref="AtBatDirector"/> reads from the flow: the coach, the banner, the result beat and the directors it calls on.</summary>
    internal interface IAtBatHost
    {
        TrainingDirector Coach { get; }
        bool TutorialOn { get; }
        /// <summary>Bind the match's seats to their pads, once per match.</summary>
        void BindSeats();
        /// <summary>A practice's next match, on the next seed.</summary>
        Match NextTrainingMatch();
        string BannerText { set; }
        string Sub { set; }
        float Smash { set; }
        /// <summary>Name the finished play (or the lesson's caption) on the banner and the subtitle.</summary>
        void Banner();
        void BeginResult();
        InPlayDirector InPlay { get; }
        JuiceDirector Juice { get; }
        StealDirector Steal { get; }
        DefenseSwapWindow Swap { get; }
        ItemToss Toss { get; }
    }
}
