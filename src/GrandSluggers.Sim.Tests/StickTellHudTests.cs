using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The pursuit stick's couch presentation (#718 Unity pass): Call time's Reset stick entry on the calibrated stick only, and the
/// let-go tell's copy and place on the broadcast HUD, the same recipe for 1P and 1v1.
/// </summary>
public sealed class StickTellHudTests
{
    [Fact]
    public void ResetStickSitsBetweenTheBookAndTitleOnlyOnTheCalibratedStick()
    {
        Assert.Equal(PauseMenu.Items, PauseMenu.ItemsFor(false));
        Assert.Equal(
            [PauseMenu.Item.Resume, PauseMenu.Item.Restart, PauseMenu.Item.HowToPlay, PauseMenu.Item.ResetStick, PauseMenu.Item.ArrangeDefense, PauseMenu.Item.Title, PauseMenu.Item.Quit],
            PauseMenu.ItemsFor(true));
        Assert.DoesNotContain(PauseMenu.Item.ResetStick, PauseMenu.Items);
        // The book keeps its row (Esc opens Call time on it) and Title stays last, with or without the entry.
        Assert.Equal(PauseMenu.Item.HowToPlay, PauseMenu.At(2, false));
        Assert.Equal(PauseMenu.Item.HowToPlay, PauseMenu.At(2, true));
        Assert.Equal(PauseMenu.Item.Quit, PauseMenu.ItemsFor(true)[^1]);
        Assert.Equal("Reset stick", PauseMenu.Label(PauseMenu.Item.ResetStick));
        Assert.Equal(0, PauseMenu.Wrap(6, 1, true));
        Assert.Equal(6, PauseMenu.Wrap(0, -1, true));
        Assert.Equal(PauseMenu.Item.ResetStick, PauseMenu.At(3, true));
    }

    [Theory]
    [InlineData(1280f, 800f)]
    [InlineData(1920f, 1080f)]
    public void TheCallTimePanelGrowsOneRowForTheEntryAndEveryRowSitsAboveTheFooter(float sw, float sh)
    {
        var shipped = PauseMenu.Panel(sw, sh);
        var stick = PauseMenu.Panel(sw, sh, true);
        Assert.Equal(PauseMenu.ItemH, stick.H - shipped.H, 3);
        for (var i = 0; i < PauseMenu.ItemsWithStick.Count; i++)
        {
            var r = PauseMenu.ItemRect(i, sw, sh, true);
            Assert.True(r.Y + r.H <= PauseMenu.FooterRect(sw, sh, true).Y, "the rows sit above the footer");
        }
        Assert.Equal(PauseMenu.Panel(sw, sh), PauseMenu.Panel(sw, sh, false));
        Assert.Equal(PauseMenu.ItemRect(0, sw, sh), PauseMenu.ItemRect(0, sw, sh, false));
    }

    [Fact]
    public void TheLetGoTellSaysWhoWhenTwoPlayAndNothingWhenReady()
    {
        Assert.Equal("LET GO OF THE STICK", BroadcastHud.StickLine(PursuitReadiness.Tell.LetGo, 0, twoPlayers: false));
        Assert.Equal("P2  ·  LET GO OF THE STICK", BroadcastHud.StickLine(PursuitReadiness.Tell.LetGo, 1, twoPlayers: true));
        Assert.Equal("STICK RESET", BroadcastHud.StickLine(PursuitReadiness.Tell.Reset, 0, twoPlayers: false));
        Assert.Equal("", BroadcastHud.StickLine(PursuitReadiness.Tell.None, 0, twoPlayers: true));
        Assert.Contains("LET GO OF THE STICK", BroadcastHud.UnreadyTell);
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void TheStickTellSitsInFrameClearOfTheScorebugTheCardsAndTheLiveTells(int sw, int sh)
    {
        var r = BroadcastHud.StickTell;
        Assert.True(BroadcastHud.InFrame(r, sw, sh));
        Assert.False(BroadcastHud.IsScreenCenterCard(r));
        var lay = BroadcastHud.Standard;
        foreach (var other in new[] { lay.Score, lay.Count, lay.MiniDiamond, lay.BatterCard, lay.PitcherCard, lay.Banner,
                     BroadcastHud.YouTell, BroadcastHud.ItemTell, BroadcastHud.StampGlove, BroadcastHud.StampBag, BroadcastHud.StampPlate, BroadcastHud.StampDirt })
            Assert.False(Overlaps(r, other), $"the stick tell overlaps {other}");
        // The throw pips are pixel-anchored to the bottom edge (HudView.BagTell: 168 px up, 88 px tall, a label 22 px above).
        Assert.True(r.Bottom * sh < sh - 168 - 22, "clear of the throw pips");
    }

    static bool Overlaps(BroadcastHud.HudRect a, BroadcastHud.HudRect b) =>
        a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
}
