namespace GrandSluggers.Sim;

/// <summary>Presentation input edges and authored take clocks; baseball remains in PitchSetupSystem.</summary>
public static class StealPresentation
{
    /// <summary>A target plus a fresh press is a base throw; choosing a target while holding a committed pitch is a step-off attempt.</summary>
    public static bool BaseThrow(int bag, int previousBag, bool pressed, bool held, bool committed) =>
        bag is >= 1 and <= 4 && (pressed || committed && held && bag != previousBag);

    /// <summary>Warp the existing throw take's release marker to the sim's preparation clock, then play its follow-through.</summary>
    public static double ThrowSample(double commandTime, double preparationSeconds) =>
        commandTime < preparationSeconds && preparationSeconds > 0
            ? Math.Max(0, commandTime) / preparationSeconds * Motion.ThrowRelease
            : Motion.ThrowRelease + Math.Max(0, commandTime - preparationSeconds);
}
