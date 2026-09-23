using System;
using System.Collections.Generic;
using System.Linq;
using GrandSluggers.Sim;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Set, pitch, swing, contact. Out/safe stay in Sim.</summary>
    public sealed class AtBatDirector
    {
        readonly MatchDirector _play;
        public AtBatDirector(MatchDirector play) { _play = play; }
        public void Tick(float dt) { _play.TickAtBat(dt); }
    }

    public sealed partial class MatchDirector
    {
        ChargeButtonState _pitchButton;

        /// <summary>
        /// The batting seat's plate buttons (spec §5.1, §5.8): the swing button, the two bunt triggers and the East / G
        /// cancel, stepped by the sim (<see cref="PlateButtons.Advance"/>) every frame in the spec's order. It also holds
        /// the leak guards (PH-13-R1, PH-14-R6): a trigger held for a bunt at contact and a cancel press the plate took
        /// are spent until they come up. It belongs to one pad (<see cref="_plateSeat"/>); a new batting pad starts at rest.
        /// </summary>
        PlateButtonsState _plate;
        /// <summary>The pad index whose buttons <see cref="_plate"/> holds; -1 for none (a CPU batter).</summary>
        int _plateSeat = -1;
        /// <summary>This frame's plate input and step, read at the plate plane and at contact.</summary>
        PlateInput _plateInput;
        PlateButtonsStep _plateStep;

        /// <summary>
        /// The mound's pre-charge family selection (spec §3, PH-02-R3/R4/R5). The state is the
        /// client's; the step that moves it is the sim's (<see cref="Match.SelectPitch"/>).
        /// </summary>
        PitchSelectionState _pitchSelect;

        /// <summary>
        /// The CPU's delivery, decided at the top of SET (§4.8, PH-18-R1) so the body has SET to
        /// walk to the rubber it solved for. Null in a tutorial, whose pitch is scripted.
        /// </summary>
        PitchCommand _cpuPitch;

        /// <summary>The direction that CPU delivery holds the stick, drawn a frame at a time in flight.</summary>
        int _cpuSteer;

        /// <summary>
        /// The rubber the <b>body</b> stands on this frame, in rubber units. A hand's is the match's,
        /// exactly — the stick already walks it. The CPU's walks toward the match's at the same rate
        /// a hand walks, so its location verb reads as a walk rather than a teleport on the release
        /// frame. Drawn only: the delivery is built from <c>Match.PitcherOffsetX</c>.
        /// </summary>
        float _moundX;

        /// <summary>
        /// Each seat's held special modifier (spec §12, PH-16-R10, R11, R17), by pad index: LB on the pad, Q on player
        /// 1's keys. The state is the leak guard; the step that moves it is the sim's (<see cref="StarModifier"/>).
        /// </summary>
        readonly StarModifierState[] _starMods = new StarModifierState[2];

        /// <summary>
        /// The special each side asked for at its accepted release, as the modifier read it. <c>_pitch</c> / <c>_swing</c>
        /// carry what the pool paid for (the flight, the rumble, the card); the match is handed the request itself
        /// (<see cref="PitchAsReleased"/>) so it settles it and records the <see cref="StarRequest"/> (PH-16-R12).
        /// </summary>
        bool _pitchStarAsked, _swingStarAsked;

        /// <summary>The "special unavailable" tell (PH-16-R12) and when it began, on the unscaled clock.</summary>
        BroadcastHud.StarUnavailableTell? _starNo;
        float _starNoAt = -99f;

        /// <summary>One tick of both seats' modifiers, before any reader: a modifier that came up is free again.</summary>
        internal void TickStarModifiers()
        {
            _starMods[0] = StarModifier.Tick(_starMods[0], Controls.Pad1.StarHeld);
            _starMods[1] = StarModifier.Tick(_starMods[1], Controls.Pad2.StarHeld);
        }

        /// <summary>Whether <paramref name="pad"/>'s LB may mean all-advance, the cutoff or half the halt on this tick.</summary>
        bool StarFree(Controls.Pad pad) =>
            pad.Index < 0 || pad.Index >= _starMods.Length || StarModifier.IsFree(_starMods[pad.Index]);

        /// <summary>The modifier is down and would ask for the special at a release on this tick.</summary>
        bool StarReady(Controls.Pad pad) => pad.Index >= 0 && pad.StarHeld && StarFree(pad);

        /// <summary>An accepted release on <paramref name="pad"/>: the special it asks for, and the hold spent if it did.</summary>
        bool ReleaseStar(Controls.Pad pad)
        {
            if (pad.Index < 0 || pad.Index >= _starMods.Length) return false;
            var release = StarModifier.Release(_starMods[pad.Index], pad.StarHeld);
            _starMods[pad.Index] = release.Next;
            return release.Request;
        }

        /// <summary>
        /// A released request, as the match will record it (<see cref="Match.PitchStarRequest"/>, the record
        /// <see cref="PlayOutcome.Stars"/> carries): not afforded, the scorebug's Stars flash and name it on this tick.
        /// </summary>
        void NoteStarRequest(StarRequest request)
        {
            var tell = BroadcastHud.StarUnavailable(request);
            if (tell == null) return;
            _starNo = tell;
            _starNoAt = Time.unscaledTime;
        }

        /// <summary>The pitch the match is handed: the delivery with the special the pitcher asked for, paid or not.</summary>
        PitchCommand PitchAsReleased => _pitch != null && _pitchStarAsked && !_pitch.Star ? _pitch with { Star = true } : _pitch;

        /// <summary>The swing the match is handed: the same rule as <see cref="PitchAsReleased"/>.</summary>
        SwingCommand SwingAsReleased => _swing != null && _swingStarAsked && !_swing.Star ? _swing with { Star = true } : _swing;

        internal void TickAtBat(float dt)
        {
            if (_phase == Phase.Set) TickSet(dt);
            else if (_phase == Phase.Flight) TickFlight(dt);
            // Off the plate the triggers and East square nothing and cancel nothing, but a spent hold still has to be
            // seen coming up (PH-14-R6, PH-13-R1) before the live ball's readers (after this tick) take it again.
            else if (HumanBats) TickPlate(dt, accepting: false, commits: false);
        }

        /// <summary>
        /// One plate tick for the batting seat (spec §5.8's one-tick order, in the sim): the bunt, then the swing
        /// button with the square and East / G as its cancel. A tutorial plate lesson is handed the same input so its
        /// verdict rests on the player's own presses.
        /// </summary>
        PlateButtonsStep TickPlate(float dt, bool accepting, bool commits)
        {
            var pad = BatPad;
            if (pad.Index != _plateSeat)
            {
                _plate = default;
                _plateSeat = pad.Index;
            }
            _plateInput = pad.Plate;
            _plateStep = PlateButtons.Advance(_plate, _plateInput, dt, _feel.SwingChargeSeconds, accepting, commits);
            _plate = _plateStep.Next;
            if (accepting && TutorialOn && _coach.Tutorial.Phase == TutorialPhase.Attempt)
                _coach.Tutorial.Plate(new TutorialPlateTick(_plateInput, dt, commits));
            return _plateStep;
        }

        /// <summary>Whether <paramref name="pad"/>'s <paramref name="trigger"/> may mean any verb on this tick (PH-14-R6).</summary>
        bool TriggerFree(Controls.Pad pad, BuntSide trigger) =>
            pad.Index < 0 || pad.Index != _plateSeat || BuntHold.IsFree(_plate.Bunt, trigger);

        /// <summary>Whether <paramref name="pad"/>'s East / G may mean a dive, a dash or a skip on this tick (PH-13-R1).</summary>
        bool CancelFree(Controls.Pad pad) =>
            pad.Index < 0 || pad.Index != _plateSeat || PlateButtons.CancelIsFree(_plate);

        void BeginSet()
        {
            BindMatchSeats();
            if (TrainingOn && (_match == null || _match.Over))
            {
                Seed++;
                _match = _coach.MakeMatch(_content, Seed);
            }
            _phase = Phase.Set;
            Controls.CatchPlay();
            Controls.ClearTargets();
            _match?.ControllerRunners.Reset();
            _t = 0;
            _charge = 0;
            _pitchCharge = 0;
            _chargePast = 0;
            _pitchButton = default;
            _previousSetupBag = 0;
            // The next pitch (§5.8): the must-release, the spent triggers and a spent cancel carry; a hold must come up.
            _plate = _plate.NextPitch();
            // Fastball, unlocked, at every SET entry (PH-02-R5): nothing on the shared screen marks
            // the active family, so the player counts presses from a known start every time.
            _pitchSelect = PitchSelectionState.Reset;
            _cpuPitch = null;
            _cpuSteer = 0;
            _swapPick = null;
            _breakX = 0;
            _dash01 = 0;
            if (_match != null) _match.Dash01 = 0;
            _match?.LivePlay.Apply(LivePlayCommand.Reset());
            _swung = false;
            _buntSide = BuntSide.None;
            _squareSec = 0f;
            _swing = null;
            _pitch = null;
            _last = null;
            _pending = null;
            _preview = null;
            _playerFielding = false;
            _cpuField = null;
            _throwing = false;
            _closePlay = false;
            _closeIcon = false;
            _closeBag = 0;
            _coverPos = "";
            _recoilT = 0;
            _bobbling = false;
            _owed = FielderTells.Owed.None;
            _diveT = _jumpT = _swapLock = 0;
            _gloveAt.Clear();
            _resultBodies = null;
            _starPitch = false;
            _starSwing = false;
            _pitchStarAsked = _swingStarAsked = false;
            _caught = false;
            _buddy = false;
            _bagStamp = "";
            _bagStampT = 0;
            _pitchPast = 0;
            _itemThrown = false;
            _itemFlying = false;
            _itemFly = 0;
            _itemId = "";
            _itemTarget = null;
            _itemPick = 0;
            _items?.Hide();
            _banner = _sub = "";
            // The body starts this SET where the last pitch left it; the rubber persists (§4.2).
            _moundX = (float)_match.PitcherOffsetX;
            var rel = PitchFlight.Release(_match.PitcherOffsetX);
            _ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            _park.Ball.Place(_ball, "", PitchFamily.Fastball, false, false);
            _pitchAir = false;
            HoldPitchInHand();
            _aimX = _aimY = 0;
            _smash = 0;
            _audio?.CrowdBed(true);
            AimSetCamera();
            LogSetCam("begin");
            ShowCursor();
            if (TrainingOn && _coach != null && _coach.Session != null && _match != null
                && _coach.Session.Lesson == PracticeLesson.Fielding && _coach.Session.LessonPart >= 2)
                _coach.Session.SetupTurnTwo(_match);
        }

        void LogSetCam(string tag)
        {
            var live = Camera.main;
            var rio = PitcherHero();
            var rp = rio != null ? rio.transform.position.ToString("F1") : "null";
            var vp = rio != null && live != null
                ? live.WorldToViewportPoint(rio.transform.position + Vector3.up * 2.2f).ToString("F2")
                : "-";
            Debug.Log("GS SET " + tag
                + " shot=" + (_cam != null ? _cam.Shot : "?")
                + " pos=" + (live != null ? live.transform.position.ToString("F1") : "null")
                + " fwd=" + (live != null ? live.transform.forward.ToString("F2") : "-")
                + " fov=" + (live != null ? live.fieldOfView.ToString("F1") : "-")
                + " fl=" + (live != null ? live.focalLength.ToString("F1") : "-")
                + " phys=" + (live != null && live.usePhysicalProperties)
                + " px=" + (live != null ? live.pixelWidth + "x" + live.pixelHeight : "-")
                + " cams=" + Camera.allCamerasCount
                + " rio=" + rp + " vp=" + vp);
        }

        void AimSetCamera()
        {
            var shot = AtBatShots.SetShot(HumanPitches, _phase == Phase.Flight,
                HumanPitches ? _pitchCharge : _charge, _aimX, _aimY, TrainingOn, LiveSeats.Count);
            // Snap. Blending SET→flight keeps looking at the dirt while the ball
            // leaves the hand, so looking strikes land with no baseball (#305).
            _cam.Cut(shot);
        }

        void TickSet(float dt)
        {
            if (_t > 0.2f && _t < 0.28f) LogSetCam("live");
            HoldPitchInHand();
            var mound = PitchPad;
            var box = BatPad;
            var pitchButton = default(ChargeButtonStep);
            var pitchFamily = PitchFamily.Fastball;
            var wasPicking = _swapPick != null;
            if (HumanPitches && !_match.PitchSetup.Committed) TickSwapPick(dt, mound);
            if (wasPicking || _swapPick != null)
            {
                // The window owns this frame, including its open/close edge. No pickoff,
                // rubber walk, steal or banked charge can leak through a menu action.
                TickChargeButton(dt, _feel.PitchChargeSeconds, mound,
                    ref _pitchButton, ref _pitchCharge, ref _pitchPast, accepting: false);
                _pitchSelect = _pitchSelect with { Locked = false };
                if (HumanBats) TickPlate(dt, accepting: false, commits: false);
                _charge = _chargePast = 0;
                _buntSide = BuntSide.None;
                return;
            }
            // A legal base throw is read before South can begin a pitch charge.
            if (HumanPitches && ReadSetupThrow(mound, _t >= (float)_feel.PitcherReadySeconds && _swapPick == null)) return;
            // The arm edge is read from the button as it stood *before* this tick's step (#813).
            var prevPitchButton = _pitchButton;
            if (HumanPitches)
                pitchButton = TickChargeButton(dt, _feel.PitchChargeSeconds, mound,
                    ref _pitchButton, ref _pitchCharge, ref _pitchPast,
                    _t >= (float)_feel.PitcherReadySeconds && _swapPick == null);
            else
                _pitchCharge = Mathf.Clamp01(_t / Mathf.Max(0.12f, (float)_feel.PitcherReadySeconds));
            if (HumanPitches && (pitchButton.Next.Armed || pitchButton.Committed) && !_match.PitchSetup.Committed)
                CommitPitchSetup();
            if (HumanPitches)
            {
                // One SET tick of the cycle (spec §3, PH-02-R3/R4/R5). Cycling is legal before the
                // pitcher-ready beat — a selection is not a delivery — but not while the swap pick
                // owns the stick and the button (§4.7), so the gate is the seat, not `accepting`.
                var selection = _match.SelectPitch(_pitchSelect, mound.CyclePitch,
                    HumanPitches && _swapPick == null, prevPitchButton, pitchButton);
                _pitchSelect = selection.Next;
                // On the commit tick this is the locked family of the delivery leaving the hand;
                // `Next` has already reset to the fastball for the SET after it.
                pitchFamily = selection.Family;
            }
            if (HumanBats)
            {
                // A press during SET is not a swing (spec §3): the hold builds, the release drops. The triggers
                // square in SET too (§5.8), and East / G discards a load here as in the flight (§5.1).
                var plate = TickPlate(dt, accepting: true, commits: false);
                _charge = (float)_plate.Swing.Fill01;
                _chargePast = (float)_plate.Swing.SecondsPastFull;
                _buntSide = plate.Bunt.Showing;
            }
            else
            {
                _plateSeat = -1;
                _plate = default;
            }
            _pip += dt * 1.35f;
            // The CPU seats' SET verbs (spec §4.7, §11.6): a tired arm swaps; the runner AI's steal table runs once per at-bat.
            if (!HumanPitches && _t < dt) _match.CpuConsidersSwap();
            if (!HumanBats && _t < dt) _match.CpuArmSteal();
            // The CPU batter's square is read at SET (§5.9, §7.3) so a human pitcher sees it before the pitch.
            if (!HumanBats && _t < dt) _match.CpuSquaresBunt();
            // The CPU decides its delivery at the top of SET, the way a hand decides before it
            // charges (§4.8, PH-18-R1): the rubber the model solves for is then somewhere to walk to
            // during SET instead of a place to appear at on the release frame.
            if (!HumanPitches && _t < dt && _cpuPitch == null && !TutorialOn)
            {
                _cpuPitch = _match.CpuPitchByInputs(out var cpuPlan);
                _cpuSteer = Math.Sign(cpuPlan.SteerDir);
            }
            // The held special modifier (PH-16-R11): no arming. The card reads STAR while it is down, free and paid for;
            // the release reads it.
            _starPitch = HumanPitches && StarReady(mound) && _match.CanStarPitch;
            _starSwing = HumanBats && StarReady(box) && _match.CanStarSwing;
            TickBaserunning(dt);
            if (AdvanceSetup(dt)) return;
            if (HumanBats)
            {
                // Down resets the box in SET only (§5.4); in flight the same axis aims launch.
                if (box.StickY < -0.7f) _match.ResetBatter();
                else _match.WalkBatter(HomeSet.BoxWalkStep(box.StickX, dt));
            }
            // The square is a clock (§7.3): the defense crashes for as long as it has been held; released, it winds back.
            TickSquare(dt, SquaredNow);
            if (HumanPitches)
            {
                if (_swapPick != null || _match.PitchSetup.Committed) { }
                else if (mound.StickY < -0.7f) _match.ResetPitcher();
                else _match.WalkPitcher(PitchWorldX(mound.StickX) * dt * 1.6f);
                _moundX = (float)_match.PitcherOffsetX;
                _aimX = (float)_match.PitcherOffsetX;
                _aimY = 0;
                if (_t >= (float)_feel.PitcherReadySeconds)
                {
                    if (pitchButton.Committed)
                    {
                        Launch(PlayerPitch(pitchButton.CommitFill01, pitchButton.CommitSecondsPastFull, pitchFamily,
                            ReleaseStar(mound)));
                        return;
                    }
                }
            }
            if (!HumanPitches)
                // The CPU's body walks to the rubber its delivery solved for, at the rate a hand
                // walks (§4.8). Presentation only: the delivery already carries that rubber, and
                // with no plan (a tutorial) the body sits on the match's own value.
                _moundX = _cpuPitch != null
                    ? Mathf.MoveTowards(_moundX, (float)_match.PitcherOffsetX, dt * 1.6f)
                    : (float)_match.PitcherOffsetX;
            ShowCursor();
            ShowAimTell(HumanPitches ? PreviewPitch(pitchFamily) : null);
            AimSetCamera();
            if (!HumanPitches && _t > (float)_feel.PitcherReadySeconds)
            {
                // The CPU pitcher's pickoff read (§4.5, §4.8): a runner who armed in SET is between bags on the motion.
                var pickoffBag = TutorialOn ? 0 : _match.CpuPickoffBag();
                if (pickoffBag > 0)
                {
                    BeginPickoff(pickoffBag);
                    return;
                }
                Launch(TutorialOn && HumanBats ? _coach.Tutorial.CpuPitch : _cpuPitch ?? _match.CpuPitch());
            }
        }

        /// <summary>The pickoff (§4.5, D3): a runner on the bag is the beat; a runner who broke is the live runner play.</summary>
        void BeginPickoff(int bag)
        {
            if (TutorialOn && _coach.Tutorial.Pickoff(bag))
            {
                if (_match.LivePlay.Active) StartRunnerPlay(null);
                else if (_coach.Tutorial.LastPlay is { } tutorialPlay)
                { _last = tutorialPlay; Banner(); BeginResult(); }
                return;
            }
            if (_match.BeginPickoff(bag, LiveSeatsNow(), out var dead, _match.LivePlay.Source))
            {
                StartRunnerPlay(null);
                return;
            }
            if (dead != null) { _last = dead; Banner(); BeginResult(); }
        }

        /// <summary>
        /// The pitcher card's verb tells: STAR and the swap pick (spec §4.1, §4.7). No family tell —
        /// the card is shared by both seats and the selection is made before the charge, so naming
        /// the held pitch would hand it to the batter (PH-02-R5). The repertoire row the card shows
        /// instead is <see cref="BroadcastHud.PitcherPitches(Match)"/> and depends on nothing held.
        /// </summary>
        string PitcherExtra()
        {
            if (_match == null) return "";
            var set = _phase == Phase.Set && HumanPitches;
            if (set && _match.PitchSetup.Committed) return BroadcastHud.ShortFamily(_match.FamilyAt(_pitchSelect)) + " · " + BroadcastHud.PitchCommitted;
            return BroadcastHud.PitcherExtra(
                _starPitch && HumanPitches,
                set ? BroadcastHud.ShortFamily(_match.FamilyAt(_pitchSelect)) + " · West cycle" : null,
                set && _swapPick == null && _match.CanArrangeDefense);
        }

        /// <summary>
        /// The pitcher's shape for the pose and the ball. Family-blind until the delivery exists
        /// (PH-02-R5): in SET the pose is the fastball's whatever is selected, and the real family
        /// arrives with <c>_pitch</c> at the launch, for the throw itself.
        /// </summary>
        string ShownPitchType => _pitch != null ? _pitch.Type : PitchFamily.Fastball;

        /// <summary>
        /// Select opens the defense window. South picks two positions; Select is the pitcher shortcut.
        /// East cancels a pending pick or closes. All baseball input waits for the window.
        /// </summary>
        bool OpenDefenseSetup()
        {
            if (_phase != Phase.Set || !HumanPitches || _match.PitchSetup.Committed || !_match.CanArrangeDefense) return false;
            _swapPick = new DefenseSetupPick(_match);
            TeamSheet.BeginPitcherPick();
            _swapX.Catch(FieldPad.MenuAxisX); _swapY.Catch(FieldPad.MenuAxisY);
            _match.SetPaused(false);
            Controls.CatchPlay();
            return true;
        }

        void TickSwapPick(float dt, Controls.Pad mound)
        {
            if (_swapPick == null) return;
            var pointer = Controls.SeatUsesKeyboard(mound.Index)
                ? TeamSheet.PitcherPointer(_swapPick) : TeamSheet.PitcherAction.None;
            if (mound.EastDown || pointer == TeamSheet.PitcherAction.Cancel)
            {
                if (_swapPick.PickedPosition != null) _swapPick.CancelPick();
                else _swapPick = null;
                return;
            }
            if (pointer == TeamSheet.PitcherAction.Done) { _swapPick = null; return; }
            var dx = _swapX.Tick(mound.MenuAxisX, mound.MenuTapX, dt);
            var dy = _swapY.Tick(mound.MenuAxisY, mound.MenuTapY, dt);
            if (dx != 0 || dy != 0) _swapPick.Move(dx, dy);
            if (mound.WestDown || pointer == TeamSheet.PitcherAction.Confirm)
                _swapPick.QuickPitcher(_match, WindowPitcherSwap);
            else if (pointer == TeamSheet.PitcherAction.Pick || (mound.SouthDown && !Controls.PointerDown))
                _swapPick.PickOrSwap(_match, WindowPitcherSwap);
        }

        bool WindowPitcherSwap(Character who)
        {
            bool changed;
            if (TutorialOn && _coach.Tutorial.SwapPitcher(who.Id)) changed = _match.Pitcher.Id == who.Id;
            else
            {
                var candidate = _swapPick.Candidates.First(c => c.Who.Id == who.Id);
                changed = _match.SwapDefensePositions("P", candidate.Pos);
            }
            if (changed) _pitchSelect = PitchSelectionState.Reset;
            return changed;
        }

        /// <summary>The pitch as it stands in SET: the selected family, the rubber, the charge so far. Not committed.</summary>
        PitchCommand PreviewPitch(string family) =>
            new(family, EffectiveCharge(_pitchCharge, _pitchPast),
                _starPitch && _match.CanStarPitch, RubberX: _match.PitcherOffsetX);

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
            var teaching = TrainingOn || TutorialOn;
            if (pitch == null || _match == null
                || !SetTells.AimTellOn(HumanPitches, _phase == Phase.Set, _phase == Phase.Flight, teaching))
            {
                _zone.AimTell(false, 0, 0);
                return;
            }
            var (x, y) = teaching
                ? SetTells.Locator(pitch, _match.Pitcher.StarPitch, _match.Rules)
                : SetTells.RubberRing(_match.PitcherOffsetX);
            _zone.AimTell(true, (float)x, (float)y);
        }

        /// <summary>
        /// The gold oval follows the batter and shows this swing's barrel (contact, charge):
        /// the sim's own oval (<see cref="SweetSpot.Oval"/>), the one the resolver judges (S-134).
        /// </summary>
        void ShowCursor()
        {
            if (_match == null) return;
            _zone.Show(SweetSpot.Oval(_match.Batter, _match.OffenseBat, EffectiveCharge(_charge, _chargePast),
                _match.BatterOffsetX, _match.Rules));
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
            (float)ChargeFeel.Effective01(charge, past, _feel.ChargeMaxHoldSeconds, _feel.ChargeOverchargeDecay);

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
            var nice = ChargeFeel.NiceCopy(true, fill01, secondsPastFull, _feel.ChargeMaxHoldSeconds);
            if (!string.IsNullOrEmpty(nice)) _banner = nice;
            _pitchStarAsked = starAsked;
            if (starAsked) NoteStarRequest(_match.PitchStarRequest);
            _starPitch = starAsked && _match.CanStarPitch;
            return new PitchCommand(family,
                EffectiveCharge((float)fill01, (float)secondsPastFull),
                _starPitch,
                RubberX: _match.PitcherOffsetX,
                Nice: ChargeFeel.NiceRelease(fill01, secondsPastFull, _feel.ChargeMaxHoldSeconds, _match.Rules));
        }

        void Launch(PitchCommand pitch)
        {
            CommitPitchSetup();
            pitch = _match.PreparePitch(pitch);
            _pitch = pitch;
            var mph = _match.PitchSpeedMph(pitch);
            _pitchDur = (float)PitchFlight.AirSeconds(mph);
            _flight = -(float)Motion.PitchRelease;
            _pitchAir = false;
            _swung = false;
            HoldPitchInHand();
            _pitchCharge = 0;
            _pitchPast = 0;
            _pitchButton = default;
            if (!HumanBats)
            {
                _charge = 0;
                _chargePast = 0;
                _plate = default;
                _plateSeat = -1;
                // The CPU batter decides at the plate plane from the final trajectory (spec §3, S-04): see TickFlight.
                _swing = null;
            }
            _phase = Phase.Flight;
            _t = 0;
            var rel = PitchFlight.Release(pitch.RubberX);
            _ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            _aimX = (float)pitch.AimX;
            _aimY = (float)pitch.AimY;
            // A hand's bend starts at nothing and grows for as long as the stick is held (§4.1).
            // A steered CPU delivery carries the whole of that hold in one number
            // (PitchFlight.BreakReach, §4.8), so the drawn ball walks there with the same BreakStep
            // rather than snapping to it at release. Without a plan this is the command, as before.
            _breakX = _cpuSteer != 0 ? 0f : (float)pitch.BreakX;
            // Body and ball agree from here: both stand on the rubber this delivery was built from.
            _moundX = (float)pitch.RubberX;
            ShowCursor();
            ShowAimTell(HumanPitches ? pitch : null);
            _rig.Punch(pitch.Star ? 8f : 4f);
            _spec.ResetDecoy();
            _hideHelp = true;
            if (pitch.Star)
            {
                _audio?.CaptainVo(_match.Pitcher.Id);
                Controls.RumbleStar();
            }
            var hero = PitcherHero();
            if (hero != null)
                hero.SetPose(Motion.Verb.ThrowPitch, (float)pitch.Charge01, pitch.Type);
            // Cut, do not blend. SET→flight blending looks at dirt while the
            // ball stays in the hand (#301).
            _cam.Cut(AtBatShots.Pitch);
            AimSetCamera();
        }

        void TickFlight(float dt)
        {
            AimSetCamera();
            var previousFlight = _flight;
            _flight += dt;
            TickBaserunning(dt);
            // Split a render frame at release so the setup and airborne speed clocks agree.
            var heldSeconds = Mathf.Min(dt, Mathf.Max(0, -previousFlight));
            if (AdvanceSetup(heldSeconds)) return;
            if (HumanPitches && !_pitchAir && ReadSetupThrow(PitchPad, true)) return;
            if (HumanBats && !(TutorialOn && _coach.Tutorial.IsStealLesson))
            {
                var box = BatPad;
                // Every flight tick, committed or not (§5.8): a committed swing follows through and no trigger squares
                // (the sim's SwingCommitted), and East stays the plate's until it comes up.
                var plate = TickPlate(dt, accepting: true, commits: true);
                _buntSide = plate.Bunt.Showing;
                if (!_swung)
                {
                    _charge = (float)_plate.Swing.Fill01;
                    _chargePast = (float)_plate.Swing.SecondsPastFull;
                    _starSwing = StarReady(box) && _match.CanStarSwing;
                    // Stick U/D never resets the box once the windup starts (§5.4); in flight it aims only a
                    // Star Swing's launch, because an ordinary swing reads no stick at contact (PH-12).
                    _match.WalkBatter(HomeSet.BoxWalkStep(box.StickX, dt));
                    ShowCursor();
                    // The held bunt is not a swing (§5.8): a committed release is always the ordinary swing; the
                    // square cancels a load before it can commit (PlateButtons), so the two never share a tick.
                    if (plate.Swing.Committed)
                        CommitSwing(SwingInputIntent.Capture(
                            plate.Swing, box.StickX, box.StickY, bunt: false, _match.BatterOffsetX), ReleaseStar(box));
                }
            }
            // The held trigger through the pitch keeps the square (§5.8); the CPU's square holds from SET.
            TickSquare(dt, SquaredNow);
            if (!_pitchAir)
            {
                HoldPitchInHand();
                // Authored release even if ThrowPitch never plays. Waiting on
                // the clip left the ball in the glove while the count ticked.
                var due = (float)Motion.PitchRelease;
                if (_flight < 0)
                {
                    return;
                }
                PitcherHero()?.SampleMotion(due);
                CaptureReleaseFromHand();
                _park.Ball.Release();
                _pitchAir = true;
                _match.PitchSetup.ReleaseBall();
                dt = Mathf.Min(dt, _flight);
            }
            BufferCatcherInput();
            if (AdvanceSetup(Mathf.Min(dt, Mathf.Max(0, _pitchDur - Mathf.Max(0, previousFlight))))) return;
            var u = Mathf.Clamp01(_flight / _pitchDur);
            // Break is a stick direction after release (spec §4.1): screen-relative from either camera.
            if (HumanPitches)
            {
                _breakX = (float)PitchFlight.BreakStep(_breakX, PitchWorldX(PitchPad.StickX), dt,
                    _match.Pitcher.Stats.Control, _match.Rules);
                // The stick *is* the human's break, so the command the umpire reads carries it.
                _pitch = _pitch with { BreakX = _breakX };
            }
            else if (_cpuSteer != 0)
                // Drawn only (§4.8, PH-18-R1): the command already carries the whole reach the sim
                // judged, and the same per-frame step arrives there over this delivery's air time.
                _breakX = (float)PitchFlight.BreakStep(_breakX, _cpuSteer, dt,
                    _match.Pitcher.Stats.Control, _match.Rules);
            var from = ((double)_relFrom.x, (double)_relFrom.y, (double)_relFrom.z);
            var shown = _cpuSteer != 0 ? _pitch with { BreakX = _breakX } : _pitch;
            var p = PitchFlight.Point(shown, u, _match.Pitcher.StarPitch, from);
            _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
            ShowAimTell(HumanPitches ? _pitch : null);
            // The CPU batter commits at the decision instant from the trajectory as it stands (spec §3, §5.9):
            // the break it can see is the one drawn so far, never the steer still to come (PH-18).
            if (!HumanBats && _swing == null && _flight >= AtBatMotion.CpuDecisionTime(_pitchDur, _match.Rules))
                _swing = WithSquare(AtBatMotion.CommitCpuSwing(
                    (TutorialOn ? new SwingCommand(false, 0, 0, false) : _match.CpuSwing(_pitch, _breakX)),
                    _pitchDur, _match.Rules));
            if (!HumanBats && _swing != null && _swing.Swing && !_swung
                && _flight >= AtBatMotion.SwingStart(_pitchDur, _swing.TimingErrorFrames, _swing.Bunt, _match.Rules))
            {
                _swung = true;
                _swingContactSec = SwingContactSec(_swing);
            }
            if (u < 1 || TutorialOn && _coach.Tutorial.IsStealLesson) return;
            // At the plate a human's squared bat is the held bunt (§5.8, PH-14-R4): no timed press, its held side.
            // Nothing squared and nothing committed is a take.
            _swing ??= WithSquare(HumanBats
                ? PlateButtons.HeldBuntAtPlate(_plateStep, _match.BatterOffsetX, _squareSec)
                  ?? new SwingCommand(false, _charge, 12, false)
                : (TutorialOn ? new SwingCommand(false, 0, 0, false) : _match.CpuSwing(_pitch)));
            Resolve();
        }

        /// <summary>
        /// The square clock (§7.3): held, it counts up and the corners crash; released, it counts back down so the
        /// bodies walk back along the same line instead of snapping to their spots (BuntDefense.Spots is a function of it).
        /// </summary>
        void TickSquare(float dt, bool squared) => _squareSec = squared ? _squareSec + dt : Mathf.Max(0f, _squareSec - dt);

        /// <summary>The swing carries how long the batter had been squared (§7.3): this client's clock, for either seat.</summary>
        SwingCommand WithSquare(SwingCommand swing) => swing.SquareSec == _squareSec ? swing : swing with { SquareSec = _squareSec };

        /// <param name="starAsked">The modifier as the accepted release read it: the same rule as the pitch's (PH-16-R11, R12).</param>
        void CommitSwing(SwingInputIntent intent, bool starAsked)
        {
            if (!intent.Committed || _swung) return;
            _swung = true;
            _swingStarAsked = starAsked;
            if (starAsked) NoteStarRequest(_match.SwingStarRequest);
            _starSwing = starAsked && _match.CanStarSwing;
            var effective = EffectiveCharge((float)intent.Fill01, (float)intent.SecondsPastFull);
            _charge = effective;
            var nice = ChargeFeel.NiceCopy(false, intent.Fill01,
                intent.SecondsPastFull, _feel.ChargeMaxHoldSeconds);
            if (!string.IsNullOrEmpty(nice)) _banner = nice;
            _swing = WithSquare(intent.Resolve(
                _flight, _pitchDur, effective, _starSwing, _match.Rules));
            _swingContactSec = SwingContactSec(_swing);
        }

        /// <summary>
        /// When the committed take's Contact mark lands after the press (D13, #612): on the ball's
        /// plate time inside the window, the take's own mark outside. Read at the press, before the
        /// pitch resolves and the next batter steps in.
        /// </summary>
        float SwingContactSec(SwingCommand swing) =>
            (float)AtBatMotion.SwingContactSec(swing.TimingErrorFrames,
                _match.SwingWindowFrames(_pitch), _match.Rules);

        float PitchWorldX(float screenX)
        {
            var shotId = AtBatShots.SetShot(HumanPitches, _phase == Phase.Flight,
                HumanPitches ? _pitchCharge : _charge, _aimX, _aimY, TrainingOn, LiveSeats.Count);
            return (float)AtBatControl.WorldHorizontal(screenX, _content.Shots.Must(shotId));
        }

        void Resolve()
        {
            var live = ResolveTutorialOrAtBat(out var hit, out var finished);
            // The held bat met the ball (PH-14-R3, PH-14-R6): the side is fixed and every trigger down now is spent,
            // so a held LT is not the item modifier, and squares nothing next pitch, until it comes up and is pressed.
            if (HumanBats && _swing != null && _swing.Bunt && hit != null && hit.Quality != ContactQuality.Miss)
                _plate = PlateButtons.Contact(_plate, _plateInput);
            if (!live)
            {
                _last = finished;
                NoteTrainingPitch();
                NoteTrainingSwing();
                Banner();
                if (finished != null && _match.StealThrowPending)
                {
                    StartRunnerPlay(finished);
                    return;
                }
                BeginResult();
                return;
            }
            NoteTrainingPitch();
            if (HumanBats) _coach?.OnSwing(_swing, hit);
            _pending = hit;
            _preview = _match.PreviewHit(hit, _swing);
            _cpuField = null;
            var playerStarts = FieldAssist.PlayerStartsOnGlove(PlayerMustField);
            _itemThrown = false;
            _itemFlying = false;
            _itemFly = 0;
            _itemId = "";
            _itemPick = 0;
            _itemTarget = _preview != null ? _preview.Fielder : null;
            if (!playerStarts)
            {
                _cpuField = _match.ResolveFielding(hit, _preview);
                if (!HumanBats)
                    _cpuField = _match.ApplyOffenseItem(hit, _cpuField, null);
            }
            _park.Ball.Release();
            StartFly(hit, alreadyLive: TutorialOn && (_coach.Tutorial.IsItemLesson || _coach.Tutorial.IsGameContactLesson) && _match.LivePlay.Active);
        }

        void StartFly(AtBatResult hit, bool alreadyLive = false)
        {
            _phase = Phase.InPlay;
            _liveBeganFrame = Time.frameCount;
            _t = 0;
            _path = null;
            // Every batted ball — foul territory included (§7.11) — is one live ball the sim plays out.
            var seat = _match.LivePlay.Source;
            if (!alreadyLive) _match.LivePlay.Apply(LivePlayCommand.BeginLive(
                _pitch, _swing, hit, _preview, _cpuField, LiveSeatsNow(), _dash01, seat));
            SyncFromLive();
            // The contact word comes from the typed zone, never from the release (#578).
            _banner = PlayStamp.ContactTell(hit.Quality);
            if (hit.HomeRun && _match.Night)
                _park.BurstFireworks(_ball);
            if (hit.Quality != ContactQuality.Miss) _audio?.Bat(hit.Quality);
            if (hit.StarSwingUsed != null)
            {
                _audio?.CaptainVo(_match.Batter.Id);
                Controls.RumbleStar();
            }
            else if (hit.Quality != ContactQuality.Miss)
                Controls.RumbleContact(hit.Quality);
            if (CartoonJuice.DirtPuff(hit.Quality))
                _park.Ball.ContactPuff(_ball);
            // The smash beat (§15): a perfect, a star swing, or a home run — smashFreeze + smashHold, the smash cam on the body.
            if (hit.Quality == ContactQuality.Perfect || hit.StarSwingUsed != null || hit.HomeRun)
            {
                _freeze = (float)_feel.SmashFreeze;
                _smash = (float)_feel.SmashHold;
                _rig.Punch(CartoonJuice.Punch(hit.Quality));
                _audio?.Swell();
            }
            else if (hit.Quality == ContactQuality.Nice)
            {
                _freeze = (float)_feel.SolidFreeze;
                _rig.Punch(CartoonJuice.Punch(hit.Quality));
            }
            else if (hit.Quality == ContactQuality.Sour)
            {
                _freeze = (float)CartoonJuice.SourFreeze;
                _rig.Punch(CartoonJuice.Punch(hit.Quality));
            }
            AimLive();
        }

        Vector3 SmashLook()
        {
            if (_match?.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var b) && b != null)
                return b.transform.position + Vector3.up * 3.2f;
            return _ball.sqrMagnitude > 0.4f
                ? _ball
                : new Vector3((float)HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterOffsetX), (float)HomeSet.BatterChestY, (float)HomeSet.BatterZ);
        }

    }
}
