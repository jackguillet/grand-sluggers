using Xunit;
namespace GrandSluggers.Sim.Tests;
public sealed class RoleTablesTests
{
    [Fact]
    public void ControllerRolesRetainStableLessonSourcesAndFitAcrossPages()
    {
        Assert.Equal(new[] { "batting", "pitching", "fielding", "running" }, RoleTables.Pad.Select(b => b.Id));
        foreach (var block in RoleTables.Pad)
        {
            Assert.InRange(block.Rows.Count, RoleTables.MinRows, RoleTables.MaxRows);
            Assert.All(block.Rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Press)));
        }
        var bat = RoleTables.Pad[0].Rows;
        Assert.Equal("Hold West", bat.Single(r => r.Verb == "Bunt to third").Press);
        Assert.Equal("Hold North", bat.Single(r => r.Verb == "Bunt to first").Press);
        Assert.Contains("RT", bat.Single(r => r.Verb == "Charge swing").Press);
        Assert.Contains("North", RoleTables.Pad[2].Rows.Single(r => r.Verb == "Jump").Press);
        Assert.Contains("Automatic", RoleTables.Pad[2].Rows.Single(r => r.Verb == "Catch").Press);
        Assert.Contains("D-pad Up", RoleTables.Pad[3].Rows.Single(r => r.Verb == "Halt").Press);
        Assert.All(RoleTables.PageIds, id => Assert.Equal(id, HowToPlay.Must(id).Id));
        Assert.Empty(TutorialCatalog.Load(ContentCatalog.Load()).Validate(ContentCatalog.Load()));
    }
}
