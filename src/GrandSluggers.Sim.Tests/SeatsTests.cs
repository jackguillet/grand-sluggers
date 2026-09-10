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
    public void TwoPadsStayOneUntilYouAskForVersus(int pads)
    {
        var seats = Seats.FromPads(pads);
        Assert.Equal(Seats.One, seats);
        Assert.False(seats.BothHuman);
        Assert.Equal(1, seats.Count);
        var vs = Seats.FromPads(pads, versus: true);
        Assert.Equal(Seats.Versus, vs);
        Assert.True(vs.BothHuman);
    }

    [Fact]
    public void VersusWantedWithoutPad2IsStillOne()
    {
        var seats = Seats.FromPads(1, versus: true);
        Assert.Equal(Seats.One, seats);
        Assert.False(seats.BothHuman);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void GamepadOneSitsAway(int pads)
    {
        var seats = Seats.FromPads(pads, versus: true);
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
        var seats = Seats.FromPads(2, versus: true);
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
    public void DisconnectingPlayerOneDoesNotPromotePlayerTwoWhenDeviceListReorders()
    {
        var devices = new DeviceSeats(pad1DeviceId: 101, pad2DeviceId: 202);

        Assert.Equal(LineupSeat.Pad1, devices.Missing(Seats.Versus, [202]));
        Assert.False(devices.Present(LineupSeat.Pad1, [202]));
        Assert.True(devices.Present(LineupSeat.Pad2, [202]));
        Assert.False(devices.Reassign(LineupSeat.Pad1, 202));
        Assert.Equal(101, devices.Pad1DeviceId);
        Assert.Equal(202, devices.Pad2DeviceId);
    }

    [Fact]
    public void DisconnectingPlayerTwoLeavesPlayerOneOnTheSameTeam()
    {
        var devices = new DeviceSeats(pad1DeviceId: 101, pad2DeviceId: 202);

        Assert.Equal(LineupSeat.Pad2, devices.Missing(Seats.Versus, [101]));
        Assert.True(devices.Present(LineupSeat.Pad1, [101]));
        Assert.False(devices.Present(LineupSeat.Pad2, [101]));
        Assert.Equal(101, devices.DeviceId(Seats.Versus.Home));
        Assert.Equal(202, devices.DeviceId(Seats.Versus.Away));
    }

    [Fact]
    public void ReconnectOrderDoesNotChangeHomeOrAwayOwnership()
    {
        var devices = new DeviceSeats(pad1DeviceId: 101, pad2DeviceId: 202);

        Assert.Equal(LineupSeat.Cpu, devices.Missing(Seats.Versus, [202, 101]));
        Assert.Equal(101, devices.DeviceId(Seats.Versus.Home));
        Assert.Equal(202, devices.DeviceId(Seats.Versus.Away));
        Assert.Equal(202, devices.DeviceId(Seats.AwayVersus.Home));
        Assert.Equal(101, devices.DeviceId(Seats.AwayVersus.Away));
    }

    [Fact]
    public void UnseatedControllerCanDeliberatelyTakeOnlyTheMissingSeat()
    {
        var devices = new DeviceSeats(pad1DeviceId: 101, pad2DeviceId: 202);
        Assert.Equal(LineupSeat.Pad2, devices.Missing(Seats.Versus, [101, 303]));

        Assert.True(devices.Reassign(LineupSeat.Pad2, 303));

        Assert.Equal(LineupSeat.Cpu, devices.Missing(Seats.Versus, [101, 303]));
        Assert.Equal(101, devices.Pad1DeviceId);
        Assert.Equal(303, devices.Pad2DeviceId);
    }

    [Fact]
    public void KeyboardAndMouseCanRecoverPlayerOneOnly()
    {
        var devices = new DeviceSeats(pad1DeviceId: 101, pad2DeviceId: 202);
        Assert.Equal(LineupSeat.Pad1, devices.Missing(Seats.Versus, [202]));

        Assert.False(devices.UseKeyboardMouse(LineupSeat.Pad2));
        Assert.True(devices.UseKeyboardMouse(LineupSeat.Pad1));

        Assert.True(devices.Pad1UsesKeyboardMouse);
        Assert.Null(devices.Pad1DeviceId);
        Assert.Equal(LineupSeat.Cpu, devices.Missing(Seats.Versus, [202]));
    }

    [Fact]
    public void OnePlayerSupportsControllerAndKeyboardMatchBindings()
    {
        var controller = DeviceSeats.BeginMatch([101], player1KeyboardMouse: false, versus: false);
        var keyboard = DeviceSeats.BeginMatch([101], player1KeyboardMouse: true, versus: false);
        var controllerModeWithoutAPad = DeviceSeats.BeginMatch([], player1KeyboardMouse: false, versus: false);

        Assert.Equal(LineupSeat.Pad1, controller.Missing(Seats.One, []));
        Assert.Equal(LineupSeat.Cpu, controller.Missing(Seats.One, [101]));
        Assert.Equal(LineupSeat.Cpu, keyboard.Missing(Seats.One, []));
        Assert.False(keyboard.Present(LineupSeat.Pad2, []));
        Assert.True(controllerModeWithoutAPad.Pad1UsesKeyboardMouse);
    }

    [Fact]
    public void KeyboardPlayerOneStillLeavesTheSecondPhysicalGamepadForPlayerTwo()
    {
        var controllerMatch = DeviceSeats.BeginMatch([101, 202], player1KeyboardMouse: false, versus: true);
        var keyboardMatch = DeviceSeats.BeginMatch([101, 202], player1KeyboardMouse: true, versus: true);

        Assert.Equal(101, controllerMatch.Pad1DeviceId);
        Assert.Equal(202, controllerMatch.Pad2DeviceId);
        Assert.True(keyboardMatch.Pad1UsesKeyboardMouse);
        Assert.Null(keyboardMatch.Pad1DeviceId);
        Assert.Equal(202, keyboardMatch.Pad2DeviceId);
    }

    [Fact]
    public void DisconnectPausePolicySurvivesSetFlightAndLiveThrowBoundaries()
    {
        var devices = new DeviceSeats(pad1DeviceId: 101, pad2DeviceId: 202);
        foreach (var moment in new[] { "SET", "pitch flight", "live ball", "throw" })
        {
            var recovery = new DeviceSeatRecovery();
            var missing = devices.Missing(Seats.Versus, [202]);
            recovery.WaitFor(missing, matchWasPaused: false);

            Assert.True(recovery.Active, moment);
            Assert.True(recovery.ResumeWhenReady, moment);
            Assert.Equal(LineupSeat.Pad1, recovery.MissingSeat);
        }
    }

    [Fact]
    public void ReconnectResumesOnlyWhenDisconnectCausedThePause()
    {
        var running = new DeviceSeatRecovery();
        running.WaitFor(LineupSeat.Pad2, matchWasPaused: false);
        running.WaitFor(LineupSeat.Pad2, matchWasPaused: true);
        Assert.True(running.ResumeWhenReady);
        running.Complete();
        Assert.False(running.Active);

        var alreadyAtCallTime = new DeviceSeatRecovery();
        alreadyAtCallTime.WaitFor(LineupSeat.Pad2, matchWasPaused: true);
        Assert.False(alreadyAtCallTime.ResumeWhenReady);
    }

    [Fact]
    public void ConfirmedSeatsSurviveSetupAndCallTimeRecoveryUntilBackToSelect()
    {
        var lifecycle = new MatchSeatLifecycle();
        Assert.Equal(Seats.One, lifecycle.Current(Seats.FromPads(1, versus: true)));

        var confirmed = lifecycle.Bind(Seats.FromPads(2, versus: true));
        var devices = DeviceSeats.BeginMatch([101, 202], player1KeyboardMouse: false, versus: confirmed.BothHuman);

        Assert.Equal(Seats.Versus, confirmed);
        Assert.True(lifecycle.Bound);
        Assert.Equal(Seats.Versus, lifecycle.Bind(Seats.AwayOne));
        foreach (var moment in new[] { "Team Setup", "Defense Setup", "Call time" })
        {
            // Player 1 disappearing must not make the surviving Player 2 become
            // the one-player seat while setup or Call time is on screen.
            Assert.Equal(Seats.Versus, lifecycle.Current(Seats.FromPads(1, versus: true)));
            var recovery = new DeviceSeatRecovery();
            recovery.WaitFor(devices.Missing(lifecycle.Seats, [202]), matchWasPaused: moment == "Call time");
            Assert.Equal(LineupSeat.Pad1, recovery.MissingSeat);
            Assert.Equal(moment != "Call time", recovery.ResumeWhenReady);
            Assert.Equal(LineupSeat.Cpu, devices.Missing(lifecycle.Seats, [101, 202]));
            recovery.Complete();
        }

        lifecycle.Release();
        Assert.False(lifecycle.Bound);
        Assert.Equal(Seats.AwayOne, lifecycle.Current(Seats.FromPads(1, pad1Home: false, versus: true)));
        Assert.Equal(Seats.AwayOne, lifecycle.Bind(Seats.FromPads(1, pad1Home: false, versus: false)));
    }

    [Fact]
    public void Pad1CanSitAwayVsCpuAndInVersus()
    {
        var one = Seats.FromPads(1, pad1Home: false);
        Assert.Equal(Seats.AwayOne, one);
        Assert.Equal(LineupSeat.Cpu, one.Home);
        Assert.Equal(LineupSeat.Pad1, one.Away);
        Assert.Equal(1, one.Count);
        Assert.True(one.CpuPitches(top: true));
        Assert.True(one.HumanBats(top: true));
        Assert.True(one.HumanPitches(top: false));
        Assert.True(one.CpuBats(top: false));

        var vs = Seats.FromPads(2, pad1Home: false, versus: true);
        Assert.Equal(Seats.AwayVersus, vs);
        Assert.Equal(LineupSeat.Pad2, vs.Home);
        Assert.Equal(LineupSeat.Pad1, vs.Away);
        Assert.True(vs.BothHuman);
        Assert.Equal(LineupSeat.Pad2, vs.Pitching(top: true));
        Assert.Equal(LineupSeat.Pad1, vs.Batting(top: true));
        Assert.Equal(Seats.FromPads(1), Seats.FromPads(1, pad1Home: true));
    }

    [Fact]
    public void SetCamera1PFollowsRoleTwoPadsStayPlate()
    {
        Assert.Equal(1, Seats.One.Count);
        Assert.Equal(2, Seats.Versus.Count);
        Assert.Equal(AtBatShots.Mound, PlayCamera.Shot(PlayCamera.Beat.Set, seats: Seats.One.Count, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: Seats.One.Count, pitchingSet: false));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: Seats.Versus.Count, pitchingSet: true));
        Assert.Equal(AtBatShots.Plate, PlayCamera.Shot(PlayCamera.Beat.Set, seats: Seats.Versus.Count, pitchingSet: false));
        Assert.Equal(AtBatShots.Mound, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 1));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(true, false, 0, 0, 0, seats: 2));
        Assert.Equal(AtBatShots.Plate, AtBatShots.SetShot(false, false, 0, 0, 0, seats: 2));
        Assert.Equal(BroadcastHud.Layout(1), BroadcastHud.Layout(2));
    }
}
