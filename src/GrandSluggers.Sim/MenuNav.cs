namespace GrandSluggers.Sim;

/// <summary>
/// Couch menus: flick once, then rest. Hold does not auto-repeat.
/// Digital taps (WASD, arrows, d-pad) are edges. Analog is pad stick only.
/// </summary>
public static class MenuNav
{
    public const float Threshold = 0.50f;
    public const float Release = 0.32f;
    public const float WheelRest = 0.15f;

    public struct Gate
    {
        float _armed;

        public void Catch(float axis) => _armed = Arm(axis);

        public int Tick(float axis, int tap, float dt) =>
            Step(axis, tap, ref _armed);
    }

    public static float Arm(float axis) =>
        axis >= Threshold ? 1f : axis <= -Threshold ? -1f : 0f;

    public static int Step(float axis, int tap, ref float armed)
    {
        if (tap != 0)
        {
            armed = tap > 0 ? 1f : -1f;
            return tap > 0 ? 1 : -1;
        }
        return AxisStep(axis, ref armed);
    }

    public static int AxisStep(float axis, ref float armed)
    {
        if (Math.Abs(axis) <= Release)
        {
            armed = 0f;
            return 0;
        }
        if (axis >= Threshold && armed <= 0f)
        {
            armed = 1f;
            return 1;
        }
        if (axis <= -Threshold && armed >= 0f)
        {
            armed = -1f;
            return -1;
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
