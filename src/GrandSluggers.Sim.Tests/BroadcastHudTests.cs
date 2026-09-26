using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

public class BroadcastHudTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ContactHandsOffToLiveInformationAndNextPlateReturnsWithoutDelay(int seats)
    {
        // SET -> pitch -> contact/freeze -> flight -> possession/throw -> dead ball -> next SET.
        // Repeated frames and lingering VFX cannot add a blank frame or a reappearance timer.
        var phases = new[] { false, false, true, true, true, false, false };
        var expected = new[] { BroadcastHud.PlayMode.Plate, BroadcastHud.PlayMode.Plate,
            BroadcastHud.PlayMode.InPlay, BroadcastHud.PlayMode.InPlay, BroadcastHud.PlayMode.InPlay,
            BroadcastHud.PlayMode.Plate, BroadcastHud.PlayMode.Plate };
        Assert.Equal(expected, phases.Select(live => BroadcastHud.Mode(live)));
        Assert.Equal(BroadcastHud.Layout(1), BroadcastHud.Layout(seats));
        foreach (var live in phases)
        {
            Assert.Equal(BroadcastHud.PlayMode.Hidden, BroadcastHud.Mode(live, forceMute: true));
            Assert.NotEqual(BroadcastHud.PlayMode.Hidden, BroadcastHud.Mode(live));
        }
    }

    [Theory]
    [InlineData(1024, 768)]
    [InlineData(1280, 800)]
    [InlineData(1920, 1080)]
    public void LiveDiamondAndOutsRemainReadableTogetherClearOfCoaching(int width, int height)
    {
        Assert.True(BroadcastHud.InFrame(BroadcastHud.LivePanel, width, height));
        Assert.True(BroadcastHud.Contains(BroadcastHud.LivePanel, BroadcastHud.LiveDiamond));
        Assert.True(BroadcastHud.Contains(BroadcastHud.LivePanel, BroadcastHud.LiveOuts));
        Assert.True(BroadcastHud.LiveDiamond.Bottom <= BroadcastHud.LiveOuts.Y);
        Assert.True(BroadcastHud.TutorialCoach.Right < BroadcastHud.LivePanel.X);
        Assert.Equal(BroadcastHud.Standard.Score.Right, BroadcastHud.LivePanel.Right, 8);
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
        Assert.Equal(0, bug.SelectedBag);
        Assert.Equal(match.Pitcher.Name, bug.Pitcher);
        Assert.Equal(match.Batter.Name, bug.Batter);
        Assert.False(string.IsNullOrWhiteSpace(bug.Next));
        Assert.NotEqual(bug.Batter, bug.Next);
        Assert.Equal(match.Away.Name, bug.AwayName);
        Assert.Equal(match.Home.Name, bug.HomeName);
        Assert.InRange(bug.OffenseStars, 0, 5);
        Assert.InRange(bug.DefenseStars, 0, 5);
        Assert.Equal(BroadcastHud.PlayMode.Plate, BroadcastHud.Mode(false));
        Assert.Throws<ArgumentNullException>(() => BroadcastHud.From(null!));
        Assert.Equal(match.Innings, bug.Innings);
        Assert.Equal(3, bug.Innings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StarMetersFollowTheMatchSettingEvenWithEmptyPools(bool stars)
    {
        foreach (var bottom in new[] { false, true })
        {
            var match = Match.Exhibition(_content, "rio", "ashlord", stars: stars);
            if (bottom) match.SkipToHomeHalf();
            if (match.Pitcher.Id != match.Defense.Captain.Id)
                Assert.True(match.SwapPitcher(match.Defense.Captain));
            Assert.Equal(stars, BroadcastHud.From(match).StarsEnabled);
            // Two of the captain's own Star Pitches spend a pool of exactly twice the price without completing a plate appearance.
            match.GiveDefenseStars(2 * match.PitchStarCost);
            for (var i = 0; i < 2; i++)
                match.Play(Scenario.PitchAt(2.5, StrikeZoneGeometry.Reference.CenterY) with { Star = true }, Scenario.Take);
            var bug = BroadcastHud.From(match);
            Assert.Equal(0, bug.DefenseStars);
            Assert.Equal(stars, bug.StarsEnabled);
        }
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
        foreach (var anchor in Enum.GetValues<StampAnchor>())
        {
            var stamp = BroadcastHud.Stamp(anchor);
            Assert.True(BroadcastHud.InFrame(stamp, screenW, screenH), $"{anchor} at {screenW}x{screenH}");
            Assert.False(BroadcastHud.IsScreenCenterCard(stamp), $"{anchor} must not cover the camera");
            Assert.False(BroadcastHud.Contains(one.Score, stamp), $"{anchor} is not the scorebug");
        }
        Assert.Equal(BroadcastHud.StampGlove, BroadcastHud.Stamp(StampAnchor.Glove));
        Assert.Equal(BroadcastHud.StampBag, BroadcastHud.Stamp(StampAnchor.Bag));
        Assert.Equal(BroadcastHud.StampPlate, BroadcastHud.Stamp(StampAnchor.Plate));
        Assert.Equal(BroadcastHud.StampDirt, BroadcastHud.Stamp(StampAnchor.Dirt));
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
        Assert.Equal("STRIKE", PlayNarrator.Headline(PlayKind.TakeStrike));
        Assert.Equal("BALL", PlayNarrator.Headline(PlayKind.TakeBall));
        Assert.Equal("OUT", PlayNarrator.Headline(PlayKind.GroundOut));
        Assert.Equal("STRIKE OUT", PlayNarrator.Headline(PlayKind.Strikeout));
        Assert.Equal("HIT BY PITCH", PlayNarrator.Headline(PlayKind.HitByPitch));
        Assert.DoesNotContain("TAKESTRIKE", PlayNarrator.Headline(PlayKind.TakeStrike));
        Assert.Equal("STEAL", BroadcastHud.BatterExtra(false, true, true, false, ""));
        Assert.Contains("L3 STEAL", BroadcastHud.BatterExtra(false, false, true, false, ""));
        Assert.Contains("BUNT", BroadcastHud.BatterExtra(false, false, false, true, ""));
        // §5.8, PH-14-R3: the card names the held side, the same public fact the bat angle carries.
        Assert.Equal("BUNT 3B", BroadcastHud.BatterExtra(false, false, false, BuntSide.Third, ""));
        Assert.Equal("BUNT 1B  STEAL", BroadcastHud.BatterExtra(false, true, true, BuntSide.First, ""));
        Assert.Equal("", BroadcastHud.BatterExtra(false, false, false, BuntSide.None, ""));
    }

    [Fact]
    public void RunnerPipFractionsAreFeetAlongTheSegment_TheBatterRunnerAndTheOverrunIncluded()
    {
        // #606: a runner 45 ft from first toward second is half of 1 → 2. On the C80 copy's 80-ft path half is 40 ft.
        Assert.Equal((1, 2, 0.5), Baserunning.PathPip(1, 40, Diamond.Baseline, 0, Diamond.Baseline));
        // The batter-runner 30 ft out of the box on a 90-ft run is a third of home → first (26.67 ft of 80 on the copy).
        var batter = Baserunning.PathPip(0, (80.0 / 3.0), Diamond.Baseline, 0, Diamond.Baseline);
        Assert.Equal((0, 1), (batter.From, batter.To));
        Assert.Equal(1.0 / 3.0, batter.U, 9);
        // Through first on the run-through: past 1.0 on home → first.
        var overrun = Baserunning.PathPip(1, 0, Diamond.Baseline, 8, Diamond.Baseline);
        Assert.Equal((0, 1), (overrun.From, overrun.To));
        Assert.Equal(1.1, overrun.U, 9);
        // Seated is fraction 0 on the bag; home is the end of 3 → 4.
        Assert.Equal((2, 3, 0.0), Baserunning.PathPip(2, 0, Diamond.Baseline, 0, Diamond.Baseline));
        Assert.Equal((3, 4, 1.0), Baserunning.PathPip(4, 0, Diamond.Baseline, 0, Diamond.Baseline));
    }

    [Fact]
    public void MiniDiamondPipsRideEveryLiveRunner_BetweenBagsOnALiveBall_OnTheBagsWhenDead()
    {
        var scenario = new Scenario(_content, seed: 2).Runner(1, 1);
        var match = scenario.Match;
        var set = BroadcastHud.From(match);
        var seated = Assert.Single(set.Runners);
        Assert.Equal((1, 1, 2, 0.0), (seated.FromBag, seated.From, seated.To, seated.U));
        Assert.DoesNotContain(set.Runners, p => p.Batter);

        // S-40's hopper to short: the runner from first and the batter-runner both run.
        var hit = FlightFixtures.Hit(match.Park, 85, -12, -18);
        var preview = match.PreviewHit(hit);
        var field = match.ResolveFielding(hit, preview);
        var live = match.LivePlay;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu));
        var sawBetween = false;
        var sawBatterRunning = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var bug = BroadcastHud.From(match);
            var bodies = match.Runners.Where(r => r.Live).ToList();
            Assert.Equal(bodies.Count, bug.Runners.Count);
            foreach (var r in bodies)
            {
                var pip = Assert.Single(bug.Runners, p => p.Id == r.Who.Id && p.FromBag == r.FromBag);
                Assert.Equal(r.Pip, (pip.From, pip.To, pip.U));
                if (r.FromBag > 0 && !r.Overrunning)
                {
                    // The pip is the body's true place: the same fraction between the same two bags.
                    var a = Diamond.Bag(pip.From);
                    var b = Diamond.Bag(pip.To);
                    Assert.Equal(a.X + (b.X - a.X) * pip.U, r.Position.X, 6);
                    Assert.Equal(a.Z + (b.Z - a.Z) * pip.U, r.Position.Z, 6);
                    sawBetween |= pip.U is > 0.05 and < 0.95;
                }
                if (pip.Batter) sawBatterRunning |= pip.U > 0.1;
            }
            play = live.Apply(LivePlayCommand.Tick(1.0 / 60.0, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
        }
        Assert.NotNull(play);
        Assert.True(sawBetween, "the runner from first was drawn between first and second");
        Assert.True(sawBatterRunning, "the batter-runner was drawn on the way to first");

        // The ball is dead: whoever is left sits on a bag, and the pips agree with occupancy.
        var dead = BroadcastHud.From(match);
        Assert.All(dead.Runners, p => Assert.Equal(0.0, p.U));
        Assert.DoesNotContain(dead.Runners, p => p.Batter);
        Assert.Equal(dead.RunnerFirst, dead.Runners.Any(p => p.From == 1));
        Assert.Equal(dead.RunnerSecond, dead.Runners.Any(p => p.From == 2));
        Assert.Equal(dead.RunnerThird, dead.Runners.Any(p => p.From == 3));
    }

    [Fact]
    public void MiniDiamondCarriesOccupancyAndTheSelectedRunner()
    {
        // D1: no lead pips. The pip is the bag a live runner last touched, and the selected one is marked.
        var match = Match.Slice(_content, seed: 1);
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        var bug = BroadcastHud.From(match);
        Assert.True(bug.RunnerFirst);
        Assert.False(bug.RunnerSecond);
        Assert.False(bug.RunnerThird);
        Assert.Equal(1, bug.SelectedBag);
        var selected = Assert.Single(bug.Runners, p => p.FromBag == bug.SelectedBag);
        Assert.Equal((1, 2, 0.0), (selected.From, selected.To, selected.U));
        Assert.True(match.Occupied(1));
        Assert.False(match.Occupied(2));
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
        Assert.Contains("JUMP", BroadcastHud.ControlDisplay(true, "CF", "Rio", jump: true));
        Assert.Contains("DIVE", BroadcastHud.ControlDisplay(true, "SS", "Nico", dive: true));
        Assert.Equal("R  →  CF", BroadcastHud.SwitchTell("SS", "CF", "", false));
        Assert.Equal("R  →  CF  ·  Rio", BroadcastHud.SwitchTell("SS", "CF", "Rio", false));
        Assert.Equal("", BroadcastHud.SwitchTell("CF", "CF", "Rio", false));
        Assert.Equal("", BroadcastHud.SwitchTell("SS", "CF", "Rio", true));
        Assert.Equal("", BroadcastHud.ItemPointer(false, "Vale"));
        Assert.Equal("", BroadcastHud.ItemPointer(true, ""));
        Assert.Equal("ITEM  →  Vale", BroadcastHud.ItemPointer(true, "Vale"));
    }

    /// <summary>
    /// The ARM bar's color grows with the fade (§4.7, #1012): fresh at fadeFrom or more, warming step by step toward amber as the
    /// pool falls to tiredBelow, and red at the same pool the TIRED word appears. Never a single switch across the whole fade.
    /// </summary>
    [Fact]
    public void TheArmBarWarmsWithTheFadeAndTurnsRedAtTired()
    {
        var rules = _content.Rules;
        var s = rules.Pitching.Stamina;
        Assert.Equal(BroadcastHud.ArmFresh, BroadcastHud.ArmColor(s.FadeFrom, rules));
        Assert.Equal(BroadcastHud.ArmFresh, BroadcastHud.ArmColor(s.FadeFrom + 20, rules));
        var prev = BroadcastHud.ArmColor(s.FadeFrom, rules);
        var distinct = 1;
        for (var pool = s.FadeFrom - 1; pool >= s.TiredBelow; pool--)
        {
            var c = BroadcastHud.ArmColor(pool, rules);
            Assert.True(c.G <= prev.G && c.B <= prev.B, $"the bar warms as the pool falls ({pool})");
            if (c != prev) distinct++;
            Assert.DoesNotContain("TIRED", BroadcastHud.ArmLine(pool, rules));
            prev = c;
        }
        Assert.Equal(BroadcastHud.ArmFading, BroadcastHud.ArmColor(s.TiredBelow, rules));
        Assert.True(distinct >= s.FadeFrom - s.TiredBelow, "a tone per pool across the fade, not a switch");
        foreach (var pool in new[] { s.TiredBelow - 1, 0, -3 })
        {
            Assert.Equal(BroadcastHud.ArmTired, BroadcastHud.ArmColor(pool, rules));
            Assert.Contains("TIRED", BroadcastHud.ArmLine(pool, rules));
        }
    }

    /// <summary>The ramp is the table's: a longer fade and a later TIRED move the colors with them.</summary>
    [Fact]
    public void TheArmBarReadsTheTablesFade()
    {
        var shipped = _content.Rules;
        var longer = shipped with
        {
            Pitching = shipped.Pitching with
            {
                Stamina = shipped.Pitching.Stamina with { FadeFrom = 80, TiredBelow = 40 }
            }
        };
        Assert.Equal(BroadcastHud.ArmFresh, BroadcastHud.ArmColor(50, shipped));
        Assert.NotEqual(BroadcastHud.ArmFresh, BroadcastHud.ArmColor(50, longer));
        Assert.Equal(BroadcastHud.ArmTired, BroadcastHud.ArmColor(39, longer));
        Assert.Equal(BroadcastHud.ArmFading, BroadcastHud.ArmColor(40, longer));
    }
}
