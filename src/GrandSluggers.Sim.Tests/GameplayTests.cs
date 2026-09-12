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
        // A frame inside the plain window and outside the charmball window (star-skills batterWindowMul).
        var plain = AtBatResolver.ContactWindowFrames(rio.Stats.Bat, false, null, park, false);
        var charm = AtBatResolver.ContactWindowFrames(rio.Stats.Bat, false, vale.StarPitch, park, false);
        Assert.True(charm < plain, $"charm {charm} vs plain {plain}");
        var edge = (plain + charm) / 4;
        for (var seed = 0; seed < 40; seed++)
        {
            var input = new AtBatInput(vale, rio, _content.Must("nico"), [], false, false, edge, false, false, bat, 80, PitchInZone: true);
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
            false, false, 0, false, true,
            _content.Bats["harbor-lumber"], 80, PitchInZone: true, Charge01: 1);
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
        var hit = FlightFixtures.OverTheFence(park, 10, 0);
        Assert.True(FieldAbilities.AirRob(park, nico, hit));
        Assert.False(FieldAbilities.AirRob(park, _content.Must("rio"), hit));
    }

    [Fact]
    public void BananaLandsOnTheGloveAsASlipNotAKindConversion()
    {
        // §12: an item is a field effect with seconds; it never turns an out into a caption.
        var field = new FieldingResult(PlayKind.InPlay, _content.Must("frost"), null, 2, 0, 80, false, false);
        var after = ErrorItems.Apply(field, "banana", new Random(1));
        Assert.Equal(PlayKind.InPlay, after.Kind);
        Assert.Equal("banana", after.Item);
        Assert.True(after.ItemHit);
        Assert.Equal("frost", after.ItemTarget?.Id);
        Assert.Equal(Rules.Default.Batting.Items.SlipSec, ErrorItems.EffectSec("banana"));
        Assert.False(ErrorItems.AffectsEveryGlove("banana"));
    }

    [Fact]
    public void ThrowItemBananaHitsTheBodyItWasAimedAt()
    {
        var match = Match.Slice(_content, seed: 1);
        var fielder = _content.Must("frost");
        var field = new FieldingResult(PlayKind.InPlay, fielder, null, 2, 0, 80, false, false);
        var after = match.ThrowItem(field, "banana", fielder);
        Assert.True(after.ItemHit);
        Assert.Equal("banana", after.Item);
        var miss = match.ThrowItem(field, "banana", _content.Must("rio"));
        Assert.False(miss.ItemHit, "a peel under another body does not slip the glove");
        Assert.Equal("banana", miss.Item);
    }

    [Fact]
    public void ThrowItemRocketTargetsABody()
    {
        var body = _content.Must("frost");
        var other = _content.Must("rio");
        var field = new FieldingResult(PlayKind.InPlay, body, null, 2, 0, 80, false, false);
        var miss = Match.Slice(_content, seed: 1).ThrowItem(field, "rocket", other);
        Assert.False(miss.ItemHit);
        Assert.Equal("rocket", miss.Item);

        var hits = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var after = Match.Slice(_content, seed: seed).ThrowItem(field, "rocket", body);
            Assert.Equal("rocket", after.Item);
            Assert.Equal(PlayKind.InPlay, after.Kind);
            if (after.ItemHit) hits++;
        }
        Assert.True(hits is > 0 and < 40, $"rocket body hits {hits}");
        Assert.Equal(Rules.Default.Batting.Items.DazeSec, ErrorItems.EffectSec("rocket"));
    }

    [Fact]
    public void ThrowItemPowHopsEveryBallOnTheDirt()
    {
        var match = Match.Slice(_content, seed: 1);
        var infielder = _content.Must("frost");
        var ground = new FieldingResult(PlayKind.InPlay, infielder, null, 1, 10, 40, false, false);
        var after = match.ThrowItem(ground, "pow", infielder);
        Assert.True(after.ItemHit);
        Assert.Equal("pow", after.Item);
        Assert.True(ErrorItems.AffectsEveryGlove("pow"));
        Assert.Equal(Rules.Default.Batting.Items.PowHopSec, ErrorItems.EffectSec("pow"));
        Assert.Equal(PlayKind.InPlay, after.Kind);
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
        var paint = new PitchCommand("fastball", 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        match.Play(paint, take);
        match.Play(paint, take);
        match.Play(paint, take);
        Assert.Equal(PlayKind.Strikeout, match.Log[^1].Kind);

        var walk = new PitchCommand("fastball", 0, false, AimX: 1.5);
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
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
        match.ToggleSteal();
        var won = false;
        for (var i = 0; i < 12 && !match.Over; i++)
        {
            var ev = match.Play(new PitchCommand("fastball", 0, false), take);
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
    public void StealArmReturnAndSlideOnTheLeadRunner()
    {
        var match = Match.Slice(_content, seed: 1);
        Assert.False(match.ReturnToBag());
        Assert.False(match.StartSteal());
        Assert.False(match.Slide());
        WalkOn(match);
        Assert.NotNull(match.LeadRunner);
        Assert.Equal(1, match.LeadBag);
        Assert.False(match.StealAttempt);
        Assert.True(match.StartSteal());
        Assert.True(match.StealAttempt);
        Assert.True(match.StealOn);
        // D1: no lead to walk back; stick back on the runner is the steal coming off.
        Assert.True(match.ReturnToBag());
        Assert.False(match.StealAttempt);
        Assert.False(match.StealOn);
        Assert.True(match.Slide());
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
        Assert.True(match.StartSteal());
        Assert.True(match.RunnerAt(2)!.StealArmed);
        Assert.False(match.RunnerAt(1)!.StealArmed);
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
        Assert.True(match.SelectRunner(2));
        Assert.Equal(match.Second!.Id, match.SelectedRunner!.Id);
        Assert.True(match.SelectRunner(1));
        Assert.False(match.CanSteal, "second occupied, runner on first cannot steal");
        Assert.False(match.StartSteal());
        Assert.True(match.SelectRunner(2));
        Assert.True(match.CanSteal, "selected second can steal third");
        Assert.True(match.StartSteal());
        Assert.Equal(2, match.ArmedStealBag);
        Assert.Equal(3, match.StealTargetBag);
        Assert.True(match.RunnerAt(2)!.StealArmed);
        Assert.False(match.RunnerAt(1)!.StealArmed);
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
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
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
        var paint = new PitchCommand("fastball", 0, false);
        while (kMatch.Strikes < 2 && !kMatch.Over)
            kMatch.Play(paint, take);
        Assert.True(kMatch.StartSteal());
        var punched = kMatch.Play(paint, take);
        Assert.Equal(PlayKind.Strikeout, punched.Kind);
        Assert.False(kMatch.StealOn);
        Assert.Equal(0, kMatch.ArmedStealBag);
    }

    [Fact]
    public void ARunnerOnTheBagIsNeverPickedOff()
    {
        // D1 / D3: no leads, no random pickoffs. A pitch with a runner standing on the bag never retires them.
        for (var seed = 1; seed <= 20; seed++)
        {
            var match = Match.Slice(_content, seed: seed);
            WalkOn(match);
            Assert.False(match.StealAttempt);
            var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
            var take = new SwingCommand(false, 0, 0, false);
            var ev = match.Play(wild, take);
            Assert.NotEqual(PlayKind.CaughtStealing, ev.Kind);
            Assert.NotEqual(RunnerPlayResult.PickedOff, ev.Outcome?.RunnerResult);
            Assert.NotNull(match.First);
            var back = match.Pickoff(1);
            Assert.NotNull(back);
            Assert.NotEqual(PlayKind.CaughtStealing, back!.Kind);
            Assert.NotNull(match.First);
        }
    }

    static void WalkOn(Match match)
    {
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.First is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.First);
    }

    static void WalkOnSecond(Match match)
    {
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        var take = new SwingCommand(false, 0, 0, false);
        while (match.Second is null && !match.Over)
            match.Play(wild, take);
        Assert.NotNull(match.Second);
    }

    [Fact]
    public void FairContactPutsTheBatterOnFirst()
    {
        var match = Match.Slice(_content, seed: 1);
        var paint = new PitchCommand("fastball", 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        var who = match.Batter;
        Assert.True(match.BeginAtBat(paint, swing, out _, out _));
        // A clean single to right: the batter-runner is a body that reaches first (§9.1) and is seated there at Complete (§10.6).
        var hit = FlightFixtures.Landing(match.Park, 220, 6, -40);
        Assert.True(InPlay.FairContactSendsBatter(hit));
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.Single, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
        var ev = match.FinishAtBat(paint, swing, hit, field);
        Assert.True(ev.Kind is PlayKind.Single or PlayKind.Double, ev.Kind.ToString());
        Assert.Contains(match.Runners, r => r.Who.Id == who.Id && r.Bag >= 1);
        Assert.Equal(ev.Kind == PlayKind.Single ? 1 : 2, ev.Outcome!.BatterToBag);
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
        // Before the pitch (D1): LB arms tag-and-go, RB and both shoulders take it off; nobody moves.
        Assert.True(match.AdvanceAll());
        Assert.True(match.SendAll);
        Assert.All(match.Runners, r => Assert.True(r.OnBag));
        Assert.True(match.ReturnAll());
        Assert.False(match.SendAll);
        Assert.True(match.AdvanceAll());
        Assert.True(match.FreezeRunners());
        Assert.False(match.SendAll);
        Assert.False(match.StealOn);
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

    [Fact]
    public void FlyWithRunnerOnFirstHoldsOnTheBagByDefault()
    {
        var match = Match.Slice(_content, seed: 1);
        var runner = _content.Must("rio");
        match.StationRunner(1, runner);
        var pitch = new PitchCommand("fastball", 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(pitch, swing, out _, out _));
        var hit = FlightFixtures.Landing(match.Park, 180, 34, 0);
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
        var ev = match.FinishAtBat(pitch, swing, hit, field);

        // A catchable fly holds every runner on the bag (§9.5); the catch is the out.
        Assert.Equal(PlayKind.FlyOut, ev.Kind);
        Assert.Equal(runner.Id, match.First!.Id);
        Assert.Null(match.Second);
        Assert.Equal(1, match.Outs);
    }

    [Fact]
    public void FlyAllAdvanceTagsRunnerOnFirstToSecondAfterTheCatch()
    {
        var match = Match.Slice(_content, seed: 1);
        var runner = _content.Must("rio");
        match.StationRunner(1, runner);
        Assert.True(match.AdvanceAll());
        var pitch = new PitchCommand("fastball", 0, false);
        var swing = new SwingCommand(true, 0, 0, false);
        Assert.True(match.BeginAtBat(pitch, swing, out _, out _));
        var hit = FlightFixtures.Landing(match.Park, 180, 34, 0);
        var preview = match.PreviewHit(hit);
        var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
        var ev = match.FinishAtBat(pitch, swing, hit, field);

        // All-advance before the catch is tag and go (§9.5): the runner leaves first at the catch; the
        // throw to second decides whether they make it. Either way they are not on first any more.
        Assert.Equal(PlayKind.FlyOut, ev.Kind);
        Assert.Null(match.First);
        var tagged = ev.Outcome!.OutsMade.Any(o => o.Type == OutType.Tag && o.FromBag == 1 && o.Runner.Id == runner.Id);
        Assert.True(tagged || match.Second?.Id == runner.Id, "tagged up and went: safe at second or thrown out there");
        Assert.True(ev.Outcome.OutsMade.Any(o => o.Type == OutType.Catch && o.FromBag == 0), "the catch is the out");
        Assert.DoesNotContain("triple", ev.Caption, StringComparison.OrdinalIgnoreCase);
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
            if (sendAll) match.AdvanceAll();
            var paint = new PitchCommand("fastball", 0, false);
            var swing = new SwingCommand(true, 0, 0, false);
            if (!match.BeginAtBat(paint, swing, out _, out _))
                continue;
            var deep = FlightFixtures.Landing(match.Park, 280, 32, 0);
            var preview = match.PreviewHit(deep);
            var field = new FieldingResult(PlayKind.FlyOut, preview.Fielder, null, preview.HangTimeSec, preview.LandingX, preview.LandingZ, false, false);
            // The offense is a human seat with nothing pressed: the default is the hold (§9.5); LB before the pitch is tag and go.
            var seats = new LiveSeats(HumanBats: true, HumanPitches: false, PlayerMustField: false, Versus: false);
            var ev = match.RunLive(paint, swing, deep, preview, field, seats, LivePlayCommandSource.Human);
            var scored = ev.RunsScored > 0;
            if (!sendAll)
                Assert.True(match.Third is null || match.Third.Id == thirdId || match.Outs >= 3);
            return (scored, ev.Kind);
        }
        return (false, PlayKind.FlyOut);
    }

    static void WalkOnThird(Match match)
    {
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
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
        Assert.Equal(6, FieldAbilities.CatchBonus(_content.Must("fenn")));
        Assert.Equal(0, FieldAbilities.CatchBonus(_content.Must("ashlord")));
        Assert.Equal(PlayKind.Single, FieldAbilities.SpinCheck(_content.Must("ashlord"), PlayKind.Double));
    }
}
