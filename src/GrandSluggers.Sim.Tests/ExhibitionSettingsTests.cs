using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class ExhibitionSettingsTests
{
    [Fact]
    public void OnlyPlayerOneCanChangeRulesAndItemsStayUnavailable()
    {
        var settings = new ExhibitionSettings();
        Assert.True(settings.Stars);
        Assert.False(settings.Change(LineupSeat.Pad2));
        Assert.False(settings.Change(LineupSeat.Cpu));
        Assert.True(settings.Stars);
        Assert.True(settings.Change(LineupSeat.Pad1));
        Assert.False(settings.Stars);
        settings.Select(1);
        Assert.False(settings.ItemsAvailable);
        Assert.Equal("UNAVAILABLE", settings.Value(1));
        Assert.False(settings.Change(LineupSeat.Pad1));
    }

    [Fact]
    public void InningsAndSkillCycleBothWaysAndMercyExplainsShortGames()
    {
        var settings = new ExhibitionSettings();
        var rules = Shipped.Content.Rules;
        Assert.Contains("3-inning", settings.Description(3, rules));
        settings.Select(2);
        foreach (var innings in new[] { 6, 9, 3 })
        {
            Assert.True(settings.Change(LineupSeat.Pad1));
            Assert.Equal(innings, settings.Innings);
        }
        settings.Change(LineupSeat.Pad1, -1);
        Assert.Equal(9, settings.Innings);
        Assert.Contains("10-run", settings.Description(3, rules));
        settings.Select(3);
        settings.Change(LineupSeat.Pad1);
        Assert.False(settings.Mercy);
        settings.Select(4);
        settings.Change(LineupSeat.Pad1);
        Assert.Equal("hard", settings.Difficulty);
        settings.Change(LineupSeat.Pad1, -1);
        Assert.Equal("normal", settings.Difficulty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothPlayersApprovePositionsThenApproveSettingsSeparately(bool reversed)
    {
        var lineup = LineupScreens.Open(Shipped.Content, "vale", "brondo",
            reversed ? LineupSeat.Pad2 : LineupSeat.Pad1, reversed ? LineupSeat.Pad1 : LineupSeat.Pad2);
        lineup.RandomFill(LineupSeat.Pad1);
        lineup.RandomFill(LineupSeat.Pad2);
        Assert.True(lineup.ConfirmTeam());
        lineup.ToggleReady(LineupSeat.Pad1);
        Assert.False(lineup.OpenSettings());
        lineup.ToggleReady(LineupSeat.Pad2);
        Assert.True(lineup.OpenSettings());
        Assert.Equal(LineupStep.MatchSettings, lineup.Step);
        Assert.False(lineup.IsReady(LineupSeat.Pad1));
        Assert.False(lineup.IsReady(LineupSeat.Pad2));
        lineup.ToggleReady(LineupSeat.Pad2);
        Assert.False(lineup.BothReady);
        lineup.ToggleReady(LineupSeat.Pad1);
        Assert.True(lineup.BothReady);
        lineup.ResetReady();
        Assert.False(lineup.IsReady(LineupSeat.Pad1));
        Assert.False(lineup.IsReady(LineupSeat.Pad2));
        Assert.False(lineup.West(LineupSeat.Pad2));
        Assert.True(lineup.West(LineupSeat.Pad1));
        Assert.Equal(LineupStep.DefenseSetup, lineup.Step);
    }

    [Fact]
    public void ReturningThroughSetupPreservesAnUnchangedTeamsOrderAndGloves()
    {
        var lineup = LineupScreens.Open(Shipped.Content, "vale", "brondo");
        lineup.RandomFill();
        lineup.ConfirmTeam();
        lineup.FocusCell(LineupSeat.Pad1, LineupFocus.HomeOrder, 0);
        lineup.PickOrSwap(LineupSeat.Pad1);
        lineup.FocusCell(LineupSeat.Pad1, LineupFocus.HomeOrder, 2);
        Assert.True(lineup.PickOrSwap(LineupSeat.Pad1));
        lineup.ToggleArea(LineupSeat.Pad1);
        lineup.PickOrSwap(LineupSeat.Pad1);
        lineup.Stick(LineupSeat.Pad1, 1, 0);
        lineup.PickOrSwap(LineupSeat.Pad1);
        var home = lineup.Home;
        var away = lineup.Away;
        lineup.ToggleReady(LineupSeat.Pad1);
        Assert.True(lineup.OpenSettings());
        Assert.True(lineup.BackToDefense());
        Assert.True(lineup.BackToTeam());
        Assert.True(lineup.ConfirmTeam());
        Assert.Same(home, lineup.Home);
        Assert.Same(away, lineup.Away);
        Assert.False(lineup.BothReady);
    }

    [Fact]
    public void SettingsPointerTargetsAreSeparateFromStartAndBack()
    {
        var rows = Enumerable.Range(0, ExhibitionSettings.RowCount).Select(ExhibitionSetupLayout.SettingsRow).ToArray();
        foreach (var row in rows)
        {
            var x = row.X + row.W / 2; var y = row.Y + row.H / 2;
            Assert.Single(rows, r => ExhibitionSetupLayout.Contains(r, x, y));
            Assert.False(ExhibitionSetupLayout.Contains(ExhibitionSetupLayout.Next, x, y));
            Assert.False(ExhibitionSetupLayout.Contains(ExhibitionSetupLayout.Back, x, y));
        }
    }
}
