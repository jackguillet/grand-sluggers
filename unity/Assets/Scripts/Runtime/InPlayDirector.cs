using System.Collections.Generic;
using System.Linq;
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
        int _liveBeganFrame = -1;

        internal void TickLive(float dt)
        {
            // A pre-contact tick already consumed this frame before handing off to live play.
            if (_liveBeganFrame == Time.frameCount) return;
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
            // The calibrated radial stick (#718) reads the device coordinate before any dead zone; the shipped Manhattan gate
            // keeps the stick it always read. One coordinate per table, handed to the sim once.
            var radial = _match != null && _match.Rules.Fielding.Stick.Radial;
            // One pad can bat and field (Training): an East press the plate took as the swing cancel is no dive and no
            // dash until it comes up (PH-13-R1), and LT held for a bunt at contact is no item modifier (PH-14-R6).
            var eastFree = CancelFree(pad);
            // LB held from a release that asked for a special is no cutoff until it comes up (PH-16-R10, R17).
            var lbFree = StarFree(pad);
            return new LivePadInput(
                radial ? pad.PursuitX : pad.StickX, radial ? pad.PursuitY : pad.StickY,
                pad.SouthDown, pad.WestDown, pad.EastDown && eastFree, pad.EastHeld && eastFree,
                pad.CutoffWith(lbFree), pad.SwapPitcher, pad.ItemWith(TriggerFree(pad, BuntSide.Third)), pad.Attack,
                pad.ThrowBag, pad.StickBag, pad.ArrowBag,
                Cancel: pad.AllReturn,
                Device: pad.Index);
        }

        /// <summary>The offense pad as the sim's runner verbs see it (spec §9.3): the bodies are moved in the sim, never here.</summary>
        LivePadInput RunInput()
        {
            var pad = RunPad;
            // A star swing's LB still down after contact is not all-advance until it comes up (PH-16-R10, R17).
            var lbFree = StarFree(pad);
            return new LivePadInput(pad.StickX, pad.StickY, pad.SouthDown, pad.WestDown,
                KeysBag: pad.ThrowBag, StickBag: pad.StickBag,
                AllAdvance: pad.AllAdvanceWith(lbFree), AllReturn: pad.AllReturn, Freeze: pad.FreezeRunnersWith(lbFree));
        }

        void TickInPlay(float dt)
        {
            var live = _match.LivePlay;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            TickItem(dt);
            var result = TutorialOn && (_coach.Tutorial.IsFieldLesson || _coach.Tutorial.IsItemLesson || _coach.Tutorial.IsGameContactLesson)
                ? TickTutorialField(dt)
                : live.Apply(LivePlayCommand.Tick(dt, FieldInput(), RunInput(), _itemFlying, live.Source));
            SyncFromLive();
            PlayLiveCues(result);
            if (_smash > 0) _smash -= dt;
            AimLive();

            if (_ring != null && _preview != null)
            {
                var hang = _path != null ? BallFlight.HangTime(_path, MatchRules) : _preview.HangTimeSec;
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
            // What each body owes (#719–#721), while the ball is live; the completing frame resets the field, so the last
            // live frame's debts carry into the result beat and run out there (ActorDirector ages them).
            if (live.Active) _owed = FielderTells.Owed.Of(live, _match.Rules);
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
            ShowSlowRings(live);
        }

        readonly Dictionary<string, GameObject> _slowRings = new Dictionary<string, GameObject>();

        /// <summary>
        /// The body tell of a status volume (FD-15, F8-b): a frost ring at the feet of every fielder the volume is slowing, this
        /// frame, read from the sim (<c>IsSlowed</c>); it goes when the slow runs out.
        /// </summary>
        void ShowSlowRings(LivePlaySystem live)
        {
            foreach (var kv in _gloveAt)
            {
                var slowed = live.Active && live.IsSlowed(kv.Key);
                if (!_slowRings.TryGetValue(kv.Key, out var ring))
                {
                    if (!slowed) continue;
                    ring = Look.Torus("SlowRing-" + kv.Key, transform, 2.2f, 0.18f, Look.Unlit(new Color(0.62f, 0.88f, 1f)), seg: 28, sides: 6);
                    _slowRings[kv.Key] = ring;
                }
                ring.SetActive(slowed);
                if (slowed) ring.transform.position = new Vector3((float)kv.Value.X, 0.15f, (float)kv.Value.Z);
            }
        }

        void PlayLiveCues(LivePlayCommandResult result)
        {
            var live = _match.LivePlay;
            foreach (var tell in live.Stamps)
                StampSmall(tell);
            foreach (var cue in live.Events)
            {
                switch (cue)
                {
                    case LiveEvent.Glove:
                        _audio?.Glove();
                        break;
                    case LiveEvent.ThrowPop:
                        _park.Ball.Release();
                        _audio?.ThrowPop();
                        break;
                    case LiveEvent.ItemSmashed:
                        _itemFlying = false;
                        _itemId = "";
                        _items?.Hide();
                        break;
                    // The hazards' tells (FD-15, F8-b): the word where it happened and a puff, from the typed events.
                    case LiveEvent.BodySlowed:
                        if (live.Slows.Any(s => s.Pos == _glovePos)) StampSmall(PlayStamp.HazardTell(cue));
                        break;
                    case LiveEvent.BallRedirected:
                        StampSmall(PlayStamp.HazardTell(cue, live.RedirectsThisPlay.Count > 0 ? live.RedirectsThisPlay[^1].Type : null));
                        _park.Ball.ContactPuff(_ball);
                        break;
                    case LiveEvent.RewardHit:
                        StampSmall(PlayStamp.HazardTell(cue));
                        _audio?.Swell();
                        break;
                    case LiveEvent.BodyCarom:
                        StampSmall(PlayStamp.HazardTell(cue));
                        _park.Ball.ContactPuff(_ball);
                        _audio?.Glove();
                        break;
                    case LiveEvent.WallCarom:
                        // The ball met the fence below its top (§7.9): a thump and dust at the wall; the sim plays the carom.
                        _park.Ball.ContactPuff(_ball);
                        _audio?.Glove();
                        break;
                    case LiveEvent.ThrowSailed:
                        // The throw skipped past its cover (§8.5, §8.6): the ball is loose; the ERROR tell
                        // is the live stamp at Dirt, not a second card at Time.
                        _park.Ball.Release();
                        _park.Ball.ContactPuff(_ball);
                        break;
                    case LiveEvent.Bobble:
                        // The fumble (§8.6): the ball scatters on the dirt; the glove chases it. A ball that got past (#721)
                        // kicks the dirt at the fumbler's feet instead and carries on as a batted ball, its trail on.
                        _park.Ball.Release();
                        if (live.Deflected) DustAt(live.StunPos);
                        else _park.Ball.ContactPuff(_ball);
                        break;
                    case LiveEvent.JumpTakeoff:
                        // The normal jump leaves the ground this frame (#719): dirt at the feet; the rise is the sim's arc.
                        DustAt(live.GlovePos);
                        break;
                    case LiveEvent.DiveCommit:
                        // The dive is committed and its recovery owed (#719): the diver hits the dirt where the lunge put it.
                        DustAt(live.DivingPos);
                        break;
                    case LiveEvent.ImpactRecoil:
                        // A hard ball costs the hands (#720): the brace squashes the body and the skid kicks dirt at its feet.
                        DustAt(live.GlovePos);
                        break;
                }
            }
            if (result.Throw is { } step && !string.IsNullOrEmpty(step.Caption))
                _sub = step.Caption;
            if (_bobbling) _park.Ball.Release();
        }

        /// <summary>A kick of dirt at a body's feet (#719–#721): the live glove where the ring is, else the body's own spot.</summary>
        void DustAt(string pos)
        {
            var at = pos == _glovePos ? (X: _fx, Z: _fz)
                : !string.IsNullOrEmpty(pos) && _gloveAt.TryGetValue(pos, out var body) ? body
                : (X: _fx, Z: _fz);
            _park.Ball.ContactPuff(new Vector3((float)at.X, 0f, (float)at.Z));
        }

        void FinishLive(PlayEvent play, FieldingResult fieldResult)
        {
            foreach (var ring in _slowRings.Values) if (ring != null) ring.SetActive(false);
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
            _camHold.Reset();
            _park.Ball.Release();
            if (_last != null && !PlayStamp.ShowsAtTime(_last) && !string.IsNullOrEmpty(_bagStamp))
                _bagStampHold = (float)PlayStamp.HoldSeconds(_last.Kind, _feel);
            BeginResult();
        }

        /// <summary>A live tell at its named anchor (OUT at the glove, SCORE at the plate, SAFE / ERROR on the dirt).</summary>
        void StampSmall(LiveStamp tell)
        {
            if (tell == null || string.IsNullOrEmpty(tell.Word)) return;
            _bagStamp = tell.Word;
            _bagStampAnchor = tell.Anchor;
            _bagStampT = 0;
            _bagStampHold = (float)PlayStamp.SafeHoldSeconds(_feel);
        }

        Character PlayFielder()
        {
            if (_cpuField != null && _cpuField.Fielder != null) return _cpuField.Fielder;
            return _preview != null ? _preview.Fielder : _match.Pitcher;
        }

        /// <summary>The live camera's target hysteresis (D14): one per director, reset when a play ends.</summary>
        readonly PlayCamera.CameraHold _camHold = new();

        /// <summary>
        /// The live camera (spec §15, D14): one typed view of this frame into the sim's beat table, through the
        /// hold. Null while the SET shot still holds after the crack (<c>contactCutSeconds</c>); the smash rides
        /// the batter; an ordinary throw keeps the follow on the ball.
        /// </summary>
        void AimLive()
        {
            var live = _match.LivePlay;
            var batter = SmashLook();
            var view = new PlayCamera.LiveView(
                LiveTime, _pending, live.RunnerPlay, _closePlay, _closeBag,
                live.InRundown, live.RunnerPlayBag, _smash,
                new Vec3(_ball.x, _ball.y, _ball.z), new Vec3(batter.x, batter.y, batter.z));
            var framed = PlayCamera.LiveFraming(_content.Shots, view, _feel, _camHold);
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

        (double X, double Z) WallPlant(FieldingPreview pre) => FlyCatch.WallPlant(pre, _match?.Park, MatchRules);

        PlayKind LiveKind() => _match.LivePlay.PlayKind;

        // ---- The runner play (§11.3, §11.4): the sim runs the catcher's throw or the pickoff; this draws it. ----

        /// <summary>After a take or a miss with a runner who broke (<paramref name="pitch"/>), or a pickoff already begun in the sim (null).</summary>
        void StartRunnerPlay(PlayEvent pitch)
        {
            if (pitch != null) _last = pitch;
            _phase = Phase.StealThrow;
            _liveBeganFrame = Time.frameCount;
            _t = 0;
            _camHold.Reset();
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
            var result = TutorialOn ? TickTutorialField(dt)
                : live.Apply(LivePlayCommand.Tick(dt, FieldInput(), RunInput(), false, live.Source));
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
