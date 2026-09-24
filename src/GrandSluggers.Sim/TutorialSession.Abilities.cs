namespace GrandSluggers.Sim;

public sealed partial class TutorialSession
{
    void ResetAbilityEvidence() { }

    void EvaluateAbilityReach(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (live.FactsThisPlay.OfType<ReachBonusTake>().LastOrDefault()?.Ability == _setup.Skill
            && (live.HoldsBall || result.CompletedPlay?.Outcome?.OutsMade.Any(o => o.Type == OutType.Catch) == true))
        {
            var who = result.CompletedPlay?.Fielder?.Id ?? live.FirstGloveId;
            var manual = _manualGloves.Contains(who) && !_assistedSinceManual.Contains(who);
            Finish(manual, manual ? "ability-reach-" + _setup.Skill : "assisted-ability-catch",
                manual ? "Your " + _setup.Skill + " fielder reached a ball an ordinary glove could not."
                    : "The ability helped, but the helper took the glove. Move and catch it yourself.");
        }
        else if (result.CompletedPlay is not null)
            Finish(false, "ability-opportunity-ended", "The ball got past before your ability fielder could secure it.");
    }
}
