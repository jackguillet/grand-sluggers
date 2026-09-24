using System;
using System.IO;
using System.Reflection;
using GrandSluggers.Sim;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>Opt-in Harbor render and held/hidden lifecycle check. The look gate stays human.</summary>
    [InitializeOnLoad]
    public static class BallShadowGate
    {
        const string Pending = "GrandSluggers.BallShadowGate";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static BallShadowGate() { EditorApplication.update += Update; }

        [MenuItem("Grand Sluggers/Verify Ball Shadow")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in a dedicated Edit-mode worktree.");
            EditorSceneManager.OpenScene("Assets/Scenes/HarborDiamond.unity");
            SessionState.SetFloat(Pending + ".deadline", (float)EditorApplication.timeSinceStartup + 180);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        static void Update()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ball-shadow"));
            Directory.CreateDirectory(folder);
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Pending + ".deadline", 0))
            {
                SessionState.SetBool(Pending, false);
                File.WriteAllText(Path.Combine(folder, "result.txt"), "FAIL: Harbor startup timed out");
                return;
            }
            if (!EditorApplication.isPlaying) return;
            var play = UnityEngine.Object.FindAnyObjectByType<MatchDirector>();
            if (play == null || Get<Match>(play, "_match") == null) return;
            SessionState.SetBool(Pending, false);
            try
            {
                // Real contact, sim flight, actors and gameplay camera; no screenshot-only ball/camera placement.
                var content = Get<ContentCatalog>(play, "_content");
                var match = Get<Match>(play, "_match");
                Invoke(play, "BeginSet");
                Set(play, "_gateHold", true);
                var pitch = new PitchCommand("fastball", 0, false);
                var swing = new SwingCommand(true, 0, 0, false);
                Require(match.BeginAtBat(pitch, swing, out _, out _), "Could not start at-bat");
                var flight = BattedBall.Of(88, 32, -18, match.Park, content.Rules);
                var hit = new AtBatResult(ContactQuality.Nice, true, false, 88, 32, flight.LandingDist,
                    flight.HomeRun, false, null, null, SprayDeg: -18, Class: flight.Class);
                var preview = match.PreviewHit(hit);
                Set(play, "_pitch", pitch);
                Set(play, "_swing", swing);
                Set(play, "_pending", hit);
                Set(play, "_preview", preview);
                Set(play, "_cpuField", match.ResolveFielding(hit, preview));
                var ball = Get<ParkView>(play, "_park").Ball;
                ball.Release();
                Invoke(play, "StartFly", hit);
                var elapsed = 0f;
                foreach (var time in new[] { 1f, 2.5f, 4f })
                {
                    while (elapsed < time)
                    {
                        Invoke(play, "TickLive", 1f / 60);
                        Invoke(play, "DrawActors", 1f / 60);
                        Get<CameraRig>(play, "_rig").Tick(1f / 60);
                        elapsed += 1f / 60;
                    }
                    var shadow = Get<Transform>(ball, "_shadow");
                    Require(shadow.gameObject.activeSelf, "Airborne ball lost its shadow");
                    var p = Get<Vector3>(play, "_ball");
                    var center = shadow.TransformPoint(shadow.GetComponent<MeshFilter>().sharedMesh.vertices[0]);
                    Require(Mathf.Abs(center.x - p.x) < 0.001f && Mathf.Abs(center.z - p.z) < 0.001f,
                        "Shadow left live ball X/Z");
                    Require(center.y > ParkDiamond.GrassTop, "Shadow buried in grass");
                    Capture(Camera.main, Path.Combine(folder, "fly-" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png"));
                }
                var hold = new GameObject("ShadowGateGlove").transform;
                ball.Hold(hold);
                Require(!Get<Transform>(ball, "_shadow").gameObject.activeSelf, "Held shadow stayed visible");
                ball.Release();
                ball.Place(new Vector3(30, 12, 220), "", "fastball", false, true, true);
                Require(Get<Transform>(ball, "_shadow").gameObject.activeSelf, "Release lost shadow");
                ball.Hide();
                Require(!Get<Transform>(ball, "_shadow").gameObject.activeSelf, "Hidden shadow stayed visible");
                UnityEngine.Object.Destroy(hold.gameObject);
                File.WriteAllText(Path.Combine(folder, "result.txt"), "PASS: live flight projection, render captures, hold/release/hide. Human look gate remains open.");
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(folder, "result.txt"), "FAIL: " + e);
                Debug.LogException(e);
            }
        }

        static void Capture(Camera cam, string path)
        {
            var rt = new RenderTexture(1280, 800, 24);
            var previous = cam.targetTexture;
            var active = RenderTexture.active;
            var texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            cam.targetTexture = previous;
            RenderTexture.active = active;
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(texture);
        }

        static T Get<T>(object owner, string name) => (T)owner.GetType().GetField(name, Hidden).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Hidden).SetValue(owner, value);
        static void Invoke(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Hidden).Invoke(owner, args);
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
