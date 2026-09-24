using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;

namespace GrandSluggers.Cli;

sealed class StillsCommand : Command
{
    public override string Name => "stills";
    public override IReadOnlyList<string> Usage => ["stills"];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var catalog = DualStills.Load(content.Root, new List<string>());
        Console.WriteLine($"STILLS drop {catalog.Drop}  kinds {catalog.Kinds.Count}");
        foreach (var kind in catalog.Kinds)
            Console.WriteLine($"  {kind.Id,-12} dcc {kind.Dcc,-22} in-game {string.Join(" ", kind.InGame)}");
        Console.WriteLine($"  critic {catalog.Critic.Skill}  mayPassLook {catalog.Critic.MayPassLook}  mayClose188 {catalog.Critic.MayClose188}");
        return Report(DualStills.Validate(content.Root), "dual stills match spec §4; critic files, does not pass");
    }
}
