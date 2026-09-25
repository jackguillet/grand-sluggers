using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// What the menus chose (#1042): the mode, the exhibition's pick and its switches, the seed, and the match settings.
    /// <see cref="MatchDirector"/> creates it once and hands it to the menu directors, so a menu reads and writes the
    /// choice here instead of through MatchDirector's fields. The match built from it is <see cref="PlayState.Match"/>.
    /// </summary>
    internal sealed class FlowChoices
    {
        public MatchDirector.PlayMode Mode { get; set; }
        /// <summary>The pick: the captains, the park, and whether pad 1 sits home.</summary>
        public string HomeCaptain { get; set; } = ExhibitionPick.Default.Home;
        public string AwayCaptain { get; set; } = ExhibitionPick.Default.Away;
        public string ParkId { get; set; } = ExhibitionPick.DefaultPark;
        public bool Pad1Home { get; set; } = true;
        public bool Night { get; set; }
        /// <summary>The hazards switch (FD-10): on by default; the title and the field toggle it for an exhibition.</summary>
        public bool Hazards { get; set; } = true;
        /// <summary>The stadium screen's player count: a second pad wanted on the captain board.</summary>
        public bool VersusWanted { get; set; }
        public int Seed { get; set; } = 7;
        public int Innings { get; set; } = 3;
        /// <summary>The CPU difficulty rung (cpu.json easy / normal / hard), picked on the title next to the innings.</summary>
        public string Difficulty { get; set; } = "normal";
        /// <summary>The lineup's match settings; a guided lesson may lend its own in their place.</summary>
        public ExhibitionSettings Settings { get; set; } = new ExhibitionSettings();
        public PracticeLesson PracticePick { get; set; } = PracticeLesson.Pitching;

        public bool Exhibition => Mode == MatchDirector.PlayMode.Exhibition;

        public ExhibitionPick Pick() => new(HomeCaptain, AwayCaptain, ParkId, Pad1Home);

        /// <summary>Take a pick's captains, park and seats. The caller builds the match it names.</summary>
        public void Take(ExhibitionPick pick)
        {
            HomeCaptain = pick.Home;
            AwayCaptain = pick.Away;
            ParkId = pick.Park;
            Pad1Home = pick.Pad1Home;
        }

        /// <summary>The match settings' innings and difficulty become the flow's.</summary>
        public void FollowSettings()
        {
            Innings = Settings.Innings;
            Difficulty = Settings.Difficulty;
        }
    }
}
