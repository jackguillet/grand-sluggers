namespace GrandSluggers.Sim;

/// <summary>
/// The bodies a throw brings into the play besides its thrower and receiver (§8.7): the cutoff on the line, where it stands, and
/// the backup behind the bag. <see cref="LivePlaySystem"/> picks them when it throws, walks them to their spots, and keeps the
/// cover walk off them.
/// </summary>
public sealed class ThrowSupport
{
    /// <summary>The cutoff's position, or empty when the throw has none.</summary>
    public string CutoffPos { get; private set; } = "";

    /// <summary>Where the cutoff stands on the line, while there is one.</summary>
    public (double X, double Z)? CutoffSpot { get; private set; }

    /// <summary>The backup's position, or empty.</summary>
    public string BackupPos { get; private set; } = "";

    /// <summary>Where the backup stands behind the bag.</summary>
    public (double X, double Z) BackupSpot { get; private set; }

    public void Reset()
    {
        CutoffPos = "";
        CutoffSpot = null;
        BackupPos = "";
        BackupSpot = (0, 0);
    }

    /// <summary>A throw to the cutoff at <paramref name="pos"/>, who stands at <paramref name="spot"/> on the line.</summary>
    public void Cutoff(string pos, (double X, double Z) spot)
    {
        CutoffPos = pos;
        CutoffSpot = spot;
    }

    /// <summary>The cutoff has the ball, or the throw went elsewhere: no cutoff.</summary>
    public void ClearCutoff()
    {
        CutoffPos = "";
        CutoffSpot = null;
    }

    /// <summary>A throw to a bag, backed up by <paramref name="pos"/> at <paramref name="spot"/>.</summary>
    public void Backup(string pos, (double X, double Z) spot)
    {
        BackupSpot = spot;
        BackupPos = pos;
    }

    /// <summary>The body at <paramref name="pos"/> is the cutoff or the backup: the cover walk leaves it alone.</summary>
    public bool Supports(string pos) => pos == CutoffPos || pos == BackupPos;
}
