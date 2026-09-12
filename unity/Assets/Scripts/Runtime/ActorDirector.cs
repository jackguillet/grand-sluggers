using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Applies bones/clips from gameplay events. Captains are skins.</summary>
    public sealed class ActorDirector
    {
        readonly MatchDirector _play;
        public ActorDirector(MatchDirector play) { _play = play; }
        public void Draw(float dt) { _play.DrawBodies(dt); }
    }

    public sealed partial class MatchDirector
    {
        float _committedSwingT = (float)AtBatMotion.SwingNotStarted;

        internal void DrawBodies(float dt) => DrawActors(dt);

        void DrawActors(float dt)
        {
            if (_turntable) return;
            _used.Clear();
            if (_phase is Phase.Title or Phase.Select)
            {
                TeamSheet.HideBoard();
                _chem?.Hide();
                PlaceSelectRoster();
                foreach (var kv in _heroes)
                    if (!_used.Contains(kv.Key) && kv.Value != null)
                        kv.Value.gameObject.SetActive(false);
                _park.Ball.Hide();
                _zone.Hide();
                _ring?.Hide();
                return;
            }
            _card?.Hide();
            _logo?.Hide();
            if (_phase == Phase.Field)
            {
                TeamSheet.HideBoard();
                _chem?.Hide();
                foreach (var kv in _heroes)
                    if (kv.Value != null)
                        kv.Value.gameObject.SetActive(false);
                _park.Ball.Hide();
                _zone.Hide();
                _ring?.Hide();
                return;
            }
            if (_phase == Phase.Lineup && _lineup != null)
            {
                PlaceLineupBoard();
                foreach (var kv in _heroes)
                    if (kv.Value != null)
                        kv.Value.gameObject.SetActive(false);
                _park.Ball.Hide();
                _zone.Hide();
                _ring?.Hide();
                return;
            }
            TeamSheet.HideBoard();
            _chem?.Hide();
            var defense = FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
            var litId = "";
            if ((_phase is Phase.InPlay or Phase.StealThrow) && defense.TryGetValue(_glovePos, out var litWho))
                litId = litWho.Id;
            var itemLit = ItemOffered && _itemTarget != null ? _itemTarget.Id : "";
            var boxBatter = _phase == Phase.Result && _last != null
                ? PlayStamp.BoxBatter(_last, _match)
                : _match.Batter;
            foreach (var kv in defense)
            {
                var who = kv.Value;
                if (_phase == Phase.Result && boxBatter != null && who.Id == boxBatter.Id)
                    continue;
                var pos = Diamond.Positions[kv.Key];
                double x = pos.X, z = pos.Z;
                if (_gloveAt.TryGetValue(kv.Key, out var live))
                {
                    x = live.X;
                    z = live.Z;
                }
                var pose = Motion.Verb.Idle;
                var buddyPartner = _phase == Phase.InPlay && BuddySet && _preview.Buddy != null && who.Id == _preview.Buddy.Id;
                var highlighted = (_phase is Phase.InPlay or Phase.StealThrow) && (who.Id == itemLit || who.Id == litId || (buddyPartner && !_buddy));
                if (highlighted && !buddyPartner)
                {
                    x = _fx;
                    z = _fz;
                    if (_throwing) pose = Motion.Verb.Catch;
                    else if (_bobbling) pose = Motion.Verb.Miss;
                    else if (_recoilT > 0) pose = Motion.Verb.Dive;
                    else if (_jumpT > 0) pose = who.FieldAbility == "clamber" ? Motion.Verb.Clamber : Motion.Verb.Jump;
                    else if ((_caught || _buddy) && !_throwing &&
                             Mathf.Abs(FieldPad.StickX) + Mathf.Abs(FieldPad.StickY) >= 0.35f)
                        pose = Motion.Verb.Run;
                    else if (_caught && _preview != null && _preview.Grounder) pose = Motion.Verb.Scoop;
                    else if (_caught || _buddy) pose = Motion.Verb.Catch;
                    else if (_diveT > 0) pose = Motion.Verb.Dive;
                    else if (_preview != null && _path != null)
                    {
                        var fromX = x;
                        var fromZ = z;
                        if (_heroes.TryGetValue(who.Id, out var moving) && moving != null)
                        {
                            fromX = moving.transform.position.x;
                            fromZ = moving.transform.position.z;
                        }
                        var speed = FieldingResolver.ChaseSpeedFt(who, _preview.Frozen);
                        var route = FieldingPursuit.Plan(
                            _preview, _match.Park, _path,
                            LiveTime, fromX, fromZ, speed);
                        if (CartoonJuice.ChaseIsARun(
                                _caught || _buddy,
                                Diamond.Dist(fromX, fromZ, route.X, route.Z)))
                            pose = Motion.Verb.Run;
                        else
                            pose = FieldPose(who, _preview, false);
                    }
                    else pose = Motion.Verb.Field;
                }
                else if (buddyPartner)
                {
                    var atWall = Diamond.Dist(x, z, WallPlant(_preview).X, WallPlant(_preview).Z) < 18;
                    if (_throwing) pose = Motion.Verb.Field;
                    else if (atWall) pose = Motion.Verb.Crouch;
                    else pose = Motion.Verb.Field;
                }
                else if (_phase == Phase.InPlay && _preview != null && who.Id == _preview.Fielder.Id)
                {
                    if (_buddy && _jumpT > 0)
                        pose = who.FieldAbility == "clamber" ? Motion.Verb.Clamber : Motion.Verb.Jump;
                    else
                        pose = FieldPose(who, _preview, _caught || _buddy);
                }
                else if ((_phase is Phase.InPlay or Phase.StealThrow) && Diamond.Dist(x, z, pos.X, pos.Z) > 6)
                    pose = Motion.Verb.Run;
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    pose = _phase == Phase.Flight ? Motion.Verb.ThrowPitch : Motion.Verb.ChargePitch;
                if (kv.Key == "C" && _phase is Phase.Set or Phase.Flight)
                    pose = Motion.Verb.Crouch;
                if (_throwing && kv.Key == _throwFromPos)
                    pose = Motion.Verb.Throw;
                if (_throwing && !string.IsNullOrEmpty(_coverPos) && kv.Key == _coverPos)
                    pose = Motion.Verb.Catch;
                if (_gun && kv.Key == "C" && !_gunThrowFromPitcher) pose = Motion.Verb.Throw;
                if (_gun && kv.Key == "P" && _gunThrowFromPitcher) pose = Motion.Verb.Throw;
                var hero = Hero(who);
                hero.SetGrow(who.FieldAbility == "grow" && highlighted);
                hero.SetHighlight(highlighted);
                hero.SetYou((_phase is Phase.InPlay or Phase.StealThrow) && who.Id == litId && HumanOwnsThrow);
                hero.SetHint((_phase is Phase.InPlay or Phase.StealThrow) && kv.Key == _switchPos && kv.Key != _glovePos && !(_caught || _buddy));
                if (_pending != null && _pending.StarSwingUsed == "heart-swing" && highlighted)
                    pose = Motion.Verb.Charm;
                var pType = ShownPitchType;
                hero.SetPose(pose, kv.Key == "P" ? _pitchCharge : 0, kv.Key == "P" ? pType : null);
                hero.SetChargeRing(kv.Key == "P" && (_phase is Phase.Set or Phase.Flight) && HumanPitches ? _pitchCharge : 0f);
                hero.SetGear(_match.OffenseBat, _match.DefenseGlove);
                hero.SetHeld(false, true);
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    x += _match.PitcherOffsetX * HomeSet.PitcherWalk;
                var look = kv.Key == "P" && _phase is not Phase.InPlay and not Phase.StealThrow
                    ? new Vector3(0, 0, -1)
                    : _phase is Phase.InPlay or Phase.StealThrow
                        ? new Vector3(_ball.x - (float)x, 0, _ball.z - (float)z)
                        : new Vector3((float)-x, 0, (float)-z + 8f);
                if (_throwing && (highlighted || kv.Key == _throwFromPos))
                    look = _throwTo - new Vector3((float)x, 0, (float)z);
                if (_gun && ((kv.Key == "C" && !_gunThrowFromPitcher) || (kv.Key == "P" && _gunThrowFromPitcher)))
                    look = _gunTo - new Vector3((float)x, 0, (float)z);
                hero.Place(new Vector3((float)x, ParkDiamond.StandY(x, z), (float)z), look);
                if (pose == Motion.Verb.ThrowPitch && _phase == Phase.Flight)
                    hero.SampleMotion((float)Motion.PitchRelease + _flight);
                else hero.Tick(dt);
            }

            var batter = boxBatter;
            if (batter != null)
            {
                var bHero = Hero(batter);
                var racing = _phase == Phase.InPlay && _pending != null;
                var committedSwing = _swung && _swing != null && _swing.Swing && !_swing.Bunt;
                if (committedSwing)
                {
                    _committedSwingT = (float)AtBatMotion.AdvanceCommittedSwing(
                        _committedSwingT,
                        _flight,
                        AtBatMotion.SwingStart(_pitchDur, _swing.TimingErrorFrames),
                        dt);
                }
                else
                    _committedSwingT = (float)AtBatMotion.SwingNotStarted;
                var presentingSwing = committedSwing
                    && AtBatMotion.PresentsCommittedSwing(_committedSwingT);
                var bPose = presentingSwing
                    ? Motion.Verb.Swing
                    : racing ? Motion.Verb.Run : BatterPose();
                // Use the committed charge after release, including CPU swings.
                var swingCharge = bPose == Motion.Verb.Swing && _swing != null
                    ? (float)_swing.Charge01
                    : HumanBats ? _charge : 0f;
                bHero.SetPose(bPose, swingCharge);
                bHero.SetChargeRing((_phase is Phase.Set or Phase.Flight) && HumanBats && _swingButton.Armed
                    ? _charge : 0f);
                bHero.SetGear(_match.OffenseBat, _match.DefenseGlove);
                var batting = bPose is Motion.Verb.ChargeSwing or Motion.Verb.Swing
                    or Motion.Verb.CheckSwing or Motion.Verb.Bunt or Motion.Verb.Miss;
                bHero.SetHeld(batting, false);
                bHero.SetHighlight(false);
                if (racing)
                {
                    if (RunPad.SouthDown) _dash01 = Mathf.Min(1f, _dash01 + 0.28f);
                    _match.Dash01 = _dash01;
                    if (TrainingOn) _coach.OnRun(_match);
                    var kind = LiveKind();
                    var dest = InPlay.BatterDestBag(kind);
                    if (dest <= 0) dest = 1;
                    var feet = InPlay.RunFeet(LiveTime, batter, _dash01);
                    var startX = HomeSet.BatterBodyX(batter.Bats, _match.BatterContactOffsetX);
                    var (hx, hz) = InPlay.AlongBases(feet, dest,
                        startX, HomeSet.BatterZ);
                    var look = presentingSwing
                        ? (X: 0.0, Z: 1.0)
                        : dest >= 2 && feet > Diamond.Baseline
                        ? Diamond.Bag(Math.Min(dest, 3))
                        : Diamond.First;
                    bHero.Place(new Vector3((float)hx, 0, (float)hz), new Vector3((float)look.X, 0, (float)look.Z));
                }
                else
                    bHero.Place(new Vector3(
                        (float)HomeSet.BatterBodyX(batter.Bats, _match.BatterOffsetX),
                        0,
                        (float)HomeSet.BatterZ), new Vector3(0, 0, 1));
                if (bPose == Motion.Verb.Swing && presentingSwing)
                    bHero.SampleMotion((float)AtBatMotion.CommittedSwingSample(_committedSwingT));
                else bHero.Tick(dt);
            }

            PlaceRunner(_match.First, Diamond.First, 1);
            PlaceRunner(_match.Second, Diamond.Second, 2);
            PlaceRunner(_match.Third, Diamond.Third, 3);
            PlaceStealRunner();

            foreach (var kv in _heroes)
                if (!_used.Contains(kv.Key) && kv.Value != null)
                    kv.Value.gameObject.SetActive(false);

            var starPitch = _pitch != null && _pitch.Star ? _match.Pitcher.StarPitch : _spec.ActivePitch;
            var starSwing = _pending != null ? _pending.StarSwingUsed
                : _last != null ? _last.AtBat.StarSwingUsed : null;
            var ptype = _pitch != null ? _pitch.Type : "fastball";
            var heat = _last != null && _last.Heatball;
            if ((_caught || _buddy) && !_throwing && _phase is Phase.InPlay or Phase.StealThrow)
                HoldBallInGlove();
            if (_throwing && _armedThrow != null)
                _park.Ball.SetTrailColor(SpecialFx.ThrowColor(_armedThrow.Relation));
            var inFlight = _phase is Phase.Flight or Phase.InPlay or Phase.StealThrow;
            var inPlay = _phase is Phase.InPlay or Phase.StealThrow;
            if (_replaying || inFlight || _phase is Phase.Set || _spec.Active)
                _park.Ball.Place(_ball, starPitch, ptype, heat, inFlight, inPlay);
            else
                _park.Ball.Hide();

            var setOrFlight = _phase is Phase.Set or Phase.Flight;
            if (SetTells.ZoneOn(setOrFlight)) ShowCursor();
            else _zone.Hide();
            _park.Ball.EmitTrail(SetTells.TrailOn(_phase is Phase.Flight or Phase.InPlay or Phase.StealThrow));

            Character fielder = null;
            if ((_phase is Phase.InPlay or Phase.StealThrow) && defense.TryGetValue(_glovePos, out var gloveNow))
                fielder = gloveNow;
            else if (_preview != null) fielder = _preview.Fielder;
            else if (_last != null) fielder = _last.Fielder;
            var from = Vector3.zero;
            if (fielder != null && _heroes.TryGetValue(fielder.Id, out var fh) && fh != null)
                from = fh.transform.position;
            var lick = fielder != null && fielder.FieldAbility == "lick-catch" && _phase == Phase.InPlay;
            var laser = fielder != null && fielder.FieldAbility == "laser" && (_caught || (_last != null && _last.Throw != null));
            var burn = starSwing == "furnace" || starSwing == "heat-swing";
            var frags = starSwing == "cask-swing" || starSwing == "shell-swing";
            var swingAt = Vector3.zero;
            if (!string.IsNullOrEmpty(starSwing) && _match?.Batter != null
                && _heroes.TryGetValue(_match.Batter.Id, out var bat) && bat != null)
                swingAt = bat.transform.position + Vector3.up * 3.2f;
            _spec.Tick(dt, _ball, _phase == Phase.Flight, _phase == Phase.InPlay,
                _pitch != null && _pitch.Star, starPitch, starSwing ?? "", from, _ball, lick, laser, burn, frags, swingAt);
            var flash = _phase == Phase.InPlay && BuddySet && !_buddy && !_throwing;
            var flashAt = Vector3.zero;
            if (flash && !string.IsNullOrEmpty(_buddyPos) && _gloveAt.TryGetValue(_buddyPos, out var planted))
                flashAt = new Vector3((float)planted.X, 0f, (float)planted.Z);
            _spec.BuddyTell(flash, flashAt, _buddyWindow);
            var itemTargetPos = ItemTargetWorld();
            var showThrow = _itemFlying || (_itemThrown && _phase == Phase.InPlay);
            var flyU = !_itemFlying && _itemThrown ? 1f
                : _itemFlying ? Mathf.Clamp01(_itemFly / ItemView.FlySeconds) : 0f;
            _items?.Present(dt, ItemOffered, _itemPick, itemTargetPos, showThrow, _itemId, flyU);
        }

        Motion.Verb BatterPose()
        {
            if (_phase == Phase.Result && _last != null)
            {
                if (_last.Kind == PlayKind.SwingMiss) return Motion.Verb.Miss;
                if (_last.Kind == PlayKind.Strikeout)
                    return _swing != null && _swing.Swing ? Motion.Verb.Miss : Motion.Verb.Idle;
                if (_last.Kind == PlayKind.HomeRun) return Motion.Verb.Cheer;
                if (_swing != null && _swing.Bunt) return Motion.Verb.Bunt;
                return Motion.Verb.Idle;
            }
            if (_phase == Phase.GameOver)
                return _match.HomeScore >= _match.AwayScore ? Motion.Verb.Cheer : Motion.Verb.Idle;
            if (_phase == Phase.Flight && _swung)
            {
                if (_swing != null && _swing.Bunt) return Motion.Verb.Bunt;
                return Motion.Verb.Swing;
            }
            if (_phase is Phase.Set or Phase.Flight) return Motion.Verb.ChargeSwing;
            return Motion.Verb.Idle;
        }

        static Motion.Verb FieldPose(Character who, FieldingPreview pre, bool caught)
        {
            if (caught) return pre.Grounder ? Motion.Verb.Scoop : Motion.Verb.Catch;
            var a = who.FieldAbility;
            if (a == "dive" && pre.Grounder) return Motion.Verb.Dive;
            if (a == "burrow" && pre.Grounder) return Motion.Verb.Dive;
            if (a == "super-jump" && pre.HomeRunLikely) return Motion.Verb.Jump;
            if (a == "clamber" && pre.HomeRunLikely) return Motion.Verb.Clamber;
            if (a == "spin-check") return Motion.Verb.Spin;
            return Motion.Verb.Field;
        }

        void PlaceRunner(Character who, (double X, double Z) bag, int bagNum)
        {
            if (who == null) return;
            if (_phase == Phase.StealThrow && _match.ArmedStealBag == bagNum) return;
            if (_gun && _gunRunner != null && who.Id == _gunRunner.Id) return;
            var state = _match.RunnerAt(bagNum);
            var spot = Diamond.LeadSpot(bagNum, state != null ? state.Lead01 : 0);
            var next = Diamond.Bag(bagNum >= 3 ? 4 : bagNum + 1);
            var h = Hero(who);
            var pose = Motion.Verb.Idle;
            var racing = _phase == Phase.InPlay && _pending != null;
            if (racing)
            {
                var kind = LiveKind();
                var dest = InPlay.OccupiedDestBag(bagNum, kind, _match.SendAll, _caught || _buddy);
                var feet = InPlay.RunFeet(LiveTime, who);
                var at = InPlay.TowardBag(bagNum, dest, feet);
                spot = (at.X, at.Z);
                var tagBag = dest > bagNum ? dest : bagNum + 1;
                var threatened = _throwing && _throwBag == tagBag;
                var going = dest > bagNum && !InPlay.OccupyingBag(at.X, at.Z);
                pose = going ? (threatened ? Motion.Verb.Slide : Motion.Verb.Run) : pose;
            }
            else if (state != null && state.Sliding) pose = Motion.Verb.Slide;
            else if (state != null && state.StealAttempt) pose = Motion.Verb.Run;
            else if (state != null && state.Lead01 > 0.08) pose = Motion.Verb.StealLead;
            h.SetPose(pose);
            h.SetGear(_match.OffenseBat, _match.DefenseGlove);
            h.SetHeld(false, false);
            var selected = _match.SelectedRunner ?? _match.LeadRunner;
            h.SetHighlight(HumanBats && selected != null && who.Id == selected.Id && _phase is Phase.Set or Phase.Flight);
            h.Place(new Vector3((float)spot.X, 0, (float)spot.Z),
                new Vector3((float)(next.X - bag.X), 0, (float)(next.Z - bag.Z)));
            h.Tick(Time.deltaTime);
        }

        void PlaceStealRunner()
        {
            if (_phase == Phase.StealThrow)
            {
                PlaceLiveStealRunner();
                return;
            }
            if (!_gun || _gunRunner == null) return;
            var u = Mathf.Clamp01(_gunT / Mathf.Max(0.05f, _gunDur));
            double x, z;
            if (_gunPickoff)
            {
                var from = Diamond.LeadSpot(_gunFromBag, _gunLead > 0.15 ? _gunLead : 1);
                var to = Diamond.Bag(_gunFromBag);
                x = from.X + (to.X - from.X) * u;
                z = from.Z + (to.Z - from.Z) * u;
            }
            else
            {
                if (!_gunSafe) u *= 0.7f;
                var from = Diamond.Bag(_gunFromBag);
                var to = Diamond.Bag(_gunToBag);
                var t = 0.2 + 0.8 * u;
                x = from.X + (to.X - from.X) * t;
                z = from.Z + (to.Z - from.Z) * t;
            }
            var h = Hero(_gunRunner);
            var pose = u > 0.55f ? Motion.Verb.Slide : Motion.Verb.Run;
            if (!_gunSafe && u > 0.5f) pose = Motion.Verb.Dive;
            h.SetPose(pose);
            h.SetGear(_match.OffenseBat, _match.DefenseGlove);
            h.SetHeld(false, false);
            h.SetHighlight(true);
            var dest = Diamond.Bag(_gunToBag);
            h.Place(new Vector3((float)x, 0, (float)z), new Vector3((float)dest.X - (float)x, 0, (float)dest.Z - (float)z));
            h.Tick(Time.deltaTime);
        }

        void TickBaserunning(float dt)
        {
            if (_match == null || _match.LeadBag == 0) return;
            if (HumanBats && _phase is Phase.Set or Phase.Flight or Phase.InPlay)
            {
                var run = RunPad;
                if (run.ThrowBag > 0)
                    _match.SelectRunner(run.ThrowBag);
                if (run.FreezeRunners)
                {
                    var haltBag = InPlay.DiamondBag(run.StickX, run.StickY);
                    if (haltBag is >= 1 and <= 3) _match.HaltAt(haltBag);
                    else _match.FreezeRunners();
                }
                else if (run.AllAdvance)
                    _match.AdvanceAll(dt * 1.7f);
                else if (run.AllReturn)
                    _match.ReturnAll(dt * 2.0f);
                var bag = _match.SelectedBag > 0 ? _match.SelectedBag : _match.LeadBag;
                var stick = InPlay.DiamondBag(run.StickX, run.StickY);
                var verb = Baserunning.StickVerb(stick, bag);
                var armPhase = _phase is Phase.Set or Phase.Flight;
                // Spec §9.2 / §11.1: the stick toward the next bag arms the steal, same as L3. No lead stick.
                if (verb == RunStick.Steal && armPhase && !_match.StealAttempt) _match.StartSteal();
                else if (verb == RunStick.Return) _match.ReturnToBag(dt * 2.0f);
                if (armPhase && run.Steal) _match.ToggleSteal();
                var near = _match.Lead01 <= 0.24 || (_match.StealAttempt && _match.Lead01 >= 0.7);
                if (near && (run.WestDown || run.SouthDown))
                    _match.Slide();
                if (TrainingOn) _coach.OnRun(_match);
            }
            if (_phase == Phase.Flight && _match.StealOn)
            {
                var stealBag = _match.ArmedStealBag;
                if (stealBag > 0) _match.TakeLeadAt(stealBag, dt * 2.4f);
                else _match.TakeLead(dt * 2.4f);
            }
            else
            {
                for (var bag = 1; bag <= 3; bag++)
                    if (_match.RunnerAt(bag)?.Returning == true)
                        _match.ReturnToBagAt(bag, dt * 2.2f);
            }
        }

        void PlaceLiveStealRunner()
        {
            var fromBag = _match.ArmedStealBag;
            var state = _match.RunnerAt(fromBag);
            var runner = state?.Who;
            if (runner == null || fromBag is not 1 and not 2) return;
            var target = state.StealTarget is 2 or 3 ? state.StealTarget : Baserunning.StealTarget(fromBag);
            if (target is not 2 and not 3) return;
            var remain = (float)StealThrow.RunnerRemainSec(runner, state.Lead01);
            var u = Mathf.Clamp01(_stealT / Mathf.Max(0.2f, remain));
            var from = Diamond.LeadSpot(fromBag, state.Lead01);
            var to = Diamond.Bag(target);
            var x = from.X + (to.X - from.X) * u;
            var z = from.Z + (to.Z - from.Z) * u;
            var h = Hero(runner);
            var pose = u > 0.55f ? Motion.Verb.Slide : Motion.Verb.Run;
            if (_throwing && _throwT >= _throwDur * 0.85f && u < 0.92f) pose = Motion.Verb.Dive;
            h.SetPose(pose);
            h.SetGear(_match.OffenseBat, _match.DefenseGlove);
            h.SetHeld(false, false);
            h.SetHighlight(true);
            h.Place(new Vector3((float)x, 0, (float)z), new Vector3((float)(to.X - x), 0, (float)(to.Z - z)));
            h.Tick(Time.deltaTime);
        }

        void TickGun(float dt)
        {
            if (!_gun) return;
            _gunT += dt;
            var u = Mathf.Clamp01(_gunT / Mathf.Max(0.05f, _gunDur));
            _ball = Vector3.Lerp(_gunFrom, _gunTo, u);
            _ball.y += Mathf.Sin(u * Mathf.PI) * 3.4f;
            if (_gunT >= _gunDur) _gun = false;
        }

        void StartStealGun(Character runner, int fromBag, double lead, PlayEvent ev)
        {
            _gun = true;
            _gunT = 0;
            _gunRunner = runner;
            _gunFromBag = fromBag;
            _gunLead = lead;
            _gunSafe = ev.Kind == PlayKind.StolenBase;
            var outcome = ev.Outcome;
            _gunPickoff = outcome?.RunnerResult == RunnerPlayResult.PickedOff;
            _gunToBag = outcome?.RunnerToBag ?? Baserunning.StealTarget(fromBag);
            if (_gunToBag <= 0) _gunToBag = fromBag;
            _gunThrowFromPitcher = outcome?.ThrowEndpoint?.Origin == ThrowOrigin.PitcherRubber;
            _gunThrowToBag = outcome?.ThrowEndpoint?.DestinationBag ?? _gunToBag;
            if (_gunThrowToBag <= 0) _gunThrowToBag = _gunToBag;
            var origin = _gunThrowFromPitcher ? Diamond.Rubber : Diamond.Positions["C"];
            var dest = Diamond.Bag(_gunThrowToBag);
            _gunFrom = new Vector3((float)origin.X, 3.4f, (float)origin.Z);
            _gunTo = new Vector3((float)dest.X, 1.2f, (float)dest.Z);
            var thr = ev.Throw;
            if (thr == null)
                thr = _match.ThrowBetween(_match.Pitcher, runner);
            _spec.ArmThrow(_gunFrom, _gunTo, thr);
            _gunDur = Mathf.Max(0.5f, _spec.ThrowSeconds);
            _audio?.ThrowPop();
            ConsiderHighlight();
            _phase = Phase.Result;
            _t = 0;
        }

        void PlaceSelectRoster()
        {
            var ids = PresetTeams.CaptainIds;
            var pick = _phase == Phase.Select;
            for (var i = 0; i < ids.Length; i++)
            {
                var who = _content.Must(ids[i]);
                var hero = Hero(who);
                var yours = ids[i] == CurrentPick().Yours;
                var theirs = ids[i] == CurrentPick().Theirs;
                var spot = CarnivalFront.CaptainSpot(i, ids.Length, pick, yours);
                hero.SetPose(yours ? Motion.Verb.Cheer : theirs ? Motion.Verb.StealLead : Motion.Verb.Idle);
                hero.SetHighlight(yours);
                hero.SetGrow(false); // Grow is a field verb. Menu 1.71x at Z=4 is Ashlord's hat.
                hero.SetHeld(false, false);
                hero.SetGear(_match.OffenseBat, _match.DefenseGlove);
                hero.Place(new Vector3(spot.X, 0f, spot.Z), new Vector3(0f, 0f, -1f));
                hero.Tick(Time.deltaTime);
                if (!pick && !yours)
                    hero.gameObject.SetActive(false);
            }
            if (pick)
            {
                _logo?.Hide();
                // HUD is the select card. World placard covered the toys (#354).
                _card?.Hide();
            }
            else
            {
                _card?.Hide();
                if (_logo == null) _logo = LogoToy.Attach(transform);
                var titleShot = _content.Shots.Must("title");
                _logo.Show(
                    CarnivalFront.Logo,
                    new Vector3(CarnivalFront.LogoX, CarnivalFront.LogoY, CarnivalFront.LogoZ),
                    new Vector3((float)titleShot.Pos.X, (float)titleShot.Pos.Y, (float)titleShot.Pos.Z));
            }
        }

        void PlaceLineupBoard()
        {
            TeamSheet.Place(_lineup, transform, _chem, _card);
        }

        HeroActor Hero(Character who)
        {
            _used.Add(who.Id);
            if (!_heroes.TryGetValue(who.Id, out var h) || h == null)
            {
                var go = new GameObject("Hero-" + who.Id);
                h = go.AddComponent<HeroActor>();
                _heroes[who.Id] = h;
            }
            h.gameObject.SetActive(true);
            h.Bind(who);
            return h;
        }

    }
}
