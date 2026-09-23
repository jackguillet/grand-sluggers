using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class PlayStampTests
{
    [Fact]
    public void StampNamesTheDeadPlayNotTheSimEnum()
    {
        Assert.Equal("OUT", PlayStamp.Label(PlayKind.FlyOut, 1, 0));
        Assert.Equal("OUT", PlayStamp.Label(PlayKind.GroundOut, 1, 0));
        Assert.Equal("STRIKE OUT", PlayStamp.Label(PlayKind.Strikeout, 1, 0));
        Assert.Equal("HIT BY PITCH", PlayStamp.Label(PlayKind.HitByPitch, 0, 0));
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(PlayKind.GroundOut, 2, 0));
        Assert.Equal("TRIPLE PLAY", PlayStamp.Label(PlayKind.GroundOut, 3, 0));
        Assert.Equal("SINGLE", PlayStamp.Label(PlayKind.Single, 0, 1));
        Assert.Equal("DOUBLE", PlayStamp.Label(PlayKind.Double, 0, 1));
        Assert.Equal("TRIPLE", PlayStamp.Label(PlayKind.Triple, 0, 1));
        Assert.Equal("HOME RUN", PlayStamp.Label(PlayKind.HomeRun, 0, 1));
        Assert.Equal("GRAND SLAM", PlayStamp.Label(PlayKind.HomeRun, 0, 4));
        Assert.Equal("BALL", PlayStamp.Label(PlayKind.TakeBall, 0, 0));
        Assert.Equal("STRIKE", PlayStamp.Label(PlayKind.TakeStrike, 0, 0));
        Assert.Equal("STRIKE", PlayStamp.Label(PlayKind.SwingMiss, 0, 0));
        Assert.Equal("FOUL", PlayStamp.Label(PlayKind.Foul, 0, 0));
        Assert.Equal("WALK", PlayStamp.Label(PlayKind.Walk, 0, 0));
        Assert.Equal("STOLEN BASE", PlayStamp.Label(PlayKind.StolenBase, 0, 0));
        Assert.Equal("CAUGHT STEALING", PlayStamp.Label(PlayKind.CaughtStealing, 1, 0));
        Assert.Equal("BUNT", PlayStamp.Label(PlayKind.GroundOut, 1, 0, bunt: true));
        Assert.Equal("BUNT", PlayStamp.Label(PlayKind.Single, 0, 1, bunt: true));
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(PlayKind.GroundOut, 2, 0, bunt: true));
        Assert.True(PlayStamp.Shows(PlayKind.StolenBase));
        Assert.True(PlayStamp.Shows(PlayKind.CaughtStealing));

        Assert.Equal("DIVE", PlayStamp.Label(PlayKind.GroundOut, 1, 0, feat: DefensiveFeat.Dive));
        Assert.Equal("JUMP", PlayStamp.Label(PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.Jump));
        Assert.Equal("DOUBLE PLAY", PlayStamp.Label(PlayKind.GroundOut, 2, 0, feat: DefensiveFeat.Dive));
        Assert.True(PlayStamp.Shows(PlayKind.TakeBall));
        Assert.True(PlayStamp.Shows(PlayKind.TakeStrike));
        Assert.True(PlayStamp.Shows(PlayKind.SwingMiss));
        Assert.True(PlayStamp.Shows(PlayKind.Foul));
        Assert.True(PlayStamp.Shows(PlayKind.Walk));
        Assert.True(PlayStamp.Shows(PlayKind.HitByPitch));
        Assert.True(PlayStamp.Shows(PlayKind.Strikeout));
        Assert.True(PlayStamp.Shows(PlayKind.FlyOut));
        Assert.True(PlayStamp.Shows(PlayKind.HomeRun));
        Assert.False(PlayStamp.Shows(PlayKind.Pickoff));
        Assert.False(PlayStamp.Shows(PlayKind.InPlay));
        Assert.True(PlayStamp.IsCount(PlayKind.Walk));
        Assert.False(PlayStamp.IsCount(PlayKind.HitByPitch));
        Assert.False(PlayStamp.IsCount(PlayKind.Single));
        Assert.False(PlayStamp.IsCount(PlayKind.Strikeout));
    }

    /// <summary>
    /// Spec §15, row by row, from a typed PlayEvent and nothing else: the caption is deliberately
    /// wrong on every row so a stamp that read it would fail.
    /// </summary>
    public static IEnumerable<object[]> Section15Rows()
    {
        yield return Row("Pitch take", "BALL", PlayKind.TakeBall, 0, 0);
        yield return Row("Pitch strike", "STRIKE", PlayKind.TakeStrike, 0, 0);
        yield return Row("Pitch miss", "STRIKE", PlayKind.SwingMiss, 0, 0);
        yield return Row("Strikeout", "STRIKE OUT", PlayKind.Strikeout, 1, 0);
        yield return Row("Walk", "WALK", PlayKind.Walk, 0, 0);
        yield return Row("HBP", "HIT BY PITCH", PlayKind.HitByPitch, 0, 0);
        yield return Row("Foul", "FOUL", PlayKind.Foul, 0, 0);
        yield return Row("Grounder out", "OUT", PlayKind.GroundOut, 1, 0);
        yield return Row("Grounder single", "SINGLE", PlayKind.Single, 0, 0, batterToBag: 1);
        yield return Row("Grounder DP", "DOUBLE PLAY", PlayKind.GroundOut, 2, 0);
        yield return Row("Grounder TP", "TRIPLE PLAY", PlayKind.GroundOut, 3, 0);
        yield return Row("Grounder error", "ERROR", PlayKind.Single, 0, 0, error: true, batterToBag: 1);
        yield return Row("Grounder fielder's choice", "FIELDER'S CHOICE", PlayKind.GroundOut, 1, 0, fieldersChoice: true, batterToBag: 1);
        yield return Row("Grounder dive scoop", "DIVE", PlayKind.GroundOut, 1, 0, feat: DefensiveFeat.Dive);
        yield return Row("Bunt out", "BUNT", PlayKind.GroundOut, 1, 0, bunt: true);
        yield return Row("Bunt single", "BUNT", PlayKind.Single, 0, 0, bunt: true, batterToBag: 1);
        yield return Row("Fly out", "OUT", PlayKind.FlyOut, 1, 0);
        yield return Row("Fly dive", "DIVE", PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.Dive);
        yield return Row("Fly jump", "JUMP", PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.Jump);
        yield return Row("Wall rob super jump", "JUMP", PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.SuperJump);
        yield return Row("Wall rob clamber", "JUMP", PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.Clamber);
        yield return Row("Buddy jump", "BUDDY JUMP", PlayKind.FlyOut, 1, 0, feat: DefensiveFeat.BuddyJump);
        yield return Row("Fly doubled off", "DOUBLE PLAY", PlayKind.FlyOut, 2, 0, feat: DefensiveFeat.Jump);
        yield return Row("Liner double", "DOUBLE", PlayKind.Double, 0, 1, batterToBag: 2);
        yield return Row("Fly triple", "TRIPLE", PlayKind.Triple, 0, 0, batterToBag: 3);
        yield return Row("Fly error", "ERROR", PlayKind.Double, 0, 0, error: true, batterToBag: 2);
        yield return Row("Home run", "HOME RUN", PlayKind.HomeRun, 0, 1, batterToBag: 4);
        yield return Row("Grand slam", "GRAND SLAM", PlayKind.HomeRun, 0, 4, batterToBag: 4);
        yield return Row("Stolen base", "STOLEN BASE", PlayKind.StolenBase, 0, 0, runner: RunnerPlayResult.StolenBase);
        yield return Row("Caught stealing", "CAUGHT STEALING", PlayKind.CaughtStealing, 1, 0, runner: RunnerPlayResult.CaughtStealing);
        yield return Row("Picked off", "PICKED OFF", PlayKind.CaughtStealing, 1, 0, runner: RunnerPlayResult.PickedOff);
        yield return Row("K + caught stealing", "DOUBLE PLAY", PlayKind.CaughtStealing, 2, 0, runner: RunnerPlayResult.CaughtStealing);
        yield return Row("Steal on a sailed throw", "ERROR", PlayKind.StolenBase, 0, 0, runner: RunnerPlayResult.StolenBase, error: true);
        yield return Row("Steal of home", "STOLEN BASE", PlayKind.StolenBase, 0, 1, runner: RunnerPlayResult.StolenBase);
    }

    [Theory]
    [MemberData(nameof(Section15Rows))]
    public void EveryRowOfTheSection15TableStampsFromTheTypedOutcome(string row, string expected, PlayEvent ev)
    {
        Assert.Equal(expected, PlayStamp.Label(ev));
        Assert.True(PlayStamp.Shows(ev.Kind), row);
        // The caption is a lie on purpose: a stamp that read it would name the wrong play.
        Assert.DoesNotContain(expected, ev.Caption, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoPlayPickoffAndTheLiveBallNeverStamp()
    {
        Assert.False(PlayStamp.Shows(PlayKind.Pickoff));
        Assert.False(PlayStamp.Shows(PlayKind.InPlay));
        Assert.Equal("", PlayStamp.Label(null!));
    }

    [Fact]
    public void ContactWordsComeOnlyFromTheTypedZoneNeverTheRelease()
    {
        // #578: MAX on the release is the charge tell; a whiff shows STRIKE through the stamp, never a hit word.
        var feel = ContentCatalog.Load().Feel;
        Assert.Equal("MAX", ChargeFeel.NiceCopy(false, 1, 0, feel.ChargeMaxHoldSeconds));
        Assert.Equal("Nice!", ChargeFeel.NiceCopy(true, 1, 0, feel.ChargeMaxHoldSeconds));
        Assert.DoesNotContain("HIT", ChargeFeel.NiceCopy(false, 1, 0, feel.ChargeMaxHoldSeconds), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("", PlayStamp.ContactTell(ContactQuality.Miss));
        Assert.Equal("PERFECT", PlayStamp.ContactTell(ContactQuality.Perfect));
        Assert.Equal("NICE", PlayStamp.ContactTell(ContactQuality.Nice));
        Assert.Equal("SOUR", PlayStamp.ContactTell(ContactQuality.Sour));
        // MAX + Miss: the play stamps STRIKE; MAX + Perfect: PERFECT; slap + Nice: NICE.
        var max = new SwingCommand(true, 1, 0, false);
        var slap = new SwingCommand(true, 0, 0, false);
        Assert.Equal("STRIKE", PlayStamp.Label(Ev(PlayKind.SwingMiss, "Nice hit!", swing: max, quality: ContactQuality.Miss)));
        Assert.Equal("", PlayStamp.ContactTell(Ev(PlayKind.SwingMiss, "Nice hit!", swing: max, quality: ContactQuality.Miss).AtBat.Quality));
        Assert.Equal("PERFECT", PlayStamp.ContactTell(Ev(PlayKind.HomeRun, "x", swing: max, quality: ContactQuality.Perfect).AtBat.Quality));
        Assert.Equal("NICE", PlayStamp.ContactTell(Ev(PlayKind.Single, "x", swing: slap, quality: ContactQuality.Nice).AtBat.Quality));
    }

    [Fact]
    public void LiveCuesPopTheSmallTellsOnly()
    {
        Assert.Equal(PlayStamp.Safe, PlayStamp.LiveTell(LiveEvent.StampSafe));
        Assert.Equal(PlayStamp.Error, PlayStamp.LiveTell(LiveEvent.ThrowSailed));
        Assert.Equal(PlayStamp.Out, PlayStamp.LiveTell(LiveEvent.StampOut));
        Assert.Equal(PlayStamp.Score, PlayStamp.LiveTell(LiveEvent.StampScore));
        foreach (var cue in Enum.GetValues<LiveEvent>())
        {
            if (cue is LiveEvent.StampSafe or LiveEvent.ThrowSailed or LiveEvent.StampOut or LiveEvent.StampScore)
                continue;
            Assert.Equal("", PlayStamp.LiveTell(cue));
        }
        Assert.Contains(HudCallouts.InPlay.Marks, m => m.Id == "error" && m.Label.Contains(PlayStamp.Error)
            && m.Anchor == BroadcastHud.StampDirt);
        Assert.Contains(HowToPlay.Must("screen").Lines, l => l.Contains("stamp") && l.Contains("ERROR"));
        Assert.Contains(HowToPlay.Must("screen").Lines, l =>
            l.Contains("OUT") && l.Contains("SCORE") && l.Contains("mid-play"));
    }

    [Fact]
    public void LiveOutAndScoreTellsNameTheEventAndTheAnchor()
    {
        var catchOut = PlayStamp.OutTell(OutType.Catch, 0);
        Assert.Equal(PlayStamp.Out, catchOut.Word);
        Assert.Equal(StampAnchor.Glove, catchOut.Anchor);
        Assert.Equal("DIVE", PlayStamp.OutTell(OutType.Catch, 0, DefensiveFeat.Dive).Word);
        Assert.Equal("JUMP", PlayStamp.OutTell(OutType.Catch, 0, DefensiveFeat.Jump).Word);
        Assert.Equal("BUDDY JUMP", PlayStamp.OutTell(OutType.Catch, 0, DefensiveFeat.BuddyJump).Word);
        var force = PlayStamp.OutTell(OutType.Force, 2);
        Assert.Equal(PlayStamp.Out, force.Word);
        Assert.Equal(StampAnchor.Bag, force.Anchor);
        Assert.Equal(2, force.Bag);
        var tagHome = PlayStamp.OutTell(OutType.Tag, 4);
        Assert.Equal(StampAnchor.Plate, tagHome.Anchor);
        Assert.Equal(PlayStamp.Score, PlayStamp.ScoreTell().Word);
        Assert.Equal(StampAnchor.Plate, PlayStamp.ScoreTell().Anchor);
        Assert.Equal(StampAnchor.Dirt, PlayStamp.ErrorTell().Anchor);
        Assert.Equal(LiveEvent.StampOut, PlayStamp.Cue(catchOut));
        Assert.Equal(LiveEvent.StampScore, PlayStamp.Cue(PlayStamp.ScoreTell()));
    }

    [Fact]
    public void ShowsAtTimeSkipsPlaysThatAlreadyNamedThemselvesLive()
    {
        Assert.False(PlayStamp.ShowsAtTime(PlayKind.FlyOut));
        Assert.False(PlayStamp.ShowsAtTime(PlayKind.GroundOut));
        Assert.False(PlayStamp.ShowsAtTime(PlayKind.CaughtStealing));
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.Single));
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.HomeRun));
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.TakeBall));
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.Strikeout));
        Assert.True(PlayStamp.ShowsAtTime(PlayKind.StolenBase));
        var errorHit = Ev(PlayKind.Single, "Caption lies: a banana peel on the mound.", error: true, batterToBag: 1);
        Assert.False(PlayStamp.ShowsAtTime(errorHit));
        Assert.Equal(PlayStamp.Error, PlayStamp.Label(errorHit));
        Assert.False(PlayStamp.ShowsAtTime((PlayEvent?)null));
    }

    [Fact]
    public void ASacFlyEmitsACatchTellThenAScoreOrTagTellNotASingleEndOfPlayOut()
    {
        var scenario = new Scenario(Content, seed: 1).Runner(3, 2);
        var match = scenario.Match;
        var runner = match.Third!;
        scenario.Contact();
        var hit = FlightFixtures.Landing(match.Park, 245, 34, -14);
        var preview = match.PreviewHit(hit);
        Assert.True(FieldingResolver.IsOutfield(preview.Position));
        var stamps = RunLiveStamps(match, hit, preview);
        Assert.Equal(PlayKind.FlyOut, stamps.Play.Kind);
        Assert.False(PlayStamp.ShowsAtTime(stamps.Play));
        Assert.NotEmpty(stamps.Tells);
        Assert.True(PlayStamp.IsOutWord(stamps.Tells[0].Word), $"first tell is the catch: {stamps.Tells[0].Word}");
        Assert.Equal(StampAnchor.Glove, stamps.Tells[0].Anchor);
        var scored = stamps.Play.Outcome!.Moves.Any(m => m.Runner.Id == runner.Id && m.ToBag == 4);
        var outAtHome = stamps.Play.Outcome.OutsMade.Any(o => o.Runner.Id == runner.Id && o.Type == OutType.Tag);
        Assert.True(scored ^ outAtHome, "the sac fly is a race: the run or the tag at the plate");
        if (scored)
        {
            Assert.Contains(stamps.Tells, t => t.Word == PlayStamp.Score && t.Anchor == StampAnchor.Plate);
            Assert.True(stamps.Tells.FindIndex(t => t.Word == PlayStamp.Score)
                > stamps.Tells.FindIndex(t => PlayStamp.IsOutWord(t.Word)));
        }
        else
        {
            Assert.True(stamps.Tells.Count(t => PlayStamp.IsOutWord(t.Word)) >= 2,
                "catch then the tag, not one OUT at Time");
            Assert.Contains(stamps.Tells.Skip(1), t => t.Anchor is StampAnchor.Plate or StampAnchor.Bag);
        }
        Assert.NotEqual(PlayStamp.Label(stamps.Play), string.Join("+", stamps.Tells.Select(t => t.Word)));
    }

    [Fact]
    public void ASixFourThreeEmitsTwoOutTellsAsTheyHappen()
    {
        // Same occupancy and ball as S-40 (OutsScenarioTests): the DP matrix already turns two;
        // this pins that each out stamped when it was recorded, not as one Time card.
        var tests = new OutsScenarioTests();
        tests.DoublePlayMatrix_CpuSeat(new OutsScenarioTests.DpRow("S-40 6-4-3", 125, -3, -18, [1], 0, [2, 1], "SS"));
    }

    sealed record LiveStampRun(PlayEvent Play, List<LiveStamp> Tells);

    static LiveStampRun RunLiveStamps(Match match, AtBatResult hit, FieldingPreview preview,
        FieldingResult? field = null)
    {
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, field,
            LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        var tells = new List<LiveStamp>();
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 30 && play is null; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(1.0 / 60.0, LivePadInput.Dead, LivePadInput.Dead, false,
                LivePlayCommandSource.Cpu));
            tells.AddRange(live.Stamps);
            play = r.CompletedPlay;
        }
        Assert.NotNull(play);
        return new LiveStampRun(play!, tells);
    }

    static object[] Row(string row, string expected, PlayKind kind, int outs, int runs,
        DefensiveFeat feat = DefensiveFeat.None, bool bunt = false, bool error = false,
        bool fieldersChoice = false, int batterToBag = 0, RunnerPlayResult runner = RunnerPlayResult.None)
        => [row, expected, Ev(kind, "Caption lies: a banana peel on the mound.", outs, runs, feat, bunt, error, fieldersChoice, batterToBag, runner)];

    static readonly ContentCatalog Content = ContentCatalog.Load();

    static PlayEvent Ev(PlayKind kind, string caption, int outs = 0, int runs = 0,
        DefensiveFeat feat = DefensiveFeat.None, bool bunt = false, bool error = false,
        bool fieldersChoice = false, int batterToBag = 0, RunnerPlayResult runner = RunnerPlayResult.None,
        SwingCommand? swing = null, ContactQuality quality = ContactQuality.Nice)
    {
        var rio = Content.Must("rio");
        var ash = Content.Must("ashlord");
        var inPlay = kind is PlayKind.Single or PlayKind.Double or PlayKind.Triple
            or PlayKind.HomeRun or PlayKind.FlyOut or PlayKind.GroundOut;
        var hit = new AtBatResult(quality, inPlay, kind == PlayKind.Strikeout, 88, 20, 200,
            kind == PlayKind.HomeRun, false, null, null);
        var outsMade = new List<OutRecord>();
        for (var i = 0; i < outs; i++)
            outsMade.Add(new OutRecord(OutType.Force, i + 1, i, rio, ash));
        return new PlayEvent(
            kind, hit, new PitchCommand("fastball", 0, false), swing ?? new SwingCommand(true, 0, 0, false, Bunt: bunt),
            rio, ash, null, null, runs, [], caption,
            false, false, 0, 0, 0, 0, 0, 0,
            OutsOnPlay: outs,
            Outcome: new PlayOutcome(DefensiveFeat: feat, RunnerResult: runner, Outs: outsMade,
                BatterToBag: batterToBag, Error: error, FieldersChoice: fieldersChoice));
    }

    [Fact]
    public void StrikeoutKeepsThatBatterInTheBox()
    {
        var match = Match.Slice(ContentCatalog.Load(), innings: 3, seed: 1);
        var paint = new PitchCommand("fastball", 0, false);
        var take = new SwingCommand(false, 0, 0, false);
        PlayEvent? ev = null;
        for (var i = 0; i < 8 && (ev == null || ev.Kind != PlayKind.Strikeout); i++)
            ev = match.Play(paint, take);
        Assert.NotNull(ev);
        Assert.Equal(PlayKind.Strikeout, ev.Kind);
        var box = PlayStamp.BoxBatter(ev, match);
        Assert.NotNull(box);
        Assert.Equal(ev.Batter.Id, box.Id);
        Assert.NotEqual(match.Batter.Id, ev.Batter.Id);
    }

    [Fact]
    public void ABatterWhoIsABodyOnThePathAtTimeIsNotDrawnInTheBox()
    {
        // #574: on a third out made elsewhere the match flips and the batter is nobody's runner any more;
        // the typed bodies at Time still say where they stood, so the box stays empty for the result beat.
        var match = Match.Exhibition(ContentCatalog.Load(), "rio", "ashlord", 3, seed: 1);
        var batter = match.Batter;
        var onFirst = new PlayOutcome(Bodies: [new FieldBody(FieldBody.Runner, batter, Diamond.First.X, Diamond.First.Z)]);
        var ev = new PlayEvent(PlayKind.GroundOut, new AtBatResult(ContactQuality.Nice, true, false, 88, 8, 100, false, false, null, null),
            new PitchCommand("fastball", 0, false), new SwingCommand(true, 0, 0, false), batter, match.Pitcher, null, null, 0, [], "",
            false, false, 0, 0, 0, 0, 0, 0, Outcome: onFirst);
        Assert.Null(PlayStamp.BoxBatter(ev, match));
        Assert.NotNull(PlayStamp.BoxBatter(ev with { Outcome = PlayOutcome.Empty }, match));
    }

    [Fact]
    public void OutsRecordedSurvivesTheInningFlip()
    {
        var content = ContentCatalog.Load();
        var match = Match.Exhibition(content, "rio", "ashlord", 3, seed: 1);
        Assert.Equal(2, PlayStamp.OutsRecorded(1, match.Inning - 1, match.Top, match));
        Assert.Equal(1, PlayStamp.OutsRecorded(2, match.Inning, !match.Top, match));
        Assert.Equal(0, PlayStamp.OutsRecorded(0, match.Inning, match.Top, match));
    }

    [Fact]
    public void HoldIsABeatNotASkip()
    {
        var feel = ContentCatalog.Load().Feel;
        Assert.InRange(PlayStamp.HoldSeconds(PlayKind.FlyOut, feel), 1.0, 2.0);
        Assert.True(PlayStamp.HoldSeconds(PlayKind.HomeRun, feel) >
            PlayStamp.HoldSeconds(PlayKind.Single, feel));
        Assert.True(PlayStamp.HoldSeconds(PlayKind.TakeBall, feel) <
            PlayStamp.HoldSeconds(PlayKind.FlyOut, feel));
        Assert.True(PlayStamp.HoldSeconds(PlayKind.Walk, feel) <
            PlayStamp.HoldSeconds(PlayKind.Single, feel));
        Assert.InRange(PlayStamp.HoldSeconds(PlayKind.Foul, feel), 0.4, 0.9);
        Assert.True(PlayStamp.Scale(PlayKind.Strikeout) > PlayStamp.Scale(PlayKind.TakeStrike));
        Assert.True(PlayStamp.PopSeconds(PlayKind.TakeBall) < PlayStamp.PopSeconds(PlayKind.FlyOut));
        Assert.InRange(PlayStamp.Scale(PlayKind.Walk), 0.6, 0.85);
        Assert.Equal("SAFE", PlayStamp.Safe);
        Assert.True(PlayStamp.SafeScale < PlayStamp.Scale(PlayKind.Single));
        Assert.True(PlayStamp.SafeHoldSeconds(feel) < PlayStamp.HoldSeconds(PlayKind.Single, feel));
    }

    [Fact]
    public void HitsAndOutsStampOnTheFieldNotTheNextPitch()
    {
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.Single));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.Double));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.HomeRun));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.GroundOut));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.FlyOut));
        Assert.True(PlayStamp.HoldsLiveCamera(PlayKind.Strikeout));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.TakeBall));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.TakeStrike));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.Foul));
        Assert.False(PlayStamp.HoldsLiveCamera(PlayKind.Walk));
    }
}
