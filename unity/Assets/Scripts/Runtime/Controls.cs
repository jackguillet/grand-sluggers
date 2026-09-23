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

            /// <summary>Which bound device this pad is (#718): the sim keeps a pursuit-stick calibration and arming per device.</summary>
            public int Index => _index;

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

            /// <summary>
            /// The held special modifier (spec §12, PH-16-R10, R11, R17): LB on either seat's own pad, Q for player 1's
            /// keys. A finger button, never a thumb (the right thumb works South). Read at the accepted release of the
            /// pitch or the swing (<see cref="StarModifier.Release"/>); a hold that asked for a special is spent for LB's
            /// other verbs until it comes up (<see cref="StarModifier.IsFree"/>).
            /// </summary>
            public bool StarHeld => Kb(Key.Q) || Held(Device?.leftShoulder);
            public bool EastDown => KeyDown(Key.G) || Pressed(Device?.buttonEast);
            public bool EastHeld => Kb(Key.G) || Held(Device?.buttonEast);
            public bool WestDown => KeyDown(Key.F) || Pressed(Device?.buttonWest);
            public bool Attack => NorthDown || KeyDown(Key.B);

            /// <summary>
            /// The held bunt's two sides at the plate (spec §5.8, PH-14-R5): LT / J toward third, RT / L toward
            /// first. The trigger counts at the Input System's press point. Each seat reads only its own pad; the
            /// keys belong to player 1. A trigger held for a bunt at contact is spent (PH-14-R6): every other
            /// reader of LT / RT / J / L asks <see cref="BuntHold.IsFree"/> first.
            /// </summary>
            public bool BuntThirdDown => KeyDown(Key.J) || Pressed(Device?.leftTrigger);
            public bool BuntThirdHeld => Kb(Key.J) || Held(Device?.leftTrigger);
            public bool BuntFirstDown => KeyDown(Key.L) || Pressed(Device?.rightTrigger);
            public bool BuntFirstHeld => Kb(Key.L) || Held(Device?.rightTrigger);

            /// <summary>
            /// The explicit swing cancel (spec §5.1, PH-13-R1): East / G, the same button as the dive and the
            /// Training skip. A press the plate took is spent until it comes up
            /// (<see cref="PlateButtons.CancelIsFree"/>).
            /// </summary>
            public bool CancelDown => EastDown;
            public bool CancelHeld => EastHeld;

            /// <summary>The plate's buttons on this frame, for <see cref="PlateButtons.Advance"/>.</summary>
            public PlateInput Plate => new(SouthDown, SouthHeld, SouthUp,
                BuntThirdDown, BuntThirdHeld, BuntFirstDown, BuntFirstHeld, CancelDown, CancelHeld);

            public float Charge01 => ChargeWith(true);

            /// <summary>
            /// The item modifier (LT, Shift, right mouse). <paramref name="triggerFree"/> is false while LT is still
            /// down from a bunt that made contact (PH-14-R6): that hold is no modifier until it comes up.
            /// </summary>
            public float ChargeWith(bool triggerFree)
            {
                var v = Kb(Key.LeftShift) || (KeysEnabled && MouseRightHeld) ? 1f : 0f;
                var pad = Device;
                if (pad != null && triggerFree) v = Mathf.Max(v, pad.leftTrigger.ReadValue());
                return Mathf.Clamp01(v);
            }

            public bool Charge => Charge01 >= ChargePull;
            /// <summary>
            /// The mound's pre-charge family cycle in SET (spec §3, §4.1; PH-02-R3/R4/R5). A press
            /// edge: one press is one advance. West is no longer a pitching modifier (#825), and no
            /// longer the bunt either: the triggers hold it (PH-14-R5).
            /// </summary>
            public bool CyclePitch => KeyDown(Key.Tab) || Pressed(Device?.rightShoulder);
            public bool Skip => EastDown;
            public bool Start => KeyDown(Key.H) || Pressed(Device?.startButton);
            public bool Esc => KeyDown(Key.Escape);
            public bool AllAdvance => AllAdvanceWith(true);
            /// <summary>The LB / comma press edge. The menus read it (1 PLAYER, the batting order); play does not.</summary>
            public bool AllAdvanceDown => KeyDown(Key.Comma) || Pressed(Device?.leftShoulder);
            public bool AllReturn => Kb(Key.Period) || Held(Device?.rightShoulder);
            public bool FreezeRunners => FreezeRunnersWith(true);
            public bool Steal => KeyDown(Key.Z) || Pressed(Device?.leftStickButton);
            public bool Cutoff => CutoffWith(true);

            /// <summary>
            /// LB's live-ball verbs with the special modifier guarded (PH-16-R10, R17): <paramref name="lbFree"/> is false
            /// while LB is still down from a release that asked for a special, so that hold is no all-advance, no cutoff
            /// and no half of the halt until it comes up. The keys (comma, X, slash) are not the modifier.
            /// </summary>
            public bool AllAdvanceWith(bool lbFree) => Kb(Key.Comma) || (lbFree && Held(Device?.leftShoulder));

            /// <inheritdoc cref="AllAdvanceWith"/>
            public bool FreezeRunnersWith(bool lbFree) =>
                Kb(Key.Slash) || (lbFree && Held(Device?.leftShoulder) && Held(Device?.rightShoulder));

            /// <inheritdoc cref="AllAdvanceWith"/>
            public bool CutoffWith(bool lbFree) => Kb(Key.X) || (lbFree && Held(Device?.leftShoulder));
            public bool Item => ItemWith(true);
            public bool ItemConfirm => ItemConfirmWith(true);

            /// <summary>The item throw with the LT modifier guarded (PH-14-R6; see <see cref="ChargeWith"/>).</summary>
            public bool ItemWith(bool triggerFree) =>
                KeyDown(Key.E) || (ChargeWith(triggerFree) >= ChargePull && Pressed(Device?.rightShoulder));

            public bool ItemConfirmWith(bool triggerFree) =>
                ItemWith(triggerFree) || (SouthDown && ChargeWith(triggerFree) >= ChargePull);
            public bool SwapPitcher => KeyDown(Key.R) || Pressed(Device?.selectButton);
            public bool NightToggle => KeyDown(Key.N) || Pressed(Device?.rightStickButton);
            /// <summary>Title and field: hazards on / off (FD-10), the match option.</summary>
            public bool HazardsToggle => KeyDown(Key.R) || Pressed(Device?.selectButton);
            /// <summary>Title: the difficulty rung next to the innings (easy / normal / hard).</summary>
            public bool CycleDifficulty => KeyDown(Key.X) || Pressed(Device?.leftShoulder);

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

            /// <summary>
            /// The pursuit stick (#718): the controller's normalized device coordinate read before any dead zone — Unity's stick
            /// processor and StickPlay's .32 both stand aside — plus WASD and player 1's mouse stick, each axis capped at 1. Only
            /// the calibrated radial stick reads it (the sim centres, arms, gates and remaps it once); every other verb keeps
            /// <see cref="StickX"/> / <see cref="StickY"/> and their protections. A held key counts from the frame it is down:
            /// the sim's arming, not a catch at SET, is what keeps an old direction from steering a new half.
            /// </summary>
            public float PursuitX => Pursuit.x;

            /// <inheritdoc cref="PursuitX"/>
            public float PursuitY => Pursuit.y;

            Vector2 Pursuit
            {
                get
                {
                    var pad = Device;
                    var v = pad == null ? Vector2.zero : pad.leftStick.ReadUnprocessedValue();
                    if (KeysEnabled)
                    {
                        v.x += (Kb(Key.D) ? 1f : 0f) - (Kb(Key.A) ? 1f : 0f) + MouseStickX;
                        v.y += (Kb(Key.W) ? 1f : 0f) - (Kb(Key.S) ? 1f : 0f) + MouseStickY;
                    }
                    return new Vector2(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f));
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

        /// <summary>
        /// The controller seated at pad <paramref name="index"/> for this match (#718: the pursuit stick's device identity), or
        /// null when that seat is keyboard and mouse or empty. Before the match binds, the connected pad at that index.
        /// </summary>
        public static int? SeatDeviceId(int index)
        {
            if (index < 0) return null;
            if (!_matchDevicesBound) return DeviceForIndex(index)?.deviceId;
            return _matchDevices?.DeviceId(index == 1 ? LineupSeat.Pad2 : LineupSeat.Pad1);
        }

        /// <summary>Player 1 plays on keyboard and mouse (#718: a digital stick, the identity calibration).</summary>
        public static bool SeatUsesKeyboard(int index) => index == 0 && KeyboardMouseEnabled;

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
        public static float Charge01 => Pad1.Charge01;
        public static bool Charge => Pad1.Charge;
        public static bool CyclePitch => Pad1.CyclePitch;
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
        public static bool HazardsToggle => Pad1.HazardsToggle;
        public static bool CycleDifficulty => Pad1.CycleDifficulty;
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
                    || g.leftTrigger.ReadValue() > 0.25f || g.rightTrigger.ReadValue() > 0.25f
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
