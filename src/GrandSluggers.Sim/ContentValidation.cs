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
    static readonly HashSet<string> FieldAbilityIds = new(FieldAbilityId.All, StringComparer.Ordinal);
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
                if (row is not null) data.Characters.Add(new(row, file));
            }
        }

        // Every catalog is read strictly (spec §16, FR-03): a key a file does not declare is a stop, named with
        // its file and key, the way it is for a rule table (DataJson.Read).
        ReadRows(root, "parks", data.Parks, data.ReadErrors);
        ReadRows(root, "bats", data.Bats, data.ReadErrors);
        ReadRows(root, "gloves", data.Gloves, data.ReadErrors);

        var teamsPath = root.Resolve("teams", "teams.json");
        data.Teams = DataJson.Read<TeamsFile>(teamsPath, data.ReadErrors) ?? new();
        data.TeamsSource = teamsPath;

        var chemistryPath = root.Resolve("chemistry", "overrides.json");
        data.Chemistry = DataJson.Read<ChemistryOverrides>(chemistryPath, data.ReadErrors) ?? new();
        data.ChemistrySource = chemistryPath;
        var crewsPath = root.Resolve("chemistry", "crews.json");
        data.Crews = DataJson.Read<CrewsFile>(crewsPath, data.ReadErrors) ?? new();
        data.CrewsSource = crewsPath;

        // The continent the parks stand on (WD-05): read strictly, like every catalog.
        var worldPath = root.Resolve(WorldMap.Directory, WorldMap.FileName);
        data.World = DataJson.Read<WorldDto>(worldPath, data.ReadErrors) ?? new();
        data.WorldSource = worldPath;

        // The sidekick species (WD-27): read strictly, like every catalog.
        var speciesPath = root.Resolve(WorldMap.Directory, "species.json");
        data.Species = DataJson.Read<SpeciesFile>(speciesPath, data.ReadErrors) ?? new();
        data.SpeciesSource = speciesPath;

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
        List<string> errors) where T : class
    {
        foreach (var file in Files(root, directory, errors))
        {
            var row = DataJson.Read<T>(file, errors);
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

    static IReadOnlyList<string> Errors(ContentData data)
    {
        var errors = new List<string>(data.ReadErrors);
        DuplicateIds("character", data.Characters.Select(r => (r.Value.Id, r.Source)), errors);
        DuplicateIds("park", data.Parks.Select(r => (r.Value.Id, r.Source)), errors);
        DuplicateIds("bat", data.Bats.Select(r => (r.Value.Id, r.Source)), errors);
        DuplicateIds("glove", data.Gloves.Select(r => (r.Value.Id, r.Source)), errors);
        IdIsFileName("character", data.Characters.Select(r => (r.Value.Id, r.Source)), errors);
        IdIsFileName("park", data.Parks.Select(r => (r.Value.Id, r.Source)), errors);
        IdIsFileName("bat", data.Bats.Select(r => (r.Value.Id, r.Source)), errors);
        IdIsFileName("glove", data.Gloves.Select(r => (r.Value.Id, r.Source)), errors);

        var pitches = SkillIds("pitch", data.StarSkills.Pitches, data.StarSkillsSource, errors);
        var swings = SkillIds("swing", data.StarSkills.Swings, data.StarSkillsSource, errors);
        var characters = data.Characters
            .Select(r => r.Value.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in data.Characters)
            ValidateCharacter(row, pitches, swings, errors);
        ValidateCaptains(data, errors);
        ValidateSpecies(data, pitches, swings, errors);
        // A sidekick never carries a captain's special, and no two captains share one (§13, AB-02, AB-10): a sidekick's
        // specials are the generic pool's; a captain's belongs to that captain alone.
        SpecialsBelongToTheirCarrier(data.Characters, "starPitch", r => r.StarPitch, data.StarSkills.Pitches, errors);
        SpecialsBelongToTheirCarrier(data.Characters, "starSwing", r => r.StarSwing, data.StarSkills.Swings, errors);
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
        var regions = ValidateWorld(data, errors);
        foreach (var row in data.Parks)
        {
            var p = row.Value;
            if (string.IsNullOrWhiteSpace(p.Region))
                errors.Add($"{row.Source}: park '{p.Id}' region must name its place on the map ({data.WorldSource}); got none");
            else if (!regions.Contains(p.Region))
                errors.Add($"{row.Source}: park '{p.Id}' region '{p.Region}' is not a region in {data.WorldSource}");
        }
        UniquePerPark("region", data.Parks
            .Where(r => !string.IsNullOrWhiteSpace(r.Value.Region))
            .Select(r => (r.Value.Region, r.Source)), errors);
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
        ValidateCrews(data, errors);
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
            // The price follows the carrier (§12), so a row that still names a tier is stale.
            if (value.Tier is not null)
                errors.Add($"{source}: star {kind} '{key}' names a tier; tiers are retired and the carrier sets the price (stars.prices)");
            // The numbers the sim reads (spec §13): a value outside its range is a data error, not a fallback.
            if (kind == "pitch")
            {
                if (value.SpeedMul is null || value.SpeedMul <= 0)
                    errors.Add($"{source}: star pitch '{key}' speedMul must be greater than 0; got {value.SpeedMul?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                if (value.StaminaCost is null || value.StaminaCost < 0)
                    errors.Add($"{source}: star pitch '{key}' staminaCost must be at least 0; got {value.StaminaCost?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                // The ball comes back before the plate (§13): the hitter always sees the last stretch of every flight.
                if (value.Vanish is { } vanish && (vanish.From <= 0 || vanish.To <= vanish.From || vanish.To > PitchVanish.BackBy))
                    errors.Add($"{source}: star pitch '{key}' vanish needs 0 < from < to <= {PitchVanish.BackBy.ToString(CultureInfo.InvariantCulture)}");
                if (value.DustBowl is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry a dust bowl; it is a swing's");
                if (value.WindMul is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry windMul; it is a swing's");
                if (value.Leap is { } leap && (leap.At <= 0 || leap.HoldSpan <= 0 || leap.At + leap.HoldSpan >= 1 || leap.HoldPace < 0 || leap.HoldPace >= 1))
                    errors.Add($"{source}: star pitch '{key}' leap needs at > 0, holdSpan > 0, at + holdSpan < 1 and 0 <= holdPace < 1");
                if (value.FirstHopBounceMul is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry firstHopBounceMul; it is a swing's");
                if (value.PerfectRingMul is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry perfectRingMul; it is a swing's");
                // The rise ends at the plate and is always up (§13): a jump out of the heart of the zone, never a drop.
                if (value.Rise is { } late && (late.RiseFt <= 0 || late.RiseFt > PitchRise.MaxRiseFt || late.From <= 0 || late.From >= 1))
                    errors.Add($"{source}: star pitch '{key}' rise needs 0 < riseFt <= {PitchRise.MaxRiseFt.ToString(CultureInfo.InvariantCulture)} and 0 < from < 1");
                if (value.Loop is { } loop && (loop.At <= 0 || loop.Span <= 0 || loop.At + loop.Span >= 1 || loop.DiameterFt <= 0 || loop.DiameterFt > PitchLoop.MaxDiameterFt))
                    errors.Add($"{source}: star pitch '{key}' loop needs at > 0, span > 0, at + span < 1 and 0 < diameterFt <= {PitchLoop.MaxDiameterFt.ToString(CultureInfo.InvariantCulture)}");
                if (value.FirstHopStallSec is not null || value.FirstHopStallSpeedMul is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry a first-hop stall; it is a swing's");
                if (value.Float is { } rise && (rise.RiseFt <= 0 || rise.RiseFt > PitchFloatLimits.MaxRiseFt || rise.DropFrom <= 0 || rise.DropFrom >= 1))
                    errors.Add($"{source}: star pitch '{key}' float needs 0 < riseFt <= {PitchFloatLimits.MaxRiseFt.ToString(CultureInfo.InvariantCulture)} and 0 < dropFrom < 1");
                // The sway settles before the plate (§13): the last stretch of the flight and the crossing are the ordinary pitch's.
                if (value.Sway is { } sway && (sway.WidthFt <= 0 || sway.WidthFt > PitchSway.MaxWidthFt || sway.Cycles <= 0
                        || sway.Cycles > PitchSway.MaxCycles || sway.PeakAt <= 0 || sway.SettleBy <= sway.PeakAt || sway.SettleBy >= 1))
                    errors.Add($"{source}: star pitch '{key}' sway needs 0 < widthFt <= {PitchSway.MaxWidthFt.ToString(CultureInfo.InvariantCulture)}, 0 < cycles <= {PitchSway.MaxCycles.ToString(CultureInfo.InvariantCulture)} and 0 < peakAt < settleBy < 1");
                if (value.FielderPauseSec is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry fielderPauseSec; it is a swing's");
                // The pendulum swings out and back in on one arc, below its pivot, and hangs straight at the plate (§13).
                if (value.Pendulum is { } vine && (vine.LengthFt <= 0 || vine.LengthFt > PitchPendulum.MaxLengthFt
                        || vine.SwingDeg <= 0 || vine.SwingDeg > PitchPendulum.MaxSwingDeg || vine.WidestAt <= 0 || vine.WidestAt >= 1))
                    errors.Add($"{source}: star pitch '{key}' pendulum needs 0 < lengthFt <= {PitchPendulum.MaxLengthFt.ToString(CultureInfo.InvariantCulture)}, 0 < swingDeg <= {PitchPendulum.MaxSwingDeg.ToString(CultureInfo.InvariantCulture)} and 0 < widestAt < 1");
                if (value.Jag is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry a jag; it is a swing's");
                // The drop ends at the plate and is always down (§13): out of the bottom of the zone, never into the dirt.
                if (value.Drop is { } sink && (sink.DropFt <= 0 || sink.DropFt > PitchDrop.MaxDropFt || sink.From <= 0 || sink.From >= 1))
                    errors.Add($"{source}: star pitch '{key}' drop needs 0 < dropFt <= {PitchDrop.MaxDropFt.ToString(CultureInfo.InvariantCulture)} and 0 < from < 1");
                if (value.HotBall is not null)
                    errors.Add($"{source}: star pitch '{key}' cannot carry a hotBall; it is a swing's");
            }
            else
            {
                if (value.ExitVeloMul is null || value.ExitVeloMul <= 0)
                    errors.Add($"{source}: star swing '{key}' exitVeloMul must be greater than 0; got {value.ExitVeloMul?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                if (value.LaunchDeg is not null && (value.LaunchDeg < 0 || value.LaunchDeg > 60))
                    errors.Add($"{source}: star swing '{key}' launchDeg must be between 0 and 60; got {value.LaunchDeg}");
                if (value.Vanish is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a vanish; it is a pitch's");
                // The bowl is a grounder's first landing (§13): a disc no wider than the park's, gone inside two seconds, a slow and not a wall.
                if (value.DustBowl is { } bowl)
                {
                    if (bowl.RadiusFt <= 0 || bowl.RadiusFt > SwingDustBowl.MaxRadiusFt || bowl.Sec <= 0 || bowl.Sec > SwingDustBowl.MaxSec
                        || bowl.Mul <= 0 || bowl.Mul >= 1)
                        errors.Add($"{source}: star swing '{key}' dustBowl needs 0 < radiusFt <= {SwingDustBowl.MaxRadiusFt.ToString(CultureInfo.InvariantCulture)}, 0 < sec <= {SwingDustBowl.MaxSec.ToString(CultureInfo.InvariantCulture)} and 0 < mul < 1");
                    if (value.LaunchDeg is not { } launch || launch > SwingDustBowl.MaxLaunchDeg)
                        errors.Add($"{source}: star swing '{key}' raises a dust bowl at its first landing, so it must name a grounder's launchDeg of at most {SwingDustBowl.MaxLaunchDeg.ToString(CultureInfo.InvariantCulture)}");
                }
                if (value.Float is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a float; it is a pitch's");
                if (value.Leap is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a leap; it is a pitch's");
                if (value.Rise is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a rise; it is a pitch's");
                // A bigger heart, never a bigger bat (PH-16-R2): the ring grows, the oval and the window do not.
                if (value.PerfectRingMul is not null && (value.PerfectRingMul < 1 || value.PerfectRingMul > StarSwingSkill.MaxPerfectRingMul))
                    errors.Add($"{source}: star swing '{key}' perfectRingMul must be between 1 and {StarSwingSkill.MaxPerfectRingMul.ToString(CultureInfo.InvariantCulture)}; got {value.PerfectRingMul}");
                if (value.Loop is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a loop; it is a pitch's");
                // The stall is a grounder's (§13): it names both numbers, and a launch low enough that the hop and the stall end inside two seconds.
                if (value.FirstHopStallSec is not null || value.FirstHopStallSpeedMul is not null)
                {
                    if (value.FirstHopStallSec is not { } stall || stall <= 0 || stall > StarSwingSkill.MaxStallSec)
                        errors.Add($"{source}: star swing '{key}' firstHopStallSec must be greater than 0 and at most {StarSwingSkill.MaxStallSec.ToString(CultureInfo.InvariantCulture)}; got {value.FirstHopStallSec?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                    if (value.FirstHopStallSpeedMul is not { } after || after <= 0 || after > 1)
                        errors.Add($"{source}: star swing '{key}' firstHopStallSpeedMul must be greater than 0 and at most 1; got {value.FirstHopStallSpeedMul?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
                    if (value.LaunchDeg is not { } launch || launch > StarSwingSkill.MaxStallLaunchDeg)
                        errors.Add($"{source}: star swing '{key}' stalls on its first hop, so it must name a grounder's launchDeg of at most {StarSwingSkill.MaxStallLaunchDeg.ToString(CultureInfo.InvariantCulture)}");
                }
                if (value.Sway is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a sway; it is a pitch's");
                // The pause ends within the 2 s every special's bend ends in (§13).
                if (value.FielderPauseSec is not null && (value.FielderPauseSec <= 0 || value.FielderPauseSec > StarSwingSkill.MaxFielderPauseSec))
                    errors.Add($"{source}: star swing '{key}' fielderPauseSec must be greater than 0 and at most {StarSwingSkill.MaxFielderPauseSec.ToString(CultureInfo.InvariantCulture)}; got {value.FielderPauseSec}");
                if (value.Drop is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a drop; it is a pitch's");
                // The ball cools within two seconds of contact (§13), and a glove must be able to hold it a moment first.
                if (value.HotBall is { } hot && (hot.MoltenSec <= 0 || hot.MoltenSec > HotBall.MaxMoltenSec || hot.HoldSec <= 0 || hot.HoldSec >= hot.MoltenSec))
                    errors.Add($"{source}: star swing '{key}' hotBall needs 0 < holdSec < moltenSec <= {HotBall.MaxMoltenSec.ToString(CultureInfo.InvariantCulture)}");
                if (value.FirstHopBounceMul is not null && (value.FirstHopBounceMul < 1 || value.FirstHopBounceMul > StarSwingSkill.MaxBounceMul))
                    errors.Add($"{source}: star swing '{key}' firstHopBounceMul must be between 1 and {StarSwingSkill.MaxBounceMul.ToString(CultureInfo.InvariantCulture)}; got {value.FirstHopBounceMul}");
                if (value.WindMul is not null && (value.WindMul < 0 || value.WindMul > StarSwingSkill.MaxWindMul))
                    errors.Add($"{source}: star swing '{key}' windMul must be between 0 and {StarSwingSkill.MaxWindMul.ToString(CultureInfo.InvariantCulture)}; got {value.WindMul}");
                if (value.Pendulum is not null)
                    errors.Add($"{source}: star swing '{key}' cannot carry a pendulum; it is a pitch's");
                // Two jags inside the window, the second after the first, the ball back on its line before the window ends (§13).
                if (value.Jag is { } jag && (jag.OffsetFt <= 0 || jag.OffsetFt > BallJag.MaxOffsetFt || jag.Span <= 0
                        || jag.FirstAt <= 0 || jag.FirstAt + jag.Span > jag.SecondAt || jag.SecondAt + jag.Span >= 1))
                    errors.Add($"{source}: star swing '{key}' jag needs 0 < offsetFt <= {BallJag.MaxOffsetFt.ToString(CultureInfo.InvariantCulture)}, span > 0, 0 < firstAt, firstAt + span <= secondAt and secondAt + span < 1");
            }
        }
        return ids;
    }

    /// <summary>
    /// The sidekick species (WD-27): every build has proportions; every species names a known build, a captain's faction
    /// and generic specials and a field ability that exist; each captain's faction has three species, one of each build,
    /// and eight sidekicks; every sidekick names a species of its own faction, and a captain names none.
    /// </summary>
    static void ValidateSpecies(ContentData data, HashSet<string> pitches, HashSet<string> swings, List<string> errors)
    {
        var src = data.SpeciesSource;
        var builds = data.Species.Builds ?? [];
        foreach (var b in SpeciesBuilds.All)
            if (!builds.TryGetValue(b, out var p) || p is null)
                errors.Add($"{src}: builds.{b} proportions are required");
        var factions = data.Characters.Where(r => r.Value.Captain).Select(r => r.Value.Faction)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var byId = new Dictionary<string, SpeciesDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in data.Species.Species ?? [])
        {
            if (s is null) { errors.Add($"{src}: a species row must be an object; got null"); continue; }
            if (string.IsNullOrWhiteSpace(s.Id) || !byId.TryAdd(s.Id, s))
                errors.Add($"{src}: species id '{s.Id}' is empty or repeated");
            if (!SpeciesBuilds.All.Contains(s.Build))
                errors.Add($"{src}: species '{s.Id}' build must be one of [{string.Join(", ", SpeciesBuilds.All)}]; got '{s.Build}'");
            if (!factions.Contains(s.Faction))
                errors.Add($"{src}: species '{s.Id}' faction '{s.Faction}' has no captain");
            if (string.IsNullOrWhiteSpace(s.Name) || string.IsNullOrWhiteSpace(s.Blend) || string.IsNullOrWhiteSpace(s.Look))
                errors.Add($"{src}: species '{s.Id}' needs a name, a blend and a look");
            if (!pitches.Contains(s.StarPitch)) errors.Add($"{src}: species '{s.Id}' starPitch '{s.StarPitch}' is not a star pitch");
            if (!swings.Contains(s.StarSwing)) errors.Add($"{src}: species '{s.Id}' starSwing '{s.StarSwing}' is not a star swing");
            if (!FieldAbilityIds.Contains(s.FieldAbility)) errors.Add($"{src}: species '{s.Id}' fieldAbility '{s.FieldAbility}' is not a field ability");
        }
        foreach (var f in factions)
        {
            var mine = byId.Values.Where(s => s.Faction.Equals(f, StringComparison.OrdinalIgnoreCase)).ToList();
            var got = mine.Select(s => s.Build).OrderBy(b => b, StringComparer.Ordinal).ToList();
            if (!got.SequenceEqual(SpeciesBuilds.All.OrderBy(b => b, StringComparer.Ordinal)))
                errors.Add($"{src}: faction '{f}' needs three species, one of each build; got [{string.Join(", ", got)}]");
            var sidekicks = data.Characters.Count(r => !r.Value.Captain && r.Value.Faction.Equals(f, StringComparison.OrdinalIgnoreCase));
            if (sidekicks != SpeciesBuilds.SidekicksPerCaptain)
                errors.Add($"{data.Characters.FirstOrDefault(r => !r.Value.Captain).Source}: faction '{f}' needs {SpeciesBuilds.SidekicksPerCaptain} sidekicks; got {sidekicks}");
        }
        foreach (var row in data.Characters)
        {
            var c = row.Value;
            if (c.Captain)
            {
                if (c.Species is not null) errors.Add($"{row.Source}: captain '{c.Id}' names a species; only a sidekick does");
                continue;
            }
            if (string.IsNullOrEmpty(c.Species) || !byId.TryGetValue(c.Species, out var sp))
                errors.Add($"{row.Source}: sidekick '{c.Id}' species '{c.Species ?? "null"}' is not a row in {src}");
            else if (!sp.Faction.Equals(c.Faction, StringComparison.OrdinalIgnoreCase))
                errors.Add($"{row.Source}: sidekick '{c.Id}' species '{c.Species}' belongs to faction '{sp.Faction}', not '{c.Faction}'");
        }
    }

    static void SpecialsBelongToTheirCarrier(IEnumerable<Sourced<CharacterDto>> characters, string field,
        Func<CharacterDto, string?> skillOf, Dictionary<string, StarSkillDto?>? rows, List<string> errors)
    {
        if (rows is null) return;
        var owner = new Dictionary<string, string>(StringComparer.Ordinal);
        var families = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in characters)
        {
            var skill = skillOf(row.Value);
            if (string.IsNullOrEmpty(skill) || !rows.TryGetValue(skill, out var dto) || dto is null) continue;
            var generic = dto.Kind == StarSkills.GenericKind;
            if (!row.Value.Captain && !generic)
                errors.Add($"{row.Source}: character '{row.Value.Id}' {field} '{skill}' is a captain's special; a sidekick carries the generic pool's");
            if (row.Value.Captain && generic)
                errors.Add($"{row.Source}: captain '{row.Value.Id}' {field} '{skill}' is a sidekick's generic special; a captain carries their own");
            if (!generic && !owner.TryAdd(skill, row.Value.Id))
                errors.Add($"{row.Source}: character '{row.Value.Id}' {field} '{skill}' is already '{owner[skill]}''s; a captain's special is theirs alone");
            // No two captains share an effect family (AB-02): a row that names its family is checked against the others.
            if (!generic && !string.IsNullOrEmpty(dto.Family))
            {
                if (families.TryGetValue(dto.Family, out var other) && other != skill)
                    errors.Add($"{row.Source}: captain '{row.Value.Id}' {field} '{skill}' shares effect family '{dto.Family}' with '{other}'; every captain's special is its own");
                else families[dto.Family] = skill;
            }
        }
    }

    /// <summary>
    /// A one-row file is named for its id (<c>data/parks/harbor-diamond.json</c> holds <c>harbor-diamond</c>), so the
    /// file a person opens is the row the game plays. A list file (<c>role-players.json</c>, sources <c>file[i]</c>) names
    /// its rows itself.
    /// </summary>
    static void IdIsFileName(string kind, IEnumerable<(string Id, string Source)> rows, List<string> errors)
    {
        foreach (var (id, source) in rows)
        {
            if (source.EndsWith(']') || string.IsNullOrWhiteSpace(id)) continue;
            var name = Path.GetFileNameWithoutExtension(source);
            if (!string.Equals(id, name, StringComparison.Ordinal))
                errors.Add($"{source}: {kind} id '{id}' must match its file name '{name}'");
        }
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
    /// <summary>
    /// The world file (WD-05): a continent name, and regions each with a unique id, a name, a place on the unit map and an
    /// island flag. Returns the region ids the parks may name.
    /// </summary>
    static HashSet<string> ValidateWorld(ContentData data, List<string> errors)
    {
        var src = data.WorldSource;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(data.World.Continent))
            errors.Add($"{src}: continent must name the continent; got none");
        if (data.World.Regions is not { Count: > 0 } rows)
        {
            errors.Add($"{src}: regions must list at least one region");
            return ids;
        }
        for (var i = 0; i < rows.Count; i++)
        {
            var at = $"{src}: regions[{i}]";
            if (rows[i] is not { } r)
            {
                errors.Add($"{at} must be an object; got null");
                continue;
            }
            if (string.IsNullOrWhiteSpace(r.Id)) errors.Add($"{at} id must not be empty");
            else if (!ids.Add(r.Id)) errors.Add($"{at} id '{r.Id}' is declared twice");
            if (string.IsNullOrWhiteSpace(r.Name)) errors.Add($"{at} '{r.Id}' name must not be empty");
            foreach (var (axis, v) in new[] { ("x", r.X), ("y", r.Y) })
            {
                if (v is not { } n) errors.Add($"{at} '{r.Id}' {axis} must place it on the map; got none");
                else if (!(n >= 0 && n <= 1)) errors.Add($"{at} '{r.Id}' {axis} must be 0 to 1 on the unit map; got {n.ToString(CultureInfo.InvariantCulture)}");
            }
            if (r.Island is null) errors.Add($"{at} '{r.Id}' island must be true or false");
        }
        return ids;
    }

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
            ValidateBodyClasses(data, rules.BodyClasses, errors);
            var gloves = data.Gloves.Select(r => r.Value.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (side, glove) in new[] { ("homeGlove", rules.Match.HomeGlove), ("awayGlove", rules.Match.AwayGlove) })
                if (!gloves.Contains(glove))
                    errors.Add($"{data.MatchSource}: match.{side} '{glove}' is not a glove in data/gloves");
        }
    }

    /// <summary>
    /// Every character resolves exactly one body class (spec §8.1, SC-09): a captain names one, a role player names its own or
    /// wears its captain's, and every name is a row in <c>data/rules/body-classes.json</c>.
    /// </summary>
    static void ValidateBodyClasses(ContentData data, BodyClassLibrary classes, List<string> errors)
    {
        foreach (var row in data.Characters)
        {
            var c = row.Value;
            if (string.IsNullOrWhiteSpace(c.BodyClass))
            {
                if (c.Captain)
                    errors.Add($"{row.Source}: captain '{c.Id}' bodyClass is required; one of [{string.Join(", ", classes.Classes.Select(k => k.Id))}]");
                continue;
            }
            if (!classes.Has(c.BodyClass))
                errors.Add($"{row.Source}: character '{c.Id}' bodyClass '{c.BodyClass}' is not a row in data/rules/body-classes.json; "
                           + $"the classes are [{string.Join(", ", classes.Classes.Select(k => k.Id))}]");
        }
    }

    /// <summary>
    /// The nine sub-stats and the four bars (spec §2, CH-07, CH-08). Every sub-stat is required and 1–10.
    /// A bar is derived, so a row that authors one is refused rather than ignored: the loader drops unknown
    /// keys, and a stale <c>"pitch": 9</c> left beside the sub-stats would otherwise read as if it counted.
    /// At most <see cref="Stats.MaxStarBars"/> derived bar may reach <see cref="Stats.StarBar"/>.
    /// </summary>
    static void ValidateStats(string source, CharacterDto c, List<string> errors)
    {
        foreach (var (bar, value) in new[] { ("pitch", c.Pitch), ("bat", c.Bat), ("field", c.Field) })
            if (value is not null)
                errors.Add($"{source}: character '{c.Id}' authors the '{bar}' bar; bars are derived from sub-stats "
                           + $"(spec §2), so author {Stats.SubStatsOf(bar)} instead");

        var missing = false;
        foreach (var (key, value) in c.SubStats())
        {
            if (value is not { } v)
            {
                errors.Add($"{source}: character '{c.Id}' {key} is required (1–10); every character authors all nine sub-stats");
                missing = true;
            }
            else Range(source, $"character '{c.Id}' {key}", v, 1, 10, errors);
        }
        if (missing) return;

        var stats = c.ToStats();
        var star = new[] { ("Bat", stats.Bat), ("Pitch", stats.Pitch), ("Field", stats.Field), ("Run", stats.Run) }
            .Where(b => b.Item2 >= Stats.StarBar).Select(b => $"{b.Item1} {b.Item2}").ToList();
        if (star.Count > Stats.MaxStarBars)
            errors.Add($"{source}: character '{c.Id}' has {star.Count} bars at {Stats.StarBar} or higher ({string.Join(", ", star)}); "
                       + $"at most {Stats.MaxStarBars} may be (CH-08)");
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
        ValidateStats(row.Source, c, errors);
        if (c.ReachFt is not null)
            errors.Add($"{row.Source}: character '{c.Id}' authors reachFt; reach comes from the body class "
                       + "(data/rules/body-classes.json, spec §8.1), so name a bodyClass instead");
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
        else if (p.FenceHeightFt <= fence.RailFt)
            errors.Add($"{row.Source}: park '{p.Id}' fenceHeightFt must stand over the {fence.RailFt} ft foul rail (the drawn wall ramps up to it, D15); got {p.FenceHeightFt}");
        ValidateParkEnvironment(row.Source, p.Id, p.Environment, errors);
        ValidateParkZones(row.Source, p.Id, p.Zones, grounds, groundsSource, errors);
        ValidateParkFence(row.Source, p, fence, errors);
        ValidateParkFoulAndStarts(row.Source, p, fence, errors);
        for (var i = 0; i < (p.Hazards?.Count ?? 0); i++)
            ValidateHazard(row.Source, $"park '{p.Id}' hazard[{i}]", p.Hazards![i], hazards, infield, nightOnly: false, errors);
        ValidateParkNight(row.Source, p.Id, p.Night, hazards, infield, errors,
            new HashSet<string>((p.Hazards ?? []).Where(h => h is not null).Select(h => h!.Type), StringComparer.Ordinal));
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
        string source, string id, ParkNightDto? night, HazardRules hazards, InfieldRules infield, List<string> errors,
        HashSet<string> dayTypes)
    {
        if (night is null) return;
        if ((night.Hazards?.Count ?? 0) == 0 && (night.Without?.Count ?? 0) == 0)
        {
            errors.Add($"{source}: park '{id}' night names nothing; a night block carries the park's night-only "
                + "hazards or the day types night clears (FD-11) — remove the empty block");
            return;
        }
        for (var i = 0; i < (night.Hazards?.Count ?? 0); i++)
            ValidateHazard(source, $"park '{id}' night.hazards[{i}]", night.Hazards![i], hazards, infield, nightOnly: true, errors);
        // A cleared type must be one the day stands: clearing nothing is a dead rule, the way a misspelling would read.
        foreach (var type in night.Without ?? [])
            if (!dayTypes.Contains(type))
                errors.Add($"{source}: park '{id}' night.without names '{type}', which is not one of the park's day hazard types");
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
        // A drift wanders: every point of its path stays off the lanes, so its disc is measured with its whole travel.
        if (row.Pattern == HazardPattern.Drift) disc += row.TravelFt ?? 0;
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
        if (env.WindSchedule is { } s)
        {
            if (s.MinMph is not { } lo || s.MaxMph is not { } hi || !(lo >= 0 && lo <= hi && hi <= MaxScheduleMph))
                errors.Add($"{source}: park '{id}' environment.windSchedule needs 0 <= minMph <= maxMph <= {MaxScheduleMph}; "
                    + $"got {s.MinMph?.ToString(CultureInfo.InvariantCulture) ?? "null"} / {s.MaxMph?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
            if (s.NightMul is { } n && !(n >= 1 && n <= 2))
                errors.Add($"{source}: park '{id}' environment.windSchedule.nightMul must be between 1 and 2; got {n}");
        }
    }

    /// <summary>The strongest scheduled wind a park may name: a gust, not a gale.</summary>
    const double MaxScheduleMph = 25;

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

    /// <summary>The crews (WD-28): unique ids with a name and a line; a rival names another crew; a character names at most
    /// two known crews, none twice.</summary>
    static void ValidateCrews(ContentData data, List<string> errors)
    {
        var src = data.CrewsSource;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var rows = data.Crews.Crews ?? [];
        foreach (var c in rows)
        {
            if (c is null) { errors.Add($"{src}: a crew row must be an object; got null"); continue; }
            if (string.IsNullOrWhiteSpace(c.Id) || !ids.Add(c.Id)) errors.Add($"{src}: crew id '{c.Id}' is empty or repeated");
            if (string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.Desc)) errors.Add($"{src}: crew '{c.Id}' needs a name and a desc");
        }
        foreach (var c in rows)
            if (c?.Rival is { } r && (!ids.Contains(r) || r == c.Id))
                errors.Add($"{src}: crew '{c.Id}' rival '{r}' is not another crew");
        foreach (var row in data.Characters)
        {
            var mine = row.Value.Crews ?? [];
            if (mine.Count > 2) errors.Add($"{row.Source}: character '{row.Value.Id}' names {mine.Count} crews; at most 2");
            if (mine.Distinct(StringComparer.Ordinal).Count() != mine.Count) errors.Add($"{row.Source}: character '{row.Value.Id}' names a crew twice");
            foreach (var id in mine)
                if (id is null || !ids.Contains(id)) errors.Add($"{row.Source}: character '{row.Value.Id}' crew '{id}' is not a row in {src}");
        }
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
    public CrewsFile Crews { get; set; } = new();
    public string CrewsSource { get; set; } = "";
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
    public WorldDto World { get; set; } = new();
    public SpeciesFile Species { get; set; } = new();
    public string SpeciesSource { get; set; } = "";
    /// <summary>Where the world file was read from — named by a park whose region it does not have.</summary>
    public string WorldSource { get; set; } = "";
}

internal readonly record struct Sourced<T>(T Value, string Source);

internal sealed class CharacterDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public bool Captain { get; set; }
    /// <summary>A bar is derived (spec §2). Read only so the validator can refuse a row that still authors one.</summary>
    public int? Pitch { get; set; }

    /// <inheritdoc cref="Pitch"/>
    public int? Bat { get; set; }

    /// <inheritdoc cref="Pitch"/>
    public int? Field { get; set; }

    /// <summary>Run: speed. The Run bar is this one sub-stat. Required.</summary>
    public int? Run { get; set; }

    /// <summary>Field: throw speed. Required.</summary>
    public int? Arm { get; set; }

    /// <summary>Field: hands. Required.</summary>
    public int? Hands { get; set; }

    /// <summary>Bat: contact. Required.</summary>
    public int? Contact { get; set; }

    /// <summary>Bat: power. Required.</summary>
    public int? Power { get; set; }

    /// <summary>Pitch: power (the pitch's speed). Required.</summary>
    public int? Velocity { get; set; }

    /// <summary>Pitch: break. Required.</summary>
    public int? Movement { get; set; }

    /// <summary>Pitch: control. Required.</summary>
    public int? Control { get; set; }

    /// <summary>Pitch: stamina. Required.</summary>
    public int? Endurance { get; set; }

    /// <summary>The nine sub-stats by their JSON key, in bar order (Bat, Pitch, Field, Run).</summary>
    public IEnumerable<(string Key, int? Value)> SubStats() =>
    [
        ("contact", Contact), ("power", Power),
        ("velocity", Velocity), ("endurance", Endurance), ("control", Control), ("movement", Movement),
        ("hands", Hands), ("arm", Arm),
        ("run", Run)
    ];

    /// <summary>Only after <see cref="ContentDataValidator.Load"/> has refused a row that misses a sub-stat.</summary>
    public Stats ToStats() => new(
        Need(Contact), Need(Power), Need(Velocity), Need(Endurance), Need(Control), Need(Movement),
        Need(Hands), Need(Arm), Need(Run));

    int Need(int? value) => value ?? throw new InvalidDataException($"character '{Id}' is missing a sub-stat; the validator should have refused it");

    /// <summary>
    /// Retired: reach is the body class's (spec §8.1). Read only so the validator can refuse a row that still authors it — the
    /// loader drops unknown keys, so a stale <c>reachFt</c> would otherwise sit in the file as if it counted.
    /// </summary>
    public double? ReachFt { get; set; }

    /// <summary>
    /// The body class this character plays (<c>data/rules/body-classes.json</c>). Required on a captain; a role player that
    /// names none wears its captain's.
    /// </summary>
    public string? BodyClass { get; set; }

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
    /// <summary>The name a tight tile shows when the full name will not fit (the captain board); null is the full name.</summary>
    public string? ShortName { get; set; }
    public string? SignatureBat { get; set; }
    public ProportionsDto? Proportions { get; set; }
    /// <summary>Up to two crews (WD-28, <c>data/chemistry/crews.json</c>); none is fine.</summary>
    public List<string?>? Crews { get; set; }
    /// <summary>A sidekick's species (WD-27, <c>data/world/species.json</c>): its body. Required on a sidekick; a captain names none.</summary>
    public string? Species { get; set; }

    public Character ToCharacter() => new(
        Id, Name, Faction, Captain,
        ToStats(),
        ParseHand(Bats), ParseHand(Throws),
        StarPitch, StarSwing, FieldAbility, Bio)
    {
        BodyClass = BodyClass ?? "",
        Repertoire = ParseRepertoire(),
        TeamName = TeamName,
        ShortName = ShortName ?? "",
        SignatureBat = SignatureBat,
        BodyType = Captain ? Id.ToLowerInvariant() : "",
        Proportions = Proportions?.ToSpec() ?? default,
        Species = Species ?? "",
        CrewIds = string.Join(",", (Crews ?? []).Where(c => c is not null))
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
    /// <summary>
    /// The region of <c>data/world/regions.json</c> this park stands in (WD-05). Required; no two parks share one. Catalog
    /// data for the map (<see cref="ContentCatalog.World"/>), deliberately off <see cref="Park"/>: no rule of play reads it.
    /// </summary>
    public string Region { get; set; } = "";
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
[OnlyKeys("night keeps the stadium lights, so a night block names the park's night-only hazards, the day hazard types the night clears (and, once "
    + "F6-c defines them, the view outside the stadium) and never a rule of the at-bat, the flight, the ground "
    + "or the bodies (FD-11-R2)")]
internal sealed class ParkNightDto
{
    /// <summary>The night-only instances, each validated like a day instance and each a hazard the switch removes.</summary>
    public List<HazardDto?>? Hazards { get; set; }

    /// <summary>The day's hazard types the night clears (the dust devils: clear night air). Each must be a type the day authors.</summary>
    public List<string>? Without { get; set; }

    /// <summary>Only after <see cref="ContentDataValidator"/> has accepted the block, which is when a catalog is built.</summary>
    public ParkNight ToNight() => new(ParkDto.ToHazards(Hazards), Without is { Count: > 0 } w ? w.ToArray() : null);
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
    /// <summary>The park's wind turns each inning (<see cref="Sim.WindSchedule"/>); absent is the fixed wind.</summary>
    public WindScheduleDto? WindSchedule { get; set; }

    public ParkEnvironment ToEnvironment() => new(DragMul, WindMul,
        WindSchedule is { } w ? new WindSchedule(w.MinMph ?? 0, w.MaxMph ?? 0, w.NightMul ?? 1) : null);
}

internal sealed class WindScheduleDto
{
    public double? MinMph { get; set; }
    public double? MaxMph { get; set; }
    /// <summary>The night gusts, as a multiple of the day's speed (1 or more).</summary>
    public double? NightMul { get; set; }
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
    /// <summary>A pitch's vanish (<see cref="PitchVanish"/>); pitches only.</summary>
    public PitchVanishDto? Vanish { get; set; }
    /// <summary>A swing's dust bowl at its first landing (<see cref="SwingDustBowl"/>); swings only.</summary>
    public SwingDustBowlDto? DustBowl { get; set; }
    /// <summary>A pitch's float (<see cref="PitchFloat"/>); pitches only.</summary>
    public PitchFloatDto? Float { get; set; }
    /// <summary>A pitch's leap (<see cref="PitchLeap"/>); pitches only.</summary>
    public PitchLeapDto? Leap { get; set; }
    /// <summary>A pitch's late rise (<see cref="PitchRise"/>); pitches only.</summary>
    public PitchRiseDto? Rise { get; set; }
    /// <summary>A swing's Perfect ring is this many times the ordinary one (<see cref="StarSwingSkill.PerfectRingMul"/>); swings only.</summary>
    public double? PerfectRingMul { get; set; }
    /// <summary>A pitch's loop (<see cref="PitchLoop"/>); pitches only.</summary>
    public PitchLoopDto? Loop { get; set; }
    /// <summary>A swing's ball stands still this long at its first hop (<see cref="StarSwingSkill.FirstHopStallSec"/>); swings only.</summary>
    public double? FirstHopStallSec { get; set; }
    /// <summary>The share of its speed a stalled ball runs on at (<see cref="StarSwingSkill.FirstHopStallSpeedMul"/>); swings only.</summary>
    public double? FirstHopStallSpeedMul { get; set; }
    /// <summary>A pitch's sway (<see cref="PitchSway"/>); pitches only.</summary>
    public PitchSwayDto? Sway { get; set; }
    /// <summary>A pitch's pendulum (<see cref="PitchPendulum"/>); pitches only.</summary>
    public PitchPendulumDto? Pendulum { get; set; }
    /// <summary>A swing's jagged flight (<see cref="BallJag"/>); swings only.</summary>
    public BallJagDto? Jag { get; set; }
    /// <summary>A pitch's late drop (<see cref="PitchDrop"/>); pitches only.</summary>
    public PitchDropDto? Drop { get; set; }
    /// <summary>A swing's hot ball (<see cref="Sim.HotBall"/>); swings only.</summary>
    public HotBallDto? HotBall { get; set; }
    /// <summary>A swing's first hop springs this many times as fast upward (<see cref="StarSwingSkill.FirstHopBounceMul"/>); swings only.</summary>
    public double? FirstHopBounceMul { get; set; }
    /// <summary>A swing's ball rides the park's wind this many times as hard (<see cref="StarSwingSkill.WindMul"/>); swings only.</summary>
    public double? WindMul { get; set; }
    /// <summary>The effect family a captain's special belongs to (§13, AB-02); no two captains' specials share one.</summary>
    public string? Family { get; set; }
    /// <summary>Retired (§12): the carrier sets the price. Read only so a stale row is refused by name.</summary>
    public string? Tier { get; set; }

    public StarPitchSkill ToPitch() => new(Id, Name, Kind, SpeedMul ?? 1.0, StaminaCost ?? 0,
        LateBreak, Decoy, OnCatch,
        Float is null ? null : new PitchFloat(Float.RiseFt, Float.DropFrom),
        Leap is null ? null : new PitchLeap(Leap.At, Leap.HoldSpan, Leap.HoldPace),
        Rise is null ? null : new PitchRise(Rise.RiseFt, Rise.From),
        Loop is null ? null : new PitchLoop(Loop.At, Loop.Span, Loop.DiameterFt),
        Sway is null ? null : new PitchSway(Sway.WidthFt, Sway.Cycles, Sway.PeakAt, Sway.SettleBy),
        Pendulum is null ? null : new PitchPendulum(Pendulum.LengthFt, Pendulum.SwingDeg, Pendulum.WidestAt),
        Drop is null ? null : new PitchDrop(Drop.DropFt, Drop.From),
        Vanish is null ? null : new PitchVanish(Vanish.From, Vanish.To));

    public StarSwingSkill ToSwing() => new(Id, Name, Kind, ExitVeloMul ?? 1.0, LaunchDeg, Terrain,
        FielderPauseSec ?? 0, InfieldChaos, Decoy,
        WindMul ?? 1, FirstHopBounceMul ?? 1, FirstHopStallSec ?? 0, FirstHopStallSpeedMul ?? 1, PerfectRingMul ?? 1,
        Jag is null ? null : new BallJag(Jag.OffsetFt, Jag.FirstAt, Jag.SecondAt, Jag.Span),
        HotBall is null ? null : new Sim.HotBall(HotBall.MoltenSec, HotBall.HoldSec),
        DustBowl is null ? null : new SwingDustBowl(DustBowl.RadiusFt, DustBowl.Sec, DustBowl.Mul));
}

internal sealed class PitchDropDto
{
    public double DropFt { get; set; }
    /// <summary>The share of the flight after which the ball starts to drop (a fraction, not seconds).</summary>
    public double From { get; set; }
}

internal sealed class HotBallDto
{
    public double MoltenSec { get; set; }
    public double HoldSec { get; set; }
}

internal sealed class PitchLoopDto
{
    public double At { get; set; }
    /// <summary>The share of the flight the loop takes (a fraction, not seconds).</summary>
    public double Span { get; set; }
    public double DiameterFt { get; set; }
}

internal sealed class PitchRiseDto
{
    public double RiseFt { get; set; }
    /// <summary>The share of the flight after which the ball starts to rise (a fraction, not seconds).</summary>
    public double From { get; set; }
}

internal sealed class PitchPendulumDto
{
    public double LengthFt { get; set; }
    public double SwingDeg { get; set; }
    public double WidestAt { get; set; }
}

internal sealed class BallJagDto
{
    public double OffsetFt { get; set; }
    /// <summary>Shares of the jag window (a fraction, not seconds).</summary>
    public double FirstAt { get; set; }
    public double SecondAt { get; set; }
    public double Span { get; set; }
}

internal sealed class PitchLeapDto
{
    public double At { get; set; }
    /// <summary>The share of the flight the ball crawls for (a fraction, not seconds).</summary>
    public double HoldSpan { get; set; }
    public double HoldPace { get; set; }
}

internal sealed class PitchSwayDto
{
    public double WidthFt { get; set; }
    public double Cycles { get; set; }
    public double PeakAt { get; set; }
    public double SettleBy { get; set; }
}

internal sealed class PitchFloatDto
{
    public double RiseFt { get; set; }
    public double DropFrom { get; set; }
}

internal sealed class PitchVanishDto
{
    /// <summary>The share of the flight the ball vanishes at (a fraction, not seconds).</summary>
    public double From { get; set; }
    /// <summary>The share of the flight it shows again at.</summary>
    public double To { get; set; }
}

internal sealed class SwingDustBowlDto
{
    public double RadiusFt { get; set; }
    /// <summary>Seconds from the first landing.</summary>
    public double Sec { get; set; }
    /// <summary>What a fielder's step inside the bowl is multiplied by.</summary>
    public double Mul { get; set; }
}
