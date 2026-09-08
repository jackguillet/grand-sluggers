namespace GrandSluggers.Sim;

/// <summary>
/// In-play cartoon impact. HUD-off stills should show punch, dirt, a dirt ring, and a colored throw.
/// </summary>
public static class CartoonJuice
{
    public const double CheapFreeze = 0.06;
    public const float PerfectPunch = 16f;
    public const float SolidPunch = 10f;
    public const float CheapPunch = 6f;
    public const double RunFromBallFt = 12;
    public const double WalkFtPerSec = 3.5;
    public const double RunFtPerSec = 14;

    public static bool DirtPuff(ContactQuality quality) =>
        quality is ContactQuality.Cheap or ContactQuality.Solid or ContactQuality.Perfect;

    public static float Punch(ContactQuality quality) => quality switch
    {
        ContactQuality.Perfect => PerfectPunch,
        ContactQuality.Solid => SolidPunch,
        ContactQuality.Cheap => CheapPunch,
        _ => 0
    };

    /// <summary>
    /// Run cycle only while closing on the plant (landing in the air, hop on the dirt).
    /// Dist to the live ball while a fly is up is not a chase — the glove waits on the ring.
    /// </summary>
    public static bool ChaseIsARun(bool caught, double distToPlant) =>
        !caught && distToPlant > RunFromBallFt;

    /// <summary>Limb stride follows feet. Waiting under a fly is standing.</summary>
    public static bool StandingStill(double speedFtPerSec) => speedFtPerSec <= WalkFtPerSec;

    /// <summary>Gold/purple laser vs muddy. RGB 0–1.</summary>
    public static (double R, double G, double B) ThrowRgb(Chemistry rel) => rel switch
    {
        Chemistry.Good => (0.82, 0.42, 0.95),
        Chemistry.Bad => (0.40, 0.30, 0.16),
        _ => (0.92, 0.90, 0.84)
    };
}
