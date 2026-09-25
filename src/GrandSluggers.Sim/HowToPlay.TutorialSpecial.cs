namespace GrandSluggers.Sim.Front;

public static partial class HowToPlay
{
    static TutorialCopy? SpecialTutorial(string id)
    {
        if (id == "T-G03") return new("Earn and spend stars", "Earn stars with a strikeout, then spend them on a star pitch.",
            "The batter has two strikes. Throw an ordinary strike to earn meter. Against the next batter, throw the star pitch.",
            "Tap South for the third strike. Next batter: hold LT as you let go of RT.");
        if (id == "T-G03-U") return new("When the stars run out", "Ask for a star pitch you cannot pay for, and watch the ordinary pitch go.",
            "Your pitcher has no stars. Ask for the star pitch anyway: the pitch goes out ordinary, nothing is spent, and your stars flash red.",
            "Hold LT as you let go of RT.");
        if (id == "T-X02") return new("Field a star grounder", "Move your glove to the opponent's star grounder and secure it yourself.",
            "The CPU uses a real star ground swing. Take control and move into the ball's path; an assisted pickup does not count.",
            "Use the left stick to take control and reach the ground ball.");
        if (id is "T-X01" or "T-I-banana" or "T-I-rocket" or "T-I-pow")
        {
            var item = id == "T-I-rocket" ? "Rocket" : id == "T-I-pow" ? "POW" : "Banana";
            return new("Use " + item, "Make fair contact, then land " + item + " on your selected defender.",
                "Your batting pair offers an item after contact. Hit the strike fair, choose " + item + ", aim at a defender and throw while the play is live.",
                "RT hits. D-pad left/right cycles items; left stick aims. North throws.");
        }
        var pitch = id == "T-P09" || id.StartsWith("T-SP-", StringComparison.Ordinal);
        var skill = id switch
        {
            "T-P09" => "Heatball",
            "T-B09" => "Heat Swing",
            "T-SP-heatball" => "Heatball",
            "T-SP-charmball" => "Charmball",
            "T-SP-prismball" => "Prismball",
            "T-SP-phonyball" => "Phonyball",
            "T-SP-caskball" => "Caskball",
            "T-SP-skullball" => "Skullball",
            "T-SP-fogball" => "Fogball",
            "T-SP-fastball" => "Star Fastball",
            "T-SP-changeup" => "Star Change",
            "T-SP-breaker" => "Star Breaker",
            "T-SP-mirageball" => "Mirage Ball",
            "T-SS-heat-swing" => "Heat Swing",
            "T-SS-heart-swing" => "Heart Swing",
            "T-SS-shell-swing" => "Shell Swing",
            "T-SS-phony-swing" => "Phony Swing",
            "T-SS-cask-swing" => "Cask Swing",
            "T-SS-furnace" => "Furnace",
            "T-SS-staff-swing" => "Staff Swing",
            "T-SS-sidewinder" => "Sidewinder",
            "T-SS-ground" => "Star Grounder",
            "T-SS-fly" => "Star Fly",
            "T-SS-line" => "Star Line",
            _ => ""
        };
        if (skill.Length == 0) return null;
        return pitch
            ? new("Pitch: " + skill, "Throw " + skill + " and spend its star cost.",
                "Your pitcher has " + skill + " and enough meter. Hold the star button as you let go of the pitch.",
                "Hold LT as you let go of RT.")
            : new("Swing: " + skill, "Use " + skill + " to make fair contact and spend its star cost.",
                "Your batter has " + skill + " and enough meter. Hold the star button as you let go of the swing, and time contact with the strike.",
                "Hold LT as you let go of RT, as the pitch arrives.");
    }
}
