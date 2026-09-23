using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class DefenseArrangementTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();
    Match Game(bool drafted = false)
    {
        if (!drafted) return Match.Slice(_content, seed: 7);
        var home = TeamBuilder.Draft(_content, "rio");
        home.SetGlove("SS", home.Gloves["CF"].Id);
        var away = TeamBuilder.Draft(_content, "vale", home.Order.Select(c => c.Id));
        return Match.Exhibition(_content, home.ToTeam(), away.ToTeam(), 3, 7);
    }
    static Dictionary<string, Character> Map(Match m) => FieldingResolver.Assign(m.DefenseRoster, m.Pitcher, m.Defense.Gloves);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EveryPositionPairTradesOnlyThoseGlovesAndPreservesBattingAndArmPools(bool drafted)
    {
        for (var i = 0; i < Diamond.Order.Length; i++)
        for (var j = i + 1; j < Diamond.Order.Length; j++)
        {
            var m = Game(drafted);
            var original = m.Home;
            var originalGloves = original.Gloves?.ToDictionary(p => p.Key, p => p.Value.Id);
            var before = Map(m);
            var order = m.HomeOrder.Select(c => c.Id).ToArray();
            var other = m.Away;
            var pools = m.DefenseRoster.ToDictionary(c => c.Id, m.StaminaOf);
            var a = Diamond.Order[i]; var b = Diamond.Order[j];
            Assert.True(m.SwapDefensePositions(a, b));
            var after = Map(m);
            foreach (var pos in Diamond.Order)
                Assert.Equal(before[pos == a ? b : pos == b ? a : pos].Id, after[pos].Id);
            Assert.Equal(after, FieldingResolver.Assign(m.DefenseRoster, m.Pitcher));
            Assert.Equal(order, m.HomeOrder.Select(c => c.Id));
            Assert.Same(other, m.Away);
            Assert.Equal(originalGloves, original.Gloves?.ToDictionary(p => p.Key, p => p.Value.Id));
            foreach (var who in m.DefenseRoster) Assert.Equal(pools[who.Id], m.StaminaOf(who));
            Assert.Equal(a != "P" && b != "P", m.CanSwapPitcher);
        }
    }

    [Fact]
    public void FieldSwapsRemainAvailableAfterPitcherAllowanceIsSpent()
    {
        var m = Game(true);
        Assert.True(m.SwapDefensePositions("P", "CF"));
        var pitcher = m.Pitcher;
        Assert.True(m.SwapDefensePositions("C", "SS"));
        Assert.False(m.SwapDefensePositions("P", "SS"));
        Assert.Equal(pitcher, m.Pitcher);
        Assert.True(m.SwapDefensePositions("RF", "LF"));
    }

    [Fact]
    public void InvalidOrCommittedEditsAreRejectedWithoutChangingTheDefense()
    {
        var m = Game(); var before = Map(m);
        Assert.False(m.SwapDefensePositions("P", "P"));
        Assert.False(m.SwapDefensePositions("bench", "CF"));
        Assert.False(m.SwapDefensePositions("p", "CF"));
        m.SetPaused(true);
        Assert.False(m.SwapDefensePositions("C", "SS"));
        m.SetPaused(false);
        Assert.True(m.PitchSetup.BeginCharge());
        Assert.False(m.CanArrangeDefense);
        Assert.False(m.SwapDefensePositions("C", "SS"));
        m.PitchSetup.ReleaseBall();
        Assert.False(m.SwapDefensePositions("C", "SS"));
        Assert.Equal(before, Map(m));
    }

    [Fact]
    public void BothTeamsKeepTheirOwnArrangementAcrossHalves()
    {
        var m = Game(true);
        Assert.True(m.SwapDefensePositions("C", "LF"));
        var home = Map(m);
        for (var i = 0; m.Top && i < 200; i++) m.Play(new PitchCommand(PitchFamily.Fastball, 0, false), new SwingCommand(false, 0, 0, false));
        Assert.False(m.Top);
        Assert.True(m.SwapDefensePositions("SS", "RF"));
        var away = Map(m);
        for (var i = 0; !m.Top && i < 200; i++) m.Play(new PitchCommand(PitchFamily.Fastball, 0, false), new SwingCommand(false, 0, 0, false));
        Assert.True(m.Top);
        Assert.Equal(home, Map(m));
        Assert.Equal(away, FieldingResolver.Assign(m.Away, m.Away.Pitcher));
    }
}
