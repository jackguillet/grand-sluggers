namespace GrandSluggers.Sim;

public sealed partial class TutorialSession
{
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
