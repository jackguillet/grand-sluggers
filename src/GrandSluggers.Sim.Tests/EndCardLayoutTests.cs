using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The game-over cards are laid out like the rest of the HUD (#1046): normalized, authored at the 1280×800 reference
/// window, so they keep their pixels there and scale everywhere else, and their words live in <see cref="BroadcastHud"/>.
/// </summary>
public sealed class EndCardLayoutTests
{
    static (double X, double Y, double W, double H) At(BroadcastHud.HudRect r, double w, double h) => r.Pixel(w, h);

    [Fact]
    public void TheFinalCardKeepsItsPixelsInTheReferenceWindow()
    {
        var p = At(BroadcastHud.FinalCard.Panel, 1280, 800);
        Assert.Equal((48.0, 48.0, 640.0, 320.0), (Math.Round(p.X, 6), Math.Round(p.Y, 6), Math.Round(p.W, 6), Math.Round(p.H, 6)));
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    [InlineData(1280, 720)]
    public void TheCardsStayOnScreenAndTheFinalRowsStayInTheirCard(double w, double h)
    {
        var f = BroadcastHud.FinalCard;
        foreach (var card in new[] { f.Panel, BroadcastHud.ReplayCard.Panel, BroadcastHud.ReplayCard.Score })
        {
            var p = At(card, w, h);
            Assert.True(p.X >= 8 && p.Y >= 8 && p.X + p.W <= w - 8 && p.Y + p.H <= h - 8, $"{card} off screen at {w}x{h}");
        }
        foreach (var row in new[] { f.Title, f.Away, f.Home, f.HighlightLabel, f.Highlight, f.Mvp, f.MvpWhy, f.Continue })
        {
            Assert.True(row.X >= f.Panel.X && row.Right <= f.Panel.Right, $"{row} leaves the card sideways");
            Assert.True(row.Y >= f.Panel.Y && row.Bottom <= f.Panel.Bottom, $"{row} leaves the card");
        }
    }

    [Fact]
    public void TheFinalCardSaysThePadsConfirm()
    {
        // The game is gamepad only: the way on is South, as the title says it, never a keyboard key.
        Assert.StartsWith("South", BroadcastHud.FinalContinue, StringComparison.Ordinal);
        Assert.DoesNotContain("SPACE", BroadcastHud.FinalContinue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSmashNamesWhereItLooksWhenThePlayLeftNoMoment()
    {
        var smash = Shipped.Content.Shots.Must("smash");
        Assert.NotNull(smash.Fallback);
        Assert.Equal(HomeSet.BatterChestY, smash.Fallback!.Value.Y);
    }

    [Fact]
    public void TheRubberWalksAtTheBoxsPace() =>
        Assert.Equal(HomeSet.BoxWalkStep(1f, 0.5f), HomeSet.RubberWalkStep(1f, 0.5f));
}
