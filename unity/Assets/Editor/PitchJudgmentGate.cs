using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GrandSluggers.Sim;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>Opt-in integration check of the actual final Flight tick and its called result.</summary>
    [InitializeOnLoad]
    public static class PitchJudgmentGate
    {
        const string Pending = "GrandSluggers.PitchJudgmentGate";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static PitchJudgmentGate() { EditorApplication.update += Update; }
        [MenuItem("Grand Sluggers/Verify Pitch Judgment")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use a dedicated Edit-mode worktree.");
            EditorSceneManager.OpenScene("Assets/Scenes/HarborDiamond.unity");
            SessionState.SetString(Pending, DateTime.UtcNow.ToString("O"));
            EditorApplication.isPlaying = true;
        }
        static void Update()
        {
            var pending = SessionState.GetString(Pending, "");
            if (pending == "") return;
            var play = UnityEngine.Object.FindFirstObjectByType<MatchDirector>();
            if (!EditorApplication.isPlaying || play == null || Get<Match>(play, "_match") == null)
            {
                if (DateTime.UtcNow - DateTime.Parse(pending).ToUniversalTime() < TimeSpan.FromSeconds(180)) return;
                SessionState.EraseString(Pending);
                Write(new Evidence { error = "Harbor did not initialize within 180 seconds." });
                return;
            }
            SessionState.EraseString(Pending);
            var evidence = new Evidence();
            try
            {
                var cases = new List<Case>();
                foreach (var type in new[] { "fastball", "changeup" })
                foreach (var charge in new[] { 0f, 1f })
                foreach (var curve in new[] { -1f, 0f, 1f })
                foreach (var star in new[] { false, true })
                {
                    var match = Match.Slice(Get<ContentCatalog>(play, "_content"), innings: 3, seed: 1);
                    Set(play, "_match", match); Invoke(play, "BeginSet"); Set(play, "_gateHold", true);
                    match.Play(new PitchCommand("fastball", 0, false), new SwingCommand(false, 0, 0, false));
                    match.Play(new PitchCommand("fastball", 0, false), new SwingCommand(false, 0, 0, false));
                    Require(match.Strikes == 2, "Could not establish two-strike fixture.");
                    var command = new PitchCommand(type, charge, star, BreakX: curve, RubberX: 0.3);
                    Invoke(play, "Launch", command);
                    Set(play, "_swing", new SwingCommand(false, 0, 0, false));
                    Set(play, "_flight", Get<float>(play, "_pitchDur"));
                    Set(play, "_pitchAir", true);
                    Set(play, "_breakX", curve);
                    var starId = match.Pitcher.StarPitch;
                    Invoke(play, "TickFlight", 0f);
                    var ball = Get<Vector3>(play, "_ball");
                    var result = Get<PlayEvent>(play, "_last");
                    Require(result != null, "Final flight tick did not produce a taken-pitch result.");
                    var actual = PitchFlight.Point(result.Pitch, 1, starId);
                    Require(Math.Abs(ball.x - actual.X) < 0.0001 && Math.Abs(ball.y - actual.Y) < 0.0001,
                        "Rendered crossing differs from recorded delivery.");
                    var inside = StrikeZoneGeometry.Contains(ball.x, ball.y);
                    Require(result.AtBat.InZone == inside, "Umpire differs from visible zone.");
                    Require(inside == (result.Kind == PlayKind.Strikeout), "Wrong taken third-strike result.");
                    // The aim tell is the same crossing (#577): drawn on the pitching seat from PitchFlight.Crossing.
                    var zone = Get<StrikeZone>(play, "_zone");
                    var tell = (Transform)typeof(StrikeZone).GetField("_aim", Hidden).GetValue(zone);
                    var tellOn = tell != null && tell.gameObject.activeSelf;
                    var humanPitches = (bool)typeof(MatchDirector).GetProperty("HumanPitches", Hidden).GetValue(play);
                    Require(!tellOn || humanPitches, "Aim tell shown to the batting seat.");
                    if (tellOn)
                        Require(Math.Abs(tell.localPosition.x - actual.X) < 0.0001 && Math.Abs(tell.localPosition.y - actual.Y) < 0.0001,
                            "Aim tell differs from the delivered crossing.");
                    cases.Add(new Case { type = type, charge = charge, curve = curve, star = star, x = ball.x, y = ball.y,
                        inside = inside, result = result.Kind.ToString(), tell = tellOn });
                }
                evidence.cases = cases.ToArray(); evidence.ok = true;
            }
            catch (Exception ex) { evidence.error = ex.ToString(); Debug.LogException(ex); }
            Write(evidence);
        }
        static void Write(Evidence evidence)
        {
            evidence.revision = Environment.GetEnvironmentVariable("GS_VALIDATION_REVISION") ?? "";
            evidence.unityVersion = Application.unityVersion;
            var path = Environment.GetEnvironmentVariable("GS_PITCH_EVIDENCE") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "pitch-judgment.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
        }
        static T Get<T>(MatchDirector p, string field) => (T)typeof(MatchDirector).GetField(field, Hidden).GetValue(p);
        static void Set(MatchDirector p, string field, object value) => typeof(MatchDirector).GetField(field, Hidden).SetValue(p, value);
        static void Invoke(MatchDirector p, string method, params object[] args) => typeof(MatchDirector).GetMethod(method, Hidden).Invoke(p, args);
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        [Serializable] sealed class Evidence { public bool ok; public string revision; public string unityVersion; public string error; public Case[] cases; }
        [Serializable] sealed class Case { public string type; public float charge; public float curve; public bool star; public float x; public float y; public bool inside; public string result; public bool tell; }
    }
}
