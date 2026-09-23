using System.Linq;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Pre-contact adapter: input and frame time enter the same sim bodies used by the live play.</summary>
    public sealed partial class MatchDirector
    {
        int _previousSetupBag;
        StealRaceInset _stealInset;

        void ReadTutorialSteal()
        {
            if (!HumanBats || _match.LeadBag == 0) return;
            var pad = RunPad;
            if (pad.ThrowBag is >= 1 and <= 3) _match.SelectRunner(pad.ThrowBag);
            var bag = _match.SelectedBag > 0 ? _match.SelectedBag : _match.LeadBag;
            var verb = Baserunning.StickVerb(InPlay.DiamondBag(pad.StickX, pad.StickY), bag);
            if (pad.Steal || verb == RunStick.Steal && _match.SelectedState?.Advancing != true)
                _coach.Tutorial.ArmSteal(bag);
        }

        void UpdateStealInset()
        {
            var show = _match != null && !_match.Paused && !TutorialModal && !_turntable
                && (_phase is Phase.Set or Phase.Flight) && _match.Runners.Any(r => r.Broke && !r.Out);
            if (!show) { _stealInset?.Hide(); return; }
            _stealInset ??= gameObject.AddComponent<StealRaceInset>();
            var area = BroadcastHud.StealInset;
            var aspect = Screen.width * area.W / (Screen.height * area.H);
            _stealInset.Show(PlayCamera.RaceFraming(_content.Shots, PlayCamera.RaceSubjects(_match), aspect, _feel.RaceCamera));
        }

        bool ReadSetupThrow(Controls.Pad pad, bool accepting)
        {
            var wants = accepting && StealPresentation.BaseThrow(pad.ThrowBag, _previousSetupBag,
                pad.SouthDown, pad.SouthHeld, _match.PitchSetup.Committed);
            _previousSetupBag = pad.ThrowBag;
            if (!wants) return false;
            BeginPickoff(pad.ThrowBag);
            return true;
        }

        void CommitPitchSetup()
        {
            if (TutorialOn && _coach.Tutorial.Lesson.Objective == "human-pickoff")
                _coach.Tutorial.BeginPitchCharge();
            else _match.PitchSetup.BeginCharge();
        }

        bool AdvanceSetup(float seconds)
        {
            // The lesson runner owns its pre-contact clock and recording, once per frame.
            if (TutorialOn && _coach.Tutorial.IsStealLesson) return false;
            var play = _match.PitchSetup.Advance(seconds);
            if (play == null) return false;
            if (_match.Over)
            {
                _last = play; Banner(); BeginResult();
                return true;
            }
            _banner = PlayStamp.Label(play);
            return false;
        }

        void HoldPreparingThrow()
        {
            if (!_match.LivePlay.ThrowPreparing) return;
            var map = FieldingResolver.Assign(_match.DefenseRoster, _match.Pitcher, _match.Defense.Gloves);
            if (map.TryGetValue(_throwFromPos, out var who) && _heroes.TryGetValue(who.Id, out var hero) && hero.ThrowHand != null)
                _park.Ball.Hold(hero.ThrowHand);
        }

        void BufferCatcherInput()
        {
            if (HumanOwnsThrow && _match.PitchSetup.Phase == PitchSetupPhase.Flight)
                _match.PitchSetup.CatcherInput(FieldInput() with { StickBag = 0, ArrowBag = 0 });
        }
    }
}
