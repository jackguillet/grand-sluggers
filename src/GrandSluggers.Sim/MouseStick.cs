namespace GrandSluggers.Sim;

/// <summary>
/// Mouse analog is hold-to-aim, never cursor-vs-center (#349). This-frame
/// drag, not an integrator — a parked cursor is dead, including while
/// right-click charges. Noise below DragNeed is not a throw.
/// </summary>
public static class MouseStick
{
    public const float Sens = 0.018f;
    public const float DragNeed = 8f;

    public static (float X, float Y) Tick(float x, float y, float dx, float dy, bool analogHeld, float dt)
    {
        _ = x;
        _ = y;
        _ = dt;
        if (!analogHeld) return (0, 0);
        if (dx * dx + dy * dy < DragNeed * DragNeed) return (0, 0);
        return (Math.Clamp(dx * Sens, -1, 1), Math.Clamp(dy * Sens, -1, 1));
    }
}
