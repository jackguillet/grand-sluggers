namespace GrandSluggers.Sim;

/// <summary>Plate lesson verdicts inspect ordinary commands and the real crossing/contact result.</summary>
public static class TutorialPlateObjectives
{
    public static readonly string[] PitchIds = ["called-strike", "changeup-strike", "third-slot-strike", "max-pitch-strike", "break-strike", "rubber-strike", "called-ball"];
    public static readonly string[] SwingIds = ["slap-fair", "perfect-slap-fair", "max-swing-fair", "bunt-fair", "take-ball", "pull-fair", "push-fair", "box-perfect-fair"];
    static TutorialFeedback Fail(string code, string detail) => new(false, code, detail);

    public static TutorialFeedback Pitch(string objective, TutorialSetup setup, PitchCommand command, PlayEvent? play)
    {
        if (objective == "called-ball")
            return play?.Kind == PlayKind.TakeBall
                ? new(true, "pitched-ball", "Your pitch passed outside the zone without hitting the batter.")
                : Fail("pitch-outside", "Move off the middle and pitch outside the zone without hitting the batter.");
        if (objective == "changeup-strike" && command.Type != PitchFamily.Changeup)
            return Fail("use-changeup", "Press the pitch cycle once to select the changeup, then put it in the strike zone.");
        // By slot, not family (T-P10): the pitcher's third pitch is two cycle presses from the
        // fastball whatever it is, and the play's own pitcher is the repertoire that decides it.
        if (objective == "third-slot-strike" && (command.Star || play is null || command.Type != play.Pitcher.Repertoire[2]))
            return Fail("use-third-pitch", "Press the pitch cycle twice to select your third pitch, then put it in the strike zone.");
        if (objective == "max-pitch-strike" && (command.Charge01 < 1 || command.Star || command.Type == PitchFamily.Changeup))
            return Fail("use-max-pitch", "Release an ordinary pitch at full charge, then put it in the zone.");
        if (objective == "break-strike" && (Math.Abs(command.BreakX) < setup.MinMovement01 || command.Star || command.Type == PitchFamily.Changeup || ChargeFeel.IsCharge(command.Charge01)))
            return Fail("use-break", "Use a normal pitch and hold a direction after release to bend it.");
        if (objective == "rubber-strike" && (Math.Abs(command.RubberX) < setup.MinMovement01 || command.Star))
            return Fail("move-rubber", "Move a little off the middle of the rubber before throwing a strike.");
        return play?.Kind == PlayKind.TakeStrike
            ? new(true, "strike", "The requested pitch crossed the strike zone.")
            : Fail("outside-zone", "The pitch missed the strike zone. Adjust its location and retry.");
    }

    public static TutorialFeedback Swing(string objective, TutorialSetup setup, Hand bats, SwingCommand command, AtBatResult hit, PlayEvent? play)
    {
        if (objective == "take-ball")
            return !command.Swing && !command.Bunt && play?.Kind == PlayKind.TakeBall
                ? new(true, "took-ball", "You let an out-of-zone pitch go by for a ball.")
                : Fail("chased-ball", "Let the high pitch pass without swinging or squaring to bunt.");
        if (objective is "slap-fair" or "perfect-slap-fair" or "pull-fair" or "push-fair" or "box-perfect-fair")
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
        if (objective == "box-perfect-fair" && Math.Abs(command.BoxOffsetX) < setup.MinMovement01)
            return Fail("move-box", "Move the batter toward the pitch before making contact.");
        if (objective is "pull-fair" or "push-fair")
        {
            var direction = objective == "pull-fair" ? -1 : 1;
            if (direction * command.TimingErrorFrames < setup.MinTimingFrames || direction * hit.SprayDeg * SweetSpot.TipSign(bats) <= 0)
                return Fail(objective == "pull-fair" ? "swing-earlier" : "swing-later", "Use the requested timing and put the fair ball on that side of the field.");
        }
        if (objective is "perfect-slap-fair" or "box-perfect-fair" && hit.Quality != ContactQuality.Perfect)
            return Fail("find-sweet-spot", "Contact missed the perfect heart of the bat. Adjust the batter's position.");
        return new(true, objective == "bunt-fair" ? "fair-bunt" : "fair-contact", "The requested contact put the ball in fair territory.");
    }
}
