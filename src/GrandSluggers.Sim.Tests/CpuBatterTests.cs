using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CpuBatterTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    static PitchCommand Meatball => new("fastball", 0, 0, false, 0, 0);
    static PitchCommand Charged => new("fastball", 1, 0, false, 0, 0);
    static PitchCommand StarPitch => new("fastball", 1, 0, true, 0, 0);

    [Fact]
    public void CpuVsHumanMeatballIsMixedNotAllExtraBase()
    {
        const int n = 200;
        var extra = 0;
        var outs = 0;
        var bip = 0;
        var takes = 0;
        var misses = 0;
        for (var seed = 1; seed <= n; seed++)
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", seed: seed);
            Assert.True(match.Top, "human pitches the top");
            var inZone = AtBatResolver.PitchInZone(Meatball, match.Pitcher.Stats.Pitch);
            Assert.True(inZone, "middle-middle uncharged fastball is in the zone");
            var swing = match.CpuSwing(Meatball, inZone, vsHumanPitcher: true);
            var ev = match.Play(Meatball, swing);
            if (!swing.Swing) takes++;
            if (ev.Kind is PlayKind.SwingMiss) misses++;
            if (ev.Kind is PlayKind.GroundOut or PlayKind.FlyOut or PlayKind.Strikeout)
                outs++;
            if (ev.Kind is PlayKind.GroundOut or PlayKind.FlyOut or PlayKind.Single
                or PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)
                bip++;
            if (ev.Kind is PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)
                extra++;
        }
        Assert.True(takes + misses > 20, $"takes {takes} misses {misses} of {n}");
        Assert.True(outs > 8, $"outs {outs} of {n}");
        Assert.True(bip > 15, $"BIP {bip} of {n}");
        Assert.True(extra < n, $"extra-base {extra} of {n} — not 200 extra-base");
        Assert.True(extra < bip || bip == 0, $"extra-base {extra} vs BIP {bip}");
        Assert.True(extra < n * 0.45, $"extra-base {extra} of {n} still a meatball rocket");
    }

    [Fact]
    public void CpuVsHumanChargedAndStarStillHurt()
    {
        var meat = ExtraBase(Meatball, 80);
        var charged = ExtraBase(Charged, 80);
        var star = ExtraBase(StarPitch, 80);
        Assert.True(charged > meat, $"charged extra-base {charged} vs meatball {meat}");
        Assert.True(charged + star > 8, $"charged {charged} star {star} produced no extra-base");
    }

    [Fact]
    public void AutoPlayArcadeStillSwingsInTheZone()
    {
        var swings = 0;
        var extra = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", seed: seed);
            var inZone = AtBatResolver.PitchInZone(Meatball, match.Pitcher.Stats.Pitch);
            var swing = match.CpuSwing(Meatball, inZone);
            if (swing.Swing) swings++;
            var ev = match.Play(Meatball, swing);
            if (ev.Kind is PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)
                extra++;
        }
        Assert.True(swings >= 35, $"arcade swings {swings}");
        Assert.True(extra > 0, "arcade meatballs can still extra-base");
    }

    int ExtraBase(PitchCommand pitch, int n)
    {
        var extra = 0;
        for (var seed = 1; seed <= n; seed++)
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", seed: seed);
            var inZone = AtBatResolver.PitchInZone(pitch, match.Pitcher.Stats.Pitch);
            var swing = match.CpuSwing(pitch, inZone, vsHumanPitcher: true);
            var ev = match.Play(pitch, swing);
            if (ev.Kind is PlayKind.Double or PlayKind.Triple or PlayKind.HomeRun)
                extra++;
        }
        return extra;
    }
}
