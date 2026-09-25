namespace GrandSluggers.Sim;

public readonly record struct Vec3(double X, double Y, double Z);

public readonly record struct CameraShot(
    string Id,
    string Look,
    Vec3 Pos,
    Vec3 Target,
    double Fov,
    int Blend,
    Vec3? Fallback = null);

public sealed class CameraShots
{
    readonly Dictionary<string, CameraShot> _byId;
    readonly Dictionary<string, ParkShot> _parkById;

    CameraShots(Dictionary<string, CameraShot> byId, Dictionary<string, ParkShot> parkById)
    {
        _byId = byId;
        _parkById = parkById;
    }

    public IReadOnlyDictionary<string, CameraShot> ById => _byId;

    /// <summary>
    /// The named park shots (<c>parkShots</c>; F7-b): stills whose pose <see cref="StillShots.Frame"/> computes from the
    /// park being captured. Not in <see cref="ById"/>: a park shot has no pose until it has a park.
    /// </summary>
    public IReadOnlyDictionary<string, ParkShot> ParkById => _parkById;

    public bool TryGetPark(string id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ParkShot shot) =>
        _parkById.TryGetValue(id, out shot);

    public CameraShot Must(string id) =>
        _byId.TryGetValue(id, out var shot)
            ? shot
            : throw new KeyNotFoundException($"No camera shot '{id}'");

    public bool TryGet(string id, out CameraShot shot) => _byId.TryGetValue(id, out shot);

    public static CameraShots Load(DataRoot dataRoot)
    {
        var path = dataRoot.Resolve("feel", "shots.json");
        var dto = DataJson.Require<ShotsFile>(path);
        var map = new Dictionary<string, CameraShot>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in dto.Shots ?? [])
        {
            if (string.IsNullOrWhiteSpace(row.Id))
                throw new InvalidDataException("Camera shot missing id");
            map[row.Id] = new CameraShot(
                row.Id,
                string.IsNullOrWhiteSpace(row.Look) ? "plate" : row.Look,
                row.Pos.ToVec(),
                row.Target.ToVec(),
                row.Fov,
                row.Blend,
                row.Fallback?.ToVec());
        }
        var parks = new Dictionary<string, ParkShot>(StringComparer.OrdinalIgnoreCase);
        var rows = dto.ParkShots ?? [];
        for (var i = 0; i < rows.Count; i++)
        {
            var shot = (rows[i] ?? throw new InvalidDataException($"{path}: parkShots[{i}] is empty")).ToShot(path, i);
            // One id is one still: a park shot cannot share a name with a fixed shot or another park shot.
            if (map.ContainsKey(shot.Id) || parks.ContainsKey(shot.Id))
                throw new InvalidDataException($"{path}: parkShots[{i}] '{shot.Id}' is already a shot");
            parks.Add(shot.Id, shot);
        }
        return new CameraShots(map, parks);
    }

    sealed class ShotsFile
    {
        public List<ShotDto>? Shots { get; set; }
        public List<ParkShotDto?>? ParkShots { get; set; }
    }

    sealed class ShotDto
    {
        public string Id { get; set; } = "";
        public string Look { get; set; } = "";
        public VecDto Pos { get; set; } = new();
        public VecDto Target { get; set; } = new();
        public double Fov { get; set; }
        public int Blend { get; set; }
        /// <summary>Where a shot centered on a moment looks when the play left none (the smash replay's batter).</summary>
        public VecDto? Fallback { get; set; }
    }

    sealed class VecDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Vec3 ToVec() => new(X, Y, Z);
    }
}

/// <summary>
/// The presentation numbers (<c>data/feel/table.json</c>): charge, freeze, camera and body timing. Read like the rules
/// tables: the JSON is the only source, and the loader refuses a missing field, an unknown field or a value outside its
/// declared range. There is no code default to fall back to.
/// </summary>
public sealed record FeelTable
{
    [Positive] public double PitchChargeSeconds { get; init; }
    [Positive] public double SwingChargeSeconds { get; init; }
    [Positive] public double SmashFreeze { get; init; }
    public double SolidFreeze { get; init; }
    public double SmashHold { get; init; }
    public double CameraBlend { get; init; }
    /// <summary>Stick magnitude that takes the glove from the CPU (the one threshold; FieldAssist reads it).</summary>
    [Positive, Chance] public double FieldAssistStick { get; init; }
    [Positive] public double PitcherReadySeconds { get; init; }
    /// <summary>Stick down past this in SET resets the batter's box or the pitcher's rubber (spec §5.4, §4.2).</summary>
    [Positive, Chance] public double SetResetStick { get; init; }
    /// <summary>How fast the HUD's pips pulse, in cycles per second.</summary>
    [Positive] public double PipPulseHz { get; init; }
    /// <summary>How long the game-over replay runs before the final card, unless South skips it.</summary>
    [Positive] public double ReplaySec { get; init; }
    /// <summary>With no lineup screen open, how long before the match starts on its own.</summary>
    [Positive] public double LineupAutoStartSec { get; init; }
    [Positive] public double AfterOutSeconds { get; init; }
    [Positive] public double AfterCountSeconds { get; init; }
    [Positive] public double ChargeMaxHoldSeconds { get; init; }
    [Positive] public double ChargeOverchargeDecay { get; init; }
    /// <summary>
    /// Play seconds after the crack before the camera leaves the SET shot for the diamond (spec §8.2,
    /// §15; the reference cut at 0.42). A home run overrides it with the smash beat at the crack.
    /// </summary>
    [Positive] public double ContactCutSeconds { get; init; }
    /// <summary>
    /// Play seconds the live camera keeps a target before another may take it (D14, #610): a relay or a
    /// rundown flicker inside the hold does not re-aim (<see cref="PlayCamera.CameraHold"/>).
    /// </summary>
    public double CameraHoldSeconds { get; init; }
    /// <summary>
    /// How fast a body turns toward its heading, degrees per second of frame time (spec §8.2, #611).
    /// dt-scaled, so a 30 fps and a 60 fps body face the same way at the same moment.
    /// </summary>
    [Positive] public double BodyTurnDegPerSec { get; init; }
    /// <summary>Time constant of the measured ground velocity a running body faces (seconds).</summary>
    public double HeadingSmoothSec { get; init; }
    /// <summary>A body that moves faster than this between two frames was placed, not run: its velocity resets.</summary>
    [Positive] public double HeadingTeleportFtPerSec { get; init; }
    /// <summary>The backpedal (§8.2): inside this distance of a fly's plant, a glove moving away from the ball faces the ball.</summary>
    public double BackpedalFt { get; init; }
    /// <summary>The walk / run take threshold as a share of the body's own slowest full-effort pursuit speed (<see cref="Gait.RunFloorFt"/>, #1111).</summary>
    [Positive, Chance] public double GaitRunOfPursuit { get; init; }
    /// <summary>A ball closer than this (horizontally) is overhead or in the glove: the body keeps its heading.</summary>
    public double FaceBallMinFt { get; init; }
    /// <summary>
    /// The held swing finish (#583, #613) lets go on contact once the batter-runner is this far out
    /// of the box: the finish holds through the contact freeze and the first step of the run.
    /// </summary>
    [Positive] public double SwingFinishStepFt { get; init; }
    /// <summary>
    /// Two runners on one bag (§9.1): the one the bag does not protect is drawn this far off it, toward the bag he came
    /// from (or, forced off it, toward the next), so the two bodies never merge (<see cref="Runner.DrawPosition"/>).
    /// </summary>
    [Positive] public double RunnerShareStepFt { get; init; }

    public RaceCameraFeel RaceCamera { get; init; } = new();
    public BallShadowFeel BallShadow { get; init; } = new();

    /// <summary>How a fielding body shows what the ball cost it (#719–#721): the dive's get-up and the impact brace.</summary>
    public FieldTellsFeel FieldTells { get; init; } = new();

    public static FeelTable Load(DataRoot dataRoot)
    {
        var path = dataRoot.Resolve("feel", "table.json");
        var errors = new List<string>();
        FeelTable? table = null;
        try
        {
            var text = File.ReadAllText(path);
            using (var doc = JsonDocument.Parse(text, DataJson.Document))
            {
                RulesValidation.UnknownFields(doc.RootElement, typeof(FeelTable), "feel", path, errors);
                RulesValidation.MissingFields(doc.RootElement, typeof(FeelTable), "feel", path, errors);
            }
            table = JsonSerializer.Deserialize<FeelTable>(text, DataJson.Options);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read the feel table: {ex.Message}");
        }
        if (table is null && errors.Count == 0) errors.Add($"{path}: the feel table is empty");
        if (errors.Count == 0)
        {
            RulesValidation.Ranges(table!, path, "feel", errors);
            foreach (var check in new Action[] { table!.BallShadow.Validate, table.FieldTells.Validate, table.RaceCamera.Validate })
            {
                try { check(); }
                catch (InvalidDataException ex) { errors.Add($"{path}: {ex.Message}"); }
            }
        }
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid feel table:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return table!;
    }
}
