using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The live play as the client mirrors it each frame from the sim's <see cref="LivePlaySystem"/> (#1042): the glove
    /// and who wears it, the catch, the throw in flight, the close play, the bag stamp, the body timers. The in-play
    /// director writes it; the actors, the cameras and the HUD read it. The sim stays the owner of the play — nothing here
    /// decides an out.
    /// </summary>
    public sealed class LiveFieldState
    {
        /// <summary>A human seat fields this play.</summary>
        public bool PlayerFielding { get; set; }
        /// <summary>The glove's position on the field, and where each fielder's glove is.</summary>
        public double GloveX { get; set; }
        public double GloveZ { get; set; }
        public Dictionary<string, (double X, double Z)> GloveAt { get; } = new Dictionary<string, (double X, double Z)>();
        public bool Caught { get; set; }
        public bool Buddy { get; set; }
        /// <summary>The position that wears the glove ring, the one the switch offers, the buddy, and the cover.</summary>
        public string GlovePos { get; set; } = "P";
        public string SwitchPos { get; set; } = "";
        public string BuddyPos { get; set; } = "";
        public bool BuddyWindow { get; set; }
        public string CoverPos { get; set; } = "";
        /// <summary>The body timers: dive, jump, the swap lock, the impact recoil, and the fumble.</summary>
        public float DiveT { get; set; }
        public float JumpT { get; set; }
        public float SwapLock { get; set; }
        public float RecoilT { get; set; }
        public bool Bobbling { get; set; }
        /// <summary>The throw in flight: its clock, its ends, its bag, who threw it, and the armed one.</summary>
        public bool Throwing { get; set; }
        public float ThrowT { get; set; }
        public float ThrowDur { get; set; }
        public Vector3 ThrowFrom { get; set; }
        public Vector3 ThrowTo { get; set; }
        public int ThrowBag { get; set; }
        public string ThrowFromPos { get; set; } = "";
        public ThrowResult ArmedThrow { get; set; }
        /// <summary>The close play at a bag, its bag, and whether its icon shows.</summary>
        public bool ClosePlay { get; set; }
        public int CloseBag { get; set; }
        public bool CloseIcon { get; set; }
        /// <summary>The bag stamp: its word, anchor, age and hold.</summary>
        public string BagStamp { get; set; } = "";
        public StampAnchor BagStampAnchor { get; set; } = StampAnchor.Dirt;
        public float BagStampT { get; set; }
        public float BagStampHold { get; set; }
        /// <summary>The CPU's fielding result for a play no human fields.</summary>
        public FieldingResult CpuField { get; set; }
        /// <summary>The typed outcome's bodies at Time (§10.6): the result beat draws these, not the position table.</summary>
        public IReadOnlyList<FieldBody> ResultBodies { get; set; }
    }
}
