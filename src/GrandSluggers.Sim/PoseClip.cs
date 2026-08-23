namespace GrandSluggers.Sim;

public sealed class PoseClips
{
    readonly Dictionary<string, PoseClip> _byId;

    PoseClips(Dictionary<string, PoseClip> byId) => _byId = byId;

    public bool TryEvaluate(string id, double t, out MoveBones.Sample sample)
    {
        if (_byId.TryGetValue(id, out var clip))
        {
            sample = clip.Evaluate(t);
            return true;
        }
        sample = default;
        return false;
    }

    public static PoseClips Load(string dataRoot)
    {
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var dir = Path.Combine(dataRoot, "art", "pose-clips");
        var map = new Dictionary<string, PoseClip>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(dir)) return new PoseClips(map);
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var dto = JsonSerializer.Deserialize<ClipFile>(File.ReadAllText(file), json)
                ?? throw new InvalidDataException("Bad pose clip " + file);
            if (string.IsNullOrWhiteSpace(dto.Id) || dto.Keys is not { Count: > 0 })
                throw new InvalidDataException("Pose clip missing id/keys " + file);
            var keys = dto.Keys.Select(k => (k.T, k.ToSample())).OrderBy(k => k.T).ToList();
            map[dto.Id] = new PoseClip(dto.Id, dto.Loop, dto.Duration > 0 ? dto.Duration : keys[^1].T, keys);
        }
        return new PoseClips(map);
    }

    sealed class ClipFile
    {
        public string Id { get; set; } = "";
        public bool Loop { get; set; } = true;
        public double Duration { get; set; }
        public List<KeyDto>? Keys { get; set; }
    }

    sealed class KeyDto
    {
        public double T { get; set; }
        public double[]? Torso { get; set; }
        public double[]? Head { get; set; }
        public double[]? LUpper { get; set; }
        public double[]? LFore { get; set; }
        public double[]? RUpper { get; set; }
        public double[]? RFore { get; set; }
        public double[]? LThigh { get; set; }
        public double[]? LShin { get; set; }
        public double[]? RThigh { get; set; }
        public double[]? RShin { get; set; }
        public double[]? Bat { get; set; }
        public double Lift { get; set; }

        public MoveBones.Sample ToSample() => new(
            E(Torso), E(Head), E(LUpper), E(LFore), E(RUpper), E(RFore),
            E(LThigh), E(LShin), E(RThigh), E(RShin), E(Bat, 0, 0, 20), Lift);

        static MoveBones.Euler E(double[]? a, double dx = 0, double dy = 0, double dz = 0) =>
            a is { Length: >= 3 } ? new(a[0], a[1], a[2]) : new(dx, dy, dz);
    }
}

public sealed class PoseClip
{
    public PoseClip(string id, bool loop, double duration, IReadOnlyList<(double T, MoveBones.Sample S)> keys)
    {
        Id = id;
        Loop = loop;
        Duration = duration;
        Keys = keys;
    }

    public string Id { get; }
    public bool Loop { get; }
    public double Duration { get; }
    public IReadOnlyList<(double T, MoveBones.Sample S)> Keys { get; }

    public MoveBones.Sample Evaluate(double t)
    {
        if (Keys.Count == 0) return default;
        if (Keys.Count == 1) return Keys[0].S;
        var dur = Duration > 0 ? Duration : Keys[^1].T;
        if (dur <= 0) return Keys[0].S;
        var u = Loop ? t - Math.Floor(t / dur) * dur : Math.Clamp(t, 0, dur);
        for (var i = 0; i < Keys.Count - 1; i++)
        {
            var a = Keys[i];
            var b = Keys[i + 1];
            if (u <= b.T || i == Keys.Count - 2)
            {
                var span = b.T - a.T;
                var k = span <= 1e-8 ? 1 : Math.Clamp((u - a.T) / span, 0, 1);
                return Mix(a.S, b.S, k);
            }
        }
        return Keys[^1].S;
    }

    static MoveBones.Sample Mix(MoveBones.Sample a, MoveBones.Sample b, double u) => new(
        Le(a.Torso, b.Torso, u), Le(a.Head, b.Head, u),
        Le(a.LUpper, b.LUpper, u), Le(a.LFore, b.LFore, u),
        Le(a.RUpper, b.RUpper, u), Le(a.RFore, b.RFore, u),
        Le(a.LThigh, b.LThigh, u), Le(a.LShin, b.LShin, u),
        Le(a.RThigh, b.RThigh, u), Le(a.RShin, b.RShin, u),
        Le(a.Bat, b.Bat, u),
        a.Lift + (b.Lift - a.Lift) * u);

    static MoveBones.Euler Le(MoveBones.Euler a, MoveBones.Euler b, double u) =>
        new(a.X + (b.X - a.X) * u, a.Y + (b.Y - a.Y) * u, a.Z + (b.Z - a.Z) * u);
}
