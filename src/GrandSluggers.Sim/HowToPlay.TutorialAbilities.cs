namespace GrandSluggers.Sim.Front;

public static partial class HowToPlay
{
    static TutorialCopy? AbilityTutorial(string id)
    {
        if (id == "T-A-burrow") return new("Scoop with Burrow", "Collect a grounder at the outer edge of Burrow's reach.",
            "Soot starts at shortstop. Steer toward the grounder on the left side; scoop it at the edge of your reach so Burrow makes the difference.",
            "Use the left stick to take over shortstop and meet the ground ball at the edge of your reach.");
        if (id == "T-A-sand-scoop") return new("Scoop with Sand Scoop", "Scoop a low grounder at the outer edge of Sand Scoop's reach.",
            "Sable starts at shortstop. Steer toward the grounder on the left side and meet it low, at the edge of your reach, so Sand Scoop makes the difference.",
            "Use the left stick to take over shortstop and meet the rolling ball at the edge of your reach.");
        if (id == "T-A-lily-leap") return new("Leap with Lily Leap", "Jump for a liner over your head that only Lily Leap reaches.",
            "Reed plays shortstop. Stand under the liner's path and jump just before it arrives; an ordinary jump tops out below it.",
            "Use the left stick to stand under the liner, then press North to leap as it arrives.");
        var ability = id switch
        {
            "T-F16" or "T-A-grow" => "Grow",
            "T-A-lick-catch" => "Lick Catch",
            "T-A-withdraw" => "Withdraw",
            _ => ""
        };
        if (ability.Length == 0) return null;
        return new("Reach with " + ability, "Make a catch that needs " + ability + "'s extra reach.",
            "Control center field. Move to the right edge of the yellow landing ring and catch before the ball lands. The ability must reach beyond an ordinary glove.",
            "Start with the stick centered, then move to the ring's right edge. The ball is caught automatically in reach.");
    }
}
