using System.Linq;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// SET's Arrange defense window (spec §4.9; its own class since #1042): opened from Call time by the pitching human
    /// before the pitch commits, it moves a cursor over the defense, swaps two positions or brings in a pitcher, and
    /// closes on Back. The sim decides whether a swap is legal (<see cref="Match.SwapDefensePositions"/>); a lesson that
    /// teaches the window owns every trade.
    /// </summary>
    internal sealed class DefenseSwapWindow
    {
        readonly PlayState _play;
        readonly SeatPads _pads;
        readonly IDefenseSwapHost _host;
        MenuNav.Gate _x, _y;

        public DefenseSwapWindow(PlayState play, SeatPads pads, IDefenseSwapHost host)
        {
            _play = play; _pads = pads; _host = host;
        }

        /// <summary>The open window's pick and cursor, or null while it is closed.</summary>
        public DefenseSetupPick Pick { get; private set; }
        public bool Open => Pick != null;

        TutorialSession Lesson => _host.Coach != null ? _host.Coach.Tutorial : null;
        bool SwapLesson => Lesson != null && Lesson.IsDefenseSwapLesson;

        /// <summary>Open it: only in SET, for a human pitcher, before the pitch commits, when the match allows it.</summary>
        public bool TryOpen()
        {
            var match = _play.Match;
            if (_play.Phase != MatchDirector.Phase.Set || !_pads.HumanPitches || match.PitchSetup.Committed || !match.CanArrangeDefense) return false;
            Pick = new DefenseSetupPick(match);
            _x.Catch(_pads.FieldPad.MenuAxisX); _y.Catch(_pads.FieldPad.MenuAxisY);
            match.SetPaused(false);
            Controls.CatchPlay();
            return true;
        }

        public void Close() => Pick = null;

        /// <summary>One frame of the mound pad: Back cancels a pick or closes; the stick moves; West brings in a pitcher; South picks or swaps.</summary>
        public void Tick(float dt, Controls.Pad mound)
        {
            if (Pick == null) return;
            if (mound.EastDown)
            {
                if (Pick.PickedPosition != null) Pick.CancelPick();
                else Pick = null;
                return;
            }
            var dx = _x.Tick(mound.MenuAxisX, mound.MenuTapX, dt);
            var dy = _y.Tick(mound.MenuAxisY, mound.MenuTapY, dt);
            if (dx != 0 || dy != 0) Pick.Move(dx, dy);
            // The Arrange defense lesson owns every trade, the mound included, so a pitcher change is its typed failure.
            System.Func<Character, bool> pitcherSwap = SwapLesson ? null : PitcherSwap;
            if (mound.WestDown)
                Pick.QuickPitcher(_play.Match, pitcherSwap, PositionSwap);
            else if (mound.SouthDown)
                Pick.PickOrSwap(_play.Match, pitcherSwap, PositionSwap);
        }

        bool PositionSwap(string from, string to)
        {
            bool changed;
            if (SwapLesson) Lesson.SwapPositions(from, to, out changed);
            else changed = _play.Match.SwapDefensePositions(from, to);
            if (changed && (from == "P" || to == "P")) _host.PitcherChanged();
            return changed;
        }

        bool PitcherSwap(Character who)
        {
            bool changed;
            if (Lesson != null && Lesson.SwapPitcher(who.Id)) changed = _play.Match.Pitcher.Id == who.Id;
            else
            {
                var candidate = Pick.Candidates.First(c => c.Who.Id == who.Id);
                changed = _play.Match.SwapDefensePositions("P", candidate.Pos);
            }
            if (changed) _host.PitcherChanged();
            return changed;
        }
    }

    /// <summary>What the swap window asks of the flow: the practice coach (a lesson may own the trade), and a new pitcher's reset.</summary>
    internal interface IDefenseSwapHost
    {
        TrainingDirector Coach { get; }
        /// <summary>The pitcher changed: the pitch selection starts again.</summary>
        void PitcherChanged();
    }
}
