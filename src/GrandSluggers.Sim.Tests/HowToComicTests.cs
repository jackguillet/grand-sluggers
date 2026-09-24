using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class HowToComicTests
{
    [Fact]
    public void PitchAndSwingAreComicsNotAParagraph()
    {
        Assert.Equal(2, HowToComic.OnPitchSwingPage.Count);
        Assert.Equal("how-to-pitch", HowToComic.Pitch.Id);
        Assert.Equal("how-to-swing", HowToComic.Swing.Id);
        Assert.Equal("mound", HowToComic.Pitch.First.Shot);
        Assert.Equal("pitch", HowToComic.Pitch.Second.Shot);
        Assert.Equal("plate", HowToComic.Swing.First.Shot);
        Assert.Equal("smash", HowToComic.Swing.Second.Shot);
        Assert.Equal("how-to-pitch-1", HowToComic.Pitch.First.Picture);
        Assert.Equal("how-to-swing-1", HowToComic.Swing.First.Picture);
        Assert.Contains("MAX", HowToComic.Pitch.First.Label);
        Assert.Contains("MAX", HowToComic.Swing.First.Label);

        var pitch = HowToComic.Pitch.Caption;
        var swing = HowToComic.Swing.Caption;
        Assert.Contains("RT", pitch);
        Assert.Contains("RT", swing);
        Assert.Contains("MAX", pitch);
        Assert.Contains("MAX", swing);
        Assert.False(HowToPlay.NamesKeyboard(pitch));
        Assert.False(HowToPlay.NamesKeyboard(swing));

        Assert.Equal("Hold RT", HowToComic.Pitch.Motion.Charge);
        Assert.Equal("Release RT", HowToComic.Pitch.Motion.Commit);
        Assert.Equal("Hold RT", HowToComic.Swing.Motion.Charge);

        var row = HowToComic.Row(0, 1280, 800);
        Assert.True(row.W > 500);
        Assert.True(row.H > 190, "two comics plus measured couch copy still fit");
        var next = HowToComic.Row(1, 1280, 800);
        Assert.True(next.X > row.X);
        Assert.Equal(row.Y, next.Y);
        Assert.Equal("pitch-swing", HowToPlay.Must("pitch-swing").Id);
    }
}
