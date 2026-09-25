using System.Collections.Generic;
using GrandSluggers.Sim;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The match's scene (#1042): the data it plays on and the Unity objects that present it. <see cref="MatchDirector"/>
    /// creates it once and hands it to each director, so a director reads the park, the cameras and the bodies through
    /// this object instead of through MatchDirector's fields. It holds references only; the objects keep their own state.
    /// </summary>
    public sealed class MatchScene
    {
        public ContentCatalog Content { get; set; }
        public FeelTable Feel { get; set; }
        public ParkView Park { get; set; }
        public CameraRig Rig { get; set; }
        public CameraDirector Cam { get; set; }
        public SpecialFx Fx { get; set; }
        public ItemView Items { get; set; }
        public LandingRing Ring { get; set; }
        public StrikeZone Zone { get; set; }
        public AudioBus Audio { get; set; }
        public StarMeter Stars { get; set; }
        public CardToy Card { get; set; }
        public LogoToy Logo { get; set; }
        public ChemToy Chem { get; set; }
        /// <summary>Every body on the field, by character id.</summary>
        public Dictionary<string, HeroActor> Heroes { get; } = new Dictionary<string, HeroActor>();
    }
}
