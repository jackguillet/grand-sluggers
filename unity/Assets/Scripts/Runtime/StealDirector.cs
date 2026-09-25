using System.Collections.Generic;
using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The pre-contact runner play (#1042): the race inset while a runner is broken, the pitcher's base-throw read in SET
    /// and the pickoff it begins, the pitch's charge commit, the pre-contact clock that settles a steal before the pitch,
    /// the catcher's buffered command in flight, and the ball held in the preparing thrower's hand. It owns the inset and the
    /// base-throw edge; the match and the flow's result beat are reached through <see cref="IRunnerPlayHost"/>.
    /// </summary>
    public sealed class StealDirector
    {
        readonly GameObject _gameObject;
        readonly PlayState _play;
        readonly IRunnerPlayHost _flow;
        int _previousSetupBag;
        StealRaceInset _inset;

        internal StealDirector(GameObject host, PlayState play, IRunnerPlayHost flow)
        {
            _gameObject = host; _play = play; _flow = flow;
        }

        Match Match => _play.Match;
        TutorialSession Lesson => _flow.Lesson;

        /// <summary>A new pitch: the base-throw edge starts clean.</summary>
        public void NewPitch() => _previousSetupBag = 0;

        /// <summary>
        /// The race inset (§11): shown while <paramref name="eligible"/> (SET or the pitch's flight, not paused, no modal)
        /// and a runner is broken and not out.
        /// </summary>
        public void UpdateInset(bool eligible, Match match, ContentCatalog content, FeelTable feel)
        {
            if (!eligible || match == null || !match.Runners.Any(r => r.Broke && !r.Out)) { _inset?.Hide(); return; }
            _inset ??= _gameObject.AddComponent<StealRaceInset>();
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

        /// <summary>A legal base throw in SET (§4.5): the pad's edge begins the pickoff. True when it did.</summary>
        public bool ReadSetupThrow(Controls.Pad pad, bool accepting)
        {
            var bag = SetupThrowBag(pad, accepting, Match);
            if (bag == 0) return false;
            BeginPickoff(bag);
            return true;
        }

        /// <summary>The pitch's charge begins: the runners may no longer be picked off by a throw that was not a pitch.</summary>
        public void CommitCharge()
        {
            if (Lesson != null && Lesson.Lesson.Objective == "human-pickoff")
                Lesson.BeginPitchCharge();
            else Match.PitchSetup.BeginCharge();
        }

        /// <summary>The pre-contact clock (§11): a steal settled before the pitch ends the SET, or the game. True when the game ended.</summary>
        public bool Advance(float seconds)
        {
            // The lesson runner owns its pre-contact clock and recording, once per frame.
            if (Lesson != null && Lesson.IsStealLesson) return false;
            var play = Match.PitchSetup.Advance(seconds);
            if (play == null) return false;
            if (Match.Over)
            {
                _flow.EndWith(play);
                return true;
            }
            _flow.Banner = PlayStamp.Label(play);
            return false;
        }

        /// <summary>The pickoff at <paramref name="bag"/> (§4.5): a live runner play, or the play it ended on.</summary>
        public void BeginPickoff(int bag)
        {
            if (Lesson != null && Lesson.Pickoff(bag))
            {
                if (Match.LivePlay.Active) _flow.StartRunnerPlay();
                else if (Lesson.LastPlay is { } tutorialPlay) _flow.EndWith(tutorialPlay);
                return;
            }
            if (Match.BeginPickoff(bag, _flow.LiveSeatsNow(), out var dead, Match.LivePlay.Source))
            {
                _flow.StartRunnerPlay();
                return;
            }
            if (dead != null) _flow.EndWith(dead);
        }
    }

    /// <summary>What the runner play asks of the match flow: the lesson in play, the seats, the live play, and the result beat.</summary>
    internal interface IRunnerPlayHost
    {
        /// <summary>The tutorial lesson in play, or null.</summary>
        TutorialSession Lesson { get; }
        LiveSeats LiveSeatsNow();
        /// <summary>A pickoff or a thrown-out steal became a live runner play.</summary>
        void StartRunnerPlay();
        /// <summary>The play ended on <paramref name="play"/>: its banner, then the result beat.</summary>
        void EndWith(PlayEvent play);
        string Banner { set; }
    }
}
