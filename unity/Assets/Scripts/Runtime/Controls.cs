using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One gamepad is the product. Keyboard is a debug overlay.
    /// South / East / West / North are positions (Xbox A/B/X/Y, Nintendo B/A/Y/X).
    /// Living couch map: docs/how-to-play.md — update it in the same PR.
    /// </summary>
    public static class Controls
    {
        const float StickDead = 0.22f;
        const float ChargePull = 0.15f;

        static float _rumbleT;
        static float _rumbleLow;
        static float _rumbleHigh;

        /// <summary>One player: last-used pad, else the first connected pad.</summary>
        static Gamepad Pad => Gamepad.current != null ? Gamepad.current
            : Gamepad.all.Count > 0 ? Gamepad.all[0] : null;

        static Keyboard Keys => Keyboard.current;

        public static bool SouthDown => KeyDown(Key.Space) || KeyDown(Key.Enter) || PressedSouth;
        public static bool SouthHeld => Kb(Key.Space) || HeldSouth;
        public static bool NorthDown => KeyDown(Key.Q) || PressedNorth;
        public static bool EastDown => KeyDown(Key.G) || PressedEast;
        public static bool WestDown => KeyDown(Key.F) || PressedWest;
        public static bool WestHeld => Kb(Key.V) || Kb(Key.F) || HeldWest;

        /// <summary>Analog LT / ZL, or Shift. A light pull starts the charge clock.</summary>
        public static float Charge01
        {
            get
            {
                var v = Kb(Key.LeftShift) ? 1f : 0f;
                var pad = Pad;
                if (pad != null) v = Mathf.Max(v, pad.leftTrigger.ReadValue());
                return Mathf.Clamp01(v);
            }
        }

        public static bool Charge => Charge01 >= ChargePull;
        public static bool CyclePitch => KeyDown(Key.Tab) || PressedRb;
        public static bool Steal => KeyDown(Key.X) || PressedLb;
        public static bool Item => KeyDown(Key.E) || (Charge && PressedRb);
        /// <summary>Throw a chemistry item: E, LT+RB, or South+LT.</summary>
        public static bool ItemConfirm => Item || (SouthDown && Charge);
        public static bool Start => KeyDown(Key.H) || PressedStart;
        public static bool SwapPitcher => KeyDown(Key.R) || PressedSelect;
        public static bool TimingAid => KeyDown(Key.F1);
        /// <summary>Debug feel overlay. F2. Not a product control.</summary>
        public static bool FeelDebug => KeyDown(Key.F2);
        /// <summary>Debug slow-mo cycle. Left bracket. Not a product control.</summary>
        public static bool SlowMo => KeyDown(Key.LeftBracket);
        /// <summary>Debug freeze camera. Right bracket. Not a product control.</summary>
        public static bool FreezeCam => KeyDown(Key.RightBracket);
        /// <summary>Title: toggle night. N, or right-stick click.</summary>
        public static bool NightToggle => KeyDown(Key.N) || PressedR3;
        public static bool ParkHeld => Kb(Key.C);

        public static float StickX
        {
            get
            {
                var v = 0f;
                var pad = Pad;
                if (pad != null) v = pad.leftStick.x.ReadValue();
                if (Kb(Key.A) || Kb(Key.LeftArrow)) v -= 1f;
                if (Kb(Key.D) || Kb(Key.RightArrow)) v += 1f;
                if (Mathf.Abs(v) < StickDead) v = 0f;
                return Mathf.Clamp(v, -1f, 1f);
            }
        }

        public static float StickY
        {
            get
            {
                var v = 0f;
                var pad = Pad;
                if (pad != null) v = pad.leftStick.y.ReadValue();
                if (Kb(Key.S) || Kb(Key.DownArrow)) v -= 1f;
                if (Kb(Key.W) || Kb(Key.UpArrow)) v += 1f;
                if (Mathf.Abs(v) < StickDead) v = 0f;
                return Mathf.Clamp(v, -1f, 1f);
            }
        }

        public static int ThrowBag
        {
            get
            {
                if (Kb(Key.Digit1) || DpadRight) return 1;
                if (Kb(Key.Digit2) || DpadUp) return 2;
                if (Kb(Key.Digit3) || DpadLeft) return 3;
                if (Kb(Key.Digit4) || DpadDown) return 4;
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

        public static void Tick(float dt)
        {
            if (_rumbleT <= 0f) return;
            _rumbleT -= dt;
            if (_rumbleT <= 0f) Silence();
            else ApplyRumble();
        }

        public static void RumbleContact(ContactQuality quality)
        {
            if (quality == ContactQuality.Perfect) Pulse(0.22f, 0.45f, 0.85f);
            else if (quality == ContactQuality.Solid) Pulse(0.16f, 0.32f, 0.55f);
            else if (quality == ContactQuality.Cheap) Pulse(0.10f, 0.18f, 0.28f);
        }

        public static void RumbleStar() => Pulse(0.38f, 0.55f, 0.95f);

        public static void Silence()
        {
            _rumbleT = 0f;
            var pad = Pad;
            if (pad != null) pad.ResetHaptics();
        }

        static void Pulse(float seconds, float low, float high)
        {
            _rumbleT = seconds;
            _rumbleLow = low;
            _rumbleHigh = high;
            ApplyRumble();
        }

        static void ApplyRumble()
        {
            var pad = Pad;
            if (pad == null) return;
            pad.SetMotorSpeeds(_rumbleLow, _rumbleHigh);
        }

        static bool PressedSouth { get { var p = Pad; return p != null && p.buttonSouth.wasPressedThisFrame; } }
        static bool HeldSouth { get { var p = Pad; return p != null && p.buttonSouth.isPressed; } }
        static bool PressedEast { get { var p = Pad; return p != null && p.buttonEast.wasPressedThisFrame; } }
        static bool PressedWest { get { var p = Pad; return p != null && p.buttonWest.wasPressedThisFrame; } }
        static bool HeldWest { get { var p = Pad; return p != null && p.buttonWest.isPressed; } }
        static bool PressedNorth { get { var p = Pad; return p != null && p.buttonNorth.wasPressedThisFrame; } }
        static bool PressedLb { get { var p = Pad; return p != null && p.leftShoulder.wasPressedThisFrame; } }
        static bool PressedRb { get { var p = Pad; return p != null && p.rightShoulder.wasPressedThisFrame; } }
        static bool PressedStart { get { var p = Pad; return p != null && p.startButton.wasPressedThisFrame; } }
        static bool PressedSelect { get { var p = Pad; return p != null && p.selectButton.wasPressedThisFrame; } }
        static bool PressedR3 { get { var p = Pad; return p != null && p.rightStickButton.wasPressedThisFrame; } }
        static bool DpadRight { get { var p = Pad; return p != null && p.dpad.right.isPressed; } }
        static bool DpadUp { get { var p = Pad; return p != null && p.dpad.up.isPressed; } }
        static bool DpadLeft { get { var p = Pad; return p != null && p.dpad.left.isPressed; } }
        static bool DpadDown { get { var p = Pad; return p != null && p.dpad.down.isPressed; } }

        static bool Kb(Key k)
        {
            var kb = Keys;
            return kb != null && kb[k].isPressed;
        }

        static bool KeyDown(Key k)
        {
            var kb = Keys;
            return kb != null && kb[k].wasPressedThisFrame;
        }
    }
}
