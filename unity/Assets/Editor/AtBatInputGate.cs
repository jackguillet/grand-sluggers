using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Motion = GrandSluggers.Sim.Motion;
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
        const float Step = 1f / 60f;
        static Gamepad _pad1;
        /// <summary>The director's own rules table, set when the gate starts: the pads tick against it.</summary>
        static RulesTable _rules;
        /// <summary>The director's catalog, set with <see cref="_rules"/>.</summary>
        static ContentCatalog _content;
        static Gamepad _pad2;

        static AtBatInputGate() { EditorApplication.update += Update; }

        [MenuItem("Grand Sluggers/Verify At-Bat Input")]
        public static void Run() => Start(false);

        [MenuItem("Grand Sluggers/Verify Pitch Motion")]
        public static void RunPitchMotion() => Start(true);

        [MenuItem("Grand Sluggers/Verify Setup Input")]
        public static void RunSetup() => Start(false, true);

        static void Start(bool pitchOnly, bool setupOnly = false)
        {
            SessionState.SetBool(Pending + ".pitchOnly", pitchOnly);
            SessionState.SetBool(Pending + ".setupOnly", setupOnly);
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
            var play = UnityEngine.Object.FindAnyObjectByType<MatchDirector>();
            if (play == null || play._match == null) return;
            _content = play._content;
            _rules = _content.Rules;

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
                Neutral();
                play.StartCoroutine(ExecuteChecks(play, evidence));
            }
            catch (Exception ex)
            {
                evidence.error = ex.ToString();
                Debug.LogException(ex);
                Finish(evidence);
            }
        }

        static IEnumerator ExecuteChecks(MatchDirector play, Evidence evidence)
        {
            var checks = Checks(play, evidence);
            while (true)
            {
                object next;
                try
                {
                    if (!checks.MoveNext()) break;
                    next = checks.Current;
                }
                catch (Exception ex)
                {
                    evidence.error = ex.ToString();
                    Debug.LogException(ex);
                    break;
                }
                yield return next;
            }
            Finish(evidence);
        }

        static IEnumerator Checks(MatchDirector play, Evidence evidence)
        {
            var cases = new List<GateCase>();
            var setupOnly = SessionState.GetBool(Pending + ".setupOnly", false);
            if (!setupOnly)
            foreach (var hand in new[] { Hand.R, Hand.L })
            foreach (var charged in new[] { false, true })
            {
                var motion = VerifyPitchMotion(play, hand, charged, result => cases.Add(result));
                while (motion.MoveNext()) yield return motion.Current;
                evidence.cases = cases.ToArray();
            }
            if (!SessionState.GetBool(Pending + ".pitchOnly", false))
            foreach (var check in setupOnly ? new Func<GateCase>[]
            { () => VerifyLineupFill(play, false), () => VerifyLineupFill(play, true) } : new Func<GateCase>[]
            {
                () => VerifyControllerRouting(play),
                () => VerifyLineupFill(play, false),
                () => VerifyLineupFill(play, true),
                () => VerifyNormalTap(play),
                () => VerifyHeldRelease(play),
                () => VerifyCpuFlightRelease(play),
                () => VerifyTwoSeatFlightRelease(play),
                () => VerifyCpuSetReleaseIgnored(play),
                () => VerifyTwoSeatSetReleaseIgnored(play),
                () => VerifyScreenDirections(play),
                () => VerifyCursorIgnoresCurve(play),
                () => VerifyCycleOnceChangeup(play, padTwo: false),
                () => VerifyCycleOnceChangeup(play, padTwo: true),
                () => VerifyCycleAfterArmIgnored(play),
                () => VerifyCycleResetsEachPitch(play),
                () => VerifySelectSwapPick(play, padTwo: false),
                () => VerifySelectSwapPick(play, padTwo: true),
                // P4-c (#803): the held bunt's triggers, the East cancel and the leak guards, through the real
                // Controls -> TickSet / TickFlight path (PH-13-R1, PH-14-R3 ... R6).
                () => VerifyCancelThenTake(play, padTwo: false),
                () => VerifyCancelThenTake(play, padTwo: true),
                () => VerifyTriggerConvertsLoad(play, padTwo: false, first: false),
                () => VerifyTriggerConvertsLoad(play, padTwo: true, first: true),
                () => VerifySideChangeWhileSquared(play),
                () => VerifyReleaseAllWithdraws(play),
                () => VerifyEastCancelIsNotATrainingSkip(play),
                // P5-c (#803): the held special modifier, read at the accepted release (PH-16-R10 ... R12, R17).
                () => VerifyStarHeldAtReleaseIsSpecial(play),
                () => VerifyStarLetGoBeforeReleaseIsOrdinary(play),
                () => VerifyStarPressedWhileChargingCounts(play),
                () => VerifyStarAfterReleaseChangesNothing(play),
                () => VerifyStarSwingOnPadTwo(play),
                () => VerifyUnaffordableStarIsOrdinaryWithTell(play),
                () => VerifyStarInTheFlightIsNotAllAdvance(play),
            })
            {
                cases.Add(check());
                evidence.cases = cases.ToArray();
            }
            if (!SessionState.GetBool(Pending + ".pitchOnly", false))
            {
                var screens = VerifyControllerScreens(play);
                while (screens.MoveNext()) yield return screens.Current;
                cases.Add(new GateCase { name = "controller-title-stadium-captains-book", phase = Phase(play) });
                evidence.cases = cases.ToArray();
            }
            evidence.ok = true;
            Debug.Log("Grand Sluggers at-bat input OK: " + cases.Count + " real Controls/TickSet/TickFlight cases.");
        }

        static GateCase VerifyLineupFill(MatchDirector play, bool swapSeats)
        {
            Setup(play, swapSeats ? Seats.AwayVersus : Seats.Versus);
            play.Pad1Home = !swapSeats;
            play._versusWanted = true;
            play._lineup = null;
            play.OpenLineup();
            var lineup = play._lineup;
            foreach (var seat in new[] { LineupSeat.Pad1, LineupSeat.Pad2 })
            {
                var home = lineup.HomeSeat == seat;
                lineup.FocusCell(seat, home ? LineupFocus.HomeRow : LineupFocus.AwayRow, 0);
                var other = (home ? lineup.AwaySlots : lineup.HomeSlots).Select(c => c?.Id).ToArray();
                var rb = State().WithButton(GamepadButton.RightShoulder);
                Neutral();
                InputSystem.QueueStateEvent(seat == LineupSeat.Pad1 ? _pad1 : _pad2, rb);
                InputSystem.Update(); Controls.Tick(Step, _rules);
                play.TickLineup();
                Require(home ? lineup.HomeFull : lineup.AwayFull, "RB failed to fill the acting seat from roster focus.");
                Require(other.SequenceEqual((home ? lineup.AwaySlots : lineup.HomeSlots).Select(c => c?.Id)),
                    "RB changed the other seat's roster.");
                Require(lineup.Step == LineupStep.TeamSetup, "Fill advanced the page without confirmation.");
                lineup.FocusCell(seat, LineupFocus.Pool, 0);
                Neutral();
                InputSystem.QueueStateEvent(seat == LineupSeat.Pad1 ? _pad1 : _pad2, State().WithButton(GamepadButton.West));
                InputSystem.Update(); Controls.Tick(Step, _rules); play.TickLineup();
                Require(home ? lineup.HomeFull : lineup.AwayFull, "West on the pool removed an unselected roster player.");
            }
            return new GateCase { name = "lineup-rb-fill-both-seats-" + swapSeats, phase = Phase(play) };
        }

        static IEnumerator VerifyControllerScreens(MatchDirector play)
        {
            Setup(play, Seats.One);
            Controls.UseDevices(new DeviceSeats(_pad1.deviceId, _pad2.deviceId));
            Neutral();
            play._versusWanted = false; play.Pad1Home = true;
            play.OpenTitle();
            var folder = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("GS_AT_BAT_INPUT_EVIDENCE")
                ?? Application.dataPath)!, "controller-screens");
            Directory.CreateDirectory(folder);
            IEnumerator Capture(string name)
            {
                play.DrawActors(Step);
                yield return new WaitForEndOfFrame();
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), shot.EncodeToPNG());
                UnityEngine.Object.Destroy(shot);
            }
            void Menu(GamepadState one, GamepadState two = default)
            {
                InputSystem.QueueStateEvent(_pad1, one); InputSystem.QueueStateEvent(_pad2, two);
                InputSystem.Update(); Controls.Tick(Step, _rules);
                play._t = 1f; play.TickFlow();
            }
            void Press(GamepadButton button, bool two = false)
            {
                Menu(State());
                Menu(two ? State() : State().WithButton(button), two ? State().WithButton(button) : State());
                Menu(State());
            }
            var shot = Capture("title"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South);
            Require(Phase(play) == "Field", "Title confirm did not open stadium selection.");
            shot = Capture("stadium"); while (shot.MoveNext()) yield return shot.Current;
            for (var i = 0; i < 5; i++) Press(GamepadButton.DpadDown);
            Press(GamepadButton.South);
            Require(Phase(play) == "Select", "Stadium navigation did not reach captains: focus=" + play._fieldFocus
                + ", padEnabled=" + _pad1.enabled + ", pad=" + Controls.SeatDeviceId(0) + ", expected=" + _pad1.deviceId);
            var board = play._captains;
            var seen = new HashSet<string>();
            for (var i = 0; i < _content.CaptainIds.Count; i++)
            {
                seen.Add(board.Id(0));
                shot = Capture("captain-" + board.Id(0)); while (shot.MoveNext()) yield return shot.Current;
                Press(GamepadButton.DpadRight);
            }
            Require(seen.Count == _content.CaptainIds.Count, "A captain is unreachable through the controller.");
            Press(GamepadButton.South);
            Require(Phase(play) == "Select" && board.Ready(0) && !board.Ready(1), "P1 skipped choosing the CPU captain.");
            shot = Capture("captain-cpu"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.East);
            Require(!board.Ready(0), "East did not undo the first captain.");
            Press(GamepadButton.South); Press(GamepadButton.South);
            Require(Phase(play) == "Lineup", "Two sequential confirmations did not open lineup.");
            var lineup = play._lineup;
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.HomeRow, 8);
            for (var i = 0; i < 8; i++) Press(GamepadButton.West);
            Require(!lineup.HomeFull, "Removing roster players did not expose open team slots.");
            shot = Capture("team-one-empty"); while (shot.MoveNext()) yield return shot.Current;
            // SC-24 stills: the inspection card's four bars for every pool captain, the seat's own captain and one role player.
            var role = false;
            for (var i = 0; i < lineup.Pool.Count; i++)
            {
                if (!lineup.Pool[i].Captain && role) continue;
                role |= !lineup.Pool[i].Captain;
                lineup.FocusCell(LineupSeat.Pad1, LineupFocus.Pool, i);
                shot = Capture((lineup.Pool[i].Captain ? "team-card-" : "team-card-role-") + lineup.Pool[i].Id);
                while (shot.MoveNext()) yield return shot.Current;
            }
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.HomeRow, 0);
            shot = Capture("team-card-" + lineup.HomeCaptain.Id); while (shot.MoveNext()) yield return shot.Current;
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.HomeRow, 0);
            Press(GamepadButton.RightShoulder);
            Require(lineup.HomeFull, "RB did not fill P1 from roster focus.");
            shot = Capture("lineup-filled"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South);
            shot = Capture("positions-one"); while (shot.MoveNext()) yield return shot.Current;
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.HomeOrder, 0);
            var orderBefore = lineup.Home.Order.Select(c => c.Id).ToArray();
            Press(GamepadButton.South); Press(GamepadButton.DpadRight);
            Require(orderBefore.SequenceEqual(lineup.Home.Order.Select(c => c.Id)), "Inspecting a destination changed the batting order.");
            shot = Capture("positions-one-picked"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South);
            Require(lineup.Home.Order[0].Id == orderBefore[1] && lineup.Home.Order[1].Id == orderBefore[0], "South did not swap the chosen batting slots.");
            Press(GamepadButton.North); Press(GamepadButton.North);
            Require(Phase(play) == "Set", "One-player setup did not reach first pitch.");
            shot = Capture("first-pitch-one"); while (shot.MoveNext()) yield return shot.Current;
            play._t = (float)play._feel.PitcherReadySeconds + .1f;
            Tick(play, play.TickSet, State(south: true), State()); Tick(play, play.TickSet, State(), State());
            Require(Phase(play) == "Flight", "P1 could not release the first pitch after filling the team.");
            play.OpenLineup();
            // Back to stadium through the same controller path, then enable two players.
            Press(GamepadButton.East); Press(GamepadButton.East);
            Require(Phase(play) == "Field", "East did not return to stadium setup.");
            for (var i = 0; i < 3; i++) Press(GamepadButton.DpadDown);
            Press(GamepadButton.DpadRight);
            Press(GamepadButton.DpadDown); Press(GamepadButton.DpadRight); // P1 away
            Press(GamepadButton.DpadDown); Press(GamepadButton.South);
            Require(Phase(play) == "Select", "Two-player setup did not reach the board.");
            board = play._captains;
            Require(board.Versus && !board.Pad1Home, "Player count or P1 away did not persist.");
            Controls.UseDevices(new DeviceSeats(_pad1.deviceId, int.MaxValue));
            Press(GamepadButton.South);
            Require(Phase(play) == "Select" && !board.Ready(1), "Missing P2 silently became a CPU opponent.");
            shot = Capture("captain-waiting-pad2"); while (shot.MoveNext()) yield return shot.Current;
            Require(!play._match.Paused, "Unbound P2 loss blocked returning to setup.");
            Controls.UseDevices(new DeviceSeats(_pad1.deviceId, _pad2.deviceId));
            Controls.CatchPlay(); Press(GamepadButton.East);
            // SC-24 stills: both pads on the same captain, every captain; both team cards must show the same four bars.
            while (board.Id(1) != board.Id(0)) Press(GamepadButton.DpadRight, true);
            for (var i = 0; i < _content.CaptainIds.Count; i++)
            {
                Require(board.Id(0) == board.Id(1) && !board.Ready(0) && !board.Ready(1), "Both pads could not browse the same captain.");
                shot = Capture("captain-2p-" + board.Id(0)); while (shot.MoveNext()) yield return shot.Current;
                Press(GamepadButton.DpadRight); Press(GamepadButton.DpadRight, true);
            }
            var start = board.Id(1);
            Press(GamepadButton.DpadRight, true);
            Require(start != board.Id(1), "P2 cannot move its own cursor.");
            while (board.Id(1) != board.Id(0)) Press(GamepadButton.DpadRight, true);
            Press(GamepadButton.South);
            Press(GamepadButton.South, true);
            Require(Phase(play) == "Select" && !board.Ready(1), "Both players confirmed the same captain.");
            shot = Capture("captain-reserved"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.DpadRight, true);
            shot = Capture("captain-two-player"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South, true);
            Require(Phase(play) == "Lineup", "Both captains confirmed but lineup did not open.");
            lineup = play._lineup;
            Require(lineup.HomeSeat == LineupSeat.Pad2 && lineup.AwaySeat == LineupSeat.Pad1, "Captain confirmation lost P1 away.");
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.AwayRow, 8);
            lineup.FocusCell(LineupSeat.Pad2, LineupFocus.HomeRow, 8);
            for (var i = 0; i < 8; i++) { Press(GamepadButton.West); Press(GamepadButton.West, true); }
            shot = Capture("team-two-empty"); while (shot.MoveNext()) yield return shot.Current;
            // SC-24 stills: both seats inspect the same pool player, every pool captain and one role player.
            role = false;
            for (var i = 0; i < lineup.Pool.Count; i++)
            {
                if (!lineup.Pool[i].Captain && role) continue;
                role |= !lineup.Pool[i].Captain;
                lineup.FocusCell(LineupSeat.Pad1, LineupFocus.Pool, i);
                lineup.FocusCell(LineupSeat.Pad2, LineupFocus.Pool, i);
                Require(lineup.InspectedBy(LineupSeat.Pad1) == lineup.InspectedBy(LineupSeat.Pad2), "Both seats should inspect one pool player.");
                shot = Capture((lineup.Pool[i].Captain ? "team-card-2p-" : "team-card-2p-role-") + lineup.Pool[i].Id);
                while (shot.MoveNext()) yield return shot.Current;
            }
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.AwayRow, 0);
            lineup.FocusCell(LineupSeat.Pad2, LineupFocus.HomeRow, 0);
            shot = Capture("team-card-2p-own"); while (shot.MoveNext()) yield return shot.Current;
            lineup.FocusCell(LineupSeat.Pad1, LineupFocus.AwayRow, 8);
            lineup.FocusCell(LineupSeat.Pad2, LineupFocus.HomeRow, 8);
            Press(GamepadButton.RightShoulder); Press(GamepadButton.RightShoulder, true);
            Require(lineup.HomeFull && lineup.AwayFull, "Both controllers cannot fill their teams.");
            shot = Capture("lineup-two-filled"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South);
            Press(GamepadButton.RightShoulder); Press(GamepadButton.RightShoulder, true);
            shot = Capture("positions-two-fields"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South); Press(GamepadButton.DpadRight);
            shot = Capture("positions-two-picked"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.South);
            Press(GamepadButton.North);
            shot = Capture("positions-two-ready"); while (shot.MoveNext()) yield return shot.Current;
            Press(GamepadButton.North, true);
            Require(lineup.Step == LineupStep.MatchSettings, "Both lineup confirmations did not reach settings.");
            Press(GamepadButton.North); Press(GamepadButton.North, true);
            Require(Phase(play) == "Set", "Two-player setup did not reach first pitch.");
            shot = Capture("first-pitch-two"); while (shot.MoveNext()) yield return shot.Current;
            play._t = (float)play._feel.PitcherReadySeconds + .1f;
            Tick(play, play.TickSet, State(), State(south: true)); Tick(play, play.TickSet, State(), State());
            Require(Phase(play) == "Flight", "P2 could not release the first pitch after filling both teams.");
            play.OpenControlsBook();
            foreach (var id in new[] { "exhibition", "lineup", "two-pads" })
            {
                var page = HowToPlay.Pages.ToList().FindIndex(p => p.Id == id);
                if (page < 0) continue;
                play._pausePage = page;
                shot = Capture(id); while (shot.MoveNext()) yield return shot.Current;
            }
        }

        static void Finish(Evidence evidence)
        {
            Controls.EndMatch();
            if (_pad2 != null && _pad2.added) InputSystem.RemoveDevice(_pad2);
            if (_pad1 != null && _pad1.added) InputSystem.RemoveDevice(_pad1);
            _pad1 = _pad2 = null;
            Write(evidence);
            EditorApplication.isPlaying = false;
        }

        // Unlike a SnapTick still, this follows the live two-slot mixer from
        // held load through release and finish. A zero-delta fade must fail here.
        static IEnumerator VerifyPitchMotion(MatchDirector play, Hand hand, bool charged, Action<GateCase> record)
        {
            var match = Setup(play, Seats.One);
            if (match.Pitcher.Throws != hand)
            {
                Require(match.SwapPitcher(match.Defense.Everyone.First(c => c.Throws == hand)), "No pitcher for hand fixture.");
                play.BeginSet();
            }
            for (var settle = 0; settle < 20; settle++)
            {
                play.DrawActors(Step);
                yield return null;
            }
            play._t = (float)play._feel.PitcherReadySeconds + .01f;
            for (var frame = 0; frame < (charged ? 40 : 1); frame++)
            {
                Tick(play, play.TickSet, State(south: true), State());
                play.DrawActors(Step);
                yield return null;
            }
            var hero = play._heroes[match.Pitcher.Id];
            var name = "pitch-motion-" + hand + (charged ? "-charge" : "-normal");
            var files = new List<string> { CapturePitch(hero, name + "-load") };
            Tick(play, play.TickSet, State(), State());
            play.DrawActors(Step);
            yield return null;
            var pitch = play._pitch;
            var expected = ArtBinder.LoadClip(Motion.ClipFile(Motion.Verb.ThrowPitch,
                match.Pitcher.Bats, hand, pitch.Charge01));
            var clipCorrect = ((ClipPlayer)hero._player).Current == expected;
            var released = false;
            var releasedAt = 0f;
            var releaseLocal = Vector3.zero;
            var handAtRelease = Vector3.zero;
            var heldError = 0f;
            for (var frame = 0; frame < 42; frame++)
            {
                Tick(play, play.TickFlight, State(), State());
                play.DrawActors(Step);
                // Let Unity's normal skinning/render update run. A tight loop
                // can move bones while repeatedly capturing cached skinning.
                yield return null;
                var air = play._pitchAir;
                if (!air)
                {
                    var ball = ((Transform)(play._park.Ball)._root);
                    heldError = Mathf.Max(heldError, Vector3.Distance(ball.position, hero.ThrowHand.position));
                }
                else if (!released)
                {
                    released = true;
                    releasedAt = (float)Motion.PitchRelease + play._flight;
                    releaseLocal = hero.transform.InverseTransformPoint(play._relFrom);
                    handAtRelease = hero.transform.InverseTransformPoint(hero.ThrowHand.position);
                    files.Add(CapturePitch(hero, name + "-release"));
                }
                if (frame == 33) files.Add(CapturePitch(hero, name + "-follow"));
            }
            var finish = hero.transform.InverseTransformPoint(hero.ThrowHand.position);
            files.Add(CapturePitch(hero, name + "-finish"));
            // Only after observing the live result, sample the authored take as
            // an oracle. Snapping during the loop would hide a stalled crossfade.
            hero.SetPose(Motion.Verb.ThrowPitch, (float)pitch.Charge01);
            hero.SnapTick((float)Motion.PitchDur);
            var finishError = Vector3.Distance(finish, hero.transform.InverseTransformPoint(hero.ThrowHand.position));
            hero.SnapTick((float)Motion.PitchRelease);
            var releaseError = Vector3.Distance(releaseLocal, hero.transform.InverseTransformPoint(hero.ThrowHand.position));
            var travel = Vector3.Distance(handAtRelease, finish);
            Require(released && releasedAt >= Motion.PitchRelease && releasedAt < Motion.PitchRelease + Step + .001,
                name + " ball released outside the authored marker: " + releasedAt);
            Require(travel > .25f && finishError < .02f,
                name + " froze or missed its follow-through: travel=" + travel + ", finish error=" + finishError);
            Require(clipCorrect, name + " discarded the committed charge when selecting its take.");
            Require(heldError < .001f && releaseError < .02f,
                name + " ball left the authored palm: hold=" + heldError + ", release=" + releaseError);
            record(new GateCase { name = name, phase = Phase(play), charge = pitch.Charge01,
                releaseAt = releasedAt, followTravel = travel, finishError = finishError,
                releaseError = releaseError, heldError = heldError, images = files.ToArray() });
        }

        static string CapturePitch(HeroActor hero, string name)
        {
            var folder = Environment.GetEnvironmentVariable("GS_PITCH_MOTION_STILLS") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "pitch-motion");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, name + ".png");
            var go = new GameObject("Pitch motion review camera");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = hero.transform.TransformPoint(new Vector3(-10, 7, 13));
            camera.transform.LookAt(hero.transform.position + Vector3.up * 3.5f);
            camera.fieldOfView = 40;
            var target = new RenderTexture(960, 720, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(960, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(go);
            }
            return path;
        }

        static GateCase VerifyControllerRouting(MatchDirector play)
        {
            var match = Setup(play, Seats.One, homeAtBat: true);
            var runner = match.Offense.Roster.Last(c => c.Id != match.Batter.Id);
            Require(match.StationRunner(1, runner), "Could not station controller-routing runner.");
            var send = State(south: true, lb: true).WithButton(GamepadButton.LeftShoulder);
            Tick(play, play.TickSet, send, State());
            Require(Controls.Pad1.StarHeld && Controls.Pad1.AllAdvance && Controls.Pad1.BallHeld,
                "LT, LB and RT must be independent on the same frame.");
            Require(match.Runners.Any(r => r.Who.Id == runner.Id && r.Broke), "Physical LB did not send the runner in SET.");
            Setup(play, Seats.One);
            Tick(play, play.TickSet, State().WithButton(GamepadButton.North), State());
            var jump = ((LivePadInput)play.FieldInput());
            Require(jump.WestDown && !jump.Attack && !jump.SouthDown, "North must route only to jump on defense.");
            Neutral();
            InputSystem.QueueStateEvent(_pad1, State().WithButton(GamepadButton.South));
            InputSystem.Update(); Controls.Tick(Step, _rules);
            var close = ((LivePadInput)play.FieldInput());
            Require(close.CloseResponse == true && !close.SouthDown, "South close response must not throw or catch.");
            return new GateCase { name = "controller-star-steal-jump-routing", phase = Phase(play) };
        }

        static GateCase VerifyNormalTap(MatchDirector play)
        {
            Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true), State());
            Require(Phase(play) == "Set", "Pitch launched on press instead of release.");
            Require(play._pitchButton.Armed, "South press did not arm pitch.");
            Tick(play, play.TickSet, State(), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null, "South release did not launch pitch.");
            Require(ChargeFeel.IsSlap(pitch.Charge01), "Quick release was not a normal pitch.");
            return new GateCase { name = "normal-tap-release", phase = Phase(play), charge = pitch.Charge01 };
        }

        static GateCase VerifyHeldRelease(MatchDirector play)
        {
            Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            for (var frame = 0; frame < 40; frame++)
                Tick(play, play.TickSet, State(south: true), State());
            Require(Phase(play) == "Set", "Held South launched before release.");
            Tick(play, play.TickSet, State(), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null, "Charged release did not launch pitch.");
            Require(pitch.Charge01 > 0.9, "Held release lost its charge: " + pitch.Charge01);
            return new GateCase { name = "held-charge-release", phase = Phase(play), charge = pitch.Charge01 };
        }

        static GateCase VerifyCpuFlightRelease(MatchDirector play)
        {
            Setup(play, Seats.One, homeAtBat: true);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true), State());
            Require(Phase(play) == "Flight" && play._flight < 0,
                "CPU pitch did not enter its windup while the batter held South.");
            Require(SwingButton(play).Armed && play._swing == null,
                "Batter hold did not carry from SET into the CPU pitch windup.");
            Tick(play, play.TickFlight, State(), State());
            var swing = play._swing;
            Require(play._swung && swing != null && swing.Swing,
                "Batter release after entering the CPU pitch windup was discarded.");
            Require(swing.TimingErrorFrames < 0, "Windup release was not recorded as an early swing.");
            return new GateCase { name = "cpu-flight-release", phase = Phase(play), timingFrames = swing.TimingErrorFrames };
        }

        static GateCase VerifyTwoSeatFlightRelease(MatchDirector play)
        {
            var match = Setup(play, Seats.Versus);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true), State(south: true, stickX: -0.8f));
            Tick(play, play.TickSet, State(), State(south: true, stickX: -0.8f));
            Require(Phase(play) == "Flight" && play._flight < 0,
                "Player 1 pitch did not enter its windup while Player 2 held South.");
            Require(SwingButton(play).Armed && play._swing == null,
                "Player 2 hold did not carry from SET into the pitch windup.");
            // Release RT with the movement stick still live; no bunt button is held.
            var input = Tick(play, play.TickFlight, State(), State(stickX: -0.8f, stickY: 0.6f));
            var swing = play._swing;
            Require(play._swung && swing != null && swing.Swing,
                "Player 2 release after entering Flight was discarded.");
            Require(swing.TimingErrorFrames < 0, "Player 2 Flight release was not recorded as an early swing.");
            Require(Math.Abs(input.Pad2X) > StickPlay.Dead && Math.Abs(input.Pad2Y) > StickPlay.Dead,
                "Player 2 Flight fixture did not produce live release-frame stick input.");
            Require(!input.Pad2Bunt && !swing.Bunt && swing.BuntSide == BuntSide.None,
                "An ordinary RT release unexpectedly became a bunt.");
            Require(Math.Abs(swing.LaunchAim - input.Pad2Y) < 0.001,
                "Player 2 Flight release lost its release-frame launch intent.");
            Require(Math.Abs(swing.SprayAimDeg - AtBatResolver.SprayAimDeg(input.Pad2X, _rules)) < 0.001,
                "Player 2 Flight release lost its release-frame spray intent.");
            Require(Math.Abs(swing.BoxOffsetX - match.BatterOffsetX) < 0.001,
                "Player 2 Flight release captured the prior frame's batter box position.");
            return new GateCase
            {
                name = "two-seat-flight-release",
                phase = Phase(play),
                charge = swing.Charge01,
                timingFrames = swing.TimingErrorFrames,
                sprayAim = swing.SprayAimDeg,
                launchAim = swing.LaunchAim,
                boxOffset = swing.BoxOffsetX,
                bunt = swing.Bunt
            };
        }

        static GateCase VerifyCpuSetReleaseIgnored(MatchDirector play)
        {
            var match = Setup(play, Seats.One, homeAtBat: true);
            Tick(play, play.TickSet, State(south: true, stickX: 0.6f, stickY: 0.6f), State());
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            var input = Tick(play, play.TickSet, State(stickX: 0.6f, stickY: 0.6f), State());

            var swing = play._swing;
            Require(Phase(play) == "Flight", "CPU pitch did not launch on the batter release frame.");
            Require(Math.Abs(input.Pad1X) > StickPlay.Dead && Math.Abs(input.Pad1Y) > StickPlay.Dead,
                "CPU-boundary fixture did not produce live release-frame stick input.");
            Require(match.BatterOffsetX > 0,
                "CPU-boundary fixture did not exercise the batter's live SET verbs.");
            Require(!play._swung && swing == null,
                "Batter release committed on the CPU SET-to-Flight frame; SET releases are not swings.");
            Require(!SwingButton(play).Armed,
                "Ignored CPU-boundary SET release remained armed after entering Flight.");
            Tick(play, play.TickFlight, State(stickX: 0.6f, stickY: 0.6f), State());
            Require(!play._swung && play._swing == null,
                "Ignored CPU-boundary SET release committed one frame late in Flight.");
            return new GateCase
            {
                name = "cpu-set-release-ignored",
                phase = Phase(play),
                boxOffset = match.BatterOffsetX,
                bunt = input.Pad1Bunt
            };
        }

        static GateCase VerifyTwoSeatSetReleaseIgnored(MatchDirector play)
        {
            var match = Setup(play, Seats.Versus);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true), State(south: true, stickX: -0.8f));
            var input = Tick(play, play.TickSet, State(), State(stickX: -0.8f));

            var swing = play._swing;
            Require(Phase(play) == "Flight", "Player 1 pitch did not launch on the simultaneous release frame.");
            Require(Math.Abs(input.Pad2X) > StickPlay.Dead,
                "Two-seat fixture did not produce live Player 2 release-frame stick input.");
            Require(match.BatterOffsetX < 0,
                "Two-seat boundary fixture did not exercise Player 2's live SET walk.");
            Require(!play._swung && swing == null,
                "Player 2 release committed on the shared SET-to-Flight frame; SET releases are not swings.");
            Require(!SwingButton(play).Armed,
                "Ignored Player 2 SET release remained armed after entering Flight.");
            Tick(play, play.TickFlight, State(), State(stickX: -0.8f));
            Require(!play._swung && play._swing == null,
                "Ignored Player 2 SET release committed one frame late in Flight.");
            return new GateCase
            {
                name = "two-seat-set-release-ignored",
                phase = Phase(play),
                boxOffset = match.BatterOffsetX
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
            Tick(play, play.TickSet, State(), State(stickX: 1));
            Require(match.BatterOffsetX > 0, "Virtual batting stick did not move the batter.");
            play.Launch(new PitchCommand("fastball", 0, false));
            var zone = play._zone;
            var target = zone._target;
            var before = target.localPosition.x;
            play._pitchAir = true;
            play._flight = 0.1f;
            Tick(play, play.TickFlight, State(stickX: 1), State());
            var after = target.localPosition.x;
            var breakX = play._breakX;
            var expected = SweetSpot.WorldCenter(match.BatterOffsetX, match.BatterZone).X;
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

        /// <summary>#582: West held through the release throws a changeup, on controller 1 (1P) and controller 2 (1v1, bottom half).</summary>
        /// <summary>
        /// #825, PH-02-R3/R4/R5: one RB press in SET selects the second pitch, the charge locks it,
        /// and what leaves the hand is that family — on either pad. SET itself stays family-blind:
        /// the pose and the ball are still the fastball's while the selection is held.
        /// </summary>
        static GateCase VerifyCycleOnceChangeup(MatchDirector play, bool padTwo)
        {
            var match = padTwo ? Setup(play, Seats.Versus, homeAtBat: true) : Setup(play, Seats.One);
            Require(padTwo == !match.Top, "Fixture half does not put the expected controller on the mound.");
            var secondFamily = match.Pitcher.Repertoire.Second;
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            var cycle = State(cycle: true);
            Tick(play, play.TickSet, padTwo ? State() : cycle, padTwo ? cycle : State());
            Tick(play, play.TickSet, State(), State());
            Require(play.ShownPitchType == PitchFamily.Fastball,
                "SET leaked the selected family to the pose and the ball.");
            var hold = State(south: true);
            for (var frame = 0; frame < 6; frame++)
                Tick(play, play.TickSet, padTwo ? State() : hold, padTwo ? hold : State());
            Require(Phase(play) == "Set", "Held South launched before release.");
            Tick(play, play.TickSet, State(), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null, "The release did not launch.");
            Require(pitch.Type == secondFamily, "One cycle press did not throw this pitcher's second family.");
            return new GateCase { name = padTwo ? "cycle-once-changeup-pad2" : "cycle-once-changeup-pad1", phase = Phase(play), charge = pitch.Charge01 };
        }

        /// <summary>#825, PH-02-R4: the charge takes the family with it; presses after the arm do nothing.</summary>
        static GateCase VerifyCycleAfterArmIgnored(MatchDirector play)
        {
            Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            var hold = State(south: true);
            var holdAndCycle = State(south: true, cycle: true);
            Tick(play, play.TickSet, hold, State());
            for (var frame = 0; frame < 4; frame++)
                Tick(play, play.TickSet, frame % 2 == 0 ? holdAndCycle : hold, State());
            Require(Phase(play) == "Set", "Held South launched before release.");
            Tick(play, play.TickSet, State(), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null, "The release did not launch.");
            Require(pitch.Type == PitchFamily.Fastball,
                "A cycle press after the charge armed moved the locked family.");
            return new GateCase { name = "cycle-after-arm-ignored", phase = Phase(play), charge = pitch.Charge01 };
        }

        /// <summary>
        /// #825, PH-02-R5: every SET starts on the fastball. The pitch after a cycled changeup is a
        /// fastball with no press, because nothing on the shared screen marks the active family.
        /// </summary>
        static GateCase VerifyCycleResetsEachPitch(MatchDirector play)
        {
            Setup(play, Seats.One);
            var feel = play._feel;
            play._t = (float)feel.PitcherReadySeconds + 0.01f;
            var hold = State(south: true);
            Tick(play, play.TickSet, State(cycle: true), State());
            Tick(play, play.TickSet, State(), State());
            for (var frame = 0; frame < 6; frame++) Tick(play, play.TickSet, hold, State());
            Tick(play, play.TickSet, State(), State());
            Require(play._pitch?.Type == PitchFamily.Changeup,
                "The cycled pitch was not the changeup.");
            play.BeginSet();
            play._gateHold = true;
            play._t = (float)feel.PitcherReadySeconds + 0.01f;
            for (var frame = 0; frame < 6; frame++) Tick(play, play.TickSet, hold, State());
            Tick(play, play.TickSet, State(), State());
            var second = play._pitch;
            Require(Phase(play) == "Flight" && second != null, "The second release did not launch.");
            Require(second.Type == PitchFamily.Fastball,
                "The next SET did not start on the fastball.");
            return new GateCase { name = "cycle-resets-each-pitch", phase = Phase(play), charge = second.Charge01 };
        }

        /// <summary>
        /// PH-13-R1: South loads (SET into the flight), East cancels the armed load and discards its charge, the cancelled
        /// hold comes up and swings nothing, and the pitch reaches the plate as a take. On either pad; East stays spent.
        /// </summary>
        static GateCase VerifyCancelThenTake(MatchDirector play, bool padTwo)
        {
            var match = padTwo ? Setup(play, Seats.Versus) : Setup(play, Seats.One, homeAtBat: true);
            GamepadState Bat(GamepadState state) => state;
            (GamepadState, GamepadState) Frame(GamepadState bat) => padTwo ? (State(), bat) : (bat, State());
            var (a, b) = Frame(State(south: true));
            Tick(play, play.TickSet, a, b);
            Require(SwingButton(play).Armed, "South in SET did not load the batter's swing.");
            Launch(play);
            for (var f = 0; f < 6; f++) { (a, b) = Frame(State(south: true)); Tick(play, play.TickFlight, a, b); }
            Require(SwingButton(play).Armed && play._charge > 0, "The load did not build in the flight.");
            (a, b) = Frame(Bat(State(south: true, east: true)));
            Tick(play, play.TickFlight, a, b);
            var plate = play._plate;
            Require(!plate.Swing.Armed && plate.Swing.MustRelease && plate.Swing.Fill01 == 0,
                "East did not discard the armed load and its charge.");
            Require(!PlateButtons.CancelIsFree(plate), "The East press the plate took is not spent.");
            (a, b) = Frame(State());
            Tick(play, play.TickFlight, a, b);
            Require(!play._swung && play._swing == null,
                "Releasing the cancelled hold swung.");
            var swing = ReachPlate(play, State(), State());
            Require(swing != null && !swing.Swing && !swing.Bunt, "The cancelled pitch was not a take at the plate.");
            return new GateCase { name = padTwo ? "cancel-then-take-pad2" : "cancel-then-take-pad1", phase = Phase(play),
                batterOffset = match.BatterOffsetX };
        }

        /// <summary>
        /// PH-13-R1 option 1: a bunt trigger during an uncommitted load discards it and squares toward that side with no
        /// cancel press; the old hold's release swings nothing; at the plate the held bunt has no timed press.
        /// </summary>
        static GateCase VerifyTriggerConvertsLoad(MatchDirector play, bool padTwo, bool first)
        {
            var match = padTwo ? Setup(play, Seats.Versus) : Setup(play, Seats.One, homeAtBat: true);
            (GamepadState, GamepadState) Frame(GamepadState bat) => padTwo ? (State(), bat) : (bat, State());
            var side = first ? BuntSide.First : BuntSide.Third;
            GamepadState Trigger(bool south) => State(south: south, lt: !first, rt: first);
            Launch(play);
            var (a, b) = Frame(State(south: true));
            Tick(play, play.TickFlight, a, b);
            for (var f = 0; f < 4; f++) { (a, b) = Frame(State(south: true)); Tick(play, play.TickFlight, a, b); }
            Require(SwingButton(play).Armed, "South did not load in the flight.");
            (a, b) = Frame(Trigger(south: true));
            Tick(play, play.TickFlight, a, b);
            var plate = play._plate;
            Require(!plate.Swing.Armed && plate.Swing.Fill01 == 0, "The bunt trigger did not discard the load.");
            Require(play._buntSide == side, "The bunt trigger did not square toward its side.");
            (a, b) = Frame(Trigger(south: false));
            Tick(play, play.TickFlight, a, b);
            Require(!play._swung && play._swing == null,
                "The converted hold's release swung.");
            var swing = ReachPlate(play, a, b);
            Require(swing != null && swing.Bunt && swing.BuntSide == side && swing.TimingErrorFrames == 0 && swing.Charge01 == 0,
                "The squared bat at the plate was not the held bunt toward " + side + ".");
            return new GateCase { name = "trigger-converts-load-" + (padTwo ? "pad2-" : "pad1-") + side, phase = Phase(play),
                bunt = swing.Bunt, batterOffset = match.BatterOffsetX };
        }

        /// <summary>PH-14-R3 / R5: the side changes while squared; the latest press wins; releasing it falls back to the other.</summary>
        static GateCase VerifySideChangeWhileSquared(MatchDirector play)
        {
            Setup(play, Seats.Versus);
            Launch(play);
            Tick(play, play.TickFlight, State(), State(lt: true));
            Require(play._buntSide == BuntSide.Third, "LT did not square toward third.");
            Tick(play, play.TickFlight, State(), State(lt: true, rt: true));
            Require(play._buntSide == BuntSide.First, "RT pressed over LT did not move the bat to first.");
            Tick(play, play.TickFlight, State(), State(lt: true));
            Require(play._buntSide == BuntSide.Third, "Releasing RT with LT held did not fall back to third.");
            Tick(play, play.TickFlight, State(lt: true), State(lt: true));
            Require(play._buntSide == BuntSide.Third, "Pad 1's trigger moved Player 2's bat.");
            var swing = ReachPlate(play, State(), State(lt: true));
            Require(swing != null && swing.Bunt && swing.BuntSide == BuntSide.Third, "The plate did not take the side held at contact.");
            return new GateCase { name = "side-change-while-squared", phase = Phase(play), bunt = swing.Bunt };
        }

        /// <summary>PH-14-R5: releasing every trigger before the plate withdraws the bat; the pitch is a take, nothing is latched.</summary>
        static GateCase VerifyReleaseAllWithdraws(MatchDirector play)
        {
            Setup(play, Seats.One, homeAtBat: true);
            Tick(play, play.TickSet, State(rt: true), State());
            Require(play._buntSide == BuntSide.First && play._squareSec > 0,
                "RT in SET did not square toward first.");
            Launch(play);
            Tick(play, play.TickFlight, State(rt: true), State());
            Tick(play, play.TickFlight, State(), State());
            Require(play._buntSide == BuntSide.None, "Releasing every trigger did not withdraw the bat.");
            var swing = ReachPlate(play, State(), State());
            Require(swing != null && !swing.Swing && !swing.Bunt, "A withdrawn bat was not a take at the plate.");
            return new GateCase { name = "release-all-withdraws", phase = Phase(play) };
        }

        /// <summary>
        /// PH-14-R6: LT held for a bunt at contact is no item modifier (and squares nothing) until it comes up and is
        /// pressed again. RT is spent the same way.
        /// </summary>
        static GateCase VerifyBuntTriggerAfterContactIsNotTheItem(MatchDirector play)
        {
            Setup(play, Seats.One, homeAtBat: true);
            Launch(play);
            Tick(play, play.TickFlight, State(lt: true), State());
            var swing = ReachPlate(play, State(lt: true), State());
            var hit = play._pending ?? play._last?.AtBat;
            Require(swing != null && swing.Bunt && hit != null && hit.Quality != ContactQuality.Miss,
                "The held LT bunt fixture did not make contact.");
            var plate = play._plate;
            Require(plate.Bunt.Fixed && !BuntHold.IsFree(plate.Bunt, BuntSide.Third), "LT held at contact is not spent.");
            // Still down, with RB pressed: the item's LT + RB is not read (the spent hold is no modifier).
            Tick(play, play.TickAtBat, State(lt: true).WithButton(GamepadButton.RightShoulder), State());
            Require(!((bool)play.TriggerFree(Controls.Pad1, BuntSide.Third))
                && !Controls.Pad1.ItemWith(false) && Controls.Pad1.ItemWith(true),
                "The spent LT still reads as the item modifier.");
            // Up: free. Pressed again: LT + RB is the item.
            Tick(play, play.TickAtBat, State(), State());
            Require(((bool)play.TriggerFree(Controls.Pad1, BuntSide.Third)), "LT released did not free the trigger.");
            Tick(play, play.TickAtBat, State(lt: true).WithButton(GamepadButton.RightShoulder), State());
            Require(Controls.Pad1.ItemWith(((bool)play.TriggerFree(Controls.Pad1, BuntSide.Third))),
                "A fresh LT press with RB is not the item.");
            return new GateCase { name = "lt-after-contact-not-item", phase = Phase(play), bunt = true };
        }

        /// <summary>PH-13-R1: the East press that cancels a load is not also the Training skip (or a dive) on the same press.</summary>
        static GateCase VerifyEastCancelIsNotATrainingSkip(MatchDirector play)
        {
            Setup(play, Seats.One, homeAtBat: true);
            Tick(play, play.TickSet, State(south: true), State());
            Launch(play);
            Tick(play, play.TickFlight, State(south: true, east: true), State());
            Require(!SwingButton(play).Armed, "East did not cancel the load.");
            Require(!((bool)play.CancelFree(Controls.Pad1)), "The cancel press is free for another verb.");
            var coach = play._coach;
            Require(coach != null, "No Training director on the Harbor scene.");
            coach.Begin(play._content, PracticeLesson.Batting);
            try
            {
                Require(Controls.Skip, "The gate's East press is not this frame's skip edge.");
                coach.TickSkip(((bool)play.CancelFree(Controls.Pad1)));
                Require(!coach.Session.Finished, "The East cancel also skipped the Training drill.");
                coach.TickSkip(true);
                Require(coach.Session.Finished, "The control fixture: a free East press did not skip.");
            }
            finally { coach.Stop(); }
            Tick(play, play.TickFlight, State(), State());
            Require(((bool)play.CancelFree(Controls.Pad1)), "East released did not free the button.");
            return new GateCase { name = "east-cancel-not-training-skip", phase = Phase(play) };
        }

        /// <summary>
        /// PH-16-R11, R17: LB held at the accepted South release asks for the Star Pitch; the pool
        /// pays, the delivery flies as the special, and the hold is spent until it comes up.
        /// </summary>
        static GateCase VerifyStarHeldAtReleaseIsSpecial(MatchDirector play)
        {
            var match = Setup(play, Seats.One);
            Require(match.CanStarPitch, "The star fixture's defense cannot pay for its Star Pitch.");
            var before = match.DefenseStars;
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true, lb: true), State());
            Require(play.StarAsks.PitchShown, "LB held in SET did not read STAR on the card.");
            Tick(play, play.TickSet, State(lb: true), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null && pitch.Star, "The release with the modifier held was not the Star Pitch.");
            Require(play.StarAsks.PitchAsked, "The release did not record the request.");
            Require(!play.StarAsks.Mod(0).IsFreeNow(), "The hold that asked for the special is not spent.");
            Require(Math.Abs(match.DefenseStars - before) < 1e-9, "The pool paid before the match settled the release.");
            return new GateCase { name = "star-held-at-release-pad1", phase = Phase(play),
                charge = pitch.Charge01 };
        }

        /// <summary>PH-16-R11: the modifier let go before the release is the ordinary pitch; nothing is asked or spent.</summary>
        static GateCase VerifyStarLetGoBeforeReleaseIsOrdinary(MatchDirector play)
        {
            Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            for (var f = 0; f < 6; f++) Tick(play, play.TickSet, State(south: true, lb: true), State());
            Tick(play, play.TickSet, State(south: true), State());
            Tick(play, play.TickSet, State(), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null && !pitch.Star, "LB let go before the release still threw the special.");
            Require(!play.StarAsks.PitchAsked, "A release with LB up asked for the special.");
            Require(((bool)play.StarAsks.Free(Controls.Pad1)), "An unused modifier is spent.");
            return new GateCase { name = "star-let-go-before-release", phase = Phase(play), charge = pitch.Charge01 };
        }

        /// <summary>PH-16-R11: the intent may change while charging; LB pressed late in the charge counts at the release.</summary>
        static GateCase VerifyStarPressedWhileChargingCounts(MatchDirector play)
        {
            Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            for (var f = 0; f < 6; f++) Tick(play, play.TickSet, State(south: true), State());
            Tick(play, play.TickSet, State(south: true, lb: true), State());
            Tick(play, play.TickSet, State(lb: true), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null && pitch.Star, "LB pressed during the charge did not count at the release.");
            return new GateCase { name = "star-pressed-while-charging", phase = Phase(play), charge = pitch.Charge01 };
        }

        /// <summary>PH-16-R11: after the release the modifier changes nothing: an ordinary pitch stays ordinary.</summary>
        static GateCase VerifyStarAfterReleaseChangesNothing(MatchDirector play)
        {
            Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true), State());
            Tick(play, play.TickSet, State(), State());
            Require(Phase(play) == "Flight", "The ordinary release did not launch.");
            for (var f = 0; f < 4; f++) Tick(play, play.TickFlight, State(lb: true), State());
            var pitch = play._pitch;
            Require(!pitch.Star && !play.StarAsks.PitchAsked && !play.StarAsks.PitchShown,
                "LB after the release upgraded the pitch.");
            return new GateCase { name = "star-after-release-nothing", phase = Phase(play) };
        }

        /// <summary>PH-16-R17 on the second pad: Player 2 holds LB as the swing's South comes up; that is the Star Swing.</summary>
        static GateCase VerifyStarSwingOnPadTwo(MatchDirector play)
        {
            var match = Setup(play, Seats.Versus);
            Require(match.CanStarSwing, "The star fixture's offense cannot pay for its Star Swing.");
            Launch(play);
            for (var f = 0; f < 4; f++) Tick(play, play.TickFlight, State(), State(south: true, lb: true));
            Tick(play, play.TickFlight, State(lb: true), State(lb: true));
            var swing = play._swing;
            Require(play._swung && swing != null && swing.Swing && swing.Star,
                "Player 2's release with LB held was not the Star Swing.");
            Require(play.StarAsks.SwingAsked, "Player 2's request was not recorded.");
            Require(play.StarAsks.Mod(0).IsFreeNow(), "Player 1's LB was spent by Player 2's swing.");
            return new GateCase { name = "star-swing-pad2", phase = Phase(play), timingFrames = swing.TimingErrorFrames };
        }

        /// <summary>
        /// PH-16-R12: an unaffordable request is the ordinary pitch at the same release, spends nothing, and the tell names
        /// it on that tick; at the plate the match records the typed request as not afforded.
        /// </summary>
        static GateCase VerifyUnaffordableStarIsOrdinaryWithTell(MatchDirector play)
        {
            var match = Setup(play, Seats.One);
            match.SetDefenseStars(0);
            Require(!match.CanStarPitch && match.DefenseStars == 0, "The fixture did not empty the defense's pool.");
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            Tick(play, play.TickSet, State(south: true, lb: true), State());
            Require(!play.StarAsks.PitchShown, "The card read STAR on a pool that cannot pay.");
            Tick(play, play.TickSet, State(lb: true), State());
            var pitch = play._pitch;
            Require(Phase(play) == "Flight" && pitch != null && !pitch.Star, "The unaffordable request did not fly as the ordinary pitch.");
            Require(play.StarAsks.PitchAsked, "The unaffordable request was not recorded as asked.");
            var tell = play.StarAsks.Unavailable;
            Require(tell.HasValue && tell.Value.Action == StarAction.Pitch && tell.Value.Home == match.Top,
                "The unavailable tell did not name the defense's Star Pitch on the release tick.");
            ReachPlate(play, State(), State());
            var requests = match.StarRequestsThisPlay;
            var request = requests.SingleOrDefault(r => r.Action == StarAction.Pitch);
            Require(request != null && !request.Afforded && request.StarsBefore == 0,
                "The match did not record the typed request as not afforded.");
            Require(match.DefenseStars == 0, "The unaffordable request spent Stars.");
            return new GateCase { name = "star-unaffordable-ordinary-tell", phase = Phase(play), charge = pitch.Charge01 };
        }

        /// <summary>PH-16-R17: during the pitch LB is the modifier, not all-advance: a runner is not armed to tag and go.</summary>
        static GateCase VerifyStarInTheFlightIsNotAllAdvance(MatchDirector play)
        {
            var match = Setup(play, Seats.One, homeAtBat: true);
            Require(match.StationRunner(1, match.Offense.Roster.Last(c => c.Id != match.Batter.Id)), "Could not station the runner on first.");
            Tick(play, play.TickSet, State(lb: true), State());
            Launch(play);
            for (var f = 0; f < 3; f++) Tick(play, play.TickFlight, State(lb: true), State());
            Require(match.Runners.Where(r => r.Live && !r.IsBatter).All(r => !r.TagAndGo),
                "LB during the pitch armed all-advance.");
            return new GateCase { name = "lt-in-flight-not-all-advance", phase = Phase(play) };
        }

        /// <summary>
        /// PH-16-R10: the LB that asked for a Star Swing, still down after the release, is no all-advance and no cutoff
        /// until it comes up; up and pressed again, it is.
        /// </summary>
        static GateCase VerifySpentLbIsNoLiveVerb(MatchDirector play)
        {
            var match = Setup(play, Seats.One, homeAtBat: true);
            Require(match.CanStarSwing, "The star fixture's offense cannot pay for its Star Swing.");
            Launch(play);
            for (var f = 0; f < 4; f++) Tick(play, play.TickFlight, State(south: true, lb: true), State());
            Tick(play, play.TickFlight, State(lb: true), State());
            Require(play._swing?.Star == true, "The fixture's release was not the Star Swing.");
            Tick(play, play.TickAtBat, State(lb: true), State());
            Require(!((bool)play.StarAsks.Free(Controls.Pad1))
                && !Controls.Pad1.AllAdvanceWith(false) && !Controls.Pad1.CutoffWith(false) && Controls.Pad1.AllAdvanceWith(true),
                "The spent LB still reads as all-advance or the cutoff.");
            Tick(play, play.TickAtBat, State(), State());
            Require(((bool)play.StarAsks.Free(Controls.Pad1)), "LB released did not free the button.");
            Tick(play, play.TickAtBat, State(lb: true), State());
            Require(Controls.Pad1.AllAdvanceWith(((bool)play.StarAsks.Free(Controls.Pad1))),
                "A fresh LB press is not all-advance.");
            return new GateCase { name = "spent-lb-no-live-verb", phase = Phase(play) };
        }

        static void Launch(MatchDirector play)
        {
            play.Launch(new PitchCommand("fastball", 0, false));
            play._pitchAir = true;
            play._flight = 0.02f;
        }

        /// <summary>Carry the pitch to the plate plane with the batter's buttons as given; the plate's command, or null.</summary>
        static SwingCommand ReachPlate(MatchDirector play, GamepadState pad1, GamepadState pad2)
        {
            play._flight = play._pitchDur - Step * 0.5f;
            Tick(play, play.TickFlight, pad1, pad2);
            return play._swing;
        }

        static ChargeButtonState SwingButton(MatchDirector play) => play._plate.Swing;

        /// <summary>#582: Select opens the swap pick, the d-pad steps it, Select confirms; the mound changes and SET stays.</summary>
        static GateCase VerifySelectSwapPick(MatchDirector play, bool padTwo)
        {
            var match = padTwo ? Setup(play, Seats.Versus, homeAtBat: true) : Setup(play, Seats.One);
            play._t = (float)play._feel.PitcherReadySeconds + 0.01f;
            var before = match.Pitcher.Id;
            var select = State().WithButton(GamepadButton.West);
            Require(((bool)play.OpenDefenseSetup()), "Call time could not open Arrange defense.");
            var pick = play._swapPick;
            Require(pick != null, "Select did not open the swap pick.");
            var start = pick.Index;
            Tick(play, play.TickSet, State(), State());
            var cancel = State().WithButton(GamepadButton.East);
            Tick(play, play.TickSet, padTwo ? State() : cancel, padTwo ? cancel : State());
            Require(play._swapPick == null && match.Pitcher.Id == before && match.CanSwapPitcher,
                "Cancelling the window changed or consumed the pitcher swap.");
            Tick(play, play.TickSet, State(), State());
            Require(((bool)play.OpenDefenseSetup()), "Could not reopen Arrange defense.");
            pick = play._swapPick;
            Require(pick != null && pick.Index == start, "The cancelled window could not reopen.");
            Tick(play, play.TickSet, State(), State());
            var right = State().WithButton(GamepadButton.DpadRight);
            Tick(play, play.TickSet, padTwo ? State() : right, padTwo ? right : State());
            Require(pick.Index != start, "D-pad did not step the pick.");
            // A menu direction plus South must never become a pickoff, on either seat.
            Tick(play, play.TickSet, State(), State());
            var pickoff = State().WithButton(GamepadButton.DpadRight).WithButton(GamepadButton.South);
            Tick(play, play.TickSet, padTwo ? State() : pickoff, padTwo ? pickoff : State());
            Require(Phase(play) == "Set" && !match.LivePlay.Active, "Picker input leaked into a pickoff.");
            Require(!play._pitchButton.Armed, "Picker banked a pitch charge.");
            Require(Phase(play) == "Set" && match.Pitcher.Id == before, "The pick changed the mound before confirm.");
            Tick(play, play.TickSet, State(), State());
            var chosen = pick.Current.Who.Id;
            Tick(play, play.TickSet, padTwo ? State() : select, padTwo ? select : State());
            Require(play._swapPick != null, "Quick pitcher swap should keep defense editing open.");
            Require(match.Pitcher.Id == chosen && match.Pitcher.Id != before, "Select again did not put the pick on the mound.");
            Require(Phase(play) == "Set", "The swap left SET.");
            Tick(play, play.TickSet, State(), State());
            Require(Phase(play) == "Set", "Releasing the picker input launched a pitch.");
            return new GateCase { name = padTwo ? "select-swap-pick-pad2" : "select-swap-pick-pad1", phase = Phase(play) };
        }

        static double PitchMove(MatchDirector play, Seats seats, float stickX)
        {
            var match = Setup(play, seats);
            Tick(play, play.TickSet, State(stickX: stickX), State());
            return match.PitcherOffsetX;
        }

        static Match Setup(MatchDirector play, Seats seats, bool homeAtBat = false)
        {
            Neutral();
            var match = Match.Slice(play._content, innings: 3, seed: 1);
            if (homeAtBat) match.SkipToHomeCaptainAtBat();
            play._match = match;
            var lifecycle = play._matchSeats;
            lifecycle.Release();
            lifecycle.Bind(seats);
            Controls.UseDevices(new DeviceSeats(
                _pad1.deviceId, seats.BothHuman ? _pad2.deviceId : null));
            play.BeginSet();
            play._gateHold = true;
            play._t = 0f;
            return match;
        }

        static InputFrame Tick(MatchDirector play, Action<float> tick, GamepadState pad1, GamepadState pad2)
        {
            InputSystem.QueueStateEvent(_pad1, pad1);
            InputSystem.QueueStateEvent(_pad2, pad2);
            InputSystem.Update();
            Controls.Tick(Step, _rules);
            // The held modifier's guard ticks before any reader, as MatchDirector.Update does (PH-16-R10).
            play.StarAsks.Tick();
            var input = new InputFrame(
                Controls.Pad1.StickX, Controls.Pad1.StickY, Controls.Pad1.BuntThirdHeld || Controls.Pad1.BuntFirstHeld,
                Controls.Pad2.StickX, Controls.Pad2.StickY, Controls.Pad2.BuntThirdHeld || Controls.Pad2.BuntFirstHeld);
            tick(Step);
            return input;
        }

        static void Neutral()
        {
            if (_pad1 == null || _pad2 == null) return;
            InputSystem.QueueStateEvent(_pad1, State());
            InputSystem.QueueStateEvent(_pad2, State());
            InputSystem.Update();
            Controls.Tick(Step, _rules);
        }

        static GamepadState State(bool south = false, bool west = false, float stickX = 0, float stickY = 0,
            bool cycle = false, bool east = false, bool lt = false, bool rt = false, bool lb = false)
        {
            var state = new GamepadState
            {
                leftStick = new Vector2(stickX, stickY),
                leftTrigger = lb ? 1f : 0f,
                rightTrigger = south ? 1f : 0f
            };
            if (lt) state = state.WithButton(GamepadButton.West);
            if (rt) state = state.WithButton(GamepadButton.North);
            if (cycle) state = state.WithButton(GamepadButton.West);
            if (east) state = state.WithButton(GamepadButton.East);
            return west ? state.WithButton(GamepadButton.West) : state;
        }

        static void Write(Evidence evidence)
        {
            var output = Environment.GetEnvironmentVariable("GS_AT_BAT_INPUT_EVIDENCE") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "at-bat-input-gate.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonUtility.ToJson(evidence, true));
        }

        static string Phase(MatchDirector play) => play._phase.ToString();
        static bool IsFreeNow(this StarModifierState state) => StarModifier.IsFree(state);
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
            public float releaseAt, followTravel, finishError, releaseError, heldError;
            public string[] images;
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

        readonly record struct InputFrame(
            float Pad1X,
            float Pad1Y,
            bool Pad1Bunt,
            float Pad2X,
            float Pad2Y,
            bool Pad2Bunt);
    }
}
