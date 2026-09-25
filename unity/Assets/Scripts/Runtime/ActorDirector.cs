using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The bodies on screen (a real director since #1042): every glove, the batter, the runners and the menu captains
    /// posed and placed each frame from the sim's state; the ball, its trail and the special effects drawn; the item
    /// toss presented. Captains are skins. It reads <see cref="MatchScene"/>, <see cref="PlayState"/> and
    /// <see cref="LiveFieldState"/>, and asks the flow for seats, menus and at-bat tells through <see cref="IActorHost"/>.
    /// </summary>
    public sealed class ActorDirector
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly LiveFieldState _live;
        readonly InPlayDirector _inPlay;
        readonly IActorHost _host;
        readonly Transform _root;
        /// <summary>The heroes drawn this frame; the rest are hidden.</summary>
        readonly HashSet<string> _used = new HashSet<string>();

        internal ActorDirector(MatchScene scene, PlayState play, LiveFieldState live, InPlayDirector inPlay, IActorHost host, Transform root)
        {
            _scene = scene; _play = play; _live = live; _inPlay = inPlay; _host = host; _root = root;
        }

        float LiveTime => _play.Match != null ? (float)_play.Match.LivePlay.ElapsedSeconds : 0f;

        public void Draw(float dt)
        {
            _host.Steal.UpdateInset(_play.Match != null && !_play.Match.Paused && !_host.TutorialModal && !_host.Turntable
                && (_play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight), _play.Match, _scene.Content, _scene.Feel);
            if (_host.Turntable) return;
            _host.Juice.Age(dt);
            _used.Clear();
            if (_play.Phase is MatchDirector.Phase.Title or MatchDirector.Phase.Select)
            {
                _scene.Chem?.Hide();
                if (_play.Phase == MatchDirector.Phase.Title) PlaceSelectRoster();
                else { _scene.Logo?.Hide(); _scene.Card?.Hide(); }
                foreach (var kv in _scene.Heroes)
                    if (!_used.Contains(kv.Key) && kv.Value != null)
                        kv.Value.gameObject.SetActive(false);
                _scene.Park.Ball.Hide();
                _scene.Zone.Hide();
                _scene.Ring?.Hide();
                return;
            }
            _scene.Card?.Hide();
            _scene.Logo?.Hide();
            if (_play.Phase == MatchDirector.Phase.Field)
            {
                _scene.Chem?.Hide();
                foreach (var kv in _scene.Heroes)
                    if (kv.Value != null)
                        kv.Value.gameObject.SetActive(false);
                _scene.Park.Ball.Hide();
                _scene.Zone.Hide();
                _scene.Ring?.Hide();
                return;
            }
            if (_play.Phase == MatchDirector.Phase.Lineup && _host.Lineup != null)
            {
                PlaceLineupBoard();
                foreach (var kv in _scene.Heroes)
                    if (kv.Value != null)
                        kv.Value.gameObject.SetActive(false);
                _scene.Park.Ball.Hide();
                _scene.Zone.Hide();
                _scene.Ring?.Hide();
                return;
            }
            _scene.Chem?.Hide();
            // A debt the play ended on runs out in the result beat: the diver gets up, the jumper lands, the fumbler recovers.
            if (_play.Phase == MatchDirector.Phase.Result) _live.Owed = _live.Owed.Aged(dt);
            // The result beat draws the play's own bodies (§10.6, #574): the defense that made the play,
            // where each glove stood at Time, whatever the match flipped to after the third out.
            var resultBodies = _play.Phase == MatchDirector.Phase.Result ? _live.ResultBodies : null;
            IReadOnlyDictionary<string, Character> defense = resultBodies != null
                ? ResultDefense(resultBodies)
                : _play.Match.DefenseMap;
            var litId = "";
            if ((_play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow) && defense.TryGetValue(_live.GlovePos, out var litWho))
                litId = litWho.Id;
            var itemLit = _host.Toss.Offered && _host.Toss.Target != null ? _host.Toss.Target.Id : "";
            var boxBatter = _play.Phase == MatchDirector.Phase.Result && _play.Last != null
                ? PlayStamp.BoxBatter(_play.Last, _play.Match)
                : _play.Match.Batter;
            // The bunt tell (§7.3): while the batter is squared through SET and the pitch, the corners crash and the
            // middle covers, drawn from the function the live ball seeds from at contact (BuntDefense.Spots).
            var squareSpots = _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight && (_play.SquareSec > 0f)
                ? BuntDefense.Spots(defense, _play.SquareSec, _play.Match.Rules)
                : null;
            var squareWas = squareSpots != null
                ? BuntDefense.Spots(defense, Mathf.Max(0f, _play.SquareSec - dt), _play.Match.Rules)
                : null;
            foreach (var kv in defense)
            {
                var who = kv.Value;
                if (_play.Phase == MatchDirector.Phase.Result && boxBatter != null && who.Id == boxBatter.Id)
                    continue;
                var pos = OutfieldStarts.Of(_play.Match.Park, _play.Match.Rules)[kv.Key]; // the park's starts (F2-d)
                double x = pos.X, z = pos.Z;
                if (_live.GloveAt.TryGetValue(kv.Key, out var live))
                {
                    x = live.X;
                    z = live.Z;
                }
                var crashing = false;
                if (squareSpots != null && squareSpots.TryGetValue(kv.Key, out var squareAt))
                {
                    x = squareAt.X;
                    z = squareAt.Z;
                    crashing = squareWas != null && squareWas.TryGetValue(kv.Key, out var was)
                               && Diamond.Dist(was.X, was.Z, squareAt.X, squareAt.Z) > 1e-3;
                }
                var pose = Motion.Verb.Idle;
                var buddyPartner = _play.Phase == MatchDirector.Phase.InPlay && _inPlay.BuddySet && _play.Preview.Buddy != null && who.Id == _play.Preview.Buddy.Id;
                var highlighted = (_play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow) && (who.Id == itemLit || who.Id == litId || (buddyPartner && !_live.Buddy));
                if (highlighted && !buddyPartner)
                {
                    x = _live.GloveX;
                    z = _live.GloveZ;
                    if (_live.Throwing) pose = Motion.Verb.Catch;
                    // The shipped knockback lays the body down; the ordinary impact recoil (#720) is a brace on the take's own pose.
                    else if (_live.RecoilT > 0 && !_live.Owed.Bracing) pose = Motion.Verb.Dive;
                    else if (_live.JumpT > 0) pose = who.FieldAbility == FieldAbilityId.Clamber ? Motion.Verb.Clamber : Motion.Verb.Jump;
                    else if ((_live.Caught || _live.Buddy) && !_live.Throwing && CarryingOnTheStick(kv.Key))
                        pose = Motion.Verb.Run;
                    else if (_live.Caught && _play.Preview != null && _play.Preview.Grounder) pose = Motion.Verb.Scoop;
                    else if (_live.Caught || _live.Buddy) pose = Motion.Verb.Catch;
                    else if (_live.DiveT > 0) pose = Motion.Verb.Dive;
                    else if (_play.Preview != null && _play.Path != null)
                    {
                        var fromX = x;
                        var fromZ = z;
                        if (_scene.Heroes.TryGetValue(who.Id, out var moving) && moving != null)
                        {
                            fromX = moving.transform.position.x;
                            fromZ = moving.transform.position.z;
                        }
                        var speed = FieldingResolver.ChaseSpeedFt(who, _live.GlovePos, _play.Preview, _play.Match.Rules);
                        var route = FieldingPursuit.Plan(
                            _play.Preview, _play.Match.Park, _play.Path,
                            LiveTime, fromX, fromZ, speed, _play.Match.Rules, cutOff: !FieldingResolver.IsOutfield(_live.GlovePos));
                        if (CartoonJuice.ChaseIsARun(
                                _live.Caught || _live.Buddy,
                                Diamond.Dist(fromX, fromZ, route.X, route.Z)))
                            pose = Motion.Verb.Run;
                        else
                            pose = FieldPose(who, _play.Preview, false);
                    }
                    else pose = Motion.Verb.Field;
                }
                else if (buddyPartner)
                {
                    var atWall = Diamond.Dist(x, z, _inPlay.WallPlant(_play.Preview).X, _inPlay.WallPlant(_play.Preview).Z) < 18;
                    if (_live.Throwing) pose = Motion.Verb.Field;
                    else if (atWall) pose = Motion.Verb.Crouch;
                    else pose = Motion.Verb.Field;
                }
                else if (_play.Phase == MatchDirector.Phase.InPlay && _play.Preview != null && who.Id == _play.Preview.Fielder.Id)
                {
                    if (_live.Buddy && _live.JumpT > 0)
                        pose = who.FieldAbility == FieldAbilityId.Clamber ? Motion.Verb.Clamber : Motion.Verb.Jump;
                    else
                        pose = FieldPose(who, _play.Preview, _live.Caught || _live.Buddy);
                }
                else if ((_play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow) && Diamond.Dist(x, z, pos.X, pos.Z) > 6)
                    pose = Motion.Verb.Run;
                else if (crashing)
                    pose = Motion.Verb.Run;
                else if (squareSpots != null && Diamond.Dist(x, z, pos.X, pos.Z) > 6)
                    pose = Motion.Verb.Field;
                if (kv.Key == "P" && _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight)
                    pose = _play.Phase == MatchDirector.Phase.Flight ? Motion.Verb.ThrowPitch : Motion.Verb.ChargePitch;
                if (kv.Key == "C" && _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight)
                    pose = Motion.Verb.Crouch;
                if (_live.Throwing && kv.Key == _live.ThrowFromPos)
                    pose = Motion.Verb.Throw;
                if (_live.Throwing && !string.IsNullOrEmpty(_live.CoverPos) && kv.Key == _live.CoverPos)
                    pose = Motion.Verb.Catch;
                // A body paying for the ball shows it whoever holds the ring (#719–#721): the fumbler's stun (never the
                // batter's miss, which carries the bat), the diver down then up, the jumper reaching while the root rises.
                if (_play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow or MatchDirector.Phase.Result
                    && FielderTells.Verb(_live.Owed, kv.Key, _scene.Feel.FieldTells) is { } owedVerb)
                    pose = owedVerb;
                var hero = Hero(who);
                var holdBall = _live.Caught || _live.Buddy;
                hero.SetGrow(BodyScale.GrowOn(who.FieldAbility, playGlove: who.Id == litId, holdBall: holdBall));
                hero.SetHighlight(highlighted);
                hero.SetYou((_play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow) && who.Id == litId && _host.HumanOwnsThrow);
                hero.SetHint((_play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow) && kv.Key == _live.SwitchPos && kv.Key != _live.GlovePos && !(_live.Caught || _live.Buddy));
                if (_play.Pending != null && _play.Pending.StarSwingUsed == "heart-swing" && highlighted)
                    pose = Motion.Verb.Charm;
                var pType = _host.ShownPitchType;
                var motionCharge = pose == Motion.Verb.ThrowPitch && _play.Pitch != null
                    ? (float)_play.Pitch.Charge01 : _host.PitchCharge;
                hero.SetPose(pose, kv.Key == "P" ? motionCharge : 0, kv.Key == "P" ? pType : null);
                hero.SetChargeRing(kv.Key == "P" && (_play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight) && _host.HumanPitches ? _host.PitchCharge : 0f);
                hero.SetGear(_play.Match.OffenseBat, _play.Match.DefenseGlove);
                hero.SetHeld(false, true);
                var brace = FielderTells.Brace(_live.Owed, kv.Key, _play.Match.Rules.Fielding.Recoil.CapSec, _scene.Feel.FieldTells);
                // Juice by weight (CH-13): the thrower's load before the release, the catcher's settle after the glove.
                var toRelease = _live.Throwing && kv.Key == _live.ThrowFromPos ? _play.Match.LivePlay.ThrowReleaseSec - _live.ThrowT : double.NaN;
                hero.SetBrace(Vector3.Scale(new Vector3((float)brace.X, (float)brace.Y, (float)brace.Z), _host.Juice.Wrapper(who, toRelease, _scene.Feel)));
                if (kv.Key == "P" && _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight)
                    // The drawn rubber, not the match's: a hand's is the match's exactly, and the
                    // CPU's walks to it over SET instead of teleporting on the release frame (§4.8).
                    x += _play.MoundX * HomeSet.PitcherWalk;
                // The normal jump's root rise is the sim's (#719): two feet over the airtime, the ring left on the dirt.
                var rise = (float)FielderTells.RiseFt(_live.Owed, kv.Key);
                hero.Place(new Vector3((float)x, ParkDiamond.StandY(x, z, DiamondGeometry.Of(_play.Match.Rules)) + rise, (float)z),
                    DefenseFacing(kv.Key, x, z, highlighted && !buddyPartner));
                if (pose == Motion.Verb.ThrowPitch && _play.Phase == MatchDirector.Phase.Flight)
                    hero.SampleMotion((float)Motion.PitchRelease + _play.Flight, dt);
                else if (pose == Motion.Verb.Throw && _live.Throwing && kv.Key == _live.ThrowFromPos)
                    hero.SampleMotion((float)StealPresentation.ThrowSample(_live.ThrowT, _play.Match.LivePlay.ThrowReleaseSec), dt);
                else hero.Tick(dt);
            }

            var batter = boxBatter;
            if (batter != null)
            {
                var bHero = Hero(batter);
                var racing = _play.Phase == MatchDirector.Phase.InPlay && _play.Pending != null;
                var committedSwing = _play.Swung && _play.Swing != null && _play.Swing.Swing && !_play.Swing.Bunt;
                if (committedSwing)
                {
                    // A swing staged without a press (a gate, a replay) reads its warp here, once.
                    if (float.IsNaN(_play.SwingContactSec)) _play.SwingContactSec = _host.SwingContactSec(_play.Swing);
                    _play.CommittedSwingT = (float)AtBatMotion.AdvanceCommittedSwing(
                        _play.CommittedSwingT,
                        _play.Flight,
                        AtBatMotion.SwingStart(_play.PitchDur, _play.Swing.TimingErrorFrames, rules: _play.Match.Rules),
                        dt,
                        AtBatMotion.SwingTakeSeconds(_play.SwingContactSec));
                }
                else
                {
                    _play.CommittedSwingT = (float)AtBatMotion.SwingNotStarted;
                    _play.SwingContactSec = float.NaN;
                }
                var swingTakeSec = AtBatMotion.SwingTakeSeconds(
                    float.IsNaN(_play.SwingContactSec) ? Motion.SwingContact : _play.SwingContactSec);
                // The held finish (#583): a dead ball holds it until SET, contact until the runner's first step.
                var swingContact = _play.Pending != null
                    || (_play.Last?.AtBat != null && _play.Last.AtBat.Quality != ContactQuality.Miss);
                var runnerFromBoxFt = 0.0;
                if (committedSwing && swingContact)
                {
                    var runner = _play.Match.BatterRunner;
                    var boxX = HomeSet.BatterBodyX(batter.Bats, _play.Match.BatterContactOffsetX);
                    runnerFromBoxFt = racing && runner != null
                        ? Math.Sqrt((runner.Position.X - boxX) * (runner.Position.X - boxX)
                            + (runner.Position.Z - HomeSet.BatterZ) * (runner.Position.Z - HomeSet.BatterZ))
                        : double.PositiveInfinity;
                }
                var presentingSwing = committedSwing
                    && AtBatMotion.PresentsSwing(_play.CommittedSwingT, swingTakeSec, swingContact,
                        runnerFromBoxFt, _scene.Feel.SwingFinishStepFt);
                var bPose = presentingSwing
                    ? Motion.Verb.Swing
                    : racing ? Motion.Verb.Run : BatterPose();
                // Use the committed charge after release, including CPU swings.
                var swingCharge = bPose == Motion.Verb.Swing && _play.Swing != null
                    ? (float)_play.Swing.Charge01
                    : bPose == Motion.Verb.LetGo ? _host.Batter.LetGoCharge
                    : _host.HumanBats ? _play.Charge : 0f;
                bHero.SetBuntSide(_host.Batter.ShowingSide);
                bHero.SetPose(bPose, swingCharge);
                if (presentingSwing) bHero.SetSwingContact(_play.SwingContactSec);
                bHero.SetChargeRing((_play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight) && _host.HumanBats && _host.PlateSwingArmed
                    ? _play.Charge : 0f);
                bHero.SetGear(_play.Match.OffenseBat, _play.Match.DefenseGlove);
                var batting = bPose is Motion.Verb.ChargeSwing or Motion.Verb.Swing
                    or Motion.Verb.CheckSwing or Motion.Verb.Bunt or Motion.Verb.Miss or Motion.Verb.LetGo;
                bHero.SetHeld(batting, false);
                bHero.SetHighlight(false);
                if (racing)
                {
                    _host.OnRun();
                    // The batter-runner is a body in the sim (spec §9.1): drawn where it stands, off a bag it shares and does not hold.
                    var body = _play.Match.BatterRunner;
                    var (hx, hz) = body != null ? body.DrawPosition(_scene.Feel.RunnerShareStepFt) : (HomeSet.BatterBodyX(batter.Bats, _play.Match.BatterContactOffsetX), HomeSet.BatterZ);
                    var diamond = DiamondGeometry.Of(_play.Match.Rules);
                    var next = body != null ? diamond.Bag(Math.Min(body.NextBag, 3)) : diamond.First;
                    var look = presentingSwing
                        ? (X: 0.0, Z: 1.0)
                        : (X: next.X - hx, Z: next.Z - hz);
                    if (body != null && body.Sliding) bPose = Motion.Verb.Slide;
                    else if (body != null && (body.Held || body.OnBag) && !presentingSwing) bPose = Motion.Verb.Idle;
                    bHero.SetPose(bPose, swingCharge);
                    bHero.Place(new Vector3((float)hx, 0, (float)hz), new Vector3((float)look.X, 0, (float)look.Z), pinned: presentingSwing);
                }
                else
                    bHero.Place(new Vector3(
                        (float)HomeSet.BatterBodyX(batter.Bats, _play.Match.BatterOffsetX),
                        0,
                        (float)HomeSet.BatterZ), new Vector3(0, 0, 1), pinned: true);
                // Juice by weight (CH-13): the load before the swing's contact mark, the settle after the contact.
                var toContact = presentingSwing && !float.IsNaN(_play.SwingContactSec) ? _play.SwingContactSec - _play.CommittedSwingT : double.NaN;
                bHero.SetBrace(_host.Juice.Wrapper(batter, toContact, _scene.Feel));
                if (bPose == Motion.Verb.Swing && presentingSwing)
                    bHero.SampleMotion((float)AtBatMotion.CommittedSwingSample(_play.CommittedSwingT, swingTakeSec), dt);
                else bHero.Tick(dt);
            }

            if (resultBodies != null)
            {
                foreach (var b in resultBodies)
                    if (b.IsRunner && (boxBatter == null || b.Who.Id != boxBatter.Id))
                        PlaceBodyAtTime(b);
            }
            else
            {
                foreach (var runner in _play.Match.Runners)
                    if (!runner.IsBatter && !runner.Out) PlaceRunner(runner);
            }

            foreach (var kv in _scene.Heroes)
                if (!_used.Contains(kv.Key) && kv.Value != null)
                    kv.Value.gameObject.SetActive(false);

            var starPitch = _play.Pitch != null && _play.Pitch.Star ? _play.Match.Pitcher.StarPitch : _scene.Fx.ActivePitch;
            var starSwing = _play.Pending != null ? _play.Pending.StarSwingUsed
                : _play.Last != null ? _play.Last.AtBat.StarSwingUsed : null;
            // The ball is tinted by family only once it is out of the hand (PH-02-R5): in SET, and
            // through the windup, it is the fastball's colour whatever was selected (BallView:290).
            var ptype = _play.PitchAir && _play.Pitch != null ? _play.Pitch.Type : PitchFamily.Fastball;
            var heat = _play.Last != null && _play.Last.Heatball;
            if ((_live.Caught || _live.Buddy) && !_live.Throwing && _play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow)
                _host.HoldBallInGlove();
            if (_live.Throwing && _play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow) StealDirector.HoldPreparingThrow(_play.Match, _live.ThrowFromPos, _scene.Heroes, _scene.Park);
            var inFlight = _play.Phase is MatchDirector.Phase.Flight or MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow;
            var inPlay = _play.Phase is MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow;
            if (_host.Replaying || inFlight || _play.Phase is MatchDirector.Phase.Set || _scene.Fx.Active)
                _scene.Park.Ball.Place(_play.Ball, starPitch, ptype, heat, inFlight, inPlay);
            else
                _scene.Park.Ball.Hide();
            if (_live.Throwing && _live.ArmedThrow != null)
                _scene.Park.Ball.SetTrailColor(SpecialFx.ThrowColor(_live.ArmedThrow.Relation));

            var setOrFlight = _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight;
            if (SetTells.ZoneOn(setOrFlight)) _host.ShowCursor();
            else _scene.Zone.Hide();
            _scene.Park.Ball.EmitTrail(SetTells.TrailOn(_play.Phase is MatchDirector.Phase.Flight or MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow));

            var burn = starSwing == "furnace" || starSwing == "heat-swing";
            var frags = starSwing == "cask-swing" || starSwing == "shell-swing";
            var swingAt = Vector3.zero;
            if (!string.IsNullOrEmpty(starSwing) && _play.Match?.Batter != null
                && _scene.Heroes.TryGetValue(_play.Match.Batter.Id, out var bat) && bat != null)
                swingAt = bat.transform.position + Vector3.up * 3.2f;
            _scene.Fx.Tick(dt, _play.Ball, _play.Phase == MatchDirector.Phase.Flight, _play.Phase == MatchDirector.Phase.InPlay,
                _play.Pitch != null && _play.Pitch.Star, starPitch, starSwing ?? "", burn, frags, swingAt);
            PlaceTwin(starPitch);
            var flash = _play.Phase == MatchDirector.Phase.InPlay && _inPlay.BuddySet && !_live.Buddy && !_live.Throwing;
            var flashAt = Vector3.zero;
            if (flash && !string.IsNullOrEmpty(_live.BuddyPos) && _live.GloveAt.TryGetValue(_live.BuddyPos, out var planted))
                flashAt = new Vector3((float)planted.X, 0f, (float)planted.Z);
            _scene.Fx.BuddyTell(flash, flashAt, _live.BuddyWindow);
            _host.Toss.Present(dt);
        }

        Motion.Verb BatterPose()
        {
            if (_play.Phase == MatchDirector.Phase.Result && _play.Last != null)
            {
                if (_play.Last.Kind == PlayKind.SwingMiss) return Motion.Verb.Miss;
                if (_play.Last.Kind == PlayKind.Strikeout)
                    return _play.Swing != null && _play.Swing.Swing ? Motion.Verb.Miss : Motion.Verb.Idle;
                if (_play.Last.Kind == PlayKind.HomeRun) return Motion.Verb.Cheer;
                if (_play.Swing != null && _play.Swing.Bunt) return Motion.Verb.Bunt;
                return Motion.Verb.Idle;
            }
            if (_play.Phase == MatchDirector.Phase.GameOver)
                return _play.Match.HomeScore >= _play.Match.AwayScore ? Motion.Verb.Cheer : Motion.Verb.Idle;
            if (_play.Phase == MatchDirector.Phase.Flight && _play.Swung)
            {
                if (_play.Swing != null && _play.Swing.Bunt) return Motion.Verb.Bunt;
                return Motion.Verb.Swing;
            }
            // (_play.SquareSec > 0f) (§5.8, §7.3): the bat is on the plane before the pitch — the tell the defense and the pitcher read.
            if (_play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight) return _host.Batter.SquaredNow ? Motion.Verb.Bunt : _host.Batter.LettingGo ? Motion.Verb.LetGo : Motion.Verb.ChargeSwing;
            return Motion.Verb.Idle;
        }

        /// <summary>
        /// Which way a glove faces (§8.2, #611): the run while moving, the ball when planted, the throw target on
        /// release, the backpedal in the last feet under a fly. Before the ball is live every glove faces home.
        /// </summary>
        BodyFacing.Facts DefenseFacing(string pos, double x, double z, bool onBall)
        {
            if (_play.Phase is not (MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow))
                return pos == "P"
                    ? new BodyFacing.Facts(0, -1, Pinned: true)
                    : new BodyFacing.Facts(-x, -z + 8, Pinned: true);
            var releasing = _live.Throwing && pos == _live.ThrowFromPos;
            var fly = onBall && _play.Preview != null && !(_live.Caught || _live.Buddy)
                      && FieldingResolver.InAir(_play.Preview, _play.Ball.y, LiveTime, _play.Match.Rules, _play.Preview.HangTimeSec);
            var plant = fly ? FlyCatch.ChaseTarget(_play.Preview, _play.Match.Rules, _play.Match.Park) : default;
            return BodyFacing.Fielder(x, z, _play.Ball.x, _play.Ball.z, releasing, _live.ThrowTo.x, _live.ThrowTo.z,
                fly, plant.X, plant.Z, BodyFacing.Rates.Of(_scene.Content.Feel));
        }

        /// <summary>
        /// The glove runs with the ball on the stick: the sim's own owner on the calibrated stick (#718), the Manhattan gate it
        /// shipped with otherwise. Never while the brace holds the body (#720): the skid is not a run.
        /// </summary>
        bool CarryingOnTheStick(string pos)
        {
            if (FielderTells.Braced(_live.Owed, pos)) return false;
            if (_play.Match != null) return _play.Match.LivePlay.PursuitManual;
            var stick = _host.FieldStick;
            return Mathf.Abs(stick.x) + Mathf.Abs(stick.y) >= (float)_scene.Feel.FieldAssistStick;
        }

        static Motion.Verb FieldPose(Character who, FieldingPreview pre, bool caught)
        {
            if (caught) return pre.Grounder ? Motion.Verb.Scoop : Motion.Verb.Catch;
            var a = who.FieldAbility;
            if (a == FieldAbilityId.Dive && pre.Grounder) return Motion.Verb.Dive;
            if (a == FieldAbilityId.Burrow && pre.Grounder) return Motion.Verb.Dive;
            if (a == FieldAbilityId.SuperJump && pre.HomeRunLikely) return Motion.Verb.Jump;
            if (a == FieldAbilityId.Clamber && pre.HomeRunLikely) return Motion.Verb.Clamber;
            return Motion.Verb.Field;
        }

        static Dictionary<string, Character> ResultDefense(IReadOnlyList<FieldBody> bodies)
        {
            var map = new Dictionary<string, Character>();
            foreach (var b in bodies)
                if (!b.IsRunner) map[b.Pos] = b.Who;
            return map;
        }

        /// <summary>A live runner where the play left them (§10.6): on the bag they hold, or on the path at the third out.</summary>
        /// <summary>
        /// A star pitch's twin (spec §13), while the pitch is in the air: the sim's offset from the real ball, laid on the ball
        /// as drawn, at the sim's strength. Every pitch without a twin, and every frame after it fades, hides it.
        /// </summary>
        void PlaceTwin(string starPitch)
        {
            var pitch = _play.Pitch;
            var m = _play.Match;
            if (_play.Phase != MatchDirector.Phase.Flight || pitch == null || !pitch.Star || m == null || _play.PitchDur <= 0)
            {
                _scene.Fx.Twin(null, 0, 0);
                return;
            }
            var u = Mathf.Clamp01(_play.Flight / _play.PitchDur);
            var from = ((double)_play.ReleaseFrom.x, (double)_play.ReleaseFrom.y, (double)_play.ReleaseFrom.z);
            var twin = PitchFlight.Twin(pitch, u, m.Rules, starPitch, m.Content.StarSkills, from);
            if (twin is not { } t)
            {
                _scene.Fx.Twin(null, 0, 0);
                return;
            }
            var real = PitchFlight.Point(pitch, u, m.Rules, starPitch, from, m.Content.StarSkills);
            var at = _play.Ball + new Vector3((float)(t.X - real.X), (float)(t.Y - real.Y), (float)(t.Z - real.Z));
            _scene.Fx.Twin(at, (float)t.Alpha, ToyMesh.BallViewScale(true, at.z));
        }

        void PlaceBodyAtTime(FieldBody b)
        {
            var h = Hero(b.Who);
            h.SetPose(Motion.Verb.Idle);
            h.SetGear(_play.Match.OffenseBat, _play.Match.DefenseGlove);
            h.SetHeld(false, false);
            h.SetHighlight(false);
            var rubber = DiamondGeometry.Of(_play.Match.Rules).Rubber;
            h.Place(new Vector3((float)b.X, 0, (float)b.Z),
                new Vector3((float)(rubber.X - b.X), 0, (float)(rubber.Z - b.Z)));
            h.Tick(Time.deltaTime);
        }

        void PlaceRunner(Runner state)
        {
            var who = state.Who;
            var bagNum = state.FromBag;
            var diamond = DiamondGeometry.Of(_play.Match.Rules);
            var bag = diamond.Bag(bagNum);
            var live = _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight or MatchDirector.Phase.InPlay or MatchDirector.Phase.StealThrow;
            // Every phase draws the same body; animation never reconstructs steal distance. A body on a bag another runner
            // holds (§9.1) stands data/feel runnerShareStepFt off it so the two never merge.
            var spot = state != null && live ? state.DrawPosition(_scene.Feel.RunnerShareStepFt) : bag;
            var next = diamond.Bag(bagNum >= 3 ? 4 : bagNum + 1);
            var h = Hero(who);
            var pose = Motion.Verb.Idle;
            if (state != null && live)
            {
                next = diamond.Bag(state.DestBag >= state.Bag + 1 ? Math.Min(state.Bag + 1, 4) : state.Bag);
                if (state.Phase == RunnerPhase.Returning) next = diamond.Bag(state.Bag);
                pose = state.Sliding ? Motion.Verb.Slide
                    : state.Moving && !state.Held ? Motion.Verb.Run
                    : Motion.Verb.Idle;
            }
            h.SetPose(pose);
            h.SetGear(_play.Match.OffenseBat, _play.Match.DefenseGlove);
            h.SetHeld(false, false);
            var selected = _play.Match.SelectedRunner ?? _play.Match.LeadRunner;
            h.SetHighlight(_host.HumanBats && selected != null && who.Id == selected.Id && _play.Phase is MatchDirector.Phase.Set or MatchDirector.Phase.Flight);
            h.Place(new Vector3((float)spot.X, 0, (float)spot.Z),
                new Vector3((float)(next.X - spot.X), 0, (float)(next.Z - spot.Z)));
            h.Tick(Time.deltaTime);
        }

        void PlaceSelectRoster()
        {
            var ids = _scene.Content.CaptainIds;
            var pick = _play.Phase == MatchDirector.Phase.Select;
            if (!CarnivalFront.TitlePlacesBody(pick))
            {
                // Title is wordmark + dirt. Unused heroes go inactive in DrawActors (#685).
                _scene.Card?.Hide();
                if (_scene.Logo == null) _scene.Logo = LogoToy.Attach(_root);
                var titleShot = _scene.Content.Shots.Must("title");
                _scene.Logo.Show(
                    CarnivalFront.Logo,
                    new Vector3(CarnivalFront.LogoX, CarnivalFront.LogoY, CarnivalFront.LogoZ),
                    new Vector3((float)titleShot.Pos.X, (float)titleShot.Pos.Y, (float)titleShot.Pos.Z));
                return;
            }
            for (var i = 0; i < ids.Count; i++)
            {
                var who = _scene.Content.Must(ids[i]);
                var hero = Hero(who);
                var yours = ids[i] == _host.CurrentPick().Yours;
                var theirs = ids[i] == _host.CurrentPick().Theirs;
                var spot = CarnivalFront.CaptainSpot(i, ids.Count, pick, yours);
                hero.SetPose(CarnivalFront.SelectPose(yours, theirs));
                hero.SetHighlight(yours);
                hero.SetGrow(false); // Grow is a field verb. Menu 1.71x at Z=4 is Ashlord's hat.
                hero.SetHeld(false, false);
                hero.SetGear(_play.Match.OffenseBat, _play.Match.DefenseGlove);
                hero.Place(
                    new Vector3(spot.X, CarnivalFront.SelectDirtY, spot.Z),
                    new Vector3(0f, 0f, -1f), pinned: true);
                hero.Tick(Time.deltaTime);
            }
            _scene.Logo?.Hide();
            // HUD is the select card. World placard covered the toys (#354).
            _scene.Card?.Hide();
        }

        void PlaceLineupBoard()
        {
            TeamSheet.Place(_host.Lineup, _root, _scene.Chem, _scene.Card);
        }

        HeroActor Hero(Character who)
        {
            _used.Add(who.Id);
            if (!_scene.Heroes.TryGetValue(who.Id, out var h) || h == null)
            {
                var go = new GameObject("Hero-" + who.Id);
                h = go.AddComponent<HeroActor>();
                _scene.Heroes[who.Id] = h;
            }
            h.gameObject.SetActive(true);
            h.Bind(who, DiamondGeometry.Of(_play.Match.Rules));
            h.SetFacing(BodyFacing.Rates.Of(_scene.Content.Feel));
            return h;
        }
    }

    /// <summary>What drawing the bodies asks of the match flow: seats and menus, the at-bat tells and the item offer.</summary>
    internal interface IActorHost
    {
        bool TutorialModal { get; }
        bool Turntable { get; }
        bool Replaying { get; }
        bool HumanBats { get; }
        bool HumanPitches { get; }
        bool HumanOwnsThrow { get; }
        /// <summary>What the batter's body shows at the plate: the square and its side, and a let-go.</summary>
        IBatterTells Batter { get; }
        bool PlateSwingArmed { get; }
        float PitchCharge { get; }
        string ShownPitchType { get; }
        ItemToss Toss { get; }
        float SwingContactSec(SwingCommand swing);
        void ShowCursor();
        void HoldBallInGlove();
        /// <summary>The batter-runner's first step, for a training drill.</summary>
        void OnRun();
        StealDirector Steal { get; }
        JuiceDirector Juice { get; }
        LineupScreens Lineup { get; }
        ExhibitionPick CurrentPick();
        /// <summary>The fielding pad's stick, for the shipped carry gate.</summary>
        Vector2 FieldStick { get; }
    }
}
