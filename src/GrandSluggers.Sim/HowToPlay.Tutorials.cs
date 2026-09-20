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
        "T-P02" => "Throw a ball", "T-B03" => "Pull an early hit", "T-B03-L" => "Push a late hit",
        "T-B05" => "Move in the box", "T-B06" => "Hit a grounder", "T-B06-F" => "Lift a fly ball",
        "T-P01" => "Throw a strike", "T-P03" => "Throw a changeup", "T-B01" => "Slap hit",
        "T-P04" => "MAX pitch", "T-P05" => "Bend a pitch", "T-P06" => "Move on the rubber",
        "T-B02" => "Find the sweet spot", "T-B04" => "MAX swing", "T-B07" => "Bunt with two strikes", "T-B08" => "Take a ball",
        "T-F02" => "Take the glove", "T-F03" => "Throw to first", "T-F03-2" => "Throw to second",
        "T-F03-3" => "Throw to third", "T-F03-H" => "Throw home", "T-F04" => "Catch an airborne ball", "T-F06" => "Jump for a catch",
        "T-F01" => "Field a ground ball", "T-F05" => "Dive for an out", "T-D02" => "Turn a double play",
        _ => RemainingTutorial(id)?.Title ?? (TutorialGuidedTitle(id) is { Length: > 0 } guided ? guided : id)
    };
    public static string TutorialGoal(string id) => id switch
    {
        "T-P02" => "Place a pitch outside the strike zone without hitting the batter.",
        "T-B03" => "Swing a little early and pull a fair slap hit toward the batter's side.",
        "T-B03-L" => "Swing a little late and push a fair slap hit toward the opposite side.",
        "T-B05" => "Move the batter to meet the offset pitch with Perfect fair slap contact.",
        "T-B06" => "Hold up at contact and slap a fair ground ball.",
        "T-B06-F" => "Hold down at contact and slap a fair fly ball.",
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
        "T-F02" => "Take manual control, move the highlighted glove and collect the ground ball.",
        "T-F03" => "Collect the ball, then throw it to the glove at first base.",
        "T-F03-2" => "Collect the ball, then throw it to the glove at second base.",
        "T-F03-3" => "Collect the ball, then throw it to the glove at third base.",
        "T-F03-H" => "Collect the ball, then throw it to the glove at home plate.",
        "T-F04" => "Get your glove in position and catch the ball before it lands.",
        "T-F06" => "Get close and jump to catch the ball in the air.",
        "T-F01" => "Move your glove to the ground ball and scoop it up.",
        "T-F05" => "Move your glove and dive to catch the ball before it lands.",
        "T-D02" => "Force the runner at second, then throw to first for two outs.",
        _ => RemainingTutorial(id)?.Goal ?? TutorialGuidedGoal(id)
    };
    public static string TutorialSetup(string id, string profile = "shipped") => id switch
    {
        "T-P02" => "The batter takes. Moving farther on the rubber carries the crossing outside the zone. A strike or hit batter does not count.",
        "T-B03" or "T-B03-L" => "The CPU repeats a middle fastball. Timing steers the hit: early pulls, late pushes. The sides reverse for a left-handed batter. Keep the stick centered.",
        "T-B05" => "The CPU repeats a strike off the middle. Move the oval toward it before swinging. Your box position resets for every attempt.",
        "T-B06" or "T-B06-F" => "The CPU repeats a middle fastball. Up tops the ball; down lifts it. Hold the direction during the pitch. A charged swing does not count.",
        "T-P01" or "T-P03" or "T-P04" => "The batter will take your pitch. Aim inside the zone.",
        "T-P05" => "The batter takes. Tap a normal pitch, then hold a direction after it leaves your hand. Charge and changeup barely bend.",
        "T-P06" => "The batter takes. Your position moves the pitch's crossing. A small step can stay in the zone; a big step can miss it.",
        "T-B07" => "You start with two strikes. A foul bunt is strike three. Keep the bat lined up and bunt between the foul lines.",
        "T-B08" => "The CPU throws above the strike zone. Watch it pass: a called ball earns this attempt. Swinging or bunting does not.",
        "T-B01" or "T-B02" or "T-B04" => "The pitcher sends a repeatable fastball down the middle. Watch it approach the plate.",
        "T-F02" => "The help starts your route toward the ball. Move with the stick or keys to take over that route. Letting the help do the work does not pass.",
        "T-F03" or "T-F03-2" or "T-F03-3" or "T-F03-H" => "A ground ball starts the play. Collect it, select the requested bag and throw. The receiving glove must secure your throw at that bag.",
        "T-F04" => "Move into catching position. Keep steering while you press catch; releasing the stick lets the help catch. Scooping after a bounce does not count.",
        "T-F06" => "The ball is still in the air. Move into reach and jump in the catch window. A standing catch or dive does not count as a jump.",
        "T-F01" => "A ground ball comes to the left side. Follow the ball with the highlighted glove.",
        "T-F05" => "A low fly heads toward your glove. Get close, then dive just before it lands.",
        "T-D02" => "A runner starts on first. Field the grounder, throw to second, then throw from that glove to first.",
        _ => RemainingTutorial(id, profile)?.Setup ?? TutorialGuidedSetup(id)
    };
    public static string TutorialControls(string id, InputScheme scheme, string profile = "shipped")
    {
        var pad = scheme == InputScheme.Pad;
        return id switch
        {
            "T-P02" => pad ? "Stick left/right moves the pitcher. Tap South to pitch. Down recenters." : "A/D moves the pitcher. Tap Space to pitch. S recenters.",
            "T-B03" => pad ? "Keep the stick centered. Tap South a little early; too early misses or goes foul." : "Keep the mouse still. Tap Space a little early; too early misses or goes foul.",
            "T-B03-L" => pad ? "Keep the stick centered. Tap South a little late; too late misses or goes foul." : "Keep the mouse still. Tap Space a little late; too late misses or goes foul.",
            "T-B05" => pad ? "Stick left/right moves the batter and oval. Center the oval on the pitch, then tap South." : "A/D or mouse moves the batter and oval. Center it on the pitch, then tap Space.",
            "T-B06" => pad ? "As the pitch approaches, hold stick up and tap South. Keep up held through release." : "As the pitch approaches, hold W and tap Space. Keep W held through release.",
            "T-B06-F" => pad ? "After the pitcher starts, hold stick down and tap South. Keep down held through release." : "After the pitcher starts, hold S and tap Space. Keep S held through release.",
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
            "T-F02" => pad ? "Move the left stick to take the glove. Keep moving it yourself." : "Use WASD to take the glove. Keep moving it yourself.",
            "T-F03" => pad ? "Collect the ball. D-pad Right selects first; South throws." : "Collect the ball. 1 selects first; Space throws.",
            "T-F03-2" => pad ? "Collect the ball. D-pad Up selects second; South throws." : "Collect the ball. 2 selects second; Space throws.",
            "T-F03-3" => pad ? "Collect the ball. D-pad Left selects third; South throws." : "Collect the ball. 3 selects third; Space throws.",
            "T-F03-H" => pad ? "Collect the ball. D-pad Down selects home; South throws." : "Collect the ball. 4 selects home; Space throws.",
            "T-F04" => pad ? "Keep the left stick engaged as you tap South in the catch window." : "Keep WASD held as you tap Space in the catch window.",
            "T-F06" => pad ? "Left stick moves your glove. Tap West in the catch window to jump." : "WASD moves your glove. Tap F in the catch window to jump.",
            "T-F01" => pad ? "Left stick moves your glove. Run into the ground ball." : "WASD moves your glove. Run into the ground ball.",
            "T-F05" => pad ? "Left stick moves your glove. East dives toward the ball." : "WASD moves your glove. G dives toward the ball.",
            "T-D02" => pad ? "D-pad Up + South throws to second. Then Right + South throws to first."
                : "2 + Space throws to second. Then 1 + Space throws to first.",
            _ => (pad ? RemainingTutorial(id, profile)?.Pad : RemainingTutorial(id, profile)?.Keys) ?? TutorialGuidedControls(id, scheme)
        };
    }
    public static string TutorialFeedbackText(string code) => code switch
    {
        "guided-complete" => "You completed the steps on the game screens.",
        "fresh-pitcher" => "The tired pitcher is out and your fresh arm is on the mound.",
        "swap-missed" => "Choose a fresh eligible fielder and confirm the pitcher change.",
        "pickoff-checked" => "Your pickoff throw reached the receiver at first. A runner on the bag is safe.",
        "pickoff-safe" or "pickoff-no-runner" => "Select first for this runner and make the pickoff throw before the opportunity ends.",
        "ball-dash-carried" => "Ball Dash sped up your fielder while you carried the secured ball.",
        "ball-dash-not-carried" => "Collect the ball with the Ball Dash fielder, then steer at full speed while holding it.",
        "laser-home" => "Your throw used the Laser speed boost toward home.",
        "laser-not-used" => "Collect the ball with the Laser fielder and command a throw home while the runner is on third.",
        "relay-handoff" => "Your cutoff feed and the onward throw completed the relay home.",
        "snap-relay" => "The clean handoff used the receiver's Snap Throw on the onward leg.",
        "relay-not-completed" => "Arm home, feed the cutoff and finish the onward throw for this rules profile.",
        "carom-returned" => "You read the wall bounce and your throw reached third.",
        "wrong-carom-bag" => "After the wall bounce, select third before throwing.",
        "carom-not-returned" => "Collect the rebound and send the ball to the receiver at third.",
        "no-carom" => "This attempt ended before a wall rebound. Retry the wall setup.",
        "wall-rob" => "Your buddy jump caught the would-be home run at the wall.",
        "wall-rob-missed" => "Reach the wall play with your partner and press jump in the window.",
        "choice-at-second" => "Your throw forced the lead runner at second while the batter reached first.",
        "choice-missed" => "Make your first throw to second and beat the lead runner to the bag.",
        "all-runners-returned" => "Both runners advanced and returned safely to their own bags.",
        "runner-slid" or "slid-to-first" => "You started a slide as the runner approached first.",
        "runner-returned" => "You sent the selected runner, halted, and returned safely to second.",
        "runner-dashed" or "dashed-to-first" => "Your dash accelerated the batter-runner along the path to first.",
        "running-opportunity-ended" => "The play ended before you completed the runner sequence. Follow each step in the setup.",
        "pitched-ball" => "Your pitch passed outside the zone for a called ball.",
        "pitch-outside" => "That was not a called ball. Move farther from center and miss the zone without hitting the batter.",
        "move-box" => "Move the batter and oval toward the offset pitch before swinging.",
        "swing-earlier" => "Pull the ball with an earlier tap. Keep the stick centered; aim alone does not teach timing.",
        "swing-later" => "Push the ball with a later tap. Keep the stick centered; aim alone does not teach timing.",
        "hit-grounder" => "Hold up as you tap and release the swing. The fair ball must travel on the ground.",
        "lift-ball" => "Hold down as you tap and release the swing. Lift the fair ball into a fly, not a low liner.",
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
        "manual-takeover" => "You took control, moved the glove and secured the ground ball.",
        "no-takeover" => "Take over the highlighted glove with the stick or WASD and move it yourself.",
        "throw-first" => "Your throw reached the glove at first.",
        "throw-second" => "Your throw reached the glove at second.",
        "throw-third" => "Your throw reached the glove at third.",
        "throw-home" => "Your throw reached the glove at home.",
        "wrong-bag" => "That throw went to another bag. Select the requested base before you throw.",
        "throw-not-received" => "Your throw did not reach the receiving glove in time. Collect the ball, select the requested bag and send it there.",
        "no-throw" => "Collect the ball, select the requested bag and throw. The receiving glove must secure it.",
        "aerial-out" => "Your catch secured the airborne ball for an out.",
        "no-aerial-out" => "No airborne catch this time. Get in position and press catch before the bounce.",
        "jumping-out" => "Your jump caught the ball for an out.",
        "no-jumping-out" => "No jump catch this time. Get close and jump during the catch window.",
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
