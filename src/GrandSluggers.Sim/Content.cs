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

        var characters = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "characters"), "*.json"))
        {
            var text = File.ReadAllText(file);
            if (Path.GetFileName(file).Equals("role-players.json", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var row in JsonSerializer.Deserialize<List<CharacterDto>>(text, json) ?? [])
                    characters[row.Id] = row.ToCharacter();
            }
            else
            {
                var row = JsonSerializer.Deserialize<CharacterDto>(text, json)
                    ?? throw new InvalidDataException($"Bad character file {file}");
                characters[row.Id] = row.ToCharacter();
            }
        }

        var parks = new Dictionary<string, Park>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "parks"), "*.json"))
        {
            var dto = JsonSerializer.Deserialize<ParkDto>(File.ReadAllText(file), json)
                ?? throw new InvalidDataException($"Bad park file {file}");
            parks[dto.Id] = dto.ToPark();
        }

        var bats = new Dictionary<string, BatItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "bats"), "*.json"))
        {
            var dto = JsonSerializer.Deserialize<BatDto>(File.ReadAllText(file), json)
                ?? throw new InvalidDataException($"Bad bat file {file}");
            bats[dto.Id] = new BatItem(
                dto.Id, dto.Name, dto.ContactMod, dto.PowerMod, dto.ChargeAlwaysFull,
                string.IsNullOrWhiteSpace(dto.Visual) ? "bat-wood" : dto.Visual);
        }

        var gloves = new Dictionary<string, GloveItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "gloves"), "*.json"))
        {
            var dto = JsonSerializer.Deserialize<GloveDto>(File.ReadAllText(file), json)
                ?? throw new InvalidDataException($"Bad glove file {file}");
            gloves[dto.Id] = new GloveItem(
                dto.Id, dto.Name, dto.ErrorReduction, dto.ArmMod,
                string.IsNullOrWhiteSpace(dto.Visual) ? "glove-brown" : dto.Visual);
        }

        var overridesPath = Path.Combine(root, "chemistry", "overrides.json");
        var overrides = JsonSerializer.Deserialize<ChemistryOverrides>(File.ReadAllText(overridesPath), json)
            ?? new ChemistryOverrides();

        var chemistry = new ChemistryTable(characters.Values, overrides);
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

    sealed class CharacterDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Faction { get; set; } = "";
        public bool Captain { get; set; }
        public int Pitch { get; set; }
        public int Bat { get; set; }
        public int Field { get; set; }
        public int Run { get; set; }
        public string Bats { get; set; } = "R";
        public string Throws { get; set; } = "R";
        public string StarPitch { get; set; } = "fastball";
        public string StarSwing { get; set; } = "line";
        public string FieldAbility { get; set; } = "dive";
        public string Bio { get; set; } = "";

        public Character ToCharacter() => new(
            Id, Name, Faction, Captain,
            new Stats(Pitch, Bat, Field, Run).Clamp(),
            ParseHand(Bats), ParseHand(Throws),
            StarPitch, StarSwing, FieldAbility, Bio);

        static Hand ParseHand(string s) =>
            s.Trim().ToUpperInvariant().StartsWith('L') ? Hand.L : Hand.R;
    }

    sealed class ParkDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Faction { get; set; } = "";
        public string Surface { get; set; } = "grass";
        public int LeftFenceFt { get; set; }
        public int CenterFenceFt { get; set; }
        public int RightFenceFt { get; set; }
        public double WindMph { get; set; }
        public List<HazardDto>? Hazards { get; set; }

        public Park ToPark() => new(
            Id, Name, Faction, Surface,
            LeftFenceFt, CenterFenceFt, RightFenceFt, WindMph,
            (Hazards ?? []).Select(h => new Hazard(h.Type, h.X, h.Z, h.Radius, h.Tag)).ToList());
    }

    sealed class HazardDto
    {
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Z { get; set; }
        public double Radius { get; set; }
        public string? Tag { get; set; }
    }

    sealed class BatDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int ContactMod { get; set; }
        public int PowerMod { get; set; }
        public bool ChargeAlwaysFull { get; set; }
        public string Visual { get; set; } = "";
    }

    sealed class GloveDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double ErrorReduction { get; set; }
        public int ArmMod { get; set; }
        public string Visual { get; set; } = "";
    }
}

public sealed class ChemistryOverrides
{
    [JsonPropertyName("buddies")]
    public List<string[]> Buddies { get; set; } = [];

    [JsonPropertyName("rivals")]
    public List<string[]> Rivals { get; set; } = [];
}
