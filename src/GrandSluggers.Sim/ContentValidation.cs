using System.Text.Json;

namespace GrandSluggers.Sim;

/// <summary>
/// Validates authored gameplay data before it becomes a catalog. Art slots stay
/// permissive so a missing mesh or visual can still use the game's placeholders.
/// </summary>
public static class ContentDataValidator
{
    static readonly HashSet<string> Hands = new(StringComparer.OrdinalIgnoreCase) { "L", "R" };
    static readonly HashSet<string> Surfaces = new(StringComparer.Ordinal) { "grass", "dirt", "ice", "ash" };
    static readonly HashSet<string> FieldAbilityIds = new(StringComparer.Ordinal)
    {
        "burrow", "clamber", "dive", "grow", "laser", "lick-catch",
        "snap-throw", "spin-check", "super-jump", "withdraw"
    };
    static readonly HashSet<string> HazardTypes = new(StringComparer.Ordinal)
    {
        "ac_unit", "barrel", "billboard", "climb_wall", "fire_breath",
        "freeze_volume", "lava_pit", "statue", "train", "tree", "warp_pipe"
    };

    public static IReadOnlyList<string> Validate(string dataRoot)
    {
        var data = Read(dataRoot, JsonOptions());
        return Errors(data);
    }

    internal static ContentData Load(string dataRoot, JsonSerializerOptions json)
    {
        var data = Read(dataRoot, json);
        var errors = Errors(data);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid gameplay content:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return data;
    }

    static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static ContentData Read(string dataRoot, JsonSerializerOptions json)
    {
        var root = Path.GetFullPath(dataRoot);
        var data = new ContentData();

        foreach (var file in Files(root, "characters", data.ReadErrors))
        {
            if (Path.GetFileName(file).Equals("role-players.json", StringComparison.OrdinalIgnoreCase))
            {
                var rows = ReadJson<List<CharacterDto?>>(file, json, data.ReadErrors);
                if (rows is null) continue;
                for (var i = 0; i < rows.Count; i++)
                {
                    if (rows[i] is null)
                        data.ReadErrors.Add($"{file}[{i}]: character row must be an object; got null");
                    else
                        data.Characters.Add(new(rows[i]!, $"{file}[{i}]"));
                }
            }
            else
            {
                var row = ReadJson<CharacterDto>(file, json, data.ReadErrors);
                if (row is not null) data.Characters.Add(new(row, file));
            }
        }

        ReadRows(root, "parks", data.Parks, json, data.ReadErrors);
        ReadRows(root, "bats", data.Bats, json, data.ReadErrors);
        ReadRows(root, "gloves", data.Gloves, json, data.ReadErrors);

        var chemistryPath = Path.Combine(root, "chemistry", "overrides.json");
        data.Chemistry = ReadJson<ChemistryOverrides>(chemistryPath, json, data.ReadErrors) ?? new();
        data.ChemistrySource = chemistryPath;

        var skillsPath = Path.Combine(root, "abilities", "star-skills.json");
        data.StarSkills = ReadJson<StarSkillsDto>(skillsPath, json, data.ReadErrors) ?? new();
        data.StarSkillsSource = skillsPath;

        // Rule numbers (spec §16). Missing fields fall back to code; unknown fields and bad ranges are errors.
        data.Rules = RulesTable.Load(root, data.ReadErrors);
        return data;
    }

    static void ReadRows<T>(
        string root,
        string directory,
        List<Sourced<T>> destination,
        JsonSerializerOptions json,
        List<string> errors) where T : class
    {
        foreach (var file in Files(root, directory, errors))
        {
            var row = ReadJson<T>(file, json, errors);
            if (row is not null) destination.Add(new(row, file));
        }
    }

    static IReadOnlyList<string> Files(string root, string directory, List<string> errors)
    {
        var path = Path.Combine(root, directory);
        if (!Directory.Exists(path))
        {
            errors.Add($"{path}: required gameplay data directory is missing");
            return [];
        }
        return Directory.GetFiles(path, "*.json")
            .OrderBy(Path.GetFullPath, StringComparer.Ordinal)
            .ToList();
    }

    static T? ReadJson<T>(string path, JsonSerializerOptions json, List<string> errors) where T : class
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), json);
            if (value is null) errors.Add($"{path}: JSON document is empty");
            return value;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read gameplay data: {ex.Message}");
            return null;
        }
    }

    static IReadOnlyList<string> Errors(ContentData data)
    {
        var errors = new List<string>(data.ReadErrors);
        DuplicateIds("character", data.Characters.Select(r => (r.Value.Id, r.Source)), errors);
        DuplicateIds("park", data.Parks.Select(r => (r.Value.Id, r.Source)), errors);
        DuplicateIds("bat", data.Bats.Select(r => (r.Value.Id, r.Source)), errors);
        DuplicateIds("glove", data.Gloves.Select(r => (r.Value.Id, r.Source)), errors);

        var pitches = SkillIds("pitch", data.StarSkills.Pitches, data.StarSkillsSource, errors);
        var swings = SkillIds("swing", data.StarSkills.Swings, data.StarSkillsSource, errors);
        var characters = data.Characters
            .Select(r => r.Value.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in data.Characters)
            ValidateCharacter(row, pitches, swings, errors);
        foreach (var row in data.Parks)
            ValidatePark(row, errors);
        foreach (var row in data.Bats)
            ValidateBat(row, errors);
        foreach (var row in data.Gloves)
            ValidateGlove(row, errors);

        if (data.Chemistry.Buddies is null)
            errors.Add($"{data.ChemistrySource}: chemistry buddies must be an array; got null");
        else
            ValidateChemistry("buddies", data.Chemistry.Buddies, data.ChemistrySource, characters, errors);
        if (data.Chemistry.Rivals is null)
            errors.Add($"{data.ChemistrySource}: chemistry rivals must be an array; got null");
        else
            ValidateChemistry("rivals", data.Chemistry.Rivals, data.ChemistrySource, characters, errors);
        return errors.OrderBy(e => e, StringComparer.Ordinal).ToList();
    }

    static HashSet<string> SkillIds(
        string kind,
        Dictionary<string, StarSkillDto?>? rows,
        string source,
        List<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var caseInsensitiveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (rows is null)
        {
            errors.Add($"{source}: star {kind}s must be an object; got null");
            return ids;
        }
        foreach (var (key, value) in rows.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!caseInsensitiveIds.Add(key))
                errors.Add($"{source}: duplicate star {kind} id '{key}' (case-insensitive)");
            ids.Add(key);
            if (value is null)
            {
                errors.Add($"{source}: star {kind} '{key}' must be an object; got null");
                continue;
            }
            if (string.IsNullOrWhiteSpace(value.Id))
                errors.Add($"{source}: star {kind} '{key}' has an empty id");
            else if (!value.Id.Equals(key, StringComparison.Ordinal))
                errors.Add($"{source}: star {kind} key '{key}' does not match id '{value.Id}'");
            // The numbers the sim reads (spec §13): a value outside its range is a data error, not a fallback.
            if (kind == "pitch")
            {
                if (value.SpeedMul is null || value.SpeedMul <= 0)
                    errors.Add($"{source}: star pitch '{key}' speedMul must be greater than 0; got {value.SpeedMul?.ToString() ?? "null"}");
                if (value.StaminaCost is null || value.StaminaCost < 0)
                    errors.Add($"{source}: star pitch '{key}' staminaCost must be at least 0; got {value.StaminaCost?.ToString() ?? "null"}");
                if (value.BatterWindowMul is not null && (value.BatterWindowMul <= 0 || value.BatterWindowMul > 1))
                    errors.Add($"{source}: star pitch '{key}' batterWindowMul must be in (0, 1]; got {value.BatterWindowMul}");
            }
            else
            {
                if (value.ExitVeloMul is null || value.ExitVeloMul <= 0)
                    errors.Add($"{source}: star swing '{key}' exitVeloMul must be greater than 0; got {value.ExitVeloMul?.ToString() ?? "null"}");
                if (value.LaunchDeg is not null && (value.LaunchDeg < 0 || value.LaunchDeg > 60))
                    errors.Add($"{source}: star swing '{key}' launchDeg must be between 0 and 60; got {value.LaunchDeg}");
            }
        }
        return ids;
    }

    static void DuplicateIds(string kind, IEnumerable<(string Id, string Source)> candidates, List<string> errors)
    {
        foreach (var group in candidates
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var sources = group.Select(x => x.Source).OrderBy(x => x, StringComparer.Ordinal);
            errors.Add($"duplicate {kind} id '{group.Key.ToLowerInvariant()}' (case-insensitive): {string.Join("; ", sources)}");
        }
    }

    static void ValidateCharacter(
        Sourced<CharacterDto> row,
        HashSet<string> pitches,
        HashSet<string> swings,
        List<string> errors)
    {
        var c = row.Value;
        Required(row.Source, "character", c.Id, "id", c.Id, errors);
        Required(row.Source, "character", c.Id, "name", c.Name, errors);
        Required(row.Source, "character", c.Id, "faction", c.Faction, errors);
        Range(row.Source, $"character '{c.Id}' pitch", c.Pitch, 1, 10, errors);
        Range(row.Source, $"character '{c.Id}' bat", c.Bat, 1, 10, errors);
        Range(row.Source, $"character '{c.Id}' field", c.Field, 1, 10, errors);
        Range(row.Source, $"character '{c.Id}' run", c.Run, 1, 10, errors);
        Known(row.Source, $"character '{c.Id}' bats", c.Bats, Hands, errors);
        Known(row.Source, $"character '{c.Id}' throws", c.Throws, Hands, errors);
        Known(row.Source, $"character '{c.Id}' fieldAbility", c.FieldAbility, FieldAbilityIds, errors);
        Reference(row.Source, $"character '{c.Id}' starPitch", c.StarPitch, pitches, errors);
        Reference(row.Source, $"character '{c.Id}' starSwing", c.StarSwing, swings, errors);
    }

    static void ValidatePark(Sourced<ParkDto> row, List<string> errors)
    {
        var p = row.Value;
        Required(row.Source, "park", p.Id, "id", p.Id, errors);
        Required(row.Source, "park", p.Id, "name", p.Name, errors);
        Required(row.Source, "park", p.Id, "faction", p.Faction, errors);
        Known(row.Source, $"park '{p.Id}' surface", p.Surface, Surfaces, errors);
        Positive(row.Source, $"park '{p.Id}' leftFenceFt", p.LeftFenceFt, errors);
        Positive(row.Source, $"park '{p.Id}' centerFenceFt", p.CenterFenceFt, errors);
        Positive(row.Source, $"park '{p.Id}' rightFenceFt", p.RightFenceFt, errors);
        Finite(row.Source, $"park '{p.Id}' windMph", p.WindMph, errors);
        FiniteRange(row.Source, $"park '{p.Id}' windDeg", p.WindDeg, -360, 360, errors);
        if (!(p.FenceHeightFt > 0) || double.IsNaN(p.FenceHeightFt) || double.IsInfinity(p.FenceHeightFt))
            errors.Add($"{row.Source}: park '{p.Id}' fenceHeightFt must be greater than 0; got {p.FenceHeightFt}");
        if (!(p.NightContactWindowMul > 0 && p.NightContactWindowMul <= 1))
            errors.Add($"{row.Source}: park '{p.Id}' nightContactWindowMul must be in (0, 1]; got {p.NightContactWindowMul}");
        for (var i = 0; i < (p.Hazards?.Count ?? 0); i++)
        {
            var h = p.Hazards![i];
            var where = $"park '{p.Id}' hazard[{i}]";
            if (h is null)
            {
                errors.Add($"{row.Source}: {where} must be an object; got null");
                continue;
            }
            Known(row.Source, where + " type", h.Type, HazardTypes, errors);
            Finite(row.Source, where + " x", h.X, errors);
            Finite(row.Source, where + " z", h.Z, errors);
            NonNegative(row.Source, where + " radius", h.Radius, errors);
            if (!string.Equals(h.Type, "train", StringComparison.Ordinal) && h.Radius == 0)
                errors.Add($"{row.Source}: {where} radius must be greater than 0; got {h.Radius}");
        }
    }

    static void ValidateBat(Sourced<BatDto> row, List<string> errors)
    {
        var b = row.Value;
        Required(row.Source, "bat", b.Id, "id", b.Id, errors);
        Required(row.Source, "bat", b.Id, "name", b.Name, errors);
    }

    static void ValidateGlove(Sourced<GloveDto> row, List<string> errors)
    {
        var g = row.Value;
        Required(row.Source, "glove", g.Id, "id", g.Id, errors);
        Required(row.Source, "glove", g.Id, "name", g.Name, errors);
        FiniteRange(row.Source, $"glove '{g.Id}' errorReduction", g.ErrorReduction, 0, 1, errors);
    }

    static void ValidateChemistry(
        string table,
        IReadOnlyList<string[]> rows,
        string source,
        HashSet<string> characters,
        List<string> errors)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var pair = rows[i];
            if (pair is null)
            {
                errors.Add($"{source}: chemistry {table}[{i}] must be an array; got null");
                continue;
            }
            if (pair.Length != 2)
            {
                errors.Add($"{source}: chemistry {table}[{i}] must contain exactly two character ids; got {pair.Length}");
                continue;
            }
            for (var j = 0; j < pair.Length; j++)
            {
                if (!characters.Contains(pair[j]))
                    errors.Add($"{source}: chemistry {table}[{i}][{j}] references unknown character '{pair[j]}'");
            }
        }
    }

    static void Required(string source, string kind, string id, string field, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{source}: {kind} '{id}' {field} must not be empty");
    }

    static void Known(string source, string field, string value, HashSet<string> allowed, List<string> errors)
    {
        if (!allowed.Contains(value))
            errors.Add($"{source}: {field} must be one of [{string.Join(", ", allowed.OrderBy(x => x, StringComparer.Ordinal))}]; got '{value}'");
    }

    static void Reference(string source, string field, string value, HashSet<string> known, List<string> errors)
    {
        if (!known.Contains(value))
            errors.Add($"{source}: {field} references unknown id '{value}'");
    }

    static void Range(string source, string field, int value, int min, int max, List<string> errors)
    {
        if (value < min || value > max)
            errors.Add($"{source}: {field} must be between {min} and {max}; got {value}");
    }

    static void Positive(string source, string field, int value, List<string> errors)
    {
        if (value <= 0)
            errors.Add($"{source}: {field} must be greater than 0; got {value}");
    }

    static void Finite(string source, string field, double value, List<string> errors)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            errors.Add($"{source}: {field} must be finite; got {value}");
    }

    static void NonNegative(string source, string field, double value, List<string> errors)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            errors.Add($"{source}: {field} must be finite and at least 0; got {value}");
    }

    static void FiniteRange(string source, string field, double value, double min, double max, List<string> errors)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
            errors.Add($"{source}: {field} must be between {min} and {max}; got {value}");
    }
}

internal sealed class ContentData
{
    public List<Sourced<CharacterDto>> Characters { get; } = [];
    public List<Sourced<ParkDto>> Parks { get; } = [];
    public List<Sourced<BatDto>> Bats { get; } = [];
    public List<Sourced<GloveDto>> Gloves { get; } = [];
    public ChemistryOverrides Chemistry { get; set; } = new();
    public string ChemistrySource { get; set; } = "";
    public StarSkillsDto StarSkills { get; set; } = new();
    public string StarSkillsSource { get; set; } = "";
    public RulesTable? Rules { get; set; }
    public List<string> ReadErrors { get; } = [];
}

internal readonly record struct Sourced<T>(T Value, string Source);

internal sealed class CharacterDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public bool Captain { get; set; }
    public int Pitch { get; set; }
    public int Bat { get; set; }
    public int Field { get; set; }
    public int Run { get; set; }
    public string Bats { get; set; } = "";
    public string Throws { get; set; } = "";
    public string StarPitch { get; set; } = "";
    public string StarSwing { get; set; } = "";
    public string FieldAbility { get; set; } = "";
    public string Bio { get; set; } = "";

    public Character ToCharacter() => new(
        Id, Name, Faction, Captain,
        new Stats(Pitch, Bat, Field, Run),
        ParseHand(Bats), ParseHand(Throws),
        StarPitch, StarSwing, FieldAbility, Bio);

    static Hand ParseHand(string value) => value.Equals("L", StringComparison.OrdinalIgnoreCase) ? Hand.L : Hand.R;
}

internal sealed class ParkDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public string Surface { get; set; } = "";
    public int LeftFenceFt { get; set; }
    public int CenterFenceFt { get; set; }
    public int RightFenceFt { get; set; }
    public double WindMph { get; set; }
    /// <summary>Where the wind blows toward: 0 out to CF, 90 toward the right-field line, 180 in (docs/parks.md).</summary>
    public double WindDeg { get; set; }
    /// <summary>Outfield fence top. Below it the ball caroms; above it between the poles is a home run (§6.1).</summary>
    public double FenceHeightFt { get; set; }
    /// <summary>The contact window at night as a fraction of the day's (§14): a blackout park shrinks it; 1 (the default) is no change.</summary>
    public double NightContactWindowMul { get; set; } = 1.0;
    public List<HazardDto?>? Hazards { get; set; }

    public Park ToPark() => new(
        Id, Name, Faction, Surface,
        LeftFenceFt, CenterFenceFt, RightFenceFt, WindMph,
        (Hazards ?? []).Select(h => new Hazard(h!.Type, h.X, h.Z, h.Radius, h.Tag)).ToList(),
        WindDeg, FenceHeightFt, NightContactWindowMul);
}

internal sealed class HazardDto
{
    public string Type { get; set; } = "";
    public double X { get; set; }
    public double Z { get; set; }
    public double Radius { get; set; }
    public string? Tag { get; set; }
}

internal sealed class BatDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int ContactMod { get; set; }
    public int PowerMod { get; set; }
    public bool ChargeAlwaysFull { get; set; }
    public string Visual { get; set; } = "";
}

internal sealed class GloveDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double ErrorReduction { get; set; }
    public int ArmMod { get; set; }
    public string Visual { get; set; } = "";
}

internal sealed class StarSkillsDto
{
    public Dictionary<string, StarSkillDto?>? Pitches { get; set; }
    public Dictionary<string, StarSkillDto?>? Swings { get; set; }
}

internal sealed class StarSkillDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public double? SpeedMul { get; set; }
    public int? StaminaCost { get; set; }
    public double? BatterWindowMul { get; set; }
    public bool LateBreak { get; set; }
    public bool Decoy { get; set; }
    public string? OnCatch { get; set; }
    public double? ExitVeloMul { get; set; }
    public double? LaunchDeg { get; set; }
    public string? Terrain { get; set; }
    public double? FielderPauseSec { get; set; }
    public bool InfieldChaos { get; set; }
    public bool Fragments { get; set; }

    public StarPitchSkill ToPitch() => new(Id, Name, Kind, SpeedMul ?? 1.0, StaminaCost ?? 0,
        BatterWindowMul ?? 1.0, LateBreak, Decoy, OnCatch);

    public StarSwingSkill ToSwing() => new(Id, Name, Kind, ExitVeloMul ?? 1.0, LaunchDeg, Terrain,
        FielderPauseSec ?? 0, InfieldChaos, Decoy, Fragments);
}
