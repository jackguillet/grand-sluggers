using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// SC-24: the captain board and the lineup card show four bars — Bat, Pitch, Field, Run — and each value is the
/// sim's derived bar, for every captain and every role player, whichever seat or pad is looking.
/// </summary>
public class StatBarsTests
{
    readonly ContentCatalog _content = Shipped.Content;

    static int[] Shown(Character who)
    {
        var card = CharacterCard.Of(who);
        var shown = new int[StatBars.Count];
        for (var i = 0; i < StatBars.Count; i++) shown[i] = StatBars.Value(card.Stats, i);
        return shown;
    }

    static int[] Derived(Character who) => [who.Stats.Bat, who.Stats.Pitch, who.Stats.Field, who.Stats.Run];

    [Fact]
    public void TheFourBarsAreBatPitchFieldRun()
    {
        Assert.Equal(4, StatBars.Count);
        Assert.Equal(["BAT", "PITCH", "FIELD", "RUN"], StatBars.Labels);
    }

    [Fact]
    public void EveryCharacterShowsItsDerivedBars()
    {
        Assert.NotEmpty(_content.Characters);
        foreach (var who in _content.Characters.Values)
        {
            Assert.Equal(Derived(who), Shown(who));
            for (var i = 0; i < StatBars.Count; i++)
            {
                var card = CharacterCard.Of(who);
                Assert.Equal(StatBars.Value(card.Stats, i).ToString(System.Globalization.CultureInfo.InvariantCulture), StatBars.ValueText(card.Stats, i));
                Assert.Equal(CharacterCard.BarFill(StatBars.Value(card.Stats, i)), StatBars.Fill(card.Stats, i), 6);
            }
        }
    }

    [Fact]
    public void TheBarIsTheRoundedMeanOfItsSubStatsNotAnyOneSubStat()
    {
        // Contact 7 + Power 8 → 7.5 → 8; four Pitch sub-stats 5,5,5,6 → 5.25 → 5; Hands 6 + Arm 7 → 6.5 → 7.
        var stats = new Stats(7, 8, 5, 5, 5, 6, 6, 7, 4);
        Assert.Equal(8, StatBars.Value(stats, 0));
        Assert.Equal(5, StatBars.Value(stats, 1));
        Assert.Equal(7, StatBars.Value(stats, 2));
        Assert.Equal(4, StatBars.Value(stats, 3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EitherCaptainPanelShowsTheSameBarsForTheSameCaptain(bool versus)
    {
        foreach (var id in _content.CaptainIds)
        {
            var board = new CaptainSelection(_content, new ExhibitionPick(id, id, ExhibitionPick.DefaultPark, true), versus);
            Assert.Equal(id, board.Id(0));
            Assert.Equal(id, board.Id(1));
            var one = Shown(_content.Must(board.Id(0)));
            var two = Shown(_content.Must(board.Id(1)));
            Assert.Equal(one, two);
            Assert.Equal(Derived(_content.Must(id)), one);
        }
    }

    [Fact]
    public void EveryCaptainIsReachableOnTheBoardAndShowsItsBars()
    {
        var first = _content.CaptainIds[0];
        foreach (var versus in new[] { false, true })
        {
            var board = new CaptainSelection(_content, new ExhibitionPick(first, first, ExhibitionPick.DefaultPark, true), versus);
            for (var step = 0; step < _content.CaptainIds.Count; step++)
            {
                var panel = versus ? step % 2 : 0;
                Assert.Equal(Derived(_content.Must(board.Id(panel))), Shown(_content.Must(board.Id(panel))));
                board.Move(0, 1);
                board.Move(1, 1);
            }
        }
    }

    [Fact]
    public void BothLineupSeatsInspectingOnePlayerReadTheSameBars()
    {
        var captains = _content.CaptainIds;
        var lineup = LineupScreens.Open(_content, captains[0], captains[1], LineupSeat.Pad1, LineupSeat.Pad2);
        Assert.NotEmpty(lineup.Pool);
        var roleSeen = false;
        for (var i = 0; i < lineup.Pool.Count; i++)
        {
            Assert.True(lineup.FocusCell(LineupSeat.Pad1, LineupFocus.Pool, i));
            Assert.True(lineup.FocusCell(LineupSeat.Pad2, LineupFocus.Pool, i));
            var one = lineup.InspectedBy(LineupSeat.Pad1)!;
            var two = lineup.InspectedBy(LineupSeat.Pad2)!;
            Assert.Equal(one.Id, two.Id);
            Assert.Equal(Shown(one), Shown(two));
            Assert.Equal(Derived(one), Shown(one));
            roleSeen |= !one.Captain;
        }
        Assert.True(roleSeen, "the pool should hold role players as well as captains");
    }

    public static TheoryData<string> Layouts => new() { "captain", "lineup" };

    static (StatBarLayout Bars, float PanelW, float PanelH, float Floor, int MinFont) Layout(string which) => which == "captain"
        ? (CarnivalFront.CaptainCardBars, CarnivalFront.CaptainPanel(0).W, CarnivalFront.CaptainPanel(0).H,
            CarnivalFront.CaptainCardVerbsTop, 24)
        : (CarnivalFront.LineupCardBars, (float)(LineupLayout.CardPanel(true).W * 1280),
            (float)(LineupLayout.CardPanel(true).H * 800), CarnivalFront.LineupCardVerbsTop, 18);

    [Theory]
    [MemberData(nameof(Layouts))]
    public void TheBarsReadAtCouchSizeAndFitTheirCard(string which)
    {
        var (bars, w, h, floor, minFont) = Layout(which);
        // Couch size: bar labels and values are larger than the board's body copy (21 px select, 15 px lineup).
        Assert.True(bars.LabelFont >= minFont, which + " label font " + bars.LabelFont);
        Assert.True(bars.ValueFont >= bars.LabelFont, which + " value font");
        Assert.True(bars.BarH >= 12, which + " bar thickness " + bars.BarH);
        // Label, bar, value sit left to right without touching, inside the card.
        Assert.True(bars.LabelX + bars.LabelW <= bars.BarX, which + ": label runs into the bar");
        Assert.True(bars.BarX + bars.BarW <= bars.ValueX, which + ": bar runs into the value");
        Assert.True(bars.ValueX + bars.ValueW <= w - 4, which + ": value leaves the card");
        // The longest label fits its column at its font (bold caps, ~0.62 em a glyph); "10" fits the value column.
        var longest = 0;
        foreach (var label in StatBars.Labels) longest = Math.Max(longest, label.Length);
        Assert.True(longest * .62f * bars.LabelFont <= bars.LabelW, which + ": longest label is clipped");
        Assert.True(2 * .62f * bars.ValueFont <= bars.ValueW, which + ": 10 is clipped");
        // The bar sits inside its row and the four rows end above the verb lines.
        Assert.True(bars.BarH <= bars.Pitch && bars.Pitch >= bars.LabelFont, which + " row pitch");
        Assert.True(bars.Bottom <= floor, which + ": bars run into the verbs");
        Assert.True(floor < h, which + ": verbs start outside the card");
    }

    [Fact]
    public void TheCaptainCardsVerbLinesEndInsideTheCard()
    {
        var last = CarnivalFront.CaptainCardVerbsTop + 3 * CarnivalFront.CaptainCardVerbPitch + 24;
        Assert.True(last <= CarnivalFront.CaptainPanel(0).H - 4, "verb lines end at " + last);
    }
}
