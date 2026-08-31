namespace GrandSluggers.Sim;

/// <summary>
/// Mouse analog is hold-to-aim, never cursor-vs-center. Parked cursor is dead stick (#349).
/// </summary>
public static class MouseStick
{
    public const float Sens = 0.018f;
    public const float DecayPerSec = 6f;
    public const float DeadDelta = 1f;

    public static (float X, float Y) Tick(float x, float y, float dx, float dy, bool analogHeld, float dt)
    {
        if (!analogHeld) return (0, 0);
        x = Math.Clamp(x + dx * Sens, -1, 1);
        y = Math.Clamp(y + dy * Sens, -1, 1);
        if (dx * dx + dy * dy < DeadDelta * DeadDelta)
        {
            var step = DecayPerSec * Math.Max(0, dt);
            x = MoveTo(x, 0, step);
            y = MoveTo(y, 0, step);
        }
        return (x, y);
    }

    static float MoveTo(float v, float target, float step)
    {
        if (v > target) return Math.Max(target, v - step);
        if (v < target) return Math.Min(target, v + step);
        return target;
    }
}
