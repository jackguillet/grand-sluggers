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

    static readonly ContentCatalog Control = global::GrandSluggers.Sim.Tests.Shipped.Content;
    static string Shipped => Control.Root.Shipped;

    /// <summary>
    /// The exit criterion: one file in, one file overridden. Everything the overlay does not carry
    /// resolves to the shipped copy — the same path, so the same bytes, for ever.
    /// </summary>
    [Fact]
    public void AFileTheOverlayDoesNotCarryResolvesToTheShippedOne()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json => json["baselineFt"] = 90);

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
    ///
    /// <para>
    /// The bags stay where they ship (#862). Moved under the shipped parks, they can put a hazard on
    /// a lane, and the placement rule refuses that on any root (FD-19, SF-23): a trial that moves the
    /// bags carries the hazards that stand near them. <c>HazardPlacementTests</c> holds that refusal.
    /// </para>
    /// </summary>
    [Fact]
    public void TheCatalogTakesTheTrialsTableAndTheShippedRest()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json => json["baselineFt"] = 90);

        var control = Control;
        var candidate = ContentCatalog.Load(trial.Root);

        Assert.Equal(90, candidate.Rules.Infield.BaselineFt);
        Assert.Equal(80, control.Rules.Infield.BaselineFt);

        Assert.Equal(control.Rules.Flight.Drag, candidate.Rules.Flight.Drag);
        Assert.Equal(control.Rules.Running.Bags.TagReachFt, candidate.Rules.Running.Bags.TagReachFt);
        Assert.Equal(control.Rules.Fielding.Throw.BaseFtPerSec, candidate.Rules.Fielding.Throw.BaseFtPerSec);
        Assert.Equal(control.Characters.Count, candidate.Characters.Count);
        Assert.Equal(control.Must("rio").Stats.Run, candidate.Must("rio").Stats.Run);
        Assert.Equal(control.Parks[ParkId.Harbor].CenterFenceFt, candidate.Parks[ParkId.Harbor].CenterFenceFt);
    }

    /// <summary>Resolution is per file, not per table: a trial may override a character as readily as a rule.</summary>
    [Fact]
    public void AnyDataFileCanBeTheOneTheTrialCarries()
    {
        using var trial = new Trial();
        trial.Override("characters/rio.json", json => json["run"] = 3);

        var candidate = ContentCatalog.Load(trial.Root);

        Assert.Equal(3, candidate.Must("rio").Stats.Run);
        Assert.Equal(Control.Must("vale").Stats.Run, candidate.Must("vale").Stats.Run);
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

        // Spelled out rather than computed from Files itself: an expectation built by the method
        // under test moves with it, and every park, character and rules listing reverses unnoticed.
        string[] expected =
        [
            "canopy-yard.json", "crystal-rink.json", "ember-keep.json",
            "funfair-park.json", "harbor-diamond.json", "rooftop-city.json"
        ];
        var withTrial = trial.Root.Files("parks", "*.json");

        Assert.Equal(expected, withTrial.Select(Path.GetFileName));
        Assert.Equal(expected, new DataRoot(Shipped).Files("parks", "*.json").Select(Path.GetFileName));
        Assert.Single(withTrial, file => file.StartsWith(trial.Overlay, StringComparison.Ordinal));
        Assert.Equal(300, ContentCatalog.Load(trial.Root).Parks[ParkId.Harbor].CenterFenceFt);
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
        trial.Override("rules/infield.json", json => json["baselineFt"] = 90);

        // An overlay has to be named to be read. Nothing looks beside the data root for one, so a
        // trial can sit in the tree without being a second set of defaults.
        Assert.Null(new DataRoot(Shipped).Overlay);
        Assert.Equal(Control.Rules.Infield.BaselineFt, ContentCatalog.Load(new DataRoot(Shipped)).Rules.Infield.BaselineFt);
        Assert.Equal(90, ContentCatalog.Load(trial.Root).Rules.Infield.BaselineFt);
    }

    /// <summary>
    /// Every trial checked in under <c>trials/</c>, as it stands. A folder there is inert until a run
    /// names it, so nothing else would notice one that overrides nothing in <c>data/</c>, carries a
    /// stray file, or no longer validates against the shipped root it rides on. Constructing each
    /// root is most of the assertion: a stray file, a mis-cased name or a half-written table throws.
    /// With no trial checked in this holds vacuously; the mechanism itself is held by the synthetic
    /// overlays in the rest of this class.
    /// </summary>
    [Fact]
    public void EveryCheckedInTrialOnlyOverridesShippedFiles()
    {
        var trials = Path.GetFullPath(Path.Combine(Shipped, "..", "trials"));
        if (!Directory.Exists(trials)) return;
        foreach (var overlay in Directory.GetDirectories(trials))
        {
            var root = new DataRoot(Shipped, overlay);

            Assert.True(File.Exists(Path.Combine(overlay, DataRoot.ReadmeFile)), overlay + ": a trial explains itself");
            Assert.DoesNotContain(DataRoot.ReadmeFile, root.Overrides);
            foreach (var file in root.Overrides)
                Assert.True(File.Exists(Path.Combine(Shipped, file)), file);
            Assert.Empty(ContentDataValidator.Validate(root));
            Assert.Empty(RulesTable.Validate(root));
        }
    }

    /// <summary>A relative name is resolved beside the data root, so it means the same from any working directory.</summary>
    [Fact]
    public void ARelativeNameIsResolvedBesideTheDataRoot()
    {
        using var tree = new TrialBesideData("synthetic");
        Assert.Equal(tree.Overlay, DataRoot.NamedOverlay("trials/synthetic", tree.Data));
        Assert.Equal(tree.Overlay, DataRoot.NamedOverlay(tree.Overlay, tree.Data));
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
            () => DataRoot.NamedOverlay("trials/never-authored", Shipped));
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
    /// A cheap shape check on top of <see cref="EveryFileTheCatalogReadsComesFromTheOverlay"/>,
    /// which is the gate that actually holds the invariant. This one only catches the obvious
    /// spelling of the bug and cannot see a bare root passed under another name, so a green run
    /// here proves nothing on its own.
    /// </summary>
    [Theory]
    [InlineData("string dataRoot")]
    [InlineData("Path.Combine(dataRoot")]
    public void NoLoaderBuildsAPathOntoABareDataRoot(string pattern)
    {
        var src = Path.GetFullPath(Path.Combine(Shipped, "..", "src"));
        var offenders = Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "DataRoot.cs" && !f.Contains(".Tests"))
            .Where(f => File.ReadLines(f).Any(line => line.Contains(pattern, StringComparison.Ordinal)))
            .Select(f => Path.GetRelativePath(src, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"'{pattern}' must resolve through DataRoot so a trial overlay reaches it: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// The gate that holds "every gameplay file resolves through the overlay", by making it
    /// impossible to pass any other way: a shipped root whose every file has been replaced with
    /// text that is not data, and an overlay carrying the real thing. A loader that builds a path
    /// onto the bare root — under any name, through <see cref="DataRoot.Shipped"/>, through an
    /// implicit conversion — reads the gutted copy, and the catalog it returns cannot match the
    /// control. That is the #711 failure (a correct table and one system still on the old numbers)
    /// caught by behaviour instead of by grep.
    /// </summary>
    [Fact]
    public void EveryFileTheCatalogReadsComesFromTheOverlay()
    {
        using var probe = new GuttedRoot();

        var candidate = ContentCatalog.Load(probe.Root);

        Assert.Equal(Control.Characters.Count, candidate.Characters.Count);
        Assert.Equal(Control.Must("rio").Stats.Run, candidate.Must("rio").Stats.Run);
        Assert.Equal(Control.Must("rio").StarSwing, candidate.Must("rio").StarSwing);
        Assert.Equal(Control.Parks.Count, candidate.Parks.Count);
        Assert.Equal(Control.Parks[ParkId.Harbor].CenterFenceFt, candidate.Parks[ParkId.Harbor].CenterFenceFt);
        Assert.Equal(Control.Bats.Count, candidate.Bats.Count);
        Assert.Equal(Control.Gloves.Count, candidate.Gloves.Count);
        Assert.Equal(Control.Rules.Infield.BaselineFt, candidate.Rules.Infield.BaselineFt);
        Assert.Equal(Control.Rules.Flight.Drag, candidate.Rules.Flight.Drag);
        Assert.Equal(Control.Rules.Fielding.Throw.BaseFtPerSec, candidate.Rules.Fielding.Throw.BaseFtPerSec);
        Assert.Equal(Control.Feel.SmashFreeze, candidate.Feel.SmashFreeze);
        Assert.Equal(Control.Shots.Must("plate").Fov, candidate.Shots.Must("plate").Fov);
        Assert.Equal(Control.Art.Rig.Id, candidate.Art.Rig.Id);
        Assert.Equal(Control.Art.Clips.Count, candidate.Art.Clips.Count);
        Assert.Equal(Control.StarSkills.Swings.Count, candidate.StarSkills.Swings.Count);
        Assert.Equal(Control.Chemistry.Between(Control.Must("rio"), Control.Must("ashlord")),
            candidate.Chemistry.Between(candidate.Must("rio"), candidate.Must("ashlord")));

        // The agent catalogs and the wav drops read the same way, and neither goes through the catalog.
        Assert.Equal(DebugProtocol.Load(Control.Root).Entries.Count, DebugProtocol.Load(probe.Root).Entries.Count);
        Assert.Equal(DualStills.Load(Control.Root).Kinds.Count, DualStills.Load(probe.Root).Kinds.Count);
        Assert.Equal(DccStages.Load(Control.Root).Stages.Count, DccStages.Load(probe.Root).Stages.Count);
        Assert.Equal(AuthoredAudio.Ids(Control.Root), AuthoredAudio.Ids(probe.Root));
        Assert.True(AuthoredAudio.TryLoad(probe.Root, "bat-perfect", out var pcm, out _));
        Assert.NotEmpty(pcm);
    }

    /// <summary>
    /// A name that differs from the shipped one only in case is a stray file, not an override.
    /// <c>File.Exists</c> answers in the filesystem's case, so on a case-insensitive disk such a
    /// file was accepted, counted in the provenance line, and then never found again by the ordinal
    /// lookup — the run read the shipped table while the one diagnostic meant to catch that said
    /// the override had applied. The same overlay was refused outright on a case-sensitive disk, so
    /// a trial behaved one way on a laptop and another in CI.
    /// </summary>
    [Theory]
    [InlineData("rules/Infield.json", "rules/infield.json")]
    [InlineData("RULES/infield.json", "rules/infield.json")]
    [InlineData("parks/Harbor-Diamond.json", "parks/harbor-diamond.json")]
    public void ANameThatDiffersOnlyInCaseIsAStrayFile(string carried, string shipped)
    {
        using var trial = new Trial();
        trial.Write(carried, File.ReadAllText(Path.Combine(Shipped, shipped.Replace('/', Path.DirectorySeparatorChar))));

        var thrown = Assert.Throws<FileNotFoundException>(() => trial.Root);
        Assert.Contains(carried, thrown.Message, StringComparison.Ordinal);
        Assert.Contains($"the shipped root spells it {shipped}", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Whole files, never fields. A rules table that omits a field takes the C# default for it, so
    /// a partial copy would be half the trial's profile and half whatever the code says — with a
    /// provenance line claiming the whole table. This is the rule the README, the spec and the class
    /// doc all assert most loudly, so it is enforced rather than asked for.
    /// </summary>
    [Fact]
    public void APartialFileIsRefusedRatherThanCompletedFromTheCodeDefaults()
    {
        using var trial = new Trial();
        trial.Write("rules/infield.json", """{ "baselineFt": 80 }""");

        var thrown = Assert.Throws<InvalidDataException>(() => trial.Root);
        Assert.Contains("rules/infield.json", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("cornerFt", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("whole files", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A nested section must be whole too, or the fields below it fall back one level down.</summary>
    [Fact]
    public void APartialSectionIsRefusedToo()
    {
        using var trial = new Trial();
        trial.Override("rules/running.json", json => json["bags"]!.AsObject().Remove("tagReachFt"));

        var thrown = Assert.Throws<InvalidDataException>(() => trial.Root);
        Assert.Contains("bags.tagReachFt", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Arrays are the trial's own business: a park may carry a different number of hazards, and a
    /// roster a different number of role players, without that being a half-written file.
    /// </summary>
    [Fact]
    public void ATrialMayCarryADifferentNumberOfRows()
    {
        using var trial = new Trial();
        trial.Override("parks/harbor-diamond.json", json => json["hazards"] = new JsonArray());

        Assert.Equal(["parks/harbor-diamond.json"], trial.Root.Overrides);
        Assert.Empty(ContentCatalog.Load(trial.Root).Parks[ParkId.Harbor].Hazards);
    }

    /// <summary>
    /// A whole file, changed field by field, is exactly what a trial is for. The fields are the ones no
    /// hazard stands on (#862): the bags stay where they ship, for the reason
    /// <see cref="TheCatalogTakesTheTrialsTableAndTheShippedRest"/> gives.
    /// </summary>
    [Fact]
    public void AWholeFileIsAccepted()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json =>
        {
            json["baselineFt"] = 90;
            json["innerHalfFt"] = 50;
            json["backArcFt"] = 92;
        });

        Assert.Equal(90, ContentCatalog.Load(trial.Root).Rules.Infield.BaselineFt);
    }

    /// <summary>
    /// A root that cannot be read stops the run, named or not: there is no code table to play instead.
    /// <see cref="Diamond"/> reads this table, so the alternative is a process measuring a trial
    /// while its geometry is the control's.
    /// </summary>
    [Fact]
    public void ANamedRootStopsOnTablesItCannotRead()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json", json => json["baselineFt"] = 0);

        var asked = new DataRoot(Shipped, trial.Overlay) { Named = true };
        var thrown = Assert.Throws<InvalidDataException>(() => Rules.ForProcess(asked));
        Assert.Contains("infield.baselineFt", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(trial.Overlay, thrown.Message, StringComparison.Ordinal);

        // Unnamed, it stops too: there is no code table to play instead.
        Assert.Throws<InvalidDataException>(() => Rules.ForProcess(new DataRoot(Shipped, trial.Overlay)));
    }

    /// <summary>A root nobody named is not a run that asked for anything.</summary>
    [Fact]
    public void APlainRootIsNotNamed()
    {
        Assert.False(new DataRoot(Shipped).Named);
        Assert.False(Control.Root.Named);
    }

    /// <summary>
    /// A saved trace carries its own provenance, and a control trace carries none — so a control
    /// run's bytes are what they always were, and a trial's trace is not orphaned from the stderr
    /// line of whoever ran it.
    /// </summary>
    [Fact]
    public void ATraceNamesTheTrialItWasProducedUnderAndAControlTraceDoesNot()
    {
        Assert.Null(PlayTraceTrial.For(new DataRoot(Shipped)));

        using var trial = new Trial();
        trial.Override("rules/infield.json");
        var stamp = PlayTraceTrial.For(trial.Root);

        Assert.NotNull(stamp);
        Assert.Equal(["rules/infield.json"], stamp!.Files);

        var control = new PlayTraceLog(7, "rio", "ashlord", ParkId.Harbor, []).ToJson();
        var candidate = new PlayTraceLog(7, "rio", "ashlord", ParkId.Harbor, [], Trial: stamp).ToJson();
        Assert.DoesNotContain("trial", control, StringComparison.Ordinal);
        Assert.Contains("rules/infield.json", candidate, StringComparison.Ordinal);
    }

    /// <summary>An overlay in the repository is named the way a run names it, on any machine.</summary>
    [Fact]
    public void ATrialInTheRepositoryIsNamedByItsPathBesideTheData()
    {
        using var tree = new TrialBesideData("synthetic");
        Assert.Equal("trials/synthetic", new DataRoot(tree.Data, tree.Overlay).OverlayName);

        using var elsewhere = new Trial();
        Assert.Equal(elsewhere.Overlay, elsewhere.Root.OverlayName);
    }

    /// <summary>
    /// A path is resolved by its names, however a caller spells it, because the declaration is built
    /// from the filesystem. A stray separator that missed the lookup would read the shipped file
    /// while provenance claimed the overlay — the case defect again, by another route.
    /// </summary>
    [Fact]
    public void ASpellingDifferenceInTheSeparatorsResolvesTheSameWay()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json");
        var root = trial.Root;

        var expected = Path.Combine(trial.Overlay, "rules", "infield.json");
        Assert.Equal(expected, root.Resolve("rules", "infield.json"));
        Assert.Equal(expected, root.Resolve("rules/infield.json"));
        Assert.Equal(expected, root.Resolve("rules/", "/infield.json"));
        Assert.True(root.Overridden("rules/infield.json"));
    }

    /// <summary>Nothing below a data root needs to climb out of it, so a path that tries is a bug.</summary>
    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    public void APathThatWalksOutOfTheRootIsRefused(string part)
    {
        Assert.Throws<ArgumentException>(() => new DataRoot(Shipped).Resolve(part, "secrets.json"));
    }

    /// <summary>The list of overrides is the declaration, and cannot be edited out from under the lookup.</summary>
    [Fact]
    public void TheListOfOverridesCannotBeChanged()
    {
        using var trial = new Trial();
        trial.Override("rules/infield.json");

        Assert.Throws<NotSupportedException>(() => ((IList<string>)trial.Root.Overrides).Add("rules/flight.json"));
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

    /// <summary>
    /// A bare tree laid out the way the repository is — a <c>data</c> folder and, beside it,
    /// <c>trials/&lt;name&gt;</c> — so a test can name an overlay relative to the data root without
    /// writing into the repository. Both folders are empty: naming an overlay reads nothing.
    /// </summary>
    sealed class TrialBesideData : IDisposable
    {
        readonly string _stem;

        public TrialBesideData(string name)
        {
            _stem = Path.Combine(Path.GetTempPath(), "grand-sluggers-tree-" + Guid.NewGuid().ToString("N"));
            Data = Path.Combine(_stem, "data");
            Overlay = Path.Combine(_stem, "trials", name);
            Directory.CreateDirectory(Data);
            Directory.CreateDirectory(Overlay);
        }

        public string Data { get; }

        public string Overlay { get; }

        public void Dispose() => Directory.Delete(_stem, recursive: true);
    }

    /// <summary>
    /// A shipped root with the data taken out of it: every file replaced by text that is not data,
    /// and every real file moved into the overlay. A loader that reads the shipped side gets
    /// nonsense, so the catalog can only match the control if every read went through the overlay.
    /// </summary>
    sealed class GuttedRoot : IDisposable
    {
        const string NotData = "this file is not data — every read must resolve through the overlay";

        readonly string _shipped;
        readonly string _overlay;

        public GuttedRoot()
        {
            var stem = Path.Combine(Path.GetTempPath(), "grand-sluggers-gutted-" + Guid.NewGuid().ToString("N"));
            _shipped = Path.Combine(stem, "data");
            _overlay = Path.Combine(stem, "trial");
            foreach (var file in Directory.GetFiles(Shipped, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(Shipped, file);
                if (relative.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith('.'))) continue;
                Copy(file, Path.Combine(_overlay, relative));
                var gutted = Path.Combine(_shipped, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(gutted)!);
                File.WriteAllText(gutted, NotData);
            }
        }

        public DataRoot Root => new(_shipped, _overlay);

        static void Copy(string from, string to)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(from, to);
        }

        public void Dispose() => Directory.Delete(Path.GetDirectoryName(_shipped)!, recursive: true);
    }
}
