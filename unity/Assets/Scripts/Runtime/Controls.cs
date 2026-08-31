using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Pad 1 is gamepad 0 + keyboard. Pad 2 is gamepad 1. Not Gamepad.current —
    /// two pads must not steer the same pitcher. Menus and 1P read Pad1.
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

        /// <summary>One seated pad. Index 0 is home (keyboard too). Index 1 is away. CPU is dead.</summary>
        public readonly struct Pad
        {
            readonly int _index;
            readonly bool _keys;

            public Pad(int index, bool keys)
            {
                _index = index;
                _keys = keys;
            }

            public bool Present => Device != null || _keys;

            Gamepad Device =>
                _index >= 0 && Gamepad.all.Count > _index ? Gamepad.all[_index] : null;

            public bool SouthDown => KeyDown(Key.Space) || KeyDown(Key.Enter) || Pressed(Device?.buttonSouth);
            public bool SouthHeld => Kb(Key.Space) || Held(Device?.buttonSouth);
            public bool NorthDown => KeyDown(Key.Q) || Pressed(Device?.buttonNorth);
            public bool EastDown => KeyDown(Key.G) || Pressed(Device?.buttonEast);
            public bool EastHeld => Kb(Key.G) || Held(Device?.buttonEast);
            public bool WestDown => KeyDown(Key.F) || Pressed(Device?.buttonWest);
            public bool WestHeld => Kb(Key.V) || Kb(Key.F) || Held(Device?.buttonWest);

            public float Charge01
            {
                get
                {
                    var v = Kb(Key.LeftShift) ? 1f : 0f;
                    var pad = Device;
                    if (pad != null) v = Mathf.Max(v, pad.leftTrigger.ReadValue());
                    return Mathf.Clamp01(v);
                }
            }

            public bool Charge => Charge01 >= ChargePull;
            public bool CyclePitch => KeyDown(Key.Tab) || Pressed(Device?.rightShoulder);
            public bool Changeup => WestHeld;
            public bool Skip => EastDown;
            public bool Start => KeyDown(Key.H) || Pressed(Device?.startButton);
            public bool AllAdvance => Kb(Key.Comma) || Held(Device?.leftShoulder);
            public bool AllAdvanceDown => KeyDown(Key.Comma) || Pressed(Device?.leftShoulder);
            public bool AllReturn => Kb(Key.Period) || Held(Device?.rightShoulder);
            public bool FreezeRunners => Kb(Key.Slash) || (Held(Device?.leftShoulder) && Held(Device?.rightShoulder));
            public bool Steal => KeyDown(Key.Z) || Pressed(Device?.leftStickButton);
            public bool Cutoff => Kb(Key.X) || Held(Device?.leftShoulder);
            public bool Item => KeyDown(Key.E) || (Charge && Pressed(Device?.rightShoulder));
            public bool ItemConfirm => Item || (SouthDown && Charge);
            public bool SwapPitcher => KeyDown(Key.R) || Pressed(Device?.selectButton);
            public bool NightToggle => KeyDown(Key.N) || Pressed(Device?.rightStickButton);

            public float StickX
            {
                get
                {
                    var v = 0f;
                    var pad = Device;
                    if (pad != null) v = pad.leftStick.x.ReadValue();
                    if (Kb(Key.A)) v -= 1f;
                    if (Kb(Key.D)) v += 1f;
                    if (Mathf.Abs(v) < StickDead) v = 0f;
                    return Mathf.Clamp(v, -1f, 1f);
                }
            }

            public float StickY
            {
                get
                {
                    var v = 0f;
                    var pad = Device;
                    if (pad != null) v = pad.leftStick.y.ReadValue();
                    if (Kb(Key.S)) v -= 1f;
                    if (Kb(Key.W)) v += 1f;
                    if (Mathf.Abs(v) < StickDead) v = 0f;
                    return Mathf.Clamp(v, -1f, 1f);
                }
            }

            public bool MenuDown =>
                KeyDown(Key.S) || KeyDown(Key.DownArrow) || PressedDpad(Device?.dpad.down);
            public bool MenuUp =>
                KeyDown(Key.W) || KeyDown(Key.UpArrow) || PressedDpad(Device?.dpad.up);

            public int ThrowBag
            {
                get
                {
                    if (Kb(Key.Digit1) || Dpad(Device?.dpad.right)) return 1;
                    if (Kb(Key.Digit2) || Dpad(Device?.dpad.up)) return 2;
                    if (Kb(Key.Digit3) || Dpad(Device?.dpad.left)) return 3;
                    if (Kb(Key.Digit4) || Dpad(Device?.dpad.down)) return 4;
                    return 0;
                }
            }

            public int StickBag
            {
                get
                {
                    var pad = Device;
                    if (pad == null) return 0;
                    var x = pad.leftStick.x.ReadValue();
                    var y = pad.leftStick.y.ReadValue();
                    if (Mathf.Abs(x) < StickDead && Mathf.Abs(y) < StickDead) return 0;
                    return InPlay.DiamondBag(x, y);
                }
            }

            public int ArrowBag
            {
                get
                {
                    if (!_keys) return 0;
                    if (Kb(Key.RightArrow)) return 1;
                    if (Kb(Key.UpArrow)) return 2;
                    if (Kb(Key.LeftArrow)) return 3;
                    if (Kb(Key.DownArrow)) return 4;
                    return 0;
                }
            }

            public int AimBag
            {
                get
                {
                    var n = InPlay.DiamondBag(StickX, StickY);
                    if (n > 0) return n;
                    return ArrowBag;
                }
            }

            public void Rumble(float low, float high)
            {
                var pad = Device;
                if (pad != null) pad.SetMotorSpeeds(low, high);
            }

            public void Silence()
            {
                var pad = Device;
                if (pad != null) pad.ResetHaptics();
            }

            bool Kb(Key k)
            {
                if (!_keys) return false;
                var kb = Keyboard.current;
                return kb != null && kb[k].isPressed;
            }

            bool KeyDown(Key k)
            {
                if (!_keys) return false;
                var kb = Keyboard.current;
                return kb != null && kb[k].wasPressedThisFrame;
            }

            static bool Pressed(ButtonControl b) => b != null && b.wasPressedThisFrame;
            static bool Held(ButtonControl b) => b != null && b.isPressed;
            static bool Dpad(ButtonControl b) => b != null && b.isPressed;
            static bool PressedDpad(ButtonControl b) => b != null && b.wasPressedThisFrame;
        }

        public static Pad Pad1 { get; } = new(0, true);
        public static Pad Pad2 { get; } = new(1, false);
        public static Pad None { get; } = new(-1, false);

        public static int PadCount => Gamepad.all.Count;

        public static Pad Of(LineupSeat seat) => seat switch
        {
            LineupSeat.Pad2 => Pad2,
            LineupSeat.Cpu => None,
            _ => Pad1
        };

        public static bool SouthDown => Pad1.SouthDown;
        public static bool SouthHeld => Pad1.SouthHeld;
        public static bool NorthDown => Pad1.NorthDown;
        public static bool EastDown => Pad1.EastDown;
        public static bool EastHeld => Pad1.EastHeld;
        public static bool WestDown => Pad1.WestDown;
        public static bool WestHeld => Pad1.WestHeld;
        public static float Charge01 => Pad1.Charge01;
        public static bool Charge => Pad1.Charge;
        public static bool CyclePitch => Pad1.CyclePitch;
        public static bool Changeup => Pad1.Changeup;
        public static bool Skip => Pad1.Skip;
        public static bool CallTime => Pad1.Start || (Pad2.Present && Pad2.Start);
        public static bool AllAdvance => Pad1.AllAdvance;
        public static bool AllAdvanceDown => Pad1.AllAdvanceDown;
        public static bool AllReturn => Pad1.AllReturn;
        public static bool FreezeRunners => Pad1.FreezeRunners;
        public static bool Steal => Pad1.Steal;
        public static bool Cutoff => Pad1.Cutoff;
        public static bool Item => Pad1.Item;
        public static bool ItemConfirm => Pad1.ItemConfirm;
        public static bool Start => Pad1.Start;
        public static bool SwapPitcher => Pad1.SwapPitcher;
        public static bool TimingAid => KeyDown(Key.F1);
        public static bool FeelDebug => KeyDown(Key.F2);
        public static bool HudMute => KeyDown(Key.F3);
        public static bool SlowMo => KeyDown(Key.LeftBracket);
        public static bool FreezeCam => KeyDown(Key.RightBracket);
        public static bool NightToggle => Pad1.NightToggle;
        public static bool ParkHeld => Kb(Key.C);
        public static float StickX => Pad1.StickX;
        public static float StickY => Pad1.StickY;
        public static bool MenuDown => Pad1.MenuDown;
        public static bool MenuUp => Pad1.MenuUp;
        public static int ThrowBag => Pad1.ThrowBag;
        public static int StickBag => Pad1.StickBag;
        public static int ArrowBag => Pad1.ArrowBag;
        public static int AimBag => Pad1.AimBag;

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
            Pad1.Silence();
            Pad2.Silence();
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
            Pad1.Rumble(_rumbleLow, _rumbleHigh);
            if (Pad2.Present) Pad2.Rumble(_rumbleLow, _rumbleHigh);
        }

        static bool Kb(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].isPressed;
        }

        static bool KeyDown(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].wasPressedThisFrame;
        }
    }
}
