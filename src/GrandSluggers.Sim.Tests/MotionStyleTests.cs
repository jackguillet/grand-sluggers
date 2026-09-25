using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>CF-5 (#1118): motion styles, signature beats, stride from ground speed, the walk / run floor (#1111), SC-08.</summary>
public class MotionStyleTests
{
    readonly ContentCatalog _content = Shipped.Content;
    ArtCatalog Art => _content.Art;

    static readonly string[] StyledVerbs = ["idle", "run", Motion.SwingSlapClip, Motion.SwingChargeClip, "pitch", "pitch-charge", "cheer"];

    string Unity(string slot) => Path.GetFullPath(Path.Combine(
        Directory.GetParent(_content.Root.Shipped)!.FullName, "unity", slot.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void SC20_EveryStyleHasItsOwnRunIdleStanceWindupAndSignatureForBothHands()
    {
        Assert.InRange(Art.Styles.Count, 7, 16);
        foreach (var style in Art.Styles)
        foreach (var clipId in StyledVerbs)
        {
            Assert.True(style.Owns(clipId), $"{style.Id} has no take of {clipId}");
            Assert.True(Art.TryClip(clipId, out var clip));
            foreach (var hand in clip.Handed ? new[] { Hand.R, Hand.L } : [Hand.R])
            {
                var (slot, player) = Art.ClipFiles(clip, hand, style);
                Assert.Contains("/" + style.Id + "/", slot);
                Assert.True(File.Exists(Unity(slot)), slot);
                Assert.True(File.Exists(Unity(player)), player);
            }
        }
    }

    [Fact]
    public void EveryStyledTakeIsVouchedForByTheBakeWithTheSharedTakesContracts()
    {
        Assert.True(Art.ReceiptFound, "data/art/takes-receipt.json");
        foreach (var style in Art.Styles)
        foreach (var clipId in style.Clips)
        {
            Assert.True(Art.TryClip(clipId, out var clip));
            foreach (var hand in clip.Handed ? new[] { Hand.R, Hand.L } : [Hand.R])
            {
                var shared = ArtCatalog.ClipFiles(clip, hand).Slot[(ArtCatalog.ClipRoot.Length + 1)..];
                var own = Art.ClipFiles(clip, hand, style).Slot[(ArtCatalog.ClipRoot.Length + 1)..];
                Assert.True(Art.Receipt.TryGetValue(own, out var row), own);
                Assert.Superset(Art.Receipt[shared].Contracts.ToHashSet(), row.Contracts.ToHashSet());
                Assert.Contains("sockets", row.Contracts);
            }
        }
        var errors = Art.Validate(_content);
        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    [Fact]
    public void SC22_EveryCaptainsStyleAndSignatureResolveToRealTakes()
    {
        var signatures = new HashSet<string>();
        foreach (var id in _content.CaptainIds)
        {
            var who = _content.Must(id);
            var style = Art.StyleOf(who);
            Assert.NotNull(style);
            Assert.True(signatures.Add(style!.Signature), $"{id} shares a signature beat");
            foreach (var verb in new[] { Motion.Verb.Cheer, Motion.Verb.Idle, Motion.Verb.Run, Motion.Verb.ChargeSwing, Motion.Verb.ChargePitch })
            {
                var hand = Motion.UsesBattingHand(verb) ? who.Bats : who.Throws;
                var file = Motion.ClipFor(verb, hand, style);
                Assert.StartsWith(style.Id + "/", file);
            }
        }
        // Role players wear their captain's body, so they move in its style.
        foreach (var who in _content.Characters.Values.Where(c => !c.Captain))
            Assert.Equal(Art.StyleOf(_content.Must(who.BodyType))!.Id, Art.StyleOf(who)!.Id);
    }

    [Fact]
    public void ClipForFallsBackToTheSharedTakeUnlessTheStyleOwnsIt()
    {
        Assert.True(Art.TryStyle("harbor-kid", out var kid));
        Assert.True(Art.TryStyle("ape", out var ape));
        Assert.Equal("harbor-kid/run", Motion.ClipFor(Motion.Verb.Run, Hand.R, kid));
        Assert.Equal("harbor-kid/swing-charge-L", Motion.ClipFor(Motion.Verb.ChargeSwing, Hand.L, kid));
        Assert.Equal("throw-L", Motion.ClipFor(Motion.Verb.Throw, Hand.L, kid));
        Assert.Equal("catch", Motion.ClipFor(Motion.Verb.Catch, Hand.R, kid));
        Assert.Equal("run", Motion.ClipFor(Motion.Verb.Run, Hand.R, null));
        // The ape's reach moves the joints, so it owns every clip.
        Assert.True(ape.OwnsEveryClip);
        Assert.Equal("ape/throw-L", Motion.ClipFor(Motion.Verb.Throw, Hand.L, ape));
        Assert.Equal("ape/bunt", Motion.ClipFor(Motion.Verb.Bunt, Hand.R, ape));
    }

    [Fact]
    public void SC21_TheRunLoopTurnsWithGroundCoveredNotTime()
    {
        foreach (var id in _content.CaptainIds)
        {
            var who = _content.Must(id);
            var cycle = Gait.CycleFt(Art.StyleOf(who)!.RunCycle, who.Proportions);
            var top = FieldingResolver.ChaseSpeedFt(who, false, _content.Rules);
            const double seconds = 0.2, dt = 1.0 / 60;
            double full = 0, half = 0;
            for (var t = 0.0; t < seconds - 1e-9; t += dt)
            {
                full = Gait.Advance(full, top * dt, cycle);
                half = Gait.Advance(half, top / 2 * dt, cycle);
            }
            // Short enough not to wrap: the phase is the distance over the cycle.
            Assert.True(top * seconds < cycle, $"{id} wraps in {seconds} s");
            Assert.Equal(top * seconds / cycle, full, 9);
            Assert.Equal(full / 2, half, 9);
            Assert.Equal(0.25, Gait.Advance(0.25, 0, cycle));
        }
    }

    [Fact]
    public void Issue1111_AFullSpeedChaseIsTheRunTakeForEveryCaptainOnEveryHitClass()
    {
        var gait = Art.Gait ?? throw new InvalidOperationException("ContentCatalog sets the gait profile");
        var chase = _content.Rules.Fielding.Chase;
        foreach (var id in _content.CaptainIds)
        {
            var who = _content.Must(id);
            var floor = gait.RunFloorFt(who);
            var ground = FieldingResolver.ChaseSpeedFt(who, false, _content.Rules);
            foreach (var mul in new[] { 1.0, chase.OutfieldAirMul, chase.InfieldAirMul })
                Assert.Equal(Motion.Verb.Run, Gait.Locomotion(ground * mul, floor, false, Motion.Verb.Field));
            // Slower than the floor it walks; the backpedal always walks; standing is the still verb.
            Assert.Equal(Motion.Verb.Walk, Gait.Locomotion(Math.Max(CartoonJuice.WalkFtPerSec + 0.1, floor - 0.5), floor, false, Motion.Verb.Field));
            Assert.Equal(Motion.Verb.Walk, Gait.Locomotion(ground, floor, true, Motion.Verb.Field));
            Assert.Equal(Motion.Verb.Field, Gait.Locomotion(CartoonJuice.WalkFtPerSec, floor, false, Motion.Verb.Field));
            Assert.True(floor > CartoonJuice.WalkFtPerSec, $"{id} run floor {floor} under the walk");
        }
    }

    [Fact]
    public void SC08_KongaHasTheLongArmsAndAshlordTheHeavyBoots()
    {
        var konga = Art.StyleOf(_content.Must("konga"))!;
        var ashlord = Art.StyleOf(_content.Must("ashlord"))!;
        Assert.NotEqual(konga.Id, ashlord.Id);
        Assert.True(konga.Reach >= 1.15, "Konga's arms are longer than the shared arm");
        Assert.Equal(1.0, ashlord.Reach);
        Assert.True(ashlord.Boots >= 1.25, "Ashlord's boots are bigger than the shared shoe");
        Assert.Equal(1.0, konga.Boots);
        // A captain whose build moves the joints owns every take: the shared takes would not reach its hands.
        foreach (var style in Art.Styles)
            Assert.Equal(style.OwnsEveryClip, Motion.ClipIds.All(style.Owns));
        var weights = Silhouette.StyleWeights(konga).ToDictionary(w => w.Key, w => w.Weight);
        Assert.True(weights["reach+"] > 0.5 && weights["reach-"] == 0 && weights["boots+"] == 0);
    }
}
