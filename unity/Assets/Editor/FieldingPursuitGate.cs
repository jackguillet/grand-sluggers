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
    /// <summary>Opt-in real TickLive pursuit regression in Harbor Play mode; never attached to a player.</summary>
    [InitializeOnLoad]
    public static class FieldingPursuitGate
    {
        const string Pending = "GrandSluggers.FieldingPursuitGate";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const float Dt = 1f / 60f;
        static FieldingPursuitGate() { EditorApplication.update += Update; }

        [MenuItem("Grand Sluggers/Verify Fielding Pursuit")]
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
                Debug.LogError("Grand Sluggers fielding pursuit gate: Harbor startup timed out.");
                return;
            }
            if (!EditorApplication.isPlaying) return;
            var play = UnityEngine.Object.FindFirstObjectByType<MatchDirector>();
            if (play == null || Get<Match>(play, "_match") == null) return;
            SessionState.SetBool(Pending, false);
            var evidence = new Evidence
            {
                revision = Environment.GetEnvironmentVariable("GS_VALIDATION_REVISION") ?? "",
                unityVersion = Application.unityVersion,
                utc = DateTime.UtcNow.ToString("O")
            };
            try
            {
                var cases = new List<Case>();
                cases.Add(Verify(evidence, play, new Spec("ground-low-left", 68, 8, -28, true, false)));
                cases.Add(Verify(evidence, play, new Spec("ground-low-center", 68, 8, 0, true, false)));
                cases.Add(Verify(evidence, play, new Spec("ground-low-right", 68, 8, 28, true, false)));
                cases.Add(Verify(evidence, play, new Spec("ground-high-left", 102, 8, -28, true, false)));
                cases.Add(Verify(evidence, play, new Spec("ground-high-center", 102, 8, 0, true, false)));
                cases.Add(Verify(evidence, play, new Spec("ground-high-right", 102, 8, 28, true, false)));
                cases.Add(Verify(evidence, play, new Spec("routine-fly", 88, 32, -18, false, false)));
                cases.Add(Verify(evidence, play, new Spec("uncaught-wall", 112, 34, 0, false, true)));
                evidence.cases = cases.ToArray();
                evidence.activeCase = null;
                evidence.ok = true;
                Debug.Log("Grand Sluggers fielding pursuit OK: " + cases.Count + " real TickLive routes.");
            }
            catch (Exception ex) { evidence.error = ex.ToString(); Debug.LogException(ex); }
            Write(evidence);
        }

        static Case Verify(Evidence evidence, MatchDirector play, Spec spec)
        {
            var entry = new Case
            {
                name = spec.Name,
                exitVelo = spec.ExitVelo,
                launch = spec.Launch,
                spray = spec.Spray,
                ground = spec.Ground,
                wall = spec.Wall,
                phase = "initializing"
            };
            evidence.activeCase = entry;
            var content = Get<ContentCatalog>(play, "_content");
            var match = Match.Slice(content, parkId: "harbor-diamond", innings: 3, seed: 17);
            Set(play, "_match", match);
            Invoke(play, "BeginSet");
            Set(play, "_gateHold", true);

            var pitch = new PitchCommand("fastball", 0, 0, false);
            var swing = new SwingCommand(true, 0, 0, false);
            Require(match.BeginAtBat(pitch, swing, out _, out _), "Fixture did not enter contact.");
            // The one flight (spec §5.6): the fixture is a contact; the park says what it does.
            var flight = BattedBall.Of(spec.ExitVelo, spec.Launch, spec.Spray, match.Park, content.Rules);
            Require(!spec.Wall || flight.HomeRun, "Wall fixture did not clear the Harbor fence.");
            Require(!flight.Foul, "Fixture went foul.");
            var hit = new AtBatResult(
                ContactQuality.Nice, true, false, spec.ExitVelo, spec.Launch, flight.LandingDist,
                flight.HomeRun, false, null, null, SprayDeg: spec.Spray, Class: flight.Class);
            var preview = match.PreviewHit(hit);
            Require(preview.Grounder == spec.Ground, "Fixture trajectory classification changed.");
            Require(!spec.Wall || preview.HomeRunLikely, "Wall fixture lost its wall plant.");
            var field = spec.Wall
                ? new FieldingResult(PlayKind.HomeRun, null, null, preview.HangTimeSec,
                    preview.LandingX, preview.LandingZ, false, false)
                : match.ResolveFielding(hit, preview);

            Set(play, "_pitch", pitch);
            Set(play, "_swing", swing);
            Set(play, "_pending", hit);
            Set(play, "_preview", preview);
            Set(play, "_cpuField", field);
            Set(play, "_playerFielding", false);
            Invoke(play, "InitGloves");
            Invoke(play, "StartFly", hit);

            var unityPath = Get<Sample[]>(play, "_path");
            var map = FieldingResolver.Assign(match.Defense.Roster, match.Pitcher);
            var initialPos = Get<string>(play, "_glovePos");
            var initialWho = map[initialPos];
            var initialAt = Get<Dictionary<string, (double X, double Z)>>(play, "_gloveAt")[initialPos];
            var initialSpeed = FieldingResolver.ChaseSpeedFt(initialWho, preview.Frozen);
            var initialRoute = FieldingPursuit.Plan(
                preview, match.Park, unityPath, 0, initialAt.X, initialAt.Z, initialSpeed);
            entry.initialFielder = initialWho.Id;
            entry.initialPosition = initialPos;
            entry.initialTargetX = initialRoute.X;
            entry.initialTargetZ = initialRoute.Z;
            entry.initialReachable = initialRoute.Reachable;
            if (spec.Ground)
                Require(initialRoute.Reachable, "Ground fixture has no reachable planned intercept.");
            if (spec.Wall)
            {
                var plant = FlyCatch.ChaseTarget(preview, match.Park);
                Require(initialRoute.AirCatch, "Wall route was not treated as an air catch.");
                Require(Diamond.Dist(initialRoute.X, initialRoute.Z, plant.X, plant.Z) < 0.01,
                    "Wall route did not use the legal wall plant.");
            }

            var frames = new List<Frame>();
            var elapsed = 0f;
            var tick = 0;
            var pickedUp = false;
            var maxRunStep = 0d;
            var maxAllowedStep = 0d;
            var deadline = Math.Max(12, BallFlight.RestTime(unityPath) + 8);
            while (Phase(play) == "InPlay" && elapsed < deadline)
            {
                var beforeOwner = Get<string>(play, "_glovePos");
                var beforeAt = new Dictionary<string, (double X, double Z)>(
                    Get<Dictionary<string, (double X, double Z)>>(play, "_gloveAt"));
                var beforeCaught = Get<bool>(play, "_caught") || Get<bool>(play, "_buddy");
                Invoke(play, "TickLive", Dt);
                elapsed += Dt;
                tick++;

                var afterOwner = Get<string>(play, "_glovePos");
                var afterX = Get<double>(play, "_fx");
                var afterZ = Get<double>(play, "_fz");
                var afterCaught = Get<bool>(play, "_caught") || Get<bool>(play, "_buddy");
                if (!beforeCaught && beforeAt.TryGetValue(afterOwner, out var before))
                {
                    var step = Diamond.Dist(before.X, before.Z, afterX, afterZ);
                    var who = map.TryGetValue(afterOwner, out var active) ? active : preview.Fielder;
                    var allowed = FieldingResolver.ChaseSpeedFt(who, preview.Frozen) * Dt + 0.06;
                    maxRunStep = Math.Max(maxRunStep, step);
                    maxAllowedStep = Math.Max(maxAllowedStep, allowed);
                    Require(step <= allowed,
                        spec.Name + " pursuit exceeded rated speed at " + elapsed.ToString("0.000")
                        + "s (" + beforeOwner + " -> " + afterOwner + ", "
                        + step.ToString("0.000") + "ft > " + allowed.ToString("0.000") + "ft).");
                }

                if (!beforeCaught && afterCaught)
                {
                    pickedUp = true;
                    var ball = Get<Vector3>(play, "_ball");
                    entry.pickupDistance = Diamond.Dist(afterX, afterZ, ball.x, ball.z);
                    var catchWindow = (double)typeof(MatchDirector).GetMethod("CatchWindow", Hidden)!.Invoke(play, new object[] { map });
                    Require(entry.pickupDistance <= catchWindow + 0.5,
                        spec.Name + " acquired the ball outside its catch window.");
                }

                if (tick == 1 || tick % 6 == 0 || afterCaught || Phase(play) != "InPlay")
                {
                    var ball = Get<Vector3>(play, "_ball");
                    var who = map.TryGetValue(afterOwner, out var active) ? active : preview.Fielder;
                    var speed = FieldingResolver.ChaseSpeedFt(who, preview.Frozen);
                    var route = FieldingPursuit.Plan(
                        preview, match.Park, unityPath, match.LivePlay.ElapsedSeconds,
                        afterX, afterZ, speed);
                    frames.Add(new Frame
                    {
                        tick = tick,
                        time = match.LivePlay.ElapsedSeconds,
                        owner = afterOwner,
                        gloveX = afterX,
                        gloveZ = afterZ,
                        ballX = ball.x,
                        ballY = ball.y,
                        ballZ = ball.z,
                        targetX = route.X,
                        targetZ = route.Z,
                        caught = afterCaught,
                        phase = Phase(play)
                    });
                    entry.route = frames.ToArray();
                }
            }

            entry.elapsed = elapsed;
            entry.phase = Phase(play);
            entry.pickedUp = pickedUp;
            entry.maxRunStep = maxRunStep;
            entry.maxAllowedStep = maxAllowedStep;
            Require(Phase(play) == "Result", spec.Name + " did not reach Result: " + Phase(play));
            if (spec.Ground && initialRoute.Reachable)
                Require(pickedUp, spec.Name + " reachable grounder never reached a glove.");
            var result = Get<PlayEvent>(play, "_last");
            Require(result != null, spec.Name + " produced no play result.");
            entry.result = result.Kind.ToString();
            if (spec.Wall)
            {
                Require(!pickedUp, "Uncaught wall fixture was caught.");
                Require(result.Kind == PlayKind.HomeRun, "Uncaught wall fixture did not resolve as a home run.");
                Require(elapsed <= preview.HangTimeSec + 1.0,
                    "Uncaught home run hung after its dead-ball deadline.");
            }
            return entry;
        }

        static void Write(Evidence evidence)
        {
            var output = Environment.GetEnvironmentVariable("GS_FIELDING_PURSUIT_EVIDENCE") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "fielding-pursuit.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonUtility.ToJson(evidence, true));
        }

        static string Phase(MatchDirector p) => Get<object>(p, "_phase").ToString();
        static T Get<T>(MatchDirector p, string name) => (T)typeof(MatchDirector).GetField(name, Hidden)!.GetValue(p);
        static void Set(MatchDirector p, string name, object value) => typeof(MatchDirector).GetField(name, Hidden)!.SetValue(p, value);
        static void Invoke(MatchDirector p, string name, params object[] args) => typeof(MatchDirector).GetMethod(name, Hidden)!.Invoke(p, args);
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }

        readonly struct Spec
        {
            public readonly string Name;
            public readonly double ExitVelo;
            public readonly double Launch;
            public readonly double Spray;
            public readonly bool Ground;
            public readonly bool Wall;
            public Spec(string name, double exitVelo, double launch, double spray, bool ground, bool wall)
            { Name = name; ExitVelo = exitVelo; Launch = launch; Spray = spray; Ground = ground; Wall = wall; }
        }

        [Serializable]
        sealed class Evidence
        {
            public string revision;
            public string unityVersion;
            public string utc;
            public bool ok;
            public string error;
            public Case activeCase;
            public Case[] cases;
        }

        [Serializable]
        sealed class Case
        {
            public string name;
            public double exitVelo;
            public double launch;
            public double spray;
            public bool ground;
            public bool wall;
            public string initialFielder;
            public string initialPosition;
            public double initialTargetX;
            public double initialTargetZ;
            public bool initialReachable;
            public bool pickedUp;
            public double pickupDistance;
            public double maxRunStep;
            public double maxAllowedStep;
            public float elapsed;
            public string result;
            public string phase;
            public Frame[] route;
        }

        [Serializable]
        sealed class Frame
        {
            public int tick;
            public double time;
            public string owner;
            public double gloveX;
            public double gloveZ;
            public double ballX;
            public double ballY;
            public double ballZ;
            public double targetX;
            public double targetZ;
            public bool caught;
            public string phase;
        }
    }
}
