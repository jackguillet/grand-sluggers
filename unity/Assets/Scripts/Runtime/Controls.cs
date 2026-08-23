using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One gamepad layout is the product. Keyboard is a debug overlay.
    /// </summary>
    public static class Controls
    {
        public static bool SouthDown => KeyDown(KeyCode.Space) || KeyDown(KeyCode.Return) || PadDown(0);
        public static bool SouthHeld => Key(KeyCode.Space) || PadHeld(0);
        public static bool NorthDown => KeyDown(KeyCode.Q) || PadDown(3);
        public static bool EastDown => KeyDown(KeyCode.G) || PadDown(1);
        public static bool WestDown => KeyDown(KeyCode.F) || PadDown(2);
        public static bool WestHeld => Key(KeyCode.V) || Key(KeyCode.F) || PadHeld(2);
        public static bool Charge => Key(KeyCode.LeftShift) || PadHeld(6) || Axis("Fire3") > 0.45f;
        public static bool CyclePitch => KeyDown(KeyCode.Tab) || PadDown(5);
        public static bool Steal => KeyDown(KeyCode.X) || PadDown(4);
        public static bool Item => KeyDown(KeyCode.E) || (PadHeld(6) && PadDown(5));
        /// <summary>Throw a chemistry item: E, LT+RB, or South+LT.</summary>
        public static bool ItemConfirm => Item || (SouthDown && Charge);
        public static bool Start => KeyDown(KeyCode.H) || PadDown(7);
        public static bool SwapPitcher => KeyDown(KeyCode.R) || PadDown(8);
        public static bool TimingAid => KeyDown(KeyCode.F1);
        /// <summary>Debug feel overlay. F2. Not a product control.</summary>
        public static bool FeelDebug => KeyDown(KeyCode.F2);
        /// <summary>Debug slow-mo cycle. Left bracket. Not a product control.</summary>
        public static bool SlowMo => KeyDown(KeyCode.LeftBracket);
        /// <summary>Debug freeze camera. Right bracket. Not a product control.</summary>
        public static bool FreezeCam => KeyDown(KeyCode.RightBracket);
        /// <summary>Title: toggle night. N, or right-stick click.</summary>
        public static bool NightToggle => KeyDown(KeyCode.N) || PadDown(9);
        public static bool ParkHeld => Key(KeyCode.C);

        public static float StickX
        {
            get
            {
                var v = Input.GetAxisRaw("Horizontal");
                if (Key(KeyCode.A) || Key(KeyCode.LeftArrow)) v -= 1;
                if (Key(KeyCode.D) || Key(KeyCode.RightArrow)) v += 1;
                return Mathf.Clamp(v, -1, 1);
            }
        }

        public static float StickY
        {
            get
            {
                var v = Input.GetAxisRaw("Vertical");
                if (Key(KeyCode.S) || Key(KeyCode.DownArrow)) v -= 1;
                if (Key(KeyCode.W) || Key(KeyCode.UpArrow)) v += 1;
                return Mathf.Clamp(v, -1, 1);
            }
        }

        public static int ThrowBag
        {
            get
            {
                if (Key(KeyCode.Alpha1) || PadHeld(15)) return 1;
                if (Key(KeyCode.Alpha2) || PadHeld(13)) return 2;
                if (Key(KeyCode.Alpha3) || PadHeld(16)) return 3;
                if (Key(KeyCode.Alpha4) || PadHeld(14)) return 4;
                return 0;
            }
        }

        /// <summary>Stick flick to a bag: right 1B, up 2B, left 3B, down home.</summary>
        public static int StickBag
        {
            get
            {
                var x = StickX;
                var y = StickY;
                if (x * x + y * y < 0.55f) return 0;
                if (Mathf.Abs(x) > Mathf.Abs(y)) return x > 0 ? 1 : 3;
                return y > 0 ? 2 : 4;
            }
        }

        static bool Key(KeyCode k) => Input.GetKey(k);
        static bool KeyDown(KeyCode k) => Input.GetKeyDown(k);
        static bool PadDown(int n) => Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + n));
        static bool PadHeld(int n) => Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + n));
        static float Axis(string name) => Input.GetAxis(name);
    }
}
