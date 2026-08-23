namespace GrandSluggers.Sim;

/// <summary>
/// Scorebug is the product. Mute it while a special or smash owns the picture.
/// Title, select, lineup, and final still draw.
/// </summary>
public static class BroadcastHud
{
    public static bool MutePlay(bool spectacleActive, double smashSeconds, double freezeSeconds = 0)
        => spectacleActive || smashSeconds > 0 || freezeSeconds > 0;
}
