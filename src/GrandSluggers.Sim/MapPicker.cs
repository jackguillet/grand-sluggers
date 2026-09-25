namespace GrandSluggers.Sim;

/// <summary>
/// The continent map's cursor (WD-17 A): opened on the stadium row with the park being played, moved pin to pin by the stick
/// (<see cref="WorldMap.Step"/>), closed by South on the park under the cursor or by East on the park it opened with. Every
/// move is a park the setup plays at once, so the postcard behind the map follows the cursor.
/// </summary>
public sealed class MapPicker
{
    /// <summary>True while the map is up.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The park the map opened on: East puts it back.</summary>
    public string From { get; private set; } = "";

    /// <summary>The park under the cursor.</summary>
    public string Park { get; private set; } = "";

    public void Open(string park)
    {
        IsOpen = true;
        From = Park = park;
    }

    /// <summary>
    /// Moves the cursor by a stick step (<paramref name="dx"/> right, <paramref name="up"/> up on the pad) and returns the
    /// park under it when it moved, else null. Up on the pad is north on the map.
    /// </summary>
    public string? Move(WorldMap world, int dx, int up)
    {
        if (!IsOpen || (dx == 0 && up == 0)) return null;
        var next = world.Step(Park, dx, -up);
        if (next.Equals(Park, StringComparison.OrdinalIgnoreCase)) return null;
        Park = next;
        return next;
    }

    /// <summary>South: play the park under the cursor.</summary>
    public string Confirm()
    {
        IsOpen = false;
        return Park;
    }

    /// <summary>East: keep the park the map opened on.</summary>
    public string Cancel()
    {
        IsOpen = false;
        Park = From;
        return From;
    }
}
