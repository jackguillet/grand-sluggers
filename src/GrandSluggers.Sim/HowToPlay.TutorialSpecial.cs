namespace GrandSluggers.Sim;

public static partial class HowToPlay
{
    static TutorialCopy? SpecialTutorial(string id)
    {
        if (id == "T-G03") return new("Earn and spend stars", "Earn stars with a strikeout, then spend them on a star pitch.",
            "The batter has two strikes. Throw an ordinary strike to earn meter. Against the next batter, turn on the star pitch and throw it.",
            "Tap South for the third strike. Next batter: North selects the star pitch; South throws.",
            "Tap Space for the third strike. Next batter: Q selects the star pitch; Space throws.");
        if (id == "T-X02") return new("Field a star grounder", "Move your glove to the opponent's star grounder and secure it yourself.",
            "The CPU uses a real star ground swing. Take control and move into the ball's path; an assisted pickup does not count.",
            "Use the left stick to take control and reach the ground ball.",
            "Use WASD to take control and reach the ground ball.");
        if (id is "T-X01" or "T-I-banana" or "T-I-rocket" or "T-I-pow")
        {
            var item = id == "T-I-rocket" ? "Rocket" : id == "T-I-pow" ? "POW" : "Banana";
            return new("Use " + item, "Make fair contact, then land " + item + " on your selected defender.",
                "Your batting pair offers an item after contact. Hit the strike fair, choose " + item + ", aim at a defender and throw while the play is live.",
                "South hits. RB cycles items; hold LT to aim with the stick. LT + RB or LT + South throws.",
                "Space hits. Tab cycles items; aim toward a defender with WASD. E throws.");
        }
        var pitch = id == "T-P09" || id.StartsWith("T-SP-");
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
            "T-SS-heat-swing" => "Heat Swing",
            "T-SS-heart-swing" => "Heart Swing",
            "T-SS-shell-swing" => "Shell Swing",
            "T-SS-phony-swing" => "Phony Swing",
            "T-SS-cask-swing" => "Cask Swing",
            "T-SS-furnace" => "Furnace",
            "T-SS-staff-swing" => "Staff Swing",
            "T-SS-ground" => "Star Grounder",
            "T-SS-fly" => "Star Fly",
            "T-SS-line" => "Star Line",
            _ => ""
        };
        if (skill.Length == 0) return null;
        return pitch
            ? new("Pitch: " + skill, "Throw " + skill + " and spend its star cost.",
                "Your pitcher has " + skill + " and enough meter. Select the star pitch, then deliver it to the waiting batter.",
                "North selects the star pitch. Tap South to throw.",
                "Q selects the star pitch. Tap Space to throw.")
            : new("Swing: " + skill, "Use " + skill + " to make fair contact and spend its star cost.",
                "Your batter has " + skill + " and enough meter. Select the star swing and time contact with the incoming strike.",
                "North selects the star swing. Tap and release South as the pitch arrives.",
                "Q selects the star swing. Tap and release Space as the pitch arrives.");
    }
}
