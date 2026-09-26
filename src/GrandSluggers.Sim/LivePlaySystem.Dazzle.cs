namespace GrandSluggers.Sim;

/// <summary>
/// A star swing's pause (spec §13, <see cref="StarSwingSkill.FielderPauseSec"/>): the one body the preview named
/// (<see cref="FieldingPreview.Dazzled"/>, the nearest fielder) stands still for the pause from the contact — a human glove
/// and a CPU body alike — and every other body plays on at its own speed. The pause is that body's wait
/// (<see cref="FieldingResolver.Dazzle"/>), so the stick, the chase, the cover walk and the throw all wait it out; the ball,
/// the gloves and the throws still decide the play. Nothing is rolled.
/// </summary>
public sealed partial class LivePlaySystem
{
    /// <summary>Holds the preview's dazzled body for its pause, on both wait tables, uncapped by the ball's hang.</summary>
    void BeginDazzle()
    {
        if (Preview is not { DazzleSec: > 0 } pre || string.IsNullOrEmpty(pre.Dazzled) || Hit is null) return;
        var pos = pre.Dazzled;
        _readyAt[pos] = FieldingResolver.Dazzle(_readyAt.TryGetValue(pos, out var cpu) ? cpu : 0, pre.DazzleSec);
        _readyHuman[pos] = FieldingResolver.Dazzle(_readyHuman.TryGetValue(pos, out var human) ? human : 0, pre.DazzleSec);
        RecordFact(new FielderDazzled(Hit.StarSwingUsed ?? "", pos, pre.DazzleSec));
    }

    /// <summary>The body a star swing's pause holds this play (§13), or empty.</summary>
    public string DazzledPos => Preview is { DazzleSec: > 0 } pre ? pre.Dazzled : "";

    /// <summary>
    /// The body at <paramref name="pos"/> is inside the pause (§13): it takes no step — no chase, no stick, no cover walk —
    /// until <see cref="FieldingPreview.DazzleSec"/> from the contact. Its wait on the tables (<see cref="BeginDazzle"/>)
    /// keeps every read of "may it move" in agreement.
    /// </summary>
    bool Dazzled(string pos) =>
        Preview is { DazzleSec: > 0 } pre && ElapsedSeconds + 1e-9 < pre.DazzleSec
        && string.Equals(pos, pre.Dazzled, StringComparison.OrdinalIgnoreCase);
}
