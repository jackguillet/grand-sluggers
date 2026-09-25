using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The pitch and the play in flight (#1042): the match, the flow phase, the commands and results the at-bat, the
    /// flight and the live play hand each other. <see cref="MatchDirector"/> creates it once and hands it to each director.
    /// It is presentation state; the sim's <see cref="Sim.Match"/> stays the owner of the baseball.
    /// </summary>
    internal sealed class PlayState
    {
        public Match Match { get; set; }
        public MatchDirector.Phase Phase { get; set; } = MatchDirector.Phase.Title;
        public PitchCommand Pitch { get; set; }
        public SwingCommand Swing { get; set; }
        public PlayEvent Last { get; set; }
        /// <summary>The batted ball waiting for the flight, or the one in it.</summary>
        public AtBatResult Pending { get; set; }
        public FieldingPreview Preview { get; set; }
        /// <summary>The drawn flight path of the pending ball.</summary>
        public Sample[] Path { get; set; }
        /// <summary>The batter's charge, 0..1.</summary>
        public float Charge { get; set; }
        public float BreakX { get; set; }
        /// <summary>Seconds the pitch spends in the air, and whether it is still in the air.</summary>
        public float PitchDur { get; set; } = 0.5f;
        public bool PitchAir { get; set; }
        /// <summary>Seconds since contact.</summary>
        public float Flight { get; set; }
        /// <summary>The batter swung at this pitch; the square clock (§7.3) while the batter shows bunt; the drawn rubber offset.</summary>
        public bool Swung { get; set; }
        public float SquareSec { get; set; }
        public float MoundX { get; set; }
        /// <summary>The committed swing's clock, and seconds from the press to its take's Contact mark (D13); NaN until a swing commits.</summary>
        public float CommittedSwingT { get; set; } = (float)AtBatMotion.SwingNotStarted;
        public float SwingContactSec { get; set; } = float.NaN;
        /// <summary>The on-deck item (§12): the pick, its target, and the throw in flight.</summary>
        public int ItemPick { get; set; }
        public Character ItemTarget { get; set; }
        public bool ItemThrown { get; set; }
        public bool ItemFlying { get; set; }
        public float ItemFly { get; set; }
        public string ItemId { get; set; } = "";
        /// <summary>Where the ball is drawn this frame.</summary>
        public Vector3 Ball { get; set; }
        /// <summary>Where the ball left the pitcher's hand.</summary>
        public Vector3 ReleaseFrom { get; set; }

        /// <summary>
        /// The table this match plays on (spec §0.3): the catalog's tables at the match's difficulty rung and
        /// in the match's park (<c>RulesTable.AtLevel(...).AtPark(...)</c>). Every reader that asks the sim
        /// about the ball is handed this, never the catalog's global table, so a park that names an
        /// environment is read the same here as in the sim (FD-03). Before a match exists it is the catalog's.
        /// </summary>
        public RulesTable Rules(ContentCatalog content) => Match != null ? Match.Rules : content?.Rules;
    }
}
