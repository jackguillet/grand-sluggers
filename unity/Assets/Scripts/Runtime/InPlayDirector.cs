using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Hop, glove, throw, bag race. Sim still owns out/safe.</summary>
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

        void TickInPlay(float dt)
        {
            _hitT += dt;
            if (_path == null || _path.Length == 0) { BeginResult(); return; }
            var spray = _pending != null ? _pending.SprayDeg : _last.AtBat.SprayDeg;
            if (_bobbling)
            {
                var away = new Vector3((float)_fx, 0f, (float)_fz);
                var dir = _ball - away;
                if (dir.sqrMagnitude < 0.4f) dir = Vector3.forward;
                dir.y = 0;
                _ball += dir.normalized * (12f * dt);
                _ball.y = Mathf.Max(0f, _ball.y - 16f * dt);
            }
            else if ((_caught || _buddy) && !_throwing)
                _ball = new Vector3((float)_fx, _buddy ? 6.4f : 2.2f, (float)_fz);
            else if (!_throwing)
            {
                var p = BallFlight.PointAt(_path, spray, _hitT);
                _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
            }
            if (_smash > 0) _smash -= dt;
            _cam.HoldInPlay(_ball);

            if (_ring != null && _preview != null)
            {
                var hang = _path != null ? BallFlight.HangTime(_path) : _preview.HangTimeSec;
                if (LandingMark.On(_preview, _ball.y, _hitT, _caught, _buddy, hang))
                {
                    var plant = LandingMark.At(_preview, _match.Park);
                    var who = PlayFielder();
                    _ring.Show(plant.X, plant.Z, (float)LandingMark.RadiusFt(_preview),
                        LandingMark.Hot(_hitT, hang, who, _match.Park));
                }
                else
                    _ring.Hide();
            }

            if (_diveT > 0) _diveT -= dt;
            if (_jumpT > 0) _jumpT -= dt;
            if (_swapLock > 0) _swapLock -= dt;

            TickBuddyPartner(dt);
            TickItem(dt);
            TickCoverBags(dt);
            ChargeOutfield(dt);
            TryTakeGlove();
            ClampField();

            if (_closePlay)
            {
                TickClosePlay(dt);
                return;
            }

            if (_recoilT > 0 && !_throwing)
            {
                if (!_bobbling && _caught)
                {
                    var away = new Vector3((float)_fx - _ball.x, 0f, (float)_fz - _ball.z);
                    if (away.sqrMagnitude > 0.2f)
                    {
                        away.Normalize();
                        _fx += away.x * 14f * dt;
                        _fz += away.z * 14f * dt;
                        _gloveAt[_glovePos] = (_fx, _fz);
                    }
                }
                ClampField();
                _recoilT -= dt;
                if (_recoilT <= 0 && _bobbling)
                {
                    CommitInPlay();
                    return;
                }
                if (_recoilT > 0) return;
            }

            if (_throwing)
            {
                _throwT += dt;
                var u = Mathf.Clamp01(_throwT / Mathf.Max(0.05f, _throwDur));
                var arc = _armedThrow != null && _armedThrow.Relation == Chemistry.Good ? 5.2f
                    : _armedThrow != null && _armedThrow.Relation == Chemistry.Bad ? 1.6f : 3.2f;
                _ball = Vector3.Lerp(_throwFrom, _throwTo, u);
                _ball.y += Mathf.Sin(u * Mathf.PI) * arc;
                if (_throwT >= _throwDur && !_itemFlying)
                {
                    if (OnThrowArrived()) return;
                    if (TryBeginClosePlay()) return;
                    TickOccupy(0);
                    if (PlayIsTime())
                        CommitInPlay();
                }
                return;
            }

            if (_playerFielding && _preview != null && _pending != null)
            {
                TickPlayerField(dt);
                ClampField();
                return;
            }

            if (_preview != null && _cpuField != null && _pending != null)
            {
                TickCpuField(dt);
                ClampField();
                return;
            }

            var rest = BallFlight.RestTime(_path);
            var done = _hitT >= rest + 0.2f;
            if (_last?.Kind == PlayKind.HomeRun && _hitT > 2.4f) done = true;
            if (done && !_itemFlying) BeginResult();
        }

        void TickPlayerField(float dt)
        {
            TickOccupy(dt);
            var pre = _preview;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var hang = BallFlight.HangTime(_path);
            var rest = BallFlight.RestTime(_path);
            var chasing = !_caught && !_buddy && (pre.Grounder || pre.Line ? _hitT < rest : _hitT < hang);
            var buddyOn = FieldingResolver.BuddyJumpOffered(pre);
            var needsJump = FlyCatch.NeedsJump(pre);
            var plant = FlyCatch.ChaseTarget(pre, _match.Park);
            var who = map.TryGetValue(_glovePos, out var gloveNow) ? gloveNow : pre.Fielder;
            _buddyWindow = buddyOn && FlyCatch.JumpWindow(_hitT, hang, who, _match.Park);

            if (FieldPad.SwapPitcher && !buddyOn)
            {
                CycleGlove(map);
                _swapLock = 0.7f;
            }

            var stick = Mathf.Abs(FieldPad.StickX) + Mathf.Abs(FieldPad.StickY);
            if (chasing && _swapLock <= 0 && stick < 0.35f)
                ChaseGlove(dt, pre);

            if (chasing && map.TryGetValue(_glovePos, out var glove) && stick >= 0.35f)
            {
                var speed = (18 + glove.Stats.Run * 1.8) * (pre.Frozen ? 0.4 : 1)
                    * (FieldPad.EastHeld ? FieldDash.ChaseMul : 1);
                _fx += FieldPad.StickX * speed * dt;
                _fz += FieldPad.StickY * speed * dt;
                var stickFeet = FieldBounds.Clamp(_match.Park, _fx, _fz);
                _fx = stickFeet.X;
                _fz = stickFeet.Z;
                _gloveAt[_glovePos] = (_fx, _fz);
            }

            if ((FieldPad.Item || FieldPad.Attack) && chasing && map.TryGetValue(_glovePos, out var tossFrom))
            {
                Character partner = null;
                foreach (var kv in map)
                {
                    if (kv.Key == _glovePos) continue;
                    var dist = Diamond.Dist(_fx, _fz, _gloveAt.TryGetValue(kv.Key, out var at) ? at.X : 0,
                        _gloveAt.TryGetValue(kv.Key, out at) ? at.Z : 0);
                    if (FieldDash.BuddyTossOffered(_match.Chemistry.Between(tossFrom, kv.Value), dist))
                    { partner = kv.Value; break; }
                }
                if (partner == null && FieldPad.Attack)
                {
                    var best = 99.0;
                    foreach (var kv in map)
                    {
                        if (kv.Key == _glovePos) continue;
                        var dist = Diamond.Dist(_fx, _fz, _gloveAt.TryGetValue(kv.Key, out var at) ? at.X : 0,
                            _gloveAt.TryGetValue(kv.Key, out at) ? at.Z : 0);
                        if (FieldDash.KickOffered(dist) && dist < best)
                        { best = dist; partner = kv.Value; }
                    }
                }
                if (partner != null)
                {
                    CatchGlove();
                    var thr = _match.ThrowBetween(tossFrom, partner);
                    _cpuField = FieldDash.ApplyBuddyToss(
                        _cpuField ?? new FieldingResult(PlayKind.GroundOut, tossFrom, partner, 0.4, _fx, _fz, false, false),
                        partner, thr);
                    _glovePos = PosOf(map, partner);
                    if (_gloveAt.TryGetValue(_glovePos, out var spot))
                    {
                        _fx = spot.X;
                        _fz = spot.Z;
                    }
                }
            }
            if (FieldPad.WestDown)
                _jumpT = needsJump || buddyOn ? 0.7f : 0.55f;
            if (FieldPad.EastDown) _diveT = 0.5f;

            var window = CatchWindow(map);
            var d = Diamond.Dist(_fx, _fz, _ball.x, _ball.z);
            if (pre.Grounder || pre.Line)
            {
                if (FieldPad.SouthDown && d < window) { CatchGlove(); ArmRecoil(); }
                if (_diveT > 0 && d < window && _ball.y < 7.5f) { CatchGlove(); ArmRecoil(); }
            }
            else
            {
                var inWin = FlyCatch.JumpWindow(_hitT, hang, who, _match.Park);
                var under = FlyCatch.Under(_fx, _fz, _ball.x, _ball.z, plant.X, plant.Z, window, needsJump);
                var jumpTry = FieldPad.WestDown && FlyCatch.HighEnough(_ball.y, needsJump || buddyOn);
                if (stick < 0.35f && FlyCatch.AutoCatch(under, inWin, needsJump))
                { CatchGlove(); ArmRecoil(); }
                if (FlyCatch.PlayerCaught(jumpTry, FieldPad.SouthDown, under, inWin, needsJump))
                {
                    if (buddyOn && inWin && Diamond.Dist(_fx, _fz, plant.X, plant.Z) < 26)
                    {
                        _buddy = true;
                        _fx = plant.X;
                        _fz = plant.Z;
                        _gloveAt[_glovePos] = (_fx, _fz);
                    }
                    CatchGlove();
                    ArmRecoil();
                }
                if (!needsJump && _diveT > 0 && d < window && _ball.y < 7.5f)
                { CatchGlove(); ArmRecoil(); }
            }

            var stickOk = InPlay.StickNamesBag(chasing, _caught || _buddy);
            ReadThrowBag(stickOk);

            if (_buddy)
                _ball = new Vector3((float)_fx, 6.4f + (_jumpT > 0 ? 2.2f : 0f), (float)_fz);

            if (_awaitingRelay)
            {
                if (_throwBag <= 0) _throwBag = 1;
                var batterIn = _hitT >= InPlay.HomeToFirstSec(_match.Batter, _dash01);
                if (!FieldPad.SouthDown && !batterIn)
                    return;
                _awaitingRelay = false;
                if (batterIn && !FieldPad.SouthDown)
                {
                    CommitInPlay();
                    return;
                }
                BeginPlayerThrowOrCommit(map);
                return;
            }

            if (buddyOn && !_buddy && _hitT < hang + 0.18f) return;
            if (pre.Grounder || pre.Line)
            {
                if (!_caught && _hitT < rest) return;
            }
            else if (_hitT < hang) return;
            if (_caught || _buddy)
            {
                if (FieldPad.SouthDown || FieldPad.Cutoff)
                {
                    BeginPlayerThrowOrCommit(map);
                    return;
                }
                if (PlayIsTime())
                    CommitInPlay();
                return;
            }
            BeginPlayerThrowOrCommit(map);
        }

        void TryTakeGlove()
        {
            if (PlayerMustField) return;
            if (_playerFielding || _caught || _buddy || _throwing) return;
            if (_pending == null || _preview == null) return;
            if (!FieldAssist.StickTakesGlove(FieldPad.StickX, FieldPad.StickY, _feel.FieldAssistStick, FieldPad.SwapPitcher))
                return;
            _playerFielding = true;
            _cpuField = null;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            if (FieldPad.SwapPitcher)
                CycleGlove(map);
            else
                AutoGlove(map);
        }

        void TickCoverBags(float dt)
        {
            if (_preview == null && _phase != Phase.StealThrow) return;
            foreach (var pos in new[] { "1B", "2B", "3B", "C" })
            {
                if (pos == _glovePos) continue;
                if (!_gloveAt.TryGetValue(pos, out var at)) continue;
                var goal = FieldAssist.CoverSpot(pos);
                var dx = goal.X - at.X;
                var dz = goal.Z - at.Z;
                var dist = Math.Sqrt(dx * dx + dz * dz);
                if (dist < 1.2) continue;
                var step = Math.Min(dist, 28 * dt);
                _gloveAt[pos] = (at.X + dx / dist * step, at.Z + dz / dist * step);
            }
        }

        void TickCpuField(float dt)
        {
            TickOccupy(dt);
            var hang = BallFlight.HangTime(_path);
            var rest = BallFlight.RestTime(_path);
            var grounder = _preview.Grounder;
            var line = _preview.Line;
            if (!_caught && _path != null)
                ChaseGlove(dt, _preview);
            _gloveAt[_glovePos] = (_fx, _fz);
            var outPlay = _cpuField.Kind is PlayKind.FlyOut or PlayKind.GroundOut;
            var reached = outPlay || _cpuField.Bobble;
            if (!_caught && !grounder && _preview != null)
            {
                var plant = FlyCatch.ChaseTarget(_preview, _match.Park);
                var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
                var window = CatchWindow(map);
                var needsJump = FlyCatch.NeedsJump(_preview);
                var inWin = FlyCatch.JumpWindow(_hitT, hang, PlayFielder(), _match.Park);
                var under = FlyCatch.Under(_fx, _fz, _ball.x, _ball.z, plant.X, plant.Z, window, needsJump);
                if (FlyCatch.AutoCatch(under, inWin, needsJump))
                {
                    CatchGlove();
                    if (_cpuField.Kind is not PlayKind.FlyOut)
                        _cpuField = _cpuField with { Kind = PlayKind.FlyOut };
                    ArmRecoil();
                }
            }
            if (!_caught && reached && (grounder ? _hitT >= hang && _ball.y < 3.2f
                : line ? _hitT >= hang - 0.12f && _ball.y < 8f
                : _hitT >= hang - 0.18f))
            {
                CatchGlove();
                ArmRecoil();
            }
            if (!grounder && !line && _hitT < hang) return;
            if ((grounder || line) && !_caught && _hitT < rest) return;
            if (_cpuField.Bobble && _caught)
                return;
            if (HumanOwnsThrow && (_caught || _buddy))
            {
                _playerFielding = true;
                var owned = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
                ReadThrowBag(true);
                if (FieldPad.SouthDown || FieldPad.Cutoff)
                {
                    BeginPlayerThrowOrCommit(owned);
                    return;
                }
                if (PlayIsTime())
                    CommitInPlay();
                return;
            }
            if (outPlay && grounder)
            {
                if (StartGroundRelays()) return;
            }
            else if (outPlay && _cpuField.Throw != null)
            {
                _armedThrow = _cpuField.Throw;
                _armedCut = _cpuField.Cutoff;
                BeginThrow(_cpuField.Throw, _cpuField.Cutoff, 0);
                return;
            }
            if (_cpuField.Kind == PlayKind.HomeRun && _hitT < 2.4f) return;
            if (!grounder && _hitT < hang + 0.35f) return;
            if (_itemFlying) return;
            if (PlayIsTime())
                CommitInPlay();
        }

        static bool TagCam(int bag) => bag is 3 or 4;

        void ChaseGlove(float dt, FieldingPreview pre)
        {
            if (_path == null) return;
            var spray = _pending != null ? _pending.SprayDeg : 0;
            var live = BallFlight.PointAt(_path, spray, _hitT);
            var hang = BallFlight.HangTime(_path);
            var target = FieldingResolver.GloveChaseTarget(
                pre, _match.Park, live.X, live.Z, live.Y, _hitT, hang);
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            TryHandoffOutfield(map, target.X, target.Z);
            var who = map.TryGetValue(_glovePos, out var c) ? c : pre.Fielder;
            var run = FieldingResolver.ChaseSpeedFt(who, pre.Frozen);
            var speed = run;
            if (FieldingResolver.InAir(pre, live.Y, _hitT, hang))
                speed = FieldingResolver.CatchUpSpeedFt(
                    Diamond.Dist(_fx, _fz, target.X, target.Z), hang - _hitT, run, pre.Frozen);
            var next = FieldingResolver.StepToward(_fx, _fz, target.X, target.Z, speed, dt, _match.Park);
            _fx = next.X;
            _fz = next.Z;
            _gloveAt[_glovePos] = (_fx, _fz);
        }

        void TryHandoffOutfield(Dictionary<string, Character> map, double ballX, double ballZ)
        {
            var pick = FieldingResolver.PlayGlove(map, ballX, ballZ, _gloveAt);
            if (!FieldingResolver.HandoffToOutfield(_glovePos, pick.Pos)) return;
            _gloveAt[_glovePos] = (_fx, _fz);
            _glovePos = pick.Pos;
            if (_gloveAt.TryGetValue(_glovePos, out var at))
            {
                _fx = at.X;
                _fz = at.Z;
            }
        }

        void ChargeOutfield(float dt)
        {
            if (_preview == null || _pending == null || _path == null) return;
            if (_caught || _buddy || _throwing) return;
            var live = BallFlight.PointAt(_path, _pending.SprayDeg, _hitT);
            var hang = BallFlight.HangTime(_path);
            var inAir = FieldingResolver.InAir(_preview, live.Y, _hitT, hang);
            var plant = FlyCatch.ChaseTarget(_preview, _match.Park);
            if (!FieldingResolver.OutfieldShouldCharge(live.X, live.Z, plant.X, plant.Z))
                return;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var aim = inAir ? plant : (live.X, live.Z);
            var of = FieldingResolver.NearestOutfielder(map, aim.X, aim.Z, _gloveAt);
            if (of.Pos == _glovePos) return;
            if (!_gloveAt.TryGetValue(of.Pos, out var at)) return;
            var target = FieldingResolver.OutfieldChaseTarget(live.X, live.Z, plant.X, plant.Z, inAir);
            var speed = FieldingResolver.ChaseSpeedFt(of.Fielder, _preview.Frozen);
            _gloveAt[of.Pos] = FieldingResolver.StepToward(at.X, at.Z, target.X, target.Z, speed, dt, _match.Park);
        }

        void ClampField()
        {
            if (_match?.Park == null) return;
            var park = _match.Park;
            var feet = FieldBounds.Clamp(park, _fx, _fz);
            _fx = feet.X;
            _fz = feet.Z;
            if (_gloveAt.Count == 0)
            {
                _gloveAt[_glovePos] = feet;
                return;
            }
            var keys = new string[_gloveAt.Count];
            _gloveAt.Keys.CopyTo(keys, 0);
            foreach (var k in keys)
                _gloveAt[k] = FieldBounds.Clamp(park, _gloveAt[k].X, _gloveAt[k].Z);
            _gloveAt[_glovePos] = feet;
        }

        void AutoGlove(Dictionary<string, Character> map)
        {
            var x = (double)_ball.x;
            var z = (double)_ball.z;
            if (_preview != null)
            {
                var hang = _path != null ? BallFlight.HangTime(_path) : _preview.HangTimeSec;
                var t = FieldingResolver.GloveChaseTarget(
                    _preview, _match.Park, _ball.x, _ball.z, _ball.y, _hitT, hang);
                x = t.X;
                z = t.Z;
            }
            var pick = FieldingResolver.PlayGlove(map, x, z, _gloveAt);
            if (pick.Pos == _glovePos) return;
            _gloveAt[_glovePos] = (_fx, _fz);
            _glovePos = pick.Pos;
            var at = _gloveAt[_glovePos];
            _fx = at.X;
            _fz = at.Z;
        }

        void CycleGlove(Dictionary<string, Character> map)
        {
            var spots = new Dictionary<string, (double X, double Z)>();
            foreach (var kv in map)
            {
                spots[kv.Key] = _gloveAt.TryGetValue(kv.Key, out var live)
                    ? live
                    : Diamond.Positions[kv.Key];
            }
            var next = FieldAssist.SwapGlove(_glovePos, spots, _ball.x, _ball.z, FieldPad.StickX, FieldPad.StickY);
            if (!map.ContainsKey(next)) next = "P";
            _gloveAt[_glovePos] = (_fx, _fz);
            _glovePos = next;
            if (_gloveAt.TryGetValue(_glovePos, out var at))
            {
                _fx = at.X;
                _fz = at.Z;
            }
            else if (Diamond.Positions.TryGetValue(_glovePos, out var pad))
            {
                _fx = pad.X;
                _fz = pad.Z;
                _gloveAt[_glovePos] = (_fx, _fz);
            }
        }

        double CatchWindow(Dictionary<string, Character> map)
        {
            var who = map.TryGetValue(_glovePos, out var c) ? c : _preview.Fielder;
            var radius = 10 + who.Stats.Field * 0.6 + FieldAbilities.CatchBonus(who);
            if (_preview != null && FlyCatch.IsFly(_preview))
                radius += FieldAbilities.FlyRangeBonus(who);
            if (_preview != null && ParkHazards.CanClamber(_match.Park, who))
                radius += 6;
            return FieldingResolver.CatchWindowFt(radius, _diveT > 0, _jumpT > 0);
        }

        bool BuddySet => _preview != null && FieldingResolver.BuddyJumpOffered(_preview);

        (double X, double Z) WallPlant(FieldingPreview pre) => FlyCatch.WallPlant(pre, _match?.Park);

        void AimFlyCam() => _cam.HoldInPlay(_ball);

        static string PosOf(Dictionary<string, Character> map, Character who)
        {
            foreach (var kv in map)
                if (kv.Value.Id == who.Id) return kv.Key;
            return "";
        }

        void TickBuddyPartner(float dt)
        {
            _ = dt;
            if (_preview == null || _path == null || !FieldingResolver.BuddyJumpOffered(_preview))
            {
                _buddyWindow = false;
                return;
            }
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            _buddyPos = PosOf(map, _preview.Buddy);
            if (string.IsNullOrEmpty(_buddyPos)) return;
            var hang = BallFlight.HangTime(_path);
            var plant = FlyCatch.WallPlant(_preview, _match.Park);
            var u = Mathf.Clamp01(_hitT / Mathf.Max(0.25f, (float)hang - 0.4f));
            var start = Diamond.Positions[_buddyPos];
            _gloveAt[_buddyPos] = (start.X + (plant.X - start.X) * u, start.Z + (plant.Z - start.Z) * u);
            if (!_playerFielding)
                _buddyWindow = FlyCatch.JumpWindow(_hitT, hang, _preview.Fielder, _match.Park);
        }

        void ReadThrowBag(bool stickOk)
        {
            var stick = FieldPad.StickBag > 0 ? FieldPad.StickBag : FieldPad.ArrowBag;
            var armed = InPlay.ArmedBag(FieldPad.ThrowBag, stick, stickOk);
            if (armed > 0) _throwBag = armed;
        }

        void BeginPlayerThrowOrCommit(Dictionary<string, Character> map)
        {
            var hopperCaught = _preview != null && _preview.Grounder && (_caught || _buddy);
            var def = _match.LiveForce
                ? 1
                : InPlay.DefaultGroundBag(_match.First != null, _match.Second != null, _match.Third != null);
            _throwBag = InPlay.CommitBag(_throwBag, hopperCaught, FieldPad.Cutoff, def);
            if (!(_caught || _buddy))
            {
                CommitInPlay();
                return;
            }
            if (_throwBag <= 0)
            {
                if (FieldPad.Cutoff && hopperCaught)
                {
                    if (_playerFielding)
                        _throwBag = def is >= 1 and <= 4 ? def : 1;
                    else if (StartGroundRelays())
                        return;
                }
                if (_throwBag <= 0)
                    return;
            }
            var key = _throwBag == 1 ? "1B" : _throwBag == 2 ? "2B" : _throwBag == 3 ? "3B" : "C";
            map.TryGetValue(key, out var cut);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : _preview.Fielder;
            ThrowResult thr = null;
            if (cut != null) thr = _match.ThrowBetween(from, cut);
            _armedThrow = thr;
            _armedCut = cut;
            if (thr != null)
            {
                _relayBags = new[] { _throwBag };
                _relayI = 0;
                BeginThrow(thr, cut, _throwBag);
            }
            else CommitInPlay();
        }

        void BeginThrow(ThrowResult thr, Character cut, int bag)
        {
            _throwing = true;
            _throwT = 0;
            _throwBag = bag;
            _coverPos = FieldAssist.CoverKey(bag);
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            Character thrower = null;
            if (map.TryGetValue(_glovePos, out var gloveWho)) thrower = gloveWho;
            else thrower = _preview?.Fielder;
            var fromHand = new Vector3((float)_fx, 3.2f, (float)_fz);
            if (thrower != null && _heroes.TryGetValue(thrower.Id, out var th) && th != null && th.ThrowHand != null)
            {
                _park.Ball.Hold(th.ThrowHand);
                fromHand = th.ThrowHand.TransformPoint(0f, 0.1f, 0.52f);
                _park.Ball.Release();
            }
            else
                _park.Ball.Release();
            TakeCoverAfterThrow(bag);
            var to = cut != null && _heroes.TryGetValue(cut.Id, out var ch) && ch != null
                ? ch.transform.position
                : BagWorld(bag);
            _throwFrom = fromHand;
            _throwTo = to + Vector3.up * 1.2f;
            _spec.ArmThrow(_throwFrom, to, thr);
            _throwDur = Mathf.Max(0.55f, _spec.ThrowSeconds);
            _audio?.ThrowPop();
        }

        void TakeCoverAfterThrow(int bag)
        {
            _throwFromPos = _glovePos;
            var cover = FieldAssist.AfterThrowPos(_glovePos, bag);
            if (string.IsNullOrEmpty(cover) || cover == _glovePos) return;
            _gloveAt[_glovePos] = (_fx, _fz);
            _glovePos = cover;
            if (_gloveAt.TryGetValue(_glovePos, out var at))
            {
                _fx = at.X;
                _fz = at.Z;
            }
            else
            {
                var spot = FieldAssist.CoverSpot(_glovePos);
                _fx = spot.X;
                _fz = spot.Z;
                _gloveAt[_glovePos] = (_fx, _fz);
            }
        }

        static string CoverKey(int bag) => FieldAssist.CoverKey(bag);

        bool StartGroundRelays()
        {
            if (_relayBags != null) return false;
            if (_pending == null || _cpuField == null) return false;
            var beats = InPlay.BatterBeatsThrow(_match.Batter, _pending, _cpuField, _dash01);
            _relayBags = InPlay.GroundThrowBags(
                _match.First != null, _match.Second != null, _match.Third != null, beats);
            _relayI = 0;
            if (_relayBags.Length == 0) return false;
            return FireRelay();
        }

        bool FireRelay()
        {
            if (_relayBags == null || _relayI >= _relayBags.Length) return false;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var bag = _relayBags[_relayI];
            var key = CoverKey(bag);
            map.TryGetValue(key, out var cut);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : _preview.Fielder;
            ThrowResult thr = _cpuField != null ? _cpuField.Throw : null;
            if (thr == null && from != null && cut != null)
                thr = _match.ThrowBetween(from, cut);
            if (thr == null && _cpuField != null)
                thr = _cpuField.Throw;
            if (cut == null && thr == null) return false;
            _armedThrow = thr;
            _armedCut = cut;
            BeginThrow(thr ?? new ThrowResult(Chemistry.Neutral, 1.0, false), cut, bag);
            return true;
        }

        bool OnThrowArrived()
        {
            var bag = _throwBag;
            var dest = BagWorld(bag);
            _fx = dest.x;
            _fz = dest.z;
            var cover = CoverKey(bag);
            if (!string.IsNullOrEmpty(cover))
            {
                _glovePos = cover;
                _gloveAt[_glovePos] = (_fx, _fz);
            }
            _throwing = false;
            CatchGlove();

            InPlay.GroundThrowStep? step = null;
            if (_match != null && bag is >= 1 and <= 4)
            {
                step = _match.StepThrow(bag, RelayBeats(bag), PlayFielder());
                if (!string.IsNullOrEmpty(step.Value.Caption))
                    _sub = step.Value.Caption;
            }

            if (step != null && WaitForNextThrow(step.Value))
                return true;
            if (!_playerFielding)
                return AdvanceRelay();
            return false;
        }

        bool RelayBeats(int bag)
        {
            if (_playerFielding)
            {
                if (bag == 1)
                    return _hitT >= InPlay.HomeToFirstSec(_match.Batter, _dash01);
                if (bag == 2 && _match.First != null)
                    return _hitT >= InPlay.BagToBagSec(_match.First);
                return false;
            }
            if (bag == 1 && _pending != null && _cpuField != null)
                return InPlay.BatterBeatsThrow(_match.Batter, _pending, _cpuField, _dash01);
            return false;
        }

        Character PlayFielder()
        {
            if (_cpuField != null && _cpuField.Fielder != null) return _cpuField.Fielder;
            return _preview != null ? _preview.Fielder : _match.Pitcher;
        }

        bool WaitForNextThrow(InPlay.GroundThrowStep step)
        {
            if (!_playerFielding) return false;
            if (step.PlayOver || step.NextDefaultBag <= 0) return false;
            _awaitingRelay = true;
            _throwBag = step.NextDefaultBag;
            _relayBags = null;
            return true;
        }

        bool AdvanceRelay()
        {
            if (_relayBags == null || _relayI + 1 >= _relayBags.Length) return false;
            _relayI++;
            return FireRelay();
        }

        static Vector3 BagWorld(int bag)
        {
            var p = bag == 1 ? Diamond.First : bag == 2 ? Diamond.Second : bag == 3 ? Diamond.Third : Diamond.Home;
            return new Vector3((float)p.X, 1.2f, (float)p.Z);
        }

        void CommitInPlay()
        {
            if (_playerFielding && _pending != null && _preview != null)
            {
                var result = BuildPlayerResult();
                // Player already resolved catch/throw. CPU bananas must play visibly
                // during InPlay (TickItem) — never a silent 40% roll after the glove.
                _last = _match.FinishAtBat(_pitch, _swing, _pending, result);
                _coach?.OnField(result, _match);
            }
            else if (_cpuField != null && _pending != null)
            {
                _last = _match.FinishAtBat(_pitch, _swing, _pending, _cpuField);
                _coach?.OnField(_cpuField, _match);
            }
            Banner();
            if (_last != null && _last.Kind is PlayKind.HomeRun or PlayKind.Triple or PlayKind.Double)
                _audio?.Swell();
            _playerFielding = false;
            _pending = null;
            _cpuField = null;
            _occupyBatter = _occupy1 = _occupy2 = _occupy3 = 0;
            _throwing = false;
            _closePlay = false;
            _closeIcon = false;
            _relayBags = null;
            _awaitingRelay = false;
            _coverPos = "";
            _throwFromPos = "";
            _bobbling = false;
            _recoilT = 0;
            _park.Ball.Release();
            BeginResult();
        }

        FieldingResult BuildPlayerResult()
        {
            var pre = _preview;
            var hit = _pending;
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : pre.Fielder;
            Character cut = _armedCut;
            ThrowResult thr = _armedThrow;
            var bag = _throwBag > 0 ? _throwBag : FieldPad.ThrowBag;
            if (thr == null && (_caught || _buddy) && bag > 0)
            {
                var key = bag == 1 ? "1B" : bag == 2 ? "2B" : bag == 3 ? "3B" : "C";
                map.TryGetValue(key, out cut);
                if (cut != null) thr = _match.ThrowBetween(from, cut);
            }
            if (_buddy || _caught)
            {
                if (_bobbling || _playerBobble)
                    return new FieldingResult(PlayKind.Single, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy, Bobble: true);
                var hangNow = _path != null ? BallFlight.HangTime(_path) : pre.HangTimeSec;
                var kind = FlyCatch.PlayerKind(true, pre, hit, inAir: _hitT < hangNow);
                var knock = pre.Grounder && hit != null ? InPlay.KnockbackSec(InPlay.Energy(hit), from) : 0;
                return new FieldingResult(kind, from, cut, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, thr, pre.Buddy, KnockbackSec: knock);
            }
            var miss = FlyCatch.PlayerKind(false, pre, hit);
            return new FieldingResult(miss, from, null, pre.HangTimeSec, pre.LandingX, pre.LandingZ, pre.Heatball, pre.Furnace, Buddy: pre.Buddy);
        }

        void StartStealThrow(PlayEvent pitch)
        {
            _stealPitch = pitch;
            _last = pitch;
            _stealT = 0;
            _stealRelease = 0;
            _phase = Phase.StealThrow;
            _t = 0;
            _pending = null;
            _preview = null;
            _cpuField = null;
            _path = null;
            _caught = true;
            _buddy = false;
            _throwing = false;
            _relayBags = null;
            _relayI = 0;
            _throwBag = StealThrow.DefaultBag(_match.StealTargetBag);
            _coverPos = FieldAssist.CoverKey(_throwBag);
            _playerFielding = FieldAssist.PlayerStartsOnGlove(PlayerMustField);
            InitStealGloves();
            _armedThrow = null;
            _armedCut = null;
            _park.Ball.Release();
            CatchGlove();
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var catcher = map.TryGetValue("C", out var c) ? c : _match.Pitcher;
            _cpuGunAt = (float)StealThrow.CpuReleaseSec(catcher, new System.Random(catcher.Stats.Field * 17 + 3));
            AimStealThrowCam();
        }

        void InitStealGloves()
        {
            _gloveAt.Clear();
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            foreach (var kv in map)
                _gloveAt[kv.Key] = Diamond.Positions[kv.Key];
            _glovePos = "C";
            var spot = StealThrow.CatcherSpot;
            _fx = spot.X;
            _fz = spot.Z;
            _gloveAt["C"] = (_fx, _fz);
            if (!string.IsNullOrEmpty(_coverPos))
                _gloveAt[_coverPos] = FieldAssist.CoverSpot(_coverPos);
        }

        void TickStealThrow(float dt)
        {
            _stealT += dt;
            TickCoverBags(dt);
            var map = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            var fromBag = _match.ArmedStealBag;
            var state = _match.RunnerAt(fromBag);
            var remain = state != null
                ? (float)StealThrow.RunnerRemainSec(state.Who, state.Lead01)
                : 1.6f;

            if (_throwing)
            {
                _throwT += dt;
                var u = Mathf.Clamp01(_throwT / Mathf.Max(0.05f, _throwDur));
                var arc = _armedThrow != null && _armedThrow.Relation == Chemistry.Good ? 5.2f
                    : _armedThrow != null && _armedThrow.Relation == Chemistry.Bad ? 1.6f : 3.2f;
                _ball = Vector3.Lerp(_throwFrom, _throwTo, u);
                _ball.y += Mathf.Sin(u * Mathf.PI) * arc;
                _cam.HoldInPlay(_ball);
                if (_throwT >= _throwDur)
                    CommitStealThrow();
                return;
            }

            _ball = new Vector3((float)_fx, 2.2f, (float)_fz);
            AimStealThrowCam();
            var fieldSeat = PlayerMustField || HumanPitches;
            if (fieldSeat && FieldPad.SwapPitcher)
            {
                CycleGlove(map);
                _playerFielding = true;
            }
            if (!_playerFielding && fieldSeat &&
                FieldAssist.StickTakesGlove(FieldPad.StickX, FieldPad.StickY, _feel.FieldAssistStick, false))
            {
                _playerFielding = true;
                _glovePos = "C";
                var spot = StealThrow.CatcherSpot;
                _fx = spot.X;
                _fz = spot.Z;
                _gloveAt["C"] = (_fx, _fz);
            }

            ReadThrowBag(stickOk: true);

            if (_playerFielding)
            {
                if (FieldPad.SouthDown)
                {
                    FireStealThrow(map);
                    return;
                }
            }
            else if (_stealT >= _cpuGunAt)
            {
                _throwBag = StealThrow.DefaultBag(_match.StealTargetBag);
                FireStealThrow(map);
                return;
            }

            if (_stealT >= remain)
            {
                _throwBag = 0;
                _stealRelease = remain;
                CommitStealThrow();
            }
        }

        void FireStealThrow(Dictionary<string, Character> map)
        {
            var target = _match.StealTargetBag;
            _throwBag = StealThrow.CommitBag(_throwBag, target);
            if (_throwBag <= 0) _throwBag = StealThrow.DefaultBag(target);
            _stealRelease = _stealT;
            var key = FieldAssist.CoverKey(_throwBag);
            map.TryGetValue(key, out var cut);
            var from = map.TryGetValue(_glovePos, out var glove) ? glove : (map.TryGetValue("C", out var catcher) ? catcher : null);
            ThrowResult thr = null;
            if (from != null && cut != null) thr = _match.ThrowBetween(from, cut);
            _armedThrow = thr ?? new ThrowResult(Chemistry.Neutral, 1.0, false);
            _armedCut = cut;
            BeginThrow(_armedThrow, cut, _throwBag);
        }

        void CommitStealThrow()
        {
            if (_stealPitch == null)
            {
                BeginResult();
                return;
            }
            var bag = _throwBag;
            _last = _match.ResolveStealThrow(_stealPitch, bag, _stealRelease, _armedThrow);
            Banner();
            _throwing = false;
            _caught = false;
            _coverPos = "";
            _throwFromPos = "";
            _stealPitch = null;
            _playerFielding = false;
            _park.Ball.Release();
            BeginResult();
        }

        void AimStealThrowCam() => _cam.HoldInPlay(_ball);

        bool TryBeginClosePlay()
        {
            if (_match == null) return false;
            if (!ClosePlay.Offered(_throwBag, _match.Second != null, _match.Third != null))
                return false;
            _closePlay = true;
            _closePlayT = 0;
            _closeIcon = false;
            _closeBag = _throwBag;
            _closeOffAt = _closeDefAt = -1f;
            return true;
        }

        void TickClosePlay(float dt)
        {
            _closePlayT += dt;
            if (!_closeIcon)
            {
                if (_closePlayT < ClosePlay.IconDelay) return;
                _closeIcon = true;
                _closePlayT = 0;
                return;
            }

            var runner = _closeBag == 4 ? _match.Third : _match.Second;
            var fielder = PlayFielder();
            var offenseHuman = Versus ? HumanBats : HumanBats && !PlayerMustField && !_playerFielding;
            var defenseHuman = Versus
                ? (_playerFielding || HumanPitches || PlayerMustField)
                : (_playerFielding || HumanPitches || PlayerMustField);

            if (_closeOffAt < 0f)
            {
                if (offenseHuman)
                {
                    if (RunPad.SouthDown) _closeOffAt = _closePlayT;
                }
                else
                {
                    var cpu = (float)ClosePlay.CpuReactionSec(runner != null ? runner.Stats.Run : 5);
                    if (_closePlayT >= cpu) _closeOffAt = cpu;
                }
            }
            if (_closeDefAt < 0f)
            {
                if (defenseHuman)
                {
                    if (FieldPad.SouthDown) _closeDefAt = _closePlayT;
                }
                else
                {
                    var cpu = (float)ClosePlay.CpuReactionSec(fielder != null ? fielder.Stats.Field : 5);
                    if (_closePlayT >= cpu) _closeDefAt = cpu;
                }
            }

            if (_closeOffAt < 0f || _closeDefAt < 0f) return;
            var safe = ClosePlay.OffenseSafe(_closeOffAt, _closeDefAt);
            _match.ClosePlaySafe = safe;
            _sub = ClosePlay.Caption(_closeBag, safe);
            _closePlay = false;
            _closeIcon = false;
            CommitInPlay();
        }

        PlayKind LiveKind()
        {
            if (_cpuField != null) return _cpuField.Kind;
            if (_preview == null || _pending == null) return PlayKind.Single;
            var hang = _path != null ? BallFlight.HangTime(_path) : _preview.HangTimeSec;
            var inAir = !_caught && !_buddy || _hitT < hang;
            return FlyCatch.PlayerKind(_caught || _buddy, _preview, _pending, inAir);
        }

        void TickOccupy(float dt)
        {
            if (_match == null || _pending == null) return;
            var kind = LiveKind();
            var dest = InPlay.BatterDestBag(kind);
            var batter = _match.Batter;
            var feet = InPlay.RunFeet(_hitT, batter, _dash01);
            var (bx, bz) = dest > 0
                ? InPlay.AlongBases(feet, dest, HomeSet.BatterX, HomeSet.BatterZ)
                : (HomeSet.BatterX, HomeSet.BatterZ);
            var batterOn = dest > 0 && InPlay.OccupyingBag(bx, bz);
            var bat = InPlay.TickOccupy(batterOn, _occupyBatter, dt);
            _occupyBatter = (float)bat.Sec;

            TickOccupied(1, _match.First, kind, dt, ref _occupy1);
            TickOccupied(2, _match.Second, kind, dt, ref _occupy2);
            TickOccupied(3, _match.Third, kind, dt, ref _occupy3);
        }

        void TickOccupied(int fromBag, Character who, PlayKind kind, float dt, ref float sec)
        {
            if (who == null) { sec = 0; return; }
            var dest = InPlay.OccupiedDestBag(fromBag, kind, _match.SendAll, _caught || _buddy);
            var feet = InPlay.RunFeet(_hitT, who);
            var (x, z) = InPlay.TowardBag(fromBag, dest, feet);
            var on = InPlay.OccupyingBag(x, z);
            var o = InPlay.TickOccupy(on, sec, dt);
            sec = (float)o.Sec;
        }

        bool PlayIsTime()
        {
            if (_match == null) return false;
            var kind = LiveKind();
            var batterOut = !InPlay.LiveBatter(kind, _match.LiveBatterOut);
            var batter = new InPlay.Occupy(_occupyBatter > 0, _occupyBatter);
            return InPlay.Time(
                _caught || _buddy,
                _throwing,
                _match.Outs,
                batter,
                _match.First != null ? new InPlay.Occupy(_occupy1 > 0, _occupy1) : null,
                _match.Second != null ? new InPlay.Occupy(_occupy2 > 0, _occupy2) : null,
                _match.Third != null ? new InPlay.Occupy(_occupy3 > 0, _occupy3) : null,
                batterOut);
        }
    }
}
