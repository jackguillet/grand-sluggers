using System;
using System.Collections.Generic;
using System.Linq;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The live ball, presented (a real director since #1042). The sim (<see cref="LivePlaySystem"/>) owns the gloves, the
    /// catch, the throws, the relay chain, the close-play race, the bobble and the runner play (steals, pickoffs); this
    /// director turns the pads into one command per frame, mirrors the sim's state into <see cref="LiveFieldState"/> for the
    /// actors and the HUD, plays the cues it raises and aims the live camera. It owns the frame its live play began, the
    /// frost rings and the camera hold; the pads, the items and the result beat are the flow's (<see cref="IInPlayHost"/>).
    /// </summary>
    public sealed class InPlayDirector
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly LiveFieldState _live;
        readonly IInPlayHost _host;
        readonly Transform _root;
        int _liveBeganFrame = -1;

        internal InPlayDirector(MatchScene scene, PlayState play, LiveFieldState live, IInPlayHost host, Transform root)
        {
            _scene = scene; _play = play; _live = live; _host = host; _root = root;
        }

        /// <summary>A live play began on this frame's pre-contact tick: this frame's live tick is already spent.</summary>
        public void Began() => _liveBeganFrame = Time.frameCount;

        float LiveTime => _play.Match != null ? (float)_play.Match.LivePlay.ElapsedSeconds : 0f;


        public void Tick(float dt)
        {
            // A pre-contact tick already consumed this frame before handing off to live play.
            if (_liveBeganFrame == Time.frameCount) return;
            if (_play.Phase == MatchDirector.Phase.InPlay)
                TickInPlay(dt);
            else if (_play.Phase == MatchDirector.Phase.StealThrow) TickStealThrow(dt);
        }

        /// <summary>The pads for a live step: this frame's, with any press the hit-stop held folded in (CH-13).</summary>
        LivePadInput LiveFieldInput() => _host.Juice.Field(_host.FieldInput());
        LivePadInput LiveRunInput() => _host.Juice.Run(_host.RunInput());

        void TickInPlay(float dt)
        {
            var live = _play.Match.LivePlay;
            if (_play.Path == null || _play.Path.Length == 0) { _host.BeginResult(); return; }
            _host.TickItem(dt);
            var lesson = _host.FieldLesson;
            var result = lesson != null && (lesson.IsFieldLesson || lesson.IsItemLesson || lesson.IsGameContactLesson)
                ? TickTutorialField(dt)
                : live.Apply(LivePlayCommand.Tick(dt, LiveFieldInput(), LiveRunInput(), _host.ItemFlying, live.Source));
            SyncFromLive();
            PlayLiveCues(result);
            if (_host.Smash > 0) _host.Smash -= dt;
            AimLive();

            if (_scene.Ring != null && _play.Preview != null)
            {
                var hang = _play.Path != null ? BallFlight.HangTime(_play.Path, _play.Rules(_scene.Content)) : _play.Preview.HangTimeSec;
                if (LandingMark.On(_play.Preview, _play.Ball.y, LiveTime, _live.Caught, _live.Buddy, _play.Match.Rules, hang))
                {
                    var plant = LandingMark.At(_play.Preview, _play.Match.Rules, _play.Match.Park);
                    var who = PlayFielder();
                    _scene.Ring.Show(plant.X, plant.Z, (float)LandingMark.RadiusFt(_play.Preview),
                        LandingMark.Hot(LiveTime, hang, _play.Match.Rules, who, _play.Match.Park));
                }
                else
                    _scene.Ring.Hide();
            }

            if (result.CompletedPlay != null)
            {
                FinishLive(result.CompletedPlay, live.LastFieldResult);
                return;
            }
            // The sim commits every live ball itself, fouls included (#575). FlightDone is only a ball
            // with nothing to play (no path); it must never stand in for a result.
            if (result.FlightDone && !_host.ItemFlying) _host.BeginResult();
        }

        /// <summary>The sim's state, mirrored for the actor, HUD, and still partials.</summary>
        public void SyncFromLive()
        {
            var live = _play.Match.LivePlay;
            if (live.Events.Contains(LiveEvent.ThrowCommitted) && live.ThrowBag > 0 || live.Events.Contains(LiveEvent.ThrowQueueCleared)) _host.ClearThrowTarget();
            _play.Ball = new Vector3((float)live.BallX, (float)live.BallY, (float)live.BallZ);
            _live.GloveAt.Clear();
            foreach (var kv in live.Fielders) _live.GloveAt[kv.Key] = kv.Value;
            _live.GlovePos = live.GlovePos;
            _live.GloveX = live.GloveX;
            _live.GloveZ = live.GloveZ;
            _live.PlayerFielding = live.PlayerFielding;
            _live.Caught = live.Caught;
            _live.Buddy = live.Buddy;
            _live.Throwing = live.Throwing;
            _live.ThrowT = (float)live.ThrowT;
            _live.ThrowDur = (float)live.ThrowDur;
            _live.ThrowFrom = new Vector3((float)live.ThrowFrom.X, (float)live.ThrowFrom.Y, (float)live.ThrowFrom.Z);
            _live.ThrowTo = new Vector3((float)live.ThrowTo.X, (float)live.ThrowTo.Y, (float)live.ThrowTo.Z);
            _live.ThrowBag = live.ThrowBag;
            _live.ArmedThrow = live.ArmedThrow;
            _live.CoverPos = live.CoverPos;
            _live.ThrowFromPos = live.ThrowFromPos;
            _live.SwitchPos = live.SwitchPos;
            _live.BuddyPos = live.BuddyPos;
            _live.BuddyWindow = live.BuddyWindow;
            _live.DiveT = (float)live.DiveT;
            _live.JumpT = (float)live.JumpT;
            _live.SwapLock = (float)live.SwapLock;
            _live.RecoilT = (float)live.RecoilT;
            _live.Bobbling = live.Bobbling;
            // What each body owes (#719–#721), while the ball is live; the completing frame resets the field, so the last
            // live frame's debts carry into the result beat and run out there (ActorDirector ages them).
            if (live.Active) _live.Owed = FielderTells.Owed.Of(live, _play.Match.Rules);
            _live.ClosePlay = live.InClosePlay;
            _live.CloseBag = live.CloseBag;
            _live.CloseIcon = live.CloseIcon;
            _live.Dash01 = (float)live.Dash01;
            if (live.Field != null) _live.CpuField = live.Field;
            if (live.Preview != null) _play.Preview = live.Preview;
            if (live.Hit != null) _play.Pending = live.Hit;
            // The sim rewrites its path whole (a first-hop stall, a hop over a glove laid on each frame): copy it whenever it is a
            // new path, not only when its length changes, so the highlight replay draws the ball the play really had.
            if (live.Path != null && (_play.Path == null || !ReferenceEquals(live.Path, _pathFrom) || _play.Path.Length != live.Path.Count))
            {
                _pathFrom = live.Path;
                _play.Path = new Sample[live.Path.Count];
                for (var i = 0; i < _play.Path.Length; i++) _play.Path[i] = live.Path[i];
            }
            if (_live.Throwing && _live.ArmedThrow != null)
            {
                // The arc is presentation; the sim flies the ball flat at the receiver's glove height.
                var u = Mathf.Clamp01(_live.ThrowT / Mathf.Max(0.05f, _live.ThrowDur));
                var arc = _live.ArmedThrow.Relation == Chemistry.Good ? 5.2f
                    : _live.ArmedThrow.Relation == Chemistry.Bad ? 1.6f : 3.2f;
                var ball = _play.Ball;
                ball.y += Mathf.Sin(u * Mathf.PI) * arc;
                _play.Ball = ball;
            }
            if (!string.IsNullOrEmpty(live.Sub)) _host.Sub = live.Sub;
            ShowSlowRings(live);
        }

        /// <summary>The live path <see cref="PlayState.Path"/> was last copied from.</summary>
        IReadOnlyList<Sample> _pathFrom;

        readonly Dictionary<string, GameObject> _slowRings = new Dictionary<string, GameObject>();

        /// <summary>
        /// The body tell of a status volume (FD-15, F8-b): a frost ring at the feet of every fielder the volume is slowing, this
        /// frame, read from the sim (<c>IsSlowed</c>); it goes when the slow runs out.
        /// </summary>
        void ShowSlowRings(LivePlaySystem live)
        {
            foreach (var kv in _live.GloveAt)
            {
                var slowed = live.Active && live.IsSlowed(kv.Key);
                if (!_slowRings.TryGetValue(kv.Key, out var ring))
                {
                    if (!slowed) continue;
                    ring = Look.Torus("SlowRing-" + kv.Key, _root, 2.2f, 0.18f, Look.Unlit(new Color(0.62f, 0.88f, 1f)), seg: 28, sides: 6);
                    _slowRings[kv.Key] = ring;
                }
                ring.SetActive(slowed);
                if (slowed) ring.transform.position = new Vector3((float)kv.Value.X, 0.15f, (float)kv.Value.Z);
            }
        }

        void PlayLiveCues(LivePlayCommandResult result)
        {
            var live = _play.Match.LivePlay;
            foreach (var tell in live.Stamps)
                StampSmall(tell);
            foreach (var cue in live.Events)
            {
                switch (cue)
                {
                    case LiveEvent.Glove:
                        _scene.Audio?.Glove();
                        // The body's own catch (CH-13): its class's hold and its settle.
                        if (_play.Match.DefenseMap.TryGetValue(live.GlovePos, out var gloved)) _host.Juice.Catch(gloved, _scene.Feel);
                        break;
                    case LiveEvent.ThrowPop:
                        _scene.Park.Ball.Release();
                        _scene.Audio?.ThrowPop();
                        break;
                    case LiveEvent.ItemSmashed:
                        _host.ItemSmashed();
                        break;
                    // The hazards' tells (FD-15, F8-b): the word where it happened and a puff, from the typed events.
                    case LiveEvent.BodySlowed:
                        if (live.Slows.Any(s => s.Pos == _live.GlovePos)) StampSmall(PlayStamp.HazardTell(cue));
                        break;
                    case LiveEvent.BallRedirected:
                        StampSmall(PlayStamp.HazardTell(cue, live.RedirectsThisPlay.Count > 0 ? live.RedirectsThisPlay[^1].Type : null));
                        _scene.Park.Ball.ContactPuff(_play.Ball);
                        break;
                    case LiveEvent.RewardHit:
                        StampSmall(PlayStamp.HazardTell(cue));
                        _scene.Audio?.Swell();
                        break;
                    case LiveEvent.BodyCarom:
                        StampSmall(PlayStamp.HazardTell(cue));
                        _scene.Park.Ball.ContactPuff(_play.Ball);
                        _scene.Audio?.Glove();
                        break;
                    case LiveEvent.WallCarom:
                        // The ball met the fence below its top (§7.9): a thump and dust at the wall; the sim plays the carom.
                        _scene.Park.Ball.ContactPuff(_play.Ball);
                        _scene.Audio?.Glove();
                        break;
                    case LiveEvent.ThrowSailed:
                        // The throw skipped past its cover (§8.5, §8.6): the ball is loose; the ERROR tell
                        // is the live stamp at Dirt, not a second card at Time.
                        _scene.Park.Ball.Release();
                        _scene.Park.Ball.ContactPuff(_play.Ball);
                        break;
                    case LiveEvent.Bobble:
                        // The fumble (§8.6): the ball scatters on the dirt; the glove chases it. A ball that got past (#721)
                        // kicks the dirt at the fumbler's feet instead and carries on as a batted ball, its trail on.
                        _scene.Park.Ball.Release();
                        if (live.Deflected) DustAt(live.StunPos);
                        else _scene.Park.Ball.ContactPuff(_play.Ball);
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
                _host.Sub = step.Caption;
            if (_live.Bobbling) _scene.Park.Ball.Release();
        }

        /// <summary>A kick of dirt at a body's feet (#719–#721): the live glove where the ring is, else the body's own spot.</summary>
        void DustAt(string pos)
        {
            var at = pos == _live.GlovePos ? (X: _live.GloveX, Z: _live.GloveZ)
                : !string.IsNullOrEmpty(pos) && _live.GloveAt.TryGetValue(pos, out var body) ? body
                : (X: _live.GloveX, Z: _live.GloveZ);
            _scene.Park.Ball.ContactPuff(new Vector3((float)at.X, 0f, (float)at.Z));
        }

        void FinishLive(PlayEvent play, FieldingResult fieldResult)
        {
            foreach (var ring in _slowRings.Values) if (ring != null) ring.SetActive(false);
            _play.Last = play;
            MirrorBodiesAtTime(play);
            if (fieldResult != null) _host.OnFieldResult(fieldResult);
            _host.Banner();
            if (_play.Last != null && _play.Last.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double)
                _scene.Audio?.Swell();
            _live.PlayerFielding = false;
            _live.CpuField = null;
            _live.Throwing = false;
            _live.ClosePlay = false;
            _live.CloseIcon = false;
            _live.CoverPos = "";
            _live.ThrowFromPos = "";
            _live.Bobbling = false;
            _live.RecoilT = 0;
            _camHold.Reset();
            _scene.Park.Ball.Release();
            if (_play.Last != null && !PlayStamp.ShowsAtTime(_play.Last) && !string.IsNullOrEmpty(_live.BagStamp))
                _live.BagStampHold = (float)PlayStamp.HoldSeconds(_play.Last.Kind, _scene.Feel);
            _host.BeginResult();
        }

        /// <summary>A live tell at its named anchor (OUT at the glove, SCORE at the plate, SAFE / ERROR on the dirt).</summary>
        void StampSmall(LiveStamp tell)
        {
            if (tell == null || string.IsNullOrEmpty(tell.Word)) return;
            _live.BagStamp = tell.Word;
            _live.BagStampAnchor = tell.Anchor;
            _live.BagStampT = 0;
            _live.BagStampHold = (float)PlayStamp.SafeHoldSeconds(_scene.Feel);
        }

        public Character PlayFielder()
        {
            if (_live.CpuField != null && _live.CpuField.Fielder != null) return _live.CpuField.Fielder;
            return _play.Preview != null ? _play.Preview.Fielder : _play.Match.Pitcher;
        }

        /// <summary>The live camera's target hysteresis (D14): one per director, reset when a play ends.</summary>
        readonly PlayCamera.CameraHold _camHold = new();

        /// <summary>
        /// The live camera (spec §15, D14): one typed view of this frame into the sim's beat table, through the
        /// hold. Null while the SET shot still holds after the crack (<c>contactCutSeconds</c>); the smash rides
        /// the batter; an ordinary throw keeps the follow on the ball.
        /// </summary>
        public void AimLive()
        {
            var live = _play.Match.LivePlay;
            var batter = _host.SmashLook();
            var view = new PlayCamera.LiveView(
                LiveTime, _play.Pending, live.RunnerPlay, _live.ClosePlay, _live.CloseBag,
                live.InRundown, live.RunnerPlayBag, _host.Smash,
                new Vec3(_play.Ball.x, _play.Ball.y, _play.Ball.z), new Vec3(batter.x, batter.y, batter.z),
                DiamondGeometry.Of(_play.Match.Rules));
            var framed = PlayCamera.LiveFraming(_scene.Content.Shots, view, _scene.Feel, _camHold);
            if (framed is { } f) _scene.Cam.Live(f);
        }

        /// <summary>
        /// The play died: the sim has reset its field, so the mirror is re-seated from the typed outcome's
        /// bodies at Time (§10.6, #574). The catcher stays where the catch happened; on a third out the
        /// bodies are still the defense that made it, whatever the match flipped to.
        /// </summary>
        void MirrorBodiesAtTime(PlayEvent play)
        {
            var bodies = play?.Outcome?.BodiesAtTime;
            if (bodies == null || bodies.Count == 0) { _live.ResultBodies = null; return; }
            _live.ResultBodies = bodies;
            _live.GloveAt.Clear();
            foreach (var b in bodies)
                if (!b.IsRunner) _live.GloveAt[b.Pos] = (b.X, b.Z);
        }

        public bool BuddySet => _play.Preview != null && FieldingResolver.BuddyJumpOffered(_play.Preview);

        public (double X, double Z) WallPlant(FieldingPreview pre) => FlyCatch.WallPlant(pre, _play.Rules(_scene.Content), _play.Match?.Park);

        public PlayKind LiveKind() => _play.Match.LivePlay.PlayKind;

        // ---- The runner play (§11.3, §11.4): the sim runs the catcher's throw or the pickoff; this draws it. ----

        /// <summary>After a take or a miss with a runner who broke (<paramref name="pitch"/>), or a pickoff already begun in the sim (null).</summary>
        public void StartRunnerPlay(PlayEvent pitch)
        {
            if (pitch != null) _play.Last = pitch;
            _play.Phase = MatchDirector.Phase.StealThrow;
            _liveBeganFrame = Time.frameCount;
            _host.RestartClock();
            _camHold.Reset();
            _play.Pending = null;
            _play.Preview = null;
            _live.CpuField = null;
            _play.Path = null;
            _scene.Park.Ball.Release();
            if (pitch != null)
                _play.Match.LivePlay.Apply(LivePlayCommand.BeginSteal(pitch, _host.LiveSeatsNow(), _play.Match.LivePlay.Source));
            SyncFromLive();
            PlayLiveCues(new LivePlayCommandResult(_play.Match.LivePlay.Snapshot));
            if (!_play.Match.LivePlay.Active)
            {
                // Nobody to play on (every armed runner was entitled by the walk): the pitch stands.
                _host.BeginResult();
                return;
            }
            AimStealThrowCam();
        }

        void TickStealThrow(float dt)
        {
            var live = _play.Match.LivePlay;
            var result = _host.FieldLesson != null ? TickTutorialField(dt)
                : live.Apply(LivePlayCommand.Tick(dt, LiveFieldInput(), LiveRunInput(), false, live.Source));
            SyncFromLive();
            PlayLiveCues(result);
            AimLive();
            if (result.CompletedPlay != null)
            {
                _play.Last = result.CompletedPlay;
                MirrorBodiesAtTime(_play.Last);
                _host.Banner();
                _live.Throwing = false;
                _live.Caught = false;
                _live.CoverPos = "";
                _live.ThrowFromPos = "";
                _live.PlayerFielding = false;
                _scene.Park.Ball.Release();
                _host.BeginResult();
            }
        }

        void AimStealThrowCam() => AimLive();

        LivePlayCommandResult TickTutorialField(float dt)
        {
            var run = _host.FieldLesson;
            // Both pads are taken, so a press the hit-stop held on the pad a lesson does not read is not delivered later.
            var field = LiveFieldInput();
            var running = LiveRunInput();
            var pad = run.IsOffenseLesson ? running : field;
            // Preserve simulation time through a long rendering frame without repeating edge-triggered commands.
            var left = (double)dt;
            LivePlayCommandResult result = new LivePlayCommandResult(_play.Match.LivePlay.Snapshot);
            while (left > 0 && run.Phase == TutorialPhase.Attempt)
            {
                var step = Math.Min(left, .05);
                run.Tick(step, pad);
                result = run.LastTickResult ?? result;
                left -= step;
                if (left > 0) PlayLiveCues(result);
                pad = pad with { SouthDown = false, WestDown = false, EastDown = false, Swap = false, Cancel = false, Cutoff = false };
            }
            return result;
        }
    }

    /// <summary>What the live play asks of the match flow: the pads and seats, the lesson in play, the items, the juice and the result beat.</summary>
    internal interface IInPlayHost
    {
        LivePadInput FieldInput();
        LivePadInput RunInput();
        void ClearThrowTarget();
        LiveSeats LiveSeatsNow();
        JuiceDirector Juice { get; }
        /// <summary>The tutorial lesson in play, or null.</summary>
        TutorialSession FieldLesson { get; }
        void OnFieldResult(FieldingResult result);
        void TickItem(float dt);
        bool ItemFlying { get; }
        void ItemSmashed();
        void Banner();
        void BeginResult();
        Vector3 SmashLook();
        float Smash { get; set; }
        string Sub { set; }
        /// <summary>The phase clock starts again (a new beat).</summary>
        void RestartClock();
    }
}
