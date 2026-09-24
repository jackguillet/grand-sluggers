using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// No plate-level chemistry (#891, P2-e; PH-16-R14, PH-16-R15), Appendix B.1 rows S-144 and S-145.
///
/// <b>S-144</b>: good-chemistry runners on base change nothing at the plate — not the oval the client
/// draws, not the barrel the resolver judges, not the exit. Before #891 they widened a slap
/// (×1.05 / 1.10 / 1.20) and multiplied a charged swing's exit (×1.10 / 1.25 / 1.50).
/// <b>S-145</b>: no item offer is ever made. Before #891 a good-chemistry on-deck hitter offered an
/// item on every at-bat. The item code stays dormant (spec §12).
///
/// Both roots: the class carries the compact trait, so the second CI run replays it on c80.
/// </summary>
public sealed class PlateChemistryScenarioTests
{
    readonly ContentCatalog _content = Shipped.Content;

    RulesTable R => _content.Rules;
    Park Harbor => _content.Parks[ExhibitionPick.DefaultPark];

    static readonly double[] Charges = [0, ChargeFeel.ChargeAt - 0.01, ChargeFeel.ChargeAt, 1];

    // ---------------------------------------------------------------------------------
    // S-144  Good-chemistry runners on base change neither the barrel nor the exit
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S138_BuddiesOnBaseChangeNeitherTheBarrelNorTheExit()
    {
        var resolver = new AtBatResolver(_content.Chemistry, R, _content.StarSkills);
        var pitcher = _content.Must("vale");
        var bats = new BatItem?[] { null }.Concat(_content.Bats.Values.OrderBy(b => b.Id)).ToArray();
        var cases = 0;
        foreach (var contact in Enumerable.Range(1, 10))
        foreach (var hand in new[] { Hand.R, Hand.L })
        {
            var hitter = Hitter(contact, hand);
            var buddies = _content.Characters.Values
                .Where(r => r.Id != hitter.Id && _content.Chemistry.Between(hitter, r) == Chemistry.Good)
                .OrderBy(r => r.Id).Take(3).ToList();
            Assert.NotEmpty(buddies);
            foreach (var bat in bats)
            foreach (var charge in Charges)
            foreach (var box in new[] { 0.0, -0.6 })
            {
                var alone = SweetSpot.Oval(hitter, bat, charge, box, R);
                var (cx, cy) = (alone.CenterX, alone.CenterY);
                // The heart, inside the drawn line, just outside it, and the sour rim: perfect, nice and sour.
                var crossings = new[] { (cx, cy), (cx + alone.TipHalfFt * 0.6, cy), (cx - alone.HandleHalfFt * 0.98, cy),
                    (cx, cy + alone.HalfHeightFt * 1.02), (cx + alone.TipHalfFt * 1.2, cy) };
                foreach (var n in Enumerable.Range(1, buddies.Count))
                {
                    var runners = buddies.Take(n).ToList();
                    foreach (var (x, y) in crossings)
                    {
                        var none = resolver.Resolve(Swing(pitcher, hitter, bat, [], charge, box, x, y), Harbor, new Random(7));
                        var with = resolver.Resolve(Swing(pitcher, hitter, bat, runners, charge, box, x, y), Harbor, new Random(7));
                        Assert.Equal(none, with);
                        cases++;
                    }
                }
            }
        }
        Assert.True(cases >= 10 * 2 * bats.Length * Charges.Length * 2 * 5, $"{cases} cases");

        // The swing's barrel takes no runners at all: the oval is a function of the hitter, the bat,
        // the charge and the box, so nothing on the bases can reach it.
        var p = typeof(SweetSpot).GetMethod(nameof(SweetSpot.Oval))!.GetParameters();
        Assert.Equal(["batter", "bat", "charge01", "boxOffsetX", "rules"], p.Select(x => x.Name!).ToArray());
        Assert.DoesNotContain(typeof(ChemistryTable).GetMethods(), m => m.Name is "BuddiesOnBase" or "ChargePowerMul");
    }

    [Fact]
    public void S138_AChargedPerfectWithThreeBuddiesIsTheSameBallAsWithNone()
    {
        // The case the old multiplier moved most: a MAX charge, a perfect crossing, three buddies (×1.50).
        var resolver = new AtBatResolver(_content.Chemistry, R, _content.StarSkills);
        var rio = _content.Must("rio");
        var buddies = new[] { _content.Must("nico"), _content.Must("pip"), _content.Must("vale") };
        Assert.All(buddies, b => Assert.Equal(Chemistry.Good, _content.Chemistry.Between(rio, b)));
        var (cx, cy) = SweetSpot.WorldCenter(0);
        var none = resolver.Resolve(Swing(_content.Must("ashlord"), rio, null, [], 1, 0, cx, cy), Harbor, new Random(3));
        var with = resolver.Resolve(Swing(_content.Must("ashlord"), rio, null, buddies, 1, 0, cx, cy), Harbor, new Random(3));
        Assert.Equal(ContactQuality.Perfect, with.Quality);
        Assert.Equal(none.ExitVeloMph, with.ExitVeloMph);
        Assert.Equal(none, with);
    }

    // ---------------------------------------------------------------------------------
    // S-145  No item offer is ever made
    // ---------------------------------------------------------------------------------

    [Fact]
    public void S139_NoBatterOnDeckPairOffersAnItem()
    {
        var everyone = _content.Characters.Values.OrderBy(c => c.Id).ToList();
        var goodPairs = 0;
        foreach (var batter in everyone)
        {
            Assert.False(_content.Chemistry.ChemistryItemOffered(batter, null));
            foreach (var onDeck in everyone.Where(c => c.Id != batter.Id))
            {
                if (_content.Chemistry.Between(batter, onDeck) == Chemistry.Good) goodPairs++;
                Assert.False(_content.Chemistry.ChemistryItemOffered(batter, onDeck), batter.Id + " / " + onDeck.Id);
            }
        }
        Assert.True(goodPairs > 0, "the pairs that offered an item before #891 are still good chemistry");

        // Through the resolver: a fair ball with a good-chemistry hitter on deck carries no offer.
        var resolver = new AtBatResolver(_content.Chemistry, R, _content.StarSkills);
        var (cx, cy) = SweetSpot.WorldCenter(0);
        var hit = resolver.Resolve(Swing(_content.Must("ashlord"), _content.Must("rio"), null, [], 0, 0, cx, cy)
            with { OnDeck = _content.Must("nico") }, Harbor, new Random(1));
        Assert.NotEqual(ContactQuality.Miss, hit.Quality);
        Assert.False(hit.ChemistryItemOffered);
    }

    [Fact]
    public void S139_NoItemIsOfferedOrThrownInACpuGame()
    {
        var pairs = new[] { ("rio", "ashlord"), ("zig", "konga"), ("fenn", "brondo"), ("konga", "rio"), ("vale", "brondo") };
        var atBats = 0;
        foreach (var (h, a) in pairs)
        {
            var match = Match.Exhibition(_content, h, a, innings: 3, seed: 1);
            var guard = 0;
            while (!match.Over && guard++ < 2000)
            {
                match.AutoPlay();
                Assert.False(match.LivePlay.ItemLanded, $"{h} v {a}: an item landed");
            }
            Assert.True(match.Over);
            foreach (var ev in match.Log)
            {
                atBats++;
                Assert.False(ev.AtBat.ChemistryItemOffered, $"{h} v {a}: {ev.Caption}");
            }
        }
        Assert.True(atBats > 0);
    }

    // ---------------------------------------------------------------------------------

    /// <summary>A hand-built hitter: pip with Contact authored and a batting hand (a fixture; no data file authors a value).</summary>
    Character Hitter(int contact, Hand bats)
    {
        var who = _content.Must("pip");
        return who with { Bats = bats, Stats = new Stats(who.Stats.Pitch, 5, who.Stats.Field, 5) { Contact = contact } };
    }

    static AtBatInput Swing(Character pitcher, Character batter, BatItem? bat, IReadOnlyList<Character> runners,
        double charge, double box, double crossingX, double crossingY) => new(
        pitcher, batter, null, runners,
        ChargePitch: false, ChangeupPitch: false, TimingErrorFrames: 0,
        UseStarPitch: false, UseStarSwing: false, Bat: bat, PitcherStamina: 100,
        Charge01: charge, BoxOffsetX: box, CrossingX: crossingX, CrossingY: crossingY);
}
