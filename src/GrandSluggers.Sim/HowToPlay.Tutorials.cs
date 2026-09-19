namespace GrandSluggers.Sim;

public static partial class HowToPlay
{
    public const string TutorialMenuTitle = "TUTORIALS";
    public const string TutorialFree = "Free practice";
    public const string TutorialFreeGoal = "Keep playing in Harbor. Try any verb without a lesson goal.";
    public const string TutorialComplete = "LESSON COMPLETE";
    public const string TutorialRetry = "TRY AGAIN";
    public static string TutorialTitleHint(InputScheme scheme) => scheme == InputScheme.Pad
        ? "Harbor · South opens tutorials" : "Harbor · Space opens tutorials";
    public static string TutorialTitle(string id) => id switch
    {
        "T-P01" => "Throw a strike", "T-P03" => "Throw a changeup", "T-B01" => "Slap hit",
        "T-F01" => "Field a ground ball", "T-F05" => "Dive for an out", "T-D02" => "Turn a double play",
        _ => id
    };
    public static string TutorialGoal(string id) => id switch
    {
        "T-P01" => "Put a pitch in the strike zone.",
        "T-P03" => "Throw a changeup for a strike.",
        "T-B01" => "Tap a slap swing and put the ball in fair territory.",
        "T-F01" => "Move your glove to the ground ball and scoop it up.",
        "T-F05" => "Move your glove and dive to catch the ball before it lands.",
        "T-D02" => "Force the runner at second, then throw to first for two outs.",
        _ => ""
    };
    public static string TutorialSetup(string id) => id switch
    {
        "T-P01" or "T-P03" => "The batter will take your pitch. Aim inside the zone.",
        "T-B01" => "The pitcher sends a repeatable fastball down the middle. Watch it approach the plate.",
        "T-F01" => "A ground ball comes to the left side. Follow the ball with the highlighted glove.",
        "T-F05" => "A low fly heads toward your glove. Get close, then dive just before it lands.",
        "T-D02" => "A runner starts on first. Field the grounder, throw to second, then throw from that glove to first.",
        _ => ""
    };
    public static string TutorialControls(string id, InputScheme scheme)
    {
        var pad = scheme == InputScheme.Pad;
        return id switch
        {
            "T-P01" => pad ? "Stick aims. Tap South to pitch." : "Mouse aims. Tap Space or left click to pitch.",
            "T-P03" => pad ? "Stick aims. Hold West while you tap South." : "Mouse aims. Hold V while you tap Space or left click.",
            "T-B01" => pad ? "Stick aims the oval. Tap South as the ball arrives." : "Mouse aims the oval. Tap Space or left click as the ball arrives.",
            "T-F01" => pad ? "Left stick moves your glove. Run into the ground ball." : "WASD moves your glove. Run into the ground ball.",
            "T-F05" => pad ? "Left stick moves your glove. East dives toward the ball." : "WASD moves your glove. G dives toward the ball.",
            "T-D02" => pad ? "D-pad Up + South throws to second. Then Right + South throws to first."
                : "2 + Space throws to second. Then 1 + Space throws to first.",
            _ => ""
        };
    }
    public static string TutorialFeedbackText(string code) => code switch
    {
        "strike" => "That pitch crossed the strike zone.",
        "use-changeup" => "That was another pitch type. Hold the changeup button through release.",
        "outside-zone" => "That pitch missed the zone. Aim closer to the middle.",
        "use-slap" => "That was a charged swing. Tap and release for a slap hit.",
        "miss" => "No contact. Line up the oval and time your tap as the ball arrives.",
        "foul" => "Foul ball. Adjust your timing to keep it between the foul lines.",
        "fair-contact" => "Your slap swing put the ball in fair territory.",
        "ground-possession" => "You moved to the ground ball and secured it.",
        "assisted-pickup" => "The help collected that ball. Take over with the stick or WASD and move to the ball yourself.",
        "diving-out" => "Your dive caught the ball for an out.",
        "assisted-catch" => "The help made that catch. Take over your glove, then press dive yourself.",
        "no-diving-out" => "No diving out this time. Get close and dive before the ball lands.",
        "turned-two" => "Your two throws forced the runner at second and the batter at first.",
        "double-play-missed" => "Keep the order: second, then first. Throw again as soon as the next glove has the ball.",
        "timeout" => "Time is up. Read the goal, then retry the same setup.",
        "demonstration" => "Now try those actions yourself.",
        _ => "Read the goal and try the setup again."
    };
    public static string TutorialButton(int action, InputScheme scheme)
    {
        var pad = scheme == InputScheme.Pad;
        return action switch
        {
            -2 => pad ? "South · Start" : "Space · Start",
            -3 => pad ? "East · Lessons" : "G · Lessons",
            -4 => pad ? "East · Title" : "G · Title",
            -5 => pad ? "West · Next" : "F · Next",
            -6 => pad ? "South · Retry" : "Space · Retry",
            _ => ""
        };
    }
    // Shares the book's couch margins and type sizes. Geometry belongs to the copy/layout layer.
    public static (float X, float Y, float W, float H) TutorialRegion(float w, float h, int region)
    {
        var p = BookPanel(w, h);
        return region switch
        {
            0 => (p.X + 16, p.Y + 8, p.W - 32, 60),
            1 => (p.X + 16, p.Y + 92, (p.W - 56) * .48f, p.H - 180),
            2 => (p.X + 40 + (p.W - 56) * .48f, p.Y + 92, (p.W - 56) * .52f, p.H - 180),
            3 => (p.X + 16, p.Y + p.H - 68, p.W - 32, 60),
            _ => (p.X + 16, p.Y + 92, p.W - 32, p.H - 180)
        };
    }
    public static (float X, float Y, float W, float H) TutorialRow(float w, float h, int index, int count)
    {
        var r = TutorialRegion(w, h, 1); var rowH = r.H / count;
        return (r.X, r.Y + index * rowH, r.W, rowH - 4);
    }
    public static (float X, float Y, float W, float H) TutorialAction(float w, float h, int index, int count)
    {
        var r = TutorialRegion(w, h, 3); var width = r.W / count;
        return (r.X + index * width, r.Y, width - 8, r.H);
    }
    public static int TutorialHit(float x, float y, float w, float h, bool menu, int rows, bool feedback)
    {
        bool In((float X, float Y, float W, float H) r) => x >= r.X && x < r.X + r.W && y >= r.Y && y < r.Y + r.H;
        if (menu)
            for (var i = 0; i < rows; i++)
                if (In(TutorialRow(w, h, i, rows))) return i;
        var count = !menu && feedback ? 3 : 2;
        for (var i = 0; i < count; i++)
            if (In(TutorialAction(w, h, i, count)))
                return i == 0 ? -2 : i == count - 1 ? menu ? -4 : -3 : -5;
        return -1;
    }

}
