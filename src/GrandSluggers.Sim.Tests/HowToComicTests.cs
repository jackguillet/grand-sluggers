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

        var padPitch = HowToComic.Caption(HowToComic.Pitch, InputScheme.Pad);
        var keyPitch = HowToComic.Caption(HowToComic.Pitch, InputScheme.Keys);
        var padSwing = HowToComic.Caption(HowToComic.Swing, InputScheme.Pad);
        var keySwing = HowToComic.Caption(HowToComic.Swing, InputScheme.Keys);
        Assert.Contains("South", padPitch);
        Assert.DoesNotContain("Space", padPitch);
        Assert.Contains("Space", keyPitch);
        Assert.DoesNotContain("South", keyPitch);
        Assert.Contains("South", padSwing);
        Assert.Contains("Space", keySwing);
        Assert.Contains("MAX", padPitch);
        Assert.Contains("MAX", keySwing);
        Assert.False(HowToPlay.MixesHardware(padPitch));
        Assert.False(HowToPlay.MixesHardware(keyPitch));
        Assert.False(HowToPlay.MixesHardware(padSwing));
        Assert.False(HowToPlay.MixesHardware(keySwing));

        Assert.Equal("LT", HowToComic.MotionOf(HowToComic.Pitch, InputScheme.Pad).Charge);
        Assert.Equal("South", HowToComic.MotionOf(HowToComic.Pitch, InputScheme.Pad).Commit);
        Assert.Contains("Shift", HowToComic.MotionOf(HowToComic.Pitch, InputScheme.Keys).Charge);
        Assert.Contains("Space", HowToComic.MotionOf(HowToComic.Pitch, InputScheme.Keys).Commit);

        var row = HowToComic.Row(0, 1280, 800);
        Assert.True(row.W > 900);
        Assert.True(row.H > 180);
        var next = HowToComic.Row(1, 1280, 800);
        Assert.True(next.Y > row.Y);
        Assert.Equal("pitch-swing", HowToPlay.Must("pitch-swing").Id);
    }
}
