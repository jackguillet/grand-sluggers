using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// MatchDirector as the host of its directors (#1042): the one-line forwarders the editor gates and the directors use,
    /// and each director's host interface. It holds no flow of its own; a verb that decides something belongs in a director.
    /// </summary>
    public sealed partial class MatchDirector : IStillHost, ISeatHost, IFlowHost, IFrontMenusHost, ITutorialFlowHost, ILineupHost, IAtBatHost, IInPlayHost, IActorHost, IItemHost, IDefenseSwapHost, IRunnerPlayHost
    {
        // The flow between plays (#1042): FlowDirector owns it; these names forward to it.
        FlowDirector _flow;
        FlowDirector Flow => _flow ??= new FlowDirector(Scene, Play, Choices, Pads, this);
        internal void TickFlow() => Flow.Tick();
        internal void OpenLineup() => Flow.OpenLineup();
        internal void TickLineup() => Lineup.Tick();
        void BeginTraining() => Flow.BeginTraining();
        TrainingDirector IFlowHost.Coach => _coach;
        void IFlowHost.EnsureCoach() { if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>(); }
        bool IFlowHost.TutorialOn => TutorialOn;
        FrontMenus IFlowHost.Front => Front;
        LineupFlow IFlowHost.Lineup => Lineup;
        Match IFlowHost.NewMatch() => NewMatch();
        void IFlowHost.BuildMatchScene() => BuildMatchScene();
        void IFlowHost.ForgetHighlight() => ForgetHighlight();
        bool IFlowHost.Replaying { get => _replaying; set => _replaying = value; }
        void IFlowHost.TickReplay(float dt) => TickReplay(dt);
        void IFlowHost.BeginGameOver() => BeginGameOver();
        void IFlowHost.ReleaseMatchSeats() => ReleaseMatchSeats();
        string IFlowHost.BannerText { set => _banner = value; }
        string IFlowHost.Sub { set => _sub = value; }
        void IFlowHost.BeginSet() => BeginSet();

        void BuildMatchScene()
        {
            _park.Build(_match.Park, _match.Night, _content.Rules, _content.Feel);
            _spec.Build(transform, DiamondGeometry.Of(_match.Rules));
            _items.Build(transform);
            _stars?.Build(transform);
        }

        void ForgetHighlight() { _clip = null; _hlPath = null; }

        // The title, the stadium screen and the captain board (#1042): FrontMenus owns them; the pick and the match are the flow's.
        FrontMenus _front;
        internal FrontMenus Front => _front ??= new FrontMenus(Scene, Play, Choices, this);
        internal int _fieldFocus => Front.FieldFocus;
        internal CaptainSelection _captains => Front.Captains;
        void OpenSelect() => Front.OpenSelect();
        void OpenField() => Front.OpenField();
        internal void OpenTitle() => Front.OpenTitle();
        void RebuildTitlePark() => Front.RebuildTitlePark();

        internal void OpenControlsBook()
        {
            _pausePad = Controls.Pad1;
            _match.SetPaused(true);
            _pauseHowTo = _pauseFromHowTo = true;
            _pausePage = 0; _t = 0;
            Controls.CatchPlay();
        }

        ExhibitionPick CurrentPick() => Choices.Pick();

        void ApplyPick(ExhibitionPick pick)
        {
            Choices.Take(pick);
            _match = NewMatch();
        }

        // The Tutorials section (#1042): TutorialFlow owns the menu, the lessons and the guided hooks; these names forward to it.
        TutorialFlow _tutorials;
        internal TutorialFlow Tutorials => _tutorials ??= new TutorialFlow(Scene, Play, Live, Choices, Pads, this);
        TutorialDirector _lessons => Tutorials.Lessons;
        GuidedTutorialSession _guided => Tutorials.Guided;
        bool TutorialOn => Tutorials.TutorialOn;
        bool TutorialModal => Tutorials.TutorialModal;
        TutorialFeedback GuidedNotice => Tutorials.GuidedNotice;
        void OpenTutorials() => Tutorials.OpenTutorials();
        void PrepareTutorial(string id) => Tutorials.PrepareTutorial(id);
        bool TickTutorialUi(float dt) => Tutorials.TickTutorialUi(dt);
        bool DrawTutorialUi() => Tutorials.DrawTutorialUi();
        bool GuidedAttempt(string id) => Tutorials.GuidedAttempt(id);
        void PrepareGuidedTutorial(TutorialLesson lesson) => Tutorials.PrepareGuidedTutorial(lesson);
        void BeginGuidedAttempt() => Tutorials.BeginGuidedAttempt();
        void GuidedFeedbackOpened() => Tutorials.GuidedFeedbackOpened();
        void GuidedObserve(string lesson, GuidedAction action) => Tutorials.GuidedObserve(lesson, action);
        void StickResetClosed(bool recalibrated) => Tutorials.StickResetClosed(recalibrated);
        void GuidedSeatRecovered(LineupSeat seat) => Tutorials.GuidedSeatRecovered(seat);
        void GuidedSeatsBound() => Tutorials.GuidedSeatsBound();
        TrainingDirector ITutorialFlowHost.Coach => _coach;
        void ITutorialFlowHost.EnsureCoach() { if (_coach == null) _coach = gameObject.AddComponent<TrainingDirector>(); }
        void ITutorialFlowHost.BuildScene() { BuildMatchScene(); ForgetHighlight(); }
        Match ITutorialFlowHost.NewMatch() => NewMatch();
        void ITutorialFlowHost.ReleaseMatchSeats() => ReleaseMatchSeats();
        bool ITutorialFlowHost.SeatsBound => _matchSeats.Bound;
        void ITutorialFlowHost.ClearLineup() => _lineup = null;
        void ITutorialFlowHost.BeginSet() => BeginSet();
        void ITutorialFlowHost.StartFly(AtBatResult hit, bool alreadyLive) => StartFly(hit, alreadyLive);
        void ITutorialFlowHost.StartRunnerPlay() => _inPlay.StartRunnerPlay(null);
        void ITutorialFlowHost.OpenField() => OpenField();
        void ITutorialFlowHost.BeginTraining() => BeginTraining();

        // The still gate's host (#1042): StillStaging owns the staging; the flow owns the match it stages and the switches.
        StillStaging _stills;
        internal StillStaging Stills => _stills ??= new StillStaging(Scene, Play, this);
        /// <summary>A HUD-off still is being captured: the play HUD draws nothing.</summary>
        bool CaptureMuteHud => _stills != null && _stills.MuteHud;
        string IStillHost.HomeCaptain => HomeCaptain;
        void IStillHost.Pick(string home, string away) { HomeCaptain = home; AwayCaptain = away; }
        void IStillHost.UsePark(string parkId, bool night) { ParkId = parkId; Night = night; }
        void IStillHost.ResetForStill(bool muteHud, bool feelDebug)
        {
            _mode = PlayMode.Exhibition;
            _forceMuteHud = muteHud; _feelDebug = feelDebug; _showTiming = false;
            _freezeCam = true; _gateHold = false; _turntable = false;
            _caught = false; _smash = 0; _juice.Clear();
        }
        void IStillHost.HoldStill(bool turntable)
        {
            _gateHold = true; _freezeCam = true;
            if (turntable) _turntable = true;
        }
        Match IStillHost.NewMatch() => NewMatch();
        void IStillHost.BeginSet() => BeginSet();
        bool IStillHost.HasLineup => _lineup != null;
        void IStillHost.OpenLineup() => OpenLineup();
        void IStillHost.HoldPitchInHand() => HoldPitchInHand();
        void IStillHost.CaptureReleaseFromHand() => CaptureReleaseFromHand();
        Vector3 IStillHost.Ball { get => _ball; set => _ball = value; }

        // The lineup screens (#1042): LineupFlow owns the screens and their pads; the flow owns the settings and the match they build.
        LineupFlow _lineupFlow;
        internal LineupFlow Lineup => _lineupFlow ??= new LineupFlow(Play, Scene, Choices, Pads, this);
        internal LineupScreens _lineup { get => Lineup.Screens; set => Lineup.Screens = value; }
        TutorialDirector ILineupHost.Lessons => _lessons;
        void ILineupHost.GuidedObserve(string lesson, GuidedAction action) => GuidedObserve(lesson, action);
        void ILineupHost.GuidedFeedbackOpened() => GuidedFeedbackOpened();
        void ILineupHost.OpenSelect() => OpenSelect();
        void ILineupHost.BeginSet() => BeginSet();

        void IFrontMenusHost.ApplyPick(ExhibitionPick pick) => ApplyPick(pick);
        void IFrontMenusHost.BeginExhibition()
        {
            _mode = PlayMode.Exhibition;
            _match = NewMatch();
            BuildMatchScene();
        }
        Match IFrontMenusHost.NewMatch() => NewMatch();
        void IFrontMenusHost.EndReplay() { _clip = null; _hlPath = null; _replaying = false; }
        void IFrontMenusHost.ReleaseMatchSeats() => ReleaseMatchSeats();
        void IFrontMenusHost.SeatsChosen() { BindMatchSeats(); GuidedSeatsBound(); }
        GuidedTutorialSession IFrontMenusHost.Guided => _guided;
        void IFrontMenusHost.GuidedObserve(string lesson, GuidedAction action) => GuidedObserve(lesson, action);
        void IFrontMenusHost.OpenTutorials() => OpenTutorials();
        void IFrontMenusHost.OpenControlsBook() => OpenControlsBook();
        void IFrontMenusHost.OpenLineup() => OpenLineup();

        TrainingDirector ISeatHost.Coach => _coach;
        bool ISeatHost.Exhibition => _mode == PlayMode.Exhibition;
        bool ISeatHost.Pad1Home => Pad1Home;
        bool ISeatHost.VersusWanted => _versusWanted;

        // ---- The at-bat and the directors it calls on (#1042) ----
        // The at-bat (#1042): AtBatDirector owns SET → contact and its state; these names forward to it.
        AtBatDirector _atBat;
        internal AtBatDirector AtBat => _atBat ??= new AtBatDirector(Scene, Play, Live, Pads, this);
        internal ChargeButtonState _pitchButton { get => AtBat.PitchButton; set => AtBat.PitchButton = value; }

        /// <summary>
        /// The batting seat's plate buttons (spec §5.1, §5.8): the swing button, the two bunt triggers and the East / G
        /// cancel, stepped by the sim (<see cref="PlateButtons.Advance"/>) every frame in the spec's order. It also holds
        /// the leak guards (PH-13-R1, PH-14-R6): a trigger held for a bunt at contact and a cancel press the plate took
        /// are spent until they come up. It belongs to one pad (<see cref="_plateSeat"/>); a new batting pad starts at rest.
        /// </summary>
        internal PlateButtonsState _plate { get => Play.Plate; set => Play.Plate = value; }


        internal StarRequests StarAsks => AtBat.StarAsks;

        /// <summary>The at-bat's verbs, for the flow and the editor gates that drive them.</summary>
        internal void TickAtBat(float dt) => AtBat.TickAtBat(dt);
        internal void BeginSet() => AtBat.BeginSet();
        internal void TickSet(float dt) => AtBat.TickSet(dt);
        internal void Launch(PitchCommand pitch) => AtBat.Launch(pitch);
        internal void TickFlight(float dt) => AtBat.TickFlight(dt);
        internal void StartFly(AtBatResult hit, bool alreadyLive = false) => AtBat.StartFly(hit, alreadyLive);
        internal string ShownPitchType => AtBat.ShownPitchType;
        void HoldPitchInHand() => AtBat.HoldPitchInHand();
        void CaptureReleaseFromHand() => AtBat.CaptureReleaseFromHand();

        TrainingDirector IAtBatHost.Coach => _coach;
        bool IAtBatHost.TutorialOn => TutorialOn;
        void IAtBatHost.BindSeats() => BindMatchSeats();
        Match IAtBatHost.NextTrainingMatch() { Seed++; return _coach.MakeMatch(_content, Seed); }
        string IAtBatHost.BannerText { set => _banner = value; }
        string IAtBatHost.Sub { set => _sub = value; }
        float IAtBatHost.Smash { set => _smash = value; }
        void IAtBatHost.Banner() => Banner();
        void IAtBatHost.BeginResult() => BeginResult();
        InPlayDirector IAtBatHost.InPlay => _inPlay;
        JuiceDirector IAtBatHost.Juice => _juice;
        StealDirector IAtBatHost.Steal => Steal;
        DefenseSwapWindow IAtBatHost.Swap => Swap;
        ItemToss IAtBatHost.Toss => Toss;

        /// <summary>Whether <paramref name="pad"/>'s <paramref name="trigger"/> may mean any verb on this tick (PH-14-R6).</summary>
        internal bool TriggerFree(Controls.Pad pad, BuntSide trigger) => Pads.TriggerFree(pad, trigger);

        /// <summary>Whether <paramref name="pad"/>'s East / G may mean a dive, a dash or a skip on this tick (PH-13-R1).</summary>
        internal bool CancelFree(Controls.Pad pad) => Pads.CancelFree(pad);

        /// <summary>The pickoff (§4.5, D3): a runner on the bag is the beat; a runner who broke is the live runner play.</summary>
        StealDirector _steal;
        internal StealDirector Steal => _steal ??= new StealDirector(gameObject, Play, this);

        /// <summary>
        /// Select opens the defense window. South picks two positions; Select is the pitcher shortcut.
        /// East cancels a pending pick or closes. All baseball input waits for the window.
        /// </summary>
        /// <summary>Call time's Arrange defense: open SET's swap window.</summary>
        internal bool OpenDefenseSetup() => Swap.TryOpen();

        // The live play (#1042): InPlayDirector owns the play; the pads, the seats, the items and the result beat are the flow's.
        LiveSeats LiveSeatsNow() => Pads.LiveNow();
        internal LivePadInput FieldInput() => Pads.FieldInput();
        LivePadInput RunInput() => Pads.RunInput();

        /// <summary>A frame of the live play, for the editor gates that drive it.</summary>
        internal void TickLive(float dt) => _inPlay.Tick(dt);
        internal PlayKind LiveKind() => _inPlay.LiveKind();
        void StartRunnerPlay(PlayEvent pitch) => _inPlay.StartRunnerPlay(pitch);
        void SyncFromLive() => _inPlay.SyncFromLive();
        void AimLive() => _inPlay.AimLive();
        Character PlayFielder() => _inPlay.PlayFielder();
        bool BuddySet => _inPlay.BuddySet;
        (double X, double Z) WallPlant(FieldingPreview pre) => _inPlay.WallPlant(pre);

        LivePadInput IInPlayHost.FieldInput() => FieldInput();
        LivePadInput IInPlayHost.RunInput() => RunInput();
        void IInPlayHost.ClearThrowTarget() => FieldPad.ClearThrowTarget();
        LiveSeats IInPlayHost.LiveSeatsNow() => LiveSeatsNow();
        JuiceDirector IInPlayHost.Juice => _juice;
        TutorialSession IInPlayHost.FieldLesson => TutorialOn ? _coach.Tutorial : null;
        void IInPlayHost.OnFieldResult(FieldingResult result) => _coach?.OnField(result, _match);
        void IInPlayHost.TickItem(float dt) => Toss.Tick(dt);
        bool IInPlayHost.ItemFlying => Toss.Flying;
        void IInPlayHost.ItemSmashed() => Toss.Smashed();
        void IInPlayHost.Banner() => Banner();
        void IInPlayHost.BeginResult() => BeginResult();
        Vector3 IInPlayHost.SmashLook() => AtBat.SmashLook();
        float IInPlayHost.Smash { get => _smash; set => _smash = value; }
        string IInPlayHost.Sub { set => _sub = value; }
        void IInPlayHost.RestartClock() => _t = 0;

        // The bodies (#1042): ActorDirector draws them; the swing clocks it and the at-bat share live in PlayState.
        internal float _committedSwingT { get => Play.CommittedSwingT; set => Play.CommittedSwingT = value; }
        /// <summary>Seconds from the press to the committed take's Contact mark (D13); NaN until a swing commits.</summary>
        float _swingContactSec { get => Play.SwingContactSec; set => Play.SwingContactSec = value; }
        /// <summary>A frame of the bodies, for the editor gates that draw them.</summary>
        internal void DrawActors(float dt) => _actors.Draw(dt);

        bool IActorHost.TutorialModal => TutorialModal;
        bool IActorHost.Turntable => _turntable;
        bool IActorHost.Replaying => _replaying;
        bool IActorHost.HumanBats => HumanBats;
        bool IActorHost.HumanPitches => HumanPitches;
        bool IActorHost.HumanOwnsThrow => HumanOwnsThrow;
        IBatterTells IActorHost.Batter => AtBat;
        bool IActorHost.PlateSwingArmed => _plate.Swing.Armed;
        float IActorHost.PitchCharge => AtBat.PitchCharge;
        string IActorHost.ShownPitchType => AtBat.ShownPitchType;
        ItemToss IActorHost.Toss => Toss;
        float IActorHost.SwingContactSec(SwingCommand swing) => AtBat.SwingContactSec(swing);
        CursorOval IActorHost.ShowCursor() => AtBat.ShowCursor();
        string IActorHost.ArmedStarSwing => AtBat.ArmedStarSwing;
        void IActorHost.HoldBallInGlove() => HoldBallInGlove();
        void IActorHost.OnRun() { if (TrainingOn) _coach.OnRun(_match); }
        StealDirector IActorHost.Steal => Steal;
        JuiceDirector IActorHost.Juice => _juice;
        LineupScreens IActorHost.Lineup => _lineup;
        ExhibitionPick IActorHost.CurrentPick() => CurrentPick();
        Vector2 IActorHost.FieldStick => new Vector2(FieldPad.StickX, FieldPad.StickY);

        // The on-deck item (#1042): ItemToss owns the pick, the target and the throw; the flow owns the subtitle.
        ItemToss _toss;
        internal ItemToss Toss => _toss ??= new ItemToss(Scene, Play, Live, Pads, this);
        TrainingDirector IItemHost.Coach => _coach;
        string IItemHost.Sub { set => _sub = value; }

        // SET's Arrange defense window (#1042): DefenseSwapWindow owns the pick and the swaps; the flow resets the pitch selection.
        DefenseSwapWindow _swap;
        internal DefenseSwapWindow Swap => _swap ??= new DefenseSwapWindow(Play, Pads, this);
        TrainingDirector IDefenseSwapHost.Coach => _coach;
        void IDefenseSwapHost.PitcherChanged() => AtBat.PitchSelect = PitchSelectionState.Reset;

        // The pre-contact runner play (#1042): StealDirector owns the pickoff and the pre-contact clock; the flow owns the result beat.
        TutorialSession IRunnerPlayHost.Lesson => TutorialOn ? _coach.Tutorial : null;
        LiveSeats IRunnerPlayHost.LiveSeatsNow() => LiveSeatsNow();
        void IRunnerPlayHost.StartRunnerPlay() => StartRunnerPlay(null);
        void IRunnerPlayHost.EndWith(PlayEvent play) { _last = play; Banner(); BeginResult(); }
        string IRunnerPlayHost.Banner { set => _banner = value; }
    }
}
