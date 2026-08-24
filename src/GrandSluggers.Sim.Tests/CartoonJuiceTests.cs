using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CartoonJuiceTests
{
    [Fact]
    public void ContactPuffsDirtAndPunches()
    {
        Assert.True(CartoonJuice.DirtPuff(ContactQuality.Perfect));
        Assert.True(CartoonJuice.DirtPuff(ContactQuality.Solid));
        Assert.True(CartoonJuice.DirtPuff(ContactQuality.Cheap));
        Assert.False(CartoonJuice.DirtPuff(ContactQuality.Miss));
        Assert.True(CartoonJuice.Punch(ContactQuality.Perfect) > CartoonJuice.Punch(ContactQuality.Solid));
        Assert.True(CartoonJuice.Punch(ContactQuality.Solid) > CartoonJuice.Punch(ContactQuality.Cheap));
        Assert.Equal(0, CartoonJuice.Punch(ContactQuality.Miss));
    }

    [Fact]
    public void ChaseRunsUntilTheGloveIsClose()
    {
        Assert.True(CartoonJuice.ChaseIsARun(caught: false, distToBall: 40));
        Assert.False(CartoonJuice.ChaseIsARun(caught: false, distToBall: 4));
        Assert.False(CartoonJuice.ChaseIsARun(caught: true, distToBall: 40));
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
