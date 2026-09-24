namespace GrandSluggers.Sim;

public static partial class HowToPlay
{
    static TutorialCopy? AbilityTutorial(string id)
    {
        if (id == "T-A-burrow") return new("Scoop with Burrow", "Collect a grounder at the outer edge of Burrow's reach.",
            "Soot starts at shortstop. Steer toward the grounder on the left side; scoop it at the edge of your reach so Burrow makes the difference.",
            "Use the left stick to take over shortstop and meet the ground ball at the edge of your reach.");
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
