using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One gamepad is the couch product. Keyboard is the same scheme (docs/how-to-play.md, Scheme.cs).
    /// South / East / West / North are positions (Xbox A/B/X/Y, Nintendo B/A/Y/X).
    /// F1/F2/F3 stay debug. Update how-to-play in the same PR.
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
        /// <summary>SET batting: all-advance (held). Lineup order uses <see cref="AllAdvanceDown"/>.</summary>
        public static bool AllAdvance => Kb(Key.Comma) || HeldLb;
        public static bool AllAdvanceDown => KeyDown(Key.Comma) || PressedLb;
        public static bool AllReturn => Kb(Key.Period) || HeldRb;
        public static bool FreezeRunners => Kb(Key.Slash) || (HeldLb && HeldRb);
        /// <summary>Steal is L3 / Z so bumpers can send / return.</summary>
        public static bool Steal => KeyDown(Key.Z) || PressedL3;
        /// <summary>No-direction cutoff after a catch: LB / X. Relay, not a random bag.</summary>
        public static bool Cutoff => Kb(Key.X) || HeldLb;
        public static bool Item => KeyDown(Key.E) || (Charge && PressedRb);
        /// <summary>Throw a chemistry item: E, LT+RB, or South+LT.</summary>
        public static bool ItemConfirm => Item || (SouthDown && Charge);
        public static bool Start => KeyDown(Key.H) || PressedStart;
        public static bool SwapPitcher => KeyDown(Key.R) || PressedSelect;
        public static bool TimingAid => KeyDown(Key.F1);
        /// <summary>Debug feel overlay. F2. Not a product control.</summary>
        public static bool FeelDebug => KeyDown(Key.F2);
        /// <summary>Debug mute play HUD. F3. Trailer stills without a star.</summary>
        public static bool HudMute => KeyDown(Key.F3);
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
                if (Kb(Key.A)) v -= 1f;
                if (Kb(Key.D)) v += 1f;
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
                if (Kb(Key.S)) v -= 1f;
                if (Kb(Key.W)) v += 1f;
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

        /// <summary>Pad stick diamond: right 1B, up 2B, left 3B, down home. WASD is run, not throw.</summary>
        public static int StickBag
        {
            get
            {
                var pad = Pad;
                if (pad == null) return 0;
                var x = pad.leftStick.x.ReadValue();
                var y = pad.leftStick.y.ReadValue();
                if (Mathf.Abs(x) < StickDead && Mathf.Abs(y) < StickDead) return 0;
                return InPlay.DiamondBag(x, y);
            }
        }

        /// <summary>Arrows name a bag when not chasing (same diamond as the d-pad).</summary>
        public static int ArrowBag
        {
            get
            {
                if (Kb(Key.RightArrow)) return 1;
                if (Kb(Key.UpArrow)) return 2;
                if (Kb(Key.LeftArrow)) return 3;
                if (Kb(Key.DownArrow)) return 4;
                return 0;
            }
        }

        /// <summary>SET lead: pad stick or WASD toward the next bag.</summary>
        public static int AimBag
        {
            get
            {
                var n = InPlay.DiamondBag(StickX, StickY);
                if (n > 0) return n;
                return ArrowBag;
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
        static bool HeldLb { get { var p = Pad; return p != null && p.leftShoulder.isPressed; } }
        static bool PressedRb { get { var p = Pad; return p != null && p.rightShoulder.wasPressedThisFrame; } }
        static bool HeldRb { get { var p = Pad; return p != null && p.rightShoulder.isPressed; } }
        static bool PressedL3 { get { var p = Pad; return p != null && p.leftStickButton.wasPressedThisFrame; } }
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
