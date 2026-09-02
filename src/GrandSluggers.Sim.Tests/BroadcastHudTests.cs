using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class BroadcastHudTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void PlayHudMutesDuringSpectacleSmashAndFreeze()
    {
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        Assert.True(BroadcastHud.MutePlay(true, 0, 0));
        Assert.True(BroadcastHud.MutePlay(false, 0.55, 0));
        Assert.True(BroadcastHud.MutePlay(false, 0, 0.12));
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        foreach (var c in _content.Characters.Values.Where(c => c.Captain))
            Assert.True(BroadcastHud.MutePlay(StarSkills.SpectacleSeconds(c.StarPitch) > 0, 0, 0));
    }

    [Fact]
    public void ScorebugCoversInningScoreCountRunnersMatchupStarsAndNext()
    {
        var match = Match.Exhibition(_content, "vale", "brondo", seed: 7);
        var bug = BroadcastHud.From(match);
        Assert.Equal(1, bug.Inning);
        Assert.True(bug.Top);
        Assert.False(bug.Over);
        Assert.Equal(0, bug.AwayScore);
        Assert.Equal(0, bug.HomeScore);
        Assert.Equal(0, bug.Outs);
        Assert.Equal(0, bug.Balls);
        Assert.Equal(0, bug.Strikes);
        Assert.False(bug.RunnerFirst);
        Assert.False(bug.RunnerSecond);
        Assert.False(bug.RunnerThird);
        Assert.Equal(0, bug.LeadFirst);
        Assert.Equal(0, bug.LeadSecond);
        Assert.Equal(0, bug.LeadThird);
        Assert.Equal(0, bug.SelectedBag);
        Assert.Equal(match.Pitcher.Name, bug.Pitcher);
        Assert.Equal(match.Batter.Name, bug.Batter);
        Assert.False(string.IsNullOrWhiteSpace(bug.Next));
        Assert.NotEqual(bug.Batter, bug.Next);
        Assert.Equal(match.Away.Name, bug.AwayName);
        Assert.Equal(match.Home.Name, bug.HomeName);
        Assert.InRange(bug.OffenseStars, 0, 5);
        Assert.InRange(bug.DefenseStars, 0, 5);
        Assert.False(BroadcastHud.MutePlay(false, 0, 0));
        Assert.Throws<ArgumentNullException>(() => BroadcastHud.From(null!));
        Assert.Equal(match.Innings, bug.Innings);
        Assert.Equal(3, bug.Innings);
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void PlayHudRectsStayInFrameWithMargin(int screenW, int screenH)
    {
        var one = BroadcastHud.Layout(1);
        var two = BroadcastHud.Layout(2);
        Assert.Equal(one, two);
        Assert.Equal(BroadcastHud.Standard, one);
        foreach (var r in new[] { one.Score, one.Count, one.MiniDiamond, one.PitcherCard, one.BatterCard, one.Banner })
            Assert.True(BroadcastHud.InFrame(r, screenW, screenH), $"{r} at {screenW}x{screenH}");
    }

    [Fact]
    public void CountAndDiamondSitOnTheScorebugNotInTheSky()
    {
        var lay = BroadcastHud.Layout();
        Assert.True(BroadcastHud.OnScorebug(lay.Score, lay.Count));
        Assert.True(BroadcastHud.OnScorebug(lay.Score, lay.MiniDiamond));
        Assert.True(BroadcastHud.Contains(lay.Score, lay.Count));
        Assert.True(BroadcastHud.Contains(lay.Score, lay.MiniDiamond));
        Assert.False(BroadcastHud.Contains(lay.Score, lay.PitcherCard));
        Assert.True(lay.Count.Y > lay.Score.Y);
        Assert.True(lay.Count.Bottom <= lay.Score.Bottom + 1e-9);
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void NameAndRunsAreSeparateColumnsWithAGap(int screenW, int screenH)
    {
        var score = BroadcastHud.Layout().Score;
        Assert.Equal("ASHLORD", BroadcastHud.BugName("Ashlord"));
        Assert.Equal("SPARKS", BroadcastHud.BugName("Rio Sparks"));
        Assert.Equal("0", BroadcastHud.RunsLabel(0));
        Assert.Equal("ASHLORD0", BroadcastHud.BugName("Ashlord") + BroadcastHud.RunsLabel(0));
        Assert.NotEqual("ASHLORD0", BroadcastHud.BugName("Ashlord") + " " + BroadcastHud.RunsLabel(0));
        for (var row = 0; row < 2; row++)
        {
            var name = BroadcastHud.NameCol(score, row);
            var runs = BroadcastHud.RunsCol(score, row);
            Assert.True(BroadcastHud.Contains(score, name));
            Assert.True(BroadcastHud.Contains(score, runs));
            var (nx, _, nw, _) = name.Pixel(screenW, screenH);
            var (rx, _, _, _) = runs.Pixel(screenW, screenH);
            Assert.True(nx + nw + 8 <= rx, $"name/runs gap row {row} at {screenW}x{screenH}");
        }
    }

    [Theory]
    [InlineData(1280, 800, 3)]
    [InlineData(1280, 800, 9)]
    [InlineData(1920, 1080, 3)]
    [InlineData(1920, 1080, 9)]
    public void InningBoxesAreReadableNotMashedGlyphs(int screenW, int screenH, int innings)
    {
        var score = BroadcastHud.Layout().Score;
        Assert.True(BroadcastHud.InFrame(BroadcastHud.InningMark(score), screenW, screenH));
        for (var i = 1; i <= innings; i++)
        {
            var box = BroadcastHud.InningBox(score, i, innings);
            Assert.True(BroadcastHud.Contains(score, box), $"inning {i}");
            var (_, _, w, h) = box.Pixel(screenW, screenH);
            Assert.True(w >= 22, $"inning {i} width {w} at {screenW}x{screenH} innings={innings}");
            Assert.True(h >= 22, $"inning {i} height {h} at {screenW}x{screenH} innings={innings}");
            if (i > 1)
            {
                var prev = BroadcastHud.InningBox(score, i - 1, innings);
                Assert.True(prev.Right <= box.X + 1e-9, "inning boxes do not overlap");
            }
        }
    }

    [Fact]
    public void HeadlineTakeStrikeIsStrikeNotTakeStrikeGlued()
    {
        Assert.Equal("STRIKE", BroadcastHud.Headline(PlayKind.TakeStrike));
        Assert.Equal("BALL", BroadcastHud.Headline(PlayKind.TakeBall));
        Assert.Equal("GROUNDOUT", BroadcastHud.Headline(PlayKind.GroundOut));
        Assert.DoesNotContain("TAKESTRIKE", BroadcastHud.Headline(PlayKind.TakeStrike));
    }

    [Fact]
    public void MiniDiamondCarriesLeadsNotJustOccupancy()
    {
        var match = Match.Slice(_content, seed: 1);
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.True(match.TakeLead(0.6));
        var bug = BroadcastHud.From(match);
        Assert.True(bug.RunnerFirst);
        Assert.InRange(bug.LeadFirst, 0.59, 0.61);
        Assert.Equal(0, bug.LeadSecond);
        Assert.Equal(1, bug.SelectedBag);
        var glued = Baserunning.MiniLead(1, 0);
        var walked = Baserunning.MiniLead(1, bug.LeadFirst);
        Assert.True(walked.V > glued.V);
    }

    [Fact]
    public void GameRulesSpreadNamesArmControlAndItemPointer()
    {
        Assert.False(BroadcastHud.PoorArm(40));
        Assert.True(BroadcastHud.PoorArm(24));
        Assert.Equal("ARM  80", BroadcastHud.ArmLine(80));
        Assert.Contains("TIRED", BroadcastHud.ArmLine(12));
        Assert.Equal("", BroadcastHud.ControlDisplay(false, "RF", "Vale"));
        Assert.Equal("", BroadcastHud.ControlDisplay(true, "", "Vale"));
        Assert.Equal("YOU  RF", BroadcastHud.ControlDisplay(true, "RF", ""));
        Assert.Equal("YOU  RF  ·  Vale", BroadcastHud.ControlDisplay(true, "RF", "Vale"));
        Assert.Equal("R  →  CF", BroadcastHud.SwitchTell("SS", "CF", "", false));
        Assert.Equal("R  →  CF  ·  Rio", BroadcastHud.SwitchTell("SS", "CF", "Rio", false));
        Assert.Equal("", BroadcastHud.SwitchTell("CF", "CF", "Rio", false));
        Assert.Equal("", BroadcastHud.SwitchTell("SS", "CF", "Rio", true));
        Assert.Equal("", BroadcastHud.ItemPointer(false, "Vale"));
        Assert.Equal("", BroadcastHud.ItemPointer(true, ""));
        Assert.Equal("ITEM  →  Vale", BroadcastHud.ItemPointer(true, "Vale"));
    }
}
