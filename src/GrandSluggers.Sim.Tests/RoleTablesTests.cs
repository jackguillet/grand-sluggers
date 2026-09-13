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
            Assert.InRange(block.Rows.Count, RoleTables.MinRows, RoleTables.MaxRows);
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
        Assert.DoesNotContain(padRun, v => v.Equals("Tag", StringComparison.OrdinalIgnoreCase));
        var padField = RoleTables.Pad.First(b => b.Id == "fielding").Rows.Select(r => r.Verb);
        Assert.Contains(padField, v => v.Contains("Jump"));
        Assert.Contains(padField, v => v.Contains("Dive"));
        Assert.Contains(padField, v => v.Contains("Attack"));
        Assert.Contains(padField, v => v.Equals("Tag", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(RoleTables.Pad.SelectMany(b => b.Rows), r => r.Verb.Contains("Charge"));
        Assert.Contains(RoleTables.Pad.First(b => b.Id == "batting").Rows, r => r.Verb.Contains("Bunt"));
        foreach (var row in RoleTables.Pad.SelectMany(block => block.Rows).Where(row => row.Verb.Contains("Charge")))
        {
            Assert.Contains("Hold South", row.Press);
            Assert.Contains("release", row.Press, StringComparison.OrdinalIgnoreCase);
        }
        foreach (var row in RoleTables.Keys.SelectMany(block => block.Rows).Where(row => row.Verb.Contains("Charge")))
        {
            Assert.Contains("Hold Space / left click", row.Press);
            Assert.Contains("release", row.Press, StringComparison.OrdinalIgnoreCase);
        }

        var page = HowToPlay.Must("roles");
        Assert.Contains(page.Lines, l => l.Contains("batting") && l.Contains("running"));
        Assert.False(HowToPlay.MixesHardware(string.Join(' ', page.Lines)));
        Assert.Equal(3, RoleTables.OnPage(InputScheme.Pad, "roles").Rows.Count);
        Assert.Equal(3, RoleTables.OnPage(InputScheme.Pad, "roles-batting-2").Rows.Count);
        Assert.Equal(4, RoleTables.OnPage(InputScheme.Keys, "roles-pitching").Rows.Count);
        Assert.Equal(4, RoleTables.OnPage(InputScheme.Keys, "roles-pitching-2").Rows.Count);
        // Fielding and running continue onto a second page like batting and pitching; the halves cover every row.
        foreach (var (block, first, second) in new[]
                 {
                     (RoleTables.Pad[2], "roles-fielding", "roles-fielding-2"),
                     (RoleTables.Pad[3], "roles-running", "roles-running-2"),
                     (RoleTables.Keys[2], "roles-fielding", "roles-fielding-2"),
                     (RoleTables.Keys[3], "roles-running", "roles-running-2"),
                 })
        {
            var scheme = ReferenceEquals(block, RoleTables.Pad[2]) || ReferenceEquals(block, RoleTables.Pad[3]) ? InputScheme.Pad : InputScheme.Keys;
            var a = RoleTables.OnPage(scheme, first).Rows;
            var b = RoleTables.OnPage(scheme, second).Rows;
            Assert.Equal(block.Rows, a.Concat(b));
            Assert.InRange(a.Count, 4, 5);
            Assert.InRange(b.Count, 4, 5);
        }
        Assert.All(RoleTables.PageIds, id => Assert.Equal(id, HowToPlay.Must(id).Id));
        var cell = RoleTables.RowCard(0, RoleTables.OnPage(InputScheme.Pad, "roles-running").Rows.Count, 1280, 800);
        Assert.True(cell.W > 1000);
        Assert.True(cell.H > 40);
        var next = RoleTables.RowCard(1, RoleTables.OnPage(InputScheme.Pad, "roles-running").Rows.Count, 1280, 800);
        Assert.True(next.Y > cell.Y);
    }
}
