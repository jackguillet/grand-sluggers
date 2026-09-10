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
    public ArtCatalog Art { get; }
    public string Root { get; }

    ContentCatalog(
        string root,
        Dictionary<string, Character> characters,
        Dictionary<string, Park> parks,
        Dictionary<string, BatItem> bats,
        Dictionary<string, GloveItem> gloves,
        ChemistryTable chemistry,
        CameraShots shots,
        FeelTable feel,
        ArtCatalog art)
    {
        Root = root;
        Characters = characters;
        Parks = parks;
        Bats = bats;
        Gloves = gloves;
        Chemistry = chemistry;
        Shots = shots;
        Feel = feel;
        Art = art;
    }

    public static ContentCatalog Load(string? dataRoot = null)
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

        var chemistry = new ChemistryTable(characters.Values, data.Chemistry);
        var shots = CameraShots.Load(root);
        var feel = FeelTable.Load(root);
        var art = ArtCatalog.Load(root);
        return new ContentCatalog(root, characters, parks, bats, gloves, chemistry, shots, feel, art);
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

    static string FindDataRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(Path.Combine(candidate, "characters")))
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find data/characters from " + AppContext.BaseDirectory);
    }

}

public sealed class ChemistryOverrides
{
    [JsonPropertyName("buddies")]
    public List<string[]> Buddies { get; set; } = [];

    [JsonPropertyName("rivals")]
    public List<string[]> Rivals { get; set; } = [];
}
