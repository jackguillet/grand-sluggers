using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class RoleTablesTests
{
    [Fact]
    public void FourRoleTablesOneSchemeAtATime()
    {
        Assert.Equal(4, RoleTables.Pad.Count);
        Assert.Equal(4, RoleTables.Keys.Count);
        Assert.Equal(["batting", "pitching", "fielding", "running"], RoleTables.Pad.Select(b => b.Id));
        Assert.Equal(["batting", "pitching", "fielding", "running"], RoleTables.Keys.Select(b => b.Id));
        Assert.Same(RoleTables.Pad, RoleTables.Of(InputScheme.Pad));
        Assert.Same(RoleTables.Keys, RoleTables.Of(InputScheme.Keys));

        foreach (var block in RoleTables.Pad.Concat(RoleTables.Keys))
        {
            Assert.InRange(block.Rows.Count, 4, 8);
            foreach (var row in block.Rows)
            {
                Assert.False(string.IsNullOrWhiteSpace(row.Verb), block.Id);
                Assert.False(string.IsNullOrWhiteSpace(row.Press), row.Verb);
                Assert.False(HowToPlay.MixesHardware(row.Press), block.Id + " " + row.Verb + ": " + row.Press);
            }
        }

        var padBat = RoleTables.Pad.First(b => b.Id == "batting").Rows;
        Assert.Contains(padBat, r => r.Press.Contains("South"));
        Assert.DoesNotContain(padBat, r => r.Press.Contains("Space"));
        var keyBat = RoleTables.Keys.First(b => b.Id == "batting").Rows;
        Assert.Contains(keyBat, r => r.Press.Contains("Space"));
        Assert.DoesNotContain(keyBat, r => r.Press.Contains("South"));

        var padRun = RoleTables.Pad.First(b => b.Id == "running").Rows.Select(r => r.Verb);
        Assert.Contains(padRun, v => v.Contains("Steal"));
        Assert.Contains(padRun, v => v.Contains("Close play"));
        Assert.Contains(padRun, v => v.Equals("Tag", StringComparison.OrdinalIgnoreCase));
        var padField = RoleTables.Pad.First(b => b.Id == "fielding").Rows.Select(r => r.Verb);
        Assert.Contains(padField, v => v.Contains("Jump"));
        Assert.Contains(padField, v => v.Contains("Dive"));
        Assert.Contains(padField, v => v.Contains("Attack"));
        Assert.Contains(RoleTables.Pad.SelectMany(b => b.Rows), r => r.Press.Contains("LT") || r.Verb.Contains("Charge"));
        Assert.Contains(RoleTables.Pad.First(b => b.Id == "batting").Rows, r => r.Verb.Contains("Bunt"));

        var page = HowToPlay.Must("roles");
        Assert.Contains(page.Lines, l => l.Contains("batting") && l.Contains("running"));
        Assert.False(HowToPlay.MixesHardware(string.Join(' ', page.Lines)));
        var cell = RoleTables.Cell(0, 1280, 800);
        Assert.True(cell.W > 400);
        Assert.True(cell.H > 200);
        var next = RoleTables.Cell(1, 1280, 800);
        Assert.True(next.X > cell.X);
    }
}
