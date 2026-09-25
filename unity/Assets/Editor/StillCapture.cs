using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using GrandSluggers.Sim.Tooling;
using Motion = GrandSluggers.Sim.Motion;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Consumes <see cref="StillRequest"/> from unity/Temp in the editor's Play mode (#1043). Editor-only: a shipped player
    /// carries no capture and polls nothing. <see cref="StillCaptureHook"/> creates it only when Play starts with a request
    /// file waiting (the Grand Sluggers menu and <c>tools/still-gate*.sh</c> write one). Camera.Render PNGs — world only,
    /// no OnGUI — so HUD-off stills are honest.
    /// </summary>
    public sealed class StillCapture : MonoBehaviour
    {
        MatchDirector _play;
        StillRequest _req;
        bool _ran;
        string _temp = "";
        string _loadError = "";

        void Start() => _temp = TempDir();

        void Update()
        {
            if (_ran || _req != null) return;
            if (_play == null) _play = FindAnyObjectByType<MatchDirector>();
            // The director's catalog declares the parks, so the gate waits the
            // frame it takes to load: a park id the catalog does not have is
            // refused by name, not captured at the default park.
            if (_play == null || _play.GateContent == null) return;
            _temp = TempDir();
            if (!StillRequest.TryLoad(_temp, _play.GateContent, out _req, out var loadError))
            {
                // No request is the idle state; a request the parser refuses — an
                // unknown shot, an unknown park — says so once instead of waiting.
                if (!loadError.StartsWith("missing ", StringComparison.Ordinal) && loadError != _loadError)
                {
                    _loadError = loadError;
                    Debug.LogError("Grand Sluggers still gate: " + loadError);
                }
                return;
            }
            _play.CaptureMuteHud = _req.HudOff;
            StartCoroutine(Run());
        }

        void OnDisable()
        {
            if (_play != null) _play.CaptureMuteHud = false;
        }

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
            var park = ExhibitionPick.DefaultPark;
            try
            {
                park = _req.ResolvedPark(_play.GateContent);
                shots = _req.ResolvedShots();
                swingCaptains = _req.ResolvedSwingCaptains(_play.GateContent);
            }
            catch (Exception ex)
            {
                WriteDone(_temp, outDir, false, files, swingMetrics, park, _req.Night, ex.Message);
                enabled = false;
                yield break;
            }

            Directory.CreateDirectory(outDir);
            var cam = _play.GateCam;
            var w = _req.ResolvedWidth();
            var h = _req.ResolvedHeight();
            // The scene's park and night are Jack's pick. Capture at the park the
            // request names, then hand the pick back so Play is where he left it.
            var prevPark = _play.ParkId;
            var prevNight = _play.Night;
            _play.GateUsePark(park, _req.Night);
            try
            {
                foreach (var shot in shots)
                {
                    _play.GateStage(shot, _req);
                    if (StillRequest.IsSwingMatrixShot(shot))
                    {
                        foreach (var captain in swingCaptains)
                        foreach (var hand in new[] { Hand.R, Hand.L })
                        {
                            _play.GateStageSwingCaptain(captain, _req, hand);
                            for (var i = 0; i < 24; i++) yield return null;
                            foreach (var power in new[] { (Id: "normal", Charge: 0f), (Id: "max", Charge: 1f) })
                            foreach (var beat in new[] { "ready", "load", "contact", "follow", "finish" })
                            {
                                _play.GatePoseSwing(beat, power.Charge);
                                for (var i = 0; i < 4; i++) yield return null;
                                // ActorDirector continues drawing SET while simulation is held.
                                // Reapply the exact pose on the capture frame, after those draws.
                                var hero = _play.GatePoseSwing(beat, power.Charge);
                                var matrixPng = StillRequest.SwingPngPath(outDir, captain + "-" + hand, power.Id, beat);
                                try
                                {
                                    Capture(_play.GateCam != null ? _play.GateCam : cam, matrixPng, w, h);
                                    files.Add(matrixPng);
                                    swingMetrics.Add(_play.GateMeasureSwing(
                                        hero, beat, captain + "-" + hand, power.Id, out var metricError));
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
                    var png = StillRequest.PngPath(outDir, shot, _req.ResolvedHome(), park, _req.Night);
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
            }
            finally
            {
                _play.GateRestorePark(prevPark, prevNight);
            }

            var doneError = error ?? string.Join("; ", swingErrors);
            WriteDone(_temp, outDir, error == null && swingErrors.Count == 0,
                files, swingMetrics, park, _req.Night, doneError);
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

        static void WriteDone(string temp, string outDir, bool ok, List<string> files, List<string> swingMetrics,
            string park, bool night, string error)
        {
            var json = "{\"ok\":" + (ok ? "true" : "false")
                + ",\"park\":\"" + park + "\",\"night\":" + (night ? "true" : "false")
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

    /// <summary>
    /// Creates the capture when Play starts and a still request is waiting in unity/Temp, and not otherwise: the gate
    /// polls only when asked.
    /// </summary>
    [InitializeOnLoad]
    static class StillCaptureHook
    {
        static StillCaptureHook() => EditorApplication.playModeStateChanged += OnPlayMode;

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            var temp = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath)!.FullName, "Temp");
            if (!File.Exists(StillRequest.RequestPath(temp))) return;
            new GameObject("Still Capture").AddComponent<StillCapture>();
        }
    }
}
