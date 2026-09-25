using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// <c>SF-25</c>: the night block (§0.3, §14, §16; FD-11 B, FD-11-R2, FD-10-R1; F4-d, #895). Night keeps the
/// stadium lights (Jack, September 22, 2026: "for night time, we will still have stadium lights. the only
/// thing that changes is stadium outside view, and hazards."), so a park's optional <c>night</c> block names
/// the hazard instances that exist only at night — and, once F6-c defines them, look fields — and never a
/// rule of the at-bat, the flight, the ground or the bodies.
///
/// <para>
/// <b>One resolution.</b> A match resolves its park once (<see cref="PlayedPark.Of"/>): the day's instances,
/// then the night block's at night, then the hazards switch (FD-10-R1). Every reader plays that park's list,
/// so these rows read the park a match holds and never the block itself, except where the row is about the
/// block as authored.
/// </para>
///
/// <para>
/// <b>Parity.</b> Funfair's chompers moved into its night block at their exact places and radii, and Ember's
/// breath keeps its type's own night number where it was, so their night games are the games they were:
/// pinned against the log of the build before the move (<see cref="Before"/>). Crystal's night contact window
/// is dropped, so night at the rink changes, on purpose; the park-factors report in PR #895
/// shows by how much, and nothing is tuned (FD-13).
/// </para>
/// </summary>
public sealed class NightBlockTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    static readonly string ShippedPath = Game.Root.Shipped;

    static Park Played(ContentCatalog content, string id, bool night, bool hazards = true) =>
        PlayedPark.Of(content.Parks[id], night, hazards, content.Rules.Hazards);

    // ---------------------------------------------------------------------------------
    // The block exists only at night
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-25</c>, every park in the pick order. By day the match plays the day block and
    /// nothing else; at night the day block and then the night block, in file order; the park it holds carries
    /// no night block of its own either way; and every other member is the catalog's. A park with no night
    /// block is the catalog's own object by day and at night, and a match plays the same resolved table by day
    /// and at night, so night cannot have reached a rule (<c>SF-01</c>, FD-11-R2).
    /// </summary>
    [Fact]
    public void SF25_ANightBlocksHazardsExistOnlyAtNight()
    {
        var content = Game;
        var withBlock = 0;
        foreach (var id in content.ParkPickOrder)
        {
            var park = content.Parks[id];
            var dayMatch = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 7, parkId: id);
            var nightMatch = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 7, parkId: id, night: true);
            var night = park.Night?.Hazards ?? [];

            Assert.Null(dayMatch.Park.Night);
            Assert.Null(nightMatch.Park.Night);
            Assert.Equal(park.Hazards, dayMatch.Park.Hazards);
            // Night adds the block's instances and clears the day types it names (the mesa's dust devils), nothing else.
            var cleared = park.Night?.Without ?? [];
            var tonight = park.Hazards.Where(h => !cleared.Contains(h.Type)).Concat(night).ToList();
            Assert.Equal(tonight, nightMatch.Park.Hazards);
            // The same instances, not copies: the resolution moves no hazard and resizes none.
            Assert.All(nightMatch.Park.Hazards, h => Assert.Contains(tonight, a => ReferenceEquals(a, h)));
            Assert.Equal(park with { Night = null }, dayMatch.Park with { Hazards = park.Hazards });
            Assert.Equal(park with { Night = null }, nightMatch.Park with { Hazards = park.Hazards });
            // Night reaches no rule (FD-11-R2): the same table, by reference at a park with no air of its own, by value at
            // one that names it (Crystal, F9-a).
            if (park.Environment is null) Assert.Same(dayMatch.Rules, nightMatch.Rules);
            else Assert.Equal((dayMatch.Rules.Flight.Drag, dayMatch.Rules.Flight.WindMul), (nightMatch.Rules.Flight.Drag, nightMatch.Rules.Flight.WindMul));

            if (park.Night is null)
            {
                Assert.Same(park, dayMatch.Park);
                Assert.Same(park, nightMatch.Park);
                continue;
            }
            withBlock++;
            Assert.True(night.Count > 0 || cleared.Count > 0, id);
            Assert.All(night, h => Assert.DoesNotContain(dayMatch.Park.Hazards, d => ReferenceEquals(d, h)));
        }
        // Not vacuous: Funfair's mouths are a night block, and Sunscorch's clear night air takes the dust devils away.
        Assert.Equal(2, withBlock);
        Assert.Equal([HazardType.DustDevil], content.Parks[ParkId.Sunscorch].Night!.Without);
        Assert.Equal(
            ["L", "C", "R"],
            content.Parks[ParkId.Funfair].Night!.Hazards.Select(h => h.Type == HazardType.Chomper ? h.Tag : "not a chomper"));
    }

    /// <summary>
    /// The resolution is its own fixed point: the park a match plays, resolved again with hazards on, is itself.
    /// <c>ParkView</c> resolves whatever park it is handed — the match's played park or, on the title, the
    /// catalog's — so a played park must not gain its night block twice, and a hazards-off park must not gain
    /// its hazards back.
    /// </summary>
    [Fact]
    public void SF25_ThePlayedParkResolvesToItself()
    {
        var content = Game;
        foreach (var id in content.ParkPickOrder)
            foreach (var night in new[] { false, true })
                foreach (var hazards in new[] { true, false })
                {
                    var played = Played(content, id, night, hazards);
                    Assert.Same(played, PlayedPark.Of(played, night, hazards: true, content.Rules.Hazards));
                }
    }

    // ---------------------------------------------------------------------------------
    // The switch removes them (FD-10-R1)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-25</c> with <c>SF-24</c>: night hazard instances are hazards, so the hazards switch
    /// removes every one of them (FD-11, FD-10-R1). The switch is the last step of the one resolution, so a
    /// hazards-off night is the hazards-on night with the switch applied, and at Funfair it keeps the boxcar
    /// and loses the cans and every mouth. Every night-block instance is of a pattern that counts
    /// as a hazard — the validator refuses any other (<see cref="SF25_ANightBlockIsValidatedLikeTheDayBlock"/>).
    /// </summary>
    [Fact]
    public void SF25_TheHazardsSwitchRemovesTheNightBlocksHazards()
    {
        var content = Game;
        var library = content.Rules.Hazards;
        foreach (var id in content.ParkPickOrder)
        {
            var on = Played(content, id, night: true);
            var off = Played(content, id, night: true, hazards: false);
            var switched = HazardPattern.HazardsOff(on, library);
            Assert.Equal(switched.Hazards, off.Hazards);
            Assert.Equal(switched with { Hazards = off.Hazards }, off);
            Assert.DoesNotContain(off.Hazards, h => HazardPattern.IsHazard(library.Of(h.Type).Pattern));
            foreach (var h in content.Parks[id].Night?.Hazards ?? [])
            {
                Assert.True(HazardPattern.IsHazard(library.Of(h.Type).Pattern), $"{id} night {h.Type}");
                Assert.DoesNotContain(off.Hazards, k => ReferenceEquals(k, h));
            }
        }

        var match = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 7, parkId: ParkId.Funfair, night: true, hazards: false);
        Assert.Empty(match.Park.Hazards); // the train is a mover since F4-f, so the switch removes it too
        Assert.Null(match.Park.Night);
    }

    // ---------------------------------------------------------------------------------
    // The schema: hazards (and later look fields), never a rule
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-25</c> (the schema half, FD-11-R2): a night block that names a rule, a number or an
    /// unknown key is refused by name, with the reason, by the strict park read — a day park's rule
    /// (<c>windMph</c>, <c>fenceHeightFt</c>, the <c>environment</c> block), the dropped window under either
    /// spelling, and a look key F6-c has not defined yet. The window is refused at the park's top level as
    /// well, and so is a key inside a night hazard row (the <c>nightOnly</c> column that left the rows).
    /// </summary>
    [Fact]
    public void SF25_ANightBlockNamingARuleOrAnUnknownKeyIsRefusedByName()
    {
        using var fixture = new NightFixture();
        var named = new (string Key, JsonNode Value)[]
        {
            ("windMph", 12),
            ("fenceHeightFt", 9),
            ("nightContactWindowMul", 0.85),
            ("contactWindowMul", 0.85),
            ("environment", new JsonObject { ["dragMul"] = 1.2 }),
            ("sky", "stars")
        };
        fixture.Park(ParkId.Funfair, json =>
        {
            var night = json["night"]!.AsObject();
            foreach (var (key, value) in named) night[key] = value.DeepClone();
            night["hazards"]![0]!["nightOnly"] = true;
        });
        fixture.Park(ParkId.Crystal, json => json["nightContactWindowMul"] = 0.85);

        var errors = ContentDataValidator.Validate(fixture.Root());
        var funfair = fixture.ParkFile(ParkId.Funfair);
        foreach (var (key, _) in named)
        {
            var refusal = Assert.Single(errors, e => e.Contains($": night.{key} is not a key", StringComparison.Ordinal));
            Assert.StartsWith(funfair + ": ", refusal, StringComparison.Ordinal);
            Assert.Contains("the keys of parkNight are [hazards, without]", refusal, StringComparison.Ordinal);
            Assert.Contains("never a rule of the at-bat, the flight, the ground or the bodies (FD-11-R2)", refusal, StringComparison.Ordinal);
        }
        Assert.Contains(errors, e => e.StartsWith(funfair + ": night.hazards[0].nightOnly is not a key this file declares; the keys of hazard are [",
            StringComparison.Ordinal));
        Assert.Contains(errors, e => e.StartsWith(fixture.ParkFile(ParkId.Crystal) + ": nightContactWindowMul is not a key this file declares",
            StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => ContentCatalog.Load(fixture.Root()));
    }

    /// <summary>
    /// <c>SF-25</c>: the night block is validated like the day block (FD-11). A night instance is
    /// refused for what a day instance is refused for — a type outside the library, a missing disc, a disc on
    /// the base paths (FD-19) — and for one thing more: it must be an instance the hazards switch removes,
    /// because night hazard instances are hazards (FD-11, FD-10-R1). The placement rule measures the disc the
    /// instance plays at night, the type row's own <c>nightRadiusMul</c> (map finding 31): a breath whose own
    /// radius clears the lane and whose night disc does not is refused, in the night block and in the day
    /// block alike, and the refusal names both discs. An empty block is refused. The data as it stands passes.
    /// </summary>
    [Fact]
    public void SF25_ANightBlockIsValidatedLikeTheDayBlock()
    {
        var content = Game;
        Assert.Empty(ContentDataValidator.Validate(Game.Root));
        var infield = content.Rules.Infield;
        var mul = content.Rules.Hazards.Of(HazardType.FireBreath).NightRadiusMul;
        Assert.Equal(1.6, mul);

        // A breath of radius 4 whose own disc clears the first-second lane by 1 ft, on the infield side of its
        // midpoint; at night its disc is 6.4 ft and crosses by 1.4.
        const double r = 4;
        var half = ParkDiamond.PathWidth * 0.5;
        var off = half + r + 1;
        var (mx, mz) = (infield.CornerFt / 2, (infield.CornerFt + infield.SecondFt) / 2);
        var (x, z) = (mx - off / Math.Sqrt(2), mz - off / Math.Sqrt(2));
        Assert.Equal(1, HazardPlacement.ClearanceFt(x, z, r, infield), 9);
        Assert.Equal(1 - r * (mul - 1), HazardPlacement.ClearanceFt(x, z, ParkHazards.NightDiscFt(r, content.Rules.Hazards.Of(HazardType.FireBreath)), infield), 9);

        using var fixture = new NightFixture();
        fixture.Park(ParkId.Funfair, json =>
        {
            var night = json["night"]!["hazards"]!.AsArray();
            night.Add(new JsonObject { ["type"] = "sprinkler", ["x"] = 0, ["z"] = 200, ["radius"] = 5 });           // [3]
            night.Add(new JsonObject { ["type"] = "chomper", ["x"] = 0, ["z"] = 200, ["radius"] = 0 });             // [4]
            night.Add(new JsonObject { ["type"] = "climb_wall", ["x"] = 0, ["z"] = 250, ["radius"] = 5 });          // [5]
            night.Add(new JsonObject { ["type"] = "tree", ["x"] = 60, ["z"] = 250, ["radius"] = 5 });               // [6]
            night.Add(new JsonObject { ["type"] = "freeze_volume", ["x"] = infield.CornerFt, ["z"] = infield.CornerFt, ["radius"] = 2 }); // [7]
            night.Add(new JsonObject { ["type"] = "fire_breath", ["x"] = x, ["z"] = z, ["radius"] = r });           // [8]
        });
        fixture.Park(ParkId.Ember, json =>
            json["hazards"]!.AsArray().Add(new JsonObject { ["type"] = "fire_breath", ["x"] = x, ["z"] = z, ["radius"] = r }));
        fixture.Park(ParkId.Crystal, json => json["night"] = new JsonObject { ["hazards"] = new JsonArray() });
        var ember = content.Parks[ParkId.Ember].Hazards.Count;

        var errors = ContentDataValidator.Validate(fixture.Root());
        string Refusal(string where) => Assert.Single(errors, e => e.Contains(where, StringComparison.Ordinal));
        Assert.Contains("type must be one of", Refusal("park 'funfair-park' night.hazards[3] "), StringComparison.Ordinal);
        Assert.Contains("radius must be greater than 0", Refusal("park 'funfair-park' night.hazards[4] "), StringComparison.Ordinal);
        Assert.Contains("type 'climb_wall' is a wallTrait, which the hazards switch keeps", Refusal("park 'funfair-park' night.hazards[5] "), StringComparison.Ordinal);
        Assert.Contains("crosses first base's pad by", Refusal("park 'funfair-park' night.hazards[7] "), StringComparison.Ordinal);
        var nightBreath = Refusal("park 'funfair-park' night.hazards[8] ");
        Assert.Contains("radius 4 (6.4 at night, hazards.fireBreath.nightRadiusMul 1.6) crosses the first-second lane by 1.40 ft",
            nightBreath, StringComparison.Ordinal);
        var dayBreath = Refusal($"park 'ember-keep' hazard[{ember}] ");
        Assert.Contains("radius 4 (6.4 at night, hazards.fireBreath.nightRadiusMul 1.6) crosses the first-second lane by 1.40 ft",
            dayBreath, StringComparison.Ordinal);
        Assert.Contains("park 'crystal-rink' night names nothing", Refusal("park 'crystal-rink' night "), StringComparison.Ordinal);
        // The three mouths already there are legal and stay silent.
        Assert.DoesNotContain(errors, e => e.Contains("night.hazards[0]", StringComparison.Ordinal)
            || e.Contains("night.hazards[1]", StringComparison.Ordinal) || e.Contains("night.hazards[2]", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------
    // Parity: Funfair and Ember play the games they played
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The log of a whole game, as <c>cli match</c> prints it: the header, then one line per
    /// <see cref="Match.AutoPlay"/>, then the final score. The golden hashes below are this text from the
    /// CLI of the build before F4-d (<c>12345a76</c>), captains Rio at home and Ashlord away, three innings.
    /// </summary>
    static string Log(Match match)
    {
        var lines = new List<string>
        {
            $"{match.Away.Name} at {match.Home.Name}  {match.Park.Name}  seed {match.Seed}  {(match.Night ? "night" : "day")}  "
                + $"{match.Difficulty}  hazards {(match.Hazards ? "on" : "off")}"
        };
        var guard = 0;
        IReadOnlyList<BallRedirected> told = [];
        while (!match.Over && guard++ < 2000)
        {
            var half = $"{(match.Top ? "T" : "B")}{match.Inning}";
            var ev = match.AutoPlay();
            lines.Add($"{half,-3} {match.AwayScore}-{match.HomeScore}  {ev.Kind,-11}  {ev.Caption}");
            // The redirects the live ball went through (F4-c): a typed fact, so a mouth that moved or stopped taking the ball fails.
            // The list lives until the next live play, so a pitch with no ball in play would show the last one again.
            var now = match.LivePlay.RedirectsThisPlay.ToList();
            if (!now.SequenceEqual(told))
                foreach (var r in now)
                    lines.Add($"    redirect {r.Type} {r.Hazard} -> {r.Exit} at {r.T:0.000}");
            told = now;
        }
        Assert.True(match.Over);
        lines.Add($"Final  {match.Away.Name} {match.AwayScore}  {match.Home.Name} {match.HomeScore}");
        return string.Join("\n", lines);
    }

    static string Sha(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    /// <summary>
    /// The pinned night games: two at Funfair (seed 11 sends a fly through a chomper, so a mouth that moved, resized or
    /// stopped taking the ball fails) and two at Ember. <c>fixtures/night-games.json</c> holds each game's final line and
    /// the SHA-256 of its <see cref="Log"/>. Any change to play changes them: re-record with
    /// <c>tools/regenerate.sh night-games</c> from the build that changes play, and say in the PR why they moved. Keep a
    /// Funfair seed with a redirect in it; if the chomp moves to another seed, change the seed here.
    /// </summary>
    static readonly (string Park, int Seed)[] NightGames =
    [
        (ParkId.Funfair, 1), (ParkId.Funfair, 11), (ParkId.Ember, 1), (ParkId.Ember, 2)
    ];

    const string NightGamesFixture = "night-games.json";
    const string WriteNightGames = "GRAND_SLUGGERS_WRITE_NIGHT_GAMES";

    static string NightLog(string park, int seed) =>
        Log(Match.Exhibition(Game, "rio", "ashlord", innings: 3, seed: seed, parkId: park, night: true));

    static IReadOnlyList<(string Park, int Seed, string Final, string Sha)> Pinned()
    {
        var json = JsonNode.Parse(File.ReadAllText(Fixtures.PathOf(NightGamesFixture)))!;
        var rows = json["games"]!.AsArray().Select(g => (
            g!["park"]!.GetValue<string>(), g["seed"]!.GetValue<int>(),
            g["final"]!.GetValue<string>(), g["sha256"]!.GetValue<string>())).ToList();
        Assert.Equal(NightGames, rows.Select(r => (r.Item1, r.Item2)));
        return rows;
    }

    [WriterFact(WriteNightGames)]
    public void RegenerateNightGames()
    {
        var games = new JsonArray();
        foreach (var (park, seed) in NightGames)
        {
            var log = NightLog(park, seed);
            games.Add(new JsonObject
            {
                ["park"] = park,
                ["seed"] = seed,
                ["final"] = log[(log.LastIndexOf('\n') + 1)..],
                ["sha256"] = Sha(log)
            });
        }
        var document = new JsonObject
        {
            ["what"] = "SF-25: night games at Funfair and Ember, pinned by final line and the SHA-256 of the play log (NightBlockTests.Log).",
            ["regenerate"] = "tools/regenerate.sh night-games",
            ["games"] = games
        };
        File.WriteAllText(Fixtures.PathOf(NightGamesFixture),
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    /// <summary>
    /// <c>SF-25</c> parity: Funfair's night games are bit-identical to the games they
    /// were before the chompers moved into the night block, and Ember's are unchanged (its breath's reach is
    /// its type's own night number, left where it was). The chomp is still there to see.
    /// </summary>
    // Historical whole-game score/checksum calibration. Mechanics changes invalidate it;
    // preserve its values until a requested balance pass, alongside the other evidence seals.
    // The structural night-block contracts above remain in the breakage suite.
    [Fact]
    [Trait("Kind", "Balance")]
    public void SF25_FunfairAndEmberNightGamesAreTheGamesTheyWere()
    {
        var chomped = false;
        foreach (var (park, seed, final, sha) in Pinned())
        {
            var log = NightLog(park, seed);
            Assert.Equal(final, log[(log.LastIndexOf('\n') + 1)..]);
            Assert.True(sha == Sha(log), $"{park} night seed {seed} is not the game it was (tools/regenerate.sh night-games):\n{log}");
            chomped |= log.Contains("    redirect ", StringComparison.Ordinal);
        }
        Assert.True(chomped, "the premise: one pinned Funfair night has a redirect in it (the chompers' own are BallRedirectTests')");
    }

    /// <summary>
    /// Ember's breath still reaches farther at night through the park a match plays: a day instance, so it is
    /// in the played park by day and at night, and its disc is <see cref="ParkHazards.NightDiscFt"/> at night.
    /// </summary>
    [Fact]
    public void SF25_EmbersBreathKeepsItsTypesNightReach()
    {
        var content = Game;
        var row = content.Rules.Hazards.Of(HazardType.FireBreath);
        var byDay = Played(content, ParkId.Ember, night: false);
        var atNight = Played(content, ParkId.Ember, night: true);
        Assert.Same(content.Parks[ParkId.Ember], atNight);
        var breath = Assert.Single(atNight.Hazards, h => h.Type == HazardType.FireBreath);
        var reach = ParkHazards.NightDiscFt(breath.Radius, row);
        Assert.Equal(breath.Radius * 1.6, reach);
        var past = breath.Z + (breath.Radius + reach) / 2;
        Assert.False(ParkHazards.InSlow(byDay, breath.X, past, content.Rules, night: false));
        Assert.True(ParkHazards.InSlow(atNight, breath.X, past, content.Rules, night: true));
    }

    // ---------------------------------------------------------------------------------
    // Crystal: the day window (FD-11-R2)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-25</c> (FD-11-R2): Crystal's night contact window is dropped, so a match at the rink
    /// at night judges a swing in the window it judges it in by day, which is Harbor's; no park carries a
    /// window any more. Through the match's own window read, with and without the star pitch.
    /// </summary>
    [Fact]
    public void SF25_CrystalsNightAtBatUsesTheDayWindow()
    {
        Assert.Null(typeof(Park).GetProperty("NightContactWindowMul"));
        Assert.Null(typeof(ParkHazards).GetMethod("ContactWindowMul"));
        foreach (var pitch in new[] { new PitchCommand("fastball", 0, false), new PitchCommand("fastball", 0, true) })
        {
            var harbor = Match.Exhibition(Game, "rio", "ashlord", innings: 3, seed: 1, parkId: ParkId.Harbor).SwingWindowFrames(pitch);
            var day = Match.Exhibition(Game, "rio", "ashlord", innings: 3, seed: 1, parkId: ParkId.Crystal).SwingWindowFrames(pitch);
            var night = Match.Exhibition(Game, "rio", "ashlord", innings: 3, seed: 1, parkId: ParkId.Crystal, night: true).SwingWindowFrames(pitch);
            Assert.Equal(day, night);
            Assert.Equal(harbor, night);
        }
    }

    // ---------------------------------------------------------------------------------
    // Fixture
    // ---------------------------------------------------------------------------------

    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// A throwaway copy of the data root that a row may break on purpose.
    /// </summary>
    sealed class NightFixture : IDisposable
    {
        readonly string _shipped;

        public NightFixture()
        {
            var temp = Path.Combine(Path.GetTempPath(), "grand-sluggers-night-" + Guid.NewGuid().ToString("N"));
            _shipped = Path.Combine(temp, "data");
            Copy(ShippedPath, _shipped);
        }

        public DataRoot Root() => new(_shipped);

        public string ParkFile(string id) => Path.Combine(_shipped, "parks", id + ".json");

        public void Park(string id, Action<JsonObject> change)
        {
            var path = ParkFile(id);
            var json = JsonNode.Parse(File.ReadAllText(path), null, JsonComments)!.AsObject();
            change(json);
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        static void Copy(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }

        public void Dispose()
        {
            var temp = Path.GetFullPath(Path.Combine(_shipped, ".."));
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }
}
