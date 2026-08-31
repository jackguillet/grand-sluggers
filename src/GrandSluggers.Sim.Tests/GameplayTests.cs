using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class GameplayTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void CharmballShrinksTheWindow()
    {
        var park = _content.Parks["harbor-diamond"];
        var vale = _content.Must("vale");
        var rio = _content.Must("rio");
        var bat = _content.Bats["harbor-lumber"];
        var resolver = new AtBatResolver(_content.Chemistry);
        var hits = 0;
        var charmed = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var input = new AtBatInput(vale, rio, _content.Must("nico"), [], "fastball", false, false, 7.0, false, false, bat, 80, PitchInZone: true);
            var star = input with { UseStarPitch = true };
            if (resolver.Resolve(input, park, new Random(seed)).InPlay) hits++;
            if (resolver.Resolve(star, park, new Random(seed)).InPlay) charmed++;
        }
        Assert.True(charmed < hits, $"charm {charmed} vs plain {hits}");
    }

    [Fact]
    public void RolePlayerStarSwingIsAGrounder()
    {
        var park = _content.Parks["harbor-diamond"];
        var input = new AtBatInput(
            _content.Must("vale"), _content.Must("dart"), _content.Must("zig"), [],
            "fastball", false, true, 0, false, true,
            _content.Bats["harbor-lumber"], 80, PitchInZone: true);
        var r = new AtBatResolver(_content.Chemistry).Resolve(input, park, new Random(1));
        Assert.Equal("ground", r.StarSwingUsed);
        Assert.True(r.LaunchDeg < 14, $"launch {r.LaunchDeg}");
    }

    [Fact]
    public void GuestCaptainStarCostsTwo()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.Equal(1, match.PitchStarCost);
        Assert.True(match.SwapPitcher());
        Assert.Equal("vale", match.Pitcher.Id);
        Assert.Equal(2, match.PitchStarCost);
    }

    [Fact]
    public void LaserThrowIsFasterThanABuddyThrow()
    {
        var boom = _content.Must("boom");
        var brondo = _content.Must("brondo");
        var nico = _content.Must("nico");
        var rio = _content.Must("rio");
        var laser = FieldAbilities.ApplyThrow(boom, _content.Chemistry.FieldingThrow(boom, brondo, new Random(1)));
        var buddy = _content.Chemistry.FieldingThrow(rio, nico, new Random(1));
        Assert.True(laser.SpeedMul > buddy.SpeedMul, $"laser {laser.SpeedMul} vs buddy {buddy.SpeedMul}");
    }

    [Fact]
    public void SuperJumpHasAirRobRange()
    {
        var park = _content.Parks["harbor-diamond"];
        var nico = _content.Must("nico");
        var fence = AtBatResolver.FenceAt(park, 0);
        var hit = new AtBatResult(ContactQuality.Perfect, true, false, 100, 28, fence + 10, true, false, null, null);
        Assert.True(FieldAbilities.AirRob(park, nico, hit));
        Assert.False(FieldAbilities.AirRob(park, _content.Must("rio"), hit));
    }

    [Fact]
    public void BananaTurnsAnOutIntoASingle()
    {
        var field = new FieldingResult(PlayKind.FlyOut, _content.Must("frost"), null, 2, 0, 80, false, false);
        var after = ErrorItems.Apply(field, "banana", new Random(1));
        Assert.Equal(PlayKind.Single, after.Kind);
        Assert.Equal("banana", after.Item);
    }

    [Fact]
    public void ThrowItemBananaOnAnOutBecomesASingle()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielder = _content.Must("frost");
        var field = new FieldingResult(PlayKind.FlyOut, fielder, null, 2, 0, 80, false, false);
        var after = match.ThrowItem(field, "banana", fielder);
        Assert.Equal(PlayKind.Single, after.Kind);
        Assert.Equal("banana", after.Item);
        var miss = match.ThrowItem(field, "banana", _content.Must("rio"));
        Assert.Equal(PlayKind.FlyOut, miss.Kind);
    }

    [Fact]
    public void ThrowItemRocketTargetsABody()
    {
        var body = _content.Must("frost");
        var other = _content.Must("rio");
        var field = new FieldingResult(PlayKind.FlyOut, body, null, 2, 0, 80, false, false);
        var miss = Match.Slice(_content, seed: 1).ThrowItem(field, "rocket", other);
        Assert.Equal(PlayKind.FlyOut, miss.Kind);
        Assert.Equal("rocket", miss.Item);

        var hits = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var after = Match.Slice(_content, seed: seed).ThrowItem(field, "rocket", body);
            Assert.Equal("rocket", after.Item);
            if (after.Kind == PlayKind.Single) hits++;
        }
        Assert.True(hits is > 0 and < 40, $"rocket body hits {hits}");
    }

    [Fact]
    public void ThrowItemPowOnAGrounderBecomesASingle()
    {
        var match = Match.Slice(_content, seed: 1);
        var infielder = _content.Must("frost");
        var ground = new FieldingResult(PlayKind.GroundOut, infielder, null, 1, 10, 40, false, false);
        var after = match.ThrowItem(ground, "pow", infielder);
        Assert.Equal(PlayKind.Single, after.Kind);
        Assert.Equal("pow", after.Item);
        var fly = new FieldingResult(PlayKind.FlyOut, infielder, null, 2, 0, 80, false, false);
        var no = match.ThrowItem(fly, "pow", infielder);
        Assert.Equal(PlayKind.FlyOut, no.Kind);
    }

    [Fact]
    public void ThrowItemWithNoItemDoesNothing()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielder = _content.Must("frost");
        var field = new FieldingResult(PlayKind.FlyOut, fielder, null, 2, 0, 80, false, false);
        var empty = match.ThrowItem(field, "", fielder);
        Assert.Equal(PlayKind.FlyOut, empty.Kind);
        Assert.Null(empty.Item);
        var banned = match.ThrowItem(field, "smoke", fielder);
        Assert.Equal(PlayKind.FlyOut, banned.Kind);
        Assert.Null(banned.Item);
        Assert.False(ErrorItems.Known("ghost"));
        Assert.False(ErrorItems.Known("paint"));
    }

    [Fact]
    public void StealCanTakeSecond()
    {
        var match = Match.Slice(_content, seed: 2);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        match.Play(paint, take);
        match.Play(paint, take);
        match.Play(paint, take);
        Assert.Equal(PlayKind.Strikeout, match.Log[^1].Kind);

        var walk = new PitchCommand("fastball", 0, 40, false);
        for (var i = 0; i < 4; i++)
            match.Play(walk, take);
        Assert.NotNull(match.First);
        Assert.True(match.CanSteal);
        Assert.True(match.ToggleSteal());
        PlayKind last = PlayKind.TakeBall;
        for (var i = 0; i < 8 && match.First is not null; i++)
            last = match.Play(walk, take).Kind;
        Assert.True(last is PlayKind.StolenBase or PlayKind.CaughtStealing or PlayKind.Walk or PlayKind.TakeBall,
            last.ToString());
    }

    [Fact]
    public void ForcedStealSucceedsForABurner()
    {
        var match = Match.Slice(_content, seed: 9);
        var dart = _content.Must("dart");
        // Walk dart on: put a fast runner on first via four balls, then steal with a take.
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
        match.ToggleSteal();
        var won = false;
        for (var i = 0; i < 12 && !match.Over; i++)
        {
            var ev = match.Play(new PitchCommand("fastball", 0, 8, false), take);
            if (ev.Kind == PlayKind.StolenBase)
            {
                won = true;
                break;
            }
            if (match.CanSteal && !match.StealOn)
                match.ToggleSteal();
        }
        Assert.True(won || match.Log.Any(e => e.Kind == PlayKind.StolenBase || e.Kind == PlayKind.CaughtStealing));
    }

    [Fact]
    public void LeadReturnStealSlideOnTheLeadRunner()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.False(match.TakeLead());
        Assert.False(match.ReturnToBag());
        Assert.False(match.StartSteal());
        Assert.False(match.Slide());
        WalkOn(match);
        Assert.NotNull(match.LeadRunner);
        Assert.Equal(1, match.LeadBag);
        Assert.Equal(0, match.Lead01);
        Assert.True(match.TakeLead(0.4));
        Assert.InRange(match.Lead01, 0.39, 0.41);
        Assert.False(match.Returning);
        Assert.True(match.ReturnToBag(0.15));
        Assert.True(match.Returning);
        Assert.InRange(match.Lead01, 0.24, 0.26);
        Assert.False(match.StealAttempt);
        Assert.True(match.StartSteal());
        Assert.True(match.StealAttempt);
        Assert.True(match.StealOn);
        Assert.False(match.Returning);
        Assert.True(match.Lead01 >= 0.2);
        Assert.True(match.Slide());
        Assert.True(match.Sliding);
    }

    [Fact]
    public void LeadRunnerIsFurthestAlong()
    {
        var match = Match.Slice(_content, seed: 1);
        WalkOn(match);
        WalkOnSecond(match);
        Assert.NotNull(match.First);
        Assert.NotNull(match.Second);
        Assert.Equal(2, match.LeadBag);
        Assert.Equal(match.Second!.Id, match.LeadRunner!.Id);
        Assert.Equal(2, match.SelectedBag);
        Assert.True(match.TakeLead(0.5));
        Assert.InRange(match.RunnerAt(2)!.Lead01, 0.49, 0.51);
        Assert.Equal(0, match.RunnerAt(1)!.Lead01);
    }

    [Fact]
    public void SelectRunnerOnFirstVsSecondIndependently()
    {
        var match = Match.Slice(_content, seed: 1);
        WalkOn(match);
        WalkOnSecond(match);
        Assert.Equal(2, match.SelectedBag);
        Assert.False(match.SelectRunner(4));
        Assert.False(match.SelectRunner(0));
        Assert.Equal(2, match.SelectedBag);
        Assert.True(match.SelectRunner(1));
        Assert.Equal(1, match.SelectedBag);
        Assert.Equal(match.First!.Id, match.SelectedRunner!.Id);
        Assert.True(match.TakeLead(0.5));
        Assert.InRange(match.RunnerAt(1)!.Lead01, 0.49, 0.51);
        Assert.Equal(0, match.RunnerAt(2)!.Lead01);
        Assert.True(match.SelectRunner(2));
        Assert.True(match.TakeLead(0.4));
        Assert.InRange(match.RunnerAt(2)!.Lead01, 0.39, 0.41);
        Assert.InRange(match.RunnerAt(1)!.Lead01, 0.49, 0.51);
        Assert.True(match.SelectRunner(1));
        Assert.False(match.CanSteal, "second occupied, runner on first cannot steal");
        Assert.False(match.StartSteal());
        Assert.True(match.SelectRunner(2));
        Assert.True(match.CanSteal, "selected second can steal third");
        Assert.True(match.StartSteal());
        Assert.Equal(2, match.ArmedStealBag);
        Assert.Equal(3, match.StealTargetBag);
        Assert.True(match.RunnerAt(2)!.StealAttempt);
        Assert.False(match.RunnerAt(1)!.StealAttempt);
    }

    [Fact]
    public void StealHomeIsRejected()
    {
        var match = Match.Slice(_content, seed: 1);
        WalkOn(match);
        WalkOnSecond(match);
        WalkOnThird(match);
        Assert.NotNull(match.Third);
        Assert.Equal(3, match.LeadBag);
        Assert.True(match.SelectRunner(3));
        Assert.False(match.CanSteal);
        Assert.False(match.StartSteal());
        Assert.False(match.StealOn);
        Assert.Equal(0, match.StealTargetBag);
        Assert.Equal(0, Baserunning.StealTarget(3));
        Assert.False(match.SelectRunner(4));
    }

    [Fact]
    public void WalksAndStrikeoutsCancelASteal()
    {
        var walkMatch = Match.Slice(_content, seed: 1);
        WalkOn(walkMatch);
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (walkMatch.Balls < 3 && !walkMatch.Over)
            walkMatch.Play(wild, take);
        Assert.True(walkMatch.SelectRunner(1));
        Assert.True(walkMatch.StartSteal());
        Assert.True(walkMatch.StealOn);
        Assert.Equal(2, walkMatch.StealTargetBag);
        var walked = walkMatch.Play(wild, take);
        Assert.Equal(PlayKind.Walk, walked.Kind);
        Assert.False(walkMatch.StealOn);
        Assert.Equal(0, walkMatch.ArmedStealBag);

        var kMatch = Match.Slice(_content, seed: 2);
        WalkOn(kMatch);
        var paint = new PitchCommand("fastball", 0, 0, false);
        while (kMatch.Strikes < 2 && !kMatch.Over)
            kMatch.Play(paint, take);
        Assert.True(kMatch.StartSteal());
        var punched = kMatch.Play(paint, take);
        Assert.Equal(PlayKind.Strikeout, punched.Kind);
        Assert.False(kMatch.StealOn);
        Assert.Equal(0, kMatch.ArmedStealBag);
    }

    [Fact]
    public void BiggerLeadStealsMoreOften()
    {
        Assert.True(StealWins(1.0) > StealWins(0), "more lead should steal more");
    }

    [Fact]
    public void BigLeadCanBePickedOff()
    {
        var picks = 0;
        var stays = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var match = Match.Slice(_content, seed: seed);
            WalkOn(match);
            match.TakeLead(1);
            var wild = new PitchCommand("fastball", 0, 40, false);
            var take = new SwingCommand(false, 0, 0, false);
            var ev = match.Play(wild, take);
            if (ev.Kind == PlayKind.CaughtStealing && ev.Caption.Contains("picked off"))
                picks++;
            else
                stays++;
        }
        Assert.True(picks > 0, "max lead should risk a pickoff");
        Assert.True(stays > 0, "pickoff is a risk, not a sure out");
    }

    [Fact]
    public void NoLeadIsNeverPickedOff()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var match = Match.Slice(_content, seed: seed);
            WalkOn(match);
            Assert.Equal(0, match.Lead01);
            var wild = new PitchCommand("fastball", 0, 40, false);
            var take = new SwingCommand(false, 0, 0, false);
            var ev = match.Play(wild, take);
            Assert.NotEqual(PlayKind.CaughtStealing, ev.Kind);
            Assert.DoesNotContain("picked off", ev.Caption);
        }
    }

    [Fact]
    public void LeadSpotSitsOffTheBag()
    {
        var glued = Diamond.LeadSpot(1, 0);
        var walked = Diamond.LeadSpot(1, 1);
        Assert.Equal(Diamond.First.X, glued.X, 3);
        Assert.Equal(Diamond.First.Z, glued.Z, 3);
        Assert.True(Diamond.Dist(glued.X, glued.Z, walked.X, walked.Z) > 20);
        Assert.True(Diamond.Dist(walked.X, walked.Z, Diamond.Second.X, Diamond.Second.Z) <
                    Diamond.Dist(glued.X, glued.Z, Diamond.Second.X, Diamond.Second.Z));
    }

    int StealWins(double lead)
    {
        var n = 0;
        for (var seed = 1; seed <= 36; seed++)
        {
            var match = Match.Slice(_content, seed: seed);
            WalkOn(match);
            if (lead > 0) match.TakeLead(lead);
            match.StartSteal();
            var wild = new PitchCommand("fastball", 0, 8, false);
            var take = new SwingCommand(false, 0, 0, false);
            for (var i = 0; i < 6 && !match.Over; i++)
            {
                var ev = match.Play(wild, take);
                if (ev.Kind == PlayKind.StolenBase)
                {
                    n++;
                    break;
                }
                if (ev.Kind == PlayKind.CaughtStealing) break;
                if (match.CanSteal && !match.StealOn) match.StartSteal();
            }
        }
        return n;
    }

    static void WalkOn(Match match)
    {
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
    }

    static void WalkOnSecond(Match match)
    {
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.Second is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.Second);
    }

    [Fact]
    public void FairContactPutsTheBatterOnFirst()
    {
        var match = Match.Slice(_content, seed: 1);
        var paint = new PitchCommand("fastball", 0, 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        var who = match.Batter;
        Assert.True(match.BeginAtBat(paint, swing, out var hit, out _));
        Assert.True(InPlay.FairContactSendsBatter(hit));
        var field = new FieldingResult(PlayKind.Single, match.Pitcher, null, 0.8, 20, 40, false, false);
        var ev = match.FinishAtBat(paint, swing, hit with { InPlay = true, Foul = false }, field);
        Assert.Equal(PlayKind.Single, ev.Kind);
        Assert.NotNull(match.First);
        Assert.Equal(who.Id, match.First.Id);
    }

    [Fact]
    public void AllAdvanceMovesFirstAndSecondAllReturnBringsThemBack()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.False(match.AdvanceAll());
        Assert.False(match.ReturnAll());
        Assert.False(match.FreezeRunners());
        WalkOn(match);
        WalkOnSecond(match);
        Assert.NotNull(match.First);
        Assert.NotNull(match.Second);
        Assert.Equal(0, match.RunnerAt(1)!.Lead01);
        Assert.Equal(0, match.RunnerAt(2)!.Lead01);
        Assert.True(match.AdvanceAll(0.4));
        Assert.True(match.SendAll);
        Assert.InRange(match.RunnerAt(1)!.Lead01, 0.39, 0.41);
        Assert.InRange(match.RunnerAt(2)!.Lead01, 0.39, 0.41);
        Assert.True(match.ReturnAll(0.4));
        Assert.False(match.SendAll);
        Assert.Equal(0, match.RunnerAt(1)!.Lead01);
        Assert.Equal(0, match.RunnerAt(2)!.Lead01);
        Assert.True(match.AdvanceAll(0.3));
        Assert.True(match.FreezeRunners());
        Assert.False(match.SendAll);
        Assert.False(match.StealOn);
        Assert.True(match.TakeLead(0.2));
        Assert.True(match.ToggleSteal());
        Assert.True(match.StealOn);
    }

    [Fact]
    public void FlyDefaultHoldsThirdUntilAllAdvance()
    {
        var hold = FlyWithThird(sendAll: false);
        Assert.False(hold.Scored, "default fly holds; no sac");
        var go = FlyWithThird(sendAll: true);
        Assert.True(go.Scored, "all-advance tags up after the catch");
    }

    (bool Scored, PlayKind Kind) FlyWithThird(bool sendAll)
    {
        for (var seed = 1; seed <= 24; seed++)
        {
            var match = Match.Slice(_content, seed: seed);
            WalkOn(match);
            WalkOnSecond(match);
            WalkOnThird(match);
            if (match.Third is null) continue;
            var thirdId = match.Third.Id;
            if (sendAll) match.AdvanceAll(0.3);
            var paint = new PitchCommand("fastball", 0, 0, false);
            var swing = new SwingCommand(true, 0, 0, false);
            if (!match.BeginAtBat(paint, swing, out var hit, out _))
                continue;
            var field = new FieldingResult(PlayKind.FlyOut, match.Pitcher, null, 2, 0, 280, false, false);
            var deep = hit with { InPlay = true, Foul = false, CarryFt = 280, LaunchDeg = 32 };
            var ev = match.FinishAtBat(paint, swing, deep, field);
            var scored = ev.RunsScored > 0 || ev.Caption.Contains("Sac fly");
            if (!sendAll)
                Assert.True(match.Third is null || match.Third.Id == thirdId || match.Outs >= 3);
            return (scored, ev.Kind);
        }
        return (false, PlayKind.FlyOut);
    }

    static void WalkOnThird(Match match)
    {
        var wild = new PitchCommand("fastball", 0, 40, false);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.Third is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.Third);
    }

    [Fact]
    public void LickCatchAddsCatchRadius()
    {
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("zig")));
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("rio")));
        Assert.Equal(0, FieldAbilities.CatchBonus(_content.Must("ashlord")));
        Assert.Equal(PlayKind.Single, FieldAbilities.SpinCheck(_content.Must("ashlord"), PlayKind.Double));
    }
}
