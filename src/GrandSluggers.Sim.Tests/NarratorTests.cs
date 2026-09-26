using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The one narrator (spec §12): the match raises a <see cref="PlayCall"/> and <see cref="PlayNarrator.Narrate(PlayCall)"/>
/// is the only place its words are chosen. These pin the joins the pinned night games rely on.
/// </summary>
public sealed class NarratorTests
{
    static Character Fielder => Shipped.Content.Must("vale");

    static CallPart Live(InPlay.ThrowVerdict verdict, int bag = 1, bool aside = false) =>
        new(CallBeat.Live, Who: "Rio", Other: "Ashlord", Moment: new LiveMoment(verdict, bag, Fielder, null), Aside: aside);

    [Fact]
    public void FactsJoinWithOneSpaceAndTheBillboardAndItemWithTwo() =>
        Assert.Equal("Rio singles.  Billboard STAR!  Banana slip!",
            PlayNarrator.Narrate(PlayCall.Of(new CallPart(CallBeat.Single, Who: "Rio"), new CallPart(CallBeat.Billboard),
                new CallPart(CallBeat.Item, Word: "banana"))));

    [Fact]
    public void ALiveLineThatPlacesTheBatterDropsTheSeparateInAtFirst()
    {
        var call = PlayCall.Of(Live(InPlay.ThrowVerdict.BatterSafeAfterForce), new CallPart(CallBeat.BatterInAtFirst, Who: "Rio"));
        Assert.Equal("Force at second. Rio in at first.", PlayNarrator.Narrate(call));
        var beat = PlayCall.Of(Live(InPlay.ThrowVerdict.Beat), new CallPart(CallBeat.BatterInAtFirst, Who: "Rio"));
        Assert.Equal("Rio beats the throw. Rio in at first.", PlayNarrator.Narrate(beat));
    }

    [Fact]
    public void TurningTwoIsTheDoublePlaysOwnLine()
    {
        Assert.Equal($"{Fielder.Name} turns two.",
            PlayNarrator.Narrate(PlayCall.Of(new CallPart(CallBeat.DoublePlay), Live(InPlay.ThrowVerdict.TurnedTwo))));
        Assert.Equal($"Double play. {Fielder.Name} to first.",
            PlayNarrator.Narrate(PlayCall.Of(new CallPart(CallBeat.DoublePlay), Live(InPlay.ThrowVerdict.OutAtFirst))));
    }

    [Fact]
    public void AnAsideThatSaysNothingHandsItsGapOn()
    {
        // The runner play after a pitch: a live decision with no line leaves "Ball 2.  Zig caught stealing.".
        var ball = PlayCall.Of(new CallPart(CallBeat.Ball, Number: 2));
        Assert.Equal("Ball 2.  Zig caught stealing.", PlayNarrator.Narrate(ball.Then(
            [Live(InPlay.ThrowVerdict.None, aside: true), new CallPart(CallBeat.CaughtStealing, Who: "Zig")])));
        Assert.Equal($"Ball 2.  {Fielder.Name} tags the runner at third. Zig caught stealing.", PlayNarrator.Narrate(ball.Then(
            [Live(InPlay.ThrowVerdict.TagOut, bag: 3, aside: true), new CallPart(CallBeat.CaughtStealing, Who: "Zig")])));
    }

    [Fact]
    public void APlainFactThatSaysNothingKeepsItsSpace() =>
        // A leading live decision with no line still takes its separator: the caption the night games pinned.
        Assert.Equal(" Foul.", PlayNarrator.Narrate(PlayCall.Of(Live(InPlay.ThrowVerdict.None), new CallPart(CallBeat.Foul))));

    [Theory]
    [InlineData("furnace", "Rio HOT IRON - it's gone.")]
    [InlineData("heat-swing", "Rio HEAT-SWING - it's gone.")]
    [InlineData("cask-swing", "Rio goes deep.")]
    [InlineData(null, "Rio goes deep.")]
    public void AHomeRunNamesOnlyTheStarSwingsThatCallThemselves(string? star, string caption) =>
        Assert.Equal(caption, PlayNarrator.Narrate(PlayCall.Of(new CallPart(CallBeat.HomeRun, Who: "Rio", Word: star))));

    [Fact]
    public void EveryBeatSaysSomethingButTheLiveLine()
    {
        foreach (var beat in Enum.GetValues<CallBeat>())
        {
            if (beat == CallBeat.Live) continue;
            Assert.NotEqual("", PlayNarrator.Narrate(PlayCall.Of(new CallPart(beat, Who: "Rio", Other: "Vale", Number: 2, Word: "pow"))));
        }
    }

    [Fact]
    public void EveryPlayTheMatchLogsCarriesTheCallItsCaptionWasNarratedFrom()
    {
        var match = Match.Slice(Shipped.Content, seed: 7);
        match.AutoPlayGame();
        Assert.NotEmpty(match.Log);
        foreach (var ev in match.Log)
        {
            Assert.NotNull(ev.Call);
            Assert.Equal(PlayNarrator.Narrate(ev.Call!), ev.Caption);
        }
    }
}
