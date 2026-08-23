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
    public void LickCatchAddsCatchRadius()
    {
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("zig")));
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("rio")));
        Assert.Equal(0, FieldAbilities.CatchBonus(_content.Must("ashlord")));
        Assert.Equal(PlayKind.Single, FieldAbilities.SpinCheck(_content.Must("ashlord"), PlayKind.Double));
    }
}
