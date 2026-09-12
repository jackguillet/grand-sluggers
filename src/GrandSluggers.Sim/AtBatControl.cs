namespace GrandSluggers.Sim;

/// <summary>
/// Maps a player's horizontal screen intent into the baseball world's X axis.
/// The mound camera looks toward home and the plate camera looks toward the mound,
/// so the same world direction appears on opposite sides of those two shots.
/// </summary>
public static class AtBatControl
{
    public static double WorldHorizontal(double screenHorizontal, CameraShot shot)
    {
        var lookX = shot.Target.X - shot.Pos.X;
        var lookZ = shot.Target.Z - shot.Pos.Z;
        var lookLength = Math.Sqrt(lookX * lookX + lookZ * lookZ);
        if (lookLength < 1e-6) return screenHorizontal;

        // Unity's screen-right vector for a level camera is (forward.z, 0, -forward.x).
        // We only move on world X, so its sign is the sign of forward.z.
        var worldSignForScreenRight = Math.Sign(lookZ / lookLength);
        return screenHorizontal * (worldSignForScreenRight == 0 ? 1 : worldSignForScreenRight);
    }
}
