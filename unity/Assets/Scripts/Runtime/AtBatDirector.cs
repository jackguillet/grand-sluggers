using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
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
        ChargeButtonState _swingButton;

        internal void TickAtBat(float dt)
        {
            if (_phase == Phase.Set) TickSet(dt);
            else if (_phase == Phase.Flight) TickFlight(dt);
        }

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
            _t = 0;
            _charge = 0;
            _pitchCharge = 0;
            _chargePast = 0;
            _pitchButton = default;
            _swingButton = default;
            _breakX = 0;
            _dash01 = 0;
            if (_match != null) _match.Dash01 = 0;
            _match?.LivePlay.Apply(LivePlayCommand.Reset());
            _swung = false;
            _bunt = false;
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
            _closePlayT = 0;
            _closeBag = 0;
            _closeOffAt = _closeDefAt = -1f;
            _relayBags = null;
            _relayI = 0;
            _awaitingRelay = false;
            _coverPos = "";
            _recoilT = 0;
            _bobbling = false;
            _recoilArmed = false;
            _playerBobble = false;
            _diveT = _jumpT = _swapLock = 0;
            _catchDive = _catchJump = false;
            _gloveAt.Clear();
            _starPitch = false;
            _starSwing = false;
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
            _gun = false;
            _gunRunner = null;
            _stealPitch = null;
            _stealT = 0;
            _stealTagT = -1f;
            _cpuGunAt = 0;
            _stealRelease = 0;
            var rel = PitchFlight.Release(_match.PitcherOffsetX);
            _ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            _park.Ball.Place(_ball, "", "fastball", false, false);
            _pitchAir = false;
            HoldPitchInHand();
            _aimX = _aimY = 0;
            _smash = 0;
            _gloved = false;
            _audio?.CrowdBed(true);
            AimSetCamera();
            LogSetCam("begin");
            _zone.Show(true, BatterCursorX, 0);
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
            if (HumanPitches)
                pitchButton = TickChargeButton(dt, _feel.PitchChargeSeconds, mound,
                    ref _pitchButton, ref _pitchCharge, ref _pitchPast,
                    _t >= (float)_feel.PitcherReadySeconds);
            else
                _pitchCharge = Mathf.Clamp01(_t / Mathf.Max(0.12f, (float)_feel.PitcherReadySeconds));
            if (HumanBats)
                TickChargeButton(dt, _feel.SwingChargeSeconds, box,
                    ref _swingButton, ref _charge, ref _chargePast);
            _pip += dt * 1.35f;
            if (mound.SwapPitcher) _match.SwapPitcher();
            if (HumanPitches && mound.NorthDown && _match.CanStarPitch) _starPitch = !_starPitch;
            if (HumanBats && box.NorthDown && _match.CanStarSwing) _starSwing = !_starSwing;
            TickBaserunning(dt);
            if (HumanPitches)
            {
                if (mound.StickY < -0.7f) _match.ResetPitcher();
                else _match.WalkPitcher(PitchWorldX(mound.StickX) * dt * 1.6f);
                _aimX = (float)_match.PitcherOffsetX;
                _aimY = 0;
                _zone.Show(true, BatterCursorX, 0);
                if (_t >= (float)_feel.PitcherReadySeconds)
                {
                    if (mound.ThrowBag > 0 && mound.SouthDown)
                    {
                        var po = _match.Pickoff(mound.ThrowBag);
                        if (po != null) { _last = po; Banner(); BeginResult(); }
                        return;
                    }
                    if (pitchButton.Committed)
                    {
                        Launch(PlayerPitch(pitchButton.CommitFill01, pitchButton.CommitSecondsPastFull));
                        return;
                    }
                }
            }
            if (HumanBats)
            {
                _bunt = box.WestHeld;
                if (box.StickY < -0.7f) _match.ResetBatter();
                else _match.WalkBatter(box.StickX * dt * 1.6f);
            }
            AimSetCamera();
            if (!HumanPitches && _t > (float)_feel.PitcherReadySeconds)
                Launch(_match.CpuPitch());
        }

        static ChargeButtonStep TickChargeButton(float dt, double seconds, Controls.Pad pad,
            ref ChargeButtonState state, ref float charge, ref float past, bool accepting = true)
        {
            var step = ChargeButton.Advance(state, pad.SouthDown, pad.SouthHeld, pad.SouthUp, dt, seconds,
                accepting);
            state = step.Next;
            charge = (float)state.Fill01;
            past = (float)state.SecondsPastFull;
            return step;
        }

        float EffectiveCharge(float charge, float past) =>
            (float)ChargeFeel.Effective01(charge, past, _feel.ChargeMaxHoldSeconds, _feel.ChargeOverchargeDecay);

        PitchCommand PlayerPitch(double fill01, double secondsPastFull)
        {
            var nice = ChargeFeel.NiceCopy(true, fill01, secondsPastFull, _feel.ChargeMaxHoldSeconds);
            if (!string.IsNullOrEmpty(nice)) _banner = nice;
            return new PitchCommand("fastball", EffectiveCharge((float)fill01, (float)secondsPastFull), 0,
                _starPitch && _match.CanStarPitch,
                _match.PitcherOffsetX, 0, 0, PitchPad.Changeup, _match.PitcherOffsetX);
        }

        void Launch(PitchCommand pitch)
        {
            pitch = _match.PreparePitch(pitch);
            _pitch = pitch;
            var mph = AtBatResolver.PitchSpeedMph(pitch, _match.Pitcher);
            _pitchDur = (float)PitchFlight.AirSeconds(mph);
            _flight = -(float)MoveBones.PitchRelease;
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
                _swingButton = default;
                _swing = _match.CpuSwing(pitch,
                    AtBatResolver.PitchInZone(pitch, _match.Pitcher.Stats.Pitch, _match.Pitcher.StarPitch), vsHumanPitcher: HumanPitches);
            }
            _phase = Phase.Flight;
            _t = 0;
            var rel = PitchFlight.Release(pitch.RubberX);
            _ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            _aimX = (float)pitch.AimX;
            _aimY = (float)pitch.AimY;
            _breakX = (float)pitch.BreakX;
            _zone.Show(true, BatterCursorX, 0);
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
                hero.SetPose(HeroActor.Pose.ThrowPitch, 0f, pitch.Type);
            // Cut, do not blend. SET→flight blending looks at dirt while the
            // ball stays in the hand (#301).
            _cam.Cut(AtBatShots.Pitch);
            AimSetCamera();
        }

        void TickFlight(float dt)
        {
            AimSetCamera();
            _flight += dt;
            var swingButton = default(ChargeButtonStep);
            if (HumanBats && !_swung)
            {
                var box = BatPad;
                swingButton = TickChargeButton(dt, _feel.SwingChargeSeconds, BatPad,
                    ref _swingButton, ref _charge, ref _chargePast);
                if (box.NorthDown && _match.CanStarSwing) _starSwing = !_starSwing;
                if (box.WestHeld) _bunt = true;
                if (box.StickY < -0.7f) _match.ResetBatter();
                else _match.WalkBatter(box.StickX * dt * 1.6f);
                _zone.Show(true, BatterCursorX, 0);
                if (swingButton.Committed)
                {
                    _swung = true;
                    var effective = EffectiveCharge((float)swingButton.CommitFill01,
                        (float)swingButton.CommitSecondsPastFull);
                    _charge = effective;
                    var nice = ChargeFeel.NiceCopy(false, swingButton.CommitFill01,
                        swingButton.CommitSecondsPastFull, _feel.ChargeMaxHoldSeconds);
                    if (!string.IsNullOrEmpty(nice)) _banner = nice;
                    _swing = new SwingCommand(true,
                        effective,
                        AtBatMotion.SwingErrorFrames(_flight, _pitchDur, _bunt || box.WestHeld),
                        _starSwing && _match.CanStarSwing, AtBatResolver.SprayAimDeg(box.StickX), _bunt || box.WestHeld, box.StickY,
                        _match.BatterOffsetX);
                }
            }
            if (!_pitchAir)
            {
                HoldPitchInHand();
                // Authored release even if ThrowPitch never plays. Waiting on
                // the clip left the ball in the glove while the count ticked.
                var due = (float)MoveBones.PitchRelease;
                if (_flight < 0)
                    return;
                PitcherHero()?.SampleMotion(due);
                CaptureReleaseFromHand();
                _park.Ball.Release();
                _pitchAir = true;
                dt = Mathf.Min(dt, _flight);
            }
            var u = Mathf.Clamp01(_flight / _pitchDur);
            if (HumanPitches)
                _breakX = Mathf.Clamp(_breakX + PitchWorldX(PitchPad.StickX) * dt * 2.4f, -1f, 1f);
            var from = ((double)_relFrom.x, (double)_relFrom.y, (double)_relFrom.z);
            _pitch = _pitch with { BreakX = _breakX };
            var p = PitchFlight.Point(_pitch, u, _match.Pitcher.StarPitch, from);
            _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
            TickBaserunning(dt);
            if (!HumanBats && _swing != null && _swing.Swing && !_swung
                && _flight >= AtBatMotion.SwingStart(_pitchDur, _swing.TimingErrorFrames, _swing.Bunt))
                _swung = true;
            if (u < 1) return;
            _swing ??= HumanBats
                ? new SwingCommand(false, _charge, 12, false)
                : _match.CpuSwing(_pitch, AtBatResolver.PitchInZone(_pitch, _match.Pitcher.Stats.Pitch, _match.Pitcher.StarPitch), vsHumanPitcher: HumanPitches);
            Resolve();
        }

        float PitchWorldX(float screenX)
        {
            var shotId = AtBatShots.SetShot(HumanPitches, _phase == Phase.Flight,
                HumanPitches ? _pitchCharge : _charge, _aimX, _aimY, TrainingOn, LiveSeats.Count);
            return (float)AtBatControl.WorldHorizontal(screenX, _content.Shots.Must(shotId));
        }

        float BatterCursorX => _match != null ? (float)_match.BatterOffsetX : 0f;

        void Resolve()
        {
            var stealBag = _match.ArmedStealBag > 0 ? _match.ArmedStealBag : _match.SelectedBag;
            var stealState = _match.RunnerAt(stealBag);
            var stealRunner = stealState?.Who;
            var stealLead = stealState?.Lead01 ?? 0;
            if (!_match.BeginAtBat(_pitch, _swing, out var hit, out var finished))
            {
                _last = finished;
                NoteTrainingPitch();
                NoteTrainingSwing();
                Banner();
                if (finished != null && _match.StealThrowPending)
                {
                    StartStealThrow(finished);
                    return;
                }
                if (finished != null && stealRunner != null &&
                    (finished.Kind == PlayKind.StolenBase || finished.Kind == PlayKind.CaughtStealing))
                {
                    StartStealGun(stealRunner, stealBag, stealLead, finished);
                    return;
                }
                if (finished != null && finished.Kind == PlayKind.Foul && hit.ExitVeloMph > 1)
                    StartFly(hit);
                else
                    BeginResult();
                return;
            }
            NoteTrainingPitch();
            if (HumanBats) _coach?.OnSwing(_swing, hit);
            _pending = hit;
            _preview = _match.PreviewHit(hit);
            _cpuField = null;
            _playerFielding = FieldAssist.PlayerStartsOnGlove(PlayerMustField);
            _itemThrown = false;
            _itemFlying = false;
            _itemFly = 0;
            _itemId = "";
            _itemPick = 0;
            _itemTarget = _preview != null ? _preview.Fielder : null;
            if (!_playerFielding)
            {
                _cpuField = _match.ResolveFielding(hit, _preview);
                if (!HumanBats)
                    _cpuField = _match.ApplyOffenseItem(hit, _cpuField, null);
            }
            InitGloves();
            _caught = _buddy = false;
            _throwBag = 0;
            _throwing = false;
            _relayBags = null;
            _relayI = 0;
            _awaitingRelay = false;
            _coverPos = "";
            _recoilT = 0;
            _bobbling = false;
            _recoilArmed = false;
            _playerBobble = false;
            _armedThrow = null;
            _armedCut = null;
            _park.Ball.Release();
            _diveT = _jumpT = 0;
            _buddyWindow = false;
            _buddyPos = "";
            StartFly(hit);
        }

        void InitGloves()
        {
            _gloveAt.Clear();
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            foreach (var kv in map)
                _gloveAt[kv.Key] = Diamond.Positions[kv.Key];
            _swapLock = 0;
            if (_preview == null)
            {
                _glovePos = "P";
                _fx = Diamond.Rubber.X;
                _fz = Diamond.Rubber.Z;
                return;
            }
            // Preview owns the trajectory-planned first glove for CPU and dead-stick
            // defense alike. Player swap remains available once the ball is live.
            var pick = (who: _preview.Fielder, pos: _preview.Position);
            _glovePos = pick.pos;
            var at = _gloveAt[_glovePos];
            _fx = at.X;
            _fz = at.Z;
        }

        void StartFly(AtBatResult hit)
        {
            var list = BallFlight.Trajectory(hit.ExitVeloMph, hit.LaunchDeg, _match.Park.WindMph);
            _path = new Sample[list.Count];
            for (var i = 0; i < list.Count; i++) _path[i] = list[i];
            _phase = Phase.InPlay;
            _t = 0;
            _match.LivePlay.Apply(LivePlayCommand.Begin(LiveKind(), LiveCommandSource));
            if (hit.HomeRun && _match.Night)
                _park.BurstFireworks(_ball);
            _gloved = false;
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
            if (hit.Quality == ContactQuality.Perfect || hit.StarSwingUsed != null)
            {
                _freeze = (float)_feel.SmashFreeze;
                _smash = (float)_feel.SmashHold;
                _rig.Punch(CartoonJuice.Punch(hit.Quality));
                _audio?.Swell();
            }
            else if (hit.Quality == ContactQuality.Solid)
            {
                _freeze = (float)_feel.SolidFreeze;
                _rig.Punch(CartoonJuice.Punch(hit.Quality));
            }
            else if (hit.Quality == ContactQuality.Cheap)
            {
                _freeze = (float)CartoonJuice.CheapFreeze;
                _rig.Punch(CartoonJuice.Punch(hit.Quality));
            }
            AimDiamond(hit);
        }

        Vector3 SmashLook()
        {
            if (_match?.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var b) && b != null)
                return b.transform.position + Vector3.up * 3.2f;
            return _ball.sqrMagnitude > 0.4f
                ? _ball
                : new Vector3((float)HomeSet.BatterX, (float)HomeSet.BatterChestY, (float)HomeSet.BatterZ);
        }

        void AimDiamond(AtBatResult hit)
        {
            _ = hit;
            _cam.HoldInPlay(_ball, fly: !FieldingResolver.IsGrounder(hit) && !FieldingResolver.IsLine(hit));
        }

    }
}
