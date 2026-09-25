using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using GrandSluggers.Sim.Tooling;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

[assembly: InternalsVisibleTo("GrandSluggers.Editor")]

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The still gate's staging surface (#1043): what the editor-only <c>StillCapture</c> asks the director to pose. Nothing
    /// in a player calls it; the capture lives in the editor assembly, which sees these internals.
    /// </summary>
    public sealed partial class MatchDirector
    {
        /// <summary>A HUD-off still is being captured: the play HUD draws nothing.</summary>
        internal bool CaptureMuteHud { get; set; }

        internal Camera GateCam => _rig != null ? _rig.Cam : Camera.main;

        /// <summary>The loaded catalog, so the still gate validates a park id against the park files (#820).</summary>
        internal ContentCatalog GateContent => _content;

        /// <summary>
        /// The batch's park and night, set before the first <c>NewMatch()</c>.
        /// Dropping the match the scene came in with is what makes every shot —
        /// including the title, select and lineup shots that keep an existing
        /// match — resolve the requested park; each GateStage branch then
        /// rebuilds the view from it the way the field pick does.
        /// </summary>
        internal void GateUsePark(string parkId, bool night)
        {
            ParkId = parkId;
            Night = night;
            _match = null;
        }

        /// <summary>Hands the pick back after a batch. The drawn park stays on the last still.</summary>
        internal void GateRestorePark(string parkId, bool night)
        {
            ParkId = parkId;
            Night = night;
        }

        /// <summary>
        /// Cuts to a named park shot (F7-b1, #882) on the park this still captures. The pose is
        /// the sim's (<see cref="StillShots.Frame"/>), from the park's own pole, fence and rail;
        /// the rail is the edge <see cref="HarborWall"/> draws, <see cref="ParkBoundary.Default"/>.
        /// The client holds no Vector3 of its own.
        /// </summary>
        void CutParkShot(ParkShot row)
        {
            var pose = StillShots.Frame(row, _match.Park, ParkBoundary.Default);
            _cam.CutRaw(pose.Id,
                new Vector3((float)pose.Pos.X, (float)pose.Pos.Y, (float)pose.Pos.Z),
                new Vector3((float)pose.Target.X, (float)pose.Target.Y, (float)pose.Target.Z),
                (float)pose.Fov);
        }

        internal void GateStage(string shot, StillRequest req)
        {
            _gateSwingHand = null;
            foreach (var hero in _heroes.Values)
                if (hero != null) hero.GateBattingHand(_content.Must(hero.Id).Bats);
            _mode = PlayMode.Exhibition;
            HomeCaptain = req.ResolvedHome();
            AwayCaptain = req.ResolvedAway();
            _forceMuteHud = req.HudOff;
            _feelDebug = req.FeelDebug;
            _showTiming = false;
            _freezeCam = true;
            _gateHold = false;
            _charge = Mathf.Clamp01((float)req.Charge01);
            _caught = false;
            _pending = null;
            _preview = null;
            _path = null;
            _smash = 0;
            _freeze = 0;
            _turntable = false;

            if (StillRequest.IsSwingMatrixShot(shot))
            {
                GateStageSwingCaptain(req.ResolvedSwingCaptains(_content)[0], req);
                return;
            }

            if (shot == "char-rest" || shot == "char-pose")
            {
                if (_match == null) _match = NewMatch();
                _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
                _phase = Phase.Field;
                _gateHold = true;
                _turntable = true;
                _logo?.Hide();
                _card?.Hide();
                return;
            }

            if (_content.Shots.TryGetPark(shot, out var parkShot))
            {
                // A named park shot (F7-b): the field view the `field` cut stages, with no
                // bodies or ball in the way, and a pose computed from the park being captured.
                if (_match == null) _match = NewMatch();
                _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
                _phase = Phase.Field;
                CutParkShot(parkShot);
                _gateHold = true;
                return;
            }

            if (shot == "title" || shot == "select" || shot == "field" || shot == "lineup")
            {
                if (_match == null) _match = NewMatch();
                _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
                if (shot == "lineup")
                    OpenLineup();
                else
                    _phase = shot == "select" ? Phase.Select : shot == "field" ? Phase.Field : Phase.Title;
                _cam.Cut(shot);
                _gateHold = true;
                return;
            }

            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);

            if (shot == "mound")
            {
                BeginSet();
                _gateHold = true;
                _cam.Cut("mound");
                return;
            }

            _match.SkipToHomeCaptainAtBat();
            _match.GiveOffenseStars(5);
            BeginSet();
            _gateHold = true;

            if (shot == "plate")
            {
                _cam.Cut("plate");
                return;
            }

            if (shot == "pitch")
            {
                _pitch = new PitchCommand("fastball", 1, false);
                _phase = Phase.Flight;
                _cam.Cut("pitch");
                return;
            }

            if (shot == "diamond-grounder")
            {
                _phase = Phase.InPlay;
                return;
            }

            if (shot == "smash")
            {
                var id = _match.Batter.StarSwing;
                _pending = new AtBatResult(ContactQuality.Perfect, true, false, 100, 28, 320, false, false, null, id);
                _swing = new SwingCommand(true, 1, 0, true);
                return;
            }

            _cam.Cut(shot);
        }

        Hand? _gateSwingHand;

        internal void GateStageSwingCaptain(string captain, StillRequest req, Hand? hand = null)
        {
            _gateSwingHand = hand;
            HomeCaptain = captain;
            AwayCaptain = req.OpponentFor(captain);
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _match.SkipToHomeCaptainAtBat();
            BeginSet();
            _phase = Phase.Set;
            _gateHold = true;
            _freezeCam = true;
            _logo?.Hide();
            _card?.Hide();
            HideCatcher();
            HideBackstop();
        }

        internal HeroActor GatePoseSwing(string beat, float charge)
        {
            if (_match?.Batter == null) return null;
            foreach (var kv in _heroes)
                if (kv.Value != null) kv.Value.gameObject.SetActive(false);
            var hero = EnsureHero(_match.Batter);
            if (hero == null) return null;
            hero.gameObject.SetActive(true);
            hero.SetChargeRing(0);
            hero.GateBattingHand(_gateSwingHand ?? _match.Batter.Bats);
            hero.SetHeld(true, false);
            var box = new Vector3(
                (float)HomeSet.BatterBodyX((_gateSwingHand ?? _match.Batter.Bats)),
                0f,
                (float)HomeSet.BatterZ);
            hero.PlaceStill(box, box + Vector3.forward);

            if (beat == "ready" || beat == "rest")
            {
                // Live SET uses ChargeSwing before the button is armed. Unlike
                // Idle, this is the batting-ready pose and keeps the bat shown.
                hero.SetPose(Motion.Verb.ChargeSwing, 0);
                hero.SnapTick(0);
            }
            else if (beat == "load")
            {
                hero.SetPose(Motion.Verb.ChargeSwing, charge);
                hero.SnapTick(0);
            }
            else
            {
                // Reproduce the held-load -> committed-swing handoff so normal
                // and MAX exercise the same path as live play.
                hero.SetPose(Motion.Verb.ChargeSwing, charge);
                hero.SnapTick(0);
                hero.SetPose(Motion.Verb.Swing, charge);
                hero.SnapTick(beat == "contact" ? (float)Motion.SwingContact
                    : beat == "follow" ? (float)Motion.SwingDur
                    : (float)Motion.SwingFinish);
            }

            _cam.SmashCut(hero.transform.position + Vector3.up * (float)HomeSet.BatterChestY);
            return hero;
        }

        internal string GateMeasureSwing(
            HeroActor hero, string beat, string captain, string power, out string gateError)
        {
            gateError = "";
            var sharedRigMetrics = System.Linq.Enumerable.Any(
                _content.CaptainIds,
                id => id.Equals(captain, StringComparison.OrdinalIgnoreCase));
            if (hero == null)
            {
                gateError = $"{captain} {power} {beat}: missing batter";
                return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\","
                    + $"\"pass\":false,\"error\":\"{gateError}\"}}";
            }
            if (!hero.TrySwingGeometry(
                    out var left, out var right, out var grip, out var barrel,
                    out var socketX, out var socketY, out var socketZ))
            {
                gateError = $"{captain} {power} {beat}: missing swing geometry";
                return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\","
                    + $"\"pass\":false,\"error\":\"{gateError}\"}}";
            }
            if (!hero.TryBatVisual(
                    out var batVisual, out var batVisible,
                    out var socketGrip, out var modelGrip))
            {
                gateError = $"{captain} {power} {beat}: missing rendered bat evidence";
                return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\","
                    + $"\"pass\":false,\"error\":\"{gateError}\"}}";
            }
            var renderedHands = hero.TryRenderedSwingHands(
                out var renderedLeft, out var renderedRight);
            if (renderedHands)
            {
                left = renderedLeft.Center;
                right = renderedRight.Center;
            }
            if (!hero.TrySwingBatEvidence(out var physicalBat))
            {
                gateError = $"{captain} {power} {beat}: missing handle direction evidence";
                return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\","
                    + $"\"pass\":false,\"error\":\"{gateError}\"}}";
            }
            if (!hero.TryBatMeshEvidence(out var batMesh))
            {
                gateError = $"{captain} {power} {beat}: imported bat mesh is missing or unreadable";
                return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\","
                    + $"\"pass\":false,\"error\":\"{gateError}\"}}";
            }
            var failures = new List<string>();
            var stanceMeasured = hero.TryRenderedBattingStance(
                out var chestForward, out var eyeForward, out var feetLine);
            var towardPlate = (_gateSwingHand ?? _match.Batter.Bats) == Hand.L ? Vector3.left : Vector3.right;
            var chestTowardPlate = Vector3.Dot(chestForward, towardPlate);
            var eyesTowardPitcher = Vector3.Dot(eyeForward, Vector3.forward);
            // Signed on purpose. feetLine runs from the back foot to the lead
            // foot (BattingStance.LeadSide), which points at the pitcher from
            // either box when the hips face the plate. Taking Mathf.Abs here
            // scored hips turned out of the box -- right foot forward on a
            // right-handed batter, toes pointing away from the plate -- the
            // same as a correct stance, which is how every captain stood that
            // way through a 56/56 run.
            var feetAlongPitch = Vector3.Dot(feetLine, Vector3.forward);
            if (beat is "ready" or "rest" or "load")
            {
                if (!stanceMeasured)
                    failures.Add($"{captain} {power} {beat}: visible stance landmarks missing");
                else
                {
                    // User's square sideways stance: chest toward the plate,
                    // feet along the mound/home line, head watching the pitcher.
                    // Fifteen degrees allows an authored coil, not a front-facing body.
                    var alignment = (float)BattingStance.AlignmentDot;
                    if (chestTowardPlate < alignment)
                        failures.Add($"{captain} {power} {beat}: chest is not sideways toward plate ({chestTowardPlate:0.000})");
                    if (feetAlongPitch < alignment)
                        failures.Add($"{captain} {power} {beat}: lead foot is not toward the pitcher (back-to-lead {feetAlongPitch:0.000})");
                    if (eyesTowardPitcher < alignment)
                        failures.Add($"{captain} {power} {beat}: eyes do not face pitcher ({eyesTowardPitcher:0.000})");
                    // The lead hand rides low on the handle: left under right for a
                    // right-handed batter, right under left for a left-handed one.
                    // lHand really is the batter's left -- the blockout puts it at
                    // Blender -X facing +Y, and the import X reflection cancels
                    // against the -Z export facing -- so the drawn stack is read
                    // by name against the lead side, never against the authored
                    // key (a check anchored to the key agreed with its own bug).
                    if (renderedHands)
                    {
                        var lead = BattingStance.LeadSide((_gateSwingHand ?? _match.Batter.Bats));
                        var leadY = lead == Hand.L ? left.y : right.y;
                        var topY = lead == Hand.L ? right.y : left.y;
                        if (leadY >= topY)
                            failures.Add($"{captain} {power} {beat}: {(lead == Hand.L ? "left" : "right")} hand "
                                + $"must ride under the {(lead == Hand.L ? "right" : "left")} on the handle for a "
                                + $"{(_gateSwingHand ?? _match.Batter.Bats)} batter (lead y {leadY:0.000}, top y {topY:0.000})");
                    }
                }
            }
            var expectedPose = beat is "ready" or "rest" or "load"
                ? Motion.Verb.ChargeSwing
                : Motion.Verb.Swing;
            var expectedPoseTime = beat == "contact" ? (float)Motion.SwingContact
                : beat == "follow" ? (float)Motion.SwingDur
                : beat == "finish" ? (float)Motion.SwingFinish
                : 0f;
            if (hero.Current != expectedPose || Mathf.Abs(hero.PoseTime - expectedPoseTime) > 0.0001f)
                failures.Add(
                    $"{captain} {power} {beat}: capture frame is {hero.Current} "
                    + $"at {hero.PoseTime:0.###}, expected {expectedPose} at {expectedPoseTime:0.###}");
            if (!batVisual.Equals(GearMesh.HittingBatVisual(), StringComparison.OrdinalIgnoreCase))
                failures.Add(
                    $"{captain} {power} {beat}: rendered {batVisual}, expected {GearMesh.HittingBatVisual()}");
            if (!batVisible)
                failures.Add($"{captain} {power} {beat}: common bat is hidden");
            var gripError = Vector3.Distance(socketGrip, modelGrip);
            if (gripError > 0.01f)
                failures.Add(
                    $"{captain} {power} {beat}: model grip missed socket by {gripError:0.###}");
            if (Vector3.Distance(grip, barrel) <= SwingPresentation.BarrelRadius)
                failures.Add($"{captain} {power} {beat}: rendered bat collapsed at its socket");
            const float meshTolerance = 0.025f;
            if (Mathf.Abs(batMesh.MinY + 1.14f) > meshTolerance
                || Mathf.Abs(batMesh.MaxY - 1.25f) > meshTolerance
                || Mathf.Abs(batMesh.MaxRadius - (float)SwingPresentation.ModelBarrelRadius) > meshTolerance)
                failures.Add(
                    $"{captain} {power} {beat}: imported bat geometry lost its authored origin "
                    + $"(Y {batMesh.MinY:0.000}..{batMesh.MaxY:0.000}, radius {batMesh.MaxRadius:0.000})");
            if (!batMesh.HasGripMaterial
                || Mathf.Abs(batMesh.GripMinY + 1.00f) > meshTolerance
                || Mathf.Abs(batMesh.GripMaxY + 0.10f) > meshTolerance)
                failures.Add(
                    $"{captain} {power} {beat}: imported grip submesh lost the physical handle "
                    + (batMesh.HasGripMaterial
                        ? $"(Y {batMesh.GripMinY:0.000}..{batMesh.GripMaxY:0.000})"
                        : "(grip material missing)"));
            var leftToHandle = renderedHands
                ? PointSegmentDistance(
                    renderedLeft.RootCenter, physicalBat.RootGrip, physicalBat.RootHandleEnd)
                : float.PositiveInfinity;
            var rightToHandle = renderedHands
                ? PointSegmentDistance(
                    renderedRight.RootCenter, physicalBat.RootGrip, physicalBat.RootHandleEnd)
                : float.PositiveInfinity;
            var leftExtent = renderedHands ? MaxComponent(renderedLeft.RootExtents) : 0f;
            var rightExtent = renderedHands ? MaxComponent(renderedRight.RootExtents) : 0f;
            var leftContact = leftExtent + physicalBat.RootHandleRadius;
            var rightContact = rightExtent + physicalBat.RootHandleRadius;
            if (!renderedHands)
                failures.Add($"{captain} {power} {beat}: rendered hand meshes are missing");
            if (leftToHandle > leftContact || rightToHandle > rightContact)
                failures.Add(
                    $"{captain} {power} {beat}: rendered hands missed physical handle "
                    + $"(left {leftToHandle:0.00}/{leftContact:0.00}, "
                    + $"right {rightToHandle:0.00}/{rightContact:0.00})");
            var actualDirection = (physicalBat.BarrelEnd - physicalBat.Grip).normalized;
            var directionDot = Vector3.Dot(actualDirection, physicalBat.ExpectedDirection);
            var exactDirection = beat is "contact" or "follow" or "finish"
                || (beat == "load" && power == "max");
            if (sharedRigMetrics && exactDirection && directionDot < 0.97f)
                failures.Add(
                    $"{captain} {power} {beat}: barrel left the authored key "
                    + $"(dot {directionDot:0.000})");
            if (sharedRigMetrics && beat is "ready" or "load"
                && actualDirection.y < (float)SwingPresentation.LoadedBarrelRise)
                failures.Add(
                    $"{captain} {power} {beat}: loaded barrel did not rise above the hands "
                    + $"(world Y {actualDirection.y:0.000})");
            if (sharedRigMetrics)
            {
                var gap = Vector3.Distance(renderedLeft.RootCenter, renderedRight.RootCenter);
                var maxGap = leftExtent + rightExtent + physicalBat.RootHandleRadius * 2f;
                if (gap > maxGap)
                    failures.Add(
                        $"{captain} {power} {beat}: hands separated along the handle "
                        + $"(gap {gap:0.00}, authored max {maxGap:0.00})");
            }
            // #623: the bat never passes through the drawn head, at any beat, on any captain.
            // Root space: the head is a sphere there (its smallest half-extent is the radius).
            var headMeasured = hero.TryRenderedHead(out var renderedHead);
            var headClearance = headMeasured
                ? PointSegmentDistance(renderedHead.RootCenter, physicalBat.RootGrip, physicalBat.RootBarrelEnd)
                    - Mathf.Min(renderedHead.RootExtents.x,
                        Mathf.Min(renderedHead.RootExtents.y, renderedHead.RootExtents.z))
                    - physicalBat.RootBarrelRadius
                : float.PositiveInfinity;
            if (sharedRigMetrics && headMeasured && headClearance < 0f)
                failures.Add(
                    $"{captain} {power} {beat}: the bat passes through the head "
                    + $"(surface clearance {headClearance:0.00})");
            // #560: the plate SET must see the loaded barrel beside the head.
            // Measure from HomeSet's plate camera, not this smash diagnostic shot.
            var besideHeadDeg = 0f;
            if (headMeasured && (beat == "ready" || (beat == "load" && power == "normal")))
            {
                var plateCam = new Vector3(
                    (float)HomeSet.CamX, (float)HomeSet.CamY, (float)HomeSet.CamZ);
                var head = renderedHead.Center;
                var headR = Mathf.Min(renderedHead.Extents.x,
                    Mathf.Min(renderedHead.Extents.y, renderedHead.Extents.z));
                var toHead = head - plateCam;
                var distHead = toHead.magnitude;
                var angR = distHead > headR
                    ? Mathf.Asin(Mathf.Clamp01(headR / distHead)) * Mathf.Rad2Deg
                    : 180f;
                for (var sample = 0; sample <= 24; sample++)
                {
                    var along = sample / 24f;
                    if (along < (float)SwingPresentation.BesideWoodFrom) continue;
                    var p = Vector3.Lerp(physicalBat.BarrelStart, physicalBat.BarrelEnd, along);
                    var sep = Vector3.Angle(toHead, p - plateCam);
                    besideHeadDeg = Mathf.Max(besideHeadDeg, sep - angR);
                }
                if (sharedRigMetrics && besideHeadDeg < (float)SwingPresentation.PlateLoadedBesideDeg)
                    failures.Add(
                        $"{captain} {power} {beat}: loaded barrel hides in the head from the plate camera "
                        + $"(beside {besideHeadDeg:0.00} deg)");
            }
            var plateMin = new Vector3(
                (float)(-HomeSet.PlateW / 2 - physicalBat.BarrelRadius),
                (float)(SwingPresentation.PlateBandY - 1.2 - physicalBat.BarrelRadius),
                (float)(HomeSet.PlatePointZ - physicalBat.BarrelRadius));
            var plateMax = new Vector3(
                (float)(HomeSet.PlateW / 2 + physicalBat.BarrelRadius),
                (float)(SwingPresentation.PlateBandY + 1.2 + physicalBat.BarrelRadius),
                (float)(HomeSet.PlateFrontZ + physicalBat.BarrelRadius));
            if (beat == "contact"
                && !SegmentIntersectsBox(
                    physicalBat.BarrelStart, physicalBat.BarrelEnd, plateMin, plateMax))
                failures.Add(
                    $"{captain} {power} contact: physical barrel missed plate from "
                    + $"({physicalBat.BarrelStart.x:0.00}, {physicalBat.BarrelStart.y:0.00}, "
                    + $"{physicalBat.BarrelStart.z:0.00}) to "
                    + $"({physicalBat.BarrelEnd.x:0.00}, {physicalBat.BarrelEnd.y:0.00}, "
                    + $"{physicalBat.BarrelEnd.z:0.00})");

            var plate = new Vector3(0f, (float)SwingPresentation.PlateBandY, (float)HomeSet.PlateCenterZ);
            var axis = physicalBat.BarrelEnd - physicalBat.BarrelStart;
            var axisSq = axis.sqrMagnitude;
            var u = axisSq < 0.0001f ? 0f : Mathf.Clamp01(
                Vector3.Dot(plate - physicalBat.BarrelStart, axis) / axisSq);
            var nearest = physicalBat.BarrelStart + axis * u;
            gateError = string.Join("; ", failures);
            return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\""
                + ",\"pass\":" + (failures.Count == 0 ? "true" : "false")
                + ",\"error\":\"" + gateError.Replace("\"", "'") + "\""
                + ",\"sharedRigMetrics\":" + (sharedRigMetrics ? "true" : "false")
                + ",\"pose\":\"" + hero.Current + "\""
                + ",\"poseT\":" + SwingNumber(hero.PoseTime)
                + ",\"chestForward\":[" + SwingVector(chestForward) + "]"
                + ",\"eyeForward\":[" + SwingVector(eyeForward) + "]"
                + ",\"feetLine\":[" + SwingVector(feetLine) + "]"
                + ",\"chestTowardPlate\":" + SwingNumber(chestTowardPlate)
                + ",\"eyesTowardPitcher\":" + SwingNumber(eyesTowardPitcher)
                + ",\"feetAlongPitch\":" + SwingNumber(feetAlongPitch)
                + ",\"batVisual\":\"" + batVisual.Replace("\"", "'") + "\""
                + ",\"batVisible\":" + (batVisible ? "true" : "false")
                + ",\"socketGrip\":[" + SwingVector(socketGrip) + "]"
                + ",\"modelGrip\":[" + SwingVector(modelGrip) + "]"
                + ",\"gripError\":" + SwingNumber(gripError)
                + ",\"renderedHands\":" + (renderedHands ? "true" : "false")
                + ",\"leftHand\":[" + SwingVector(renderedLeft.Center) + "]"
                + ",\"rightHand\":[" + SwingVector(renderedRight.Center) + "]"
                + ",\"leftHandRoot\":[" + SwingVector(renderedLeft.RootCenter) + "]"
                + ",\"rightHandRoot\":[" + SwingVector(renderedRight.RootCenter) + "]"
                + ",\"physicalGrip\":[" + SwingVector(physicalBat.Grip) + "]"
                + ",\"handleEnd\":[" + SwingVector(physicalBat.HandleEnd) + "]"
                + ",\"barrelStart\":[" + SwingVector(physicalBat.BarrelStart) + "]"
                + ",\"physicalBarrelEnd\":[" + SwingVector(physicalBat.BarrelEnd) + "]"
                + ",\"physicalGripRoot\":[" + SwingVector(physicalBat.RootGrip) + "]"
                + ",\"handleEndRoot\":[" + SwingVector(physicalBat.RootHandleEnd) + "]"
                + ",\"barrelStartRoot\":[" + SwingVector(physicalBat.RootBarrelStart) + "]"
                + ",\"headMeasured\":" + (headMeasured ? "true" : "false")
                + ",\"headClearance\":" + SwingNumber(headClearance)
                + ",\"besideHeadDeg\":" + SwingNumber(besideHeadDeg)
                + ",\"leftToHandle\":" + SwingNumber(leftToHandle)
                + ",\"rightToHandle\":" + SwingNumber(rightToHandle)
                + ",\"leftHandExtents\":[" + SwingVector(renderedLeft.Extents) + "]"
                + ",\"rightHandExtents\":[" + SwingVector(renderedRight.Extents) + "]"
                + ",\"leftHandRootExtents\":[" + SwingVector(renderedLeft.RootExtents) + "]"
                + ",\"rightHandRootExtents\":[" + SwingVector(renderedRight.RootExtents) + "]"
                + ",\"handleRadius\":" + SwingNumber(physicalBat.HandleRadius)
                + ",\"barrelRadius\":" + SwingNumber(physicalBat.BarrelRadius)
                + ",\"rootHandleRadius\":" + SwingNumber(physicalBat.RootHandleRadius)
                + ",\"rootBarrelRadius\":" + SwingNumber(physicalBat.RootBarrelRadius)
                + ",\"leftHandleContact\":" + SwingNumber(leftContact)
                + ",\"rightHandleContact\":" + SwingNumber(rightContact)
                + ",\"meshMinY\":" + SwingNumber(batMesh.MinY)
                + ",\"meshMaxY\":" + SwingNumber(batMesh.MaxY)
                + ",\"meshMaxRadius\":" + SwingNumber(batMesh.MaxRadius)
                + ",\"meshHasGripMaterial\":" + (batMesh.HasGripMaterial ? "true" : "false")
                + ",\"meshGripMinY\":" + SwingNumber(batMesh.GripMinY)
                + ",\"meshGripMaxY\":" + SwingNumber(batMesh.GripMaxY)
                + ",\"expectedDirection\":[" + SwingVector(physicalBat.ExpectedDirection) + "]"
                + ",\"actualDirection\":[" + SwingVector(actualDirection) + "]"
                + ",\"directionDot\":" + SwingNumber(directionDot)
                + ",\"exactDirectionKey\":" + (exactDirection ? "true" : "false")
                + ",\"handGap\":" + SwingNumber(Vector3.Distance(left, right))
                + ",\"rootHandGap\":" + SwingNumber(Vector3.Distance(
                    renderedLeft.RootCenter, renderedRight.RootCenter))
                + ",\"leftToGrip\":" + SwingNumber(Vector3.Distance(left, grip))
                + ",\"rightToGrip\":" + SwingNumber(Vector3.Distance(right, grip))
                + ",\"grip\":[" + SwingVector(grip) + "]"
                + ",\"tip\":[" + SwingVector(barrel) + "]"
                + ",\"socketX\":[" + SwingVector(socketX) + "]"
                + ",\"socketY\":[" + SwingVector(socketY) + "]"
                + ",\"socketZ\":[" + SwingVector(socketZ) + "]"
                + ",\"nearestPlate\":[" + SwingVector(nearest) + "]"
                + ",\"plateCenterDistance\":" + SwingNumber(Vector3.Distance(nearest, plate)) + "}";
        }

        static string SwingVector(Vector3 p) =>
            SwingNumber(p.x) + "," + SwingNumber(p.y) + "," + SwingNumber(p.z);

        static string SwingNumber(float value) =>
            value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        static float PointSegmentDistance(Vector3 point, Vector3 start, Vector3 end)
        {
            var axis = end - start;
            var u = axis.sqrMagnitude < 0.0001f
                ? 0f
                : Mathf.Clamp01(Vector3.Dot(point - start, axis) / axis.sqrMagnitude);
            return Vector3.Distance(point, start + axis * u);
        }

        static float MaxComponent(Vector3 value) =>
            Mathf.Max(value.x, Mathf.Max(value.y, value.z));

        static bool SegmentIntersectsBox(Vector3 start, Vector3 end, Vector3 min, Vector3 max)
        {
            var direction = end - start;
            var enter = 0f;
            var exit = 1f;
            for (var axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(direction[axis]) < 0.00001f)
                {
                    if (start[axis] < min[axis] || start[axis] > max[axis]) return false;
                    continue;
                }
                var a = (min[axis] - start[axis]) / direction[axis];
                var b = (max[axis] - start[axis]) / direction[axis];
                if (a > b) (a, b) = (b, a);
                enter = Mathf.Max(enter, a);
                exit = Mathf.Min(exit, b);
                if (enter > exit) return false;
            }
            return true;
        }

        internal void GatePose(string shot, StillRequest req)
        {
            _freezeCam = true;
            _gateHold = true;
            var charge = Mathf.Clamp01((float)req.Charge01);
            if (shot == "char-rest" || shot == "char-pose")
            {
                PoseCharacterTurntable(shot == "char-pose");
                return;
            }

            if (shot == "field")
            {
                _cam.Cut("field");
                return;
            }

            if (_content.Shots.TryGetPark(shot, out var parkShot))
            {
                CutParkShot(parkShot);
                return;
            }

            if (shot == "lineup")
            {
                if (_lineup == null) OpenLineup();
                _cam.Cut("lineup");
                return;
            }

            if (shot == "title" || shot == "select")
            {
                if (shot == "select")
                {
                    var ids = _content.CaptainIds;
                    var i = 0;
                    for (; i < ids.Count; i++)
                        if (ids[i] == HomeCaptain) break;
                    if (i >= ids.Count) i = 0;
                    var look = CarnivalFront.SelectLook(i, ids.Count);
                    _cam.CutLook("select", new Vector3(look.X, look.Y, look.Z));
                }
                else
                    _cam.Cut("title");
                return;
            }

            if (shot == "mound")
            {
                PosePitcher(Motion.Verb.ChargePitch, charge, true);
                PoseBatter(Motion.Verb.Idle, 0, false);
                _cam.Cut("mound");
                return;
            }

            if (shot == "plate")
            {
                HideCatcher();
                PoseBatter(Motion.Verb.ChargeSwing, charge, true);
                PosePitcher(Motion.Verb.ChargePitch, 1, false);
                HoldPitchInHand();
                _cam.CutRaw("plate",
                    new Vector3((float)StillPose.PlateCamX, (float)StillPose.PlateCamY, (float)StillPose.PlateCamZ),
                    new Vector3((float)StillPose.PlateLookX, (float)StillPose.PlateLookY, (float)StillPose.PlateLookZ),
                    (float)StillPose.PlateFov);
                return;
            }

            if (shot == "pitch")
            {
                HideCatcher();
                PoseBatter(Motion.Verb.ChargeSwing, charge, true);
                PosePitcher(Motion.Verb.ThrowPitch, 1, false);
                if (_match.Pitcher != null && _heroes.TryGetValue(_match.Pitcher.Id, out var ph) && ph != null)
                    ph.SnapTick((float)Motion.PitchRelease);
                _pitch ??= new PitchCommand("fastball", 1, false);
                CaptureReleaseFromHand();
                if (!StillPose.PitchReleaseIsOnTheMound(_relFrom.z))
                {
                    var rel = PitchFlight.Release(_match.Rules, _pitch.RubberX);
                    _relFrom = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
                }
                _park.Ball.Release();
                var p = PitchFlight.Point("fastball", StillPose.PitchBallU, _match.Rules, 0, 0, 0, 0,
                    ((double)_relFrom.x, (double)_relFrom.y, (double)_relFrom.z), zone: _match.BatterZone);
                _ball = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                _park.Ball.Place(_ball, "", "fastball", false, true);
                _cam.CutRaw("pitch",
                    new Vector3((float)StillPose.PitchCamX, (float)StillPose.PitchCamY, (float)StillPose.PitchCamZ),
                    new Vector3((float)StillPose.PitchLookX, (float)StillPose.PitchLookY, (float)StillPose.PitchLookZ),
                    (float)StillPose.PitchFov);
                return;
            }

            if (shot == "diamond-grounder")
            {
                var gx = (float)StillPose.ScoopX;
                var gz = (float)StillPose.ScoopZ;
                foreach (var kv in _heroes)
                    if (kv.Value != null) kv.Value.gameObject.SetActive(false);
                PoseBatter(Motion.Verb.Run, 0, false);
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var run) && run != null)
                {
                    run.gameObject.SetActive(true);
                    var runAt = new Vector3((float)StillPose.RunnerX, 0f, (float)StillPose.RunnerZ);
                    run.PlaceStill(runAt, runAt + new Vector3((float)Diamond.First.X, 0f, (float)Diamond.First.Z));
                }
                var defense = _match.DefenseMap;
                Character scoopWho = null;
                if (!defense.TryGetValue(StillPose.ScoopGlove, out scoopWho) || scoopWho == null)
                {
                    foreach (var key in new[] { "SS", "3B", "1B" })
                    {
                        if (!defense.TryGetValue(key, out scoopWho) || scoopWho == null) continue;
                        break;
                    }
                }
                var fh = EnsureHero(scoopWho);
                if (fh != null)
                {
                    fh.gameObject.SetActive(true);
                    fh.SetPose(Motion.Verb.Scoop, 0);
                    fh.SetHeld(false, true);
                    fh.PlaceStill(new Vector3(gx, 0f, gz), new Vector3(gx + 1f, 0f, gz + 1f));
                    fh.SnapTick((float)StillPose.ScoopPoseT);
                    _ball = new Vector3(gx, (float)StillPose.ScoopBallY, gz);
                    _park.Ball.Place(_ball, "", "fastball", false);
                    if (fh.CatchHand != null) _park.Ball.Hold(fh.CatchHand);
                }
                // Side 3/4. Looking down the path hid the glove; looking at the scoop
                // only put the runner behind the camera.
                _cam.CutRaw("diamond-grounder",
                    new Vector3((float)StillPose.CamX, (float)StillPose.CamY, (float)StillPose.CamZ),
                    new Vector3((float)StillPose.ScoopLookX, (float)StillPose.ScoopLookY, (float)StillPose.ScoopLookZ),
                    50f);
                return;
            }

            if (shot == "smash")
            {
                HideCatcher();
                HideBackstop();
                foreach (var kv in _heroes)
                    if (kv.Value != null) kv.Value.gameObject.SetActive(false);
                PoseBatter(Motion.Verb.Swing, 1, false);
                var chest = new Vector3(
                    (float)HomeSet.BatterXFor(_match.Batter.Bats),
                    (float)HomeSet.BatterChestY,
                    (float)HomeSet.BatterZ);
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var sw) && sw != null)
                {
                    sw.gameObject.SetActive(true);
                    sw.SnapTick((float)Motion.SwingContact);
                    chest = sw.transform.position + Vector3.up * 3.2f;
                }
                var star = _pending != null ? _pending.StarSwingUsed : _match.Batter.StarSwing;
                _spec.Tick(0, chest, false, true, false, "", star ?? "", false, false, chest);
                _cam.SmashCut(chest);
            }
        }

        void PoseCharacterTurntable(bool pose)
        {
            HideBackstop();
            _logo?.Hide();
            _card?.Hide();
            foreach (var kv in _heroes)
            {
                if (kv.Value == null) continue;
                kv.Value.gameObject.SetActive(kv.Key.Equals(HomeCaptain, StringComparison.OrdinalIgnoreCase));
            }
            var who = _content.Must(HomeCaptain);
            var hero = EnsureHero(who);
            if (hero == null) return;
            hero.gameObject.SetActive(true);
            hero.SetHeld(false, false);
            hero.SetChargeRing(0);
            var shot = StillPose.CharFraming(_content, HomeCaptain);
            hero.PlaceStill(
                new Vector3((float)StillPose.CharX, 0f, (float)StillPose.CharZ),
                new Vector3((float)shot.Pos.X, (float)shot.Pos.Y, (float)shot.Pos.Z));
            if (pose)
            {
                hero.SetPose(Motion.Verb.Swing, 1);
                hero.SnapTick((float)StillPose.CharPoseT);
            }
            else
            {
                hero.SetPose(Motion.Verb.Idle, 0);
                hero.SnapTick(0f);
            }
            _cam.CutRaw("select",
                new Vector3((float)shot.Pos.X, (float)shot.Pos.Y, (float)shot.Pos.Z),
                new Vector3((float)shot.Target.X, (float)shot.Target.Y, (float)shot.Target.Z),
                (float)shot.Fov);
        }

        void HideCatcher()
        {
            if (_match == null) return;
            var defense = _match.DefenseMap;
            if (defense.TryGetValue("C", out var catcher) && catcher != null
                && _heroes.TryGetValue(catcher.Id, out var ch) && ch != null)
                ch.gameObject.SetActive(false);
        }

        void HideBackstop()
        {
            _park?.Kit?.ShowBackstop(false);
        }

        HeroActor EnsureHero(Character who)
        {
            if (who == null) return null;
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

        void PoseBatter(Motion.Verb pose, float charge, bool ring)
        {
            if (_match?.Batter == null) return;
            if (!_heroes.TryGetValue(_match.Batter.Id, out var b) || b == null) return;
            b.SetPose(pose, charge);
            b.SetChargeRing(ring ? charge : 0);
            b.SetHeld(pose is Motion.Verb.ChargeSwing or Motion.Verb.Swing, false);
            b.Place(new Vector3(
                (float)HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterOffsetX),
                0f,
                (float)HomeSet.BatterZ), new Vector3(0f, 0f, 1f));
            b.SnapTick(0.08f);
        }

        void PosePitcher(Motion.Verb pose, float charge, bool ring)
        {
            if (_match?.Pitcher == null) return;
            var p = EnsureHero(_match.Pitcher);
            if (p == null) return;
            p.SetPose(pose, charge, "fastball");
            p.SetChargeRing(ring ? charge : 0);
            p.SetHeld(false, false);
            p.Place(
                new Vector3(0f, 0f, (float)Diamond.Mound),
                new Vector3(0f, 0f, -1f));
            p.SnapTick(pose == Motion.Verb.ThrowPitch ? (float)Motion.PitchRelease : 0.08f);
        }
    }
}
