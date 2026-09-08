namespace GrandSluggers.Sim;

/// <summary>
/// Couch menus: one step per flick. Held analog, stick drift, and inertial
/// scroll do not auto-repeat. Mouse aim is not a menu stick.
/// </summary>
public static class MenuNav
{
    public const float Threshold = 0.45f;
    public const float Dead = 0.22f;
    public const float WheelRest = 0.15f;

    /// <summary>Latch the current side so a held stick does not fire on open.</summary>
    public static float Arm(float axis) =>
        axis >= Threshold ? 1f : axis <= -Threshold ? -1f : 0f;

    /// <summary>Digital tap wins. Analog still has to rest. A drifted pad does not eat the keys.</summary>
    public static int Step(float axis, int tap, ref float armed)
    {
        if (tap != 0)
        {
            armed = tap > 0 ? 1f : -1f;
            return tap > 0 ? 1 : -1;
        }
        return AxisStep(axis, ref armed);
    }

    /// <summary>+1 / -1 when the axis leaves dead and crosses threshold. Must rest to fire again.</summary>
    public static int AxisStep(float axis, ref float armed)
    {
        if (Math.Abs(axis) <= Dead)
        {
            armed = 0f;
            return 0;
        }
        if (armed != 0f) return 0;
        if (axis >= Threshold)
        {
            armed = 1f;
            return 1;
        }
        if (axis <= -Threshold)
        {
            armed = -1f;
            return -1;
        }
        return 0;
    }

    /// <summary>One page per scroll burst. Inertia after the first tick is ignored until rest.</summary>
    public static int WheelStep(float scrollY, ref bool spinning)
    {
        if (Math.Abs(scrollY) < WheelRest)
        {
            spinning = false;
            return 0;
        }
        if (spinning) return 0;
        spinning = true;
        return scrollY < 0f ? 1 : -1;
    }
}
