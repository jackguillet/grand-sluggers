using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class CaptainSelectionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OneControllerConfirmsOwnThenOpponentAndPreservesSides(bool home)
    {
        var pick = ExhibitionPick.Default with { Pad1Home = home };
        var board = new CaptainSelection(Shipped.Content, pick, false);
        Assert.Equal(pick.Yours, board.Id(0));
        Assert.True(board.Confirm(0));
        Assert.Equal(1, board.ActiveOne);
        Assert.False(board.Complete);
        Assert.True(board.Confirm(1));
        Assert.True(board.Complete);
        Assert.Equal(pick, board.ApplyTo(pick));
        Assert.False(board.Back(0));
        Assert.False(board.Ready(0));
        Assert.False(board.Ready(1));
        Assert.True(board.Back(0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EitherSeatCanReserveACaptainWithoutMovingOrOverwritingTheOther(int first)
    {
        var board = new CaptainSelection(Shipped.Content, ExhibitionPick.Default, true);
        var other = 1 - first;
        while (board.Id(other) != board.Id(first)) board.Move(other, 1);
        var chosen = board.Id(first);
        Assert.True(board.Confirm(first));
        Assert.True(board.Taken(other));
        Assert.False(board.Confirm(other));
        Assert.False(board.Complete);
        board.Move(first, 1);
        Assert.Equal(chosen, board.Id(first));
        board.Move(other, 1);
        Assert.True(board.Confirm(other));
        Assert.True(board.Complete);
        Assert.False(board.Back(first));
        Assert.True(board.Ready(other));
    }

    [Fact]
    public void BothCursorsReachEveryCaptainAndWrapWithoutChangingTheOther()
    {
        var board = new CaptainSelection(Shipped.Content, ExhibitionPick.Default, true);
        foreach (var panel in new[] { 0, 1 })
        {
            var start = board.Id(panel);
            var untouched = board.Id(1 - panel);
            var seen = new HashSet<string>();
            for (var i = 0; i < board.Count; i++)
            {
                seen.Add(board.Id(panel)); board.Move(panel, 1);
                Assert.Equal(untouched, board.Id(1 - panel));
            }
            Assert.Equal(Shipped.CaptainIds.Count, seen.Count);
            Assert.Equal(start, board.Id(panel));
            board.Move(panel, -1); board.Move(panel, 1);
            Assert.Equal(start, board.Id(panel));
        }
    }

    [Fact]
    public void TeamPanelsAndEveryPortraitHaveSeparateSpaceAboveTheFooter()
    {
        var boxes = new List<CaptainPanelRect> { CarnivalFront.CaptainPanel(0), CarnivalFront.CaptainPanel(1) };
        boxes.AddRange(Enumerable.Range(0, Shipped.CaptainIds.Count)
            .Select(i => CarnivalFront.CaptainTile(i, Shipped.CaptainIds.Count)));
        for (var i = 0; i < boxes.Count; i++)
        {
            var a = boxes[i];
            Assert.InRange(a.X, 24, 1256 - a.W);
            Assert.InRange(a.Y, 92, 724 - a.H);
            for (var j = i + 1; j < boxes.Count; j++)
            {
                var b = boxes[j];
                Assert.True(a.X + a.W <= b.X || b.X + b.W <= a.X || a.Y + a.H <= b.Y || b.Y + b.H <= a.Y);
            }
        }
    }
}
