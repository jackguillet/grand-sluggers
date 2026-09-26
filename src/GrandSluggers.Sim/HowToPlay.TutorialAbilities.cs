namespace GrandSluggers.Sim.Front;

public static partial class HowToPlay
{
    static TutorialCopy? AbilityTutorial(string id) => id switch
    {
        "T-A-lick-catch" => new("Snap with Lick Catch", "Take a fly with Zig's tongue before it lands.",
            "Zig plays center. Stop a few steps short of the ring, facing the ball, and press East as it drops: the tongue snaps 8 ft ahead.",
            "Use the left stick to stop short of the ring, then press East as the ball comes down in front of you."),
        "T-A-wall-spring" => new("Spring off the wall", "Rob a ball over the fence with a Wall Spring fielder.",
            "Tambo plays center. Get to the wall and jump in the window: the spring reaches 4 ft higher than an ordinary leap.",
            "Use the left stick to reach the wall, then press North as the ball arrives."),
        "T-A-relay-pivot" => new("Pivot on the relay", "Feed a Relay Pivot cutoff and send the ball home.",
            "Relay Pivot throws on a caught relay in 0.15 s. Feed the cutoff and command the onward throw at the catch.",
            "Right stick Down arms home; RB feeds the cutoff. RT near the catch sends the next leg."),
        _ => null
    };
}
