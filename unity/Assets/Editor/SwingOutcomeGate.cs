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
    /// <summary>
    /// Opt-in Play-mode gate for committed swing presentation across the real
    /// TickFlight -> Resolve -> Result and DrawActors path.
    /// </summary>
    [InitializeOnLoad]
    public static class SwingOutcomeGate
    {
        const string Pending = "GrandSluggers.SwingOutcomeGate";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const float Step = 1f / 60f;
        const double LateFrames = 12;
        const double EarlyFrames = -36;

        static SwingOutcomeGate() { EditorApplication.update += Update; }

        [MenuItem("Grand Sluggers/Verify Swing Outcomes")]
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
                Debug.LogError("Grand Sluggers swing outcome gate: Harbor startup timed out.");
                EditorApplication.isPlaying = false;
                return;
            }
            if (!EditorApplication.isPlaying) return;
            var play = UnityEngine.Object.FindFirstObjectByType<MatchDirector>();
            if (play == null || Get<Match>(play, "_match") == null) return;

            SessionState.SetBool(Pending, false);
            var evidence = new Evidence
            {
                revision = Environment.GetEnvironmentVariable("GS_VALIDATION_REVISION") ?? "",
                unityVersion = Application.unityVersion
            };
            try
            {
                var output = OutputPath();
                var frameDir = Path.Combine(
                    Path.GetDirectoryName(output)!, Path.GetFileNameWithoutExtension(output) + "-frames");
                Directory.CreateDirectory(frameDir);
                var cases = new List<GateCase>();
                foreach (var captain in new[]
                {
                    (Id: "rio", Rig: "shared"),
                    (Id: "zig", Rig: "shared"),
                    (Id: "fenn", Rig: "generic")
                })
                foreach (var power in new[] { (Id: "normal", Charge: 0f), (Id: "max", Charge: 1f) })
                {
                    cases.Add(VerifyCommitted(play, captain.Id, captain.Rig,
                        power.Id, power.Charge, "late-whiff", LateFrames, strikeout: false, frameDir));
                    cases.Add(VerifyCommitted(play, captain.Id, captain.Rig,
                        power.Id, power.Charge, "swinging-strikeout", LateFrames, strikeout: true, frameDir));
                    cases.Add(VerifyCommitted(play, captain.Id, captain.Rig,
                        power.Id, power.Charge, "early-whiff", EarlyFrames, strikeout: false, frameDir));
                }
                foreach (var captain in new[]
                {
                    (Id: "rio", Rig: "shared"),
                    (Id: "zig", Rig: "shared"),
                    (Id: "fenn", Rig: "generic")
                })
                    cases.Add(VerifyCalledStrikeout(play, captain.Id, captain.Rig, frameDir));

                evidence.cases = cases.ToArray();
                evidence.ok = true;
                Debug.Log("Grand Sluggers swing outcome OK: " + cases.Count
                    + " real Resolve/DrawActors cases.");
            }
            catch (Exception ex)
            {
                evidence.error = ex.ToString();
                Debug.LogException(ex);
            }
            finally
            {
                Write(evidence);
                EditorApplication.isPlaying = false;
            }
        }

        static GateCase VerifyCommitted(
            MatchDirector play, string captain, string rig, string power, float charge,
            string scenario, double timingFrames, bool strikeout, string frameDir)
        {
            var fixture = Setup(play, captain, strikeout);
            var pitch = new PitchCommand("fastball", 0, 0, false);
            Invoke(play, "Launch", pitch);
            var pitchDur = Get<float>(play, "_pitchDur");
            var swing = new SwingCommand(true, charge, timingFrames, false);
            Set(play, "_swing", swing);
            Set(play, "_swung", true);
            Set(play, "_pitchAir", true);
            Set(play, "_flight", (float)AtBatMotion.SwingStart(pitchDur, timingFrames));
            Invoke(play, "DrawActors", 0f);

            var result = new GateCase
            {
                name = captain + "-" + power + "-" + scenario,
                captain = captain,
                hand = fixture.Batter.Bats.ToString(),
                rig = rig,
                power = power,
                charge = charge,
                scenario = scenario,
                timingFrames = timingFrames
            };
            var hero = Hero(play, fixture.Batter.Id);
            Require(hero.Current == HeroActor.Pose.Swing,
                result.name + ": committed start did not enter Swing.");
            var startT = Get<float>(play, "_committedSwingT");
            Require(Math.Abs(startT) < 0.001f,
                result.name + ": action did not start at zero: " + startT);
            result.frames.Add(CaptureFrame(play, hero, result.name, "start", frameDir));

            var previous = startT;
            var sawContact = false;
            var sawFollow = false;
            var resolved = false;
            var swingToMiss = 0;
            var lastPose = hero.Current;
            for (var frame = 0; frame < 180; frame++)
            {
                if (Phase(play) == "Flight")
                    Invoke(play, "TickFlight", Step);
                Invoke(play, "DrawActors", Step);
                hero = Hero(play, fixture.Batter.Id);
                var actionT = Get<float>(play, "_committedSwingT");
                Require(actionT + 0.0001f >= previous,
                    result.name + ": committed action clock moved backward.");
                previous = actionT;
                if (lastPose == HeroActor.Pose.Swing && hero.Current == HeroActor.Pose.Miss)
                    swingToMiss++;
                lastPose = hero.Current;
                resolved |= Phase(play) == "Result";

                if (!sawContact && actionT >= MoveBones.SwingContact - 0.0001f
                    && actionT <= MoveBones.SwingDur)
                {
                    Require(hero.Current == HeroActor.Pose.Swing,
                        result.name + ": left Swing before authored contact.");
                    result.frames.Add(CaptureFrame(play, hero, result.name, "contact", frameDir));
                    sawContact = true;
                    result.contactPhase = Phase(play);
                    result.contactT = hero.PoseTime;
                }
                if (!sawFollow && Math.Abs(actionT - MoveBones.SwingDur) < 0.0001f)
                {
                    Require(hero.Current == HeroActor.Pose.Swing,
                        result.name + ": left Swing before authored follow-through.");
                    result.frames.Add(CaptureFrame(play, hero, result.name, "follow", frameDir));
                    sawFollow = true;
                    result.followPhase = Phase(play);
                    result.followT = hero.PoseTime;
                }
                if (resolved && actionT > MoveBones.SwingDur + 0.0001f)
                    break;
            }

            var last = Get<PlayEvent>(play, "_last");
            var expected = strikeout ? PlayKind.Strikeout : PlayKind.SwingMiss;
            Require(last != null && last.Kind == expected,
                result.name + ": expected " + expected + ", got " + last?.Kind);
            Require(sawContact && sawFollow,
                result.name + ": did not present both contact and follow-through.");
            Require(Phase(play) == "Result", result.name + ": pitch did not resolve to Result.");
            Require(hero.Current == HeroActor.Pose.Miss,
                result.name + ": completed whiff did not settle on Miss.");

            for (var frame = 0; frame < 6; frame++)
            {
                Invoke(play, "DrawActors", Step);
                hero = Hero(play, fixture.Batter.Id);
                Require(hero.Current == HeroActor.Pose.Miss,
                    result.name + ": completed swing restarted during Result.");
            }
            Require(swingToMiss == 1,
                result.name + ": expected one Swing-to-Miss transition, got " + swingToMiss);
            result.eventKind = last.Kind.ToString();
            result.resultPose = hero.Current.ToString();
            result.finalActionT = Get<float>(play, "_committedSwingT");
            return result;
        }

        static GateCase VerifyCalledStrikeout(
            MatchDirector play, string captain, string rig, string frameDir)
        {
            var fixture = Setup(play, captain, strikeout: true);
            Invoke(play, "Launch", new PitchCommand("fastball", 0, 0, false));
            Set(play, "_swing", new SwingCommand(false, 0, 0, false));
            Set(play, "_swung", false);
            Set(play, "_pitchAir", true);
            Set(play, "_flight", Get<float>(play, "_pitchDur") - Step);
            Invoke(play, "TickFlight", Step);
            Invoke(play, "DrawActors", Step);

            var hero = Hero(play, fixture.Batter.Id);
            var last = Get<PlayEvent>(play, "_last");
            Require(last != null && last.Kind == PlayKind.Strikeout,
                captain + " called strikeout did not resolve as Strikeout.");
            Require(hero.Current != HeroActor.Pose.Swing,
                captain + " called strikeout entered Swing.");
            Require(Get<float>(play, "_committedSwingT") == (float)AtBatMotion.SwingNotStarted,
                captain + " called strikeout armed a committed action clock.");
            var result = new GateCase
            {
                name = captain + "-called-strikeout",
                captain = captain,
                hand = fixture.Batter.Bats.ToString(),
                rig = rig,
                power = "none",
                scenario = "called-strikeout",
                eventKind = last.Kind.ToString(),
                resultPose = hero.Current.ToString(),
                finalActionT = Get<float>(play, "_committedSwingT")
            };
            result.frames.Add(CaptureFrame(play, hero, result.name, "result", frameDir));
            return result;
        }

        static (Match Match, Character Batter) Setup(
            MatchDirector play, string captain, bool strikeout)
        {
            play.HomeCaptain = captain;
            play.AwayCaptain = captain == "ashlord" ? "brondo" : "ashlord";
            var content = Get<ContentCatalog>(play, "_content");
            var match = Match.Exhibition(content, play.HomeCaptain, play.AwayCaptain,
                innings: 3, seed: 548, parkId: "harbor-diamond");
            match.SkipToHomeCaptainAtBat();
            if (strikeout)
            {
                var paint = new PitchCommand("fastball", 0, 0, false);
                var take = new SwingCommand(false, 0, 0, false);
                match.Play(paint, take);
                match.Play(paint, take);
                Require(match.Strikes == 2, captain + ": strikeout fixture did not reach two strikes.");
            }
            var batter = match.Batter;
            Set(play, "_match", match);
            Invoke(play, "BeginSet");
            Set(play, "_gateHold", true);
            Invoke(play, "DrawActors", 0f);
            return (match, batter);
        }

        static Frame CaptureFrame(
            MatchDirector play, HeroActor hero, string caseName, string beat, string frameDir)
        {
            var path = Path.Combine(frameDir, caseName + "-" + beat + ".png");
            var rig = Get<CameraRig>(play, "_rig");
            Capture(rig != null ? rig.Cam : Camera.main, path, 960, 540);
            return new Frame
            {
                beat = beat,
                phase = Phase(play),
                pose = hero.Current.ToString(),
                poseT = hero.PoseTime,
                actionT = Get<float>(play, "_committedSwingT"),
                png = path.Replace("\\", "/")
            };
        }

        static void Capture(Camera cam, string path, int width, int height)
        {
            if (cam == null) throw new InvalidOperationException("Swing outcome gate has no camera.");
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var before = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            cam.targetTexture = before;
            RenderTexture.active = null;
            File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        static HeroActor Hero(MatchDirector play, string id)
        {
            var heroes = Get<Dictionary<string, HeroActor>>(play, "_heroes");
            Require(heroes.TryGetValue(id, out var hero) && hero != null,
                "Missing rendered batter " + id + ".");
            return hero;
        }

        static string OutputPath() =>
            Environment.GetEnvironmentVariable("GS_SWING_OUTCOME_EVIDENCE") ??
            Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "swing-outcome-gate.json");

        static void Write(Evidence evidence)
        {
            var output = OutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonUtility.ToJson(evidence, true));
        }

        static string Phase(MatchDirector play) => Get<object>(play, "_phase").ToString();
        static T Get<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, Hidden)!.GetValue(owner);
        static void Set(object owner, string name, object value) =>
            owner.GetType().GetField(name, Hidden)!.SetValue(owner, value);
        static void Invoke(object owner, string name, params object[] args) =>
            owner.GetType().GetMethod(name, Hidden)!.Invoke(owner, args);
        static void Require(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }

        [Serializable]
        sealed class Evidence
        {
            public string revision;
            public string unityVersion;
            public bool ok;
            public string error;
            public GateCase[] cases;
        }

        [Serializable]
        sealed class GateCase
        {
            public string name;
            public string captain;
            public string hand;
            public string rig;
            public string power;
            public string scenario;
            public double charge;
            public double timingFrames;
            public string eventKind;
            public string contactPhase;
            public string followPhase;
            public double contactT;
            public double followT;
            public double finalActionT;
            public string resultPose;
            public List<Frame> frames = new List<Frame>();
        }

        [Serializable]
        sealed class Frame
        {
            public string beat;
            public string phase;
            public string pose;
            public double poseT;
            public double actionT;
            public string png;
        }
    }
}
