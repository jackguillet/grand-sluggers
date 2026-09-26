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
            "T-SP-heatball" => "Skyrocket",
            "T-SP-charmball" => "Aurora Ribbon",
            "T-SP-prismball" => "Loop-the-Loop",
            "T-SP-phonyball" => "Phonyball",
            "T-SP-caskball" => "Vine Swing",
            "T-SP-skullball" => "Anvil",
            "T-SP-fogball" => "Undertow",
            "T-SP-fastball" => "Star Fastball",
            "T-SP-changeup" => "Star Change",
            "T-SP-breaker" => "Star Breaker",
            "T-SP-dot" => "Star Dot",
            "T-SP-sinkball" => "Star Sinker",
            "T-SP-lob" => "Star Lob",
            "T-SP-sidearm" => "Star Sidearm",
            "T-SP-mirageball" => "Mirage",
            "T-SP-rockfall" => "Cable Car",
            "T-SP-leapfrog" => "Leapfrog",
            "T-SS-heat-swing" => "Sparkler",
            "T-SS-heart-swing" => "Follow Spot",
            "T-SS-shell-swing" => "Spinning Top",
            "T-SS-phony-swing" => "Double Deal",
            "T-SS-cask-swing" => "Lightning Liner",
            "T-SS-furnace" => "Hot Iron",
            "T-SS-staff-swing" => "Driftwood Reach",
            "T-SS-sidewinder" => "Dust Bowl",
            "T-SS-updraft" => "Summit Gust",
            "T-SS-pond-skip" => "Pond Skip",
            "T-SS-ground" => "Star Grounder",
            "T-SS-fly" => "Star Fly",
            "T-SS-line" => "Star Line",
            "T-SS-pull" => "Star Pull",
            "T-SS-opposite" => "Star Opposite",
            "T-SS-chopper" => "Star Chopper",
            "T-SS-drag-bunt" => "Star Drag Bunt",
            _ => ""
        };
        if (skill.Length == 0) return null;
        // What the special does, where it is its own (§13): the lesson names the bend it teaches.
        var effect = id switch
        {
            "T-SP-heatball" => " It flies fast, then rises up to a foot over the last third, so aim it low.",
            "T-SS-heat-swing" => " Its Perfect ring is half again as wide, but the bat must still meet the ball.",
            "T-SP-fogball" => " It comes in slow; if it is hit fair, a ring on home slows the batter's first steps for 2 seconds.",
            "T-SS-staff-swing" => " Its contact oval reaches higher and lower, not wider: a wide pitch still beats it.",
            "T-SP-prismball" => " It runs one loop mid-flight, then crosses where you aimed, on time.",
            "T-SS-shell-swing" => " The grounder spins in place at its first hop, then rolls on slowly. Run.",
            "T-SP-charmball" => " The ball sways widest at mid-flight, then settles onto your aim before the plate.",
            "T-SS-heart-swing" => " The follow spot holds the nearest fielder still for a moment after contact.",
            "T-SP-phonyball" => " It shows one side early and switches late. Read the switch.",
            "T-SS-phony-swing" => " A card-back decoy flies beside the real ball until the top of its arc.",
            "T-SP-caskball" => " It swings in wide on a vine, then crosses where you aimed, on time.",
            "T-SS-cask-swing" => " The liner jags twice, then lands where a straight one would.",
            "T-SP-skullball" => " It flies fast, clangs late and drops up to a foot and a half, so aim it above where you want it.",
            "T-SS-furnace" => " The ball stays hot for two seconds: a glove that holds it drops it, so fielders must throw at once.",
            "T-SP-rockfall" => " It stops dead mid-flight for a beat, then runs down its line to cross where you aimed, on time.",
            "T-SS-updraft" => " At the top of its flight the fly catches a gust and carries farther along its line.",
            "T-SP-mirageball" => " The ball vanishes for the middle third of its flight; its shadow still shows the way.",
            "T-SS-sidewinder" => " The grounder kicks up a bowl of dust where it lands; fielders in it run at half speed.",
            "T-SP-dot" => " It flies a little faster and true: it crosses exactly where you aimed.",
            "T-SP-sinkball" => " If it is hit, the ball leaves lower, so it is more often on the ground.",
            "T-SP-lob" => " It leaves slow on a high arc and falls through the zone where you aimed, on time.",
            "T-SP-sidearm" => " It leaves your hand wider and crosses the zone on a diagonal, where you aimed.",
            "T-SS-pull" => " The ball goes harder and leans toward your pull line.",
            "T-SS-opposite" => " The ball goes harder and leans toward the opposite field.",
            "T-SS-chopper" => " The ball is driven into the dirt and bounces high. Run.",
            "T-SS-drag-bunt" => " The bat squares at contact and the bunt rolls along the line to stop just fair. Run.",
            _ => ""
        };
        return pitch
            ? new("Pitch: " + skill, "Throw " + skill + " and spend its star cost.",
                "Your pitcher has " + skill + " and enough meter." + effect + " Hold the star button as you let go of the pitch.",
                "Hold LT as you let go of RT.")
            : new("Swing: " + skill, "Use " + skill + " to make fair contact and spend its star cost.",
                "Your batter has " + skill + " and enough meter." + effect + " Hold the star button as you let go of the swing, and time contact with the strike.",
                "Hold LT as you let go of RT, as the pitch arrives.");
    }
}
