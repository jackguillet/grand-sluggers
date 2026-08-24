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
            try
            {
                var shots = _req.ResolvedShots();
                _play.GateSkipToSet(_req.ResolvedHome(), _req.ResolvedAway(), (float)_req.Charge01, _req.FeelDebug, _req.HudOff);
                for (var i = 0; i < 12; i++) yield return null;

                var outDir = _req.ResolvedOutDir(_temp);
                Directory.CreateDirectory(outDir);
                var cam = _play.GateCam;
                var w = _req.ResolvedWidth();
                var h = _req.ResolvedHeight();
                foreach (var shot in shots)
                {
                    _play.GateCut(shot);
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    var png = StillRequest.PngPath(outDir, shot);
                    Capture(cam, png, w, h);
                    files.Add(png);
                }

                WriteDone(_temp, true, files, "");
                try { File.Delete(StillRequest.RequestPath(_temp)); }
                catch { /* leftover request is ok */ }
            }
            catch (Exception ex)
            {
                WriteDone(_temp, false, files, ex.Message);
            }
            enabled = false;
        }

        static void Capture(Camera cam, string path, int w, int h)
        {
            if (cam == null) throw new System.InvalidOperationException("no camera for still " + path);
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
            File.WriteAllBytes(path, tex.EncodeToPNG());
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

        internal void GateSkipToSet(string home, string away, float charge, bool feelDebug, bool muteHud)
        {
            _mode = PlayMode.Exhibition;
            HomeCaptain = home;
            AwayCaptain = away;
            _forceMuteHud = muteHud;
            _feelDebug = feelDebug;
            _showTiming = false;
            _freezeCam = true;
            _charge = Mathf.Clamp01(charge);
            _match = NewMatch();
            _park.Build(_match.Park, _match.Night);
            _spec.Build(transform);
            _items.Build(transform);
            _stars?.Build(transform);
            BeginSet();
        }

        internal void GateCut(string shot)
        {
            _freezeCam = true;
            if (_cam == null) return;
            _cam.Cut(shot);
        }
    }
}
