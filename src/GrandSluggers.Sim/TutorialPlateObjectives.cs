namespace GrandSluggers.Sim;

/// <summary>Plate lesson verdicts inspect ordinary commands and the real crossing/contact result.</summary>
public static class TutorialPlateObjectives
{
    public static readonly string[] PitchIds = ["called-strike", "changeup-strike", "max-pitch-strike", "break-strike", "rubber-strike"];
    public static readonly string[] SwingIds = ["slap-fair", "perfect-slap-fair", "max-swing-fair", "bunt-fair", "take-ball"];
    static TutorialFeedback Fail(string code, string detail) => new(false, code, detail);

    public static TutorialFeedback Pitch(string objective, TutorialSetup setup, PitchCommand command, PlayEvent? play)
    {
        if (objective == "changeup-strike" && !command.IsChangeup)
            return Fail("use-changeup", "Use the changeup command, then put it in the strike zone.");
        if (objective == "max-pitch-strike" && (command.Charge01 < 1 || command.Star || command.IsChangeup))
            return Fail("use-max-pitch", "Release an ordinary pitch at full charge, then put it in the zone.");
        if (objective == "break-strike" && (Math.Abs(command.BreakX) < setup.MinMovement01 || command.Star || command.IsChangeup || ChargeFeel.IsCharge(command.Charge01)))
            return Fail("use-break", "Use a normal pitch and hold a direction after release to bend it.");
        if (objective == "rubber-strike" && (Math.Abs(command.RubberX) < setup.MinMovement01 || command.Star))
            return Fail("move-rubber", "Move a little off the middle of the rubber before throwing a strike.");
        return play?.Kind == PlayKind.TakeStrike
            ? new(true, "strike", "The requested pitch crossed the strike zone.")
            : Fail("outside-zone", "The pitch missed the strike zone. Adjust its location and retry.");
    }

    public static TutorialFeedback Swing(string objective, SwingCommand command, AtBatResult hit, PlayEvent? play)
    {
        if (objective == "take-ball")
            return !command.Swing && !command.Bunt && play?.Kind == PlayKind.TakeBall
                ? new(true, "took-ball", "You let an out-of-zone pitch go by for a ball.")
                : Fail("chased-ball", "Let the high pitch pass without swinging or squaring to bunt.");
        if (objective is "slap-fair" or "perfect-slap-fair")
        {
            if (command.Bunt || command.Star || ChargeFeel.IsCharge(command.Charge01))
                return Fail("use-slap", "Use an ordinary uncharged swing for this lesson.");
        }
        if (objective == "max-swing-fair" && (command.Charge01 < 1 || command.Bunt || command.Star))
            return Fail("use-max-swing", "Build full charge, then release in time to make fair contact.");
        if (objective == "bunt-fair" && (!command.Bunt || command.Star))
            return Fail("use-bunt", "Square to bunt and hold the bat out through the pitch.");
        if (!command.Swing || hit.Quality == ContactQuality.Miss)
            return Fail("miss", "Meet the pitch with the cursor and the right timing.");
        if (hit.Foul || !hit.InPlay)
            return Fail(objective == "bunt-fair" ? "foul-bunt" : "foul", "The ball was not put in fair territory.");
        if (objective == "perfect-slap-fair" && hit.Quality != ContactQuality.Perfect)
            return Fail("find-sweet-spot", "Contact missed the perfect heart of the bat. Adjust the batter's position.");
        return new(true, objective == "bunt-fair" ? "fair-bunt" : "fair-contact", "The requested contact put the ball in fair territory.");
    }
}
