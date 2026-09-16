using System.Text.Json;
using System.Text.Json.Nodes;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The trial overlay (#716): a root that carries only its own diff and resolves everything else
/// against the shipped data. Every 3c slice is authored into one, so the mechanism has to be worth
/// trusting before a hundred anchors ride on it — a trial that quietly drifts from the control on a
/// file it never meant to own would make the comparison worthless without anybody noticing.
/// </summary>
public sealed class TrialOverlayTests
{
    static readonly JsonDocumentOptions JsonComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static string Shipped => ContentCatalog.Load().Root.Shipped;

    /// <summary>
    /// The exit criterion: one file in, one file overridden. Everything the overlay does not carry
    /// resolves to the shipped copy — the same path, so the same bytes, for ever.
    /// </summary>
    [Fact]
    public void AFileTheOverlayDoesNotCarryResolvesToTheShippedOne()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json => json["baselineFt"] = 80);

        var root = trial.Root;

        Assert.Equal(["rules/infield.json"], root.Overrides);
        Assert.Equal(Path.Combine(trial.Overlay, "rules", "infield.json"), root.Resolve("rules", "infield.json"));

        foreach (var unnamed in new[]
                 {
                     new[] { "rules", "flight.json" },
                     new[] { "rules", "running.json" },
                     new[] { "characters", "rio.json" },
                     new[] { "parks", "harbor-diamond.json" },
                     new[] { "feel", "table.json" },
                     new[] { "abilities", "star-skills.json" }
                 })
        {
            var shipped = Path.Combine(Shipped, Path.Combine(unnamed));
            Assert.False(root.Overridden(unnamed));
            Assert.Equal(shipped, root.Resolve(unnamed));
            Assert.Equal(File.ReadAllText(shipped), File.ReadAllText(root.Resolve(unnamed)));
        }
    }

    /// <summary>
    /// The same thing one layer up, where it matters: a catalog loaded through the overlay plays the
    /// trial's infield and the shipped everything-else, table for table.
    /// </summary>
    [Fact]
    public void TheCatalogTakesTheTrialsTableAndTheShippedRest()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json =>
        {
            json["baselineFt"] = 80;
            json["cornerFt"] = 56.57;
            json["secondFt"] = 113.14;
        });

        var control = ContentCatalog.Load();
        var candidate = ContentCatalog.Load(trial.Root);

        Assert.Equal(80, candidate.Rules.Infield.BaselineFt);
        Assert.Equal(90, control.Rules.Infield.BaselineFt);

        Assert.Equal(control.Rules.Flight.Drag, candidate.Rules.Flight.Drag);
        Assert.Equal(control.Rules.Running.Bags.TagReachFt, candidate.Rules.Running.Bags.TagReachFt);
        Assert.Equal(control.Rules.Fielding.Throw.BaseFtPerSec, candidate.Rules.Fielding.Throw.BaseFtPerSec);
        Assert.Equal(control.Characters.Count, candidate.Characters.Count);
        Assert.Equal(control.Must("rio").Stats.Run, candidate.Must("rio").Stats.Run);
        Assert.Equal(control.Parks["harbor-diamond"].CenterFenceFt, candidate.Parks["harbor-diamond"].CenterFenceFt);
    }

    /// <summary>Resolution is per file, not per table: a trial may override a character as readily as a rule.</summary>
    [Fact]
    public void AnyDataFileCanBeTheOneTheTrialCarries()
    {
        using var trial = new Trial();
        trial.Override("characters/rio.json", json => json["run"] = 3);

        var candidate = ContentCatalog.Load(trial.Root);

        Assert.Equal(3, candidate.Must("rio").Stats.Run);
        Assert.Equal(ContentCatalog.Load().Must("vale").Stats.Run, candidate.Must("vale").Stats.Run);
    }

    /// <summary>
    /// A listing is resolved file by file, and its order comes from the shipped root: a trial
    /// substitutes a park, so a run reads the same six rows in the same order either way.
    /// </summary>
    [Fact]
    public void ADirectoryListingSubstitutesPerFileAndKeepsTheShippedOrder()
    {
        using var trial = new Trial();
        trial.Override("parks/harbor-diamond.json", json => json["centerFenceFt"] = 300);

        var shipped = new DataRoot(Shipped).Files("parks", "*.json");
        var withTrial = trial.Root.Files("parks", "*.json");

        Assert.Equal(shipped.Select(Path.GetFileName), withTrial.Select(Path.GetFileName));
        Assert.Single(withTrial, file => file.StartsWith(trial.Overlay, StringComparison.Ordinal));
        Assert.Equal(300, ContentCatalog.Load(trial.Root).Parks["harbor-diamond"].CenterFenceFt);
    }

    /// <summary>
    /// A file the shipped root does not have is a typo, and the same reasoning as #711's refusal to
    /// fall back silently applies: a trial that quietly runs the control is worse than one that fails.
    /// </summary>
    [Theory]
    [InlineData("rules/feilding.json")]
    [InlineData("rules/flight.jsonn")]
    [InlineData("parks/seventh-park.json")]
    [InlineData("nonsense/anything.json")]
    public void AnOverlayThatNamesAFileTheShippedRootLacksIsRefused(string relative)
    {
        using var trial = new Trial();
        trial.Write(relative, "{}");

        var thrown = Assert.Throws<FileNotFoundException>(() => trial.Root);
        Assert.Contains(relative, thrown.Message, StringComparison.Ordinal);
        Assert.Contains("never adds to it", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>The refusal names every stray file at once, so a mistyped trial is fixed in one pass.</summary>
    [Fact]
    public void TheRefusalNamesEveryStrayFile()
    {
        using var trial = new Trial();
        trial.Write("rules/feilding.json", "{}");
        trial.Write("parks/seventh-park.json", "{}");
        trial.Override("rules/flight.json");

        var thrown = Assert.Throws<FileNotFoundException>(() => trial.Root);
        Assert.Contains("parks/seventh-park.json", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("rules/feilding.json", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A trial that cannot explain itself is not evidence either, so its README is the one exception.</summary>
    [Fact]
    public void TheReadmeIsTheOnlyFileThatOverridesNothing()
    {
        using var trial = new Trial();
        trial.Write(DataRoot.ReadmeFile, "# what this trial changes");
        trial.Override("rules/flight.json");

        Assert.Equal(["rules/flight.json"], trial.Root.Overrides);
    }

    /// <summary>A stray <c>.DS_Store</c> is not a trial anchor and must not fail a run.</summary>
    [Fact]
    public void ADotFileIsNotData()
    {
        using var trial = new Trial();
        trial.Write(".DS_Store", "junk");
        trial.Write("rules/.DS_Store", "junk");

        Assert.Empty(trial.Root.Overrides);
    }

    /// <summary>With nothing named, a root is the shipped root and nothing else — no overlay, no diff.</summary>
    [Fact]
    public void AnUnnamedOverlayLeavesTheRootExactlyAsItWas()
    {
        var root = new DataRoot(Shipped);

        Assert.Null(root.Overlay);
        Assert.Empty(root.Overrides);
        Assert.False(root.Overridden("rules", "infield.json"));
        Assert.Equal(Path.Combine(Shipped, "rules", "infield.json"), root.Resolve("rules", "infield.json"));
    }

    /// <summary>
    /// A checked-in trial is inert until a run asks for it. An overlay sitting in the tree changes
    /// nothing about a process that does not name it, which is why one can be committed at all.
    /// </summary>
    [Fact]
    public void ACheckedInTrialDoesNotTouchARunThatDidNotAskForIt()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json => json["baselineFt"] = 80);

        Assert.Null(new DataRoot(Shipped).Overlay);
        Assert.Equal(90, ContentCatalog.Load(new DataRoot(Shipped)).Rules.Infield.BaselineFt);
        Assert.Equal(90, Diamond.Baseline);
    }

    /// <summary>
    /// The trial this series is authored into, checked as it stands. Every slice from #717 on adds
    /// files here, and this is the test that refuses one that overrides nothing in <c>data/</c>.
    /// </summary>
    [Fact]
    public void TheCheckedInTrialOnlyEverOverridesShippedFiles()
    {
        var overlay = Path.GetFullPath(Path.Combine(Shipped, "..", "trials", "c80"));
        Assert.True(Directory.Exists(overlay), overlay + " is the overlay every 3c slice is authored into");

        var root = new DataRoot(Shipped, overlay);

        foreach (var file in root.Overrides)
            Assert.True(File.Exists(Path.Combine(Shipped, file)), file);
        Assert.Empty(ContentDataValidator.Validate(root));
        Assert.Empty(RulesTable.Validate(root));
    }

    /// <summary>A relative name is resolved beside the data root, so it means the same from any working directory.</summary>
    [Fact]
    public void ARelativeNameIsResolvedBesideTheDataRoot()
    {
        var expected = Path.GetFullPath(Path.Combine(Shipped, "..", "trials", "c80"));
        Assert.Equal(expected, DataRoot.NamedOverlay("trials/c80", Shipped));
        Assert.Equal(expected, DataRoot.NamedOverlay(expected, Shipped));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoNamedOverlayIsNoOverlay(string? value)
    {
        Assert.Null(DataRoot.NamedOverlay(value, Shipped));
    }

    /// <summary>A trial named at a folder that is not there stops the run rather than running the control.</summary>
    [Fact]
    public void AnOverlayThatIsNotThereIsRefusedRatherThanIgnored()
    {
        var thrown = Assert.Throws<DirectoryNotFoundException>(
            () => DataRoot.NamedOverlay("trials/c80-that-was-never-authored", Shipped));
        Assert.Contains(DataRoot.OverlayVariable, thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// What <c>cli</c> prints. A trace whose provenance has to be reconstructed from memory is not
    /// evidence, so the line names the root, the overlay, and every file the trial overrides.
    /// </summary>
    [Fact]
    public void ProvenanceNamesTheRootTheOverlayAndTheFiles()
    {
        Assert.Equal($"data root {Shipped}  overlay none", new DataRoot(Shipped).Provenance);

        using var empty = new Trial();
        Assert.Equal($"data root {Shipped}  overlay {empty.Overlay} (overrides nothing)", empty.Root.Provenance);

        using var trial = new Trial();
        trial.Override("rules/infield.json");
        Assert.Equal(
            $"data root {Shipped}  overlay {trial.Overlay} (1 file: rules/infield.json)",
            trial.Root.Provenance);

        trial.Override("rules/flight.json");
        Assert.Equal(
            $"data root {Shipped}  overlay {trial.Overlay} (2 files: rules/flight.json, rules/infield.json)",
            trial.Root.Provenance);
    }

    /// <summary>
    /// Every gameplay file the sim reads has to go through <see cref="DataRoot"/>, or a trial would
    /// give a correct table and one loader still quietly reading the control — the same failure
    /// #711 found in the bags of the play view. A path built onto a bare root is that bug waiting.
    /// </summary>
    [Theory]
    [InlineData("string dataRoot")]
    [InlineData("Path.Combine(dataRoot")]
    public void NoLoaderBuildsAPathOntoABareDataRoot(string pattern)
    {
        var sim = Path.GetFullPath(Path.Combine(Shipped, "..", "src", "GrandSluggers.Sim"));
        var offenders = Directory
            .EnumerateFiles(sim, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "DataRoot.cs")
            .Where(f => File.ReadLines(f).Any(line => line.Contains(pattern, StringComparison.Ordinal)))
            .Select(f => Path.GetRelativePath(sim, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"'{pattern}' must resolve through DataRoot so a trial overlay reaches it: " + string.Join(", ", offenders));
    }

    /// <summary>An overlay built beside the shipped root, free to carry whatever a test is about.</summary>
    sealed class Trial : IDisposable
    {
        public Trial()
        {
            Overlay = Path.Combine(Path.GetTempPath(), "grand-sluggers-trial-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Overlay);
        }

        public string Overlay { get; }

        /// <summary>The shipped root with this overlay laid over it. Built fresh: the tree is the declaration.</summary>
        public DataRoot Root => new(Shipped, Overlay);

        /// <summary>Carry a shipped file into the trial, optionally with a number changed.</summary>
        public void Override(string relative, Action<JsonObject>? change = null)
        {
            var source = Path.Combine(Shipped, relative);
            var json = JsonNode.Parse(File.ReadAllText(source), null, JsonComments)!.AsObject();
            change?.Invoke(json);
            Write(relative, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>Put a file in the trial whether or not the shipped root has one.</summary>
        public void Write(string relative, string text)
        {
            var path = Path.Combine(Overlay, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public void Dispose() => Directory.Delete(Overlay, recursive: true);
    }
}
