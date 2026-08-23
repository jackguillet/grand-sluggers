using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class StarSpectacleTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void SixCaptainsHaveUniquePitchAndSwing()
    {
        var caps = _content.Characters.Values.Where(c => c.Captain).ToList();
        Assert.Equal(6, caps.Count);
        Assert.Equal(6, caps.Select(c => c.StarPitch).Distinct().Count());
        Assert.Equal(6, caps.Select(c => c.StarSwing).Distinct().Count());
        Assert.Contains(caps, c => c.Id == "rio" && c.StarPitch == "heatball" && c.StarSwing == "heat-swing");
        Assert.Contains(caps, c => c.Id == "vale" && c.StarPitch == "charmball" && c.StarSwing == "heart-swing");
        Assert.Contains(caps, c => c.Id == "zig" && c.StarPitch == "prismball" && c.StarSwing == "shell-swing");
        Assert.Contains(caps, c => c.Id == "brondo" && c.StarPitch == "phonyball" && c.StarSwing == "phony-swing");
        Assert.Contains(caps, c => c.Id == "konga" && c.StarPitch == "caskball" && c.StarSwing == "cask-swing");
        Assert.Contains(caps, c => c.Id == "ashlord" && c.StarPitch == "skullball" && c.StarSwing == "furnace");
    }

    [Fact]
    public void EveryCaptainSpecialOwnsTwoSecondsThenBaseballResumes()
    {
        foreach (var c in _content.Characters.Values.Where(c => c.Captain))
        {
            Assert.Equal(2.0, StarSkills.SpectacleSeconds(c.StarPitch));
            Assert.Equal(2.0, StarSkills.SpectacleSeconds(c.StarSwing));
        }
        Assert.Equal(0, StarSkills.SpectacleSeconds(null));
        Assert.Equal(0, StarSkills.SpectacleSeconds(""));
    }
}
