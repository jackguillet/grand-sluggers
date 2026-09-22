namespace GrandSluggers.Sim;

public readonly record struct Vec3(double X, double Y, double Z);

public readonly record struct CameraShot(
    string Id,
    string Look,
    Vec3 Pos,
    Vec3 Target,
    double Fov,
    int Blend);

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
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var dto = JsonSerializer.Deserialize<ShotsFile>(File.ReadAllText(path), json)
            ?? throw new InvalidDataException($"Bad shots file {path}");
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
                row.Blend);
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
    }

    sealed class VecDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Vec3 ToVec() => new(X, Y, Z);
    }
}

public sealed class FeelTable
{
    FeelTable(
        double pitchChargeSeconds,
        double swingChargeSeconds,
        double smashFreeze,
        double solidFreeze,
        double smashHold,
        double cameraBlend,
        double fieldAssistStick,
        double pitcherReadySeconds,
        double afterOutSeconds,
        double afterCountSeconds,
        double chargeMaxHoldSeconds,
        double chargeOverchargeDecay,
        double contactCutSeconds,
        double cameraHoldSeconds)
    {
        PitchChargeSeconds = pitchChargeSeconds;
        SwingChargeSeconds = swingChargeSeconds;
        SmashFreeze = smashFreeze;
        SolidFreeze = solidFreeze;
        SmashHold = smashHold;
        CameraBlend = cameraBlend;
        FieldAssistStick = fieldAssistStick;
        PitcherReadySeconds = pitcherReadySeconds;
        AfterOutSeconds = afterOutSeconds;
        AfterCountSeconds = afterCountSeconds;
        ChargeMaxHoldSeconds = chargeMaxHoldSeconds;
        ChargeOverchargeDecay = chargeOverchargeDecay;
        ContactCutSeconds = contactCutSeconds;
        CameraHoldSeconds = cameraHoldSeconds;
    }

    public double PitchChargeSeconds { get; }
    public double SwingChargeSeconds { get; }
    public double SmashFreeze { get; }
    public double SolidFreeze { get; }
    public double SmashHold { get; }
    public double CameraBlend { get; }
    /// <summary>Stick magnitude that takes the glove from the CPU (the one threshold; FieldAssist reads it).</summary>
    public double FieldAssistStick { get; }
    public double PitcherReadySeconds { get; }
    public double AfterOutSeconds { get; }
    public double AfterCountSeconds { get; }
    public double ChargeMaxHoldSeconds { get; }
    public double ChargeOverchargeDecay { get; }
    /// <summary>
    /// Play seconds after the crack before the camera leaves the SET shot for the diamond (spec §8.2,
    /// §15; the reference cut at 0.42). A home run overrides it with the smash beat at the crack.
    /// </summary>
    public double ContactCutSeconds { get; }
    /// <summary>
    /// Play seconds the live camera keeps a target before another may take it (D14, #610): a relay or a
    /// rundown flicker inside the hold does not re-aim (<see cref="PlayCamera.CameraHold"/>).
    /// </summary>
    public double CameraHoldSeconds { get; }
    /// <summary>
    /// How fast a body turns toward its heading, degrees per second of frame time (spec §8.2, #611).
    /// dt-scaled, so a 30 fps and a 60 fps body face the same way at the same moment.
    /// </summary>
    public double BodyTurnDegPerSec { get; private init; } = 720;
    /// <summary>Time constant of the measured ground velocity a running body faces (seconds).</summary>
    public double HeadingSmoothSec { get; private init; } = 0.08;
    /// <summary>A body that moves faster than this between two frames was placed, not run: its velocity resets.</summary>
    public double HeadingTeleportFtPerSec { get; private init; } = 90;
    /// <summary>The backpedal (§8.2): inside this distance of a fly's plant, a glove moving away from the ball faces the ball.</summary>
    public double BackpedalFt { get; private init; } = 12;
    /// <summary>A ball closer than this (horizontally) is overhead or in the glove: the body keeps its heading.</summary>
    public double FaceBallMinFt { get; private init; } = 3;
    /// <summary>
    /// The held swing finish (#583, #613) lets go on contact once the batter-runner is this far out
    /// of the box: the finish holds through the contact freeze and the first step of the run.
    /// </summary>
    public double SwingFinishStepFt { get; private init; } = 2.5;

    public BallShadowFeel BallShadow { get; private init; } = new();

    /// <summary>How a fielding body shows what the ball cost it (#719–#721): the dive's get-up and the impact brace.</summary>
    public FieldTellsFeel FieldTells { get; private init; } = new();

    public static FeelTable Load(DataRoot dataRoot)
    {
        var path = dataRoot.Resolve("feel", "table.json");
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var dto = JsonSerializer.Deserialize<FeelDto>(File.ReadAllText(path), json)
            ?? throw new InvalidDataException($"Bad feel table {path}");
        if (dto.PitchChargeSeconds <= 0 || dto.SmashFreeze <= 0)
            throw new InvalidDataException("Feel table charge and smash freeze must be positive");
        dto.BallShadow.Validate();
        dto.FieldTells.Validate();
        var assist = dto.FieldAssistStick > 0 ? dto.FieldAssistStick : 0.35;
        var ready = dto.PitcherReadySeconds > 0 ? dto.PitcherReadySeconds : 0.55;
        var after = dto.AfterOutSeconds > 0 ? dto.AfterOutSeconds : 1.35;
        var count = dto.AfterCountSeconds > 0 ? dto.AfterCountSeconds : 0.7;
        var maxHold = dto.ChargeMaxHoldSeconds > 0 ? dto.ChargeMaxHoldSeconds : 0.5;
        var over = dto.ChargeOverchargeDecay > 0 ? dto.ChargeOverchargeDecay : 0.8;
        var cut = dto.ContactCutSeconds > 0 ? dto.ContactCutSeconds : 0.42;
        var hold = dto.CameraHoldSeconds >= 0 ? dto.CameraHoldSeconds : 0.25;
        return new FeelTable(
            dto.PitchChargeSeconds,
            dto.SwingChargeSeconds,
            dto.SmashFreeze,
            dto.SolidFreeze,
            dto.SmashHold,
            dto.CameraBlend,
            assist,
            ready,
            after,
            count,
            maxHold,
            over,
            cut,
            hold)
        {
            BallShadow = dto.BallShadow,
            FieldTells = dto.FieldTells,
            BodyTurnDegPerSec = dto.BodyTurnDegPerSec > 0 ? dto.BodyTurnDegPerSec : 720,
            HeadingSmoothSec = dto.HeadingSmoothSec >= 0 ? dto.HeadingSmoothSec : 0.08,
            HeadingTeleportFtPerSec = dto.HeadingTeleportFtPerSec > 0 ? dto.HeadingTeleportFtPerSec : 90,
            BackpedalFt = dto.BackpedalFt >= 0 ? dto.BackpedalFt : 12,
            FaceBallMinFt = dto.FaceBallMinFt >= 0 ? dto.FaceBallMinFt : 3,
            SwingFinishStepFt = dto.SwingFinishStepFt > 0 ? dto.SwingFinishStepFt : 2.5
        };
    }

    sealed class FeelDto
    {
        public BallShadowFeel BallShadow { get; set; } = new();
        public FieldTellsFeel FieldTells { get; set; } = new();
        public double PitchChargeSeconds { get; set; }
        public double SwingChargeSeconds { get; set; }
        public double SmashFreeze { get; set; }
        public double SolidFreeze { get; set; }
        public double SmashHold { get; set; }
        public double CameraBlend { get; set; } = 6;
        public double FieldAssistStick { get; set; } = 0.35;
        public double PitcherReadySeconds { get; set; } = 0.55;
        public double AfterOutSeconds { get; set; } = 1.35;
        public double AfterCountSeconds { get; set; } = 0.7;
        public double ChargeMaxHoldSeconds { get; set; } = 0.5;
        public double ChargeOverchargeDecay { get; set; } = 0.8;
        public double ContactCutSeconds { get; set; } = 0.42;
        public double CameraHoldSeconds { get; set; } = 0.25;
        public double BodyTurnDegPerSec { get; set; } = 720;
        public double HeadingSmoothSec { get; set; } = 0.08;
        public double HeadingTeleportFtPerSec { get; set; } = 90;
        public double BackpedalFt { get; set; } = 12;
        public double FaceBallMinFt { get; set; } = 3;
        public double SwingFinishStepFt { get; set; } = 2.5;
    }
}
