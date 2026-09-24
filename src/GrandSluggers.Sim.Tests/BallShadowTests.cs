using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public sealed class BallShadowTests
{
    readonly BallShadowFeel _feel = ContentCatalog.Load().Feel.BallShadow;

    [Fact]
    public void HighFlyKeepsAReadableFootprintAndTightensAsItRises()
    {
        Assert.Equal(5, BallShadow.Diameter(0, _feel));
        Assert.Equal(4, BallShadow.Diameter(30, _feel));
        Assert.Equal(3, BallShadow.Diameter(60, _feel));
        Assert.Equal(3, BallShadow.Diameter(200, _feel));
        Assert.Equal(5, BallShadow.Diameter(-1, _feel));
        Assert.True(_feel.FarDiameterFt > Baseball.InPlayDiameterFt * 2);
    }

    [Theory]
    [InlineData(0, 0)] // Home dirt
    [InlineData(30, 64)] // Infield grass
    [InlineData(-70, 250)] // Outfield
    [InlineData(0, 60.5)] // Mound table
    [InlineData(5, 60.5)] // Mound slope
    [InlineData(63.64, 63.64)] // Bag apron
    public void ProjectsStraightDownAndClearsEveryFieldSkin(double x, double z)
    {
        var point = BallShadow.Project(x, z, _feel);
        Assert.Equal(x, point.X);
        Assert.Equal(z, point.Z);
        Assert.True(point.Y > ParkDiamond.GrassTop);
        Assert.True(point.Y > ParkDiamond.PathTop);
        Assert.True(point.Y > ParkDiamond.StandY(x, z));
    }

    [Fact]
    public void TuningChangesTheFootprintWithoutChangingItsGroundPosition()
    {
        var custom = _feel with { NearDiameterFt = 8, FarDiameterFt = 4, HeightRangeFt = 100 };
        Assert.Equal(6, BallShadow.Diameter(50, custom));
        Assert.Equal(BallShadow.Project(12, 210, _feel), BallShadow.Project(12, 210, custom));
        custom = custom with { SurfaceLiftFt = 0.1 };
        Assert.Equal(0.06, BallShadow.Project(12, 210, custom).Y - BallShadow.Project(12, 210, _feel).Y, 6);
    }

    [Fact]
    public void InvalidTuningCannotHideOrInvertTheShadow()
    {
        _feel.Validate();
        Assert.Throws<InvalidDataException>(() => (_feel with { FarDiameterFt = 0 }).Validate());
        Assert.Throws<InvalidDataException>(() => (_feel with { NearDiameterFt = 2 }).Validate());
        Assert.Throws<InvalidDataException>(() => (_feel with { HeightRangeFt = double.NaN }).Validate());
        Assert.Throws<InvalidDataException>(() => (_feel with { SurfaceLiftFt = -1 }).Validate());
        Assert.Throws<InvalidDataException>(() => (_feel with { Opacity = 0 }).Validate());
    }
}
