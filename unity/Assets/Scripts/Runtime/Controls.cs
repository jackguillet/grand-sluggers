using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The front end offers gamepad 0 + keyboard/mouse to Player 1 and gamepad 1 to Player 2.
    /// BeginMatch locks InputDevice.deviceId per logical seat so Gamepad.all reordering cannot
    /// move a surviving controller to the other team. Menus and 1P read Pad1.
    /// South / East / West / North are positions (Xbox A/B/X/Y, Nintendo B/A/Y/X).
    /// F1/F2/F3 stay debug. Update how-to-play in the same PR.
    /// </summary>
    public static class Controls
    {
        const float ChargePull = 0.15f;
        const string P1InputKey = "gs.input.p1";

        public enum P1InputMode
        {
            Auto = 0,
            Controller = 1,
            KeyboardMouse = 2
        }

        static P1InputMode _p1InputMode = P1InputMode.Auto;
        static DeviceSeats _matchDevices;
        static bool _matchDevicesBound;

        /// <summary>Player 1's device choice. Auto prefers a connected controller.</summary>
        public static P1InputMode Player1InputMode => _p1InputMode;

        public static string Player1InputLabel => _p1InputMode switch
        {
            P1InputMode.Controller => "Controller",
            P1InputMode.KeyboardMouse => "Keyboard + mouse",
            _ => HasController(0) ? "Controller (auto)" : "Keyboard + mouse (auto)"
        };

        static bool KeyboardMouseEnabled => _matchDevicesBound
            ? _matchDevices != null && _matchDevices.Pad1UsesKeyboardMouse
            : _p1InputMode == P1InputMode.KeyboardMouse || !HasController(0);

        static bool HasController(int index) => index >= 0 && Gamepad.all.Count > index;

        public static void Initialize()
        {
            EndMatch();
            _p1InputMode = PlayerPrefs.HasKey(P1InputKey)
                ? (P1InputMode)Mathf.Clamp(PlayerPrefs.GetInt(P1InputKey), 0, 2)
                : P1InputMode.Auto;
        }

        public static bool Player1InputToggleDown => RawKeyDown(Key.F6);

        public static void CyclePlayer1Input()
        {
            _p1InputMode = _p1InputMode switch
            {
                P1InputMode.Auto => P1InputMode.Controller,
                P1InputMode.Controller => P1InputMode.KeyboardMouse,
                _ => P1InputMode.Auto
            };
            PlayerPrefs.SetInt(P1InputKey, (int)_p1InputMode);
            PlayerPrefs.Save();
            CatchPlay();
        }

        static float _rumbleT;
        static float _rumbleLow;
        static float _rumbleHigh;
        static float _mouseX;
        static float _mouseY;
        static float _mouseLive;
        static readonly StickPlay.Pad[] _pads = new StickPlay.Pad[2];
        static StickPlay.Key _keyA, _keyD, _keyW, _keyS;

        /// <summary>One logical seat. Device identity is fixed while a match is active. CPU is dead.</summary>
        public readonly struct Pad
        {
            readonly int _index;
            readonly bool _keys;

            public Pad(int index, bool keys)
            {
                _index = index;
                _keys = keys;
            }

            bool KeysEnabled => _keys && KeyboardMouseEnabled;

            public bool Present => Device != null || KeysEnabled;

            Gamepad Device => DeviceForIndex(_index);

            public bool SouthDown => KeyDown(Key.Space) || KeyDown(Key.Enter) || Pressed(Device?.buttonSouth)
                || (KeysEnabled && MouseLeftDown);
            public bool SouthHeld => Kb(Key.Space) || Kb(Key.Enter) || Held(Device?.buttonSouth)
                || (KeysEnabled && MouseLeftHeld);
            public bool SouthUp => KeyUp(Key.Space) || KeyUp(Key.Enter) || Released(Device?.buttonSouth)
                || (KeysEnabled && MouseLeftUp);
            public bool NorthDown => KeyDown(Key.Q) || Pressed(Device?.buttonNorth) || (KeysEnabled && MouseMiddleDown);
            public bool EastDown => KeyDown(Key.G) || Pressed(Device?.buttonEast);
            public bool EastHeld => Kb(Key.G) || Held(Device?.buttonEast);
            public bool WestDown => KeyDown(Key.F) || Pressed(Device?.buttonWest);
            public bool WestHeld => Kb(Key.V) || Kb(Key.F) || Kb(Key.LeftCtrl) || Held(Device?.buttonWest);
            public bool Attack => NorthDown || KeyDown(Key.B);

            public float Charge01
            {
                get
                {
                    var v = Kb(Key.LeftShift) || (KeysEnabled && MouseRightHeld) ? 1f : 0f;
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
            public bool Esc => KeyDown(Key.Escape);
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
                    var pad = PlayPad.LiveX(RawX);
                    var key = 0f;
                    var mouse = 0f;
                    if (KeysEnabled)
                    {
                        key = (_keyD.On ? 1f : 0f) - (_keyA.On ? 1f : 0f);
                        mouse = MouseStickX;
                    }
                    return StickPlay.Mix(pad, key, mouse);
                }
            }

            public float StickY
            {
                get
                {
                    var pad = PlayPad.LiveY(RawY);
                    var key = 0f;
                    var mouse = 0f;
                    if (KeysEnabled)
                    {
                        key = (_keyW.On ? 1f : 0f) - (_keyS.On ? 1f : 0f);
                        mouse = MouseStickY;
                    }
                    return StickPlay.Mix(pad, key, mouse);
                }
            }

            StickPlay.Pad PlayPad =>
                _index >= 0 && _index < _pads.Length ? _pads[_index] : default;

            float RawX
            {
                get
                {
                    var pad = Device;
                    return pad == null ? 0f : pad.leftStick.x.ReadValue();
                }
            }

            float RawY
            {
                get
                {
                    var pad = Device;
                    return pad == null ? 0f : pad.leftStick.y.ReadValue();
                }
            }

            public bool MenuDown =>
                KeyDown(Key.S) || KeyDown(Key.DownArrow) || PressedDpad(Device?.dpad.down);
            public bool MenuUp =>
                KeyDown(Key.W) || KeyDown(Key.UpArrow) || PressedDpad(Device?.dpad.up);

            /// <summary>Recentered pad stick + held d-pad. Mouse aim is not a menu stick.</summary>
            public float MenuAxisX
            {
                get
                {
                    var v = PlayPad.LiveX(RawX);
                    if (Dpad(Device?.dpad.left)) v -= 1f;
                    if (Dpad(Device?.dpad.right)) v += 1f;
                    return Mathf.Clamp(v, -1f, 1f);
                }
            }

            public float MenuAxisY
            {
                get
                {
                    var v = PlayPad.LiveY(RawY);
                    if (Dpad(Device?.dpad.down)) v -= 1f;
                    if (Dpad(Device?.dpad.up)) v += 1f;
                    return Mathf.Clamp(v, -1f, 1f);
                }
            }

            public int MenuTapX
            {
                get
                {
                    var n = 0;
                    if (KeyDown(Key.A) || KeyDown(Key.LeftArrow) || PressedDpad(Device?.dpad.left)) n -= 1;
                    if (KeyDown(Key.D) || KeyDown(Key.RightArrow) || PressedDpad(Device?.dpad.right)) n += 1;
                    return n;
                }
            }

            public int MenuTapY
            {
                get
                {
                    var n = 0;
                    if (KeyDown(Key.S) || KeyDown(Key.DownArrow) || PressedDpad(Device?.dpad.down)) n -= 1;
                    if (KeyDown(Key.W) || KeyDown(Key.UpArrow) || PressedDpad(Device?.dpad.up)) n += 1;
                    return n;
                }
            }

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
                    var x = 0f;
                    var y = 0f;
                    x = PlayPad.LiveX(RawX);
                    y = PlayPad.LiveY(RawY);
                    if (KeysEnabled)
                    {
                        x += MouseStickX;
                        y += MouseStickY;
                    }
                    if (Mathf.Abs(x) < StickPlay.Dead && Mathf.Abs(y) < StickPlay.Dead) return 0;
                    return InPlay.DiamondBag(x, y);
                }
            }

            public int ArrowBag
            {
                get
                {
                    if (!KeysEnabled) return 0;
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
                if (!KeysEnabled) return false;
                var kb = Keyboard.current;
                return kb != null && kb[k].isPressed;
            }

            bool KeyDown(Key k)
            {
                if (!KeysEnabled) return false;
                var kb = Keyboard.current;
                return kb != null && kb[k].wasPressedThisFrame;
            }

            bool KeyUp(Key k)
            {
                if (!KeysEnabled) return false;
                return Controls.KeyUp(k);
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

        public static void BeginMatch(bool versus)
        {
            var ids = ConnectedDeviceIds();
            _matchDevices = DeviceSeats.BeginMatch(
                ids,
                _p1InputMode == P1InputMode.KeyboardMouse,
                versus);
            _matchDevicesBound = true;
            CatchPlay();
        }

        public static void EndMatch()
        {
            _matchDevices = null;
            _matchDevicesBound = false;
        }

        public static LineupSeat MissingMatchSeat(Seats seats) =>
            !_matchDevicesBound || _matchDevices == null
                ? LineupSeat.Cpu
                : _matchDevices.Missing(seats, ConnectedDeviceIds());

        /// <summary>
        /// South on an unowned controller deliberately takes a missing seat. Player 1 may
        /// instead press Space/Enter/left-click to use keyboard and mouse for this match.
        /// </summary>
        public static bool TryRecoverMatchSeat(LineupSeat seat)
        {
            if (!_matchDevicesBound || _matchDevices == null || seat == LineupSeat.Cpu) return false;
            foreach (var gamepad in Gamepad.all)
            {
                if (_matchDevices.OwnsDevice(gamepad.deviceId)) continue;
                if (gamepad.buttonSouth.wasPressedThisFrame)
                    return _matchDevices.Reassign(seat, gamepad.deviceId);
            }
            if (seat == LineupSeat.Pad1
                && (RawKeyDown(Key.Space) || RawKeyDown(Key.Enter) || MouseLeftDown))
            {
                BookScheme.Observe(InputScheme.Keys);
                return _matchDevices.UseKeyboardMouse(seat);
            }
            return false;
        }

        public static Pad Of(LineupSeat seat) => seat switch
        {
            LineupSeat.Pad2 => Pad2,
            LineupSeat.Cpu => None,
            _ => Pad1
        };

        public static bool SouthDown => Pad1.SouthDown;
        public static bool SouthHeld => Pad1.SouthHeld;
        public static bool SouthUp => Pad1.SouthUp;
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
        public static bool Esc => Pad1.Esc || (Pad2.Present && Pad2.Esc);
        /// <summary>Esc opens the book. Title Start still cycles mode (#348).</summary>
        public static bool HowTo => Esc;
        public static bool Attack => Pad1.Attack;
        public static bool MouseBack => MouseRightDown;
        public static Vector2 GuiMouse
        {
            get
            {
                var m = Mouse.current;
                if (m == null) return default;
                var p = m.position.ReadValue();
                return new Vector2(p.x, Screen.height - p.y);
            }
        }
        public static int Wheel
        {
            get
            {
                var m = Mouse.current;
                if (m == null) return 0;
                var y = m.scroll.ReadValue().y;
                if (y > 0.1f) return -1;
                if (y < -0.1f) return 1;
                return 0;
            }
        }
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
        public static float MenuX => Pad1.MenuAxisX;
        public static float MenuY => Pad1.MenuAxisY;
        public static int MenuTapX => Pad1.MenuTapX;
        public static int MenuTapY => Pad1.MenuTapY;
        public static bool PointerDown => MouseLeftDown;
        public static float ScrollY
        {
            get
            {
                var m = Mouse.current;
                return m == null ? 0f : m.scroll.ReadValue().y;
            }
        }
        public static bool MenuDown => Pad1.MenuDown;
        public static bool MenuUp => Pad1.MenuUp;
        public static int ThrowBag => Pad1.ThrowBag;
        public static int StickBag => Pad1.StickBag;
        public static int ArrowBag => Pad1.ArrowBag;
        public static int AimBag => Pad1.AimBag;

        /// <summary>SET / new verb. Sitting analog and already-down WASD do not walk.</summary>
        public static void CatchPlay()
        {
            CatchPad(0);
            CatchPad(1);
            _keyA.Catch(Kb(Key.A));
            _keyD.Catch(Kb(Key.D));
            _keyW.Catch(Kb(Key.W));
            _keyS.Catch(Kb(Key.S));
            _mouseX = _mouseY = _mouseLive = 0f;
        }

        public static void Tick(float dt)
        {
            UpdateMouse(dt);
            _keyA.Tick(KeyDown(Key.A), Kb(Key.A));
            _keyD.Tick(KeyDown(Key.D), Kb(Key.D));
            _keyW.Tick(KeyDown(Key.W), Kb(Key.W));
            _keyS.Tick(KeyDown(Key.S), Kb(Key.S));
            TickPad(0, dt);
            TickPad(1, dt);
            if (_rumbleT <= 0f) return;
            _rumbleT -= dt;
            if (_rumbleT <= 0f) Silence();
            else ApplyRumble();
        }

        static void CatchPad(int index)
        {
            var s = RawStick(index);
            _pads[index].Catch(s.x, s.y);
        }

        static void TickPad(int index, float dt)
        {
            var s = RawStick(index);
            _pads[index].Tick(s.x, s.y, dt);
        }

        static Vector2 RawStick(int index)
        {
            var gamepad = DeviceForIndex(index);
            return gamepad == null ? default : gamepad.leftStick.ReadValue();
        }

        static Gamepad DeviceForIndex(int index)
        {
            if (index < 0) return null;
            if (!_matchDevicesBound)
            {
                if (index == 0 && _p1InputMode == P1InputMode.KeyboardMouse) return null;
                return index < Gamepad.all.Count ? Gamepad.all[index] : null;
            }
            if (_matchDevices == null) return null;
            var seat = index == 1 ? LineupSeat.Pad2 : LineupSeat.Pad1;
            var deviceId = _matchDevices.DeviceId(seat);
            if (!deviceId.HasValue) return null;
            foreach (var gamepad in Gamepad.all)
                if (gamepad.deviceId == deviceId.Value) return gamepad;
            return null;
        }

        static int[] ConnectedDeviceIds()
        {
            var ids = new int[Gamepad.all.Count];
            for (var i = 0; i < ids.Length; i++) ids[i] = Gamepad.all[i].deviceId;
            return ids;
        }

        static void UpdateMouse(float dt)
        {
            var m = Mouse.current;
            if (m == null)
            {
                _mouseLive = 0f;
                _mouseX = _mouseY = 0f;
                return;
            }
            var d = m.delta.ReadValue();
            var hold = m.rightButton.isPressed;
            var next = MouseStick.Tick(_mouseX, _mouseY, d.x, d.y, hold, dt);
            _mouseX = next.X;
            _mouseY = next.Y;
            _mouseLive = hold && (Mathf.Abs(_mouseX) > 0.02f || Mathf.Abs(_mouseY) > 0.02f) ? 1f : 0f;
        }

        static bool MouseLeftDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        static bool MouseLeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        static bool MouseLeftUp => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        static bool MouseRightHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;
        static bool MouseRightDown => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        static bool MouseMiddleDown => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        static float MouseStickX => _mouseLive > 0f ? _mouseX : 0f;
        static float MouseStickY => _mouseLive > 0f ? _mouseY : 0f;

        public static void RumbleContact(ContactQuality quality)
        {
            if (quality == ContactQuality.Perfect) Pulse(0.22f, 0.45f, 0.85f);
            else if (quality == ContactQuality.Nice) Pulse(0.16f, 0.32f, 0.55f);
            else if (quality == ContactQuality.Sour) Pulse(0.10f, 0.18f, 0.28f);
        }

        public static void RumbleStar() => Pulse(0.38f, 0.55f, 0.95f);

        public static void Silence()
        {
            _rumbleT = 0f;
            Pad1.Silence();
            Pad2.Silence();
        }

        /// <summary>How to play follows last input until the booklet toggle locks.</summary>
        public static void NoteInput()
        {
            if (KeyboardMouseEnabled && KeysSpoke()) BookScheme.Observe(InputScheme.Keys);
            else if (!KeyboardMouseEnabled && PadSpoke()) BookScheme.Observe(InputScheme.Pad);
        }

        static bool PadSpoke()
        {
            if (_p1InputMode == P1InputMode.KeyboardMouse) return false;
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var g = Gamepad.all[i];
                if (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame
                    || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame
                    || g.startButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame
                    || g.leftShoulder.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame
                    || g.leftStickButton.wasPressedThisFrame
                    || g.leftTrigger.ReadValue() > 0.25f
                    || g.leftStick.ReadValue().sqrMagnitude > 0.2f)
                    return true;
            }
            return false;
        }

        static bool KeysSpoke()
        {
            if (!KeyboardMouseEnabled) return false;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame || Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f
                || mouse.rightButton.isPressed))
                return true;
            var kb = Keyboard.current;
            return kb != null && kb.anyKey.wasPressedThisFrame;
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

        static bool KeyUp(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].wasReleasedThisFrame;
        }

        static bool Released(ButtonControl button) => button != null && button.wasReleasedThisFrame;

        static bool RawKeyDown(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].wasPressedThisFrame;
        }
    }
}
