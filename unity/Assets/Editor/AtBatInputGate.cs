using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GrandSluggers.Sim;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Opt-in Play-mode gate for the real Controls -> MatchDirector at-bat path.
    /// Virtual pads make release edges and camera-relative stick movement repeatable;
    /// this is a regression gate, not a physical-controller feel acceptance.
    /// </summary>
    [InitializeOnLoad]
    public static class AtBatInputGate
    {
        const string Pending = "GrandSluggers.AtBatInputGate";
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
        const float Step = 1f / 60f;
        static Gamepad _pad1;
        static Gamepad _pad2;
        static Keyboard _keyboard;

        static AtBatInputGate() { EditorApplication.update += Update; }

        [MenuItem("Grand Sluggers/Verify At-Bat Input")]
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
                Debug.LogError("Grand Sluggers at-bat input gate: Harbor startup timed out.");
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
                _pad1 = InputSystem.AddDevice<Gamepad>("AtBatGatePad1");
                _pad2 = InputSystem.AddDevice<Gamepad>("AtBatGatePad2");
                _keyboard = InputSystem.AddDevice<Keyboard>("AtBatGateKeyboard");
                _keyboard.MakeCurrent();
                Neutral();
                evidence.cases = new[]
                {
                    VerifyNormalTap(play),
                    VerifyHeldRelease(play),
                    VerifyWindupRelease(play),
                    VerifyCpuLaunchBoundaryRelease(play),
                    VerifyTwoSeatLaunchBoundaryRelease(play),
                    VerifyKeyboardCannotReleasePadTwo(play),
                    VerifyScreenDirections(play),
                    VerifyCursorIgnoresCurve(play)
                };
                evidence.ok = true;
                Debug.Log("Grand Sluggers at-bat input OK: " + evidence.cases.Length
                    + " real Controls/TickSet/TickFlight cases.");
            }
            catch (Exception ex)
            {
                evidence.error = ex.ToString();
                Debug.LogException(ex);
            }
            finally
            {
                Controls.EndMatch();
                if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
                if (_pad2 != null && _pad2.added) InputSystem.RemoveDevice(_pad2);
                if (_pad1 != null && _pad1.added) InputSystem.RemoveDevice(_pad1);
                _keyboard = null;
                _pad1 = _pad2 = null;
                Write(evidence);
                EditorApplication.isPlaying = false;
            }
        }

        static GateCase VerifyNormalTap(MatchDirector play)
        {
            Setup(play, Seats.One);
            Set(play, "_t", (float)Get<FeelTable>(play, "_feel").PitcherReadySeconds + 0.01f);
            Tick(play, "TickSet", State(south: true), State());
            Require(Phase(play) == "Set", "Pitch launched on press instead of release.");
            Require(Get<ChargeButtonState>(play, "_pitchButton").Armed, "South press did not arm pitch.");
            Tick(play, "TickSet", State(), State());
            var pitch = Get<PitchCommand>(play, "_pitch");
            Require(Phase(play) == "Flight" && pitch != null, "South release did not launch pitch.");
            Require(ChargeFeel.IsSlap(pitch.Charge01), "Quick release was not a normal pitch.");
            return new GateCase { name = "normal-tap-release", phase = Phase(play), charge = pitch.Charge01 };
        }

        static GateCase VerifyHeldRelease(MatchDirector play)
        {
            Setup(play, Seats.One);
            Set(play, "_t", (float)Get<FeelTable>(play, "_feel").PitcherReadySeconds + 0.01f);
            for (var frame = 0; frame < 40; frame++)
                Tick(play, "TickSet", State(south: true), State());
            Require(Phase(play) == "Set", "Held South launched before release.");
            Tick(play, "TickSet", State(), State());
            var pitch = Get<PitchCommand>(play, "_pitch");
            Require(Phase(play) == "Flight" && pitch != null, "Charged release did not launch pitch.");
            Require(pitch.Charge01 > 0.9, "Held release lost its charge: " + pitch.Charge01);
            return new GateCase { name = "held-charge-release", phase = Phase(play), charge = pitch.Charge01 };
        }

        static GateCase VerifyWindupRelease(MatchDirector play)
        {
            var match = Setup(play, Seats.One, homeAtBat: true);
            Tick(play, "TickSet", State(south: true), State());
            Invoke(play, "Launch", match.CpuPitch());
            var releaseAt = Get<float>(play, "_flight");
            Require(releaseAt < 0, "Fixture did not start inside pitcher windup.");
            Tick(play, "TickFlight", State(), State());
            var swing = Get<SwingCommand>(play, "_swing");
            Require(Get<bool>(play, "_swung") && swing != null && swing.Swing,
                "Swing release during pitcher windup was discarded.");
            Require(swing.TimingErrorFrames < 0, "Windup release was not recorded as an early swing.");
            return new GateCase { name = "windup-release", phase = Phase(play), timingFrames = swing.TimingErrorFrames };
        }

        static GateCase VerifyKeyboardCannotReleasePadTwo(MatchDirector play)
        {
            Setup(play, Seats.Versus);
            Tick(play, "TickSet", State(), State(south: true), Keys(Key.Space));
            Require(Get<ChargeButtonState>(play, "_swingButton").Armed,
                "Pad 2 South did not arm the away batter.");
            Tick(play, "TickSet", State(), State(south: true), Keys());
            Require(Get<ChargeButtonState>(play, "_swingButton").Armed,
                "Player 1 keyboard release committed Player 2's held swing.");
            Require(Get<SwingCommand>(play, "_swing") == null,
                "Player 1 keyboard release created Player 2's swing.");
            return new GateCase { name = "keyboard-seat-isolation", phase = Phase(play) };
        }

        static GateCase VerifyCpuLaunchBoundaryRelease(MatchDirector play)
        {
            var match = Setup(play, Seats.One, homeAtBat: true);
            Tick(play, "TickSet", State(south: true, west: true, stickX: 0.6f, stickY: 0.25f), State());
            Set(play, "_t", (float)Get<FeelTable>(play, "_feel").PitcherReadySeconds + 0.01f);
            Tick(play, "TickSet", State(west: true, stickX: 0.6f, stickY: 0.25f), State());

            var swing = Get<SwingCommand>(play, "_swing");
            Require(Phase(play) == "Flight", "CPU pitch did not launch on the batter release frame.");
            Require(Get<bool>(play, "_swung") && swing != null && swing.Swing,
                "Batter release was discarded on the CPU SET-to-Flight frame.");
            Require(swing.TimingErrorFrames < 0,
                "CPU-boundary release was not recorded inside the pitcher windup.");
            Require(Math.Abs(swing.SprayAimDeg - AtBatResolver.SprayAimDeg(0.6)) < 0.001,
                "CPU-boundary release lost the release-frame spray intent.");
            Require(swing.Bunt && Math.Abs(swing.LaunchAim - 0.25) < 0.001,
                "CPU-boundary release lost the release-frame bunt or launch intent.");
            Require(Math.Abs(swing.BoxOffsetX - match.BatterOffsetX) < 0.001,
                "CPU-boundary release captured the prior frame's batter box position.");
            return new GateCase
            {
                name = "cpu-launch-boundary-release",
                phase = Phase(play),
                charge = swing.Charge01,
                timingFrames = swing.TimingErrorFrames,
                sprayAim = swing.SprayAimDeg,
                launchAim = swing.LaunchAim,
                boxOffset = swing.BoxOffsetX,
                bunt = swing.Bunt
            };
        }

        static GateCase VerifyTwoSeatLaunchBoundaryRelease(MatchDirector play)
        {
            Setup(play, Seats.Versus);
            Set(play, "_t", (float)Get<FeelTable>(play, "_feel").PitcherReadySeconds + 0.01f);
            Tick(play, "TickSet", State(south: true), State(south: true, stickX: -0.5f));
            Tick(play, "TickSet", State(), State(stickX: -0.5f));

            var swing = Get<SwingCommand>(play, "_swing");
            Require(Phase(play) == "Flight", "Player 1 pitch did not launch on the simultaneous release frame.");
            Require(Get<bool>(play, "_swung") && swing != null && swing.Swing,
                "Player 2 batter release was discarded on the shared SET-to-Flight frame.");
            Require(swing.TimingErrorFrames < 0,
                "Two-seat boundary release was not recorded inside the pitcher windup.");
            Require(Math.Abs(swing.SprayAimDeg - AtBatResolver.SprayAimDeg(-0.5)) < 0.001,
                "Two-seat boundary release lost Player 2's release-frame spray intent.");
            Require(Math.Abs(swing.BoxOffsetX - Get<Match>(play, "_match").BatterOffsetX) < 0.001,
                "Two-seat boundary release captured the prior frame's batter box position.");
            return new GateCase
            {
                name = "two-seat-launch-boundary-release",
                phase = Phase(play),
                charge = swing.Charge01,
                timingFrames = swing.TimingErrorFrames,
                sprayAim = swing.SprayAimDeg,
                launchAim = swing.LaunchAim,
                boxOffset = swing.BoxOffsetX,
                bunt = swing.Bunt
            };
        }

        static GateCase VerifyScreenDirections(MatchDirector play)
        {
            var moundRight = PitchMove(play, Seats.One, 1);
            var moundLeft = PitchMove(play, Seats.One, -1);
            var plateRight = PitchMove(play, Seats.Versus, 1);
            var plateLeft = PitchMove(play, Seats.Versus, -1);
            Require(moundRight < 0 && moundLeft > 0,
                $"Mound screen directions reversed: left {moundLeft}, right {moundRight}.");
            Require(plateRight > 0 && plateLeft < 0,
                $"Plate screen directions reversed: left {plateLeft}, right {plateRight}.");
            return new GateCase
            {
                name = "screen-directions",
                moundLeft = moundLeft,
                moundRight = moundRight,
                plateLeft = plateLeft,
                plateRight = plateRight
            };
        }

        static GateCase VerifyCursorIgnoresCurve(MatchDirector play)
        {
            var match = Setup(play, Seats.Versus);
            Tick(play, "TickSet", State(), State(stickX: 1));
            Require(match.BatterOffsetX > 0, "Virtual batting stick did not move the batter.");
            Invoke(play, "Launch", new PitchCommand("fastball", 0, 0, false));
            var zone = Get<StrikeZone>(play, "_zone");
            var target = Get<Transform>(zone, "_target");
            var before = target.localPosition.x;
            Set(play, "_pitchAir", true);
            Set(play, "_flight", 0.1f);
            Tick(play, "TickFlight", State(stickX: 1), State());
            var after = target.localPosition.x;
            var breakX = Get<float>(play, "_breakX");
            var expected = SweetSpot.WorldCenter(match.BatterOffsetX).X;
            Require(breakX > 0, "Plate-view screen-right did not curve screen-right.");
            Require(Math.Abs(after - before) < 0.0001f && Math.Abs(after - expected) < 0.0001,
                $"Curve moved batter cursor: before {before}, after {after}, expected {expected}.");
            return new GateCase
            {
                name = "cursor-follows-batter",
                cursorBefore = before,
                cursorAfter = after,
                breakX = breakX,
                batterOffset = match.BatterOffsetX
            };
        }

        static double PitchMove(MatchDirector play, Seats seats, float stickX)
        {
            var match = Setup(play, seats);
            Tick(play, "TickSet", State(stickX: stickX), State());
            return match.PitcherOffsetX;
        }

        static Match Setup(MatchDirector play, Seats seats, bool homeAtBat = false)
        {
            Neutral();
            var match = Match.Slice(Get<ContentCatalog>(play, "_content"), innings: 3, seed: 1);
            if (homeAtBat) match.SkipToHomeCaptainAtBat();
            Set(play, "_match", match);
            var lifecycle = Get<MatchSeatLifecycle>(play, "_matchSeats");
            lifecycle.Release();
            lifecycle.Bind(seats);
            SetStatic(typeof(Controls), "_matchDevices", new DeviceSeats(
                _pad1.deviceId, seats.BothHuman ? _pad2.deviceId : null));
            SetStatic(typeof(Controls), "_matchDevicesBound", true);
            Invoke(play, "BeginSet");
            Set(play, "_gateHold", true);
            Set(play, "_t", 0f);
            return match;
        }

        static void Tick(MatchDirector play, string method, GamepadState pad1, GamepadState pad2)
        {
            Tick(play, method, pad1, pad2, Keys());
        }

        static void Tick(MatchDirector play, string method, GamepadState pad1, GamepadState pad2,
            KeyboardState keyboard)
        {
            InputSystem.QueueStateEvent(_pad1, pad1);
            InputSystem.QueueStateEvent(_pad2, pad2);
            InputSystem.QueueStateEvent(_keyboard, keyboard);
            InputSystem.Update();
            Controls.Tick(Step);
            Invoke(play, method, Step);
        }

        static void Neutral()
        {
            if (_pad1 == null || _pad2 == null || _keyboard == null) return;
            InputSystem.QueueStateEvent(_pad1, State());
            InputSystem.QueueStateEvent(_pad2, State());
            InputSystem.QueueStateEvent(_keyboard, Keys());
            InputSystem.Update();
            Controls.Tick(Step);
        }

        static GamepadState State(bool south = false, bool west = false, float stickX = 0, float stickY = 0)
        {
            var state = new GamepadState { leftStick = new Vector2(stickX, stickY) };
            if (south) state = state.WithButton(GamepadButton.South);
            return west ? state.WithButton(GamepadButton.West) : state;
        }

        static KeyboardState Keys(params Key[] pressed) => new(pressed);

        static void Write(Evidence evidence)
        {
            var output = Environment.GetEnvironmentVariable("GS_AT_BAT_INPUT_EVIDENCE") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "at-bat-input-gate.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonUtility.ToJson(evidence, true));
        }

        static string Phase(MatchDirector play) => Get<object>(play, "_phase").ToString();
        static T Get<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, Hidden)!.GetValue(owner);
        static void Set(object owner, string name, object value) =>
            owner.GetType().GetField(name, Hidden)!.SetValue(owner, value);
        static void SetStatic(Type owner, string name, object value) =>
            owner.GetField(name, StaticHidden)!.SetValue(null, value);
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
            public string phase;
            public double charge;
            public double timingFrames;
            public double sprayAim;
            public double launchAim;
            public double boxOffset;
            public bool bunt;
            public double moundLeft;
            public double moundRight;
            public double plateLeft;
            public double plateRight;
            public double cursorBefore;
            public double cursorAfter;
            public double breakX;
            public double batterOffset;
        }
    }
}
