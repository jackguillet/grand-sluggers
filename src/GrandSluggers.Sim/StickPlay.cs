namespace GrandSluggers.Sim;

/// <summary>
/// Gameplay analog: the pad's left stick. A parked pad does not
/// walk the pitcher. Analog is dead until the stick has been in the deadzone
/// this Catch (SET). Holding a throw still walks.
/// </summary>
public static class StickPlay
{
    public const float Dead = 0.32f;
    public const float RecenterRadius = 0.45f;
    public const float StillDelta = 0.05f;
    public const float RecenterAfter = 0.12f;

    public struct Pad
    {
        public float RestX;
        public float RestY;
        public float Still;
        public float LastX;
        public float LastY;
        public bool SeenCenter;

        /// <summary>
        /// Verb start (SET). Off-center analog is ignored until it passes
        /// through rest. A sitting Steam stick does not walk.
        /// </summary>
        public void Catch(float rawX, float rawY)
        {
            LastX = rawX;
            LastY = rawY;
            Still = 0;
            if (Mag(rawX, rawY) <= Dead)
            {
                RestX = rawX;
                RestY = rawY;
                SeenCenter = true;
            }
            else
            {
                RestX = 0;
                RestY = 0;
                SeenCenter = false;
            }
        }

        public void Tick(float rawX, float rawY, float dt)
        {
            var mag = Mag(rawX, rawY);
            var shake = Math.Abs(rawX - LastX) + Math.Abs(rawY - LastY);
            LastX = rawX;
            LastY = rawY;
            if (mag <= Dead)
            {
                RestX = rawX;
                RestY = rawY;
                Still = 0;
                SeenCenter = true;
                return;
            }
            if (!SeenCenter)
            {
                Still = 0;
                return;
            }
            if (mag <= RecenterRadius && shake <= StillDelta)
            {
                Still += Math.Max(0, dt);
                if (Still >= RecenterAfter)
                {
                    RestX = rawX;
                    RestY = rawY;
                }
                return;
            }
            Still = 0;
        }

        public float LiveX(float rawX) => SeenCenter ? Live(rawX, RestX) : 0;
        public float LiveY(float rawY) => SeenCenter ? Live(rawY, RestY) : 0;
    }

    public static float Live(float raw, float rest)
    {
        var v = raw - rest;
        if (Math.Abs(v) < Dead) return 0;
        return Math.Clamp(v, -1, 1);
    }

    public static float Mag(float x, float y) => (float)Math.Sqrt(x * x + y * y);
}
