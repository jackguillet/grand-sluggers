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
    /// <summary>Opt-in real TickLive regression in Harbor Play mode; never attached to a player.</summary>
    [InitializeOnLoad]
    public static class LivePlayLifecycleGate
    {
        const string Pending = "GrandSluggers.LivePlayLifecycleGate";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static LivePlayLifecycleGate() { EditorApplication.update += Update; }

        [MenuItem("Grand Sluggers/Verify Live Play Lifecycle")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Run this gate from Edit mode in a dedicated validation worktree.");
            EditorSceneManager.OpenScene("Assets/Scenes/HarborDiamond.unity");
            SessionState.SetFloat(Pending + ".deadline", (float)EditorApplication.timeSinceStartup + 180f);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        static void Update()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Pending + ".deadline", 0))
            {
                SessionState.SetBool(Pending, false);
                Write(new Evidence { error = "Harbor Play mode did not initialize within 180 seconds." });
                Debug.LogError("Grand Sluggers lifecycle gate: Harbor startup timed out.");
                return;
            }
            if (!EditorApplication.isPlaying) return;
            var play = UnityEngine.Object.FindFirstObjectByType<MatchDirector>();
            if (play == null || Get<Match>(play, "_match") == null) return;
            SessionState.SetBool(Pending, false);
            var evidence = new Evidence { revision = Environment.GetEnvironmentVariable("GS_VALIDATION_REVISION") ?? "",
                unityVersion = Application.unityVersion };
            try
            {
                var cases = new List<Case>();
                foreach (var human in new[] { false, true })
                {
                    cases.Add(Verify(evidence, play, human, false, false));
                    cases.Add(Verify(evidence, play, human, true, false));
                    cases.Add(Verify(evidence, play, human, false, true));
                    cases.Add(Verify(evidence, play, human, true, false, walkoff: true));
                }
                evidence.cases = cases.ToArray();
                evidence.ok = true;
                Debug.Log("Grand Sluggers live play lifecycle OK: " + cases.Count + " real TickLive cases.");
            }
            catch (Exception ex) { evidence.error = ex.ToString(); Debug.LogException(ex); }
            Write(evidence);
        }

        static void Write(Evidence evidence)
        {
            var output = Environment.GetEnvironmentVariable("GS_LIVE_PLAY_EVIDENCE") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "live-play-lifecycle.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonUtility.ToJson(evidence, true));
        }

        static Case Verify(Evidence evidence, MatchDirector play, bool human, bool loaded, bool robbed, bool walkoff = false)
        {
            var entry = new Case { human = human, loaded = loaded, robbed = robbed, walkoff = walkoff,
                phase = "initializing" };
            evidence.activeCase = entry;
            var match = Match.Slice(Get<ContentCatalog>(play, "_content"), innings: walkoff ? 1 : 3, seed: 1);
            if (walkoff) match.SkipToHomeCaptainAtBat();
            Set(play, "_match", match);
            Invoke(play, "BeginSet");
            Set(play, "_gateHold", true);
            if (loaded)
                for (var bag = 1; bag <= 3; bag++)
                    Require(match.StationRunner(bag, match.Offense.Roster[bag]), "Could not load bases.");
            var pitch = new PitchCommand("fastball", 0, 0, false);
            var swing = new SwingCommand(true, 0, 0, false);
            Require(match.BeginAtBat(pitch, swing, out var hit, out _), "Fixture did not enter contact.");
            hit = hit with { HomeRun = true, CarryFt = BallFlight.CarryFeet(110, 35, match.Park.WindMph), ExitVeloMph = 110, LaunchDeg = 35, SprayDeg = 0 };
            var preview = match.PreviewHit(hit);
            var field = new FieldingResult(PlayKind.HomeRun, null, null, preview.HangTimeSec,
                preview.LandingX, preview.LandingZ, false, false);
            Set(play, "_pitch", pitch);
            Set(play, "_swing", swing);
            Set(play, "_pending", hit);
            Set(play, "_preview", preview);
            Set(play, "_cpuField", human ? null : field);
            Set(play, "_playerFielding", human);
            Invoke(play, "InitGloves");
            Invoke(play, "StartFly", hit);
            var batter = match.Batter.Id;
            var hang = BallFlight.HangTime(Get<Sample[]>(play, "_path"));
            var elapsed = 0f;
            var caught = false;
            while (Phase(play) == "InPlay" && elapsed < hang + 5)
            {
                // Supply the catch observation at the real wall window; the production
                // fielding/Time/commit path must preserve it, never award a homer.
                if (robbed && !caught && elapsed >= hang - 0.12)
                {
                    Require(match.AwayScore == 0, "Homer committed before wall-catch opportunity.");
                    Set(play, "_caught", true);
                    Set(play, "_buddy", true);
                    if (!human) Set(play, "_cpuField", field with { Kind = PlayKind.FlyOut, Fielder = preview.Fielder });
                    caught = true;
                }
                Invoke(play, "TickLive", 1f / 60f);
                elapsed += 1f / 60f;
                entry.elapsed = elapsed;
                entry.phase = Phase(play);
                entry.score = walkoff ? match.HomeScore : match.AwayScore;
                entry.liveTime = match.LivePlay.ElapsedSeconds;
                entry.liveKind = ((PlayKind)typeof(MatchDirector).GetMethod("LiveKind", Hidden)!.Invoke(play, null)).ToString();
                entry.caught = Get<bool>(play, "_caught");
                entry.buddy = Get<bool>(play, "_buddy");
                entry.paused = match.Paused;
                entry.active = match.LivePlay.Active;
                entry.playerFielding = Get<bool>(play, "_playerFielding");
                entry.pending = Get<AtBatResult>(play, "_pending") != null;
                entry.throwing = Get<bool>(play, "_throwing");
                entry.effect = Get<bool>(play, "_itemFlying");
                entry.recoil = Get<float>(play, "_recoilT");
                entry.closePlay = Get<bool>(play, "_closePlay");
            }
            Require(Phase(play) == "Result", "Live ball did not reach Result: " + Phase(play));
            var result = Get<PlayEvent>(play, "_last");
            Require(result != null && result.Kind == (robbed ? PlayKind.FlyOut : PlayKind.HomeRun), "Wrong result.");
            var expected = robbed ? 0 : loaded ? 4 : 1;
            Require((walkoff ? match.HomeScore : match.AwayScore) == expected, "Wrong score.");
            Require(match.Batter.Id != batter, "Batter did not advance.");
            var logs = match.Log.Count;
            for (var i = 0; i < 10; i++) Invoke(play, "TickLive", 1f / 60f);
            Require(match.Log.Count == logs && (walkoff ? match.HomeScore : match.AwayScore) == expected, "Result applied twice.");
            Set(play, "_t", 100f);
            Invoke(play, "TickFlow");
            Require(Phase(play) == (walkoff ? "GameOver" : "Set"), "Result did not reach next SET/game over.");
            Require(match.LivePlay.ElapsedSeconds == 0, "Next SET retained prior clock.");
            return new Case { human = human, loaded = loaded, robbed = robbed, walkoff = walkoff, elapsed = elapsed,
                kind = result.Kind.ToString(), score = walkoff ? match.HomeScore : match.AwayScore, phase = Phase(play), nextBatter = match.Batter.Id };
        }

        static string Phase(MatchDirector p) => Get<object>(p, "_phase").ToString();
        static T Get<T>(MatchDirector p, string name) => (T)typeof(MatchDirector).GetField(name, Hidden)!.GetValue(p);
        static void Set(MatchDirector p, string name, object value) => typeof(MatchDirector).GetField(name, Hidden)!.SetValue(p, value);
        static void Invoke(MatchDirector p, string name, params object[] args) => typeof(MatchDirector).GetMethod(name, Hidden)!.Invoke(p, args);
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        [Serializable] sealed class Evidence { public string revision; public string unityVersion; public bool ok; public string error; public Case activeCase; public Case[] cases; }
        [Serializable] sealed class Case { public bool human; public bool loaded; public bool robbed; public bool walkoff; public float elapsed; public string kind; public int score; public string phase; public string nextBatter;
            public double liveTime; public string liveKind; public bool caught; public bool buddy; public bool paused;
            public bool active; public bool playerFielding; public bool pending; public bool throwing; public bool effect;
            public float recoil; public bool closePlay; }
    }
}
