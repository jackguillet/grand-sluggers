using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class TeamBuilderTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void DraftFillsNineAndKeepsTheCaptain()
    {
        foreach (var id in PresetTeams.CaptainIds)
        {
            var b = TeamBuilder.Draft(_content, id);
            Assert.Equal(9, b.Order.Count);
            Assert.Equal(9, b.Order.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(id, b.Captain.Id);
            Assert.Contains(b.Order, c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            Assert.Equal("P", b.PosOf(id));
            Assert.Equal(id, b.ToTeam().Pitcher.Id);
        }
    }

    [Fact]
    public void LockedCaptainCannotBeReplaced()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        var other = b.Pool().First(c => !c.Captain);
        Assert.False(b.Replace("vale", other.Id));
        Assert.Contains(b.Order, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(9, b.Order.Count);
        Assert.Equal("vale", b.Captain.Id);
    }

    [Fact]
    public void SwappingARivalOntoTheRosterLowersStartingStarsVsABuddy()
    {
        var neutrals = new[] { "boom", "hex", "nugget", "grit", "moss", "basil", "jester", "gull" };
        foreach (var id in neutrals)
            Assert.Equal(Chemistry.Neutral, _content.Chemistry.Between("vale", id));
        Assert.Equal(Chemistry.Bad, _content.Chemistry.Between("vale", "ashlord"));
        Assert.Equal(Chemistry.Good, _content.Chemistry.Between("vale", "rio"));

        var rival = TeamBuilder.Draft(_content, "vale");
        var buddy = TeamBuilder.Draft(_content, "vale");
        var withRival = (string[])neutrals.Clone();
        withRival[0] = "ashlord";
        var withBuddy = (string[])neutrals.Clone();
        withBuddy[0] = "rio";
        Assert.True(rival.Fill(withRival));
        Assert.True(buddy.Fill(withBuddy));

        Assert.True(rival.StartingStars < buddy.StartingStars,
            $"rival {rival.StartingStars} vs buddy {buddy.StartingStars} (avg {rival.AverageWithCaptain:0} vs {buddy.AverageWithCaptain:0})");
        Assert.True(rival.AverageWithCaptain < buddy.AverageWithCaptain);
        Assert.Contains(rival.Order, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(buddy.Order, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(9, rival.Order.Count);
        Assert.Equal(9, buddy.Order.Count);
        Assert.Contains(rival.Order, c => c.Id.Equals("ashlord", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(buddy.Order, c => c.Id.Equals("rio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReplaceKeepsNineAndTheCaptain()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        var outgoing = b.Order.First(c => !c.Captain).Id;
        var incoming = b.Pool().First(c => !c.Captain);
        Assert.True(b.Replace(outgoing, incoming.Id));
        Assert.Equal(9, b.Order.Count);
        Assert.DoesNotContain(b.Order, c => c.Id.Equals(outgoing, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(b.Order, c => c.Id.Equals(incoming.Id, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(b.Order, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SetGloveMovesTheMoundOffTheCaptain()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        Assert.Equal("vale", b.Gloves["P"].Id);
        var other = b.Order.First(c => !c.Captain);
        Assert.True(b.SetGlove("P", other.Id));
        Assert.Equal(other.Id, b.Gloves["P"].Id);
        Assert.Equal("C", b.PosOf("vale"));
        Assert.Equal("P", TeamBuilder.GloveGroup(b.PosOf(other.Id)!));
        Assert.Equal("vale", b.Captain.Id);

        var away = PresetTeams.ForCaptain(_content, "brondo", exclude: b.Order.Select(c => c.Id));
        var match = Match.Exhibition(_content, b.ToTeam(), away, seed: 7);
        Assert.Equal(other.Id, match.Pitcher.Id);
        Assert.Equal("vale", match.Home.Captain.Id);
        Assert.Equal(9, match.Home.Roster.Count);
        Assert.Equal(b.StartingStars, (int)match.HomeStars);
    }

    [Fact]
    public void CycleGloveWalksPThenCThenIfThenOf()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        Assert.Equal("P", b.PosOf("vale"));
        Assert.True(b.CycleGlove("vale"));
        Assert.Equal("C", b.PosOf("vale"));
        Assert.Equal("C", TeamBuilder.GloveGroup(b.PosOf("vale")!));
        Assert.True(b.CycleGlove("vale"));
        Assert.Equal("1B", b.PosOf("vale"));
        Assert.Equal("IF", TeamBuilder.GloveGroup(b.PosOf("vale")!));
        b.CycleGlove("vale");
        b.CycleGlove("vale");
        b.CycleGlove("vale");
        Assert.Equal("SS", b.PosOf("vale"));
        Assert.True(b.CycleGlove("vale"));
        Assert.Equal("LF", b.PosOf("vale"));
        Assert.Equal("OF", TeamBuilder.GloveGroup(b.PosOf("vale")!));
        b.CycleGlove("vale");
        b.CycleGlove("vale");
        Assert.True(b.CycleGlove("vale"));
        Assert.Equal("P", b.PosOf("vale"));
    }

    [Fact]
    public void CustomBattingOrderIsUsedByTheMatch()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        Assert.True(b.SwapOrder(0, 8));
        var team = b.ToTeam();
        var away = PresetTeams.ForCaptain(_content, "brondo", exclude: team.Roster.Select(c => c.Id));
        var match = Match.Exhibition(_content, team, away, seed: 1);
        Assert.Equal(team.BattingOrder[0].Id, match.HomeOrder[0].Id);
        Assert.Equal(team.BattingOrder[8].Id, match.HomeOrder[8].Id);
        Assert.Equal(9, match.HomeOrder.Count);
        Assert.Contains(match.HomeOrder, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExhibitionFromBuildersKeepsNineUniqueAndPlays()
    {
        var home = TeamBuilder.Draft(_content, "vale");
        var away = TeamBuilder.Draft(_content, "brondo", exclude: home.Order.Select(c => c.Id));
        var overlap = home.Order.Select(c => c.Id)
            .Intersect(away.Order.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
        Assert.Empty(overlap);
        var match = Match.Exhibition(_content, home.ToTeam(), away.ToTeam(), seed: 7, parkId: "crystal-rink");
        Assert.Equal("vale", match.Home.Captain.Id);
        Assert.Equal("brondo", match.Away.Captain.Id);
        Assert.Equal(9, match.Home.Roster.Count);
        Assert.Equal(9, match.Away.Roster.Count);
        Assert.Equal("vale", match.Pitcher.Id);
        match.AutoPlayGame();
        Assert.True(match.Over);
    }

    [Fact]
    public void DefaultExhibitionStillPitchesTheCaptain()
    {
        var match = Match.Exhibition(_content, "vale", "brondo", seed: 7);
        Assert.Equal("vale", match.Pitcher.Id);
        Assert.Equal("vale", match.Home.Captain.Id);
        Assert.Equal(9, match.Home.Roster.Count);
        Assert.Equal(9, match.HomeOrder.Count);
    }

    [Fact]
    public void FillRejectsWrongCountAndCaptainInMates()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        Assert.False(b.Fill(["frost", "lace"]));
        Assert.False(b.Fill(["vale", "frost", "lace", "pewter", "nico", "pip", "marlow", "gull"]));
        Assert.Contains(b.Order, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(9, b.Order.Count);
    }
}
