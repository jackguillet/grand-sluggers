namespace GrandSluggers.Sim;

public static partial class HowToPlay
{
    public const string TutorialMenuTitle = "TUTORIALS";
    public const string TutorialFree = "Free practice";
    public const string TutorialFreeGoal = "Keep playing in Harbor. Try any verb without a lesson goal.";
    public const string TutorialComplete = "LESSON COMPLETE";
    public const string TutorialRetry = "TRY AGAIN";
    public const string TutorialRule = "Do it successfully three times to pass. Failures keep your earlier successes.";
    public static string TutorialCount(int successes) => $"{successes}/{TutorialProgress.RequiredSuccesses}";
    public static string TutorialAttemptTitle(string id, int successes) => TutorialTitle(id) + " · " + TutorialCount(successes);
    public static string TutorialResultTitle(bool success, int successes) =>
        (success ? successes >= TutorialProgress.RequiredSuccesses ? TutorialComplete : "GOOD!" : TutorialRetry)
        + " · " + TutorialCount(successes);
    public static string TutorialTitleHint(InputScheme scheme) => scheme == InputScheme.Pad
        ? "Harbor · South opens tutorials" : "Harbor · Space opens tutorials";
    public static string TutorialTitle(string id) => id switch
    {
        "T-P01" => "Throw a strike", "T-P03" => "Throw a changeup", "T-B01" => "Slap hit",
        "T-P04" => "MAX pitch", "T-P05" => "Bend a pitch", "T-P06" => "Move on the rubber",
        "T-B02" => "Find the sweet spot", "T-B04" => "MAX swing", "T-B07" => "Bunt with two strikes", "T-B08" => "Take a ball",
        "T-F01" => "Field a ground ball", "T-F05" => "Dive for an out", "T-D02" => "Turn a double play",
        _ => id
    };
    public static string TutorialGoal(string id) => id switch
    {
        "T-P01" => "Put a pitch in the strike zone.",
        "T-P03" => "Throw a changeup for a strike.",
        "T-P04" => "Release a fully charged pitch for a strike.",
        "T-P05" => "Bend a normal pitch after release and finish in the strike zone.",
        "T-P06" => "Move a little off the middle of the rubber, then throw a strike.",
        "T-B02" => "Put the ball on the perfect heart of a slap swing and hit it fair.",
        "T-B04" => "Release a fully charged swing and hit the ball fair.",
        "T-B07" => "With two strikes, square up and bunt the ball fair.",
        "T-B08" => "Recognize the high ball and let it pass without swinging.",
        "T-B01" => "Tap a slap swing and put the ball in fair territory.",
        "T-F01" => "Move your glove to the ground ball and scoop it up.",
        "T-F05" => "Move your glove and dive to catch the ball before it lands.",
        "T-D02" => "Force the runner at second, then throw to first for two outs.",
        _ => ""
    };
    public static string TutorialSetup(string id) => id switch
    {
        "T-P01" or "T-P03" or "T-P04" => "The batter will take your pitch. Aim inside the zone.",
        "T-P05" => "The batter takes. Tap a normal pitch, then hold a direction after it leaves your hand. Charge and changeup barely bend.",
        "T-P06" => "The batter takes. Your position moves the pitch's crossing. A small step can stay in the zone; a big step can miss it.",
        "T-B07" => "You start with two strikes. A foul bunt is strike three. Keep the bat lined up and bunt between the foul lines.",
        "T-B08" => "The CPU throws above the strike zone. Watch it pass: a called ball earns this attempt. Swinging or bunting does not.",
        "T-B01" or "T-B02" or "T-B04" => "The pitcher sends a repeatable fastball down the middle. Watch it approach the plate.",
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
            "T-P04" => pad ? "Hold South until MAX, then release. Keep the pitch in the zone." : "Hold Space or left click until MAX, then release.",
            "T-P05" => pad ? "Tap South. After release, hold the stick left or right to bend the ball." : "Tap Space. After release, hold A or D to bend the ball.",
            "T-P06" => pad ? "Stick left/right moves the pitcher. Down resets. Tap South to pitch." : "A/D or mouse moves the pitcher. S resets. Tap Space to pitch.",
            "T-B02" => pad ? "Stick moves the batter and oval. Line up the center, then tap South." : "A/D or mouse moves the batter and oval. Line up the center, then tap Space.",
            "T-B04" => pad ? "Hold South before the pitch arrives. Release at MAX as it nears the plate." : "Hold Space or left click before the pitch arrives. Release at MAX as it nears the plate.",
            "T-B07" => pad ? "Line up with the stick. Hold West through the pitch to bunt." : "Line up with A/D or mouse. Hold V or Ctrl through the pitch to bunt.",
            "T-B08" => pad ? "Watch the pitch. Leave South and West alone as it passes." : "Watch the pitch. Leave Space, left click and V/Ctrl alone as it passes.",
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
        "use-max-pitch" => "Build the pitch to MAX and release. Use an ordinary pitch; leave changeup and star off.",
        "use-break" => "Tap a normal pitch, then hold a direction after release. Keep steering long enough for the ball to bend.",
        "move-rubber" => "That pitch stayed too close to the middle. Take a small step on the rubber before throwing.",
        "use-max-swing" => "That swing was not a full ordinary charge. Start holding sooner and release at MAX.",
        "find-sweet-spot" => "You made contact outside the perfect heart. Move the batter so the oval's center meets the pitch.",
        "use-bunt" => "Square to bunt and keep the bat out through the pitch.",
        "foul-bunt" => "Foul bunt. With two strikes that is strike three. Line up the bat to keep the next bunt fair.",
        "fair-bunt" => "You bunted fair with two strikes.",
        "chased-ball" => "That pitch was high. Let it pass without swinging or squaring to bunt.",
        "took-ball" => "You recognized the ball and let it go by.",
        "strike" => "That pitch crossed the strike zone.",
        "use-changeup" => "That was another pitch type. Hold the changeup button through release.",
        "outside-zone" => "That pitch missed the zone. Aim closer to the middle.",
        "use-slap" => "That was a charged swing. Tap and release for a slap hit.",
        "miss" => "No contact. Line up the oval and time your tap as the ball arrives.",
        "foul" => "Foul ball. Adjust your timing to keep it between the foul lines.",
        "fair-contact" => "Your swing put the ball in fair territory.",
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
            -7 => pad ? "South · Continue" : "Space · Continue",
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
        var r = TutorialBrowseRegion(w, h, 1); var rowH = r.H / count;
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

    public const int TutorialPageSize = 6;
    public static string[] TutorialCategories(IEnumerable<TutorialLesson> lessons) =>
        lessons.Select(l => l.Category).Distinct().Concat(new[] { "practice" }).ToArray();
    public static string TutorialCategoryTitle(string category) => category switch
    {
        "pitching" => "Pitching", "batting" => "Batting", "fielding" => "Fielding", "outs" => "Outs",
        "running" => "Running", "game" => "Team", "special" => "Special", "practice" => "Free play", _ => category
    };
    public static string TutorialBrowseHint(InputScheme scheme) => scheme == InputScheme.Pad
        ? "←/→ categories · ↑/↓ lessons" : "A/D categories · W/S lessons";
    public static int TutorialPages(int count) => Math.Max(1, (count + TutorialPageSize - 1) / TutorialPageSize);
    public static int TutorialPageStart(int pick) => Math.Max(0, pick) / TutorialPageSize * TutorialPageSize;
    public static (float X, float Y, float W, float H) TutorialBrowseRegion(float w, float h, int region)
    {
        var r = TutorialRegion(w, h, region);
        return region is 1 or 2 ? (r.X, r.Y + 54, r.W, r.H - 54) : r;
    }
    public static (float X, float Y, float W, float H) TutorialTab(float w, float h, int index, int count)
    {
        var r = TutorialRegion(w, h, 0); var width = r.W / count;
        return (r.X + index * width, r.Y + 68, width - 6, 44);
    }
    public static (float X, float Y, float W, float H) TutorialPageButton(float w, float h, int direction)
    {
        var r = TutorialRegion(w, h, 0);
        return (r.X + r.W - (direction < 0 ? 184 : 56), r.Y, 56, 54);
    }
    public static int TutorialTabHit(float x, float y, float w, float h, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var r = TutorialTab(w, h, i, count);
            if (x >= r.X && x < r.X + r.W && y >= r.Y && y < r.Y + r.H) return i;
        }
        return -1;
    }
    public static int TutorialPageHit(float x, float y, float w, float h)
    {
        foreach (var direction in new[] { -1, 1 })
        {
            var r = TutorialPageButton(w, h, direction);
            if (x >= r.X && x < r.X + r.W && y >= r.Y && y < r.Y + r.H) return direction;
        }
        return 0;
    }

}
