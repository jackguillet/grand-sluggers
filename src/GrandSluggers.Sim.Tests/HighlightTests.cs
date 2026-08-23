using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class HighlightTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void EmptyLogHasNoClip()
    {
        Assert.Null(Highlight.Pick([]));
    }

    [Fact]
    public void BallsAndStrikesAreNotHighlights()
    {
        var log = new[]
        {
            Ev(PlayKind.TakeBall, "Ball 1."),
            Ev(PlayKind.TakeStrike, "Strike 1 looking."),
            Ev(PlayKind.SwingMiss, "Strike 2."),
            Ev(PlayKind.Foul, "Foul.")
        };
        Assert.Null(Highlight.Pick(log));
    }

    [Fact]
    public void HomeRunBeatsSingle()
    {
        var single = Ev(PlayKind.Single, "singles.");
        var hr = Ev(PlayKind.HomeRun, "goes deep.");
        Assert.Equal(PlayKind.HomeRun, Highlight.Pick([single, hr])!.Play.Kind);
        Assert.Equal(PlayKind.HomeRun, Highlight.Pick([hr, single])!.Play.Kind);
        Assert.Equal(HighlightBeat.HomeRun, Highlight.Pick([single, hr])!.Beat);
    }

    [Fact]
    public void DoubleBeatsSingle()
    {
        var single = Ev(PlayKind.Single, "singles.");
        var extra = Ev(PlayKind.Double, "doubles.");
        Assert.Equal(PlayKind.Double, Highlight.Pick([single, extra])!.Play.Kind);
        Assert.Equal(HighlightBeat.ExtraBase, Highlight.Pick([single, extra])!.Beat);
    }

    [Fact]
    public void BuddyJumpPreferredOverHomeRun()
    {
        var hr = Ev(PlayKind.HomeRun, "goes deep.");
        var buddy = Ev(PlayKind.FlyOut, "Nico + Gull BUDDY JUMP!");
        var pick = Highlight.Pick([hr, buddy]);
        Assert.NotNull(pick);
        Assert.Equal(HighlightBeat.BuddyJump, pick.Beat);
        Assert.Equal(PlayKind.FlyOut, pick.Play.Kind);
        Assert.Equal(HighlightBeat.BuddyJump, Highlight.Pick([buddy, hr])!.Beat);
    }

    [Fact]
    public void RobbedHomerBeatsHomeRun()
    {
        var hr = Ev(PlayKind.HomeRun, "goes deep.");
        var jump = Ev(PlayKind.FlyOut, "Nico SUPER JUMP!");
        Assert.Equal(HighlightBeat.RobbedHomer, Highlight.Pick([hr, jump])!.Beat);
        var climb = Ev(PlayKind.FlyOut, "Konga CLAMBERS the wall!");
        Assert.Equal(HighlightBeat.RobbedHomer, Highlight.Pick([hr, climb])!.Beat);
    }

    [Fact]
    public void BuddyJumpPreferredOverRobbedHomer()
    {
        var rob = Ev(PlayKind.FlyOut, "Nico SUPER JUMP!");
        var buddy = Ev(PlayKind.FlyOut, "Nico + Gull BUDDY JUMP!");
        Assert.Equal(HighlightBeat.BuddyJump, Highlight.Pick([rob, buddy])!.Beat);
    }

    [Fact]
    public void StarKBeatsSingle()
    {
        var single = Ev(PlayKind.Single, "singles.");
        var k = Ev(PlayKind.Strikeout, "goes down swinging.", starPitch: true);
        var pick = Highlight.Pick([single, k]);
        Assert.NotNull(pick);
        Assert.Equal(HighlightBeat.StarK, pick.Beat);
        Assert.Equal(PlayKind.Strikeout, pick.Play.Kind);
    }

    [Fact]
    public void PlainStrikeoutIsNotAClip()
    {
        var k = Ev(PlayKind.Strikeout, "is caught looking.");
        Assert.Null(Highlight.Pick([k]));
    }

    [Fact]
    public void HomeRunBeatsStarK()
    {
        var k = Ev(PlayKind.Strikeout, "goes down swinging.", starPitch: true);
        var hr = Ev(PlayKind.HomeRun, "goes deep.");
        Assert.Equal(HighlightBeat.HomeRun, Highlight.Pick([k, hr])!.Beat);
    }

    [Fact]
    public void TieBreaksToTheLaterPlay()
    {
        var a = Ev(PlayKind.HomeRun, "first.");
        var b = Ev(PlayKind.HomeRun, "second.");
        Assert.Equal("second.", Highlight.Pick([a, b])!.Play.Caption);
    }

    PlayEvent Ev(PlayKind kind, string caption, bool starPitch = false)
    {
        var rio = _content.Must("rio");
        var ash = _content.Must("ashlord");
        var inPlay = kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple
            or PlayKind.HomeRun or PlayKind.FlyOut or PlayKind.GroundOut;
        var hr = kind == PlayKind.HomeRun;
        var hit = new AtBatResult(
            hr ? ContactQuality.Perfect : ContactQuality.Solid,
            inPlay,
            kind == PlayKind.Strikeout,
            hr ? 105 : 88,
            28,
            hr ? 380 : 220,
            hr,
            false,
            starPitch ? "heatball" : null,
            null);
        var pitch = new PitchCommand("fastball", 0, 0, starPitch);
        var swing = new SwingCommand(kind != PlayKind.TakeBall && kind != PlayKind.TakeStrike, 0, 0, false);
        return new PlayEvent(
            kind, hit, pitch, swing, rio, ash, null, null, 0, [], caption,
            false, false, 0, 0, 0, 0, 0, 0);
    }
}
