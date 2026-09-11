using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Consumes <see cref="StillRequest"/> from unity/Temp while Play is on.
    /// Camera.Render PNGs — world only, no OnGUI — so HUD-off stills are honest.
    /// </summary>
    public sealed class StillCapture : MonoBehaviour
    {
        public static bool ForceMute { get; private set; }

        MatchDirector _play;
        StillRequest _req;
        bool _ran;
        string _temp = "";

        public static void Attach(MatchDirector play)
        {
            if (play == null) return;
            var gate = play.GetComponent<StillCapture>();
            if (gate == null) gate = play.gameObject.AddComponent<StillCapture>();
            gate._play = play;
            gate.enabled = true;
        }

        void Start()
        {
            if (_play == null) _play = GetComponent<MatchDirector>();
            _temp = TempDir();
        }

        void Update()
        {
            if (_ran || _req != null) return;
            _temp = TempDir();
            if (!StillRequest.TryLoad(_temp, out _req, out _)) return;
            ForceMute = _req.HudOff;
            StartCoroutine(Run());
        }

        void OnDisable() => ForceMute = false;

        IEnumerator Run()
        {
            if (_ran) yield break;
            _ran = true;
            var files = new List<string>();
            var swingMetrics = new List<string>();
            var swingErrors = new List<string>();
            string error = null;
            var outDir = _req.ResolvedOutDir(_temp);
            IReadOnlyList<string> shots;
            IReadOnlyList<string> swingCaptains;
            try
            {
                shots = _req.ResolvedShots();
                swingCaptains = _req.ResolvedSwingCaptains();
            }
            catch (Exception ex)
            {
                WriteDone(_temp, outDir, false, files, swingMetrics, ex.Message);
                enabled = false;
                yield break;
            }

            Directory.CreateDirectory(outDir);
            var cam = _play.GateCam;
            var w = _req.ResolvedWidth();
            var h = _req.ResolvedHeight();
            foreach (var shot in shots)
            {
                _play.GateStage(shot, _req);
                if (StillRequest.IsSwingMatrixShot(shot))
                {
                    foreach (var captain in swingCaptains)
                    {
                        _play.GateStageSwingCaptain(captain);
                        for (var i = 0; i < 24; i++) yield return null;
                        foreach (var power in new[] { (Id: "normal", Charge: 0f), (Id: "max", Charge: 1f) })
                        foreach (var beat in new[] { "ready", "load", "contact", "follow" })
                        {
                            _play.GatePoseSwing(beat, power.Charge);
                            for (var i = 0; i < 4; i++) yield return null;
                            // ActorDirector continues drawing SET while simulation is held.
                            // Reapply the exact pose on the capture frame, after those draws.
                            var hero = _play.GatePoseSwing(beat, power.Charge);
                            var matrixPng = StillRequest.SwingPngPath(outDir, captain, power.Id, beat);
                            try
                            {
                                Capture(_play.GateCam != null ? _play.GateCam : cam, matrixPng, w, h);
                                files.Add(matrixPng);
                                swingMetrics.Add(_play.GateMeasureSwing(
                                    hero, beat, captain, power.Id, out var metricError));
                                if (!string.IsNullOrEmpty(metricError)) swingErrors.Add(metricError);
                            }
                            catch (Exception ex)
                            {
                                error = ex.Message;
                                break;
                            }
                        }
                        if (error != null) break;
                    }
                    if (error != null) break;
                    continue;
                }
                for (var i = 0; i < 24; i++) yield return null;
                _play.GatePose(shot, _req);
                for (var i = 0; i < 4; i++) yield return null;
                // ActorDirector draws SET every frame. Reapply the requested
                // authored pose in the capture frame, as the swing matrix does.
                _play.GatePose(shot, _req);
                var png = StillRequest.PngPath(outDir, shot, _req.ResolvedHome());
                try
                {
                    Capture(_play.GateCam != null ? _play.GateCam : cam, png, w, h);
                    files.Add(png);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    break;
                }
            }

            var doneError = error ?? string.Join("; ", swingErrors);
            WriteDone(_temp, outDir, error == null && swingErrors.Count == 0,
                files, swingMetrics, doneError);
            try { File.Delete(StillRequest.RequestPath(_temp)); }
            catch { /* leftover request is ok */ }
            enabled = false;
        }

        static void Capture(Camera cam, string path, int w, int h)
        {
            if (cam == null) throw new InvalidOperationException("no camera for still " + path);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = null;
            File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
            Destroy(tex);
            rt.Release();
            Destroy(rt);
        }

        static void WriteDone(string temp, string outDir, bool ok, List<string> files, List<string> swingMetrics, string error)
        {
            var json = "{\"ok\":" + (ok ? "true" : "false")
                + ",\"files\":[" + string.Join(",", files.ConvertAll(f => "\"" + f.Replace("\\", "/") + "\""))
                + "],\"swing\":[" + string.Join(",", swingMetrics)
                + "],\"error\":\"" + (error ?? "").Replace("\"", "'") + "\"}";
            File.WriteAllText(StillRequest.DonePath(temp), json);
            // A Unity restart clears Temp. Preserve opt-in captures and their raw
            // Transform evidence together when the request names another folder.
            var defaultOut = Path.GetFullPath(Path.Combine(temp, StillRequest.DefaultOutFolder));
            if (!Path.GetFullPath(outDir).Equals(defaultOut, StringComparison.OrdinalIgnoreCase))
                File.WriteAllText(Path.Combine(outDir, "gs-still-done.json"), json);
        }

        static string TempDir()
        {
            var data = Application.dataPath;
            var unity = Path.GetDirectoryName(data);
            return Path.Combine(unity ?? data, "Temp");
        }
    }

    public sealed partial class MatchDirector
    {
        internal Camera GateCam => _rig != null ? _rig.Cam : Camera.main;

        internal void GateStage(string shot, StillRequest req)
        {
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
                GateStageSwingCaptain(req.ResolvedSwingCaptains()[0]);
                return;
            }

            if (shot == "char-rest" || shot == "char-pose")
            {
                if (_match == null) _match = NewMatch();
                _park.Build(_match.Park, _match.Night);
                _phase = Phase.Field;
                _gateHold = true;
                _turntable = true;
                _logo?.Hide();
                _card?.Hide();
                return;
            }

            if (shot == "title" || shot == "select" || shot == "field" || shot == "lineup")
            {
                if (_match == null) _match = NewMatch();
                _park.Build(_match.Park, _match.Night);
                if (shot == "lineup")
                    OpenLineup();
                else
                    _phase = shot == "select" ? Phase.Select : shot == "field" ? Phase.Field : Phase.Title;
                _cam.Cut(shot);
                _gateHold = true;
                return;
            }

            _match = NewMatch();
            _park.Build(_match.Park, _match.Night);

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
                _pitch = new PitchCommand("fastball", 1, 0, false);
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

        internal void GateStageSwingCaptain(string captain)
        {
            HomeCaptain = captain;
            AwayCaptain = captain.Equals("ashlord", StringComparison.OrdinalIgnoreCase)
                ? "brondo"
                : "ashlord";
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night);
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
            hero.SetHeld(true, false);
            hero.Place(new Vector3(
                    (float)HomeSet.BatterBodyX(_match.Batter.Bats),
                    0f,
                    (float)HomeSet.BatterZ),
                Vector3.forward);

            if (beat == "ready" || beat == "rest")
            {
                // Live SET uses ChargeSwing before the button is armed. Unlike
                // Idle, this is the batting-ready pose and keeps the bat shown.
                hero.SetPose(HeroActor.Pose.ChargeSwing, 0);
                hero.SnapTick(0);
            }
            else if (beat == "load")
            {
                hero.SetPose(HeroActor.Pose.ChargeSwing, charge);
                hero.SnapTick(0);
            }
            else
            {
                // Reproduce the held-load -> committed-swing handoff so normal
                // and MAX exercise the same path as live play.
                hero.SetPose(HeroActor.Pose.ChargeSwing, charge);
                hero.SnapTick(0);
                hero.SetPose(HeroActor.Pose.Swing, charge);
                hero.SnapTick(beat == "contact"
                    ? (float)MoveBones.SwingContact
                    : (float)MoveBones.SwingDur);
            }

            _cam.SmashCut(hero.transform.position + Vector3.up * (float)HomeSet.BatterChestY);
            return hero;
        }

        internal string GateMeasureSwing(
            HeroActor hero, string beat, string captain, string power, out string gateError)
        {
            gateError = "";
            var sharedRigMetrics = Array.Exists(
                SwingPresentation.SharedCaptains,
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
                gateError = $"{captain} {power} {beat}: missing imported bat mesh evidence";
                return $"{{\"captain\":\"{captain}\",\"power\":\"{power}\",\"beat\":\"{beat}\","
                    + $"\"pass\":false,\"error\":\"{gateError}\"}}";
            }
            var failures = new List<string>();
            var expectedPose = beat is "ready" or "rest" or "load"
                ? HeroActor.Pose.ChargeSwing
                : HeroActor.Pose.Swing;
            var expectedPoseTime = beat == "contact" ? (float)MoveBones.SwingContact
                : beat == "follow" ? (float)MoveBones.SwingDur
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
            if (sharedRigMetrics && !renderedHands)
                failures.Add($"{captain} {power} {beat}: rendered hand meshes are missing");
            if (sharedRigMetrics && (leftToHandle > leftContact || rightToHandle > rightContact))
                failures.Add(
                    $"{captain} {power} {beat}: rendered hands missed physical handle "
                    + $"(left {leftToHandle:0.00}/{leftContact:0.00}, "
                    + $"right {rightToHandle:0.00}/{rightContact:0.00})");
            var actualDirection = (physicalBat.BarrelEnd - physicalBat.Grip).normalized;
            var directionDot = Vector3.Dot(actualDirection, physicalBat.ExpectedDirection);
            var exactDirection = beat is "contact" or "follow"
                || (beat == "load" && power == "max");
            if (sharedRigMetrics && exactDirection && directionDot < 0.97f)
                failures.Add(
                    $"{captain} {power} {beat}: barrel left the authored key "
                    + $"(dot {directionDot:0.000})");
            if (sharedRigMetrics && beat is "ready" or "load" && actualDirection.y < 0.70f)
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
            var plateMin = new Vector3(
                (float)(-HomeSet.PlateW / 2 - physicalBat.BarrelRadius),
                (float)(PitchFlight.PlateY - 1.2 - physicalBat.BarrelRadius),
                (float)(HomeSet.PlatePointZ - physicalBat.BarrelRadius));
            var plateMax = new Vector3(
                (float)(HomeSet.PlateW / 2 + physicalBat.BarrelRadius),
                (float)(PitchFlight.PlateY + 1.2 + physicalBat.BarrelRadius),
                (float)(HomeSet.PlateFrontZ + physicalBat.BarrelRadius));
            if (sharedRigMetrics && beat == "contact"
                && !SegmentIntersectsBox(
                    physicalBat.BarrelStart, physicalBat.BarrelEnd, plateMin, plateMax))
                failures.Add(
                    $"{captain} {power} contact: physical barrel missed plate from "
                    + $"({physicalBat.BarrelStart.x:0.00}, {physicalBat.BarrelStart.y:0.00}, "
                    + $"{physicalBat.BarrelStart.z:0.00}) to "
                    + $"({physicalBat.BarrelEnd.x:0.00}, {physicalBat.BarrelEnd.y:0.00}, "
                    + $"{physicalBat.BarrelEnd.z:0.00})");

            var plate = new Vector3(0f, (float)PitchFlight.PlateY, (float)HomeSet.PlateCenterZ);
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
                    var ids = PresetTeams.CaptainIds;
                    var i = 0;
                    for (; i < ids.Length; i++)
                        if (ids[i] == HomeCaptain) break;
                    if (i >= ids.Length) i = 0;
                    var look = CarnivalFront.SelectLook(i, ids.Length);
                    _cam.CutLook("select", new Vector3(look.X, look.Y, look.Z));
                }
                else
                    _cam.Cut("title");
                return;
            }

            if (shot == "mound")
            {
                PosePitcher(HeroActor.Pose.ChargePitch, charge, true);
                PoseBatter(HeroActor.Pose.Idle, 0, false);
                _cam.Cut("mound");
                return;
            }

            if (shot == "plate")
            {
                HideCatcher();
                PoseBatter(HeroActor.Pose.ChargeSwing, charge, true);
                PosePitcher(HeroActor.Pose.ChargePitch, 1, false);
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
                PoseBatter(HeroActor.Pose.ChargeSwing, charge, true);
                PosePitcher(HeroActor.Pose.ThrowPitch, 1, false);
                if (_match.Pitcher != null && _heroes.TryGetValue(_match.Pitcher.Id, out var ph) && ph != null)
                    ph.SnapTick((float)MoveBones.PitchRelease);
                _pitch ??= new PitchCommand("fastball", 1, 0, false);
                CaptureReleaseFromHand();
                if (!StillPose.PitchReleaseIsOnTheMound(_relFrom.z))
                {
                    var rel = PitchFlight.Release(_pitch.RubberX);
                    _relFrom = new Vector3((float)rel.X, (float)rel.Y, (float)rel.Z);
                }
                _park.Ball.Release();
                var p = PitchFlight.Point("fastball", StillPose.PitchBallU, 0, 0, 0, false, 0,
                    ((double)_relFrom.x, (double)_relFrom.y, (double)_relFrom.z));
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
                PoseBatter(HeroActor.Pose.Run, 0, false);
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var run) && run != null)
                {
                    run.gameObject.SetActive(true);
                    run.Place(
                        new Vector3((float)StillPose.RunnerX, 0f, (float)StillPose.RunnerZ),
                        new Vector3((float)Diamond.First.X, 0f, (float)Diamond.First.Z));
                }
                var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
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
                    fh.SetPose(HeroActor.Pose.Scoop, 0);
                    fh.SetHeld(false, true);
                    fh.Place(new Vector3(gx, 0f, gz), new Vector3(1f, 0f, 1f));
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
                PoseBatter(HeroActor.Pose.Swing, 1, false);
                var chest = new Vector3(
                    (float)HomeSet.BatterXFor(_match.Batter.Bats),
                    (float)HomeSet.BatterChestY,
                    (float)HomeSet.BatterZ);
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var sw) && sw != null)
                {
                    sw.gameObject.SetActive(true);
                    sw.SnapTick((float)MoveBones.SwingContact);
                    chest = sw.transform.position + Vector3.up * 3.2f;
                }
                var star = _pending != null ? _pending.StarSwingUsed : _match.Batter.StarSwing;
                _spec.Tick(0, chest, false, true, false, "", star ?? "", chest, chest, false, false, false, false, chest);
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
            hero.PlaceStill(
                new Vector3((float)StillPose.CharX, 0f, (float)StillPose.CharZ),
                new Vector3((float)StillPose.CharCamX, (float)StillPose.CharCamY, (float)StillPose.CharCamZ));
            if (pose)
            {
                hero.enabled = true;
                hero.SetPose(HeroActor.Pose.Swing, 1);
                hero.SnapTick((float)StillPose.CharPoseT);
            }
            else
            {
                // Bind pose. Idle take / CharacterMotion was shredding the rest still.
                hero.SetPose(HeroActor.Pose.Idle, 0);
                hero.enabled = false;
            }
            _cam.CutRaw("select",
                new Vector3((float)StillPose.CharCamX, (float)StillPose.CharCamY, (float)StillPose.CharCamZ),
                new Vector3((float)StillPose.CharX, (float)StillPose.CharLookY, (float)StillPose.CharZ),
                (float)StillPose.CharFov);
        }

        void HideCatcher()
        {
            if (_match == null) return;
            var defense = FieldingResolver.Assign(_match.Defense.Roster, _match.Pitcher);
            if (defense.TryGetValue("C", out var catcher) && catcher != null
                && _heroes.TryGetValue(catcher.Id, out var ch) && ch != null)
                ch.gameObject.SetActive(false);
        }

        void HideBackstop()
        {
            var kit = HarborKit.Instance != null ? HarborKit.Instance : FindObjectOfType<HarborKit>();
            kit?.ShowBackstop(false);
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

        void PoseBatter(HeroActor.Pose pose, float charge, bool ring)
        {
            if (_match?.Batter == null) return;
            if (!_heroes.TryGetValue(_match.Batter.Id, out var b) || b == null) return;
            b.SetPose(pose, charge);
            b.SetChargeRing(ring ? charge : 0);
            b.SetHeld(pose is HeroActor.Pose.ChargeSwing or HeroActor.Pose.Swing, false);
            b.Place(new Vector3(
                (float)HomeSet.BatterBodyX(_match.Batter.Bats, _match.BatterOffsetX),
                0f,
                (float)HomeSet.BatterZ), new Vector3(0f, 0f, 1f));
            b.SnapTick(0.08f);
        }

        void PosePitcher(HeroActor.Pose pose, float charge, bool ring)
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
            p.SnapTick(pose == HeroActor.Pose.ThrowPitch ? (float)MoveBones.PitchRelease : 0.08f);
        }
    }
}
