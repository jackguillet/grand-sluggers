using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ToyMeshTests
{
    [Fact]
    public void AuthoredBaseballIsUnitDiameterSoApparentScaleIsTheLocalScale()
    {
        Assert.Equal(1f, ToyMesh.BaseballRestDiameter);
        Assert.Equal((float)Baseball.DiameterFt, ToyMesh.BallViewScale(false, 60));
        Assert.Equal((float)Baseball.InPlayDiameterFt, ToyMesh.BallViewScale(true, 45, inPlay: true));
        Assert.Equal((float)Baseball.ApparentScale(true, 58), ToyMesh.BallViewScale(true, 58));
        Assert.True(ToyMesh.BallViewScale(false, 60) < ToyMesh.BallViewScale(true, 58));
    }

    [Fact]
    public void AuthoredGloveRootTakesSilhouetteGloveScale()
    {
        Assert.Equal(Silhouette.GloveScale, ToyMesh.GloveRootScale(false));
        Assert.True(ToyMesh.GloveRootScale(true) > ToyMesh.GloveRootScale(false));
        Assert.Equal(Silhouette.GloveScale * 1.12f, ToyMesh.GloveRootScale(true));
    }
}
