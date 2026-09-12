using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CartoonJuiceTests
{
    [Fact]
    public void ContactPuffsDirtAndPunches()
    {
        Assert.True(CartoonJuice.DirtPuff(ContactQuality.Perfect));
        Assert.True(CartoonJuice.DirtPuff(ContactQuality.Nice));
        Assert.True(CartoonJuice.DirtPuff(ContactQuality.Sour));
        Assert.False(CartoonJuice.DirtPuff(ContactQuality.Miss));
        Assert.True(CartoonJuice.Punch(ContactQuality.Perfect) > CartoonJuice.Punch(ContactQuality.Nice));
        Assert.True(CartoonJuice.Punch(ContactQuality.Nice) > CartoonJuice.Punch(ContactQuality.Sour));
        Assert.Equal(0, CartoonJuice.Punch(ContactQuality.Miss));
    }

    [Fact]
    public void ChaseRunsUntilTheGloveIsClose()
    {
        Assert.True(CartoonJuice.ChaseIsARun(caught: false, distToPlant: 40));
        Assert.False(CartoonJuice.ChaseIsARun(caught: false, distToPlant: 4),
            "under the landing ring is waiting, not a run");
        Assert.False(CartoonJuice.ChaseIsARun(caught: true, distToPlant: 40));
        Assert.True(CartoonJuice.StandingStill(0));
        Assert.True(CartoonJuice.StandingStill(2));
        Assert.False(CartoonJuice.StandingStill(8));
        Assert.False(CartoonJuice.StandingStill(CartoonJuice.RunFtPerSec));
    }

    [Fact]
    public void GoodThrowIsPurpleGoldBadThrowIsMud()
    {
        var good = CartoonJuice.ThrowRgb(Chemistry.Good);
        var bad = CartoonJuice.ThrowRgb(Chemistry.Bad);
        Assert.True(good.B > good.G, "good chem is a purple laser");
        Assert.True(bad.R > bad.B && bad.G >= bad.B * 0.5, "bad chem is muddy");
    }
}
