using System.Collections.Generic;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The front of house before the lineup (its own class since #1042): the title menu, the stadium screen with its
    /// continent map (WD-17 A), and the captain board. The choices are the sim's (<see cref="CaptainSelection"/>,
    /// <see cref="MapPicker"/>, <see cref="ExhibitionPick"/>); this class turns the pads into them, keeps the title park
    /// in step with the pick, and draws the stadium screen. The pick itself, the match it builds, the seats and the
    /// screens on either side are the flow's (<see cref="IFrontMenusHost"/>).
    /// </summary>
    internal sealed class FrontMenus
    {
        readonly MatchScene _scene;
        readonly PlayState _play;
        readonly IFrontMenusHost _host;
        MenuNav.Gate _x, _y, _x2;
        /// <summary>The continent map on the stadium row (WD-17 A).</summary>
        readonly MapPicker _map = new MapPicker();

        public FrontMenus(MatchScene scene, PlayState play, IFrontMenusHost host)
        {
            _scene = scene; _play = play; _host = host;
        }

        /// <summary>The title menu's row, the stadium screen's row, and the captain board while it is up.</summary>
        public int TitleFocus { get; private set; }
        public int FieldFocus { get; private set; }
        public CaptainSelection Captains { get; private set; }

        ContentCatalog Content => _scene.Content;
        string ParkId => _host.CurrentPick().Park;

        public void TickTitle()
        {
            var dy = _y.Tick(Controls.MenuY, Controls.MenuTapY, Time.unscaledDeltaTime);
            if (dy != 0) TitleFocus = (TitleFocus + (dy > 0 ? 3 : 1)) % 4;
            _scene.Cam.Cut("title");
            if (!Controls.SouthDown || _play.T <= .15f) return;
            if (TitleFocus == 1) { _host.OpenTutorials(); return; }
            if (TitleFocus == 2) { _host.OpenControlsBook(); return; }
            if (TitleFocus == 3) { Application.Quit(); return; }
            _host.BeginExhibition();
            OpenField();
        }

        public void OpenTitle()
        {
            if (_host.Guided != null)
            {
                _host.OpenTutorials();
                return;
            }
            _host.ReleaseMatchSeats();
            _play.Phase = MatchDirector.Phase.Title;
            _play.T = 0;
            _host.EndReplay();
            RebuildTitlePark();
            _scene.Cam.Cut("title");
        }

        /// <summary>The park as this exhibition will play it behind the menus: tonight's instances, and none with hazards off (FD-10).</summary>
        public void RebuildTitlePark()
        {
            if (_scene.Park == null || Content == null) return;
            if (!Content.Parks.TryGetValue(ParkId, out var park)) return;
            var hazards = _host.Hazards || !_host.ExhibitionMode;
            _scene.Park.Build(PlayedPark.Of(park, _host.Night, hazards, Content.Rules.Hazards), _host.Night, Content.Rules, Content.Feel);
            if (_play.Phase == MatchDirector.Phase.Title)
                _scene.Cam?.Cut("title");
        }

        public void OpenSelect()
        {
            _host.ReleaseMatchSeats();
            _play.Match = _host.NewMatch();
            _play.Phase = MatchDirector.Phase.Select;
            Captains = new CaptainSelection(Content, _host.CurrentPick(), _host.VersusWanted);
            _play.T = 0;
            _x.Catch(Controls.Pad1.MenuAxisX);
            _y.Catch(Controls.Pad1.MenuAxisY);
            _x2.Catch(Controls.Pad2.MenuAxisX);
            _host.EndReplay();
            _scene.Cam.Cut("select");
        }

        public void TickSelect()
        {
            if (Captains.Versus && !Controls.Pad2.Present)
                Controls.TryRecoverMatchSeat(LineupSeat.Pad2);
            var p1 = Controls.Pad1;
            var p2 = Controls.Pad2;
            var dt = Time.unscaledDeltaTime;
            var dx = _x.Tick(p1.MenuAxisX, p1.MenuTapX, dt);
            var dx2 = _x2.Tick(p2.MenuAxisX, p2.MenuTapX, dt);
            Captains.Move(Captains.ActiveOne, dx);
            if (Captains.Versus && p2.Present) Captains.Move(1, dx2);
            if (_play.T <= .15f) return;
            if (p1.EastDown)
            {
                if (Captains.Back(0)) OpenField();
                return;
            }
            if (Captains.Versus && p2.EastDown) Captains.Back(1);
            if (p1.SouthDown) Captains.Confirm(Captains.ActiveOne);
            if (Captains.Versus && p2.Present && p2.SouthDown) Captains.Confirm(1);
            if (!Captains.Complete || (Captains.Versus && !p2.Present)) return;
            _host.ApplyPick(Captains.ApplyTo(_host.CurrentPick()));
            _host.SeatsChosen();
            if (_host.Guided?.Phase != TutorialPhase.Feedback) _host.OpenLineup();
        }

        void WantVersus(bool versus)
        {
            if (_host.VersusWanted == versus) return;
            _host.VersusWanted = versus;
            if (versus)
                _x2.Catch(Controls.Pad2.MenuAxisX);
        }

        public void OpenField()
        {
            _host.ReleaseMatchSeats();
            _play.Phase = MatchDirector.Phase.Field;
            FieldFocus = 0;
            _y.Catch(Controls.MenuY);
            _play.T = 0;
            _x.Catch(Controls.MenuX);
            _host.EndReplay();
            _play.Match = _host.NewMatch();
            RebuildTitlePark();
            _scene.Cam.Play("field");
        }

        public void TickField()
        {
            var dy = _y.Tick(Controls.MenuY, Controls.MenuTapY, Time.unscaledDeltaTime);
            var dx = _x.Tick(Controls.MenuX, Controls.MenuTapX, Time.unscaledDeltaTime);
            if (_map.IsOpen) { TickMap(dx, dy); return; }
            if (dy != 0) FieldFocus = (FieldFocus + (dy > 0 ? 5 : 1)) % 6;
            if (_play.T <= .15f) return;
            if (Controls.EastDown) { OpenTitle(); return; }
            // The stadium row opens the map: a change on it (left, right or South) is a pick on the map.
            if (FieldFocus == 0 && (dx != 0 || Controls.SouthDown)) { _map.Open(ParkId); _play.T = 0; return; }
            if (dx != 0 || Controls.SouthDown)
            {
                if (FieldFocus == 1) _host.Night = !_host.Night;
                if (FieldFocus is 0 or 1) _host.GuidedObserve("T-G07", GuidedAction.StadiumChosen);
                if (FieldFocus == 2) _host.Hazards = !_host.Hazards;
                if (FieldFocus == 3) WantVersus(!_host.VersusWanted);
                if (FieldFocus == 4) _host.ApplyPick(ExhibitionPick.ToggleSeat(_host.CurrentPick()));
                if (FieldFocus == 5 && Controls.SouthDown) { OpenSelect(); return; }
                RebuildTitlePark();
            }
            _scene.Cam.Play("field");
        }

        /// <summary>The map is up: the stick moves the cursor and the postcard follows; South plays the park, East keeps yours.</summary>
        void TickMap(int dx, int dy)
        {
            if (_map.Move(Content.World, dx, dy) is { } park) { _host.ApplyPick(_host.CurrentPick() with { Park = park }); RebuildTitlePark(); }
            if (_play.T > .15f && Controls.SouthDown)
            {
                _host.ApplyPick(_host.CurrentPick() with { Park = _map.Confirm() });
                _host.GuidedObserve("T-G07", GuidedAction.StadiumChosen);
                RebuildTitlePark();
            }
            else if (_play.T > .15f && Controls.EastDown)
            {
                _host.ApplyPick(_host.CurrentPick() with { Park = _map.Cancel() });
                RebuildTitlePark();
            }
            _scene.Cam.Play("field");
        }

        /// <summary>The stadium screen: the map while it is up, else the postcard's HUD and the setup rows.</summary>
        public void DrawField()
        {
            var night = _host.Night;
            var hazards = _host.Hazards;
            if (_map.IsOpen) { SetupSheet.Map(Content, _map.Park, night, hazards); return; }
            HudView.Field(ParkId, ParkName(ParkId), night, hazards, FieldHazardsLine(), FieldCardLines());
            SetupSheet.FieldFocus(FieldFocus, ParkName(ParkId), night, hazards, _host.VersusWanted, _host.CurrentPick().Pad1Home);
        }

        /// <summary>The field card of the park as this exhibition will play it (F8-a): tonight's instances, the hazards switch applied.</summary>
        IReadOnlyList<string> FieldCardLines() =>
            Content != null && Content.Parks.TryGetValue(ParkId, out var park)
                ? CarnivalFront.FieldCard(PlayedPark.Of(park, _host.Night, _host.Hazards, Content.Rules.Hazards), Content.Rules)
                : null;

        string FieldHazardsLine() =>
            Content != null && Content.Parks.TryGetValue(ParkId, out var park)
                ? CarnivalFront.HazardsOffLine(park, _host.Night, _host.Hazards, Content.Rules.Hazards)
                : null;

        /// <summary>A park's name on the card, or its id when the catalog does not know it.</summary>
        public string ParkName(string parkId) =>
            Content != null && Content.Parks.TryGetValue(parkId, out var park) ? park.Name : parkId;
    }

    /// <summary>What <see cref="FrontMenus"/> reads from the flow: the pick and its switches, the match, the seats, and the screens on either side.</summary>
    internal interface IFrontMenusHost
    {
        ExhibitionPick CurrentPick();
        /// <summary>Take this pick (captains, park, seats) and build its match.</summary>
        void ApplyPick(ExhibitionPick pick);
        bool Night { get; set; }
        bool Hazards { get; set; }
        bool VersusWanted { get; set; }
        /// <summary>The flow is in an exhibition (a practice's park keeps its hazards).</summary>
        bool ExhibitionMode { get; }
        /// <summary>Play from the title: an exhibition's match and its park, specials and items built.</summary>
        void BeginExhibition();
        Match NewMatch();
        /// <summary>Stop any replay and forget the highlight.</summary>
        void EndReplay();
        void ReleaseMatchSeats();
        /// <summary>The captains are chosen: bind the match's seats and tell a guided lesson.</summary>
        void SeatsChosen();
        GuidedTutorialSession Guided { get; }
        void GuidedObserve(string lesson, GuidedAction action);
        void OpenTutorials();
        void OpenControlsBook();
        void OpenLineup();
    }
}
