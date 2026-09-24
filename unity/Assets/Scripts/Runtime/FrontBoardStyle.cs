using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Shared captain and lineup board palette. Seat colors stay fixed when home/away changes.</summary>
    public static class FrontBoardStyle
    {
        public static readonly Color Ink = new Color(.035f, .065f, .095f);
        public static readonly Color Panel = new Color(.075f, .12f, .16f);
        public static readonly Color Raised = new Color(.12f, .19f, .24f);
        public static readonly Color Gold = new Color(1f, .73f, .24f);
        public static readonly Color Blue = new Color(.31f, .79f, .95f);
        public static readonly Color Muted = new Color(.69f, .77f, .82f);
        public static Color Seat(LineupSeat seat) => seat == LineupSeat.Pad1 ? Gold : Blue;
    }
}
