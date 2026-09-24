using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GrandSluggers.UnityClient
{
    /// <summary>Controller-only couch input. Device identity persists through setup, match, and recovery.</summary>
    public static class Controls
    {
        static DeviceSeats _devices = new(null, null);
        static readonly ControllerInput[] _input = { new(), new() };
        static readonly StickPlay.Pad[] _pads = new StickPlay.Pad[2];
        static float _rumbleT, _rumbleLow, _rumbleHigh;
        public enum P1InputMode { Controller }
        public static P1InputMode Player1InputMode => P1InputMode.Controller;
        public static string Player1InputLabel => "Controller";
        public static bool Player1InputToggleDown => false;
        public static void CyclePlayer1Input() { }
        public static void Initialize() { _devices = new(null, null); SeatNewDevices(); CatchPlay(); }
        public static void BeginMatch(bool versus) { SeatNewDevices(); CatchPlay(); }
        public static void EndMatch() { CatchPlay(); }
        public static Pad Pad1 { get; } = new(0, false);
        public static Pad Pad2 { get; } = new(1, false);
        public static Pad None { get; } = new(-1, false);
        public static int PadCount => (Pad1.Present ? 1 : 0) + (Pad2.Present ? 1 : 0);
        public static Pad Of(LineupSeat seat) => seat == LineupSeat.Pad2 ? Pad2 : seat == LineupSeat.Cpu ? None : Pad1;
        public static int? SeatDeviceId(int index) => index < 0 ? null : _devices.DeviceId(index == 1 ? LineupSeat.Pad2 : LineupSeat.Pad1);
        public static bool SeatUsesKeyboard(int index) => false;
        public static LineupSeat MissingMatchSeat(Seats seats) => _devices.Missing(seats, ConnectedDeviceIds());
        public static bool TryRecoverMatchSeat(LineupSeat seat)
        {
            foreach (var g in Gamepad.all)
                if (!_devices.OwnsDevice(g.deviceId) && g.buttonSouth.wasPressedThisFrame)
                { _devices.Reassign(seat, g.deviceId); CatchPlay(); return true; }
            return false;
        }
        static void SeatNewDevices()
        {
            foreach (var g in Gamepad.all)
            {
                if (_devices.OwnsDevice(g.deviceId)) continue;
                if (!_devices.Pad1DeviceId.HasValue) _devices.Reassign(LineupSeat.Pad1, g.deviceId);
                else if (Pad1.Present && !_devices.Pad2DeviceId.HasValue) _devices.Reassign(LineupSeat.Pad2, g.deviceId);
            }
        }
        static Gamepad DeviceForIndex(int index)
        {
            var id = SeatDeviceId(index);
            foreach (var g in Gamepad.all) if (g.deviceId == id) return g;
            return null;
        }
        static int[] ConnectedDeviceIds()
        {
            var ids = new int[Gamepad.all.Count];
            for (var i = 0; i < ids.Length; i++) ids[i] = Gamepad.all[i].deviceId;
            return ids;
        }
        public readonly struct Pad
        {
            readonly int _index;
            public int Index => _index;
            public Pad(int index, bool keys) { _index = index; }
            Gamepad Device => DeviceForIndex(_index);
            ControllerInput Input => _index < 0 ? null : _input[_index];
            bool Down(ControllerButton b) => Input?.IsDown(b) == true;
            bool Held(ControllerButton b) => Input?.IsHeld(b) == true;
            bool Up(ControllerButton b) => Input?.IsUp(b) == true;
            public bool Present => Device != null;
            public bool SouthDown => Down(ControllerButton.South);
            public bool SouthHeld => Held(ControllerButton.South);
            public bool SouthUp => Up(ControllerButton.South);
            public bool EastDown => Down(ControllerButton.East);
            public bool EastHeld => Held(ControllerButton.East);
            public bool WestDown => Down(ControllerButton.West);
            public bool NorthDown => Down(ControllerButton.North);
            public bool BallDown => Down(ControllerLayout.Ball);
            public bool BallHeld => Held(ControllerLayout.Ball);
            public bool BallUp => Up(ControllerLayout.Ball);
            public bool StarHeld => Held(ControllerLayout.Star);
            public bool JumpDown => Down(ControllerLayout.Jump);
            public bool Attack => Down(ControllerLayout.Attack);
            public bool BuntThirdDown => Down(ControllerLayout.BuntThird);
            public bool BuntThirdHeld => Held(ControllerLayout.BuntThird);
            public bool BuntFirstDown => Down(ControllerLayout.BuntFirst);
            public bool BuntFirstHeld => Held(ControllerLayout.BuntFirst);
            public bool CancelDown => EastDown;
            public bool CancelHeld => EastHeld;
            public PlateInput Plate => new(BallDown, BallHeld, BallUp, BuntThirdDown, BuntThirdHeld,
                BuntFirstDown, BuntFirstHeld, CancelDown, CancelHeld);
            public bool CyclePitch => Down(ControllerLayout.PitchFamily);
            public bool PagePrevious => Down(ControllerButton.LB);
            public bool PageNext => Down(ControllerButton.RB);
            public bool Start => Down(ControllerButton.Start);
            public bool Esc => Down(ControllerButton.View);
            public bool Skip => EastDown;
            public bool AllAdvance => Held(ControllerLayout.Advance);
            public bool AllAdvanceDown => Down(ControllerLayout.Advance);
            public bool AllReturn => Held(ControllerLayout.Return);
            public bool FreezeRunners => Down(ControllerLayout.Halt) || AllAdvance && AllReturn;
            public bool Steal => AllAdvanceDown;
            public bool Cutoff => Down(ControllerLayout.Relay);
            public bool SwapPitcher => Down(ControllerLayout.Switch);
            public bool AllAdvanceWith(bool free) => AllAdvance;
            public bool FreezeRunnersWith(bool free) => FreezeRunners;
            public bool CutoffWith(bool free) => Cutoff;
            public RunnerOrderInput RunnerOrders => new(Input?.TargetFlick ?? 0,
                Down(ControllerLayout.All), AllAdvance, AllReturn, FreezeRunners);
            public int ThrowBag => Input?.ThrowTarget ?? 0;
            public int PickoffBag => Input?.TargetHeld ?? 0;
            public int StickBag => 0;
            public int ArrowBag => 0;
            public int AimBag => ThrowBag;
            public void ClearThrowTarget() => Input?.ClearTarget();
            public int ItemCycle => MenuTapX;
            public bool Item => NorthDown;
            public bool ItemConfirm => NorthDown;
            public bool ItemWith(bool free) => free && NorthDown;
            public bool ItemConfirmWith(bool free) => free && NorthDown;
            public float Charge01 => 0;
            public bool Charge => false;
            public float ChargeWith(bool free) => 0;
            public bool NightToggle => false;
            public bool HazardsToggle => false;
            public bool CycleDifficulty => false;
            public float StickX => _index < 0 ? 0 : _pads[_index].LiveX(Device?.leftStick.x.ReadValue() ?? 0);
            public float StickY => _index < 0 ? 0 : _pads[_index].LiveY(Device?.leftStick.y.ReadValue() ?? 0);
            public float PursuitX => Device?.leftStick.ReadUnprocessedValue().x ?? 0;
            public float PursuitY => Device?.leftStick.ReadUnprocessedValue().y ?? 0;
            public bool MenuDown => Down(ControllerButton.Down);
            public bool MenuUp => Down(ControllerButton.Up);
            public int MenuTapX => (Down(ControllerButton.Right) ? 1 : 0) - (Down(ControllerButton.Left) ? 1 : 0);
            public int MenuTapY => (MenuUp ? 1 : 0) - (MenuDown ? 1 : 0);
            public float MenuAxisX => Mathf.Clamp(StickX + (Held(ControllerButton.Right) ? 1 : 0) - (Held(ControllerButton.Left) ? 1 : 0), -1, 1);
            public float MenuAxisY => Mathf.Clamp(StickY + (Held(ControllerButton.Up) ? 1 : 0) - (Held(ControllerButton.Down) ? 1 : 0), -1, 1);
            public void Rumble(float low, float high) => Device?.SetMotorSpeeds(low, high);
            public void Silence() => Device?.ResetHaptics();
        }
        public static bool SouthDown => Pad1.SouthDown;
        public static bool SouthHeld => Pad1.SouthHeld;
        public static bool SouthUp => Pad1.SouthUp;
        public static bool NorthDown => Pad1.NorthDown;
        public static bool EastDown => Pad1.EastDown;
        public static bool EastHeld => Pad1.EastHeld;
        public static bool WestDown => Pad1.WestDown;
        public static float Charge01 => 0;
        public static bool Charge => false;
        public static bool CyclePitch => Pad1.CyclePitch;
        public static bool Skip => Pad1.Skip;
        public static bool CallTime => Pad1.Start || Pad2.Start;
        public static bool Esc => Pad1.Esc || Pad2.Esc;
        public static bool HowTo => Esc;
        public static bool Attack => Pad1.Attack;
        public static bool AllAdvance => Pad1.AllAdvance;
        public static bool AllAdvanceDown => Pad1.AllAdvanceDown;
        public static bool AllReturn => Pad1.AllReturn;
        public static bool FreezeRunners => Pad1.FreezeRunners;
        public static bool Steal => Pad1.Steal;
        public static bool Cutoff => Pad1.Cutoff;
        public static bool Item => false;
        public static bool ItemConfirm => false;
        public static bool Start => Pad1.Start;
        public static bool SwapPitcher => Pad1.SwapPitcher;
        public static bool NightToggle => false;
        public static bool HazardsToggle => false;
        public static bool CycleDifficulty => false;
        public static bool ParkHeld => false;
        public static float StickX => Pad1.StickX;
        public static float StickY => Pad1.StickY;
        public static float MenuX => Pad1.MenuAxisX;
        public static float MenuY => Pad1.MenuAxisY;
        public static int MenuTapX => Pad1.MenuTapX;
        public static int MenuTapY => Pad1.MenuTapY;
        public static bool MenuDown => Pad1.MenuDown;
        public static bool MenuUp => Pad1.MenuUp;
        public static int ThrowBag => Pad1.ThrowBag;
        public static int StickBag => 0;
        public static int ArrowBag => 0;
        public static int AimBag => Pad1.AimBag;
        // Compatibility for old layout hit-testing: the product has no pointer input.
        public static bool PointerDown => false;
        public static bool MouseBack => false;
        public static Vector2 GuiMouse => new(-10000, -10000);
        public static int Wheel => 0;
        public static float ScrollY => 0;
        // Developer diagnostics never enter a player command.
        static bool DebugKey(Key key) => Application.isEditor && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static bool TimingAid => DebugKey(Key.F1);
        public static bool FeelDebug => DebugKey(Key.F2);
        public static bool HudMute => DebugKey(Key.F3);
        public static bool SlowMo => DebugKey(Key.LeftBracket);
        public static bool FreezeCam => DebugKey(Key.RightBracket);
        public static void NoteInput() => BookScheme.Select(InputScheme.Pad);
        public static void ClearTargets() { foreach (var input in _input) input.ClearTarget(); }
        public static void CatchPlay()
        {
            for (var i = 0; i < 2; i++)
            {
                var g = DeviceForIndex(i);
                var s = g?.leftStick.ReadValue() ?? Vector2.zero;
                _pads[i].Catch(s.x, s.y);
                _input[i].Catch();
            }
        }
        public static void Tick(float dt)
        {
            SeatNewDevices();
            for (var i = 0; i < 2; i++)
            {
                var g = DeviceForIndex(i);
                var b = ControllerButton.None;
                void Read(ButtonControl key, ControllerButton flag) { if (key != null && key.isPressed) b |= flag; }
                Read(g?.buttonSouth, ControllerButton.South); Read(g?.buttonEast, ControllerButton.East);
                Read(g?.buttonWest, ControllerButton.West); Read(g?.buttonNorth, ControllerButton.North);
                Read(g?.leftShoulder, ControllerButton.LB); Read(g?.rightShoulder, ControllerButton.RB);
                Read(g?.startButton, ControllerButton.Start); Read(g?.selectButton, ControllerButton.View);
                Read(g?.dpad.up, ControllerButton.Up); Read(g?.dpad.down, ControllerButton.Down);
                Read(g?.dpad.left, ControllerButton.Left); Read(g?.dpad.right, ControllerButton.Right);
                var right = g?.rightStick.ReadValue() ?? Vector2.zero;
                _input[i].Tick(b, g?.rightTrigger.ReadValue() ?? 0, g?.leftTrigger.ReadValue() ?? 0, right.x, right.y);
                var left = g?.leftStick.ReadValue() ?? Vector2.zero;
                _pads[i].Tick(left.x, left.y, dt);
            }
            if (_rumbleT <= 0) return;
            _rumbleT -= dt;
            if (_rumbleT <= 0) Silence(); else ApplyRumble();
        }
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

    }
}
