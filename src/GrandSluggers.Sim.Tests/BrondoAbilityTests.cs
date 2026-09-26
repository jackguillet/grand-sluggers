using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Brondo's two specials (#1223): Phonyball (a decoy switch on the pitch) and Double Deal (a decoy ball on the swing),
/// two decoy families of his own that no other captain carries (AB-02).
/// </summary>
public sealed class BrondoAbilityTests
{
    static ContentCatalog Content => Shipped.Content;

    // S-218  Brondo carries Phonyball and Double Deal, each its own family
    [Fact]
    public void S218_BrondoCarriesPhonyballAndDoubleDealInTwoFamiliesOfHisOwn()
    {
        var brondo = Content.Must("brondo");
        Assert.Equal("phonyball", brondo.StarPitch);
        Assert.Equal("phony-swing", brondo.StarSwing);
        Assert.Equal("Phonyball", Content.StarSkills.Pitch("phonyball")!.Name);
        Assert.Equal("Double Deal", Content.StarSkills.Swing("phony-swing")!.Name);
        Assert.Empty(ContentDataValidator.Validate(Content.Root));
    }

    // S-219  Double Deal's lesson names it and its decoy
    [Fact]
    public void S219_TheDoubleDealLessonNamesTheSpecialAndItsDecoy()
    {
        Assert.Contains("Double Deal", GrandSluggers.Sim.Front.HowToPlay.TutorialTitle("T-SS-phony-swing"));
        Assert.Contains("decoy", GrandSluggers.Sim.Front.HowToPlay.TutorialSetup("T-SS-phony-swing"));
    }
}
