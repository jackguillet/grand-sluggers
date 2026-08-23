using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class TrainingTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

    [Fact]
    public void StartsOnHarborDiamond()
    {
        var run = Training.Start(_content);
        Assert.Equal("harbor-diamond", run.Park.Id);
        Assert.Equal(Training.ParkId, run.Park.Id);
        Assert.Empty(run.Park.Hazards);
        Assert.Equal(1, run.CurrentDrill);
        Assert.False(run.Finished);

        var match = run.MakeMatch(_content, seed: 1);
        Assert.Equal("harbor-diamond", match.Park.Id);
        Assert.Empty(match.Park.Hazards);
        Assert.Equal("rio", match.Home.Captain.Id);
        Assert.Equal("ashlord", match.Away.Captain.Id);
    }

    [Fact]
    public void MakeMatchIgnoresOtherParks()
    {
        var run = Training.Start(_content);
        var slice = Match.Slice(_content, parkId: "ember-keep");
        Assert.Equal("ember-keep", slice.Park.Id);
        Assert.Equal("harbor-diamond", run.MakeMatch(_content, seed: 3).Park.Id);
    }

    [Fact]
    public void OutOfZonePitchDoesNotCount()
    {
        var run = Training.Start(_content);
        var ball = new PitchCommand("fastball", 0, 0, false, 0.95, 0);
        Assert.False(AtBatResolver.PitchInZone(ball, 10));
        Assert.False(run.RecordPitch(ball, 10, canStar: false));
        Assert.Equal(1, run.CurrentDrill);
        Assert.Empty(run.InZonePitchTypes);
    }

    [Fact]
    public void DrillOneAdvancesAfterInZoneTypes()
    {
        var run = Training.Start(_content);
        foreach (var type in Training.CorePitches)
        {
            Assert.Equal(1, run.CurrentDrill);
            run.RecordPitch(new PitchCommand(type, 0, 0, false), 10, canStar: false);
        }
        Assert.Equal(2, run.CurrentDrill);
        Assert.False(run.Finished);
        Assert.Equal(4, run.InZonePitchTypes.Count);
    }

    [Fact]
    public void DrillOneRequiresStarWhenTheyHaveStars()
    {
        var run = Training.Start(_content);
        var match = run.MakeMatch(_content, seed: 1);
        Assert.True(match.CanStarPitch);

        foreach (var type in Training.CorePitches)
            run.RecordPitch(new PitchCommand(type, 0, 0, false), match);
        Assert.Equal(1, run.CurrentDrill);
        Assert.True(run.NeedStar);
        Assert.False(run.StarPitchInZone);

        Assert.True(run.RecordPitch(new PitchCommand("fastball", 0, 0, true), match));
        Assert.Equal(2, run.CurrentDrill);
        Assert.True(run.StarPitchInZone);
    }

    [Fact]
    public void WrongDrillRecordsDoNotAdvance()
    {
        var run = Training.Start(_content);
        var hit = SolidHit();
        Assert.False(run.RecordSwing(new SwingCommand(true, 0.9, 0, false, 8), hit));
        Assert.False(run.RecordFielding(CaughtThrow(_content)));
        Assert.Equal(1, run.CurrentDrill);
    }

    [Fact]
    public void DrillTwoNeedsTimedChargedContact()
    {
        var run = FinishPitchDrill(stars: false);
        var miss = new AtBatResult(ContactQuality.Miss, false, true, 0, 0, 0, false, false, null, null);
        Assert.False(run.RecordSwing(new SwingCommand(true, 1, 20, false), miss));
        Assert.Equal(2, run.CurrentDrill);

        var hit = SolidHit();
        Assert.False(run.RecordSwing(new SwingCommand(true, 0.2, 0, false, 12), hit));
        Assert.True(run.TimedContact);
        Assert.False(run.ChargedContact);
        Assert.Equal(2, run.CurrentDrill);

        Assert.True(run.RecordSwing(new SwingCommand(true, 0.8, 0.4, false, 12), hit));
        Assert.True(run.ChargedContact);
        Assert.Equal(3, run.CurrentDrill);
    }

    [Fact]
    public void DrillTwoSprayIsOptional()
    {
        var run = FinishPitchDrill(stars: false);
        var hit = SolidHit();
        Assert.True(run.RecordSwing(new SwingCommand(true, 0.7, 0, false, 0), hit));
        Assert.Equal(3, run.CurrentDrill);
    }

    [Fact]
    public void DrillThreeNeedsCatchAndThrowToABag()
    {
        var run = FinishSwingDrill();
        var rio = _content.Must("rio");
        var fly = new FieldingResult(PlayKind.FlyOut, rio, null, 2.2, 0, 240, false, false);
        Assert.False(run.RecordFielding(fly));
        Assert.Equal(3, run.CurrentDrill);
        Assert.False(run.CaughtAndThrew);

        Assert.True(run.RecordFielding(CaughtThrow(_content)));
        Assert.True(run.CaughtAndThrew);
        Assert.Equal(4, run.CurrentDrill);
    }

    [Fact]
    public void DrillFourGoodChemThrowIsFasterThanBad()
    {
        var run = FinishFieldDrill();
        var match = run.MakeMatch(_content, seed: 7);
        Assert.True(Training.TryFindChemPair(match, out var from, out var goodTo, out var badTo));
        Assert.Equal(Chemistry.Good, match.Chemistry.Between(from, goodTo));
        Assert.Equal(Chemistry.Bad, match.Chemistry.Between(from, badTo));

        Assert.True(run.RecordChemThrows(match));
        Assert.True(run.LastGoodThrow is { } good && run.LastBadThrow is { } bad && good.SpeedMul > bad.SpeedMul,
            $"good {run.LastGoodThrow?.SpeedMul} vs bad {run.LastBadThrow?.SpeedMul}");
        Assert.True(run.Finished);
        Assert.Equal(5, run.CurrentDrill);
        Assert.Equal("harbor-diamond", match.Park.Id);
    }

    [Fact]
    public void DrillFourRejectsSameChemPair()
    {
        var run = FinishFieldDrill();
        var good = new ThrowResult(Chemistry.Good, 1.35, false);
        var alsoGood = new ThrowResult(Chemistry.Good, 1.0, false);
        Assert.False(run.RecordChemThrows(good, alsoGood));
        Assert.Equal(4, run.CurrentDrill);
        Assert.False(run.Finished);
    }

    [Fact]
    public void FullSessionAdvancesOneTwoThreeFour()
    {
        var run = Training.Start(_content);
        var match = run.MakeMatch(_content, seed: 2);
        Assert.Equal(1, run.CurrentDrill);

        foreach (var type in Training.CorePitches)
            run.RecordPitch(new PitchCommand(type, 0, 0, false), match);
        run.RecordPitch(new PitchCommand("slider", 0, 0, true), match);
        Assert.Equal(2, run.CurrentDrill);

        run.RecordSwing(new SwingCommand(true, 0.9, 0, false, 6), SolidHit());
        Assert.Equal(3, run.CurrentDrill);

        run.RecordFielding(CaughtThrow(_content));
        Assert.Equal(4, run.CurrentDrill);

        Assert.True(run.RecordChemThrows(match));
        Assert.True(run.Finished);
        Assert.True(run.LastGoodThrow is { } g && run.LastBadThrow is { } b && g.SpeedMul > b.SpeedMul);
        Assert.Equal("Ready.", run.Caption);
    }

    Training FinishPitchDrill(bool stars)
    {
        var run = Training.Start(_content);
        foreach (var type in Training.CorePitches)
            run.RecordPitch(new PitchCommand(type, 0, 0, false), 10, stars);
        if (stars)
            run.RecordPitch(new PitchCommand("fastball", 0, 0, true), 10, true);
        Assert.Equal(2, run.CurrentDrill);
        return run;
    }

    Training FinishSwingDrill()
    {
        var run = FinishPitchDrill(stars: false);
        run.RecordSwing(new SwingCommand(true, 0.8, 0, false), SolidHit());
        Assert.Equal(3, run.CurrentDrill);
        return run;
    }

    Training FinishFieldDrill()
    {
        var run = FinishSwingDrill();
        run.RecordFielding(CaughtThrow(_content));
        Assert.Equal(4, run.CurrentDrill);
        return run;
    }

    static AtBatResult SolidHit() =>
        new(ContactQuality.Solid, true, false, 88, 22, 240, false, false, null, null);

    static FieldingResult CaughtThrow(ContentCatalog content)
    {
        var from = content.Must("rio");
        var to = content.Must("nico");
        var thr = new ThrowResult(Chemistry.Good, 1.35, false);
        return new FieldingResult(PlayKind.FlyOut, from, to, 2.4, 12, 250, false, false, thr);
    }
}
