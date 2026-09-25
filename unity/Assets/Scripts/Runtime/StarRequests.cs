using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The special's modifier at the couch (spec §12, PH-16-R10, R11, R12, R17; its own class since #1042): each seat's
    /// held modifier and its leak guard (the step is the sim's <see cref="StarModifier"/>), what each side's release
    /// asked for, whether the card reads STAR, and the "special unavailable" tell. The match is handed the request
    /// itself (<see cref="AsReleased(PitchCommand)"/>) so it settles it and records the <see cref="StarRequest"/>.
    /// </summary>
    internal sealed class StarRequests
    {
        readonly StarModifierState[] _mods = new StarModifierState[2];

        /// <summary>Each seat's modifier, by pad index (LT on either controller).</summary>
        public StarModifierState Mod(int padIndex) => _mods[padIndex];

        /// <summary>The special each side asked for at its accepted release, as the modifier read it.</summary>
        public bool PitchAsked { get; set; }
        public bool SwingAsked { get; set; }

        /// <summary>The card reads STAR: the modifier is down and the pool can pay, for the pitcher and the batter.</summary>
        public bool PitchShown { get; set; }
        public bool SwingShown { get; set; }

        /// <summary>The "special unavailable" tell (PH-16-R12) and when it began, on the unscaled clock.</summary>
        public BroadcastHud.StarUnavailableTell? Unavailable { get; private set; }
        public float UnavailableAt { get; private set; } = -99f;

        /// <summary>One tick of both seats' modifiers, before any reader: a modifier that came up is free again.</summary>
        public void Tick()
        {
            _mods[0] = StarModifier.Tick(_mods[0], Controls.Pad1.StarHeld);
            _mods[1] = StarModifier.Tick(_mods[1], Controls.Pad2.StarHeld);
        }

        /// <summary>A new pitch: nothing asked, nothing shown.</summary>
        public void NewPitch()
        {
            PitchShown = SwingShown = false;
            PitchAsked = SwingAsked = false;
        }

        /// <summary>Whether this seat may make a fresh Star request. Runner orders are independent.</summary>
        public bool Free(Controls.Pad pad) =>
            pad.Index < 0 || pad.Index >= _mods.Length || StarModifier.IsFree(_mods[pad.Index]);

        /// <summary>The modifier is down and would ask for the special at a release on this tick.</summary>
        public bool Ready(Controls.Pad pad) => pad.Index >= 0 && pad.StarHeld && Free(pad);

        /// <summary>An accepted release on <paramref name="pad"/>: the special it asks for, and the hold spent if it did.</summary>
        public bool Release(Controls.Pad pad)
        {
            if (pad.Index < 0 || pad.Index >= _mods.Length) return false;
            var release = StarModifier.Release(_mods[pad.Index], pad.StarHeld);
            _mods[pad.Index] = release.Next;
            return release.Request;
        }

        /// <summary>
        /// A released request, as the match will record it (<see cref="Match.PitchStarRequest"/>, the record
        /// <see cref="PlayOutcome.Stars"/> carries): not afforded, the scorebug's Stars flash and name it on this tick.
        /// </summary>
        public void Note(StarRequest request)
        {
            var tell = BroadcastHud.StarUnavailable(request);
            if (tell == null) return;
            Unavailable = tell;
            UnavailableAt = Time.unscaledTime;
        }

        /// <summary>The pitch the match is handed: the delivery with the special the pitcher asked for, paid or not.</summary>
        public PitchCommand AsReleased(PitchCommand pitch) => pitch != null && PitchAsked && !pitch.Star ? pitch with { Star = true } : pitch;

        /// <summary>The swing the match is handed: the same rule as the pitch.</summary>
        public SwingCommand AsReleased(SwingCommand swing) => swing != null && SwingAsked && !swing.Star ? swing with { Star = true } : swing;
    }
}
