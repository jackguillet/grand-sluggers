namespace GrandSluggers.Sim;

/// <summary>
/// Staged still-gate poses. Scoop lives on the dirt in the first-base hole,
/// not on the rubber — the 2026-08-24 PNG was Ashlord on the mound.
/// </summary>
public static class StillPose
{
    public const double ScoopX = 24;
    public const double ScoopZ = 36;
    public const double ScoopBallY = 1.05;
    public const double RunnerX = 22;
    public const double RunnerZ = 22;
    public const string ScoopGlove = "2B";

    public static bool ScoopIsNotTheMound(double x, double z) =>
        Diamond.Dist(x, z, 0, Diamond.Mound) > 20;
}
