using System.Collections.Generic;
using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The pre-contact steal presentation (#1042): the race inset while a runner is broken, the pitcher's base-throw read in
    /// SET, the catcher's buffered command in flight, and the ball held in the preparing thrower's hand. It owns the inset
    /// and the base-throw edge; everything else it reads is handed in, so it never reaches into <see cref="MatchDirector"/>.
    /// </summary>
    public sealed class StealDirector
    {
        readonly GameObject _host;
        int _previousSetupBag;
        StealRaceInset _inset;

        public StealDirector(GameObject host) => _host = host;

        /// <summary>A new pitch: the base-throw edge starts clean.</summary>
        public void NewPitch() => _previousSetupBag = 0;

        /// <summary>
        /// The race inset (§11): shown while <paramref name="eligible"/> (SET or the pitch's flight, not paused, no modal)
        /// and a runner is broken and not out.
        /// </summary>
        public void UpdateInset(bool eligible, Match match, ContentCatalog content, FeelTable feel)
        {
            if (!eligible || match == null || !match.Runners.Any(r => r.Broke && !r.Out)) { _inset?.Hide(); return; }
            _inset ??= _host.AddComponent<StealRaceInset>();
            var area = BroadcastHud.StealInset;
            var aspect = Screen.width * area.W / (Screen.height * area.H);
            _inset.Show(PlayCamera.RaceFraming(content.Shots, PlayCamera.RaceSubjects(match), aspect, feel.RaceCamera));
        }

        /// <summary>The bag a legal base throw in SET goes to this tick (§4.5), or 0: the pad's edge, once per press.</summary>
        public int SetupThrowBag(Controls.Pad pad, bool accepting, Match match)
        {
            var wants = accepting && StealPresentation.BaseThrow(pad.PickoffBag, _previousSetupBag,
                pad.BallDown, pad.BallHeld, match.PitchSetup.Committed);
            _previousSetupBag = pad.PickoffBag;
            return wants ? pad.PickoffBag : 0;
        }

        /// <summary>The catcher's command while the pitch flies (§11): buffered for the throw the steal will ask for.</summary>
        public static void BufferCatcherInput(Match match, bool humanOwnsThrow, LivePadInput field)
        {
            if (humanOwnsThrow && match.PitchSetup.Phase == PitchSetupPhase.Flight)
                match.PitchSetup.CatcherInput(field with { StickBag = 0, ArrowBag = 0 });
        }

        /// <summary>The ball stays in the throwing hand while the live system prepares the throw.</summary>
        public static void HoldPreparingThrow(Match match, string throwFromPos, IReadOnlyDictionary<string, HeroActor> heroes, ParkView park)
        {
            if (!match.LivePlay.ThrowPreparing) return;
            var map = match.DefenseMap;
            if (map.TryGetValue(throwFromPos, out var who) && heroes.TryGetValue(who.Id, out var hero) && hero.ThrowHand != null)
                park.Ball.Hold(hero.ThrowHand);
        }
    }
}
