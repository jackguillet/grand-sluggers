namespace GrandSluggers.Sim;

/// <summary>
/// Couch menus: flick once, then rest. Hold a hard throw (stick / d-pad) to
/// repeat after a beat. Drift and mouse aim do not auto-repeat.
/// </summary>
public static class MenuNav
{
    public const float Threshold = 0.50f;
    public const float Release = 0.32f;
    public const float RepeatNeed = 0.75f;
    public const float RepeatAfter = 0.40f;
    public const float RepeatEvery = 0.14f;
    public const float WheelRest = 0.15f;

    public struct Gate
    {
        float _armed;
        float _hold;

        public void Catch(float axis)
        {
            _armed = Arm(axis);
            _hold = 0f;
        }

        public int Tick(float axis, int tap, float dt) =>
            Step(axis, tap, dt, ref _armed, ref _hold);
    }

    public static float Arm(float axis) =>
        axis >= Threshold ? 1f : axis <= -Threshold ? -1f : 0f;

    public static int Step(float axis, int tap, ref float armed) =>
        AxisStep(axis, tap, ref armed);

    public static int Step(float axis, int tap, float dt, ref float armed, ref float hold)
    {
        if (tap != 0)
        {
            armed = tap > 0 ? 1f : -1f;
            hold = 0f;
            return tap > 0 ? 1 : -1;
        }
        return AxisStep(axis, dt, ref armed, ref hold);
    }

    public static int AxisStep(float axis, ref float armed) =>
        AxisStep(axis, 0, ref armed);

    static int AxisStep(float axis, int tap, ref float armed)
    {
        var hold = 0f;
        if (tap != 0)
        {
            armed = tap > 0 ? 1f : -1f;
            return tap > 0 ? 1 : -1;
        }
        return AxisStep(axis, 0f, ref armed, ref hold);
    }

    public static int AxisStep(float axis, float dt, ref float armed, ref float hold)
    {
        if (Math.Abs(axis) <= Release)
        {
            armed = 0f;
            hold = 0f;
            return 0;
        }
        if (axis >= Threshold && armed <= 0f)
        {
            armed = 1f;
            hold = 0f;
            return 1;
        }
        if (axis <= -Threshold && armed >= 0f)
        {
            armed = -1f;
            hold = 0f;
            return -1;
        }
        if (armed != 0f && Math.Abs(axis) >= RepeatNeed && dt > 0f)
        {
            hold += dt;
            if (hold >= RepeatAfter)
            {
                hold = RepeatAfter - RepeatEvery;
                return armed > 0f ? 1 : -1;
            }
        }
        return 0;
    }

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
