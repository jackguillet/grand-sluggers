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

    CameraShots(Dictionary<string, CameraShot> byId) => _byId = byId;

    public IReadOnlyDictionary<string, CameraShot> ById => _byId;

    public CameraShot Must(string id) =>
        _byId.TryGetValue(id, out var shot)
            ? shot
            : throw new KeyNotFoundException($"No camera shot '{id}'");

    public bool TryGet(string id, out CameraShot shot) => _byId.TryGetValue(id, out shot);

    public static CameraShots Load(string dataRoot)
    {
        var path = Path.Combine(dataRoot, "feel", "shots.json");
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
        return new CameraShots(map);
    }

    sealed class ShotsFile
    {
        public List<ShotDto>? Shots { get; set; }
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
        double throwEase,
        double cameraBlend,
        double chargeDecay,
        double runHz,
        double fieldAssistStick,
        double pitcherReadySeconds,
        double afterOutSeconds,
        double inPlayCommitSeconds,
        double cpuVsHumanTake,
        double cpuVsHumanMiss,
        double chargeMaxHoldSeconds,
        double chargeOverchargeDecay)
    {
        PitchChargeSeconds = pitchChargeSeconds;
        SwingChargeSeconds = swingChargeSeconds;
        SmashFreeze = smashFreeze;
        SolidFreeze = solidFreeze;
        SmashHold = smashHold;
        ThrowEase = throwEase;
        CameraBlend = cameraBlend;
        ChargeDecay = chargeDecay;
        RunHz = runHz;
        FieldAssistStick = fieldAssistStick;
        PitcherReadySeconds = pitcherReadySeconds;
        AfterOutSeconds = afterOutSeconds;
        InPlayCommitSeconds = inPlayCommitSeconds;
        CpuVsHumanTake = cpuVsHumanTake;
        CpuVsHumanMiss = cpuVsHumanMiss;
        ChargeMaxHoldSeconds = chargeMaxHoldSeconds;
        ChargeOverchargeDecay = chargeOverchargeDecay;
    }

    public double PitchChargeSeconds { get; }
    public double SwingChargeSeconds { get; }
    public double SmashFreeze { get; }
    public double SolidFreeze { get; }
    public double SmashHold { get; }
    public double ThrowEase { get; }
    public double CameraBlend { get; }
    public double ChargeDecay { get; }
    public double RunHz { get; }
    public double FieldAssistStick { get; }
    public double PitcherReadySeconds { get; }
    public double AfterOutSeconds { get; }
    public double InPlayCommitSeconds { get; }
    public double CpuVsHumanTake { get; }
    public double CpuVsHumanMiss { get; }
    public double ChargeMaxHoldSeconds { get; }
    public double ChargeOverchargeDecay { get; }

    public static FeelTable Load(string dataRoot)
    {
        var path = Path.Combine(dataRoot, "feel", "table.json");
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
        var assist = dto.FieldAssistStick > 0 ? dto.FieldAssistStick : FieldAssist.StickTake;
        var ready = dto.PitcherReadySeconds > 0 ? dto.PitcherReadySeconds : 0.55;
        var after = dto.AfterOutSeconds > 0 ? dto.AfterOutSeconds : 1.35;
        var commit = dto.InPlayCommitSeconds > 0 ? dto.InPlayCommitSeconds : 1.6;
        var take = dto.CpuVsHumanTake > 0 ? dto.CpuVsHumanTake : 0.32;
        var miss = dto.CpuVsHumanMiss > 0 ? dto.CpuVsHumanMiss : 0.22;
        var maxHold = dto.ChargeMaxHoldSeconds > 0 ? dto.ChargeMaxHoldSeconds : 0.5;
        var over = dto.ChargeOverchargeDecay > 0 ? dto.ChargeOverchargeDecay : 0.8;
        return new FeelTable(
            dto.PitchChargeSeconds,
            dto.SwingChargeSeconds,
            dto.SmashFreeze,
            dto.SolidFreeze,
            dto.SmashHold,
            dto.ThrowEase,
            dto.CameraBlend,
            dto.ChargeDecay,
            dto.RunHz,
            assist,
            ready,
            after,
            commit,
            take,
            miss,
            maxHold,
            over);
    }

    sealed class FeelDto
    {
        public double PitchChargeSeconds { get; set; }
        public double SwingChargeSeconds { get; set; }
        public double SmashFreeze { get; set; }
        public double SolidFreeze { get; set; }
        public double SmashHold { get; set; }
        public double ThrowEase { get; set; } = 6;
        public double CameraBlend { get; set; } = 6;
        public double ChargeDecay { get; set; } = 1.4;
        public double RunHz { get; set; } = 2.55;
        public double FieldAssistStick { get; set; } = 0.35;
        public double PitcherReadySeconds { get; set; } = 0.55;
        public double AfterOutSeconds { get; set; } = 1.35;
        public double InPlayCommitSeconds { get; set; } = 1.6;
        public double CpuVsHumanTake { get; set; } = 0.32;
        public double CpuVsHumanMiss { get; set; } = 0.22;
        public double ChargeMaxHoldSeconds { get; set; } = 0.5;
        public double ChargeOverchargeDecay { get; set; } = 0.8;
    }
}
