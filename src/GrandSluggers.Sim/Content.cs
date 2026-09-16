using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrandSluggers.Sim;

public sealed class ContentCatalog
{
    public IReadOnlyDictionary<string, Character> Characters { get; }
    public IReadOnlyDictionary<string, Park> Parks { get; }
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

    /// <summary>Where this catalog was read from: the data root, and the trial overlay laid over it.</summary>
    public DataRoot Root { get; }

    ContentCatalog(
        DataRoot root,
        Dictionary<string, Character> characters,
        Dictionary<string, Park> parks,
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
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var data = ContentDataValidator.Load(root, json);

        var characters = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Characters)
            characters.Add(row.Value.Id, row.Value.ToCharacter());

        var parks = new Dictionary<string, Park>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Parks)
            parks.Add(row.Value.Id, row.Value.ToPark());

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

        var rules = data.Rules ?? RulesTable.Defaults;
        var chemistry = new ChemistryTable(characters.Values, data.Chemistry, rules);
        var shots = CameraShots.Load(root);
        var feel = FeelTable.Load(root);
        var art = ArtCatalog.Load(root);
        var starPitches = new Dictionary<string, StarPitchSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, dto) in data.StarSkills.Pitches ?? [])
            if (dto is not null) starPitches[id] = dto.ToPitch();
        var starSwings = new Dictionary<string, StarSwingSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, dto) in data.StarSkills.Swings ?? [])
            if (dto is not null) starSwings[id] = dto.ToSwing();
        var starSkills = new StarSkillTable(starPitches, starSwings);
        return new ContentCatalog(root, characters, parks, bats, gloves, chemistry, shots, feel, rules, starSkills, art);
    }

    public Character Must(string id) =>
        Characters.TryGetValue(id, out var c) ? c : throw new KeyNotFoundException($"No character '{id}'");

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
        var shipped = ShippedRoot();
        return shipped is null ? null : DataRoot.FromEnvironment(shipped);
    }

    static string? ShippedRoot()
    {
        var named = NamedDataRoot(Environment.GetEnvironmentVariable(DataRootVariable));
        if (named is not null) return named;

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

public sealed class ChemistryOverrides
{
    [JsonPropertyName("buddies")]
    public List<string[]> Buddies { get; set; } = [];

    [JsonPropertyName("rivals")]
    public List<string[]> Rivals { get; set; } = [];
}
