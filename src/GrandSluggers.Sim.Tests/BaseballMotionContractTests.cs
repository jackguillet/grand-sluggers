using System.Text.Json.Nodes;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class BaseballMotionContractTests
{
    static JsonNode Art(string name)
    {
        var content = ContentCatalog.Load();
        return JsonNode.Parse(File.ReadAllText(Path.Combine(content.Root, "art", name)))!;
    }

    [Fact]
    public void HumanRigSeparatesWristsAnklesAndPelvisAndRemainsSymmetric()
    {
        var rig = Art("rig.json");
        Assert.Equal(2, rig["revision"]!.GetValue<int>());
        var joints = rig["joints"]!.AsArray().ToDictionary(j => j!["name"]!.GetValue<string>(), j => j!);
        var parents = new Dictionary<string, string>
        {
            ["pelvis"]="root", ["spine"]="pelvis", ["torso"]="spine", ["neck"]="torso", ["head"]="neck"
        };
        foreach (var side in new[] { "l", "r" })
        {
            parents[side+"Clavicle"]="torso";
            parents[side+"Upper"]=side+"Clavicle";
            parents[side+"Fore"]=side+"Upper";
            parents[side+"Wrist"]=side+"Fore";
            parents[side+"Glove"]=side+"Wrist";
            parents[side+"Release"]=side+"Wrist";
            parents[side+"Thigh"]="pelvis";
            parents[side+"Shin"]=side+"Thigh";
            parents[side+"Foot"]=side+"Shin";
        }
        foreach (var (joint, parent) in parents)
            Assert.Equal(parent, joints[joint]["parent"]!.GetValue<string>());
        foreach (var name in joints.Keys.Where(n => n.StartsWith("l")))
        foreach (var endpoint in new[] { "head", "tail" })
        for (var axis = 0; axis < 3; axis++)
            Assert.Equal(joints[name][endpoint]![axis]!.GetValue<double>() * (axis == 0 ? -1 : 1),
                joints["r"+name[1..]][endpoint]![axis]!.GetValue<double>(), 8);
        var anatomy=rig["anatomy"]!;
        var height=anatomy["height"]!.GetValue<double>();
        Assert.InRange(height / anatomy["headDiameter"]!.GetValue<double>(), 5.0, 6.0);
        Assert.InRange(joints["pelvis"]["head"]![2]!.GetValue<double>()/height, .42, .50);
        Assert.Equal(anatomy["headDiameter"]!.GetValue<double>()/2, SwingPresentation.HeadRadius, 8);
        Assert.Equal(anatomy["headCenter"]![2]!.GetValue<double>(), SwingPresentation.HeadCenterAtRest.Y, 8);
    }

    [Theory]
    [InlineData(Hand.R, Hand.L, "glove-brown")]
    [InlineData(Hand.L, Hand.R, "glove-brown-R")]
    public void FieldingGloveIsOppositeThrowingHand(Hand throws, Hand worn, string mesh)
    {
        Assert.Equal(worn, BaseballEquipment.GloveHand(throws));
        Assert.Equal(mesh, BaseballEquipment.GloveMesh(throws));
        Assert.Equal(mesh.Replace("brown", "gold"), BaseballEquipment.GloveMesh(throws, true));
        foreach (var bats in new[] { Hand.R, Hand.L })
        {
            Assert.Equal(Motion.ClipFile("throw", throws), Motion.ClipFile(Motion.Verb.Throw, bats, throws));
            Assert.Equal(Motion.ClipFile("pitch-charge", throws), Motion.ClipFile(Motion.Verb.ThrowPitch, bats, throws, 1));
        }
    }

    [Fact]
    public void PitchCatalogHasSharedReleaseAndContinuousChargeHold()
    {
        var takes=Art("baseball-takes.json")["takes"]!.AsArray();
        foreach (var row in takes)
        {
            var id=row!["id"]!.GetValue<string>();
            Assert.True(Motion.TryClip(id,out var clip));
            Assert.Equal(clip.MarkAt,row["releaseAt"]!.GetValue<double>(),8);
            Assert.Equal(clip.Duration,row["duration"]!.GetValue<double>(),8);
            var times=row["keys"]!.AsArray().Select(k=>k!["t"]!.GetValue<double>()).ToArray();
            Assert.Contains(clip.MarkAt,times);
            Assert.Equal(times.OrderBy(t=>t).Distinct(),times);
        }
        for (var i=0;i<=100;i++)
        {
            double charge=i/100.0;
            Assert.Equal(Motion.LoadAtFor(Motion.Verb.ThrowPitch,charge),AtBatMotion.PitchClipTime(0,charge),8);
            Assert.Equal(Motion.PitchRelease,AtBatMotion.PitchClipTime(Motion.PitchRelease,charge),8);
        }
        var normal=takes.First(t=>t!["id"]!.GetValue<string>()=="pitch")!;
        var charged=takes.First(t=>t!["id"]!.GetValue<string>()=="pitch-charge")!;
        var ready=charged["keys"]!.AsArray().First(k=>k!["t"]!.GetValue<double>()==Motion.PitchNormalLoadAt)!;
        Assert.True(JsonNode.DeepEquals(normal["keys"]![0]!["pose"],ready["pose"]));
        Assert.True(JsonNode.DeepEquals(normal["keys"]![0]!["feet"],ready["feet"]));
    }

    [Fact]
    public void EquipmentProfilePreservesTheMeasuredBatAndBuntGrip()
    {
        var equipment=Art("baseball-equipment.json");
        var bat=equipment["bat"]!;
        var profile=bat["profile"]!.AsArray();
        Assert.Equal(-SwingPresentation.ModelCenterFromGrip,bat["grip"]!.GetValue<double>(),8);
        Assert.Equal(SwingPresentation.BatStartFromGrip-SwingPresentation.ModelCenterFromGrip,profile[0]![0]!.GetValue<double>(),8);
        Assert.Equal(SwingPresentation.BarrelFromModelCenter,profile.Last()![0]!.GetValue<double>(),8);
        Assert.Equal(SwingPresentation.ModelBarrelRadius,profile.Max(k=>k![1]!.GetValue<double>()),8);
        var bunt=Art("baseball-takes.json")["bunt"]!;
        Assert.True(bunt["leadAlong"]!.GetValue<double>()<SwingPresentation.HandleLength);
        Assert.InRange(bunt["topAlong"]!.GetValue<double>(),SwingPresentation.HandleLength,SwingPresentation.BarrelReach);
        Assert.Equal(4,equipment["glove"]!["fingers"]!.GetValue<int>());
    }
}
