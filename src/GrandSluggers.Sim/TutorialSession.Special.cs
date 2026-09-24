namespace GrandSluggers.Sim;

public sealed partial class TutorialSession
{
    void BeginSpecialGround()
    {
        var swing = new SwingCommand(true, 0, 0, true);
        var beforeStars = Match.OffenseStars;
        var cost = Match.SwingStarCost;
        if (!Match.CanStarSwing) throw new InvalidDataException("Special ground opponent lacks stars.");
        var liveBall = Match.BeginAtBat(CpuPitch, swing, out var hit, out var play);
        var preview = liveBall ? Match.PreviewHit(hit, swing) : null;
        if (!liveBall || hit.Foul || hit.StarSwingUsed != _setup.Skill || preview?.Grounder != true
            || Math.Abs(beforeStars - Match.OffenseStars - cost) > 0.0001)
            throw new InvalidDataException("Special ground fixture no longer yields a fair funded star grounder.");
        LastHit = hit;
        Match.LivePlay.Recording = true;
        var result = Match.LivePlay.Apply(LivePlayCommand.BeginLive(CpuPitch, swing, hit, preview, null,
            new LiveSeats(false, true, true, false), 0, LivePlayCommandSource.Cpu));
        if (!result.Snapshot.Active) throw new InvalidDataException("Special ground fixture did not start live play.");
    }

    void EvaluateSpecialGround(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (live.HoldsBall && LastHit?.StarSwingUsed == _setup.Skill && live.Preview?.Grounder == true)
        {
            var manual = _manualGloves.Contains(live.FirstGloveId)
                && !_assistedSinceManual.Contains(live.FirstGloveId);
            Finish(manual, manual ? "special-ground-fielded" : "assisted-special-field",
                manual ? "You moved your glove to the star grounder and secured it."
                    : "The helper took the special grounder. Move the glove to the ball yourself.");
        }
        else if (result.CompletedPlay is not null)
            Finish(false, "special-ground-escaped", "The star grounder ended before your glove secured it.");
    }

    bool _earnedStarThisAttempt;
    string _humanItemId = "";
    string _humanItemTarget = "";
    void ResetSpecialEvidence()
    {
        _earnedStarThisAttempt = false;
        _humanItemId = "";
        _humanItemTarget = "";
    }

    public bool Item(string itemId, string targetId, LivePlayCommandSource source = LivePlayCommandSource.Human)
    {
        if (!Accepts(source) || !IsItemLesson || LastHit?.ChemistryItemOffered != true
            || !Match.LivePlay.Active || _humanItemId.Length > 0 || !ErrorItems.Known(itemId)) return false;
        itemId = itemId.Trim().ToLowerInvariant();
        var target = Match.DefenseRoster.FirstOrDefault(c => c.Id == targetId);
        if (target is null) return false;
        _inputs.Add(new(Elapsed, source, ItemId: itemId, ItemTargetId: targetId));
        _humanItemId = itemId;
        _humanItemTarget = targetId;
        Match.LivePlay.Apply(LivePlayCommand.ApplyItem(itemId, target, source));
        if (itemId != _setup.Skill)
            Finish(false, "wrong-item", "Choose the named item for this lesson.");
        return true;
    }

    void ObserveSpecialItem(LivePlaySystem live, LivePlayCommandResult result)
    {
        if (_humanItemId.Length == 0 || _humanItemId != _setup.Skill || !live.ItemLanded) return;
        var field = live.Field;
        if (field is not { ItemHit: true } || field.Item != _setup.Skill
            || field.ItemTarget?.Id != _humanItemTarget || !live.ItemEffectActive(_setup.Skill, _humanItemTarget)) return;
        Finish(true, "item-effect-" + _setup.Skill,
            "Your " + _setup.Skill + " landed and its field effect became active.");
    }

    TutorialFeedback? EvaluateStarResource(PitchCommand command, PlayEvent? play,
        double beforeStars, int cost, bool hadMeter)
    {
        if (!_earnedStarThisAttempt)
        {
            var earned = !command.Star && play?.Kind == PlayKind.Strikeout
                && Match.DefenseStars > beforeStars;
            if (!earned) return new(false, "no-star-earned", "Get the called third strike to earn stars first.");
            _earnedStarThisAttempt = true;
            return null;
        }
        var spent = beforeStars - Match.DefenseStars;
        var used = command.Star && hadMeter && play?.Pitch.Star == true
            && Match.Pitcher.StarPitch == _setup.Skill && Math.Abs(spent - cost) < 0.0001;
        return used
            ? new(true, "earned-and-spent-stars", "Your strikeout earned meter, then your star pitch spent it.")
            : new(false, "no-star-spent", "Spend the earned meter on your star pitch.");
    }

    /// <summary>
    /// The unavailable special (PH-16-R12): the player held the modifier at the release on a pool short of the price.
    /// Success is the play's own typed record — one Star Pitch request, not afforded, at the named ability's price — with
    /// the ordinary pitch thrown and nothing spent. A release without the modifier asked for nothing and teaches nothing.
    /// </summary>
    TutorialFeedback EvaluateStarUnavailable(PitchCommand command, PlayEvent? play, double beforeStars)
    {
        var requests = play?.Outcome?.Stars ?? [];
        var request = requests.Count == 1 ? requests[0] : null;
        var shown = command.Star && play is not null && !play.Pitch.Star
            && request is { Action: StarAction.Pitch, Afforded: false } && request.AbilityId == _setup.Skill
            && request.Cost == Match.PitchStarCost && Math.Abs(request.StarsBefore - beforeStars) < 0.0001
            && Math.Abs(Match.DefenseStars - beforeStars) < 0.0001;
        return shown
            ? new(true, "star-unavailable", "No stars: your pitch went out ordinary and spent nothing.")
            : new(false, "star-not-asked", "Hold the star modifier as you let go of the pitch.");
    }

    TutorialFeedback EvaluateStarPitch(PitchCommand command, PlayEvent? play,
        double beforeStars, int cost, bool hadMeter)
    {
        var spent = beforeStars - Match.DefenseStars;
        var accepted = command.Star && hadMeter && Match.Pitcher.StarPitch == _setup.Skill
            && play?.Pitch.Star == true && Math.Abs(spent - cost) < 0.0001;
        return accepted
            ? new(true, "star-pitch-" + _setup.Skill, "Your " + _setup.Skill + " crossed the plate and spent its star cost.")
            : new(false, "star-pitch-missed", "Use the named star pitch with enough stars to pay its cost.");
    }

    TutorialFeedback EvaluateStarSwing(SwingCommand command, AtBatResult hit,
        double beforeStars, int cost, bool hadMeter)
    {
        var spent = beforeStars - Match.OffenseStars;
        var accepted = command.Swing && command.Star && hadMeter && Match.Batter.StarSwing == _setup.Skill
            && hit.StarSwingUsed == _setup.Skill && hit.InPlay && !hit.Foul
            && Math.Abs(spent - cost) < 0.0001;
        return accepted
            ? new(true, "star-swing-" + _setup.Skill, "Your " + _setup.Skill + " made fair contact and spent its star cost.")
            : new(false, "star-swing-missed", "Use the named star swing and put it in fair territory.");
    }
}
