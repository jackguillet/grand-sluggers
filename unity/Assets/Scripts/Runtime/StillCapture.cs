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
                for (var i = 0; i < 8; i++) yield return null;
                _play.GatePose(shot, _req);
                yield return null;
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
            _charge = Mathf.Clamp01((float)req.Charge01);
            if (_match == null || _park == null)
            {
                _match = NewMatch();
                _park.Build(_match.Park, _match.Night);
            }

            if (shot == "title" || shot == "select")
            {
                _phase = shot == "select" ? Phase.Select : Phase.Title;
                _cam.Cut(shot);
                return;
            }

            if (shot == "mound")
            {
                _match = NewMatch();
                _park.Build(_match.Park, _match.Night);
                BeginSet();
                _cam.Cut("mound");
                return;
            }

            if (shot == "plate")
            {
                _match = NewMatch();
                _match.SkipToHomeHalf();
                _park.Build(_match.Park, _match.Night);
                BeginSet();
                _cam.Cut("plate");
                return;
            }

            if (shot == "diamond-grounder")
            {
                _match = NewMatch();
                _match.SkipToHomeHalf();
                BeginSet();
                var hopper = new AtBatResult(ContactQuality.Solid, true, false, 90, 8, 40, false, false, null, null, SprayDeg: 4);
                _pending = hopper;
                _preview = _match.PreviewHit(hopper);
                _playerFielding = true;
                _caught = true;
                InitGloves();
                StartFly(hopper);
                _hitT = 0.22f;
                _cam.Cut("diamond-grounder");
                return;
            }

            if (shot == "smash")
            {
                _match = NewMatch();
                _match.SkipToHomeHalf();
                _match.GiveOffenseStars(5);
                BeginSet();
                var id = _match.Batter.StarSwing;
                var hit = new AtBatResult(ContactQuality.Perfect, true, false, 100, 28, 320, false, false, null, id);
                _pending = hit;
                _swing = new SwingCommand(true, 1, 0, true);
                StartFly(hit);
                return;
            }

            _cam.Cut(shot);
        }

        internal void GatePose(string shot, StillRequest req)
        {
            _freezeCam = true;
            var charge = Mathf.Clamp01((float)req.Charge01);
            if (shot == "title" || shot == "select")
            {
                _cam.Cut(shot);
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
                PoseBatter(HeroActor.Pose.ChargeSwing, charge, true);
                PosePitcher(HeroActor.Pose.Idle, 0, false);
                _cam.Cut("plate");
                return;
            }

            if (shot == "diamond-grounder")
            {
                PoseBatter(HeroActor.Pose.Run, 0, false);
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var run) && run != null)
                    run.Place(new Vector3(18f, 0f, 18f), new Vector3((float)Diamond.First.X, 0f, (float)Diamond.First.Z));
                Character glove = _preview != null ? _preview.Fielder : null;
                if (glove != null && _heroes.TryGetValue(glove.Id, out var fh) && fh != null)
                {
                    var x = _preview != null ? (float)_preview.LandingX : -20f;
                    var z = _preview != null ? (float)_preview.LandingZ : 40f;
                    fh.SetPose(HeroActor.Pose.Scoop, 0);
                    fh.SetHeld(false, true);
                    fh.Place(new Vector3(x, 0f, z), new Vector3(-x, 0f, -z));
                    fh.Tick((float)MoveBones.Mark(MoveBones.Verb.Scoop, MoveBones.ClipEvent.Contact));
                    _ball = new Vector3(x, 1.1f, z);
                    _park.Ball.Place(_ball, "", "fastball", false);
                    if (fh.CatchHand != null) _park.Ball.Hold(fh.CatchHand);
                }
                _cam.Cut("diamond-grounder");
                return;
            }

            if (shot == "smash")
            {
                PoseBatter(HeroActor.Pose.Swing, 1, false);
                if (_match.Batter != null && _heroes.TryGetValue(_match.Batter.Id, out var sw) && sw != null)
                    sw.Tick((float)MoveBones.SwingContact);
                _cam.Cut("smash");
            }
        }

        void PoseBatter(HeroActor.Pose pose, float charge, bool ring)
        {
            if (_match?.Batter == null) return;
            if (!_heroes.TryGetValue(_match.Batter.Id, out var b) || b == null) return;
            b.SetPose(pose, charge);
            b.SetChargeRing(ring ? charge : 0);
            b.SetHeld(pose is HeroActor.Pose.ChargeSwing or HeroActor.Pose.Swing, false);
            b.Place(new Vector3(2.55f, 0f, 2.4f), new Vector3(0f, 0f, 1f));
            b.Tick(0.08f);
        }

        void PosePitcher(HeroActor.Pose pose, float charge, bool ring)
        {
            if (_match?.Pitcher == null) return;
            if (!_heroes.TryGetValue(_match.Pitcher.Id, out var p) || p == null) return;
            p.SetPose(pose, charge, "fastball");
            p.SetChargeRing(ring ? charge : 0);
            p.Tick(0.08f);
        }
    }
}
