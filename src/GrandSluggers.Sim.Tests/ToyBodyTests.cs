using System.Text.Json.Nodes;
using GrandSluggers.Sim.Tooling;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// CF-2 (#1115): the toy body. About four heads tall (CH-02), a compressed height ladder (CH-03), and head, arms and
/// torso that shape the 3D body from data on the one rig (CH-04). The still is the gate (SC-08); these hold the numbers
/// the still is built from, for the next captain as well as these seven.
/// </summary>
public class ToyBodyTests
{
    static readonly ContentCatalog Content = Shipped.Content;

    static JsonNode Rig() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(Content.Root.Shipped, "art", "rig.json")))!;

    static double[] Point(JsonNode node) => node.AsArray().Select(v => v!.GetValue<double>()).ToArray();

    static Silhouette.Spec Body(string id) => Silhouette.Proportions(Content, id);

    [Fact]
    public void SC05_TheRestRigIsAboutFourHeadsTall()
    {
        var anatomy = Rig()["anatomy"]!;
        var heads = anatomy["height"]!.GetValue<double>() / anatomy["headDiameter"]!.GetValue<double>();
        // CH-02 picked about four (5.3 before). The band is the proposal; Jack judges the still.
        Assert.InRange(heads, 3.8, 4.2);
        Assert.Equal(Silhouette.RestHeadsTall, heads, 8);
        // The neutral build is the rest rig: its measured head count is the rig's.
        var neutral = new Silhouette.Spec(1, 1, (float)Silhouette.HeadBuild.Neutral, (float)Silhouette.ArmsBuild.Neutral,
            (float)Silhouette.TorsoBuild.Neutral);
        Assert.Equal(Silhouette.RestHeadsTall, Silhouette.HeadsTall(neutral), 6);
        // A big-head captain reads more toy than the rest rig, a small-head one less; nobody drifts back to 5.3.
        foreach (var id in Shipped.CaptainIds)
            Assert.InRange(Silhouette.HeadsTall(Body(id)), 3.4, 4.6);
        Assert.True(Silhouette.HeadsTall(Body("zig")) < Silhouette.RestHeadsTall, "zig is the huge head");
        Assert.True(Silhouette.HeadsTall(Body("fenn")) < Silhouette.RestHeadsTall, "fenn is the big head");
    }

    [Fact]
    public void SC06_TheLadderFitsTheCapAndTheFloorFromTheOneFunction()
    {
        // The head top in world feet, the head build included: the number the still, the select camera and Unity's
        // root scale plus shape keys all draw. CH-03: tallest at most 1.35 x Rio, shortest at least 0.70 x Rio.
        var rio = Silhouette.HeadTopFt(Body("rio"));
        var tops = Shipped.CaptainIds.ToDictionary(id => id, id => Silhouette.HeadTopFt(Body(id)));
        foreach (var (id, top) in tops)
        {
            Assert.True(top <= 1.35 * rio + 1e-9, $"{id} stands {top / rio:0.000} x Rio; the cap is 1.35");
            Assert.True(top >= 0.70 * rio - 1e-9, $"{id} stands {top / rio:0.000} x Rio; the floor is 0.70");
            Assert.Equal(top, StillPose.CharHeadTopY(Content, id), 9);
        }
        // The ladder keeps its order and each cut's read (silhouette bible).
        var order = new[] { "zig", "reed", "fenn", "rio", "brondo", "sable", "vale", "hollis", "konga", "ashlord" };
        for (var i = 1; i < order.Length; i++)
            Assert.True(tops[order[i]] > tops[order[i - 1]], $"{order[i]} must stand taller than {order[i - 1]}");
        Assert.Equal(order.OrderBy(x => x), Shipped.CaptainIds.OrderBy(x => x));
    }

    [Fact]
    public void SC07_ArmsTorsoAndHeadShowInTheBody()
    {
        // Each channel is data: 1 + gain x (proportion / neutral - 1). The ratio between two captains' builds is their
        // proportions' ratio through the one function, so Konga's arms, Brondo's torso and Fenn's head read from data.
        static double Expected(Silhouette.BuildChannel c, double a, double b) => c.ScaleFor(a) / c.ScaleFor(b);
        var rio = Body("rio");
        var konga = Body("konga");
        var brondo = Body("brondo");
        var fenn = Body("fenn");
        Assert.Equal(Expected(Silhouette.ArmsBuild, konga.Arms, rio.Arms),
            Silhouette.Build(konga).Arms / Silhouette.Build(rio).Arms, 9);
        Assert.Equal(Expected(Silhouette.TorsoBuild, brondo.Torso, rio.Torso),
            Silhouette.Build(brondo).Torso / Silhouette.Build(rio).Torso, 9);
        Assert.Equal(Expected(Silhouette.HeadBuild, fenn.Head, rio.Head),
            Silhouette.Build(fenn).Head / Silhouette.Build(rio).Head, 9);
        Assert.True(Silhouette.Build(konga).Arms > 1.3 * Silhouette.Build(rio).Arms, "konga has the ape arms");
        Assert.True(Silhouette.Build(brondo).Torso > 1.2 * Silhouette.Build(rio).Torso, "brondo has the brick torso");
        Assert.True(Silhouette.Build(fenn).Head > 1.1 * Silhouette.Build(rio).Head, "fenn has the big head");

        // Every captain's build fits the shape keys on the mesh, so Unity draws what the sim measures (no clamp).
        foreach (var id in Shipped.CaptainIds)
        {
            var build = Silhouette.Build(Body(id));
            foreach (var (channel, scale) in new[]
                     { (Silhouette.HeadBuild, build.Head), (Silhouette.ArmsBuild, build.Arms), (Silhouette.TorsoBuild, build.Torso) })
            {
                Assert.InRange(scale, channel.Min, channel.Max);
                // The weights Unity sets redraw exactly this scale from the two keys.
                var (up, down) = channel.Weights(scale);
                Assert.Equal(scale, 1 + up * (channel.Max - 1) - down * (1 - channel.Min), 9);
                Assert.True(up == 0 || down == 0, $"{id} {channel.Id} pulls both keys");
            }
            // The head build moves the drawn head: the measured head top grows with it.
            var spec = Body(id);
            Assert.Equal(Silhouette.HeadCenterRig(spec).Y + Silhouette.HeadDiameter / 2 * build.Head, Silhouette.HeadTopRig(spec), 9);
        }
        var weights = Silhouette.BuildWeights(konga).ToDictionary(w => w.Key, w => w.Weight);
        Assert.Equal(6, weights.Count);
        Assert.True(weights["arms+"] > 0.9 && weights["arms-"] == 0);
    }

    [Fact]
    public void TheKneeAndChestLandmarksAreRigDataInWorldFeetPerCaptain()
    {
        var joints = Rig()["joints"]!.AsArray().ToDictionary(j => j!["name"]!.GetValue<string>(), j => j!);
        var anatomy = Rig()["anatomy"]!;
        // The knee is the thigh -> shin joint; the chest mark sits on the torso bone.
        Assert.Equal(Point(joints["lShin"]["head"]!), Point(anatomy["knee"]!));
        Assert.Equal(Silhouette.KneeY, Point(anatomy["knee"]!)[2], 8);
        Assert.Equal(Silhouette.ChestY, Point(anatomy["chest"]!)[2], 8);
        Assert.InRange(Silhouette.ChestY, Point(joints["torso"]["head"]!)[2], Point(joints["torso"]["tail"]!)[2]);
        foreach (var id in Shipped.CaptainIds)
        {
            var who = Content.Must(id);
            var spec = Silhouette.Proportions(who);
            var (knee, chest, top) = Silhouette.Landmarks(who);
            Assert.Equal(Silhouette.KneeY * Silhouette.SharedRootScale(spec).Y, knee, 9);
            Assert.Equal(Silhouette.ChestY * Silhouette.SharedRootScale(spec).Y, chest, 9);
            Assert.True(0 < knee && knee < chest && chest < top, $"{id}: knee {knee:0.00}, chest {chest:0.00}, head top {top:0.00}");
        }
        // A sidekick stands on its species' landmarks (WD-27): two of one species stand alike.
        Assert.Equal(Content.Must("dart").Species, Content.Must("jester").Species);
        Assert.Equal(Silhouette.Landmarks(Content.Must("dart")), Silhouette.Landmarks(Content.Must("jester")));
    }

    [Fact]
    public void TheSimMirrorsOfTheRigAreTheRigsNumbers()
    {
        var rig = Rig();
        var anatomy = rig["anatomy"]!;
        Assert.Equal(Silhouette.RigHeight, anatomy["height"]!.GetValue<double>(), 8);
        Assert.Equal(Silhouette.HeadDiameter, anatomy["headDiameter"]!.GetValue<double>(), 8);
        var head = Point(anatomy["headCenter"]!);
        // Rig (x, y, z) is Blender; the sim is Unity batter-local: (-x, z, -y).
        Assert.Equal(new Vec3(-head[0], head[2], -head[1]), Silhouette.HeadCenterAtRest);
        var build = rig["build"]!;
        var pivot = Point(build["head"]!["pivot"]!);
        Assert.Equal(new Vec3(-pivot[0], pivot[2], -pivot[1]), Silhouette.HeadPivot);
        var joints = rig["joints"]!.AsArray().ToDictionary(j => j!["name"]!.GetValue<string>(), j => j!);
        Assert.Equal(Point(joints["head"]["head"]!), pivot);
        foreach (var channel in Silhouette.BuildChannels)
        {
            var row = build[channel.Id]!;
            Assert.Equal(channel.Neutral, row["neutral"]!.GetValue<double>(), 8);
            Assert.Equal(channel.Gain, row["gain"]!.GetValue<double>(), 8);
            Assert.Equal(channel.Min, row["min"]!.GetValue<double>(), 8);
            Assert.Equal(channel.Max, row["max"]!.GetValue<double>(), 8);
            Assert.True(channel.Min < 1 && channel.Max > 1, channel.Id + " keys must straddle the neutral body");
        }
    }
}
