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
            _ball = new Vector3(0, 5.4f, 60.5f);
            _park.Ball.Place(_ball, "", "fastball", false);
            _aimX = _aimY = 0;
            _smash = 0;
            _gloved = false;
            _audio?.CrowdBed(true);
            AimSetCamera();
            _zone.Show(true, 0, 0);
        }

        void AimSetCamera()
        {
            if (HumanPitches) _cam.Play("mound");
            else _cam.Play("plate");
        }

        void TickSet(float dt)
        {
            AimSetCamera();
            _pip += dt * 1.35f;
            if (Controls.CyclePitch) _pitchIndex = (_pitchIndex + 1) % _pitches.Length;
            if (Controls.SwapPitcher) _match.SwapPitcher();
            if (Controls.NorthDown && (HumanPitches ? _match.CanStarPitch : _match.CanStarSwing)) _star = !_star;
            TickBaserunning(dt);
            if (HumanPitches)
            {
                _aimX = Mathf.Clamp(Controls.StickX, -1, 1);
                _aimY = Mathf.Clamp(Controls.StickY, -1, 1);
                _zone.Show(true, _aimX, _aimY);
                _charge = Controls.Charge
                    ? Mathf.Min(1, _charge + dt / (float)_feel.PitchChargeSeconds)
                    : Mathf.Max(0, _charge - dt * (float)_feel.ChargeDecay);
                if (Controls.SouthDown) Launch(PlayerPitch());
                return;
            }
            if (_t > 0.55f) Launch(_match.CpuPitch());
        }

        PitchCommand PlayerPitch()
        {
            return new PitchCommand(_pitches[_pitchIndex], _charge, 0, _star && _match.CanStarPitch, _aimX, _aimY);
        }

        void Launch(PitchCommand pitch)
        {
            _pitch = pitch;
            var mph = AtBatResolver.PitchSpeedMph(pitch, _match.Pitcher);
            _pitchDur = Mathf.Clamp((float)(Diamond.Mound / (mph * 1.4667)), 0.32f, 0.85f);
            _flight = 0;
            _swung = false;
            _charge = 0;
            _phase = Phase.Flight;
            _t = 0;
            _ball = new Vector3(0, 5.4f, 60.5f);
            _aimX = (float)pitch.AimX;
            _aimY = (float)pitch.AimY;
            _zone.Show(true, _aimX, _aimY);
            _rig.Punch(pitch.Star ? 8f : 4f);
            _spec.ResetDecoy();
            _hideHelp = true;
            if (pitch.Star) _audio?.CaptainVo(_match.Pitcher.Id);
        }

        void TickFlight(float dt)
        {
            AimSetCamera();
            _flight += dt;
            var u = Mathf.Clamp01(_flight / _pitchDur);
            var p = PitchFlight.Point(_pitch.Type, u, _pitch.AimX, _pitch.AimY);
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
                if (Controls.Charge) _charge = Mathf.Min(1, _charge + dt / (float)_feel.SwingChargeSeconds);
                if (Controls.SouthDown && !_swung)
                {
                    _swung = true;
                    _swing = new SwingCommand(true, _charge, (_flight - _pitchDur) * 60f, _star && _match.CanStarSwing, Controls.StickX * 18f, Controls.WestHeld, Controls.StickY);
                }
            }
            if (u < 1) return;
            _swing ??= HumanBats
                ? new SwingCommand(false, _charge, 12, false)
                : _match.CpuSwing(_pitch, AtBatResolver.PitchInZone(_pitch, _match.Pitcher.Stats.Pitch));
            Resolve();
        }

        void Resolve()
        {
            var stealRunner = _match.LeadRunner;
            var stealBag = _match.LeadBag;
            var stealLead = _match.Lead01;
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
            _playerFielding = PlayerFields;
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
            if (hit.StarSwingUsed != null) _audio?.CaptainVo(_match.Batter.Id);
            if (hit.Quality == ContactQuality.Perfect || hit.StarSwingUsed != null)
            {
                _freeze = (float)_feel.SmashFreeze;
                _smash = (float)_feel.SmashHold;
                _cam.SmashAt(_ball);
            }
            else if (hit.Quality == ContactQuality.Solid)
            {
                _freeze = (float)_feel.SolidFreeze;
                _rig.Punch(8f);
            }
            AimDiamond(hit);
        }

        void AimDiamond(AtBatResult hit)
        {
            var grounder = hit.LaunchDeg < 14;
            var look = _ball.sqrMagnitude > 1 ? _ball : new Vector3((float)(_preview?.LandingX ?? 0), 3f, (float)(_preview?.LandingZ ?? 80));
            if (hit.StarSwingUsed != null)
                _cam.SmashAt(_ball);
            else if (grounder && hit.SprayDeg < -8)
                _cam.PlayLook("diamond-pull", look);
            else if (grounder)
                _cam.PlayLook("diamond-grounder", look);
            else
                _cam.PlayLook("diamond", look + new Vector3(0, 3, 10));
        }

    }
}
