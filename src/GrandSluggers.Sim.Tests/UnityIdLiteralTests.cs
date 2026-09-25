using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Unity client names no captain and no park (#1050): portraits come from the skins catalog, the kit from the park's
/// placed row, the defaults from <see cref="GrandSluggers.Sim.Front.ExhibitionPick"/>. A captain or park id spelled in a
/// Runtime script is a special case the next captain or park would have to copy.
/// </summary>
public sealed class UnityIdLiteralTests
{
    [Fact]
    public void NoRuntimeScriptSpellsACaptainOrParkId()
    {
        var runtime = Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..", "unity", "Assets", "Scripts", "Runtime"));
        var ids = Shipped.Content.CaptainIds.Concat(Shipped.Content.Parks.Keys).Select(id => "\"" + id + "\"").ToList();
        var offenders = Directory.GetFiles(runtime, "*.cs")
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (File: Path.GetFileName(f), Line: line, At: i + 1)))
            .Where(x => !x.Line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .SelectMany(x => ids.Where(id => x.Line.Contains(id, StringComparison.Ordinal)).Select(id => $"{x.File}:{x.At}: {id}"))
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void EveryAudioSlotIsTheWavTheGameLoads() =>
        Assert.All(Shipped.Content.Art.Audio, e => Assert.Equal("data/art/audio-clips/" + e.Id + ".wav", e.Slot));

    [Fact]
    public void OnlyAPlacedParkNamesAKitMesh()
    {
        foreach (var park in Shipped.Content.Art.Parks)
            if (park.Placed) Assert.EndsWith(".fbx", park.Slot, StringComparison.Ordinal);
    }
}
