using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrandSluggers.Sim;

public sealed class ContentCatalog
{
    public IReadOnlyDictionary<string, Character> Characters { get; }
    public IReadOnlyDictionary<string, Park> Parks { get; }
    /// <summary>
    /// The park ids in the field-pick cycle's declared order, from each park file's <c>pickOrder</c>
    /// (§16, FR-04). The pregame cycle used to be a six-id literal in <see cref="ExhibitionPick"/>, so a
    /// park file could not be the one place a park is declared; a directory listing would be
    /// alphabetical, which is a different order (#820).
    /// </summary>
    public IReadOnlyList<string> ParkPickOrder { get; }
    /// <summary>Faction to the park that faction plays at home. One park per faction; the validator refuses a second.</summary>
    readonly IReadOnlyDictionary<string, string> _parkOfFaction;
    public IReadOnlyDictionary<string, BatItem> Bats { get; }
    public IReadOnlyDictionary<string, GloveItem> Gloves { get; }
    public ChemistryTable Chemistry { get; }
    public CameraShots Shots { get; }
    public FeelTable Feel { get; }
    /// <summary>The rule numbers of play (data/rules/*.json, spec §16).</summary>
    public RulesTable Rules { get; }
    /// <summary>The star skills as data (data/abilities/star-skills.json, spec §13).</summary>
    public StarSkillTable StarSkills { get; }
    public ArtCatalog Art { get; }
    /// <summary>The continent and the region each park stands in (data/world/regions.json; WD-05).</summary>
    public WorldMap World { get; private init; } = null!;

    /// <summary>The crews by id (WD-28, <c>data/chemistry/crews.json</c>): the lineup's badges and the chemistry reasons name them.</summary>
    public IReadOnlyDictionary<string, Crew> Crews { get; private init; } = new Dictionary<string, Crew>();

    /// <summary>A crew's name as a player reads it; an id the catalog lacks reads as itself.</summary>
    public string CrewName(string id) => Crews.TryGetValue(id, out var crew) ? crew.Name : id;

    /// <summary>The sidekick species by id (WD-27, <c>data/world/species.json</c>).</summary>
    public IReadOnlyDictionary<string, Species> Species { get; private init; } = new Dictionary<string, Species>();

    /// <summary>Where this catalog was read from: the data root, and the trial overlay laid over it.</summary>
    public DataRoot Root { get; }

    /// <summary>The captains in select order (<c>data/teams/teams.json</c>): the captain sheet, the pick and Challenge walk it.</summary>
    public IReadOnlyList<string> CaptainIds { get; private init; } = [];

    IReadOnlyDictionary<string, (string Name, IReadOnlyList<string> Roster)> _presets { get; init; } =
        new Dictionary<string, (string, IReadOnlyList<string>)>();

    /// <summary>An authored nine from <c>data/teams/teams.json</c>, by id; its first name is its captain.</summary>
    public Team PresetTeam(string id) =>
        _presets.TryGetValue(id, out var p)
            ? Team(p.Name, p.Roster[0], p.Roster.Skip(1).ToArray())
            : throw new KeyNotFoundException($"No preset team '{id}'; the presets are {string.Join(", ", _presets.Keys)}");

    /// <summary>The captain after (or before, <paramref name="step"/> −1) this one in select order, wrapping.</summary>
    public string StepCaptain(string captainId, int step)
    {
        var i = IndexOfCaptain(captainId);
        var n = CaptainIds.Count;
        return CaptainIds[((i + step) % n + n) % n];
    }

    /// <summary>A captain's place in select order; an id that is not a captain is the first.</summary>
    public int IndexOfCaptain(string captainId)
    {
        for (var i = 0; i < CaptainIds.Count; i++)
            if (CaptainIds[i].Equals(captainId, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }

    ContentCatalog(
        DataRoot root,
        Dictionary<string, Character> characters,
        Dictionary<string, Park> parks,
        IReadOnlyList<string> parkPickOrder,
        Dictionary<string, BatItem> bats,
        Dictionary<string, GloveItem> gloves,
        ChemistryTable chemistry,
        CameraShots shots,
        FeelTable feel,
        RulesTable rules,
        StarSkillTable starSkills,
        ArtCatalog art)
    {
        Root = root;
        StarSkills = starSkills;
        Characters = characters;
        Parks = parks;
        ParkPickOrder = parkPickOrder;
        _parkOfFaction = parkPickOrder
            .Select(id => parks[id])
            .Where(p => !string.IsNullOrWhiteSpace(p.Faction))
            .ToDictionary(p => p.Faction, p => p.Id, StringComparer.OrdinalIgnoreCase);
        Bats = bats;
        Gloves = gloves;
        Chemistry = chemistry;
        Shots = shots;
        Feel = feel;
        Rules = rules;
        Art = art;
    }

    public static ContentCatalog Load(DataRoot? dataRoot = null)
    {
        var root = dataRoot ?? FindDataRoot();
        var data = ContentDataValidator.Load(root);

        var characters = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Characters)
            characters.Add(row.Value.Id, row.Value.ToCharacter());
        // A sidekick wears its species' body (WD-27): the species' own proportions, else its build's. It keeps its faction
        // captain's rig variant and body class (§8.1), unless it names its own class. Resolved once, here, from the data;
        // the validator has already refused a sidekick with no species or no captain.
        var builds = (data.Species.Builds ?? []).ToDictionary(kv => kv.Key, kv => kv.Value!.ToSpec(), StringComparer.OrdinalIgnoreCase);
        var species = (data.Species.Species ?? []).Select(s => s!).ToDictionary(
            s => s.Id,
            s => new Species(s.Id, s.Name, s.Faction, s.Blend, s.Build, s.Look, s.StarPitch, s.StarSwing, s.FieldAbility,
                s.Proportions?.ToSpec() ?? builds[s.Build]),
            StringComparer.OrdinalIgnoreCase);
        var captainOf = characters.Values.Where(c => c.Captain)
            .ToDictionary(c => c.Faction, c => c, StringComparer.OrdinalIgnoreCase);
        foreach (var c in characters.Values.Where(c => !c.Captain).ToList())
        {
            var cap = captainOf[c.Faction];
            characters[c.Id] = c with
            {
                BodyType = cap.BodyType,
                Proportions = species[c.Species].Proportions,
                BodyClass = string.IsNullOrEmpty(c.BodyClass) ? cap.BodyClass : c.BodyClass
            };
        }
        var captainIds = data.Teams.Captains!.Select(id => characters[id!].Id).ToList();
        var presets = (data.Teams.Presets ?? []).ToDictionary(
            p => p!.Id, p => (p!.Name, Roster: (IReadOnlyList<string>)p.Roster!.Select(id => characters[id!].Id).ToList()),
            StringComparer.OrdinalIgnoreCase);

        var parks = new Dictionary<string, Park>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Parks)
            parks.Add(row.Value.Id, row.Value.ToPark());
        // The cycle the pregame field pick walks, from the data (#820). `pickOrder` is validated present
        // and unique before this runs, so the sort is total and the same on every filesystem.
        var parkPickOrder = data.Parks
            .OrderBy(row => row.Value.PickOrder ?? int.MaxValue)
            .Select(row => row.Value.Id)
            .ToList();

        var bats = new Dictionary<string, BatItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Bats)
        {
            var dto = row.Value;
            bats[dto.Id] = new BatItem(
                dto.Id, dto.Name, dto.ContactMod, dto.PowerMod, dto.ChargeAlwaysFull,
                string.IsNullOrWhiteSpace(dto.Visual) ? "bat-wood" : dto.Visual);
        }

        var gloves = new Dictionary<string, GloveItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Gloves)
        {
            var dto = row.Value;
            gloves[dto.Id] = new GloveItem(
                dto.Id, dto.Name, dto.ErrorReduction, dto.ArmMod,
                string.IsNullOrWhiteSpace(dto.Visual) ? "glove-brown" : dto.Visual);
        }

        var rules = data.Rules ?? throw new InvalidOperationException("content data read no rules table");
        var crews = (data.Crews.Crews ?? []).Select(c => new Crew(c!.Id, c.Name, c.Desc, c.Rival)).ToList();
        var chemistry = new ChemistryTable(characters.Values, data.Chemistry, rules, crews);
        var shots = CameraShots.Load(root);
        var feel = FeelTable.Load(root);
        var art = ArtCatalog.Load(root);
        // The walk / run take reads the body's own pursuit profile (#1111): the rules' chase speeds and the feel share.
        art.Gait = new GaitProfile(rules, feel.GaitRunOfPursuit);
        // A body moves in the style its body class names (data/rules/body-classes.json motionStyle).
        art.Classes = rules.BodyClasses;
        var starPitches = new Dictionary<string, StarPitchSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, dto) in data.StarSkills.Pitches ?? [])
            if (dto is not null) starPitches[id] = dto.ToPitch();
        var starSwings = new Dictionary<string, StarSwingSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, dto) in data.StarSkills.Swings ?? [])
            if (dto is not null) starSwings[id] = dto.ToSwing();
        var starSkills = new StarSkillTable(starPitches, starSwings);
        // Juice by weight (CH-13): every body class has its juice row in the feel table, and every row dresses a class.
        var juiceGaps = feel.WeightJuice.Coverage(rules.BodyClasses);
        if (juiceGaps.Count > 0)
            throw new InvalidDataException("Invalid weight juice:" + Environment.NewLine
                + string.Join(Environment.NewLine, juiceGaps.Select(e => "  - " + e)));
        var world = WorldMap.From(data.World,
            data.Parks.ToDictionary(row => row.Value.Id, row => row.Value.Region, StringComparer.OrdinalIgnoreCase));
        return new ContentCatalog(root, characters, parks, parkPickOrder, bats, gloves, chemistry, shots, feel, rules, starSkills, art)
        {
            World = world,
            Crews = crews.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase),
            Species = species,
            CaptainIds = captainIds,
            _presets = presets
        };
    }

    public Character Must(string id) =>
        Characters.TryGetValue(id, out var c) ? c : throw new KeyNotFoundException($"No character '{id}'");

    /// <summary>
    /// The park an id names, or a stop. An id the catalog does not have used to fall back to Harbor in
    /// silence, so a typo played the control park and the trace claimed the park that was asked for (#820).
    /// </summary>
    public Park MustPark(string id) =>
        Parks.TryGetValue(id, out var p) ? p
            : throw new KeyNotFoundException($"No park '{id}'; the fields are {string.Join(", ", ParkPickOrder)}");

    /// <summary>
    /// The park a faction plays at home: the park whose <c>faction</c> is that one, else the default park
    /// (D21). A faction with no park of its own — every roster faction that is not a park's — is at Harbor.
    /// </summary>
    public string HomeParkIdOfFaction(string faction) =>
        faction is not null && _parkOfFaction.TryGetValue(faction, out var id) ? id : ExhibitionPick.DefaultPark;

    public Team Team(string name, string captainId, params string[] rosterIds)
    {
        var captain = Must(captainId);
        var roster = new List<Character> { captain };
        foreach (var id in rosterIds)
        {
            var c = Must(id);
            if (!roster.Any(x => x.Id.Equals(c.Id, StringComparison.OrdinalIgnoreCase)))
                roster.Add(c);
        }
        return new Team(name, captain, roster);
    }

    static DataRoot FindDataRoot() =>
        TryFindDataRoot()
        ?? throw new DirectoryNotFoundException("Could not find data/characters from " + AppContext.BaseDirectory);

    /// <summary>
    /// The environment variable that points a whole process at another data root. Set it to play
    /// a trial profile — a different infield, a different park table — against the same binary,
    /// without editing the shipped data. It is read once per process and never written, so the
    /// control and the trial are two runs to diff, not two tables inside one run.
    /// </summary>
    public const string DataRootVariable = "GRAND_SLUGGERS_DATA";

    /// <summary>
    /// The root <see cref="DataRootVariable"/> names, or null when it is unset. A value that is not
    /// a data root is a mistake worth stopping on: falling back to the shipped data would quietly
    /// run the control while the operator believed they were running the trial.
    /// </summary>
    public static string? NamedDataRoot(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Directory.Exists(Path.Combine(value, "characters"))) return value;
        throw new DirectoryNotFoundException(
            $"{DataRootVariable}={value} has no characters/ — point it at a data root, not at one folder inside it");
    }

    /// <summary>
    /// The data root this process runs on — the one <see cref="DataRootVariable"/> names, else the
    /// one above the running binary — with whatever trial overlay <see cref="DataRoot.OverlayVariable"/>
    /// lays over it. Null when the binary sits elsewhere (Unity passes its own root).
    ///
    /// The overlay rides on the root the environment resolves, never on a root a caller hands in:
    /// a fixture root asked for by name is exactly that root, or tests running in parallel would
    /// inherit a trial none of them asked for.
    /// </summary>
    public static DataRoot? TryFindDataRoot()
    {
        var named = NamedDataRoot(Environment.GetEnvironmentVariable(DataRootVariable));
        var shipped = named ?? RootAboveTheBinary();
        return shipped is null ? null : DataRoot.FromEnvironment(shipped, rootWasNamed: named is not null);
    }

    static string? RootAboveTheBinary()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(Path.Combine(candidate, "characters")))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }

}

/// <summary>A crew (WD-28, <c>data/chemistry/crews.json</c>): a shared trait across parks and builds.</summary>
public sealed record Crew(string Id, string Name, string Desc, string? Rival);

public sealed class CrewsFile
{
    [JsonPropertyName("crews")]
    public List<CrewDto?>? Crews { get; set; }
}

public sealed class CrewDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("desc")] public string Desc { get; set; } = "";
    [JsonPropertyName("rival")] public string? Rival { get; set; }
}

public sealed class ChemistryOverrides
{
    [JsonPropertyName("buddies")]
    public List<string[]> Buddies { get; set; } = [];

    [JsonPropertyName("rivals")]
    public List<string[]> Rivals { get; set; } = [];
}
