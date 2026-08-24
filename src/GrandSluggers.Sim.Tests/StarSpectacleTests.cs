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

    [Fact]
    public void CaptainSpecialsHaveCatalogVfxEvents()
    {
        foreach (var c in _content.Characters.Values.Where(c => c.Captain))
        {
            Assert.True(_content.Art.TryVfx(c.StarPitch, out var pitch), c.Id + " pitch " + c.StarPitch);
            Assert.False(string.IsNullOrWhiteSpace(pitch.Slot));
            Assert.True(_content.Art.TryVfx(c.StarSwing, out var swing), c.Id + " swing " + c.StarSwing);
            Assert.False(string.IsNullOrWhiteSpace(swing.Slot));
            Assert.Equal(2.0, StarSkills.SpectacleSeconds(pitch.Id));
            Assert.Equal(2.0, StarSkills.SpectacleSeconds(swing.Id));
        }
        Assert.True(_content.Art.TryVfx("heart-swing", out _));
        Assert.True(_content.Art.TryVfx("shell-swing", out _));
        Assert.True(_content.Art.TryVfx("phony-swing", out _));
        Assert.True(_content.Art.TryVfx("cask-swing", out _));
    }

    [Fact]
    public void RemainingSpecialsAreTwoSecondCatalogEventsNotBlinds()
    {
        foreach (var id in new[]
        {
            "charmball", "prismball", "phonyball", "caskball", "skullball",
            "heart-swing", "shell-swing", "phony-swing", "cask-swing", "furnace"
        })
        {
            Assert.Equal(2.0, StarSkills.SpectacleSeconds(id));
            Assert.True(_content.Art.TryVfx(id, out var slot), id);
            Assert.False(string.IsNullOrWhiteSpace(slot.Slot), id);
            var kind = (slot.Kind ?? "").ToLowerInvariant();
            Assert.Contains(kind, new[] { "ball", "field" });
            Assert.DoesNotContain("blind", kind);
        }
        Assert.Equal(2.0, StarSkills.SpectacleSeconds("heatball"));
        Assert.Equal("ball", _content.Art.TryVfx("heatball", out var heat) ? heat.Kind.ToLowerInvariant() : "");
    }
}
