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
        internal void TickAtBat(float dt)
        {
            if (_phase == Phase.Set) TickSet(dt);
            else if (_phase == Phase.Flight) TickFlight(dt);
        }

        void BeginSet()
        {
            if (TrainingOn && (_match == null || _match.Over))
            {
                Seed++;
                _match = _coach.MakeMatch(_content, Seed);
            }
            _phase = Phase.Set;
            _t = 0;
            _charge = 0;
            _pitchCharge = 0;
            _chargePast = 0;
            _breakX = 0;
            _dash01 = 0;
            if (_match != null) _match.Dash01 = 0;
            _swung = false;
            _swing = null;
            _pitch = null;
            _last = null;
            _pending = null;
            _preview = null;
            _playerFielding = false;
            _cpuField = null;
            _throwing = false;
            _relayBags = null;
            _relayI = 0;
            _awaitingRelay = false;
            _coverPos = "";
            _recoilT = 0;
            _bobbling = false;
            _recoilArmed = false;
            _playerBobble = false;
            _diveT = _jumpT = _swapLock = 0;
            _gloveAt.Clear();
            _star = false;
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
            _zone.Show(true, 0, 0);
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
            var shot = AtBatShots.SetShot(HumanPitches, _phase == Phase.Flight, _charge, _aimX, _aimY, TrainingOn);
            // Snap. Blending SET→flight keeps looking at the dirt while the ball
            // leaves the hand, so looking strikes land with no baseball (#305).
            _cam.Cut(shot);
        }

        void TickSet(float dt)
        {
            if (_t > 0.2f && _t < 0.28f) LogSetCam("live");
            HoldPitchInHand();
            if (HumanPitches)
                _pitchCharge = _charge;
            else
                _pitchCharge = Mathf.Clamp01(_t / Mathf.Max(0.12f, (float)_feel.PitcherReadySeconds));
            _pip += dt * 1.35f;
            if (Controls.SwapPitcher) _match.SwapPitcher();
            if (Controls.NorthDown && (HumanPitches ? _match.CanStarPitch : _match.CanStarSwing)) _star = !_star;
            TickBaserunning(dt);
            TickCharge(dt, HumanPitches ? _feel.PitchChargeSeconds : _feel.SwingChargeSeconds);
            if (HumanPitches)
            {
                if (Controls.StickY < -0.7f) _match.ResetPitcher();
                else _match.WalkPitcher(Controls.StickX * dt * 1.6f);
                _aimX = (float)_match.PitcherOffsetX;
                _aimY = 0;
                _zone.Show(true, _aimX, _aimY);
                AimSetCamera();
                if (_t < (float)_feel.PitcherReadySeconds) return;
                if (Controls.ThrowBag > 0 && Controls.SouthDown)
                {
                    var po = _match.Pickoff(Controls.ThrowBag);
                    if (po != null) { _last = po; BeginResult(); }
                    return;
                }
                if (Controls.SouthDown) Launch(PlayerPitch());
                return;
            }
            if (HumanBats)
            {
                if (Controls.StickY < -0.7f) _match.ResetBatter();
                else _match.WalkBatter(Controls.StickX * dt * 1.6f);
            }
            AimSetCamera();
            if (_t > (float)_feel.PitcherReadySeconds) Launch(_match.CpuPitch());
        }

        void TickCharge(float dt, double seconds)
        {
            if (Controls.Charge)
            {
                var next = Mathf.Min(1, _charge + dt / (float)seconds);
                if (next >= 1 && _charge >= 1) _chargePast += dt;
                else if (next >= 1) _chargePast = 0;
                _charge = next;
            }
            else
            {
                _charge = Mathf.Max(0, _charge - dt * (float)_feel.ChargeDecay);
                _chargePast = 0;
            }
        }

        float EffectiveCharge() =>
            (float)ChargeFeel.Effective01(_charge, _chargePast, _feel.ChargeMaxHoldSeconds, _feel.ChargeOverchargeDecay);

        PitchCommand PlayerPitch()
        {
            var nice = ChargeFeel.NiceCopy(true, _charge, _chargePast, _feel.ChargeMaxHoldSeconds);
            if (!string.IsNullOrEmpty(nice)) _banner = nice;
            return new PitchCommand("fastball", EffectiveCharge(), 0, _star && _match.CanStarPitch,
                _match.PitcherOffsetX, 0, 0, Controls.Changeup, _match.PitcherOffsetX);
        }

        void Launch(PitchCommand pitch)
        {
            _pitch = pitch;
            var mph = AtBatResolver.PitchSpeedMph(pitch, _match.Pitcher);
            _pitchDur = (float)PitchFlight.AirSeconds(mph);
            _flight = 0;
            _pitchAir = false;
            _swung = false;
            HoldPitchInHand();
            if (!HumanBats)
            {
                _charge = 0;
                _chargePast = 0;
            }
            _phase = Phase.Flight;
            _t = 0;
            var rel = PitchFlight.Release(pitch.RubberX);
            _ball = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
            _aimX = (float)pitch.AimX;
            _aimY = (float)pitch.AimY;
            _breakX = 0;
            _zone.Show(true, _aimX, _aimY);
            _rig.Punch(pitch.Star ? 8f : 4f);
            _spec.ResetDecoy();
            _hideHelp = true;
            if (pitch.Star)
            {
                _audio?.CaptainVo(_match.Pitcher.Id);
                Controls.RumbleStar();
            }
            AimSetCamera();
        }

        void TickFlight(float dt)
        {
            AimSetCamera();
            if (!_pitchAir)
            {
                HoldPitchInHand();
                if (HumanBats)
                    TickCharge(dt, _feel.SwingChargeSeconds);
                if (!PitcherReleased() && _t < 0.55f)
                    return;
                CaptureReleaseFromHand();
                _park.Ball.Release();
                _pitchAir = true;
                _flight = 0;
            }
            _flight += dt;
            var u = Mathf.Clamp01(_flight / _pitchDur);
            if (HumanPitches)
                _breakX = Mathf.Clamp(_breakX + Controls.StickX * dt * 2.4f, -1f, 1f);
            var from = ((double)_relFrom.x, (double)_relFrom.y, (double)_relFrom.z);
            var p = PitchFlight.Point(_pitch.Type, u, _pitch.AimX, _pitch.AimY, _breakX, _pitch.Changeup, _pitch.RubberX, from);
            var x = (float)p.X;
            var y = (float)p.Y;
            var z = (float)p.Z;
            if (_pitch.Star)
            {
                var id = _match.Pitcher.StarPitch;
                if (id == "heatball") x += Mathf.Sin(u * 18f) * 0.4f;
                else if (id == "prismball") x += Mathf.Sin(u * 24f) * 1.8f;
                else if (id == "charmball") x += Mathf.Sin(u * 9f) * 0.7f;
                else if (id == "phonyball") x += u > 0.55f ? 2.4f : -0.5f;
                else if (id == "caskball") y += 0.55f * u;
            }
            _ball = new Vector3(x, y, z);
            TickBaserunning(dt);
            if (HumanBats)
            {
                if (Controls.NorthDown && _match.CanStarSwing) _star = !_star;
                TickCharge(dt, _feel.SwingChargeSeconds);
                if (Controls.SouthDown && !_swung)
                {
                    _swung = true;
                    var nice = ChargeFeel.NiceCopy(false, _charge, _chargePast, _feel.ChargeMaxHoldSeconds);
                    if (!string.IsNullOrEmpty(nice)) _banner = nice;
                    _swing = new SwingCommand(true, EffectiveCharge(), (_flight - _pitchDur) * 60f,
                        _star && _match.CanStarSwing, Controls.StickX * 18f, Controls.WestHeld, Controls.StickY,
                        _match.BatterOffsetX);
                }
            }
            if (u < 1) return;
            _swing ??= HumanBats
                ? new SwingCommand(false, _charge, 12, false)
                : _match.CpuSwing(_pitch, AtBatResolver.PitchInZone(_pitch, _match.Pitcher.Stats.Pitch), vsHumanPitcher: HumanPitches);
            Resolve();
        }

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
            (Character who, string pos) pick;
            if (_playerFielding && !FieldingResolver.BuddyJumpOffered(_preview))
                pick = FieldingResolver.NearestGlove(map, _preview.LandingX, _preview.LandingZ, _gloveAt);
            else
                pick = ( _preview.Fielder, _preview.Position );
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
            _hitT = 0;
            _phase = Phase.InPlay;
            _t = 0;
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
                _cam.SmashAt(SmashLook());
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
            return _ball.sqrMagnitude > 0.4f ? _ball : new Vector3(2.55f, 3.2f, 2.4f);
        }

        void AimDiamond(AtBatResult hit)
        {
            var id = InPlay.TheaterShot(hit);
            var look = _ball.sqrMagnitude > 1 ? _ball : new Vector3((float)(_preview?.LandingX ?? 0), 3f, (float)(_preview?.LandingZ ?? 80));
            if (id == "smash")
            {
                _cam.SmashAt(SmashLook());
                return;
            }
            if (FieldingResolver.IsGrounder(hit) || FieldingResolver.IsLine(hit))
                look.y = Mathf.Min(look.y, 3.2f);
            else
                look += new Vector3(0, 3, 10);
            _cam.PlayLook(id, look);
        }

    }
}
