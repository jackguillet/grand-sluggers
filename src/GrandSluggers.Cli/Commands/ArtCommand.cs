using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class ArtCommand : Command
{
    public override string Name => "art";
    public override IReadOnlyList<string> Usage => ["art"];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var art = content.Art;
        Console.WriteLine($"RIG    {art.Rig.Id}  bones {art.Rig.Bones.Count}  events {string.Join(",", art.Rig.Events)}");
        Console.WriteLine($"CLIPS  {art.Clips.Count}  {string.Join(" ", art.Clips.Select(c => c.Id))}");
        Console.WriteLine($"SKINS  {art.Skins.Count} captains authored, role players inherit body type");
        Console.WriteLine($"VFX    {art.Vfx.Count} events");
        Console.WriteLine($"AUDIO  {art.Audio.Count} events ({art.Audio.Count(e => e.Authored)} authored)");
        Console.WriteLine($"PARKS  {art.Parks.Count} kit slots ({art.Parks.Count(p => p.Placed)} placed)");
        foreach (var kit in art.Parks)
        {
            var empty = kit.Empty;
            var filled = ParkKitSlots.All.Count - empty.Count;
            Console.WriteLine($"  {kit.Id,-15} {filled,2}/{ParkKitSlots.All.Count} filled  empty: {(empty.Count == 0 ? "none" : string.Join(" ", empty))}");
        }
        Console.WriteLine($"FOLDERS {art.Folders.Count}");
        var errors = art.Validate(content)
            .Concat(AgentData.Validate(content.Root))
            .Concat(RuntimePackage.Validate(content.Root.Shipped))
            .Concat(DataNaming.Validate(content.Root.Shipped))
            .ToList();
        return Report(errors, "catalog matches roster, clips, parks; agent data and the runtime package are valid; data keys name their units");
    }
}
