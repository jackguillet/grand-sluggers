namespace GrandSluggers.Sim;

/// <summary>A captain's star pitch (data/abilities/star-skills.json, spec §13): the numbers the sim reads at runtime.</summary>
public sealed record StarPitchSkill(
    string Id,
    string Name,
    string Kind,
    double SpeedMul,
    int StaminaCost,
    double BatterWindowMul,
    bool LateBreak,
    bool Decoy,
    string? OnCatch);

/// <summary>A captain's star swing (data/abilities/star-skills.json, spec §13).</summary>
public sealed record StarSwingSkill(
    string Id,
    string Name,
    string Kind,
    double ExitVeloMul,
    double? LaunchDeg,
    string? Terrain,
    double FielderPauseSec,
    bool InfieldChaos,
    bool Decoy,
    bool Fragments);

/// <summary>
/// The star skills as loaded from JSON. The JSON is the only copy (spec §13): no C# switch may
/// re-type a multiplier. Callers with a <see cref="ContentCatalog"/> use its table; a caller
/// without one falls back to the table found from the data root, like <see cref="Rules"/>.
/// </summary>
public sealed class StarSkillTable
{
    public const string Path = "abilities/star-skills.json";

    public StarSkillTable(IReadOnlyDictionary<string, StarPitchSkill> pitches, IReadOnlyDictionary<string, StarSwingSkill> swings)
    {
        Pitches = pitches;
        Swings = swings;
    }

    public IReadOnlyDictionary<string, StarPitchSkill> Pitches { get; }
    public IReadOnlyDictionary<string, StarSwingSkill> Swings { get; }

    public StarPitchSkill? Pitch(string? id) =>
        !string.IsNullOrEmpty(id) && Pitches.TryGetValue(id, out var p) ? p : null;

    public StarSwingSkill? Swing(string? id) =>
        !string.IsNullOrEmpty(id) && Swings.TryGetValue(id, out var s) ? s : null;

    /// <summary>An empty table: every lookup is the identity. Only for callers that own no data at all.</summary>
    public static StarSkillTable Empty { get; } = new(
        new Dictionary<string, StarPitchSkill>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, StarSwingSkill>(StringComparer.OrdinalIgnoreCase));

    static StarSkillTable? _default;

    /// <summary>The table found from the data root, else empty.</summary>
    public static StarSkillTable Default => _default ??= LoadDefault();

    public static StarSkillTable Or(StarSkillTable? table) => table ?? Default;

    static StarSkillTable LoadDefault()
    {
        try
        {
            var root = ContentCatalog.TryFindDataRoot();
            return root is null ? Empty : ContentCatalog.Load(root).StarSkills;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Empty;
        }
    }
}

/// <summary>Star-skill lookups (spec §13). Every number comes from the table; an unknown id bends nothing.</summary>
public static class StarSkills
{
    public static double PitchSpeedMul(string? id, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(id)?.SpeedMul ?? 1.0;

    public static double BatterWindowMul(string? starPitch, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(starPitch)?.BatterWindowMul ?? 1.0;

    public static int StaminaCost(string? starPitch, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Pitch(starPitch)?.StaminaCost ?? 0;

    public static double SwingExitMul(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.ExitVeloMul ?? 1.0;

    /// <summary>A role player's star swing names its launch; a captain's keeps the swing's own.</summary>
    public static double? SwingLaunchDeg(string? starSwing, StarSkillTable? table = null) =>
        StarSkillTable.Or(table).Swing(starSwing)?.LaunchDeg;

    /// <summary>
    /// How long a special owns the ball or the field before baseball resumes.
    /// Readable clip, not a full-screen blind.
    /// </summary>
    public static double SpectacleSeconds(string? id) =>
        string.IsNullOrEmpty(id) ? 0 : 2.0;
}
