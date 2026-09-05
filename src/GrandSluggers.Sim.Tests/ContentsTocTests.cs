using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ContentsTocTests
{
    [Fact]
    public void ContentsIsATableOfChaptersWithPageNumbers()
    {
        Assert.Equal("contents", HowToPlay.Pages[0].Id);
        Assert.True(ContentsToc.Chapters.Count >= 4);
        Assert.Equal(ContentsToc.ChapterIds.Count, ContentsToc.Chapters.Count);
        Assert.DoesNotContain(ContentsToc.Chapters, c => c.Id == "contents");
        Assert.Equal(
            ["controls", "roles", "pitch-swing", "running", "fielding",
                "getting-started", "screen", "chemistry", "abilities"],
            ContentsToc.Chapters.Select(c => c.Id));
        foreach (var chapter in ContentsToc.Chapters)
        {
            var page = HowToPlay.Must(chapter.Id);
            var index = HowToPlay.Pages.ToList().FindIndex(p => p.Id == chapter.Id);
            Assert.Equal(page.Title, chapter.Title);
            Assert.Equal(index + 1, chapter.Number);
            Assert.InRange(chapter.Number, 2, HowToPlay.Pages.Count);
            Assert.False(HowToPlay.MixesHardware(chapter.Title), chapter.Id);
        }
        Assert.Contains(HowToPlay.Must("contents").Lines,
            l => l.Contains("instruction booklet") || l.Contains("Call time"));
        Assert.Contains(HowToPlay.Must("contents").Lines, l => l.Contains("list") || l.Contains("book"));
        Assert.Equal("contents", ContentsToc.Picture);
        const float w = 1280, h = 800;
        var still = ContentsToc.Still(w, h);
        var card = ContentsToc.Card(w, h);
        var band = ContentsToc.LineBand(w, h);
        Assert.True(card.X >= still.X);
        Assert.True(card.X + card.W < still.X + still.W * 0.55f, "card sits on the left, like SMS");
        Assert.True(card.Y + card.H <= still.Y + still.H + 1f);
        Assert.True(band.Y >= still.Y + still.H - 1f);
        var first = ContentsToc.Row(0, w, h);
        var last = ContentsToc.Row(ContentsToc.Chapters.Count - 1, w, h);
        Assert.True(last.Y > first.Y);
        Assert.True(first.X >= card.X);
        Assert.True(first.X + first.W <= card.X + card.W + 1f);
    }
}
