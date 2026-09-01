using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Applies bones/clips from gameplay events. Captains are skins.</summary>
    public sealed class ActorDirector
    {
        readonly MatchDirector _play;
        public ActorDirector(MatchDirector play) { _play = play; }
        public void Draw() { _play.DrawBodies(); }
    }

    public sealed partial class MatchDirector
    {
        internal void DrawBodies() => DrawActors();

        void DrawActors()
        {
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
                _zone.Show(false, 0, 0);
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
                _zone.Show(false, 0, 0);
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
                _zone.Show(false, 0, 0);
                _ring?.Hide();
                return;
            }
            TeamSheet.HideBoard();
            _chem?.Hide();
            var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var litId = "";
            if ((_phase is Phase.InPlay or Phase.StealThrow) && defense.TryGetValue(_glovePos, out var litWho))
                litId = litWho.Id;
            var itemLit = ItemOffered && _itemTarget != null ? _itemTarget.Id : "";
            foreach (var kv in defense)
            {
                var who = kv.Value;
                var pos = Diamond.Positions[kv.Key];
                double x = pos.X, z = pos.Z;
                if (_gloveAt.TryGetValue(kv.Key, out var live))
                {
                    x = live.X;
                    z = live.Z;
                }
                var pose = HeroActor.Pose.Idle;
                var buddyPartner = _phase == Phase.InPlay && BuddySet && _preview.Buddy != null && who.Id == _preview.Buddy.Id;
                var highlighted = (_phase is Phase.InPlay or Phase.StealThrow) && (who.Id == itemLit || who.Id == litId || (buddyPartner && !_buddy));
                if (highlighted && !buddyPartner)
                {
                    x = _fx;
                    z = _fz;
                    if (_throwing) pose = HeroActor.Pose.Catch;
                    else if (_bobbling) pose = HeroActor.Pose.Miss;
                    else if (_recoilT > 0) pose = HeroActor.Pose.Dive;
                    else if (_jumpT > 0) pose = who.FieldAbility == "clamber" ? HeroActor.Pose.Clamber : HeroActor.Pose.Jump;
                    else if (_caught && _preview != null && _preview.Grounder) pose = HeroActor.Pose.Scoop;
                    else if (_caught || _buddy) pose = HeroActor.Pose.Catch;
                    else if (_diveT > 0) pose = HeroActor.Pose.Dive;
                    else if (_preview != null && CartoonJuice.ChaseIsARun(_caught || _buddy,
                                 Diamond.Dist(x, z, _ball.x, _ball.z)))
                        pose = HeroActor.Pose.Run;
                    else if (_preview != null) pose = FieldPose(who, _preview, false);
                    else pose = HeroActor.Pose.Field;
                }
                else if (buddyPartner)
                {
                    var atWall = Diamond.Dist(x, z, WallPlant(_preview).X, WallPlant(_preview).Z) < 18;
                    if (_throwing) pose = HeroActor.Pose.Field;
                    else if (atWall) pose = HeroActor.Pose.Crouch;
                    else pose = HeroActor.Pose.Field;
                }
                else if (_phase == Phase.InPlay && _preview != null && who.Id == _preview.Fielder.Id)
                {
                    if (_buddy && _jumpT > 0)
                        pose = who.FieldAbility == "clamber" ? HeroActor.Pose.Clamber : HeroActor.Pose.Jump;
                    else
                        pose = FieldPose(who, _preview, _caught || _buddy);
                }
                else if ((_phase is Phase.InPlay or Phase.StealThrow) && Diamond.Dist(x, z, pos.X, pos.Z) > 6)
                    pose = HeroActor.Pose.Run;
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    pose = _phase == Phase.Flight ? HeroActor.Pose.ThrowPitch : HeroActor.Pose.ChargePitch;
                if (kv.Key == "C" && _phase is Phase.Set or Phase.Flight)
                    pose = HeroActor.Pose.Crouch;
                if (_throwing && kv.Key == _throwFromPos)
                    pose = HeroActor.Pose.Throw;
                if (_throwing && !string.IsNullOrEmpty(_coverPos) && kv.Key == _coverPos)
                    pose = HeroActor.Pose.Catch;
                if (_gun && kv.Key == "C" && !_gunPickoff) pose = HeroActor.Pose.Throw;
                if (_gun && kv.Key == "P" && _gunPickoff) pose = HeroActor.Pose.Throw;
                var hero = Hero(who);
                hero.SetGrow(who.FieldAbility == "grow" && highlighted);
                hero.SetHighlight(highlighted);
                if (_pending != null && _pending.StarSwingUsed == "heart-swing" && highlighted)
                    pose = HeroActor.Pose.Charm;
                var pType = _pitch != null ? _pitch.Type : _pitches[_pitchIndex];
                hero.SetPose(pose, kv.Key == "P" ? _pitchCharge : 0, kv.Key == "P" ? pType : null);
                hero.SetChargeRing(kv.Key == "P" && (_phase is Phase.Set or Phase.Flight) && HumanPitches ? _pitchCharge : 0f);
                hero.SetGear(_match.OffenseBat, _match.DefenseGlove);
                hero.SetHeld(false, true);
                if (kv.Key == "P" && _phase is Phase.Set or Phase.Flight)
                    x += _match.PitcherOffsetX * 2.2;
                var look = kv.Key == "P" && _phase is not Phase.InPlay and not Phase.StealThrow
                    ? new Vector3(0, 0, -1)
                    : _phase is Phase.InPlay or Phase.StealThrow
                        ? new Vector3(_ball.x - (float)x, 0, _ball.z - (float)z)
                        : new Vector3((float)-x, 0, (float)-z + 8f);
                if (_throwing && (highlighted || kv.Key == _throwFromPos))
                    look = _throwTo - new Vector3((float)x, 0, (float)z);
                if (_gun && ((kv.Key == "C" && !_gunPickoff) || (kv.Key == "P" && _gunPickoff)))
                    look = _gunTo - new Vector3((float)x, 0, (float)z);
                hero.Place(new Vector3((float)x, 0, (float)z), look);
                hero.Tick(Time.deltaTime);
            }

            var batter = _match.Batter;
            var bHero = Hero(batter);
            var racing = _phase == Phase.InPlay && _pending != null;
            var stillSwing = racing && _hitT < 0.40f && _swing != null && _swing.Swing && !_swing.Bunt;
            var bPose = racing ? (stillSwing ? HeroActor.Pose.Swing : HeroActor.Pose.Run) : BatterPose();
            bHero.SetPose(bPose, HumanBats ? _charge : 0);
            bHero.SetChargeRing((_phase is Phase.Set or Phase.Flight) && HumanBats ? _charge : 0f);
            bHero.SetGear(_match.OffenseBat, _match.DefenseGlove);
            var batting = bPose is HeroActor.Pose.ChargeSwing or HeroActor.Pose.Swing
                or HeroActor.Pose.CheckSwing or HeroActor.Pose.Bunt or HeroActor.Pose.Miss;
            bHero.SetHeld(batting, false);
            bHero.SetHighlight(false);
            if (racing)
            {
                if (RunPad.SouthDown) _dash01 = Mathf.Min(1f, _dash01 + 0.28f);
                _match.Dash01 = _dash01;
                if (TrainingOn) _coach.OnRun(_match);
                var tFirst = (float)InPlay.HomeToFirstSec(batter, _dash01);
                var u = Mathf.Clamp01(_hitT / Mathf.Max(0.4f, tFirst));
                var hx = 2.55f + (float)(Diamond.First.X - 2.55) * u;
                var hz = 2.4f + (float)(Diamond.First.Z - 2.4) * u;
                bHero.Place(new Vector3(hx, 0, hz), new Vector3((float)Diamond.First.X, 0, (float)Diamond.First.Z));
            }
            else
                bHero.Place(new Vector3(2.55f + (float)_match.BatterOffsetX * 2.4f, 0, 2.4f), new Vector3(0, 0, 1));
            bHero.Tick(Time.deltaTime);

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
            if ((_caught || _buddy) && !_throwing)
                HoldBallInGlove();
            if (_throwing && _armedThrow != null)
                _park.Ball.SetTrailColor(SpecialFx.ThrowColor(_armedThrow.Relation));
            else
                _park.Ball.SetTrailColor(Color.white);
            var inFlight = _phase is Phase.Flight or Phase.InPlay or Phase.StealThrow;
            var inPlay = _phase is Phase.InPlay or Phase.StealThrow;
            if (_replaying || inFlight || _phase is Phase.Set || _spec.Active)
                _park.Ball.Place(_ball, starPitch, ptype, heat, inFlight, inPlay);
            else
                _park.Ball.Hide();

            var setOrFlight = _phase is Phase.Set or Phase.Flight;
            _zone.Show(SetTells.ZoneOn(setOrFlight), _aimX, _aimY);
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
            _spec.Tick(Time.deltaTime, _ball, _phase == Phase.Flight, _phase == Phase.InPlay,
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
            _items?.Present(Time.deltaTime, ItemOffered, _itemPick, itemTargetPos, showThrow, _itemId, flyU);
        }

        HeroActor.Pose BatterPose()
        {
            if (_phase == Phase.Result && _last != null)
            {
                if (_last.Kind == PlayKind.SwingMiss) return HeroActor.Pose.Miss;
                if (_last.Kind == PlayKind.HomeRun) return HeroActor.Pose.Cheer;
                if (_swing != null && _swing.Bunt) return HeroActor.Pose.Bunt;
                if (_swing != null && _swing.Swing) return HeroActor.Pose.Swing;
                return HeroActor.Pose.Idle;
            }
            if (_phase == Phase.GameOver)
                return _match.HomeScore >= _match.AwayScore ? HeroActor.Pose.Cheer : HeroActor.Pose.Idle;
            if (_phase == Phase.Flight && (_swung || (_swing != null && _swing.Swing)))
            {
                if (_swing != null && _swing.Bunt) return HeroActor.Pose.Bunt;
                if (_charge < 0.2f && _flight > _pitchDur * 0.88f) return HeroActor.Pose.CheckSwing;
                return HeroActor.Pose.Swing;
            }
            if (_phase is Phase.Set or Phase.Flight) return HeroActor.Pose.ChargeSwing;
            return HeroActor.Pose.Idle;
        }

        static HeroActor.Pose FieldPose(Character who, FieldingPreview pre, bool caught)
        {
            if (caught) return pre.Grounder ? HeroActor.Pose.Scoop : HeroActor.Pose.Catch;
            var a = who.FieldAbility;
            if (a == "dive" && pre.Grounder) return HeroActor.Pose.Dive;
            if (a == "burrow" && pre.Grounder) return HeroActor.Pose.Dive;
            if (a == "super-jump" && pre.HomeRunLikely) return HeroActor.Pose.Jump;
            if (a == "clamber" && pre.HomeRunLikely) return HeroActor.Pose.Clamber;
            if (a == "spin-check") return HeroActor.Pose.Spin;
            return HeroActor.Pose.Field;
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
            var pose = HeroActor.Pose.Idle;
            var racing = _phase == Phase.InPlay && _pending != null && (_preview == null || _preview.Grounder || _pending.LaunchDeg < 18);
            if (racing)
            {
                var u = Mathf.Clamp01(_hitT / 3.1f);
                spot = (spot.X + (next.X - spot.X) * u, spot.Z + (next.Z - spot.Z) * u);
                var tagBag = bagNum >= 3 ? 4 : bagNum + 1;
                var threatened = _throwing && _throwBag == tagBag;
                pose = (threatened && u > 0.42f) || u > 0.82f ? HeroActor.Pose.Slide : HeroActor.Pose.Run;
            }
            else if (state != null && state.Sliding) pose = HeroActor.Pose.Slide;
            else if (state != null && state.StealAttempt) pose = HeroActor.Pose.Run;
            else if (state != null && state.Lead01 > 0.08) pose = HeroActor.Pose.StealLead;
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
            var pose = u > 0.55f ? HeroActor.Pose.Slide : HeroActor.Pose.Run;
            if (!_gunSafe && u > 0.5f) pose = HeroActor.Pose.Dive;
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
            if (HumanBats && (_phase is Phase.Set or Phase.Flight || (_phase == Phase.InPlay && Versus)))
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
                var next = Baserunning.NextBag(bag);
                var prev = Baserunning.PrevBag(bag);
                var stick = InPlay.DiamondBag(run.StickX, run.StickY);
                if (stick == next) _match.TakeLead(dt * 1.7f);
                else if (stick == bag || stick == prev) _match.ReturnToBag(dt * 2.0f);
                if ((_phase is Phase.Set or Phase.Flight) && run.Steal) _match.ToggleSteal();
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
            var pose = u > 0.55f ? HeroActor.Pose.Slide : HeroActor.Pose.Run;
            if (_throwing && _throwT >= _throwDur * 0.85f && u < 0.92f) pose = HeroActor.Pose.Dive;
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
            _gunPickoff = ev.Caption != null && ev.Caption.IndexOf("picked off", System.StringComparison.OrdinalIgnoreCase) >= 0;
            _gunToBag = _gunPickoff ? fromBag : Baserunning.StealTarget(fromBag);
            if (_gunToBag <= 0) _gunToBag = fromBag;
            var origin = _gunPickoff ? Diamond.Rubber : Diamond.Positions["C"];
            var dest = Diamond.Bag(_gunToBag);
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
                var home = ids[i] == HomeCaptain;
                var away = ids[i] == AwayCaptain;
                var spot = CarnivalFront.CaptainSpot(i, ids.Length, pick, home);
                hero.SetPose(home ? HeroActor.Pose.Cheer : away ? HeroActor.Pose.StealLead : HeroActor.Pose.Idle);
                hero.SetHighlight(home);
                hero.SetGrow(false); // Grow is a field verb. Menu 1.71x at Z=4 is Ashlord's hat.
                hero.SetHeld(false, false);
                hero.SetGear(_match.OffenseBat, _match.DefenseGlove);
                hero.Place(new Vector3(spot.X, 0f, spot.Z), new Vector3(0f, 0f, -1f));
                hero.Tick(Time.deltaTime);
                if (!pick && !home)
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
