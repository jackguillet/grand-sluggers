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
        Assert.True(match.TakeLead(0.5));
        Assert.InRange(match.RunnerAt(2)!.Lead01, 0.49, 0.51);
        Assert.Equal(0, match.RunnerAt(1)!.Lead01);
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
    public void LickCatchAddsCatchRadius()
    {
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("zig")));
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("rio")));
        Assert.Equal(0, FieldAbilities.CatchBonus(_content.Must("ashlord")));
        Assert.Equal(PlayKind.Single, FieldAbilities.SpinCheck(_content.Must("ashlord"), PlayKind.Double));
    }
}
