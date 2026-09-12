using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The live ball, presented. The sim (<see cref="LivePlaySystem"/>) owns the gloves, the
    /// catch, the throws, the relay chain, the close-play race, the bobble and the steal phase;
    /// this partial translates the pads into one command per frame, mirrors the sim's state
    /// into the director's fields for the actors and HUD, and plays the cues it raises.
    /// </summary>
    public sealed class InPlayDirector
    {
        readonly MatchDirector _play;
        public InPlayDirector(MatchDirector play) { _play = play; }
        public void Tick(float dt) { _play.TickLive(dt); }
    }

    public sealed partial class MatchDirector
    {
        internal void TickLive(float dt)
        {
            if (_phase == Phase.InPlay)
            {
                TickBaserunning(dt);
                TickInPlay(dt);
            }
            else if (_phase == Phase.StealThrow) TickStealThrow(dt);
        }

        LiveSeats LiveSeatsNow() => new LiveSeats(HumanBats, HumanPitches, PlayerMustField, Versus);

        LivePadInput FieldInput()
        {
            var pad = FieldPad;
            return new LivePadInput(
                pad.StickX, pad.StickY,
                pad.SouthDown, pad.WestDown, pad.EastDown, pad.EastHeld,
                pad.Cutoff, pad.SwapPitcher, pad.Item, pad.Attack,
                pad.ThrowBag, pad.StickBag, pad.ArrowBag);
        }

        LivePadInput RunInput()
        {
            var pad = RunPad;
            return new LivePadInput(pad.StickX, pad.StickY, pad.SouthDown);
        }

        void TickInPlay(float dt)
        {
            var live = _match.LivePlay;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            TickItem(dt);
            var result = live.Apply(LivePlayCommand.Tick(dt, FieldInput(), RunInput(), _itemFlying, live.Source));
            SyncFromLive();
            PlayLiveCues(result);
            if (_smash > 0) _smash -= dt;
            _cam.HoldInPlay(_ball, FlyCam());

            if (_ring != null && _preview != null)
            {
                var hang = _path != null ? BallFlight.HangTime(_path, _content.Rules) : _preview.HangTimeSec;
                if (LandingMark.On(_preview, _ball.y, LiveTime, _caught, _buddy, hang))
                {
                    var plant = LandingMark.At(_preview, _match.Park);
                    var who = PlayFielder();
                    _ring.Show(plant.X, plant.Z, (float)LandingMark.RadiusFt(_preview),
                        LandingMark.Hot(LiveTime, hang, who, _match.Park));
                }
                else
                    _ring.Hide();
            }

            if (result.CompletedPlay != null)
            {
                FinishLive(result.CompletedPlay, live.LastFieldResult);
                return;
            }
            if (result.FlightDone && !_itemFlying) BeginResult();
        }

        /// <summary>The sim's state, mirrored for the actor, HUD, and still partials.</summary>
        void SyncFromLive()
        {
            var live = _match.LivePlay;
            _ball = new Vector3((float)live.BallX, (float)live.BallY, (float)live.BallZ);
            _gloveAt.Clear();
            foreach (var kv in live.Fielders) _gloveAt[kv.Key] = kv.Value;
            _glovePos = live.GlovePos;
            _fx = live.GloveX;
            _fz = live.GloveZ;
            _playerFielding = live.PlayerFielding;
            _caught = live.Caught;
            _buddy = live.Buddy;
            _throwing = live.Throwing;
            _throwT = (float)live.ThrowT;
            _throwDur = (float)live.ThrowDur;
            _throwFrom = new Vector3((float)live.ThrowFrom.X, (float)live.ThrowFrom.Y, (float)live.ThrowFrom.Z);
            _throwTo = new Vector3((float)live.ThrowTo.X, (float)live.ThrowTo.Y, (float)live.ThrowTo.Z);
            _throwBag = live.ThrowBag;
            _armedThrow = live.ArmedThrow;
            _coverPos = live.CoverPos;
            _throwFromPos = live.ThrowFromPos;
            _switchPos = live.SwitchPos;
            _buddyPos = live.BuddyPos;
            _buddyWindow = live.BuddyWindow;
            _diveT = (float)live.DiveT;
            _jumpT = (float)live.JumpT;
            _swapLock = (float)live.SwapLock;
            _recoilT = (float)live.RecoilT;
            _bobbling = live.Bobbling;
            _catchDive = live.CatchDive;
            _catchJump = live.CatchJump;
            _closePlay = live.InClosePlay;
            _closeBag = live.CloseBag;
            _closeIcon = live.CloseIcon;
            _dash01 = (float)live.Dash01;
            _stealT = (float)live.StealT;
            if (live.Field != null) _cpuField = live.Field;
            if (live.Preview != null) _preview = live.Preview;
            if (live.Hit != null) _pending = live.Hit;
            if (live.Path != null && (_path == null || _path.Length != live.Path.Count))
            {
                _path = new Sample[live.Path.Count];
                for (var i = 0; i < _path.Length; i++) _path[i] = live.Path[i];
            }
            if (_throwing && _armedThrow != null)
            {
                // The arc is presentation; the sim flies the ball flat at the receiver's glove height.
                var u = Mathf.Clamp01(_throwT / Mathf.Max(0.05f, _throwDur));
                var arc = _armedThrow.Relation == Chemistry.Good ? 5.2f
                    : _armedThrow.Relation == Chemistry.Bad ? 1.6f : 3.2f;
                _ball.y += Mathf.Sin(u * Mathf.PI) * arc;
            }
            if (!string.IsNullOrEmpty(live.Sub)) _sub = live.Sub;
        }

        void PlayLiveCues(LivePlayCommandResult result)
        {
            var live = _match.LivePlay;
            foreach (var cue in live.Events)
            {
                switch (cue)
                {
                    case LiveEvent.Glove:
                        _audio?.Glove();
                        break;
                    case LiveEvent.ThrowPop:
                        _park.Ball.Release();
                        if (_armedThrow != null) _spec.ArmThrow(_throwFrom, _throwTo, _armedThrow);
                        _audio?.ThrowPop();
                        break;
                    case LiveEvent.StampSafe:
                        StampSafe();
                        break;
                    case LiveEvent.ItemSmashed:
                        _itemFlying = false;
                        _itemId = "";
                        _items?.Hide();
                        break;
                }
            }
            if (result.Throw is { } step && !string.IsNullOrEmpty(step.Caption))
                _sub = step.Caption;
            if (_bobbling) _park.Ball.Release();
        }

        void FinishLive(PlayEvent play, FieldingResult fieldResult)
        {
            _last = play;
            if (fieldResult != null) _coach?.OnField(fieldResult, _match);
            Banner();
            if (_last != null && _last.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double)
                _audio?.Swell();
            _playerFielding = false;
            _cpuField = null;
            _throwing = false;
            _closePlay = false;
            _closeIcon = false;
            _coverPos = "";
            _throwFromPos = "";
            _bobbling = false;
            _recoilT = 0;
            _park.Ball.Release();
            BeginResult();
        }

        void StampSafe()
        {
            _bagStamp = PlayStamp.Safe;
            _bagStampT = 0;
        }

        Character PlayFielder()
        {
            if (_cpuField != null && _cpuField.Fielder != null) return _cpuField.Fielder;
            return _preview != null ? _preview.Fielder : _match.Pitcher;
        }

        bool FlyCam()
        {
            if (_preview != null) return FlyCatch.IsFly(_preview);
            if (_pending != null)
                return !FieldingResolver.IsGrounder(_pending, _content.Rules) && !FieldingResolver.IsLine(_pending, _content.Rules);
            return _last != null
                && !FieldingResolver.IsGrounder(_last.AtBat, _content.Rules)
                && !FieldingResolver.IsLine(_last.AtBat, _content.Rules);
        }

        bool BuddySet => _preview != null && FieldingResolver.BuddyJumpOffered(_preview);

        (double X, double Z) WallPlant(FieldingPreview pre) => FlyCatch.WallPlant(pre, _match?.Park, _content.Rules);

        PlayKind LiveKind() => _match.LivePlay.PlayKind;

        // ---- Steal phase: the sim runs the catcher's throw play; this draws it. ----

        void StartStealThrow(PlayEvent pitch)
        {
            _last = pitch;
            _phase = Phase.StealThrow;
            _t = 0;
            _pending = null;
            _preview = null;
            _cpuField = null;
            _path = null;
            _park.Ball.Release();
            _match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch, LiveSeatsNow(), _match.LivePlay.Source));
            SyncFromLive();
            PlayLiveCues(new LivePlayCommandResult(_match.LivePlay.Snapshot));
            AimStealThrowCam();
        }

        void TickStealThrow(float dt)
        {
            var live = _match.LivePlay;
            var result = live.Apply(LivePlayCommand.Tick(dt, FieldInput(), RunInput(), false, live.Source));
            SyncFromLive();
            PlayLiveCues(result);
            _cam.HoldInPlay(_ball);
            if (result.CompletedPlay != null)
            {
                _last = result.CompletedPlay;
                Banner();
                _throwing = false;
                _caught = false;
                _coverPos = "";
                _throwFromPos = "";
                _playerFielding = false;
                _park.Ball.Release();
                BeginResult();
            }
        }

        void AimStealThrowCam() => _cam.HoldInPlay(_ball);
    }
}
