using Raylib_cs;

namespace GrandSluggers.Play;

public readonly record struct FrameInput(
    bool ConfirmPressed,
    bool ConfirmDown,
    bool Charge,
    bool StarPressed,
    bool CyclePitch,
    float Spray,
    bool Quit);

public static class PlayerInput
{
    public static FrameInput Read()
    {
        var pad = Raylib.IsGamepadAvailable(0);
        var confirmPressed =
            Raylib.IsKeyPressed(KeyboardKey.Space) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter) ||
            (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown));
        var confirmDown =
            Raylib.IsKeyDown(KeyboardKey.Space) ||
            (pad && Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceDown));
        var charge =
            Raylib.IsKeyDown(KeyboardKey.LeftShift) ||
            Raylib.IsKeyDown(KeyboardKey.RightShift) ||
            (pad && (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftTrigger2) ||
                     Raylib.IsGamepadButtonDown(0, GamepadButton.RightTrigger2)));
        var star =
            Raylib.IsKeyPressed(KeyboardKey.Q) ||
            Raylib.IsKeyPressed(KeyboardKey.F) ||
            (pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceUp));
        var cycle =
            Raylib.IsKeyPressed(KeyboardKey.Tab) ||
            Raylib.IsKeyPressed(KeyboardKey.One) ||
            Raylib.IsKeyPressed(KeyboardKey.Two) ||
            Raylib.IsKeyPressed(KeyboardKey.Three) ||
            (pad && (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceRight) ||
                     Raylib.IsGamepadButtonPressed(0, GamepadButton.RightTrigger1)));
        var spray = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) spray -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) spray += 1;
        if (pad)
            spray += Raylib.GetGamepadAxisMovement(0, GamepadAxis.LeftX);
        var quit = Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.WindowShouldClose();
        return new FrameInput(confirmPressed, confirmDown, charge, star, cycle, Math.Clamp(spray, -1, 1), quit);
    }
}
