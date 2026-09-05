using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class ChemBookTests
{
    [Fact]
    public void ChemistryAndAbilitiesAreStillsAndATypeTable()
    {
        Assert.Equal(2, ChemBook.ChemistryPairs.Count);
        Assert.Equal(Chemistry.Good, ChemBook.Good.Chem);
        Assert.Equal(Chemistry.Bad, ChemBook.Bad.Chem);
        Assert.Contains("Hearts", ChemBook.Good.Caption);
        Assert.Contains("Scribbles", ChemBook.Bad.Caption);
        Assert.Equal(["PIT", "BAT", "FLD", "RUN"], ChemBook.CardStats);
        Assert.Equal(4, ChemBook.Types.Count);
        Assert.Equal(["pitches", "swings", "running", "fielding"], ChemBook.Types.Select(t => t.Id));
        Assert.Contains(ChemBook.Types, t => t.Id == "pitches" && t.Line.Contains("mound"));
        Assert.Contains(ChemBook.Types, t => t.Id == "swings" && t.Line.Contains("plate"));
        Assert.Contains(ChemBook.Types, t => t.Id == "running" && t.Line.Contains("Close play"));
        Assert.Contains(ChemBook.Types, t => t.Id == "fielding" && t.Line.Contains("field verb"));
        foreach (var pair in ChemBook.ChemistryPairs)
            Assert.False(HowToPlay.MixesHardware(pair.Caption), pair.Id);
        foreach (var row in ChemBook.Types)
            Assert.False(HowToPlay.MixesHardware(row.Line), row.Id);
        var good = ChemBook.ChemCell(0, 1280, 800);
        var bad = ChemBook.ChemCell(1, 1280, 800);
        Assert.True(bad.X > good.X);
        Assert.Contains(HowToPlay.Must("chemistry").Lines, l => l.Contains("Hearts"));
        Assert.Contains(HowToPlay.Must("abilities").Lines, l => l.Contains("field verb"));
        var rio = CharacterCard.Of(ContentCatalog.Load().Must("rio"));
        Assert.False(string.IsNullOrWhiteSpace(rio.StarPitch));
        Assert.False(string.IsNullOrWhiteSpace(rio.StarSwing));
        Assert.False(string.IsNullOrWhiteSpace(rio.FieldVerb));
    }
}
