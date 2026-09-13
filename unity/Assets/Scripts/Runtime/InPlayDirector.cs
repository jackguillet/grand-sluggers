using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The live ball, presented. The sim (<see cref="LivePlaySystem"/>) owns the gloves, the
    /// catch, the throws, the relay chain, the close-play race, the bobble and the runner play (steals, pickoffs);
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
                TickInPlay(dt);
            else if (_phase == Phase.StealThrow) TickStealThrow(dt);
        }

        /// <summary>The sim's seat table for this half. Training seats come from the coach; a match derives them from (half, home/away, pads).</summary>
        LiveSeats LiveSeatsNow() => TrainingOn
            ? new LiveSeats(HumanBats, HumanPitches, PlayerMustField, Versus: false)
            : _match != null ? GrandSluggers.Sim.LiveSeats.For(LiveSeats, _match.Top) : GrandSluggers.Sim.LiveSeats.CpuOnly;

        LivePadInput FieldInput()
        {
            var pad = FieldPad;
            return new LivePadInput(
                pad.StickX, pad.StickY,
                pad.SouthDown, pad.WestDown, pad.EastDown, pad.EastHeld,
                pad.Cutoff, pad.SwapPitcher, pad.Item, pad.Attack,
                pad.ThrowBag, pad.StickBag, pad.ArrowBag);
        }

        /// <summary>The offense pad as the sim's runner verbs see it (spec §9.3): the bodies are moved in the sim, never here.</summary>
        LivePadInput RunInput()
        {
            var pad = RunPad;
            return new LivePadInput(pad.StickX, pad.StickY, pad.SouthDown, pad.WestDown,
                KeysBag: pad.ThrowBag, StickBag: pad.StickBag,
                AllAdvance: pad.AllAdvance, AllReturn: pad.AllReturn, Freeze: pad.FreezeRunners);
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
            AimLive();

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
            // The sim commits every live ball itself, fouls included (#575). FlightDone is only a ball
            // with nothing to play (no path); it must never stand in for a result.
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
            _closePlay = live.InClosePlay;
            _closeBag = live.CloseBag;
            _closeIcon = live.CloseIcon;
            _dash01 = (float)live.Dash01;
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
                        StampSmall(PlayStamp.LiveTell(cue));
                        break;
                    case LiveEvent.ItemSmashed:
                        _itemFlying = false;
                        _itemId = "";
                        _items?.Hide();
                        break;
                    case LiveEvent.WallCarom:
                        // The ball met the fence below its top (§7.9): a thump and dust at the wall; the sim plays the carom.
                        _park.Ball.ContactPuff(_ball);
                        _audio?.Glove();
                        break;
                    case LiveEvent.ThrowSailed:
                        // The throw skipped past its cover (§8.5, §8.6): the ball is loose, the small ERROR tell
                        // pops now, and the play's stamp comes at Time from the typed outcome.
                        _park.Ball.Release();
                        _park.Ball.ContactPuff(_ball);
                        StampSmall(PlayStamp.LiveTell(cue));
                        break;
                    case LiveEvent.Bobble:
                        // The fumble (§8.6): the ball scatters on the dirt; the glove chases it.
                        _park.Ball.Release();
                        _park.Ball.ContactPuff(_ball);
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
            MirrorBodiesAtTime(play);
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

        /// <summary>The small mid-play tell (SAFE, ERROR): the same sticker, the count's scale and hold.</summary>
        void StampSmall(string tell)
        {
            if (string.IsNullOrEmpty(tell)) return;
            _bagStamp = tell;
            _bagStampT = 0;
        }

        Character PlayFielder()
        {
            if (_cpuField != null && _cpuField.Fielder != null) return _cpuField.Fielder;
            return _preview != null ? _preview.Fielder : _match.Pitcher;
        }

        /// <summary>
        /// The live camera (spec §15): one typed view of this frame into the sim's beat table. Null while
        /// the SET shot still holds after the crack (<c>contactCutSeconds</c>); the smash rides the batter.
        /// </summary>
        void AimLive()
        {
            var live = _match.LivePlay;
            var batter = SmashLook();
            var view = new PlayCamera.LiveView(
                LiveTime, _pending, live.RunnerPlay, _throwing, _throwBag, _closePlay, _closeBag,
                live.InRundown, live.RunnerPlayBag, _smash,
                new Vec3(_ball.x, _ball.y, _ball.z), new Vec3(batter.x, batter.y, batter.z));
            var framed = PlayCamera.LiveFraming(_content.Shots, view, _feel);
            if (framed is { } f) _cam.Live(f);
        }

        /// <summary>
        /// The play died: the sim has reset its field, so the mirror is re-seated from the typed outcome's
        /// bodies at Time (§10.6, #574). The catcher stays where the catch happened; on a third out the
        /// bodies are still the defense that made it, whatever the match flipped to.
        /// </summary>
        void MirrorBodiesAtTime(PlayEvent play)
        {
            var bodies = play?.Outcome?.BodiesAtTime;
            if (bodies == null || bodies.Count == 0) { _resultBodies = null; return; }
            _resultBodies = bodies;
            _gloveAt.Clear();
            foreach (var b in bodies)
                if (!b.IsRunner) _gloveAt[b.Pos] = (b.X, b.Z);
        }

        bool BuddySet => _preview != null && FieldingResolver.BuddyJumpOffered(_preview);

        (double X, double Z) WallPlant(FieldingPreview pre) => FlyCatch.WallPlant(pre, _match?.Park, _content.Rules);

        PlayKind LiveKind() => _match.LivePlay.PlayKind;

        // ---- The runner play (§11.3, §11.4): the sim runs the catcher's throw or the pickoff; this draws it. ----

        /// <summary>After a take or a miss with a runner who broke (<paramref name="pitch"/>), or a pickoff already begun in the sim (null).</summary>
        void StartRunnerPlay(PlayEvent pitch)
        {
            if (pitch != null) _last = pitch;
            _phase = Phase.StealThrow;
            _t = 0;
            _pending = null;
            _preview = null;
            _cpuField = null;
            _path = null;
            _park.Ball.Release();
            if (pitch != null)
                _match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch, LiveSeatsNow(), _match.LivePlay.Source));
            SyncFromLive();
            PlayLiveCues(new LivePlayCommandResult(_match.LivePlay.Snapshot));
            if (!_match.LivePlay.Active)
            {
                // Nobody to play on (every armed runner was entitled by the walk): the pitch stands.
                BeginResult();
                return;
            }
            AimStealThrowCam();
        }

        void TickStealThrow(float dt)
        {
            var live = _match.LivePlay;
            var result = live.Apply(LivePlayCommand.Tick(dt, FieldInput(), RunInput(), false, live.Source));
            SyncFromLive();
            PlayLiveCues(result);
            AimLive();
            if (result.CompletedPlay != null)
            {
                _last = result.CompletedPlay;
                MirrorBodiesAtTime(_last);
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

        void AimStealThrowCam() => AimLive();
    }
}
