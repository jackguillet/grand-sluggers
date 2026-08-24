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
        Assert.Equal(PracticeLesson.Pitching, run.Lesson);
        Assert.Equal(1, run.CurrentDrill);
        Assert.False(run.Finished);
        var match = run.MakeMatch(_content, seed: 1);
        Assert.Equal("harbor-diamond", match.Park.Id);
        Assert.Equal("rio", match.Home.Captain.Id);
    }

    [Fact]
    public void SkipFromLessonOneLeavesPractice()
    {
        var run = Training.Start(_content);
        Assert.True(run.Skip());
        Assert.True(run.Finished);
        Assert.Equal("Ready.", run.Caption);
    }

    [Fact]
    public void ChooseFieldingSkipsThePitchTypesTrap()
    {
        var run = Training.Start(_content);
        Assert.True(run.Choose(PracticeLesson.Fielding));
        Assert.Equal(PracticeLesson.Fielding, run.Lesson);
        Assert.Equal(3, run.CurrentDrill);
        Assert.False(run.Finished);
        Assert.True(run.RecordFielding(CaughtThrow(_content)));
        Assert.True(run.CaughtAndThrew);
    }

    [Fact]
    public void PitchingLessonTwoCompletesOnMaxChargesNotFourEnums()
    {
        var run = Training.Start(_content);
        var max = new PitchCommand("fastball", 1, 0, false);
        Assert.True(ChargeFeel.AtMax(1, 0, 0.5));
        Assert.False(run.RecordPitch(max, 10, canStar: false));
        Assert.Equal(PracticeLesson.Pitching, run.Lesson);
        Assert.False(run.RecordPitch(max, 10, false));
        Assert.True(run.RecordPitch(max, 10, false));
        Assert.Equal(3, run.MaxCharges);
        Assert.Equal(PracticeLesson.Batting, run.Lesson);
        Assert.Equal(2, run.CurrentDrill);
    }

    [Fact]
    public void OutOfZonePitchDoesNotCount()
    {
        var run = Training.Start(_content);
        var ball = new PitchCommand("fastball", 1, 0, false, 0.95, 0);
        Assert.False(AtBatResolver.PitchInZone(ball, 10));
        Assert.False(run.RecordPitch(ball, 10, canStar: false));
        Assert.Equal(PracticeLesson.Pitching, run.Lesson);
        Assert.Equal(0, run.MaxCharges);
    }

    [Fact]
    public void BattingLessonNeedsTimedChargedContact()
    {
        var run = Training.Start(_content);
        run.Choose(PracticeLesson.Batting);
        var miss = new AtBatResult(ContactQuality.Miss, false, true, 0, 0, 0, false, false, null, null);
        Assert.False(run.RecordSwing(new SwingCommand(true, 1, 20, false), miss));
        var hit = SolidHit();
        Assert.False(run.RecordSwing(new SwingCommand(true, 0.1, 0, false), hit));
        Assert.True(run.TimedContact);
        Assert.False(run.ChargedContact);
        Assert.True(run.RecordSwing(new SwingCommand(true, 1, 0, false), hit));
        Assert.Equal(PracticeLesson.Fielding, run.Lesson);
    }

    [Fact]
    public void LessonsCoverPitchBatFieldRunSpecialFree()
    {
        Assert.Equal(6, Training.Lessons.Length);
        Assert.Contains(PracticeLesson.Free, Training.Lessons);
        var run = Training.Start(_content);
        Assert.True(run.Choose(PracticeLesson.Running));
        Assert.True(run.RecordRun());
        Assert.Equal(PracticeLesson.Special, run.Lesson);
        Assert.True(run.RecordSpecial(true));
        Assert.Equal(PracticeLesson.Free, run.Lesson);
        Assert.False(run.Finished);
    }

    [Fact]
    public void FieldingAcceptsBuddyToss()
    {
        var run = Training.Start(_content);
        run.Choose(PracticeLesson.Fielding);
        var from = _content.Must("rio");
        var to = _content.Must("nico");
        var tossed = FieldDash.ApplyBuddyToss(
            new FieldingResult(PlayKind.GroundOut, from, to, 0.8, 10, 40, false, false),
            to, new ThrowResult(Chemistry.Good, 1.35, false));
        Assert.True(run.RecordFielding(tossed));
        Assert.True(run.CaughtAndThrew);
    }

    static AtBatResult SolidHit() =>
        new(ContactQuality.Solid, true, false, 88, 22, 240, false, false, null, null);

    static FieldingResult CaughtThrow(ContentCatalog content)
    {
        var from = content.Must("rio");
        var to = content.Must("nico");
        return new FieldingResult(PlayKind.FlyOut, from, to, 2.4, 12, 250, false, false,
            new ThrowResult(Chemistry.Good, 1.35, false));
    }
}
