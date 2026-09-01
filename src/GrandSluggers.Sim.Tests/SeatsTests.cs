using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class SeatsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GamepadZeroIsHomeMissingPad2IsCpu(int pads)
    {
        var seats = Seats.FromPads(pads);
        Assert.Equal(LineupSeat.Pad1, seats.Home);
        Assert.Equal(LineupSeat.Cpu, seats.Away);
        Assert.Equal(Seats.One, seats);
        Assert.True(seats.HomeHuman);
        Assert.False(seats.AwayHuman);
        Assert.False(seats.BothHuman);
        Assert.Equal(1, seats.Count);
        Assert.Equal(LineupSeat.Pad1, seats.Pitching(top: true));
        Assert.Equal(LineupSeat.Cpu, seats.Batting(top: true));
        Assert.Equal(LineupSeat.Cpu, seats.Pitching(top: false));
        Assert.Equal(LineupSeat.Pad1, seats.Batting(top: false));
        Assert.True(seats.HumanPitches(top: true));
        Assert.True(seats.CpuBats(top: true));
        Assert.True(seats.CpuPitches(top: false));
        Assert.True(seats.HumanBats(top: false));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void GamepadOneSitsAway(int pads)
    {
        var seats = Seats.FromPads(pads);
        Assert.Equal(LineupSeat.Pad1, seats.Home);
        Assert.Equal(LineupSeat.Pad2, seats.Away);
        Assert.Equal(Seats.Versus, seats);
        Assert.True(seats.BothHuman);
        Assert.Equal(2, seats.Count);
        Assert.Equal(LineupSeat.Pad1, seats.Fielding(top: true));
        Assert.Equal(LineupSeat.Pad2, seats.Running(top: true));
        Assert.Equal(LineupSeat.Pad2, seats.Fielding(top: false));
        Assert.Equal(LineupSeat.Pad1, seats.Running(top: false));
    }

    [Fact]
    public void BothSeatsHumanCpuPitchAndSwingDoNotFire()
    {
        var seats = Seats.FromPads(2);
        Assert.True(seats.HumanPitches(top: true));
        Assert.True(seats.HumanBats(top: true));
        Assert.False(seats.CpuPitches(top: true));
        Assert.False(seats.CpuBats(top: true));
        Assert.True(seats.HumanPitches(top: false));
        Assert.True(seats.HumanBats(top: false));
        Assert.False(seats.CpuPitches(top: false));
        Assert.False(seats.CpuBats(top: false));
    }

    [Fact]
    public void UnplugPad2BecomesCpuWithoutANewInning()
    {
        var vs = Seats.FromPads(2);
        var unplug = Seats.FromPads(1);
        Assert.True(vs.HumanBats(top: true));
        Assert.True(unplug.CpuBats(top: true));
        Assert.True(unplug.HumanPitches(top: true));
        Assert.Equal(LineupSeat.Pad1, unplug.Home);
        Assert.Equal(vs.Home, unplug.Home);
        Assert.Equal(1, unplug.Count);
    }

    [Fact]
    public void SetCameraForksBySeatNotByRole()
    {
        Assert.Equal(1, Seats.One.Count);
        Assert.Equal(2, Seats.Versus.Count);
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.Set, seats: Seats.One.Count));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: Seats.Versus.Count));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 2));
        Assert.Equal(BroadcastHud.Layout(1), BroadcastHud.Layout(2));
    }
}
