using Raylib_cs;

namespace GrandSluggers.Play;

public readonly record struct FrameInput(
    bool ConfirmPressed,
    bool ConfirmDown,
    bool Charge,
    bool StarPressed,
    bool CyclePitch,
    float Spray,
    float MoveX,
    float MoveZ,
    int ThrowBase,
    bool Swap,
    bool Jump,
    bool TogglePark,
    bool ToggleTwoPlayer,
    bool Quit);

public static class PlayerInput
{
    public static FrameInput ReadP1()
    {
        var pad = Raylib.IsGamepadAvailable(0);
        var mx = 0f;
        var mz = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) mx -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) mx += 1;
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) mz += 1;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) mz -= 1;
        if (pad)
        {
            mx += Raylib.GetGamepadAxisMovement(0, GamepadAxis.LeftX);
            mz -= Raylib.GetGamepadAxisMovement(0, GamepadAxis.LeftY);
        }
        var throwBase = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.One) || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceLeft))) throwBase = 1;
        if (Raylib.IsKeyPressed(KeyboardKey.Two) || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceUp))) throwBase = 2;
        if (Raylib.IsKeyPressed(KeyboardKey.Three) || (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceRight))) throwBase = 3;
        if (Raylib.IsKeyPressed(KeyboardKey.Four) || Raylib.IsKeyPressed(KeyboardKey.H) ||
            (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceDown))) throwBase = 4;

        return new FrameInput(
            ConfirmPressed: Raylib.IsKeyPressed(KeyboardKey.Space) ||
                            (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown)),
            ConfirmDown: Raylib.IsKeyDown(KeyboardKey.Space) ||
                         (pad && Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceDown)),
            Charge: Raylib.IsKeyDown(KeyboardKey.LeftShift) ||
                    (pad && Raylib.IsGamepadButtonDown(0, GamepadButton.LeftTrigger2)),
            StarPressed: Raylib.IsKeyPressed(KeyboardKey.Q) ||
                         (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceUp)),
            CyclePitch: Raylib.IsKeyPressed(KeyboardKey.Tab) ||
                        (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightTrigger1)),
            Spray: Math.Clamp(mx, -1, 1),
            MoveX: Math.Clamp(mx, -1, 1),
            MoveZ: Math.Clamp(mz, -1, 1),
            ThrowBase: throwBase,
            Swap: Raylib.IsKeyPressed(KeyboardKey.R) ||
                  (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightThumb)),
            Jump: Raylib.IsKeyPressed(KeyboardKey.F) ||
                  (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceRight)),
            TogglePark: Raylib.IsKeyPressed(KeyboardKey.C),
            ToggleTwoPlayer: Raylib.IsKeyPressed(KeyboardKey.T),
            Quit: Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.WindowShouldClose());
    }

    public static FrameInput ReadP2()
    {
        var pad = Raylib.IsGamepadAvailable(1);
        var mx = 0f;
        var mz = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.J)) mx -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.L)) mx += 1;
        if (Raylib.IsKeyDown(KeyboardKey.I)) mz += 1;
        if (Raylib.IsKeyDown(KeyboardKey.K)) mz -= 1;
        if (pad)
        {
            mx += Raylib.GetGamepadAxisMovement(1, GamepadAxis.LeftX);
            mz -= Raylib.GetGamepadAxisMovement(1, GamepadAxis.LeftY);
        }
        var throwBase = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.Kp1) || (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.LeftFaceLeft))) throwBase = 1;
        if (Raylib.IsKeyPressed(KeyboardKey.Kp2) || (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.LeftFaceUp))) throwBase = 2;
        if (Raylib.IsKeyPressed(KeyboardKey.Kp3) || (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.LeftFaceRight))) throwBase = 3;
        if (Raylib.IsKeyPressed(KeyboardKey.Kp0) || (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.LeftFaceDown))) throwBase = 4;

        return new FrameInput(
            ConfirmPressed: Raylib.IsKeyPressed(KeyboardKey.Enter) ||
                            (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.RightFaceDown)),
            ConfirmDown: Raylib.IsKeyDown(KeyboardKey.Enter) ||
                         (pad && Raylib.IsGamepadButtonDown(1, GamepadButton.RightFaceDown)),
            Charge: Raylib.IsKeyDown(KeyboardKey.RightShift) ||
                    (pad && Raylib.IsGamepadButtonDown(1, GamepadButton.LeftTrigger2)),
            StarPressed: Raylib.IsKeyPressed(KeyboardKey.P) ||
                         (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.RightFaceUp)),
            CyclePitch: Raylib.IsKeyPressed(KeyboardKey.RightBracket) ||
                        (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.RightTrigger1)),
            Spray: Math.Clamp(mx, -1, 1),
            MoveX: Math.Clamp(mx, -1, 1),
            MoveZ: Math.Clamp(mz, -1, 1),
            ThrowBase: throwBase,
            Swap: Raylib.IsKeyPressed(KeyboardKey.Apostrophe) ||
                  (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.RightThumb)),
            Jump: Raylib.IsKeyPressed(KeyboardKey.Semicolon) ||
                  (pad && Raylib.IsGamepadButtonPressed(1, GamepadButton.RightFaceRight)),
            TogglePark: false,
            ToggleTwoPlayer: false,
            Quit: false);
    }
}
