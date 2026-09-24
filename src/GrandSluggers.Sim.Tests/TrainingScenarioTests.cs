using System.Text.Json.Nodes;
using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Spec Appendix B.1 row S-133 (#888): Training offers the families of the table it was started
/// from, in library order. The shipped root is loaded in process through
/// a <see cref="DataRoot"/> with no overlay, so a trial in the environment cannot change the row;
/// the two-family table is a copy of the shipped root, built here, whose <c>pitching.json</c> drops
/// the three optional rows (a trial overlay may not carry part of a file).
/// </summary>
public sealed class TrainingScenarioTests
{
    static readonly string ShippedRoot = ContentCatalog.Load().Root.Shipped;

    [Fact]
    public void S133_TrainingOffersTheLoadedTablesPitchFamilies()
    {
        // (a) The shipped root authors all five: a practice session offers all five, in library order.
        var shipped = ContentCatalog.Load(new DataRoot(ShippedRoot));
        var onShipped = Training.Start(shipped);
        Assert.Equal(PitchFamily.All, onShipped.Pitches);
        Assert.Equal(
            new[] { PitchFamily.Fastball, PitchFamily.Changeup, PitchFamily.Curveball, PitchFamily.Slider, PitchFamily.Sinker },
            onShipped.Pitches);
        Assert.Equal(shipped.Rules.Pitching.Families.Authored, Training.PitchesOf(shipped.Rules));

        // (b) A loaded table that authors two offers two — the same session code, a different table.
        var copy = Path.Combine(Path.GetTempPath(), "grand-sluggers-training-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var directory in Directory.GetDirectories(ShippedRoot, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(copy, Path.GetRelativePath(ShippedRoot, directory)));
            foreach (var file in Directory.GetFiles(ShippedRoot, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(copy, Path.GetRelativePath(ShippedRoot, file)));
            var path = Path.Combine(copy, RulesTable.Directory, "pitching.json");
            var pitching = JsonNode.Parse(File.ReadAllText(path))!;
            var families = pitching["families"]!.AsObject();
            foreach (var dropped in new[] { PitchFamily.Curveball, PitchFamily.Slider, PitchFamily.Sinker })
                Assert.True(families.Remove(dropped), dropped + " is in the shipped pitching.json");
            File.WriteAllText(path, pitching.ToJsonString());

            var two = ContentCatalog.Load(new DataRoot(copy));
            Assert.Equal(new[] { PitchFamily.Fastball, PitchFamily.Changeup }, Training.Start(two).Pitches);
            Assert.Equal(new[] { PitchFamily.Fastball, PitchFamily.Changeup }, Training.PitchesOf(two.Rules));
        }
        finally
        {
            Directory.Delete(copy, recursive: true);
        }

        // (c) A table that authors two offers two.
        Assert.Equal(new[] { PitchFamily.Fastball, PitchFamily.Changeup }, Training.PitchesOf(RuleCopies.TwoFamilies()));
    }
}
