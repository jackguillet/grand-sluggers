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
        var data = Read(dataRoot);
        return Errors(data);
    }

    internal static ContentData Load(DataRoot dataRoot)
    {
        var data = Read(dataRoot);
        var errors = Errors(data);
        if (errors.Count > 0)
            throw new InvalidDataException("Invalid gameplay content:" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        return data;
    }

    static ContentData Read(DataRoot root)
    {
        var data = new ContentData();

        foreach (var file in Files(root, "characters", data.ReadErrors))
        {
            if (Path.GetFileName(file).Equals("role-players.json", StringComparison.OrdinalIgnoreCase))
            {
                var rows = DataJson.Read<List<CharacterDto?>>(file, data.ReadErrors);
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
                var row = DataJson.Read<CharacterDto>(file, data.ReadErrors);
                if (row is null) continue;
                IdMatchesFile(file, row.Id, data.ReadErrors);
                data.Characters.Add(new(row, file));
            }
        }

        // Every catalog is read strictly (spec §16, FR-03): a key a file does not declare is a stop, named with
        // its file and key, the way it is for a rule table (DataJson.Read).
        ReadRows(root, "parks", data.Parks, row => row.Id, data.ReadErrors);
        ReadRows(root, "bats", data.Bats, row => row.Id, data.ReadErrors);
        ReadRows(root, "gloves", data.Gloves, row => row.Id, data.ReadErrors);

        var teamsPath = root.Resolve("teams", "teams.json");
        data.Teams = DataJson.Read<TeamsFile>(teamsPath, data.ReadErrors) ?? new();
        data.TeamsSource = teamsPath;

        var chemistryPath = root.Resolve("chemistry", "overrides.json");
        data.Chemistry = DataJson.Read<ChemistryOverrides>(chemistryPath, data.ReadErrors) ?? new();
        data.ChemistrySource = chemistryPath;

        var skillsPath = root.Resolve("abilities", "star-skills.json");
        // Read strictly (spec §13): a key no skill declares is a stop, so a retired key such as
        // batterWindowMul (PH-16-R1) cannot sit in the file looking like it still bends a pitch.
        data.StarSkills = DataJson.Read<StarSkillsDto>(skillsPath, data.ReadErrors) ?? new();
        data.StarSkillsSource = skillsPath;

        // Rule numbers (spec §16). Missing fields, unknown fields and bad ranges are errors; there is no code fallback.
        data.Rules = RulesTable.Load(root, data.ReadErrors);
        // Resolved through the overlay like every other path, so a trial that carries its own ground
        // rows is the file a bad `surface` is reported against (FD-05, SF-03).
        data.GroundsSource = RulesTable.PathFor(root, "grounds");
        // The same for the wall-material library a fence span names (FD-06, SF-03).
        data.WallsSource = RulesTable.PathFor(root, "walls");
        data.MatchSource = RulesTable.PathFor(root, "match");
        return data;
    }

    static void ReadRows<T>(
        DataRoot root,
        string directory,
        List<Sourced<T>> destination,
        Func<T, string> id,
        List<string> errors) where T : class
    {
        foreach (var file in Files(root, directory, errors))
        {
            var row = DataJson.Read<T>(file, errors);
            if (row is null) continue;
            IdMatchesFile(file, id(row), errors);
            destination.Add(new(row, file));
        }
    }

    /// <summary>
    /// A one-row file is named for its row: <c>parks/X.json</c> carries <c>"id": "X"</c>. A
    /// renamed file or a copied row that kept its old id would otherwise load under a name no file shows.
    /// A missing id is reported by the row's own check.
    /// </summary>
    static void IdMatchesFile(string file, string id, List<string> errors)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        if (id.Length > 0 && !string.Equals(id, name, StringComparison.Ordinal))
            errors.Add($"{file}: id '{id}' must equal the file name '{name}'");
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
        ValidateCaptains(data, errors);
        // The top cost tier is the captains' (§12, PH-16-R8): a role player carrying a top-tier special is refused.
        foreach (var row in data.Characters.Where(r => !r.Value.Captain))
        {
            TopTierIsCaptainOnly(row, "starPitch", row.Value.StarPitch, data.StarSkills.Pitches, errors);
            TopTierIsCaptainOnly(row, "starSwing", row.Value.StarSwing, data.StarSkills.Swings, errors);
        }
        // The hazard type set is the library's table, not a list this file keeps (FD-09, FR-02), and
        // so is the ground set a park's surface and zones are checked against (FD-05, SF-03). Both are
        // the tables this root loaded, so a trial that authors either file is checked against its own
        // rows. A rules table that failed to load has already reported itself.
        var rules = data.Rules ?? throw new InvalidOperationException("content data read no rules table");
        var hazards = rules.Hazards;
        var grounds = rules.Grounds;
        // Where a hazard may stand is measured on this root's own diamond (SF-23): the bags and the
        // rubber are the infield table this root loaded, so a trial's 80-ft diamond is the one its
        // parks are checked against, never the process-wide one.
        var infield = rules.Infield;
        // A polyline fence (FD-06, F2-c) is checked against this root's own lip, rail and wall
        // library, so a block that is legal on the shipped field and not on a trial's is refused on
        // the trial, by name, rather than played.
        var fence = new FenceLimits(
            rules.Flight.Classes.InfieldLipFt,
            rules.Boundary.RailHeightFt,
            rules.Walls,
            data.WallsSource);
        foreach (var row in data.Parks)
            ValidatePark(row, hazards, grounds, data.GroundsSource, infield, fence, errors);
        UniquePerPark("pickOrder", data.Parks.Where(r => r.Value.PickOrder is not null)
            .Select(r => (r.Value.PickOrder!.Value.ToString(CultureInfo.InvariantCulture), r.Source)), errors);
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
            // Every special names the cost tier stars.json prices (§12, PH-16-R7).
            if (!StarTierRules.IsTier(value.Tier))
                errors.Add($"{source}: star {kind} '{key}' tier must be one of [{string.Join(", ", StarTierRules.Ids)}]; got '{value.Tier ?? "null"}'");
            // The numbers the sim reads (spec §13): a value outside its range is a data error, not a fallback.
            if (kind == "pitch")
            {
                if (value.SpeedMul is null || value.SpeedMul <= 0)
                    errors.Add($"{source}: star pitch '{key}' speedMul must be greater than 0; got {value.SpeedMul?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                if (value.StaminaCost is null || value.StaminaCost < 0)
                    errors.Add($"{source}: star pitch '{key}' staminaCost must be at least 0; got {value.StaminaCost?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
            }
            else
            {
                if (value.ExitVeloMul is null || value.ExitVeloMul <= 0)
                    errors.Add($"{source}: star swing '{key}' exitVeloMul must be greater than 0; got {value.ExitVeloMul?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                if (value.LaunchDeg is not null && (value.LaunchDeg < 0 || value.LaunchDeg > 60))
                    errors.Add($"{source}: star swing '{key}' launchDeg must be between 0 and 60; got {value.LaunchDeg}");
            }
        }
        return ids;
    }

    static void TopTierIsCaptainOnly(
        Sourced<CharacterDto> row, string field, string? skill, Dictionary<string, StarSkillDto?>? rows, List<string> errors)
    {
        if (string.IsNullOrEmpty(skill) || rows is null || !rows.TryGetValue(skill, out var dto) || dto is null) return;
        if (dto.Tier == StarTierRules.TopId)
            errors.Add($"{row.Source}: character '{row.Value.Id}' {field} '{skill}' is a top-tier special, and the top tier is for captains only");
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

    /// <summary>
    /// A captain carries its identity in data (#1032) — its team's name, a signature bat the catalog has, a body with every
    /// proportion above zero — and a role player carries none of it: its body is its faction's captain's. So every role
    /// player's faction must have exactly one captain; a faction with none used to fall back to rio's body in silence.
    /// teams.json names every captain once in select order, and each preset nine names characters the catalog has.
    /// The two sides' gloves (match.json) name gloves the catalog has.
    /// </summary>
    static void ValidateCaptains(ContentData data, List<string> errors)
    {
        var bats = data.Bats.Select(r => r.Value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ids = data.Characters.Select(r => r.Value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var captainOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Characters)
        {
            var c = row.Value;
            if (c.Captain)
            {
                if (string.IsNullOrWhiteSpace(c.TeamName)) errors.Add($"{row.Source}: captain '{c.Id}' teamName is required");
                if (string.IsNullOrWhiteSpace(c.SignatureBat)) errors.Add($"{row.Source}: captain '{c.Id}' signatureBat is required");
                else if (!bats.Contains(c.SignatureBat)) errors.Add($"{row.Source}: captain '{c.Id}' signatureBat '{c.SignatureBat}' is not a bat in data/bats");
                if (c.Proportions is not { } p) errors.Add($"{row.Source}: captain '{c.Id}' proportions are required");
                else if (!(p.Height > 0 && p.Width > 0 && p.Head > 0 && p.Arms > 0 && p.Torso > 0))
                    errors.Add($"{row.Source}: captain '{c.Id}' proportions must all be greater than 0");
                if (!string.IsNullOrWhiteSpace(c.Faction) && !captainOf.TryAdd(c.Faction, c.Id))
                    errors.Add($"{row.Source}: faction '{c.Faction}' has two captains, '{captainOf[c.Faction]}' and '{c.Id}'");
            }
            else if (c.TeamName is not null || c.SignatureBat is not null || c.Proportions is not null)
                errors.Add($"{row.Source}: role player '{c.Id}' names teamName, signatureBat or proportions; those are a captain's (its body is its faction's captain's)");
        }
        foreach (var row in data.Characters.Where(r => !r.Value.Captain && !string.IsNullOrWhiteSpace(r.Value.Faction)))
            if (!captainOf.ContainsKey(row.Value.Faction))
                errors.Add($"{row.Source}: role player '{row.Value.Id}' faction '{row.Value.Faction}' has no captain to take its body from");

        var t = data.Teams;
        var source = data.TeamsSource;
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in t.Captains ?? [])
        {
            if (string.IsNullOrWhiteSpace(id)) { errors.Add($"{source}: captains has an empty id"); continue; }
            if (!listed.Add(id)) errors.Add($"{source}: captains names '{id}' twice");
            else if (!data.Characters.Any(r => r.Value.Captain && r.Value.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"{source}: captains names '{id}', which is not a captain in data/characters");
        }
        foreach (var cap in data.Characters.Where(r => r.Value.Captain))
            if (!listed.Contains(cap.Value.Id)) errors.Add($"{source}: captains leaves out '{cap.Value.Id}'");
        var presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < (t.Presets?.Count ?? 0); i++)
        {
            var preset = t.Presets![i];
            if (preset is null) { errors.Add($"{source}: presets[{i}] must be an object; got null"); continue; }
            if (string.IsNullOrWhiteSpace(preset.Id) || !presets.Add(preset.Id)) errors.Add($"{source}: presets[{i}] needs a unique id");
            if (string.IsNullOrWhiteSpace(preset.Name)) errors.Add($"{source}: preset '{preset.Id}' name is required");
            var roster = preset.Roster ?? [];
            if (roster.Count != 9 || roster.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 9)
                errors.Add($"{source}: preset '{preset.Id}' roster must name nine different characters");
            foreach (var id in roster)
                if (id is null || !ids.Contains(id)) errors.Add($"{source}: preset '{preset.Id}' roster names '{id}', which is not a character");
        }

        if (data.Rules is { } rules)
        {
            var gloves = data.Gloves.Select(r => r.Value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (side, glove) in new[] { ("homeGlove", rules.Match.HomeGlove), ("awayGlove", rules.Match.AwayGlove) })
                if (!gloves.Contains(glove))
                    errors.Add($"{data.MatchSource}: match.{side} '{glove}' is not a glove in data/gloves");
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
        // Arm, hands and reach are optional: absent means seeded from field / the table's stand-up reach.
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
        ValidateParkEnvironment(row.Source, p.Id, p.Environment, errors);
        ValidateParkZones(row.Source, p.Id, p.Zones, grounds, groundsSource, errors);
        ValidateParkFence(row.Source, p, fence, errors);
        ValidateParkFoulAndStarts(row.Source, p, fence, errors);
        for (var i = 0; i < (p.Hazards?.Count ?? 0); i++)
            ValidateHazard(row.Source, $"park '{p.Id}' hazard[{i}]", p.Hazards![i], hazards, infield, nightOnly: false, errors);
        ValidateParkNight(row.Source, p.Id, p.Night, hazards, infield, errors);
    }

    /// <summary>
    /// One hazard instance, from the day block (<c>hazards[i]</c>) or the night block
    /// (<c>night.hazards[i]</c>): the same rules for both (FD-11: the night block is validated like the
    /// day block) — a real object, a finite centre, a radius that is not negative, a type in the library
    /// with an authored row (<c>SF-03</c>), a disc for anything that acts, and the FD-19 placement rule
    /// at the disc the instance plays (<see cref="HazardPlace"/>). A night instance must also be one the
    /// hazards-off switch removes (<see cref="ValidateParkNight"/>).
    /// </summary>
    static void ValidateHazard(
        string source, string where, HazardDto? h, HazardRules hazards, InfieldRules infield, bool nightOnly, List<string> errors)
    {
        if (h is null)
        {
            errors.Add($"{source}: {where} must be an object; got null");
            return;
        }
        Finite(source, where + " x", h.X, errors);
        Finite(source, where + " z", h.Z, errors);
        NonNegative(source, where + " radius", h.Radius, errors);
        // The set of types is derived from the hazard library's table (SF-03, FD-09): an id
        // outside the library and an id inside it with no authored row are different mistakes,
        // and each is named for what it is.
        if (!HazardType.IsKnown(h.Type))
        {
            errors.Add($"{source}: {where} type must be one of "
                + $"[{string.Join(", ", hazards.Authored.OrderBy(x => x, StringComparer.Ordinal))}]; got '{h.Type}'");
            return;
        }
        if (!hazards.IsAuthored(h.Type))
        {
            errors.Add($"{source}: {where} type '{h.Type}' is in the library but has no authored row "
                + "in hazards.json; every hazard type a park may name carries a row (FD-09)");
            return;
        }
        var row = hazards.Of(h.Type);
        // A disc that acts needs a disc. A decoration is drawn and never played, so it may have
        // none at all — which is why Funfair's boxcar no longer needs a special case by name.
        if (h.Radius == 0 && row.Pattern != HazardPattern.Decoration)
            errors.Add($"{source}: {where} radius must be greater than 0; got {h.Radius}");
        // Night hazard instances are hazards (FD-11), so the switch removes every one of them
        // (FD-10-R1). A wall trait is part of the wall (FD-06) and a decoration does nothing in play:
        // the switch keeps both, and night changes neither the wall nor the look inside the bowl.
        if (nightOnly && !HazardPattern.IsHazard(row.Pattern))
            errors.Add($"{source}: {where} type '{h.Type}' is a {row.Pattern}, which the hazards switch keeps; "
                + $"a night block carries hazard instances only — [{string.Join(", ", HazardPattern.Hazards)}], "
                + "each one the switch removes (FD-11, FD-10-R1) — so author it in the day block");
        HazardPlace(source, where, h, row, nightOnly, infield, errors);
    }

    /// <summary>
    /// The park's night block (§0.3, §14, §16; FD-11 B, FD-11-R2; F4-d). Optional, and absent is a park
    /// whose night is its day. Night keeps the stadium lights, so the block may name hazard instances and
    /// — once F6-c defines them — look fields for the view outside the stadium, and nothing else: a key
    /// that names a rule or a number is refused by name by the strict read, with the reason
    /// (<see cref="OnlyKeysAttribute"/> on <see cref="ParkNightDto"/>). Each instance passes the day
    /// block's rules (<see cref="ValidateHazard"/>), measured at its night disc. A block that names
    /// nothing is refused as well: it changes nothing, and a dead block is how a misspelled one would read.
    /// </summary>
    static void ValidateParkNight(
        string source, string id, ParkNightDto? night, HazardRules hazards, InfieldRules infield, List<string> errors)
    {
        if (night is null) return;
        if ((night.Hazards?.Count ?? 0) == 0)
        {
            errors.Add($"{source}: park '{id}' night names nothing; a night block carries the park's night-only "
                + "hazards (FD-11) — remove the empty block");
            return;
        }
        for (var i = 0; i < night.Hazards!.Count; i++)
            ValidateHazard(source, $"park '{id}' night.hazards[{i}]", night.Hazards[i], hazards, infield, nightOnly: true, errors);
    }

    /// <summary>
    /// <c>SF-23</c> (§14, FD-19): a hazard stays off the running lanes, the mound-to-plate lane, the bag
    /// pads, the mound and the plate area. One refusal per hazard, naming every piece of ground its disc
    /// crosses and by how many feet, deepest first, so the author can see how far it has to go. A disc
    /// whose centre or radius is not a number has already been refused for that, and is not measured.
    ///
    /// <para>
    /// <b>The disc the instance plays, day and night</b> (map finding 31, F4-d). A day instance stands by
    /// day and at night, so it is measured at the larger of its radius and its night disc
    /// (<see cref="ParkHazards.NightDiscFt"/>, the type row's own <c>nightRadiusMul</c>); a night-block
    /// instance stands only at night, so at its night disc. For every row but Ember's breath the two are
    /// the same number and the refusal reads exactly as it always has; where they differ it names both.
    /// </para>
    /// </summary>
    static void HazardPlace(
        string source, string where, HazardDto h, HazardTypeRules row, bool nightOnly, InfieldRules infield, List<string> errors)
    {
        if (!double.IsFinite(h.X) || !double.IsFinite(h.Z) || !double.IsFinite(h.Radius) || h.Radius < 0) return;
        var atNight = ParkHazards.NightDiscFt(h.Radius, row);
        var disc = nightOnly ? atNight : Math.Max(h.Radius, atNight);
        var crossings = HazardPlacement.Crossings(h.X, h.Z, disc, infield);
        if (crossings.Count == 0) return;
        var what = string.Join(", ", crossings.Select(c =>
            $"{c.What} by {c.ByFt.ToString("0.00", CultureInfo.InvariantCulture)} ft"));
        var radius = h.Radius.ToString(CultureInfo.InvariantCulture)
            + (disc == h.Radius ? "" : $" ({disc.ToString(CultureInfo.InvariantCulture)} at night, "
                + $"hazards.{HazardType.Key(h.Type)}.nightRadiusMul {row.NightRadiusMul.ToString(CultureInfo.InvariantCulture)})");
        errors.Add($"{source}: {where} {h.Type} at ({h.X.ToString(CultureInfo.InvariantCulture)}, "
            + $"{h.Z.ToString(CultureInfo.InvariantCulture)}) radius {radius} "
            + $"crosses {what}; a hazard stays off the running lanes, the mound-to-plate lane, the bag pads, "
            + "the mound and the plate area (FD-19, SF-23)");
    }

    /// <summary>
    /// The park's air (§0.3 FD-03, §16): the block and both of its fields are optional, and a park that
    /// names neither plays the global table. A named field has to be a number the flight can fly —
    /// <see cref="MaxParkDragMul"/> is the sane envelope the schema refuses past, not an accepted value.
    /// No park names one; the first number is a trial, not a validator change.
    /// </summary>
    /// <summary>
    /// The park's foul territory and outfield starts (FD-07 C, F2-d; <c>SF-09</c>): each foul value is optional and positive, a
    /// rail under the park's fence; each named start stands past the infield lip and inside the park's fence at its bearing.
    /// </summary>
    static void ValidateParkFoulAndStarts(string source, ParkDto p, FenceLimits limits, List<string> errors)
    {
        if (p.Foul is { } foul)
        {
            foreach (var (name, value) in new[] { ("offsetFt", foul.OffsetFt), ("flareStartFt", foul.FlareStartFt), ("railHeightFt", foul.RailHeightFt) })
                if (value is { } v && !(v > 0 && double.IsFinite(v)))
                    errors.Add($"{source}: park '{p.Id}' foul.{name} must be greater than 0; got {v}");
            if (foul.RailHeightFt is { } rail && rail >= p.FenceHeightFt)
                errors.Add($"{source}: park '{p.Id}' foul.railHeightFt {rail} must stand under the fence ({p.FenceHeightFt} ft)");
        }
        if (p.OutfieldStarts is not { } starts) return;
        var park = p.ToPark();
        foreach (var (pos, spot) in new[] { ("LF", starts.LF), ("CF", starts.CF), ("RF", starts.RF) })
        {
            if (spot is null) continue;
            var d = Math.Sqrt(spot.X * spot.X + spot.Z * spot.Z);
            var bearing = Math.Atan2(spot.X, spot.Z) * 180 / Math.PI;
            if (d <= limits.LipFt)
                errors.Add($"{source}: park '{p.Id}' outfieldStarts.{pos} ({spot.X}, {spot.Z}) must stand past the infield lip ({limits.LipFt} ft)");
            else if (Math.Abs(bearing) > 45 || d >= AtBatResolver.FenceAt(park, bearing))
                errors.Add($"{source}: park '{p.Id}' outfieldStarts.{pos} ({spot.X}, {spot.Z}) must stand in fair ground inside the fence");
        }
    }

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
    public TeamsFile Teams { get; set; } = new();
    public string TeamsSource { get; set; } = "";
    /// <summary>Where match.json was read from — named by a glove the catalog does not have.</summary>
    public string MatchSource { get; set; } = "";
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

    /// <summary>Authored stand-up catch reach in feet. Absent takes the table's <c>standUpReachFt</c>.</summary>
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
    /// <summary>A captain's identity (#1032): its team's name, its signature bat, its body on the shared rig. A role player names none.</summary>
    public string? TeamName { get; set; }
    public string? SignatureBat { get; set; }
    public ProportionsDto? Proportions { get; set; }

    public Character ToCharacter() => new(
        Id, Name, Faction, Captain,
        new Stats(Pitch, Bat, Field, Run) { Arm = Arm, Hands = Hands, Contact = Contact, Power = Power,
            Velocity = Velocity, Movement = Movement, Control = Control, Endurance = Endurance },
        ParseHand(Bats), ParseHand(Throws),
        StarPitch, StarSwing, FieldAbility, Bio, ReachFt)
    {
        Repertoire = ParseRepertoire(),
        TeamName = TeamName,
        SignatureBat = SignatureBat,
        BodyType = Captain ? Id.ToLowerInvariant() : "",
        Proportions = Proportions?.ToSpec() ?? default
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

internal sealed class ProportionsDto
{
    public float Height { get; set; }
    public float Width { get; set; }
    public float Head { get; set; }
    public float Arms { get; set; }
    public float Torso { get; set; }
    public Silhouette.Spec ToSpec() => new(Height, Width, Head, Arms, Torso);
}

/// <summary><c>data/teams/teams.json</c> (#1032): the captains in select order and the authored nines.</summary>
internal sealed class TeamsFile
{
    public List<string?>? Captains { get; set; }
    public List<PresetDto?>? Presets { get; set; }
}

internal sealed class PresetDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string?>? Roster { get; set; }
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

    /// <summary>
    /// What this park is at night (§0.3, §14, §16; FD-11 B, FD-11-R2; F4-d). Optional, and absent is a park
    /// whose night is its day. Carried onto <see cref="Park"/>, where only the one resolution reads it
    /// (<see cref="PlayedPark.Of"/>); a park that names none writes nothing into <see cref="PlayTraceIdentity"/>.
    /// Crystal's night contact window (<c>nightContactWindowMul</c>) was a park key and is gone on both roots
    /// (FD-11-R2): night keeps the stadium lights and changes no rule of the at-bat.
    /// </summary>
    public ParkNightDto? Night { get; set; }

    /// <summary>This park's foul territory where it differs from <c>boundary.json</c> (FD-07 C, F2-d). Optional; no park names one.</summary>
    public ParkFoulDto? Foul { get; set; }

    /// <summary>This park's named outfield starts (FD-07 C, F2-d; <c>SF-09</c>). Optional; no park names one.</summary>
    public ParkOutfieldDto? OutfieldStarts { get; set; }

    public Park ToPark() => new(
        Id, Name, Faction, Surface,
        LeftFenceFt, CenterFenceFt, RightFenceFt, WindMph,
        ToHazards(Hazards),
        WindDeg, FenceHeightFt, Environment?.ToEnvironment(), Zones?.ToZones(),
        Fence?.ToFence(), Night?.ToNight(),
        Foul is null ? null : new ParkFoul(Foul.OffsetFt, Foul.FlareStartFt, Foul.RailHeightFt),
        OutfieldStarts?.ToOutfield());

    /// <summary>Only after <see cref="ContentDataValidator"/> has accepted the rows, which is when a catalog is built.</summary>
    internal static List<Hazard> ToHazards(List<HazardDto?>? rows) =>
        (rows ?? []).Select(h => new Hazard(h!.Type, h.X, h.Z, h.Radius, h.Tag)).ToList();
}

/// <summary>
/// The optional <c>night</c> block of a park file (§0.3, §14, §16; FD-11 B, FD-11-R2; F4-d). One key today,
/// <c>hazards</c>: the instances that exist only at night, in the day block's shape. The block has room for
/// look fields — the view outside the stadium, F6-c's — and defines none. Inside the strict park read, so
/// any other key (a rule, a number, a misspelling) is refused by name, with the reason.
/// </summary>
[OnlyKeys("night keeps the stadium lights, so a night block names the park's night-only hazards (and, once "
    + "F6-c defines them, the view outside the stadium) and never a rule of the at-bat, the flight, the ground "
    + "or the bodies (FD-11-R2)")]
internal sealed class ParkNightDto
{
    /// <summary>The night-only instances, each validated like a day instance and each a hazard the switch removes.</summary>
    public List<HazardDto?>? Hazards { get; set; }

    /// <summary>Only after <see cref="ContentDataValidator"/> has accepted the block, which is when a catalog is built.</summary>
    public ParkNight ToNight() => new(ParkDto.ToHazards(Hazards));
}

/// <summary>
/// Why a block of a strictly read file declares only the keys it does. The strict read
/// (<c>DataJson.UnknownKeys</c>) appends it to the refusal of any other key, so the author
/// reads the rule and not only the key list.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class OnlyKeysAttribute : Attribute
{
    public OnlyKeysAttribute(string why) => Why = why;

    /// <summary>The rule, in words, that the declared keys are all of.</summary>
    public string Why { get; }
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
internal sealed class ParkFoulDto
{
    public double? OffsetFt { get; set; }
    public double? FlareStartFt { get; set; }
    public double? RailHeightFt { get; set; }
}

internal sealed class StartSpotDto
{
    public double X { get; set; }
    public double Z { get; set; }
}

internal sealed class ParkOutfieldDto
{
    public StartSpotDto? LF { get; set; }
    public StartSpotDto? CF { get; set; }
    public StartSpotDto? RF { get; set; }

    static StartSpot? Of(StartSpotDto? s) => s is null ? null : new StartSpot(s.X, s.Z);
    public ParkOutfield ToOutfield() => new(Of(LF), Of(CF), Of(RF));
}

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
    public bool LateBreak { get; set; }
    public bool Decoy { get; set; }
    public string? OnCatch { get; set; }
    public double? ExitVeloMul { get; set; }
    public double? LaunchDeg { get; set; }
    public string? Terrain { get; set; }
    public double? FielderPauseSec { get; set; }
    public bool InfieldChaos { get; set; }
    public bool Fragments { get; set; }
    /// <summary>The cost tier (PH-16-R7): one of <see cref="StarTierRules.Ids"/>. Required.</summary>
    public string? Tier { get; set; }

    public StarPitchSkill ToPitch() => new(Id, Name, Kind, SpeedMul ?? 1.0, StaminaCost ?? 0,
        LateBreak, Decoy, OnCatch, Tier ?? StarTierRules.LowId);

    public StarSwingSkill ToSwing() => new(Id, Name, Kind, ExitVeloMul ?? 1.0, LaunchDeg, Terrain,
        FielderPauseSec ?? 0, InfieldChaos, Decoy, Fragments, Tier ?? StarTierRules.LowId);
}
