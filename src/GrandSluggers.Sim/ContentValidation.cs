using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace GrandSluggers.Sim;

/// <summary>
/// Validates authored gameplay data before it becomes a catalog. Art slots stay
/// permissive so a missing mesh or visual can still use the game's placeholders.
/// </summary>
public static class ContentDataValidator
{
    static readonly HashSet<string> Hands = new(StringComparer.OrdinalIgnoreCase) { "L", "R" };
    static readonly HashSet<string> FieldAbilityIds = new(StringComparer.Ordinal)
    {
        "ball-dash", "burrow", "clamber", "dive", "grow", "laser", "lick-catch",
        "snap-throw", "spin-check", "super-jump", "withdraw"
    };
    public static IReadOnlyCollection<string> TutorialFieldAbilities => FieldAbilityIds;

    /// <summary>
    /// The thickest air a park may name, as a multiple of the root's <c>flight.drag</c> (§0.3 FD-03).
    /// A sane envelope for a schema that no park uses yet — the compact trial's own drag is about 2.1×
    /// the shipped one — not a number anyone accepted for a park.
    /// </summary>
    const double MaxParkDragMul = 4;

    public static IReadOnlyList<string> Validate(DataRoot dataRoot)
    {
        var data = Read(dataRoot, JsonOptions());
        return Errors(data);
    }

    internal static ContentData Load(DataRoot dataRoot, JsonSerializerOptions json)
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

    static ContentData Read(DataRoot root, JsonSerializerOptions json)
    {
        var data = new ContentData();
        data.ReadErrors.AddRange(RaceEvidence.Validate(root));

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

        // Parks are read strictly (spec §16, FR-03): a key a park file does not declare is a stop,
        // the way it is for a rule table. Every other catalog stays permissive until its own child.
        ReadRows(root, "parks", data.Parks, json, data.ReadErrors, strict: true);
        ReadRows(root, "bats", data.Bats, json, data.ReadErrors);
        ReadRows(root, "gloves", data.Gloves, json, data.ReadErrors);

        var chemistryPath = root.Resolve("chemistry", "overrides.json");
        data.Chemistry = ReadJson<ChemistryOverrides>(chemistryPath, json, data.ReadErrors) ?? new();
        data.ChemistrySource = chemistryPath;

        var skillsPath = root.Resolve("abilities", "star-skills.json");
        data.StarSkills = ReadJson<StarSkillsDto>(skillsPath, json, data.ReadErrors) ?? new();
        data.StarSkillsSource = skillsPath;

        // Rule numbers (spec §16). Missing fields fall back to code; unknown fields and bad ranges are errors.
        data.Rules = RulesTable.Load(root, data.ReadErrors);
        // Resolved through the overlay like every other path, so a trial that carries its own ground
        // rows is the file a bad `surface` is reported against (FD-05, SF-03).
        data.GroundsSource = RulesTable.PathFor(root, "grounds");
        // The same for the wall-material library a fence span names (FD-06, SF-03).
        data.WallsSource = RulesTable.PathFor(root, "walls");
        return data;
    }

    static void ReadRows<T>(
        DataRoot root,
        string directory,
        List<Sourced<T>> destination,
        JsonSerializerOptions json,
        List<string> errors,
        bool strict = false) where T : class
    {
        foreach (var file in Files(root, directory, errors))
        {
            var row = ReadJson<T>(file, json, errors, strict);
            if (row is not null) destination.Add(new(row, file));
        }
    }

    /// <summary>
    /// A data directory's files, resolved one by one against the trial overlay. The listing and its
    /// order come from the shipped root, so a run reads the same set of rows whether or not a trial
    /// is named — only the contents of the files the trial carries change.
    /// </summary>
    static IReadOnlyList<string> Files(DataRoot root, string directory, List<string> errors)
    {
        var path = root.Resolve(directory);
        if (!Directory.Exists(path))
        {
            errors.Add($"{path}: required gameplay data directory is missing");
            return [];
        }
        return root.Files(directory, "*.json");
    }

    static T? ReadJson<T>(string path, JsonSerializerOptions json, List<string> errors, bool strict = false) where T : class
    {
        try
        {
            var text = File.ReadAllText(path);
            if (strict) UnknownKeys(text, typeof(T), path, errors);
            var value = JsonSerializer.Deserialize<T>(text, json);
            if (value is null) errors.Add($"{path}: JSON document is empty");
            return value;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            errors.Add($"{path}: cannot read gameplay data: {ex.Message}");
            return null;
        }
    }

    static readonly JsonDocumentOptions StrictJson = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The strict read (spec §16, FR-03, #820): a key the row type does not declare stops the load and
    /// names the file and the key, the way <see cref="RulesValidation.UnknownFields"/> does for a rule
    /// table. Before this, <c>System.Text.Json</c> dropped an unknown park key in silence, so a
    /// misspelled <c>centerFenceFt</c> played Harbor's fence and no test failed.
    ///
    /// Rows nested in a list (a park's hazards) are walked against their own row type; a malformed
    /// document is left to <see cref="JsonSerializer"/>, which reports it with the parser's message.
    /// </summary>
    static void UnknownKeys(string text, Type type, string source, List<string> errors)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(text, StrictJson); }
        catch (JsonException) { return; }
        using (doc)
            UnknownKeys(doc.RootElement, type, "", source, errors);
    }

    static void UnknownKeys(JsonElement element, Type type, string path, string source, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{source}: {(path.Length == 0 ? "the row" : path.TrimEnd('.'))} must be an object; got {element.ValueKind}");
            return;
        }
        var declared = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var field in element.EnumerateObject())
        {
            if (!declared.TryGetValue(field.Name, out var p))
            {
                var names = declared.Values.Select(x => Camel(x.Name)).OrderBy(x => x, StringComparer.Ordinal);
                errors.Add($"{source}: {path}{field.Name} is not a key this file declares; "
                    + $"the keys of {Camel(TypeLabel(type))} are [{string.Join(", ", names)}]");
                continue;
            }
            if (RowType(p.PropertyType) is { } row && field.Value.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var entry in field.Value.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Null)
                        UnknownKeys(entry, row, $"{path}{Camel(p.Name)}[{i}].", source, errors);
                    i++;
                }
            }
            else if (Nested(p.PropertyType))
                UnknownKeys(field.Value, p.PropertyType, $"{path}{Camel(p.Name)}.", source, errors);
        }
    }

    /// <summary>The row type behind a <c>List&lt;T?&gt;</c> of authored rows, else null.</summary>
    static Type? RowType(Type type)
    {
        if (!type.IsGenericType) return null;
        var arg = type.GetGenericArguments()[0];
        arg = Nullable.GetUnderlyingType(arg) ?? arg;
        return Nested(arg) ? arg : null;
    }

    static bool Nested(Type type) =>
        type.IsClass && type != typeof(string) && type.Namespace == typeof(ContentDataValidator).Namespace;

    static string TypeLabel(Type type) =>
        type.Name.EndsWith("Dto", StringComparison.Ordinal) ? type.Name[..^3] : type.Name;

    static string Camel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

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
        // The hazard type set is the library's table, not a list this file keeps (FD-09, FR-02), and
        // so is the ground set a park's surface and zones are checked against (FD-05, SF-03). Both are
        // the tables this root loaded, so a trial that authors either file is checked against its own
        // rows; a rules table that failed to load has already reported itself, and the code rows stand
        // in so a park's own mistakes are still named in the same pass.
        var hazards = data.Rules?.Hazards ?? RulesTable.Defaults.Hazards;
        var grounds = data.Rules?.Grounds ?? RulesTable.Defaults.Grounds;
        // Where a hazard may stand is measured on this root's own diamond (SF-23): the bags and the
        // rubber are the infield table this root loaded, so a trial's 80-ft diamond is the one its
        // parks are checked against, never the process-wide one.
        var infield = data.Rules?.Infield ?? RulesTable.Defaults.Infield;
        // A polyline fence (FD-06, F2-c) is checked against this root's own lip, rail and wall
        // library, so a block that is legal on the shipped field and not on a trial's is refused on
        // the trial, by name, rather than played.
        var fence = new FenceLimits(
            (data.Rules?.Flight ?? RulesTable.Defaults.Flight).Classes.InfieldLipFt,
            (data.Rules?.Boundary ?? RulesTable.Defaults.Boundary).RailHeightFt,
            data.Rules?.Walls ?? RulesTable.Defaults.Walls,
            data.WallsSource);
        foreach (var row in data.Parks)
            ValidatePark(row, hazards, grounds, data.GroundsSource, infield, fence, errors);
        UniquePerPark("pickOrder", data.Parks.Where(r => r.Value.PickOrder is not null)
            .Select(r => (r.Value.PickOrder!.Value.ToString(), r.Source)), errors);
        UniquePerPark("faction", data.Parks
            .Where(r => !string.IsNullOrWhiteSpace(r.Value.Faction))
            .Select(r => (r.Value.Faction, r.Source)), errors);
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

    /// <summary>
    /// A park field that has to be one park's alone (#820): the field-pick order, so the cycle is a
    /// declared sequence rather than a directory listing, and the faction, so a captain's home park
    /// is one park and never a coin toss between two. Both errors name every file that shares the value.
    /// </summary>
    static void UniquePerPark(string field, IEnumerable<(string Value, string Source)> candidates, List<string> errors)
    {
        foreach (var group in candidates
            .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var sources = group.Select(x => x.Source).OrderBy(x => x, StringComparer.Ordinal);
            errors.Add($"park {field} '{group.Key}' is claimed by more than one park: {string.Join("; ", sources)}");
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
        // Arm, hands and reach are optional: absent means seeded from field / the legacy radius.
        if (c.Arm != 0) Range(row.Source, $"character '{c.Id}' arm", c.Arm, 1, 10, errors);
        if (c.Hands != 0) Range(row.Source, $"character '{c.Id}' hands", c.Hands, 1, 10, errors);
        // Contact and power are optional the same way: absent means seeded from bat (PH-15-R5).
        if (c.Contact != 0) Range(row.Source, $"character '{c.Id}' contact", c.Contact, 1, 10, errors);
        if (c.Power != 0) Range(row.Source, $"character '{c.Id}' power", c.Power, 1, 10, errors);
        // The four pitching ratings are optional the same way: absent means seeded from pitch (PH-15-R6).
        if (c.Velocity != 0) Range(row.Source, $"character '{c.Id}' velocity", c.Velocity, 1, 10, errors);
        if (c.Movement != 0) Range(row.Source, $"character '{c.Id}' movement", c.Movement, 1, 10, errors);
        if (c.Control != 0) Range(row.Source, $"character '{c.Id}' control", c.Control, 1, 10, errors);
        if (c.Endurance != 0) Range(row.Source, $"character '{c.Id}' endurance", c.Endurance, 1, 10, errors);
        if (c.ReachFt is { } reach && reach <= 0)
            errors.Add($"{row.Source}: character '{c.Id}' reachFt must be positive when present");
        Known(row.Source, $"character '{c.Id}' bats", c.Bats, Hands, errors);
        Known(row.Source, $"character '{c.Id}' throws", c.Throws, Hands, errors);
        Known(row.Source, $"character '{c.Id}' fieldAbility", c.FieldAbility, FieldAbilityIds, errors);
        ValidateRepertoire(row.Source, c, errors);
        Reference(row.Source, $"character '{c.Id}' starPitch", c.StarPitch, pitches, errors);
        Reference(row.Source, $"character '{c.Id}' starSwing", c.StarSwing, swings, errors);
    }

    /// <summary>
    /// The ordinary repertoire (spec §4.3, PH-15-R1): exactly two different assignable families
    /// beside the implied fastball, in accepted order.
    ///
    /// Required rather than optional, because the character loader ignores keys it does not know:
    /// a row that spells the key <c>repertoir</c> would load as a character with no repertoire and
    /// quietly take the fixture default, so "missing" has to be an error of its own.
    ///
    /// Star pitch ids are a different namespace and are checked against
    /// <c>data/abilities/star-skills.json</c> above. A star id that is not a family — <c>breaker</c>,
    /// <c>heatball</c> — is refused here for the same reason any other unknown id is: it is not in
    /// <see cref="PitchFamily.Assignable"/>.
    /// </summary>
    static void ValidateRepertoire(string source, CharacterDto c, List<string> errors)
    {
        var families = string.Join(", ", PitchFamily.Assignable);
        var rows = c.Repertoire;
        if (rows is null)
        {
            errors.Add($"{source}: character '{c.Id}' repertoire must list the two ordinary pitches "
                + $"beside the fastball, each one of [{families}]; the key is missing");
            return;
        }
        if (rows.Count != 2)
        {
            errors.Add($"{source}: character '{c.Id}' repertoire must name exactly 2 ordinary pitches "
                + $"beside the fastball; got {rows.Count}");
            return;
        }
        for (var i = 0; i < rows.Count; i++)
        {
            var family = rows[i] ?? "";
            if (string.Equals(family, PitchFamily.Fastball, StringComparison.Ordinal))
                errors.Add($"{source}: character '{c.Id}' repertoire[{i}] must not name 'fastball'; "
                    + "every pitcher throws it and it is never authored");
            else if (!PitchFamily.IsAssignable(family))
                errors.Add($"{source}: character '{c.Id}' repertoire[{i}] must be one of [{families}]; got '{family}'");
        }
        if (string.Equals(rows[0], rows[1], StringComparison.Ordinal))
            errors.Add($"{source}: character '{c.Id}' repertoire names '{rows[0]}' twice; "
                + "the two ordinary pitches beside the fastball are different families");
    }

    static void ValidatePark(
        Sourced<ParkDto> row, HazardRules hazards, GroundLibrary grounds, string groundsSource, InfieldRules infield,
        FenceLimits fence, List<string> errors)
    {
        var p = row.Value;
        Required(row.Source, "park", p.Id, "id", p.Id, errors);
        Required(row.Source, "park", p.Id, "name", p.Name, errors);
        Required(row.Source, "park", p.Id, "faction", p.Faction, errors);
        // The field-pick cycle is authored, not alphabetical (#820): a park that names no place in it
        // would take whichever place the filesystem happened to list it in.
        if (p.PickOrder is not { } pick)
            errors.Add($"{row.Source}: park '{p.Id}' pickOrder must name its place in the field-pick cycle; got none");
        else if (pick < 1)
            errors.Add($"{row.Source}: park '{p.Id}' pickOrder must be at least 1; got {pick}");
        // `surface` names the park's outfield ground (and, through the derived map, its foul apron).
        // It used to be checked against a set spelled out in this file; since F3-b the only thing that
        // decides whether a surface exists is whether the ground library has a row for it (FD-05).
        GroundId(row.Source, $"park '{p.Id}' surface", p.Surface, grounds, groundsSource, errors);
        Positive(row.Source, $"park '{p.Id}' leftFenceFt", p.LeftFenceFt, errors);
        Positive(row.Source, $"park '{p.Id}' centerFenceFt", p.CenterFenceFt, errors);
        Positive(row.Source, $"park '{p.Id}' rightFenceFt", p.RightFenceFt, errors);
        Finite(row.Source, $"park '{p.Id}' windMph", p.WindMph, errors);
        FiniteRange(row.Source, $"park '{p.Id}' windDeg", p.WindDeg, -360, 360, errors);
        if (!(p.FenceHeightFt > 0) || double.IsNaN(p.FenceHeightFt) || double.IsInfinity(p.FenceHeightFt))
            errors.Add($"{row.Source}: park '{p.Id}' fenceHeightFt must be greater than 0; got {p.FenceHeightFt}");
        // The floor is the boundary table's rail, not a park's class (#826, FR-05): every park's
        // fence stands over the rail the ball meets, and no park's name decides another park's rule.
        else if (p.FenceHeightFt <= ParkBoundary.Default.RailHeightFt)
            errors.Add($"{row.Source}: park '{p.Id}' fenceHeightFt must stand over the {ParkBoundary.Default.RailHeightFt} ft foul rail (the drawn wall ramps up to it, D15); got {p.FenceHeightFt}");
        if (!(p.NightContactWindowMul > 0 && p.NightContactWindowMul <= 1))
            errors.Add($"{row.Source}: park '{p.Id}' nightContactWindowMul must be in (0, 1]; got {p.NightContactWindowMul}");
        ValidateParkEnvironment(row.Source, p.Id, p.Environment, errors);
        ValidateParkZones(row.Source, p.Id, p.Zones, grounds, groundsSource, errors);
        ValidateParkFence(row.Source, p, fence, errors);
        for (var i = 0; i < (p.Hazards?.Count ?? 0); i++)
        {
            var h = p.Hazards![i];
            var where = $"park '{p.Id}' hazard[{i}]";
            if (h is null)
            {
                errors.Add($"{row.Source}: {where} must be an object; got null");
                continue;
            }
            Finite(row.Source, where + " x", h.X, errors);
            Finite(row.Source, where + " z", h.Z, errors);
            NonNegative(row.Source, where + " radius", h.Radius, errors);
            // The set of types is derived from the hazard library's table (SF-03, FD-09): an id
            // outside the library and an id inside it with no authored row are different mistakes,
            // and each is named for what it is.
            if (!HazardType.IsKnown(h.Type))
            {
                errors.Add($"{row.Source}: {where} type must be one of "
                    + $"[{string.Join(", ", hazards.Authored.OrderBy(x => x, StringComparer.Ordinal))}]; got '{h.Type}'");
                continue;
            }
            if (!hazards.IsAuthored(h.Type))
            {
                errors.Add($"{row.Source}: {where} type '{h.Type}' is in the library but has no authored row "
                    + "in hazards.json; every hazard type a park may name carries a row (FD-09)");
                continue;
            }
            // A disc that acts needs a disc. A decoration is drawn and never played, so it may have
            // none at all — which is why Funfair's boxcar no longer needs a special case by name.
            if (h.Radius == 0 && hazards.Of(h.Type).Pattern != HazardPattern.Decoration)
                errors.Add($"{row.Source}: {where} radius must be greater than 0; got {h.Radius}");
            HazardPlace(row.Source, where, h, infield, errors);
        }
    }

    /// <summary>
    /// <c>SF-23</c> (§14, FD-19): a hazard stays off the running lanes, the mound-to-plate lane, the bag
    /// pads, the mound and the plate area. One refusal per hazard, naming every piece of ground its disc
    /// crosses and by how many feet, deepest first, so the author can see how far it has to go. A disc
    /// whose centre or radius is not a number has already been refused for that, and is not measured.
    /// </summary>
    static void HazardPlace(string source, string where, HazardDto h, InfieldRules infield, List<string> errors)
    {
        if (!double.IsFinite(h.X) || !double.IsFinite(h.Z) || !double.IsFinite(h.Radius) || h.Radius < 0) return;
        var crossings = HazardPlacement.Crossings(h.X, h.Z, h.Radius, infield);
        if (crossings.Count == 0) return;
        var what = string.Join(", ", crossings.Select(c =>
            $"{c.What} by {c.ByFt.ToString("0.00", CultureInfo.InvariantCulture)} ft"));
        errors.Add($"{source}: {where} {h.Type} at ({h.X.ToString(CultureInfo.InvariantCulture)}, "
            + $"{h.Z.ToString(CultureInfo.InvariantCulture)}) radius {h.Radius.ToString(CultureInfo.InvariantCulture)} "
            + $"crosses {what}; a hazard stays off the running lanes, the mound-to-plate lane, the bag pads, "
            + "the mound and the plate area (FD-19, SF-23)");
    }

    /// <summary>
    /// The park's air (§0.3 FD-03, §16): the block and both of its fields are optional, and a park that
    /// names neither plays the global table. A named field has to be a number the flight can fly —
    /// <see cref="MaxParkDragMul"/> is the sane envelope the schema refuses past, not an accepted value.
    /// No park names one; the first number is a trial, not a validator change.
    /// </summary>
    static void ValidateParkEnvironment(string source, string id, ParkEnvironmentDto? env, List<string> errors)
    {
        if (env is null) return;
        if (env.DragMul is { } drag && !(drag > 0 && drag <= MaxParkDragMul))
            errors.Add($"{source}: park '{id}' environment.dragMul must be greater than 0 and at most {MaxParkDragMul}; got {drag}");
        if (env.WindMul is { } wind)
            FiniteRange(source, $"park '{id}' environment.windMul", wind, 0, 1, errors);
    }

    /// <summary>
    /// The park's ground (§6.1, FD-05, <c>SF-03</c>): the block and each of its four zones are
    /// optional, and a zone the park does not name is the one derived from <c>surface</c>
    /// (<see cref="GroundZones.Of(Park, RulesTable?)"/>). A zone that <em>is</em> named has to name a
    /// ground the library has a row for — there is no silent fallback to grass, because a park rolling
    /// on a ground nobody authored is exactly the drift FR-02 exists to stop. No park names one.
    /// </summary>
    static void ValidateParkZones(
        string source, string id, ParkZonesDto? zones, GroundLibrary grounds, string groundsSource, List<string> errors)
    {
        if (zones is null) return;
        foreach (var (zone, named) in zones.Named())
        {
            if (named is null) continue;
            GroundId(source, $"park '{id}' zones.{zone}", named, grounds, groundsSource, errors);
        }
    }

    /// <summary>What a polyline fence is checked against on one data root: its lip, its rail and its wall library.</summary>
    readonly record struct FenceLimits(double LipFt, double RailFt, WallMaterialLibrary Walls, string WallsSource);

    /// <summary>
    /// The park's polyline fence (§6.1, §16; FD-06 C, FD-06-R1, FD-12 B; F2-c). The block is optional
    /// and no park names one. When a park does, every refusal names the park, the point or span by index,
    /// and the reason, on whichever root the file was authored in:
    /// <list type="bullet">
    /// <item>fewer than two points, or a point that is not an object;</item>
    /// <item>a first point that is not on the left-field line (−45) or a last that is not on the right
    /// (+45): the fence runs from pole to pole, where the foul rail meets it;</item>
    /// <item>a bearing that is not greater than the one before it — equal is a fence behind a fence
    /// (two distances on one bearing), smaller is an overhang (the fence folds back). With every bearing
    /// strictly increasing inside ±45°, each ray from home meets exactly one span, which is what keeps
    /// <see cref="AtBatResolver.FenceAt"/> a function of the bearing (FD-06-R1);</item>
    /// <item>a <c>fenceFrac</c> that is not a positive number, and a point — or any part of a span
    /// between two points, since a long chord can dip nearer home than either end — inside this root's
    /// infield lip (<c>flight.classes.infieldLipFt</c>), measured on the park's own three-post fence;</item>
    /// <item>a height that does not stand over this root's foul rail (the floor <c>fenceHeightFt</c> has
    /// always had, D15);</item>
    /// <item>a span material with no row in this root's <c>walls.json</c> (<c>SF-03</c>), and a material
    /// on the right-field pole's point, which starts no span.</item>
    /// </list>
    /// </summary>
    static void ValidateParkFence(string source, ParkDto p, FenceLimits limits, List<string> errors)
    {
        if (p.Fence is not { } fence) return;
        var where = $"park '{p.Id}' fence.points";
        var points = fence.Points;
        if (points is null || points.Count < 2)
        {
            errors.Add($"{source}: {where} must list at least 2 points, from the left-field pole (bearingDeg -45) "
                + $"to the right-field pole (bearingDeg 45); got {points?.Count ?? 0}");
            return;
        }

        var last = points.Count - 1;
        var bearing = new double?[points.Count];
        var frac = new double?[points.Count];
        for (var k = 0; k <= last; k++)
        {
            var at = $"{where}[{k}]";
            if (points[k] is not { } q)
            {
                errors.Add($"{source}: {at} must be an object; got null");
                continue;
            }
            if (q.BearingDeg is { } b && double.IsFinite(b)) bearing[k] = b;
            else errors.Add($"{source}: {at} bearingDeg must be a finite number of degrees from centre field "
                + $"(-45 the left-field line, 45 the right); got {Got(q.BearingDeg)}");
            if (q.FenceFrac is { } f && double.IsFinite(f) && f > 0) frac[k] = f;
            else errors.Add($"{source}: {at} fenceFrac must be greater than 0 (the fraction of the park's "
                + $"three-post fence at that bearing); got {Got(q.FenceFrac)}");
            if (q.HeightFt is not { } h || !double.IsFinite(h))
                errors.Add($"{source}: {at} heightFt must be a finite number of feet; got {Got(q.HeightFt)}");
            else if (h <= limits.RailFt)
                errors.Add($"{source}: {at} heightFt must stand over the {Show(limits.RailFt)} ft foul rail (D15); got {Show(h)}");
            if (q.Material is not { } material) continue;
            if (k == last)
                errors.Add($"{source}: {at} material names the span from a point to the next one, and the "
                    + $"right-field pole's point ends the fence; got '{material}'");
            else if (!limits.Walls.Has(material))
                errors.Add($"{source}: {at} material must be a wall material with a row in {limits.WallsSource}; "
                    + $"the library is [{string.Join(", ", limits.Walls.Ids)}]; got '{material}'");
        }

        if (bearing[0] is { } first && first != -AtBatResolver.FoulLineDeg)
            errors.Add($"{source}: {where}[0] bearingDeg must be -45, the left-field line: the fence starts at the "
                + $"left-field pole, where the foul rail meets it; got {Show(first)}");
        if (bearing[last] is { } end && end != AtBatResolver.FoulLineDeg)
            errors.Add($"{source}: {where}[{last}] bearingDeg must be 45, the right-field line: the fence ends at the "
                + $"right-field pole, where the foul rail meets it; got {Show(end)}");
        for (var k = 1; k <= last; k++)
        {
            if (bearing[k] is not { } b || bearing[k - 1] is not { } before) continue;
            if (b == before)
                errors.Add($"{source}: {where}[{k}] bearingDeg {Show(b)} is points[{k - 1}]'s bearing: two fence "
                    + "distances on one bearing is a fence behind a fence, and the fence keeps one distance per "
                    + "bearing from home (FD-06-R1)");
            else if (b < before)
                errors.Add($"{source}: {where}[{k}] bearingDeg {Show(b)} is left of points[{k - 1}]'s {Show(before)}: "
                    + "the fence would fold back over itself, an overhang; bearings run strictly from the left-field "
                    + "pole to the right (FD-06-R1)");
        }

        // Where each point stands is a fraction of the park's own three-post fence (FD-12), so the lip
        // can only be measured on posts that are themselves legal.
        if (p.LeftFenceFt <= 0 || p.CenterFenceFt <= 0 || p.RightFenceFt <= 0) return;
        var posts = new Park(p.Id, p.Name, p.Faction, p.Surface, p.LeftFenceFt, p.CenterFenceFt, p.RightFenceFt, 0, []);
        var place = new (double X, double Z)?[points.Count];
        for (var k = 0; k <= last; k++)
        {
            if (bearing[k] is not { } b || frac[k] is not { } f) continue;
            var circle = AtBatResolver.FenceAt(posts, b);
            var ft = f * circle;
            place[k] = BallFlight.GroundPoint(ft, b);
            if (ft < limits.LipFt)
                errors.Add($"{source}: {where}[{k}] stands {Show(ft)} ft from home ({Show(f)} of the park's "
                    + $"{Show(circle)} ft fence at {Show(b)} degrees), inside the {Show(limits.LipFt)} ft infield lip; "
                    + "the fence stands beyond the infield");
        }
        for (var k = 0; k < last; k++)
        {
            if (place[k] is not { } a || place[k + 1] is not { } z) continue;
            if (!(bearing[k + 1] > bearing[k])) continue;
            var nearest = Nearest(a, z);
            if (nearest < limits.LipFt
                && Diamond.Dist(0, 0, a.X, a.Z) >= limits.LipFt && Diamond.Dist(0, 0, z.X, z.Z) >= limits.LipFt)
                errors.Add($"{source}: {where}[{k}] to [{k + 1}] runs {Show(nearest)} ft from home at its nearest, "
                    + $"inside the {Show(limits.LipFt)} ft infield lip; a span is straight between its points, so a "
                    + "long one dips nearer home than either end");
        }

        static double Nearest((double X, double Z) a, (double X, double Z) z)
        {
            var ex = z.X - a.X;
            var ez = z.Z - a.Z;
            var length2 = ex * ex + ez * ez;
            var t = length2 == 0 ? 0 : Math.Clamp(-(a.X * ex + a.Z * ez) / length2, 0, 1);
            return Diamond.Dist(0, 0, a.X + t * ex, a.Z + t * ez);
        }
    }

    static string Show(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static string Got(double? value) => value is { } v ? v.ToString(CultureInfo.InvariantCulture) : "none";

    /// <summary>
    /// A ground id with no row is a stop that names both files: the park file the id was written in,
    /// and the library file the rows live in (<c>SF-03</c>). Two files, because the reader either
    /// misspelled the id or has not authored the row yet, and the message should not make them guess
    /// which.
    /// </summary>
    static void GroundId(
        string source, string field, string value, GroundLibrary grounds, string groundsSource, List<string> errors)
    {
        if (grounds.Has(value)) return;
        errors.Add($"{source}: {field} must be a ground with a row in {groundsSource}; "
            + $"the library is [{string.Join(", ", grounds.Ids)}]; got '{value}'");
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

/// <summary>
/// Where a hazard may stand (§14, §0.3 FD-19, <c>SF-23</c>). A hazard may sit anywhere except the four
/// running lanes, the mound-to-plate lane, the three bag pads, the mound and the plate area; a disc that
/// crosses any of them is refused on whichever data root it was authored in.
///
/// <para>
/// <b>No new number.</b> The ground a body needs is the ground the diamond already draws: a lane is
/// <see cref="ParkDiamond.PathWidth"/> wide, centred on the line from bag to bag (and from the rubber to
/// the plate), a pad is <see cref="ParkDiamond.BagPadR"/> round a bag, the mound is
/// <see cref="ParkDiamond.MoundR"/> round the rubber and the plate area is
/// <see cref="ParkDiamond.HomePackedR"/> round the plate. Those are feet of body and equipment and do not
/// scale. Where the bags and the rubber <i>are</i> is the root's own <c>infield.json</c>
/// (<c>cornerFt</c>, <c>secondFt</c>, <c>moundFt</c>), handed in, so the 80-ft trial is checked on its
/// own diamond. The mound is the <c>float</c> 9.2 the hill has always stood at.
/// </para>
///
/// <para>
/// <b>The disc is the hazard's own radius</b> — the thing that stands on the field where a runner or a
/// fielder would meet it. A <c>ballRedirect</c> row's <c>reachPadFt</c> is not counted: it is how near a
/// <i>ball</i> must land for the can to take it (#732), not ground a body stands on, and FD-19 keeps
/// shallow ball hazards legal. A <c>decoration</c> with no radius is a point. There is no moving hazard
/// yet (F4-f); when there is, every point of its path is checked by this same function.
/// </para>
/// </summary>
public static class HazardPlacement
{
    /// <summary>One piece of protected ground a disc crosses, and by how many feet.</summary>
    public readonly record struct Crossing(string What, double ByFt);

    /// <summary>
    /// Every piece of protected ground the disc at (<paramref name="x"/>, <paramref name="z"/>) of
    /// <paramref name="radiusFt"/> crosses, deepest first. Empty is a legal place. A disc that only
    /// touches the edge is legal.
    /// </summary>
    public static IReadOnlyList<Crossing> Crossings(double x, double z, double radiusFt, InfieldRules infield) =>
        Ground(infield)
            .Select(g => new Crossing(g.What, radiusFt + g.HalfWidthFt - g.DistanceFt(x, z)))
            .Where(c => c.ByFt > 0)
            .OrderByDescending(c => c.ByFt)
            .ToList();

    /// <summary>
    /// How far the disc stands from the nearest protected ground, in feet: positive is clear by that
    /// much, zero touches, negative crosses by that much.
    /// </summary>
    public static double ClearanceFt(double x, double z, double radiusFt, InfieldRules infield) =>
        Ground(infield).Min(g => g.DistanceFt(x, z) - g.HalfWidthFt - radiusFt);

    /// <summary>A lane is a segment with its half-width; a pad is a point with its radius.</summary>
    readonly record struct Protected(string What, double Ax, double Az, double Bx, double Bz, double HalfWidthFt)
    {
        public double DistanceFt(double x, double z)
        {
            var dx = Bx - Ax;
            var dz = Bz - Az;
            var length2 = dx * dx + dz * dz;
            var t = length2 == 0 ? 0 : Math.Clamp(((x - Ax) * dx + (z - Az) * dz) / length2, 0, 1);
            return Diamond.Dist(x, z, Ax + t * dx, Az + t * dz);
        }
    }

    static Protected[] Ground(InfieldRules infield)
    {
        var lane = ParkDiamond.PathWidth * 0.5;
        var corner = infield.CornerFt;
        var second = infield.SecondFt;
        var rubber = infield.MoundFt;
        return
        [
            new("the home-first lane", 0, 0, corner, corner, lane),
            new("the first-second lane", corner, corner, 0, second, lane),
            new("the second-third lane", 0, second, -corner, corner, lane),
            new("the third-home lane", -corner, corner, 0, 0, lane),
            new("the mound-to-plate lane", 0, rubber, 0, 0, lane),
            new("first base's pad", corner, corner, corner, corner, ParkDiamond.BagPadR),
            new("second base's pad", 0, second, 0, second, ParkDiamond.BagPadR),
            new("third base's pad", -corner, corner, -corner, corner, ParkDiamond.BagPadR),
            new("the mound", 0, rubber, 0, rubber, ParkDiamond.MoundR),
            new("the plate area", 0, 0, 0, 0, ParkDiamond.HomePackedR)
        ];
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
    /// <summary>Where this root's ground library was read from — named by every <c>SF-03</c> refusal.</summary>
    public string GroundsSource { get; set; } = "";
    /// <summary>Where this root's wall-material library was read from — named by a fence span's <c>SF-03</c> refusal.</summary>
    public string WallsSource { get; set; } = "";
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

    /// <summary>Explicit throwing rating. Absent seeds from <see cref="Field"/> (F693-02-defensive-trait-mapping).</summary>
    public int Arm { get; set; }

    /// <summary>Explicit handling rating. Absent seeds from <see cref="Field"/>.</summary>
    public int Hands { get; set; }

    /// <summary>Explicit contact rating. Absent seeds from <see cref="Bat"/> (PH-15-R5).</summary>
    public int Contact { get; set; }

    /// <summary>Explicit power rating. Absent seeds from <see cref="Bat"/> (PH-15-R5).</summary>
    public int Power { get; set; }

    /// <summary>Explicit velocity rating. Absent seeds from <see cref="Pitch"/> (PH-15-R6).</summary>
    public int Velocity { get; set; }

    /// <summary>Explicit movement rating. Absent seeds from <see cref="Pitch"/> (PH-15-R6).</summary>
    public int Movement { get; set; }

    /// <summary>Explicit control rating. Absent seeds from <see cref="Pitch"/> (PH-15-R6).</summary>
    public int Control { get; set; }

    /// <summary>Explicit endurance rating. Absent seeds from <see cref="Pitch"/> (PH-15-R6).</summary>
    public int Endurance { get; set; }

    /// <summary>Authored stand-up catch reach in feet. Absent keeps the legacy radius formula.</summary>
    public double? ReachFt { get; set; }

    public string Bats { get; set; } = "";
    public string Throws { get; set; } = "";

    /// <summary>
    /// The two ordinary pitches beside the implied fastball (#807), lowercase family ids in accepted
    /// order. Null means the row did not author the key at all, which is an error rather than a
    /// default: the loader ignores unknown JSON keys, so a misspelled <c>repertoir</c> would
    /// otherwise pass as a character with no repertoire.
    /// </summary>
    public List<string?>? Repertoire { get; set; }

    public string StarPitch { get; set; } = "";
    public string StarSwing { get; set; } = "";
    public string FieldAbility { get; set; } = "";
    public string Bio { get; set; } = "";

    public Character ToCharacter() => new(
        Id, Name, Faction, Captain,
        new Stats(Pitch, Bat, Field, Run) { Arm = Arm, Hands = Hands, Contact = Contact, Power = Power,
            Velocity = Velocity, Movement = Movement, Control = Control, Endurance = Endurance },
        ParseHand(Bats), ParseHand(Throws),
        StarPitch, StarSwing, FieldAbility, Bio, ReachFt)
    {
        Repertoire = ParseRepertoire()
    };

    /// <summary>
    /// Never falls back to <see cref="Sim.Repertoire.Default"/>: a catalog is only built after
    /// <see cref="ContentDataValidator.Load"/> has reported every bad row by name, so a row that
    /// reaches here without two families is a loader bug, not a roster that should quietly throw
    /// somebody else's pitches.
    /// </summary>
    Repertoire ParseRepertoire() =>
        Repertoire is { Count: 2 } rows && rows[0] is { } second && rows[1] is { } third
            ? new Repertoire(second, third)
            : throw new InvalidDataException(
                $"character '{Id}' repertoire must name exactly two ordinary pitches; "
                + "the content validator reports this before a catalog is built");

    static Hand ParseHand(string value) => value.Equals("L", StringComparison.OrdinalIgnoreCase) ? Hand.L : Hand.R;
}

internal sealed class ParkDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>
    /// Whose park it is. A captain whose faction is this one plays here at home
    /// (<see cref="ContentCatalog.HomeParkIdOfFaction"/>); the validator refuses two parks with one faction.
    /// </summary>
    public string Faction { get; set; } = "";
    /// <summary>
    /// This park's place in the field-pick cycle (<see cref="ContentCatalog.ParkPickOrder"/>). Required and
    /// unique: a directory listing is alphabetical, and the pregame cycle is an authored sequence (#820).
    /// </summary>
    public int? PickOrder { get; set; }
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
    /// <summary>
    /// Authored prose about the park, for whoever opens the file. Declared here so the strict read
    /// accepts it, and deliberately off <see cref="Park"/>: no rule reads it, and a member on the record
    /// would enter <see cref="PlayTraceIdentity"/> and move every stored identity SHA (#820).
    /// </summary>
    public string? Notes { get; set; }
    /// <summary>
    /// What this park changes about the ball's air (§0.3 FD-03, #827). Optional, and absent is the global
    /// table. Unlike <see cref="Notes"/> this one <em>is</em> on <see cref="Park"/>, because the flight
    /// reads it: a match resolves it into its own table (<see cref="RulesTable.AtPark"/>), and evidence
    /// taken in one park's air must not be readable as another's. A park that names none writes nothing
    /// into the identity, so no stored SHA moves.
    /// </summary>
    public ParkEnvironmentDto? Environment { get; set; }

    /// <summary>
    /// Which ground each of this park's four zones names (§6.1, FD-05, #846). Optional, and absent is
    /// the map derived from <see cref="Surface"/>. Like <see cref="Environment"/> and unlike
    /// <see cref="Notes"/> it <em>is</em> carried onto <see cref="Park"/>, because the ball will read
    /// it (F3-c): evidence taken on one park's ground must not be readable as another's. A park that
    /// names none writes nothing into <see cref="PlayTraceIdentity"/>, so no stored SHA moves.
    /// </summary>
    public ParkZonesDto? Zones { get; set; }

    /// <summary>
    /// The outfield fence as a polyline (§6.1, §16; FD-06, FD-06-R1, FD-12; F2-c). Optional, and absent is
    /// the circle through the three posts at <see cref="FenceHeightFt"/>. Carried onto <see cref="Park"/>
    /// because the flight, the zone map and the drawn wall read it; a park that names none writes nothing
    /// into <see cref="PlayTraceIdentity"/>, so no stored SHA moves. No shipped or trial park names one.
    /// </summary>
    public ParkFenceDto? Fence { get; set; }

    public Park ToPark() => new(
        Id, Name, Faction, Surface,
        LeftFenceFt, CenterFenceFt, RightFenceFt, WindMph,
        (Hazards ?? []).Select(h => new Hazard(h!.Type, h.X, h.Z, h.Radius, h.Tag)).ToList(),
        WindDeg, FenceHeightFt, NightContactWindowMul, Environment?.ToEnvironment(), Zones?.ToZones(),
        Fence?.ToFence());
}

/// <summary>
/// The optional <c>fence</c> block of a park file (§6.1, §16; FD-06 C, FD-12 B). One key, <c>points</c>,
/// from the left-field pole to the right-field pole. Inside the strict park read (#820), so a misspelled
/// key in the block or in a point is refused by name rather than dropped.
/// </summary>
internal sealed class ParkFenceDto
{
    public List<FencePointDto?>? Points { get; set; }

    /// <summary>Only after <see cref="ContentDataValidator"/> has accepted the block, which is when a catalog is built.</summary>
    public ParkFence ToFence() => new((Points ?? []).Select(p => p!.ToPoint()).ToList());
}

/// <summary>
/// One point of a park's fence (FD-06 C, FD-12 B). The three numbers are nullable so a missing one is
/// refused by name instead of reading as 0.
/// </summary>
internal sealed class FencePointDto
{
    /// <summary>Degrees from centre field: −45 the left-field line, 45 the right.</summary>
    public double? BearingDeg { get; set; }

    /// <summary>The distance from home as a fraction of the park's three-post fence at that bearing.</summary>
    public double? FenceFrac { get; set; }

    /// <summary>The fence top at this point, in feet (not scaled between roots).</summary>
    public double? HeightFt { get; set; }

    /// <summary>The wall material of the span from this point to the next. Absent is <c>padded</c>.</summary>
    public string? Material { get; set; }

    public FencePoint ToPoint() => new(BearingDeg!.Value, FenceFrac!.Value, HeightFt!.Value, Material);
}

/// <summary>
/// The optional <c>zones</c> block of a park file (§6.1, §16; FD-05). Four optional ground ids: an
/// absent zone is the one derived from <c>surface</c>, never an empty string. The block sits inside
/// the strict park read (#820), so a misspelled zone name is refused by name rather than quietly
/// leaving the park on its derived map.
/// </summary>
internal sealed class ParkZonesDto
{
    /// <summary>The ground inside the infield lip. Absent is <c>dirt</c>.</summary>
    public string? InfieldDirt { get; set; }

    /// <summary>The ground past the lip. Absent is the park's <c>surface</c>.</summary>
    public string? Outfield { get; set; }

    /// <summary>The ground within a track width of the fence. Absent is <c>dirt</c>.</summary>
    public string? WarningTrack { get; set; }

    /// <summary>The ground outside the chalk. Absent is the park's <c>surface</c>.</summary>
    public string? FoulApron { get; set; }

    /// <summary>The zones this block names, by the name the file spells, for the validator's messages.</summary>
    public IEnumerable<(string Zone, string? Ground)> Named()
    {
        yield return ("infieldDirt", InfieldDirt);
        yield return ("outfield", Outfield);
        yield return ("warningTrack", WarningTrack);
        yield return ("foulApron", FoulApron);
    }

    public ParkZones ToZones() => new(InfieldDirt, Outfield, WarningTrack, FoulApron);
}

/// <summary>
/// The optional <c>environment</c> block of a park file (§0.3 FD-03, §16). Air only, both fields optional:
/// an absent field is the global table's number, never a zero.
/// </summary>
internal sealed class ParkEnvironmentDto
{
    /// <summary>Multiplies the root's <c>flight.drag</c>.</summary>
    public double? DragMul { get; set; }
    /// <summary>Replaces <c>flight.windMul</c>.</summary>
    public double? WindMul { get; set; }

    public ParkEnvironment ToEnvironment() => new(DragMul, WindMul);
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
