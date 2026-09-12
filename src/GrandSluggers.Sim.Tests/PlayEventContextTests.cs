using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PlayEventContextTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    static readonly PitchCommand Paint = new("fastball", 0, false);
    static readonly SwingCommand Swing = new(true, 0, 0, false);

    [Fact]
    public void HitKeepsTheActorsWhoProducedItAndNamesTheNextLineupState()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var batter = match.Batter;
        var pitcher = match.Pitcher;
        Assert.True(match.BeginAtBat(Paint, Swing, out _, out _));
        var hit = FlightFixtures.Landing(match.Park, 220, 6, -40);

        var ev = match.FinishAtBat(Paint, Swing, hit,
            new FieldingResult(PlayKind.Single, pitcher, null, 1, 20, 80, false, false));

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(batter.Id, ev.Batter.Id);
        Assert.Equal(pitcher.Id, ev.Pitcher.Id);
        Assert.Equal(batter.Id, context.BatterId);
        Assert.Equal(pitcher.Id, context.PitcherId);
        Assert.Equal((1, true, 0), (context.Inning, context.Top, context.OutsBefore));
        Assert.NotEqual(context.BatterId, next.BatterId);
        Assert.Equal(match.Batter.Id, next.BatterId);
        Assert.Equal((match.Inning, match.Top, match.Outs), (next.Inning, next.Top, next.Outs));
        Assert.Equal(0, ev.OutsOnPlay);
        Assert.Equal(ev, match.Log[^1]);
    }

    [Fact]
    public void ThirdOutKeepsTheCompletedHalfSeparateFromTheNextHalf()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        FinishFlyOut(match);
        FinishFlyOut(match);
        var batter = match.Batter;
        var pitcher = match.Pitcher;

        var ev = FinishFlyOut(match);

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(batter.Id, ev.Batter.Id);
        Assert.Equal(pitcher.Id, ev.Pitcher.Id);
        Assert.Equal((1, true, 2), (context.Inning, context.Top, context.OutsBefore));
        Assert.Equal(1, ev.OutsOnPlay);
        Assert.Equal(0, ev.OutsAfter);
        Assert.Equal((1, false, 0), (next.Inning, next.Top, next.Outs));
        Assert.Equal(match.Batter.Id, next.BatterId);
        Assert.Equal(match.Pitcher.Id, next.PitcherId);
    }

    [Fact]
    public void InningEndingDoublePlayRecordsTwoOutsBeforeTheHalfFlips()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        FinishFlyOut(match);
        Assert.True(match.StationRunner(1, match.OnDeck!));
        var batter = match.Batter;
        var pitcher = match.Pitcher;
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));
        var field = new FieldingResult(PlayKind.GroundOut, pitcher, null, 1, 40, 70, false, false);
        BeginLive(match);
        Assert.True(StepThrow(match, 2, runnerBeats: false, pitcher).Out);
        Assert.True(StepThrow(match, 1, runnerBeats: false, pitcher).Out);

        var ev = match.FinishAtBat(Paint, Swing, hit, field);

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(batter.Id, ev.Batter.Id);
        Assert.Equal(pitcher.Id, ev.Pitcher.Id);
        Assert.Equal(1, context.OutsBefore);
        Assert.Equal(2, ev.OutsOnPlay);
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(ev));
        Assert.Equal((1, false, 0), (next.Inning, next.Top, next.Outs));
        Assert.Equal(0, ev.OutsAfter);
    }

    [Fact]
    public void WalkoffKeepsTheWinningBatterAndRecordsTheFinalState()
    {
        var match = Match.Slice(_content, innings: 1, seed: 1);
        match.SkipToHomeHalf();
        Assert.True(match.StationRunner(3, match.OnDeck!));
        var batter = match.Batter;
        var pitcher = match.Pitcher;
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));

        var ev = match.FinishAtBat(Paint, Swing, hit,
            new FieldingResult(PlayKind.Single, pitcher, null, 1, 30, 100, false, false));

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(batter.Id, ev.Batter.Id);
        Assert.Equal(pitcher.Id, ev.Pitcher.Id);
        Assert.Equal((1, false), (context.Inning, context.Top));
        Assert.Equal(0, ev.OutsOnPlay);
        Assert.True(next.Over);
        Assert.True(match.Over);
        Assert.True(next.HomeScore > next.AwayScore);
        Assert.Equal((match.AwayScore, match.HomeScore), (next.AwayScore, next.HomeScore));
    }

    [Fact]
    public void CaughtStealingForTheThirdOutKeepsPitchActorsAndNextHalfState()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        FinishFlyOut(match);
        FinishFlyOut(match);
        Assert.True(match.StationRunner(1, match.OnDeck!));
        Assert.True(match.StartSteal());
        var batter = match.Batter;
        var pitcher = match.Pitcher;
        var take = new SwingCommand(false, 0, 0, false);
        var wild = new PitchCommand("fastball", 0, false);
        Assert.False(match.BeginAtBat(wild, take, out _, out var pitchEvent));
        Assert.NotNull(pitchEvent);

        var ev = match.ResolveStealThrow(
            pitchEvent!, 2, 0, new ThrowResult(Chemistry.Good, 5, false));

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(PlayKind.CaughtStealing, ev.Kind);
        Assert.Equal(batter.Id, ev.Batter.Id);
        Assert.Equal(pitcher.Id, ev.Pitcher.Id);
        Assert.Equal((1, true, 2), (context.Inning, context.Top, context.OutsBefore));
        Assert.Equal(1, ev.OutsOnPlay);
        Assert.Equal((1, false, 0), (next.Inning, next.Top, next.Outs));
        Assert.Null(next.FirstRunnerId);
        Assert.Equal(ev, match.Log[^1]);
    }

    [Fact]
    public void StolenBaseRecordsRunnerOccupancyBeforeAndAfterThePlay()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var runner = match.OnDeck!;
        Assert.True(match.StationRunner(1, runner));
        Assert.True(match.StartSteal());
        var take = new SwingCommand(false, 0, 0, false);
        var wild = new PitchCommand("fastball", 0, false);
        Assert.False(match.BeginAtBat(wild, take, out _, out var pitchEvent));
        Assert.NotNull(pitchEvent);

        var ev = match.ResolveStealThrow(
            pitchEvent!, 2, 9, new ThrowResult(Chemistry.Neutral, 1, false));

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(PlayKind.StolenBase, ev.Kind);
        Assert.Equal(runner.Id, context.FirstRunnerId);
        Assert.Null(context.SecondRunnerId);
        Assert.Null(next.FirstRunnerId);
        Assert.Equal(runner.Id, next.SecondRunnerId);
        Assert.Equal(0, ev.OutsOnPlay);
    }

    [Fact]
    public void PickoffPlayCarriesItsOwnContextAndNextState()
    {
        var match = Match.Slice(_content, innings: 3, seed: 1);
        var runner = match.OnDeck!;
        Assert.True(match.StationRunner(1, runner));
        var batter = match.Batter;
        var pitcher = match.Pitcher;

        var ev = Assert.IsType<PlayEvent>(match.Pickoff(1));

        var context = Assert.IsType<PlayContext>(ev.Context);
        var next = Assert.IsType<MatchState>(ev.NextState);
        Assert.Equal(batter.Id, context.BatterId);
        Assert.Equal(pitcher.Id, context.PitcherId);
        Assert.Equal(runner.Id, context.FirstRunnerId);
        Assert.Equal(runner.Id, next.FirstRunnerId);
        Assert.Equal(0, ev.OutsOnPlay);
        Assert.Equal((context.Inning, context.Top), (next.Inning, next.Top));
    }

    [Fact]
    public void TerminalCountPlaysKeepTheirActorsAndPrePitchCount()
    {
        var take = new SwingCommand(false, 0, 0, false);

        var strikeout = Match.Slice(_content, innings: 3, seed: 1);
        strikeout.Play(Paint, take);
        strikeout.Play(Paint, take);
        var strikeBatter = strikeout.Batter;
        var strikePitcher = strikeout.Pitcher;
        var strikeEvent = strikeout.Play(Paint, take);
        var strikeContext = Assert.IsType<PlayContext>(strikeEvent.Context);
        Assert.Equal(PlayKind.Strikeout, strikeEvent.Kind);
        Assert.Equal(strikeBatter.Id, strikeEvent.Batter.Id);
        Assert.Equal(strikePitcher.Id, strikeEvent.Pitcher.Id);
        Assert.Equal((0, 2), (strikeContext.BallsBefore, strikeContext.StrikesBefore));
        Assert.Equal(1, strikeEvent.OutsOnPlay);

        var walk = Match.Slice(_content, innings: 3, seed: 1);
        var wild = new PitchCommand("fastball", 0, false, AimX: 1.5);
        walk.Play(wild, take);
        walk.Play(wild, take);
        walk.Play(wild, take);
        var walkBatter = walk.Batter;
        var walkPitcher = walk.Pitcher;
        var walkEvent = walk.Play(wild, take);
        var walkContext = Assert.IsType<PlayContext>(walkEvent.Context);
        Assert.Equal(PlayKind.Walk, walkEvent.Kind);
        Assert.Equal(walkBatter.Id, walkEvent.Batter.Id);
        Assert.Equal(walkPitcher.Id, walkEvent.Pitcher.Id);
        Assert.Equal((3, 0), (walkContext.BallsBefore, walkContext.StrikesBefore));
        Assert.Equal(walk.Batter.Id, Assert.IsType<MatchState>(walkEvent.NextState).BatterId);

        var hitByPitch = Match.Slice(_content, innings: 3, seed: 1);
        var plunked = hitByPitch.Batter;
        var plunkPitcher = hitByPitch.Pitcher;
        var bodyX = AtBatResolver.BatterBodyX(0, plunked.Bats) / PitchFlight.PlateScaleX;
        var plunk = PitchFlight.AimForCrossing(
            new PitchCommand("fastball", 0, false), bodyX, 0);
        var plunkEvent = hitByPitch.Play(plunk, take);
        Assert.Equal(PlayKind.HitByPitch, plunkEvent.Kind);
        Assert.Equal(plunked.Id, plunkEvent.Batter.Id);
        Assert.Equal(plunkPitcher.Id, plunkEvent.Pitcher.Id);
        Assert.Equal(plunked.Id, Assert.IsType<PlayContext>(plunkEvent.Context).BatterId);
        Assert.Equal(hitByPitch.Batter.Id, Assert.IsType<MatchState>(plunkEvent.NextState).BatterId);
    }

    [Fact]
    public void CompletedGameGivesEveryEventAnOriginAndNextState()
    {
        var match = Match.Slice(_content, innings: 1, seed: 7);

        match.AutoPlayGame();

        Assert.NotEmpty(match.Log);
        Assert.All(match.Log, ev =>
        {
            var context = Assert.IsType<PlayContext>(ev.Context);
            var next = Assert.IsType<MatchState>(ev.NextState);
            Assert.Equal(context.BatterId, ev.Batter.Id);
            Assert.Equal(context.PitcherId, ev.Pitcher.Id);
            Assert.Equal(next.Outs, ev.OutsAfter);
            Assert.Equal(next.AwayScore, ev.AwayScoreAfter);
            Assert.Equal(next.HomeScore, ev.HomeScoreAfter);
        });
    }

    static PlayEvent FinishFlyOut(Match match)
    {
        Assert.True(match.BeginAtBat(Paint, Swing, out var hit, out _));
        var field = new FieldingResult(PlayKind.FlyOut, match.Pitcher, null, 1, 0, 200, false, false);
        return match.FinishAtBat(Paint, Swing, hit, field);
    }

    static void BeginLive(Match match) =>
        match.LivePlay.Apply(LivePlayCommand.Begin(PlayKind.GroundOut));

    static InPlay.GroundThrowStep StepThrow(Match match, int bag, bool runnerBeats, Character? fielder) =>
        Assert.IsType<InPlay.GroundThrowStep>(
            match.LivePlay.Apply(LivePlayCommand.ThrowArrived(bag, runnerBeats, fielder)).Throw);
}
