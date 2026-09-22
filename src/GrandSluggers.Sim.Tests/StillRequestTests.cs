using System.Text.Json;
using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StillRequestTests
{
    [Fact]
    public void ExternalRequestMustExistAndPassTheSameParserBeforeStaging()
    {
        var root = Path.Combine(Path.GetTempPath(), "gs-still-request-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "fenn-plate.json");
            const string json = """{"shots":["plate","pitch","char-pose"],"home":"fenn","away":"rio"}""";
            File.WriteAllText(path, json);

            Assert.Equal(json, StillRequest.ReadValidatedJsonFile(path));

            File.WriteAllText(path, """{"shots":["not-a-shot"]}""");
            Assert.Throws<InvalidDataException>(() => StillRequest.ReadValidatedJsonFile(path));
            Assert.Throws<FileNotFoundException>(() =>
                StillRequest.ReadValidatedJsonFile(Path.Combine(root, "missing.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DefaultRequestIsTitlePlateMoundHudOffRio()
    {
        var req = StillRequest.Parse("{}");
        Assert.Equal(new[] { "title", "select", "lineup", "plate", "pitch", "mound", "diamond-grounder", "smash" }, req.ResolvedShots());
        Assert.Contains("select", StillRequest.AllowedShots);
        Assert.Contains("field", StillRequest.AllowedShots);
        Assert.Contains("lineup", StillRequest.AllowedShots);
        Assert.Equal("rio", req.ResolvedHome());
        Assert.Equal("ashlord", req.ResolvedAway());
        Assert.True(req.HudOff);
        Assert.Equal(1, req.Charge01);
        Assert.False(req.FeelDebug);
        Assert.Equal(1920, req.ResolvedWidth());
        Assert.Equal(1080, req.ResolvedHeight());
        Assert.Equal("plate", AtBatShots.Plate);
        Assert.Equal("mound", AtBatShots.Mound);
        Assert.Contains("plate", StillRequest.AllowedShots);
        Assert.Contains("pitch", StillRequest.AllowedShots);
        Assert.Contains("mound", StillRequest.AllowedShots);
        Assert.Contains("diamond-grounder", StillRequest.AllowedShots);
        Assert.Contains("diamond-line", StillRequest.AllowedShots);
        Assert.Equal(new[] { "diamond-grounder" }, StillRequest.Parse("""{"shots":["scoop"]}""").ResolvedShots());
    }

    [Fact]
    public void ParseHonorsShotsHomeAwayAndRejectsUnknown()
    {
        var req = StillRequest.Parse("""
            {"shots":["plate","mound"],"home":"vale","away":"konga","hudOff":false,"width":1280,"height":720}
            """);
        Assert.Equal(new[] { "plate", "mound" }, req.ResolvedShots());
        Assert.Equal("vale", req.ResolvedHome());
        Assert.Equal("konga", req.ResolvedAway());
        Assert.False(req.HudOff);
        Assert.Equal(1280, req.ResolvedWidth());
        Assert.Equal(720, req.ResolvedHeight());
        Assert.Equal("/tmp/gs/plate.png", StillRequest.PngPath("/tmp/gs", "plate"));
        var ex = Assert.Throws<InvalidDataException>(() =>
            StillRequest.Parse("""{"shots":["catcher-spine"]}"""));
        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CharacterShotsWriteNamedPngs()
    {
        Assert.Contains("char-rest", StillRequest.AllowedShots);
        Assert.Contains("char-pose", StillRequest.AllowedShots);
        var req = StillRequest.Parse("""{"shots":["char-rest","char-pose"],"home":"fenn"}""");
        Assert.Equal(new[] { "char-rest", "char-pose" }, req.ResolvedShots());
        Assert.Equal("fenn", req.ResolvedHome());
        Assert.Equal("/tmp/gs/char-fenn-rest.png", StillRequest.PngPath("/tmp/gs", "char-rest", req.ResolvedHome()));
        Assert.Equal("/tmp/gs/char-fenn-pose.png", StillRequest.PngPath("/tmp/gs", "char-pose", "fenn"));
        Assert.True(StillRequest.IsCharShot("char-rest"));
        Assert.False(StillRequest.IsCharShot("plate"));
    }

    [Fact]
    public void SwingMatrixIsOptInAndWritesLabeledRosterPaths()
    {
        var req = StillRequest.Parse("""{"shots":["swing-matrix"]}""");
        Assert.Equal(new[] { "swing-matrix" }, req.ResolvedShots());
        Assert.Equal(SwingPresentation.SharedCaptains, req.ResolvedSwingCaptains());
        Assert.DoesNotContain("swing-matrix", StillRequest.DefaultShots);
        Assert.True(StillRequest.IsSwingMatrixShot("swing-matrix"));
        Assert.Equal("/tmp/gs/swing-ashlord-max-contact.png",
            StillRequest.SwingPngPath("/tmp/gs", "ashlord", "max", "contact"));
    }

    [Fact]
    public void SwingMatrixCanSelectFennWithoutAddingItToSharedRigMetrics()
    {
        var req = StillRequest.Parse("""
            {"shots":["swing-matrix"],"swingCaptains":["FENN","rio"]}
            """);

        Assert.Equal(new[] { "fenn", "rio" }, req.ResolvedSwingCaptains());
        Assert.Contains("fenn", SwingPresentation.SharedCaptains);
        var unknown = Assert.Throws<InvalidDataException>(() => StillRequest.Parse("""
            {"shots":["swing-matrix"],"swingCaptains":["not-a-player"]}
            """));
        Assert.Contains("not playable", unknown.Message);
        var duplicate = Assert.Throws<InvalidDataException>(() => StillRequest.Parse("""
            {"shots":["swing-matrix"],"swingCaptains":["fenn","FENN"]}
            """));
        Assert.Contains("duplicated", duplicate.Message);
    }

    [Fact]
    public void AwayWillNotMatchHome()
    {
        var req = StillRequest.Parse("""{"home":"rio","away":"rio"}""");
        Assert.Equal("rio", req.ResolvedHome());
        Assert.NotEqual("rio", req.ResolvedAway());
    }

    /// <summary>F7-a (FD-17, FR-04): a still names a park; absent is the one default park.</summary>
    [Fact]
    public void ParkAndNightAreAbsentByDefaultAndResolveToTheDefaultParkInDaylight()
    {
        var req = StillRequest.Parse("{}");
        Assert.Null(req.Park);
        Assert.False(req.Night);
        Assert.Equal(ExhibitionPick.DefaultPark, req.ResolvedPark());
        Assert.Equal(ExhibitionPick.DefaultPark, StillRequest.Parse("""{"park":"   "}""").ResolvedPark());
        Assert.Equal(ExhibitionPick.DefaultPark, StillRequest.Parse("""{"park":null}""").ResolvedPark());
    }

    [Fact]
    public void ParkResolvesLikeHomeAndAnUnknownParkIsRefusedByName()
    {
        var req = StillRequest.Parse("""{"shots":["plate"],"park":"  Crystal-Rink  ","night":true}""");
        Assert.Equal("crystal-rink", req.ResolvedPark());
        Assert.True(req.Night);
        Assert.Contains("crystal-rink", ExhibitionPick.Parks);

        var unknown = Assert.Throws<InvalidDataException>(() =>
            StillRequest.Parse("""{"shots":["plate"],"park":"grand-canyon"}"""));
        Assert.Contains("not allowed", unknown.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("grand-canyon", unknown.Message);

        var root = Path.Combine(Path.GetTempPath(), "gs-still-park-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "park.json");
            File.WriteAllText(path, """{"shots":["plate"],"park":"grand-canyon"}""");
            Assert.Throws<InvalidDataException>(() => StillRequest.ReadValidatedJsonFile(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AParkAndNightRequestRoundTripsThroughJson()
    {
        const string json = """{"shots":["plate","mound"],"home":"vale","park":"ember-keep","night":true}""";
        var req = StillRequest.Parse(json);
        var written = JsonSerializer.Serialize(req);
        var back = StillRequest.Parse(written);

        Assert.Equal("ember-keep", back.ResolvedPark());
        Assert.True(back.Night);
        Assert.Equal(req.ResolvedShots(), back.ResolvedShots());
        Assert.Equal("vale", back.ResolvedHome());
        Assert.Equal(req.ResolvedAway(), back.ResolvedAway());
        Assert.Equal(req.ResolvedWidth(), back.ResolvedWidth());
        Assert.Equal(req.ResolvedHeight(), back.ResolvedHeight());
    }

    /// <summary>
    /// The default park in daylight keeps today's names, so the existing stills
    /// and the character gate do not move. Every other park, and night, is named.
    /// </summary>
    [Fact]
    public void PngNamesMoveOnlyWhenTheStillIsNotTheDefaultParkInDaylight()
    {
        Assert.Equal("/tmp/gs/plate.png", StillRequest.PngPath("/tmp/gs", "plate"));
        Assert.Equal("/tmp/gs/plate.png", StillRequest.PngPath("/tmp/gs", "plate", "rio", ExhibitionPick.DefaultPark));
        Assert.Equal("/tmp/gs/plate.png", StillRequest.PngPath("/tmp/gs", "plate", "rio", ""));
        Assert.Equal("/tmp/gs/char-fenn-rest.png",
            StillRequest.PngPath("/tmp/gs", "char-rest", "fenn", ExhibitionPick.DefaultPark));

        Assert.Equal("/tmp/gs/plate-crystal-rink.png",
            StillRequest.PngPath("/tmp/gs", "plate", "rio", "crystal-rink"));
        Assert.Equal("/tmp/gs/plate-crystal-rink-night.png",
            StillRequest.PngPath("/tmp/gs", "plate", "rio", "  CRYSTAL-RINK ", night: true));
        Assert.Equal("/tmp/gs/diamond-grounder-ember-keep.png",
            StillRequest.PngPath("/tmp/gs", "diamond-grounder", "rio", "ember-keep"));
        Assert.Equal("/tmp/gs/char-fenn-pose-ember-keep.png",
            StillRequest.PngPath("/tmp/gs", "char-pose", "fenn", "ember-keep"));
        // Night at the default park is a picture today's names cannot tell apart,
        // so it names itself too. No name that exists today moves.
        Assert.Equal("/tmp/gs/plate-night.png",
            StillRequest.PngPath("/tmp/gs", "plate", "rio", ExhibitionPick.DefaultPark, night: true));
        Assert.Equal("", StillRequest.ParkSuffix(null, false));
        Assert.Equal("-night", StillRequest.ParkSuffix(ExhibitionPick.DefaultPark, true));
    }

    /// <summary>
    /// With no flag the gate must write today's request byte for byte; the two
    /// flags are the only way that line grows (F7-a).
    /// </summary>
    [Fact]
    public void StillGateScriptWritesTodaysRequestUntilAFlagNamesAPark()
    {
        var repo = Path.GetFullPath(Path.Combine(ContentCatalog.Load().Root.Shipped, ".."));
        var script = File.ReadAllText(Path.Combine(repo, "tools", "still-gate.sh"));
        const string today = """{"shots":["title","select","lineup","plate","pitch","mound","diamond-grounder","smash"],"home":"rio","away":"ashlord","hudOff":true,"charge01":1,"width":1920,"height":1080}""";

        Assert.Contains("request='" + today + "'", script);
        Assert.Contains("printf '%s\\n' \"$request\" > \"$temp/gs-still-request.json\"", script);
        Assert.Contains("--park", script);
        Assert.Contains("--night", script);
        Assert.Contains("\\\"park\\\":\\\"$park\\\"", script);
        Assert.Contains("\\\"night\\\":true", script);

        var req = StillRequest.Parse(today);
        Assert.Equal(StillRequest.DefaultShots, req.ResolvedShots());
        Assert.Equal(ExhibitionPick.DefaultPark, req.ResolvedPark());
        Assert.False(req.Night);
        Assert.Equal("rio", req.ResolvedHome());
        Assert.Equal("ashlord", req.ResolvedAway());
        Assert.Equal(1920, req.ResolvedWidth());
        Assert.Equal(1080, req.ResolvedHeight());
        Assert.True(req.HudOff);
        Assert.Equal(1, req.Charge01);
        // What the flags compose, read back through the parser.
        var flagged = StillRequest.Parse(
            today[..^1] + ",\"park\":\"crystal-rink\",\"night\":true}");
        Assert.Equal("crystal-rink", flagged.ResolvedPark());
        Assert.True(flagged.Night);
        Assert.Equal(req.ResolvedShots(), flagged.ResolvedShots());
    }
}
