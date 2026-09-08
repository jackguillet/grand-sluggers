namespace GrandSluggers.Sim;

/// <summary>
/// Gameplay analog: recenter a sitting stick (drift), then deadzone.
/// A parked pad does not walk the pitcher. Holding left still walks.
/// </summary>
public static class StickPlay
{
    public const float Dead = 0.28f;
    public const float RecenterRadius = 0.42f;
    public const float StillDelta = 0.05f;
    public const float RecenterAfter = 0.18f;

    public static (float RestX, float RestY, float Still) Recenter(
        float rawX, float rawY, float lastX, float lastY,
        float restX, float restY, float still, float dt)
    {
        var mag = Math.Sqrt(rawX * rawX + rawY * rawY);
        var shake = Math.Abs(rawX - lastX) + Math.Abs(rawY - lastY);
        if (mag <= RecenterRadius && shake <= StillDelta)
        {
            still += Math.Max(0, dt);
            if (still >= RecenterAfter)
                return (rawX, rawY, still);
            return (restX, restY, still);
        }
        return (restX, restY, 0);
    }

    public static float Live(float raw, float rest)
    {
        var v = raw - rest;
        if (Math.Abs(v) < Dead) return 0;
        return Math.Clamp(v, -1, 1);
    }
}
