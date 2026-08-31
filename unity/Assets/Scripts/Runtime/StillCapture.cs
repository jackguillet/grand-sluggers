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
            string error = null;
            IReadOnlyList<string> shots;
            try { shots = _req.ResolvedShots(); }
            catch (Exception ex)
            {
                WriteDone(_temp, false, files, ex.Message);
                enabled = false;
                yield break;
            }

            var outDir = _req.ResolvedOutDir(_temp);
            Directory.CreateDirectory(outDir);
            var cam = _play.GateCam;
            var w = _req.ResolvedWidth();
            var h = _req.ResolvedHeight();
            foreach (var shot in shots)
            {
                _play.GateStage(shot, _req);
                for (var i = 0; i < 24; i++) yield return null;
                _play.GatePose(shot, _req);
                for (var i = 0; i < 4; i++) yield return null;
                var png = StillRequest.PngPath(outDir, shot);
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

            WriteDone(_temp, error == null, files, error ?? "");
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

        static void WriteDone(string temp, bool ok, List<string> files, string error)
        {
            var json = "{\"ok\":" + (ok ? "true" : "false")
                + ",\"files\":[" + string.Join(",", files.ConvertAll(f => "\"" + f.Replace("\\", "/") + "\""))
                + "],\"error\":\"" + (error ?? "").Replace("\"", "'") + "\"}";
            File.WriteAllText(StillRequest.DonePath(temp), json);
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

        internal void GatePose(string shot, StillRequest req)
        {
            _freezeCam = true;
            _gateHold = true;
            var charge = Mathf.Clamp01((float)req.Charge01);
            if (shot == "field")
            {
                _cam.Cut("field");
                return;
            }

            if (shot == "lineup")
            {
                if (_homeDraft == null) OpenLineup();
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
                    var spot = CarnivalFront.CaptainSpot(i, ids.Length, select: true, home: true);
                    _cam.CutLook("select", new Vector3(spot.X, 4.4f, spot.Z));
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
                var chest = new Vector3(2.55f, 3.2f, 2.4f);
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
            b.Place(new Vector3(2.55f, 0f, 2.4f), new Vector3(0f, 0f, 1f));
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
